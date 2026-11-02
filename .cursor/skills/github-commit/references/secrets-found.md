# When the scan reports findings

Read this only when `scan_secrets.py` reports `BLOCK` or `REVIEW`.

## REVIEW findings
A suspicious file name (local config override, `.pem`, cloud config). Open just that file and decide: commit it if it holds no credentials, otherwise treat it as BLOCK. Ask the user if unsure: "This file may contain credentials ([reason]). Commit anyway or exclude?"

Don't print secret values back to the terminal when you inspect a flagged file. Use the scan's `file:line` and redacted snippet to talk about it.

## BLOCK findings
1. Do not stage those files or lines.
2. Tell the user what was found and where (rule + `file:line` from the report).
3. Move the secrets to environment variables, a secrets manager, or ignored local config. Offer an `.example` template with placeholder values.
4. Make sure the secret-holding path is in `.gitignore` (template below).
5. **Advise rotating the exposed credentials.** Removing them from the file does not undo exposure: they sat in plaintext on disk, in editor buffers, maybe in backups.
6. If the secret is already in git history, say so. Do not rewrite history unless asked.

Then rerun the scan. If it is clean, go ahead with the commit the user asked for and report what you removed and what you committed. The stop protects the secrets, not the user's request.

## .gitignore template
Insert into existing sections where possible:

```
.env
.env.*
!.env.example
*.local.json
**/secrets.json
**/credentials.json
```

Plus stack-appropriate build/tooling entries (`dist/`, `node_modules/`, `.next/`, `__pycache__/`, `bin/`, `obj/`, `.vs/`, …).
