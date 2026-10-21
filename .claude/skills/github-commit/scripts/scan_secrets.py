#!/usr/bin/env python3
"""Pre-commit secret scan for the github-commit skill.

Scans everything that could be committed: added lines in both the staged
and the working-tree versions of tracked files, plus untracked, non-ignored
files (full contents). Prints a compact report so the agent does not have
to scan diffs by eye.

Usage:
    python3 scan_secrets.py [path ...]     # run from inside the repo

Also prints the branch/upstream line and the last 3 commits, so it doubles as
the recon step.

Paths, if given, limit the scan to those pathspecs.

Exit codes: 0 = clean, 1 = findings (BLOCK or REVIEW), 2 = error.
"""

import codecs
import os
import re
import subprocess
import sys
from collections import defaultdict

EMPTY_TREE = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"
MAX_BYTES = 1_000_000
MAX_LISTED = 60          # changeset lines before collapsing untracked dirs
MAX_FINDINGS = 30        # finding lines printed in total
MAX_PER_FILE = 5         # finding lines printed per file
# Override user config that would change diff output we parse.
GIT_CFG = ["-c", "core.quotepath=false", "-c", "diff.noprefix=false", "-c", "diff.mnemonicPrefix=false",
           "-c", "diff.relative=false", "-c", "color.ui=false", "-c", "diff.external="]
# Dependency/cache folders: listed collapsed, contents not scanned.
DEP_DIRS = {"node_modules", "bower_components", ".venv", "venv", "__pycache__", ".next", ".nuxt",
            ".gradle", ".pytest_cache", ".mypy_cache", ".tox", ".terraform"}

# --- File-name rules ---------------------------------------------------------
# (severity, rule id, regex on the basename, description)
NAME_RULES = [
    ("BLOCK", "env-file", r"^\.env(rc)?(\..+)?$|\.env$", "environment file"),
    ("BLOCK", "private-key-file", r"^(id_rsa|id_dsa|id_ecdsa|id_ed25519)$|\.(pfx|p12|jks|keystore)$", "private key / keystore"),
    ("BLOCK", "secret-store", r"^(credentials|secrets|serviceAccountKey)\.json$|\.secrets\.", "secret store file"),
    ("REVIEW", "key-file", r"\.(pem|key)$", "key/cert file - check for private key material"),
    ("REVIEW", "local-config", r"\.local\.(json|ya?ml|toml|ini|env)$|^config\.local\.|^appsettings\.Development\.json$", "local config override"),
    ("REVIEW", "cloud-config", r"^(google-services\.json|GoogleService-Info\.plist)$", "cloud project config"),
]
TEMPLATE_NAME = re.compile(r"(\.|^)(example|sample|template|dist)(\.|$)", re.I)
CONFIG_EXT = re.compile(r"\.(ya?ml|properties|ini|toml|conf|cfg|env)$|^\.env|^Dockerfile", re.I)

# --- Content rules -----------------------------------------------------------
# Strong token formats: reported even in template files.
TOKEN_RULES = [
    ("private-key", r"-----BEGIN (?:RSA |EC |DSA |OPENSSH |PGP |ENCRYPTED )?PRIVATE KEY-----"),
    ("github-token", r"\b(?:ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{30,}|\bgithub_pat_[A-Za-z0-9_]{20,}"),
    ("anthropic-key", r"\bsk-ant-[A-Za-z0-9_-]{20,}"),
    ("openai-key", r"\bsk-(?!ant-)(?:proj-)?[A-Za-z0-9_-]{20,}"),
    ("aws-access-key", r"\b(?:AKIA|ASIA)[0-9A-Z]{16}\b"),
    ("stripe-key", r"\b(?:sk|rk)_(?:live|test)_[A-Za-z0-9]{10,}|\bpk_live_[A-Za-z0-9]{10,}"),
    ("slack-token", r"\bxox[baprs]-[A-Za-z0-9-]{10,}"),
    ("google-api-key", r"\bAIza[0-9A-Za-z_-]{35}"),
    ("google-oauth-token", r"\bya29\.[0-9A-Za-z_-]{20,}"),
    ("gitlab-token", r"\bglpat-[0-9A-Za-z_-]{20,}"),
    ("npm-token", r"\bnpm_[A-Za-z0-9]{36}"),
    ("digitalocean-token", r"\bdop_v1_[a-f0-9]{64}"),
    ("huggingface-token", r"\bhf_[A-Za-z0-9]{30,}"),
    ("sendgrid-key", r"\bSG\.[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{16,}"),
    ("mailgun-key", r"\bkey-[0-9a-f]{32}\b"),
    ("jwt", r"\beyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}"),
]
SECRET_WORDS = (r"password|passwd|pwd|secret|api[_-]?key|apikey|access[_-]?key|auth[_-]?token|access[_-]?token|"
                r"client[_-]?secret|private[_-]?key|secret[_-]?key")
