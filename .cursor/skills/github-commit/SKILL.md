---
name: github-commit
description: >-
  Safe Git commits with brief prefixed messages, secret scanning, and
  .gitignore hygiene. Use whenever the user asks to commit, push changes,
  save work to git, create a commit, stage files, or says "commit and push
  to github" (the primary activation phrase — always use this skill for that).
  Also triggers on casual phrasing ("commit this", "git it up", "push to
  github"). Use before any git operation that might touch merges or conflicts.
  Do NOT use for pull requests, branch strategy, or code review without a
  commit request.
author: Rayder
version: 1.2.1
---

<!-- Author: Rayder | Version: 1.2.1 -->

# GitHub Commit

Commit only when the user explicitly asks. Never commit proactively.

## Activation

**Primary phrase:** `commit and push to github`

When the user says this (or a close variant — e.g. "commit and push to GitHub", "commit & push to github"), treat it as full activation:

1. Run the complete workflow in this skill (secret scan → stage → commit with prefixed subject + body).
2. **Then push** to the remote after a successful commit (`git push`, or `git push -u origin HEAD` when upstream is not set).

If the user asks to **commit only** (no "push" in the request), commit locally and do not push unless they ask separately.

## Commit message format

**Subject line:** One line, clean and brief. Start with exactly one of these prefixes (including the colon):

| Prefix | Use when |
|--------|----------|
| `Add:` | New files, features, endpoints, or capabilities |
| `Update:` | Changes to existing behavior, config tweaks, or dependency version bumps |
| `Refactor:` | Structural or internal changes with no behavior change |
| `Fix:` | Bug fixes, error corrections, regressions |
| `Feat:` | A user-facing capability that spans multiple areas or the whole project |
| `Feat(scope):` | Same as `Feat`, but scoped to a named domain, module, or subsystem — e.g. `Feat(Auth):`, `Feat(Billing):`, `Feat(Search):`. The scope should be a short noun matching a folder, layer, or product area. Use plain `Feat:` when the change cuts across multiple areas. |
| `Remove:` | Deletions, deprecations, dead code removal |
| `Docs:` | README, comments, changelogs, or any documentation-only changes |
| `Test:` | Adding, fixing, or restructuring tests — no production code changes |
| `Perf:` | Performance improvements with no behavior change |
| `Style:` | Formatting, whitespace, linting — zero logic change |
| `Build:` | Build system changes, compilation config, bundler or toolchain setup |
| `Ci:` | CI/CD pipelines and workflow files — e.g. `.github/workflows/`, deploy pipelines |
| `Chore:` | Housekeeping that fits no other prefix — dependency lockfiles, `.gitignore`, editor/tooling config, file moves with no code change |
| `Revert:` | Explicitly reverting a previous commit — reference the original in the body |

**Pattern:** `Prefix: short imperative summary in plain language`

**Body (default):** After a blank line, add a short body that explains *why* the change was made — bullet points or paragraphs. Focus on intent and impact, not a file list.

**Example with body:**
```
Fix: paid amount not updating after partial payment insert

Payment rows were inserted without syncing the invoice paid amount.
Update paid amount on each insert so the running balance stays correct.
```

**Subject-only examples:**
- `Add: student fee allocation check during invoice generation`
- `Update: rate limit config in API gateway`
- `Refactor: split user service into auth and profile modules`
- `Feat(Billing): late fee applied when past due date`
- `Remove: unused logger middleware`
- `Docs: add setup steps to README`
- `Test: cover edge cases in discount calculation`
- `Perf: cache repeated DB lookups in report generation`
- `Style: apply linter rules across src/`
- `Build: migrate bundler from Webpack to Vite`
- `Ci: add lint and test steps to PR workflow`
- `Chore: update .gitignore for local config files`
- `Revert: undo rate limiter change from previous commit`

Pick the prefix that best matches the **primary intent** of the diff. Do not combine prefixes on the subject line.

Intent beats file status. A brand-new file does not automatically mean `Add:` — a new `payments.js` that fixes a balance bug is `Fix:`, and a new `package-lock.json` is `Chore:`. Ask what the change is *for*, not which files git happens to call new.

## Pre-commit workflow

One command gives you everything needed to pick a prefix and start the scan:

```bash
git status --porcelain -uall && git log -3 --oneline && git diff HEAD --stat
```

`git status --porcelain -uall` is the important part: it lists **untracked** files individually. Plain `git diff` shows nothing for untracked files, so a changeset made entirely of new files — the usual shape of "I just added a config file with keys in it" — looks empty to a diff-only scan. `git diff HEAD` covers staged and unstaged tracked changes in one pass, replacing separate `git diff` / `git diff --cached` calls.

Then read the content you are about to commit:

- **Tracked changes:** `git diff HEAD` (add `-- path/` to scope when the diff is large — roughly >1000 lines or >20 files).
- **Untracked files:** read them directly. They are invisible to every `git diff` variant.

**If files are already staged when you arrive**, do not reset and re-stage them — the user may have staged a deliberate subset. Scan the staged set as-is, and if it contains something that should not be committed, say so and let them decide rather than silently unstaging it.

Read each changed file once and carry it in context — the prefix decision, the body, and the secret scan all use the same content, so re-running diffs per step is wasted work. Never narrow *coverage* to save tokens: every file being committed must be looked at. Narrow the reads, not the set of files.

Use recent commit style as a tiebreaker when the best prefix is ambiguous.

### 1. Secret and credential scan (mandatory)

Before `git add`, inspect **staged and unstaged** changes for sensitive data. Never commit:

| Blocked | Notes |
|---------|-------|
| `.env`, `.env.local`, `.env.*` | **Exception:** `*.example`, `*.example.env`, files clearly named as templates with **no real secrets** |
| Connection strings | Any config file or code containing real hostnames, passwords, or credentials |
| API keys, tokens, passwords | Including cloud provider keys, JWT secrets, SMTP credentials, OAuth secrets |
| Local config overrides | e.g. `appsettings.Development.json`, `config.local.*`, `*.local.json` |
| Certificates and private keys | `.pfx`, `.pem`, `.key`, `.p12` with private key material |
| Secret store files | `credentials.json`, `secrets.json`, `*.secrets.*`, `keystore.*` |

Scan the file contents already gathered above — tracked changes from `git diff HEAD`, plus every untracked file read directly. No extra git calls are needed here.

Note that `git status` hides files matched by `.gitignore`, so a secret already covered by an ignore rule is safe but invisible. That is the correct outcome — it will not be committed — so do not go hunting for ignored files on every commit. Only when a sensitive path *is* about to be committed does it matter, and that path shows up in the scan above by definition.

Flag patterns: `password`, `passwd`, `pwd=`, `secret`, `api_key`, `apikey`, `Bearer `, `token`, `sk-`, `sk_live_`, `ghp_`, `AKIA`, `AccountKey=`, `private_key`, base64 blobs in config files.

**If secrets are found:**
1. **Stop** — do not stage or commit those files.
2. Tell the user exactly what was found and in which file.
3. Move the secrets to environment variables, a secrets manager, or local-only config excluded from version control.
4. Add or update `.gitignore` (see below).
5. **Advise rotating the exposed credentials.** Removing a key from a file does not undo the exposure — it sat in plaintext on disk, possibly in an editor buffer, shell history, or backup. Rotation is what actually limits the damage, so say so plainly even though it is work outside the commit.
6. If the file was already committed historically, warn the user that the secret is in history; do not rewrite history unless they explicitly ask.

**After remediating, may you commit the cleaned files?** Yes — if the user asked for a commit and the cleaned diff now passes the scan, commit it and report both what you removed and what you committed. The "stop" in step 1 protects the *secrets*, not the user's request. What needs explicit approval is anything that rewrites history or touches files they did not ask you to change.

For extended patterns, read [references/secrets-patterns.md](references/secrets-patterns.md).

### 2. .gitignore hygiene

If a sensitive or generated path is not already ignored, append to `.gitignore` **before** committing other work:

```
# Secrets and local config
.env
.env.*
!.env.example
*.local.json
**/secrets.json
**/credentials.json
```

Add project-appropriate entries for your stack's build artifacts and local tooling (e.g. `dist/`, `build/`, `node_modules/`, `.next/`, `__pycache__/`, `*.pyc`, `bin/`, `obj/`, `.vs/`). Prefer inserting into existing `.gitignore` sections rather than appending loosely.

### 3. Stage only intended files

- Do not stage build output, dependency folders, or editor artifacts unless the user explicitly asks.
- Do not stage files that failed the secret scan.
- Warn if the user asked to commit a file that looks like it contains secrets.

## Commit execution

**Git safety (non-negotiable):**
- Never change `git config`
- Never use `--no-verify`, `--no-gpg-sign`, or skip hooks unless the user explicitly requests it
- Never `push --force` to `main`/`master` without explicit user approval; warn if they ask
- Avoid `git commit --amend` unless: user asked, HEAD was created in this session, and the commit was not pushed

**Sequence:**
1. Complete secret scan and `.gitignore` updates
2. `git add` only approved paths
3. Commit with the formatted message (see quoting below)
4. `git status` after commit to confirm success

When the message has a body, use a here-string (PowerShell) or `-m` twice (bash) to avoid shell escaping issues:

```bash
# bash / zsh
git commit -m "Fix: paid amount not updating after partial payment insert" \
           -m "Payment rows were inserted without syncing the invoice paid amount. Update paid amount on each insert so the running balance stays correct."
```

```powershell
# PowerShell — use a SINGLE-quoted here-string so $ and backticks stay literal.
# The opener must end its line, and the closing '@ must be at column 0.
git commit -m @'
Fix: paid amount not updating after partial payment insert

Payment rows were inserted without syncing the invoice paid amount.
Update paid amount on each insert so the running balance stays correct.
'@
```

Never use `@"…"@` (double-quoted) for commit messages. PowerShell expands `$var` and backticks inside it, and the failure is silent: `git commit` still exits 0, so a body mentioning `$total` commits as `The  variable naming…` with the token deleted and no error anywhere. If the message contained a `$` or a backtick, confirm it survived with `git log -1 --pretty=%B` — that one call is the only way this class of corruption is visible.

Single-line commits are fine when the change is trivial and needs no explanation.

**After commit — push (when activated):** If the user used the activation phrase or explicitly asked to push:

```bash
git push
# or, if no upstream is set yet:
git push -u origin HEAD
```

Report the push result (branch, remote, commits pushed). Never force-push to `main`/`master` without explicit approval.

## Merges and conflicts — always ask first

**Never** run these without explicit user approval in the current message:
- `git merge`, `git pull` (when it merges), `git rebase`, `git cherry-pick`
- Resolving conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`)
- `git checkout --theirs` / `--ours` on conflicted files
- `git reset --hard`, `git push --force`

If a commit attempt surfaces merge conflicts or a dirty merge state:
1. Stop
2. Explain what conflicted (files and branches involved)
3. Present options (pull first, abort, manual resolution)
4. Wait for the user to choose

## When commit fails

- Hook rejected commit → fix the issue, create a **new** commit (do not amend unless amend rules apply)
- Nothing to commit → say so; do not create an empty commit

## Efficiency

A routine commit should take roughly four commands: one recon call, one `git add`, one `git commit`, one `git status`. If you find yourself re-running diffs between steps, you are re-reading content you already have — gather once, then decide the prefix, write the body, and run the scan from that single read.