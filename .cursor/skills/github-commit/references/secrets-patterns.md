# Secret scan patterns
Use when reviewing diffs before commit. A single match is enough to block the file.

## File names (block unless clearly a template)
- `.env`, `.env.local`, `.env.production`, `.env.development`
- Local config overrides — e.g. `appsettings.Development.json`, `config.local.json`, `*.local.json`, `*.local.yaml`
- `secrets.json`, `credentials.json`, `serviceAccountKey.json`
- `*.pfx`, `*.p12`, `id_rsa`, `id_ed25519`, `*.pem` (private keys)
- Any config file containing real connection strings or credentials

## Safe template exceptions
Allow only when contents are placeholders, not real credentials:
- `*.example`, `*.example.json`, `*.example.yaml`, `*.example.env`
- `.env.example` with values like `your-api-key-here`, `changeme`, `TODO`
- Any file clearly named as a template where all sensitive values are placeholder strings

## Content patterns (case-insensitive)
```
Password=
Pwd=
User ID=
Server=.*;
AccountKey=
SharedAccessKey=
DefaultEndpointsProtocol=https;AccountName=
mongodb(\+srv)?://[^:]+:[^@]+@
postgres(ql)?://[^:]+:[^@]+@
mysql://[^:]+:[^@]+@
api_key\s*=\s*["'][^"']+["']
api_key\s*:\s*["'][^"']+["']
APIKEY=
x-api-key:\s*[^\s]+
Authorization:\s*Bearer\s+[^\s]{16,}
private_key\s*=
```

## Token prefixes and API keys
```
ghp_          # GitHub PAT
github_pat_
gho_
ghu_
ghs_
ghr_
sk-           # OpenAI
sk-ant-       # Anthropic
AKIA          # AWS access key
ASIA          # AWS temporary key
xox[baprs]-   # Slack
sk_live_      # Stripe secret key — highest severity
sk_test_      # Stripe test secret key
rk_live_      # Stripe restricted key
pk_live_      # Stripe publishable key (low risk but flag anyway)
AIza          # Google API key
ya29.         # Google OAuth access token
glpat-        # GitLab PAT
npm_          # npm access token
dop_v1_       # DigitalOcean PAT
hf_           # Hugging Face token
SG.           # SendGrid
key-          # Mailgun
Bearer        # Hardcoded Bearer token in source code or config
```

## Framework-specific notes

### Node / frontend
- `.env*` files at project root — commonly committed by accident
- `next.config.js` or `vite.config.ts` with hardcoded keys in `env` or `define` blocks
- `package.json` scripts with inline secrets passed as env vars

### Python
- `settings.py` or `config.py` with hardcoded `SECRET_KEY`, `DATABASE_URL`, or provider keys
- `.pyc` files are safe to ignore but should never be committed anyway

### Java / Kotlin
- `application.properties` or `application.yml` with real datasource passwords
- `google-services.json` — contains Firebase project credentials

### General
- Any `launchSettings.json` or equivalent runner config with real keys in environment variable blocks
- CI config files (`.github/workflows/*.yml`, `.gitlab-ci.yml`) with hardcoded secrets instead of secret references

## If unsure
Ask the user: "This file may contain credentials ([reason]). Commit anyway or exclude?"