# Assignment-style rules: the captured value is checked against placeholders.
# (rule id, regex, config files only)
VALUE_RULES = [
    ("url-credentials", r"\b[a-z][a-z0-9+.-]*://[^\s:/@'\"]+:([^\s@/'\"]{3,})@", False),
    ("connection-string-password", r"(?i)(?:^|[;\s\"'])(?:password|pwd)\s*=\s*([^;\s\"']{3,})", False),
    ("azure-account-key", r"(?i)\b(?:AccountKey|SharedAccessKey)\s*=\s*([^;\s\"']{10,})", False),
    ("bearer-token", r"(?i)\bBearer\s+([A-Za-z0-9._~+/=-]{16,})", False),
    ("hardcoded-secret",
     rf"(?i)[\"']?[\w.-]*(?:{SECRET_WORDS})[\w.-]*[\"']?\s*(?::\s*[A-Za-z_][\w.<>\[\]| ]*?\s*)?[:=]\s*"
     r"[\"']([^\"'\s]{6,})[\"']", False),
    ("env-secret-assignment",
     r"(?i)^\s*(?:export\s+)?[A-Z0-9_]*(?:PASSWORD|PASSWD|SECRET|API_?KEY|TOKEN|PRIVATE_KEY|ACCESS_KEY)"
     r"[A-Z0-9_]*\s*=\s*[\"']?([^\s\"'#]{6,})", False),
    ("config-secret",
     rf"(?i)^\s*-?\s*[\w.-]*(?:{SECRET_WORDS}|token)[\w.-]*\s*[:=]\s*([^\s#'\"]{{6,}})\s*(?:#.*)?$", True),
]
PLACEHOLDER = re.compile(
    r"example|changeme|change_me|your[_-]|xxx|placeholder|dummy|sample|redacted|todo|replace|"
    r"<[^>]*>|\$\{|\{\{|%\(|process\.env|os\.environ|getenv|env\(|config\[|secrets\.|vault|"
    r"^\*+$|^\.+$|^[\d.]+$|^[a-z][a-z0-9+.-]*://|"
    r"^(true|false|null|none|undefined|password|secret|token)$",
    re.I,
)

TOKEN_RES = [(rid, re.compile(rx)) for rid, rx in TOKEN_RULES]
VALUE_RES = [(rid, re.compile(rx), cfg) for rid, rx, cfg in VALUE_RULES]
NAME_RES = [(sev, rid, re.compile(rx, re.I), desc) for sev, rid, rx, desc in NAME_RULES]


def git(*args, check=True):
    r = subprocess.run(["git", *GIT_CFG, *args], capture_output=True)
    if check and r.returncode != 0:
        raise RuntimeError(r.stderr.decode("utf-8", "replace").strip() or f"git {args[0]} failed")
    return r


def redact(s):
    s = s.strip()
    return s[:4] + "..." if len(s) > 6 else "..."


def scan_line(text, is_template, is_config):
    """Return (rule, redacted) for the first finding on a line, or None."""
    for rid, rx in TOKEN_RES:
        m = rx.search(text)
        if m and (rid == "private-key" or not PLACEHOLDER.search(m.group(0))):
            return rid, redact(m.group(0))
    if is_template:
        return None
    for rid, rx, cfg_only in VALUE_RES:
        if cfg_only and not is_config:
            continue
        m = rx.search(text)
        if m and not PLACEHOLDER.search(m.group(1)):
            return rid, redact(m.group(1))
    return None


