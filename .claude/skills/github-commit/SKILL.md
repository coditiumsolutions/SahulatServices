---
name: github-commit
description: >-
  Safe Git commits with brief prefixed messages, bullet-point bodies, logical
  commit splitting, scripted secret scanning, and .gitignore hygiene. Use
  whenever the user asks to commit, push changes, save work to git, create a
  commit, stage files, or says "commit and push to github" (the primary
  activation phrase — always use this skill for that). Also triggers on casual
  phrasing ("commit this", "git it up", "push to github"). Use before any git
  operation that might touch merges or conflicts. Do NOT use for pull
  requests, branch strategy, or code review without a commit request.
author: Rayder
version: 1.3.0
---

<!-- Author: Rayder | Version: 1.3.0 -->

# GitHub Commit

Commit only when the user asks. "Commit and push to github" (or close variants) means commit, then push. "Commit" alone means do not push.

## Message format

`Prefix: short imperative summary`. One line, exactly one prefix, chosen by the change's **intent** (not git file status: a new file that fixes a bug is `Fix:`). Ties → match `Recent commits` style.

- `Add:` new files/features/endpoints · `Update:` changed behavior, config, dependency bumps · `Fix:` bugs/regressions · `Refactor:` structure, no behavior change · `Remove:` deletions, dead code
- `Feat:` user-facing capability across areas · `Feat(Scope):` same, one area (`Feat(Auth):`, short noun)
- `Docs:` docs only · `Test:` tests only · `Perf:` speed, no behavior change · `Style:` formatting only
- `Build:` build/toolchain · `Ci:` CI/CD workflows · `Chore:` lockfiles, `.gitignore`, tooling config, file moves · `Revert:` reverts (name the original commit in the body)

**Body** (after a blank line; skip for trivial changes): explain *why* and the impact. No file lists.
- One point → one short paragraph, at most 2 sentences / ~200 chars.
- More → 2–5 one-line bullets (`- `). Never long or multiple paragraphs.

```
Fix: Paid amount not updating after partial payment insert

- Payment inserts did not sync the invoice paid amount
- Recalculate paid amount on each insert so the running balance stays correct
```
```
Update: Raise API gateway rate limit to 200 req/min

Peak-hour traffic was hitting the old 100 req/min limit and returning 429s to normal users.
```

## Workflow

**1. Recon + scan, one call** (from the repo root; `python` if `python3` is missing):

```
python3 <skill-dir>/scripts/scan_secrets.py
```

It prints the branch/upstream line, recent commits, every changed file (staged, unstaged, untracked) with line counts, and a secret scan (values redacted). Don't also run `git status`, `git log`, `git branch` or `git remote`: the output already covers them.

- `CLEAN` → continue.
- `BLOCK` / `REVIEW` → read [references/secrets-found.md](references/secrets-found.md) and follow it.
- No Python → scan by hand with [references/secrets-patterns.md](references/secrets-patterns.md) over `git diff HEAD` plus untracked files from `git status --porcelain -uall`.

**2. Plan commits.** Group the changeset into small, logical commits. Prefer two small commits over one large one.
- One intent per commit: feature, fix, refactor, docs, chores/config, and CI each get their own. Tests go with the code they cover.
- Order: config/dependencies → code that uses them → docs.
- Split if the subject needs "and" or the files are unrelated. Don't split one cohesive change.
- Whole files only (no `git add -p`). A mixed file goes where it mostly belongs.
- Already-staged files: commit that set as-is as its own commit; don't reset it.

**3. Read only what you need.** `git diff HEAD -U1 -- <paths>` per group. Read untracked files directly. Read each file once. Skip files whose purpose is clear from the name and line counts (lockfiles, `.gitignore`).

**4. Commit each group.** `git add <paths>` then a plain `git commit` (no `-a`, `-o`/`--only`). Each commit should leave the code working: keep code with the code it calls, so a UI that needs a new API goes in the API's commit or after it. Never stage build output, dependency folders, or flagged files. Finish with one `git status --short`.

Keep multi-line messages literal:
- bash: `git commit -F - <<'EOF'` … `EOF`
- PowerShell: pipe a single-quoted here-string to `git commit -F -` (closing `'@` at column 0). Don't pass it with `-m`: Windows PowerShell 5.1 strips `"` from native arguments.
  ```powershell
  $OutputEncoding = [Text.UTF8Encoding]::new($false)   # no BOM in the message
  @'
  Fix: Subject

  - Bullet
  '@ | git commit -F -
  ```
  Never use `@"…"@`: it silently expands `$var` and backticks. If the message has `$`, `"` or a backtick, check it with `git log -1 --pretty=%B`.

**5. Push** (only when asked), once after all commits: `git push` (`git push -u origin HEAD` if there's no upstream). Report branch, remote, and the commits pushed.

## Safety

- Never change `git config`, skip hooks (`--no-verify`), or bypass signing.
- Amend only if the user asked, the commit is from this session, and it isn't pushed. If a hook rejects a commit, fix the issue and make a new commit.
- Ask first (explicit approval in the current message) before: `git merge`, a merging `git pull`, `rebase`, `cherry-pick`, resolving conflict markers, `checkout --theirs/--ours`, `reset --hard`, or any force push. Never force-push `main`/`master` without explicit approval.
- On conflicts: stop, explain what conflicted, offer options (keep ours / theirs / abort), and wait.
- Nothing to commit → say so; never make an empty commit.