def check_name(path):
    base = os.path.basename(path)
    if TEMPLATE_NAME.search(base):
        return []
    return [(sev, rid, desc) for sev, rid, rx, desc in NAME_RES if rx.search(base)]


def read_text(path):
    try:
        with open(path, "rb") as f:
            data = f.read(MAX_BYTES + 1)
    except OSError:
        return None, "unreadable"
    if len(data) > MAX_BYTES:
        return None, "over 1 MB, content not scanned"
    if b"\0" in data[:8192]:
        return None, "binary, content not scanned"
    return data.decode("utf-8", "replace"), None


def parse_status(pathspecs):
    """Return ({path: xy}, {new path: old path}) for every changed path."""
    out = git("status", "--porcelain", "-z", "-uall", "--", *pathspecs).stdout.decode("utf-8", "replace")
    entries, renames, parts, i = {}, {}, out.split("\0"), 0
    while i < len(parts):
        e = parts[i]
        i += 1
        if len(e) < 4:
            continue
        xy, path = e[:2], e[3:]
        if xy[0] in "RC" or xy[1] in "RC":
            renames[path] = parts[i]
            i += 1
        entries[path] = xy
    return entries, renames


def label(xy):
    if xy == "??":
        return "untracked"
    if "U" in xy or xy in ("AA", "DD"):
        return "conflict"
    if "D" in xy:
        return "deleted" if xy[0] in "D " else "partly staged"
    if xy[0] in "RC":
        return "renamed" if xy[1] == " " else "partly staged"
    return "staged" if xy[1] == " " else "unstaged" if xy[0] == " " else "partly staged"


def unquote(p):
    """Undo git's C-style quoting of unusual paths."""
    if len(p) >= 2 and p[0] == p[-1] == '"':
        p = codecs.escape_decode(p[1:-1].encode("utf-8"))[0].decode("utf-8", "replace")
    return p


def added_lines(args, paths):
    """Return {path: [(lineno, text), ...]} for added lines of one diff."""
    result = defaultdict(list)
    if not paths:
        return result
    diff = git("diff", *args, "--unified=0", "--no-renames", "--no-ext-diff", "--src-prefix=a/",
               "--dst-prefix=b/", "--", *paths).stdout
    current, lineno, in_hunk = None, 0, False
    for raw in diff.decode("utf-8", "replace").splitlines():
        if raw.startswith("diff --git "):
            current, in_hunk = None, False
        elif not in_hunk and raw.startswith("+++ "):
            p = unquote(raw[4:].rstrip("\t"))
            current = p[2:] if p.startswith("b/") else None
        elif raw.startswith("@@"):
            m = re.search(r"\+(\d+)", raw)
            lineno, in_hunk = (int(m.group(1)) if m else 0), True
        elif in_hunk and raw.startswith("+") and current is not None:
            result[current].append((lineno, raw[1:]))
            lineno += 1
    return result


def numstat(base, paths):
    stats = {}
    if not paths:
        return stats
    out = git("diff", base, "--numstat", "-z", "--no-renames", "--", *paths).stdout.decode("utf-8", "replace")
    for rec in out.split("\0"):
        if rec.count("\t") >= 2:
            a, d, p = rec.split("\t", 2)
            stats[p] = f"+{a} -{d}" if a != "-" else "binary"
    return stats


def dep_dir(path):
    parts = path.split("/")
    for i, part in enumerate(parts[:-1]):
        if part in DEP_DIRS:
            return "/".join(parts[: i + 1]) + "/"
    return None


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    pathspecs = [os.path.abspath(p) for p in sys.argv[1:]]
    try:
        top = git("rev-parse", "--show-toplevel").stdout.decode("utf-8").strip()
        os.chdir(top)
        base = "HEAD" if git("rev-parse", "--verify", "-q", "HEAD", check=False).returncode == 0 else EMPTY_TREE
        status, renames = parse_status(pathspecs)
    except (RuntimeError, OSError) as exc:
        print(f"secret-scan: error: {exc}")
        return 2

    branch = git("status", "-sb", check=False).stdout.decode("utf-8", "replace").splitlines()
    print(f"Branch: {branch[0][3:] if branch else '?'}")
    if base == "HEAD":
        recent = git("log", "-3", "--oneline", check=False).stdout.decode("utf-8", "replace").rstrip()
        print("Recent commits:\n" + "\n".join("  " + l for l in recent.splitlines()))

    if not status:
        print("secret-scan: nothing to commit")
        return 0

    tracked = [p for p, s in status.items() if s != "??" and label(s) != "deleted"]
    # Staged and working-tree versions can differ; a secret in either could be committed.
    staged = added_lines(["--cached", base], tracked)
    worktree = added_lines([base], tracked)
    stats = numstat(base, tracked)
    findings, notes = [], []
    deps = defaultdict(int)

    for path, st in sorted(status.items()):
        if st == "??" and dep_dir(path):
            deps[dep_dir(path)] += 1
            continue
        base_name = os.path.basename(path)
        is_template = bool(TEMPLATE_NAME.search(base_name))
        is_config = bool(CONFIG_EXT.search(base_name))
        for sev, rid, desc in check_name(path):
            findings.append((sev, path, None, rid, desc))
        if label(st) == "deleted":
            continue
        if st == "??":
            text, why = read_text(path)
            if text is None:
                notes.append(f"{path}: {why}")
                stats[path] = why if why.startswith("binary") else "new"
                continue
            numbered = list(enumerate(text.splitlines(), 1))
            stats[path] = f"+{len(numbered)} (new)"
        else:
            seen = {t for _, t in worktree.get(path, [])}
            numbered = worktree.get(path, []) + [(n, t) for n, t in staged.get(path, []) if t not in seen]
        for lineno, text in numbered:
            hit = scan_line(text, is_template, is_config)
            if hit:
                findings.append(("BLOCK", path, lineno, *hit))

    # --- Changeset ---
    rows = []
    for path, st in sorted(status.items()):
        if st == "??" and dep_dir(path):
            continue
        extra = f"(from {renames[path]})" if path in renames else stats.get(path, "")
        rows.append((label(st), path, extra))
    if len(rows) > MAX_LISTED:
        # Collapse untracked files by top-level directory.
        groups = defaultdict(list)
        for row in rows:
            key = row[1].split("/")[0] + "/" if row[0] == "untracked" and "/" in row[1] else row[1]
            groups[key].append(row)
        rows = [g[0] if len(g) == 1 else ("untracked", key, f"({len(g)} files; list with git status -uall {key})")
                for key, g in sorted(groups.items())]
    print(f"Changeset ({len(status)} files, base {base if base == 'HEAD' else 'empty tree'}):")
    for i, (lab, path, extra) in enumerate(rows):
        if i == MAX_LISTED:
            print(f"  ... {len(rows) - MAX_LISTED} more (see git status --short)")
            break
        print(f"  {lab:<13} {path}  {extra}".rstrip())
    for d, n in sorted(deps.items()):
        print(f"  untracked     {d}  ({n} files, dependency/cache folder: not scanned, add it to .gitignore)")

    # --- Findings ---
    blocks = sum(1 for f in findings if f[0] == "BLOCK")
    reviews = len(findings) - blocks
    print(f"\nsecret-scan: {'CLEAN' if not findings else f'{blocks} BLOCK, {reviews} REVIEW'}")
    per_file, printed, hidden = defaultdict(int), 0, defaultdict(int)
    for sev, path, lineno, rid, detail in findings:
        per_file[path] += 1
        if printed >= MAX_FINDINGS or per_file[path] > MAX_PER_FILE:
            hidden[path] += 1
            continue
        where = f"{path}:{lineno}" if lineno else path
        print(f"  {sev:<6} {where}  [{rid}] {detail}")
        printed += 1
    for path, n in hidden.items():
        print(f"  ...    {path}  {n} more finding(s)")
    for n in notes:
        print(f"  note   {n}")
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
