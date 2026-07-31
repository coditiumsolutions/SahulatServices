---
name: update-db-docs
description: >-
  Connects to a database using a provided connection string, introspects the
  live schema, and updates database documentation files (db.txt, Database.md,
  db.md) while respecting each file's existing formatting. Manages version
  and UpdatedAt stamps on every update. Activation phrase: "using my connection
  string, update @db.txt file which contains my database tables and columns".
  Also triggers on variants like "update my db docs", "refresh db.md from my
  connection string", or any request to sync database documentation from a live
  connection. Never stores the connection string itself in any documentation file.
author: Rayder
version: 1.0.1
---

<!-- Author: Rayder | Version: 1.0.1 -->

# Update DB Docs

Update database documentation only when the user explicitly asks. Never run schema introspection or modify documentation files proactively.

## Activation

**Primary phrase:** `using my connection string, update @db.txt/<filename> file which contains my database tables and columns`

Where `<filename>` is one of the targeted files. Close variants also activate this skill:
- "update my db docs from my connection string"
- "refresh Database.md with the latest schema"
- "sync db.md from my live database"

## Targeted files

Operate only on these files, and only if they exist in the project:

| File | Notes |
|------|-------|
| `db.txt` | Plain text format — preserve any existing structure and indentation |
| `Database.md` | Markdown format — preserve headings, tables, and section layout |
| `db.md` | Markdown format — same rules as Database.md |

If none of these files exist, stop and ask the user which file to create and in what format before proceeding.

If multiple files exist, update all of them in a single run unless stated inside the file itself.

## Connection string handling

**The connection string must never appear in any documentation file, commit, or log output.**

### Source
 
Resolve the connection string in this order — stop at the first successful step:
 
**1. Inline (user-provided)**
If the user pasted a connection string directly in the prompt, use it and skip auto-discovery.
 
**2. User-referenced file**
If the user pointed to a specific file (e.g. `@.env`, `@appsettings.json`), read only from that file.
 
**3. Auto-discovery — known locations**
If no connection string was provided or referenced, scan the project for these files in priority order:
 
| Priority | Stack | Files to check | Key to look for |
|----------|-------|---------------|-----------------|
| 1 | Any | `.env.local` | `DATABASE_URL`, `DB_URL`, `CONNECTION_STRING` |
| 2 | Any | `.env.development` | same keys as above |
| 3 | Any | `.env` | same keys as above |
| 4 | ASP.NET Core | `appsettings.Development.json` | `ConnectionStrings.*` |
| 5 | ASP.NET Core | `appsettings.json` | `ConnectionStrings.*` |
| 6 | Node / Next.js | `.env.local`, `.env.development.local` | `DATABASE_URL`, `DB_CONNECTION` |
| 7 | Python | `settings.py`, `config.py` | `DATABASE_URL`, `DATABASES['default']` |
 
Stop scanning as soon as a valid connection string is found.
 
**4. Multiple found — ask the user**
If valid connection strings are found in more than one file, do not pick one silently. Present the options and wait:
 
```
Found connection strings in multiple files:
  [1] .env — postgres://... (credentials masked)
  [2] appsettings.Development.json — Server=... (credentials masked)
 
Which should I use?
```
 
**5. Not found — ask the user**
If no connection string is found after scanning all known locations, stop and ask:
 
```
No connection string found in known config files.
Please paste it directly or point me to the file (e.g. @myconfig.env).
```
 
---
 
When reading from any file, extract only the connection string value needed for introspection. Do not echo it back in full. Acknowledge it with a masked summary only:
 
```
Connected to: <host> / <database_name>  (credentials masked)
```

### Security check (mandatory)

Before proceeding, confirm the connection string is not headed into any doc file — it must only be used transiently for the introspection query. If the user's instructions suggest writing the connection string to a file, stop and warn them.

## Schema introspection

Connect to the database using the appropriate driver or CLI tool for the detected engine:

| Engine | Detection | Introspection approach |
|--------|-----------|----------------------|
| SQL Server | `Server=` / `Data Source=` | Query `INFORMATION_SCHEMA.COLUMNS`, `TABLES`, `KEY_COLUMN_USAGE` |
| PostgreSQL | `postgres://` / `postgresql://` / `host=` | Query `information_schema.columns`, `pg_indexes` |
| MySQL / MariaDB | `mysql://` / `Server=` + `Database=` | Query `information_schema.COLUMNS`, `STATISTICS` |
| SQLite | File path ending in `.db`, `.sqlite` | `.schema` or `PRAGMA table_info` |

Extract for each table:
- Table name
- Column names, data types, nullability
- Primary keys
- Foreign keys and relationships
- Indexes (if present in the existing doc)

Do not extract or document stored procedures, views, or triggers unless the existing file already contains a section for them — in that case, update that section too.

## Updating documentation files

### Core rule: respect existing formatting

Each file may have its own layout — tables, plain lists, prose descriptions, custom sections. **Do not reformat or restructure the file.** Update values in place using the same style already present.

- If the file uses Markdown tables → update cell values, keep column structure
- If the file uses plain text indentation → keep indentation and spacing
- If the file has custom section headers → keep them; only update content within each section
- If a table or column is new (exists in DB but not in doc) → append it at the end of the relevant section in the file's existing style
- If a table or column was removed from the DB → mark it as `[REMOVED]` inline rather than deleting it outright, so the user can review before cleaning up

### Version and timestamp

Every documentation file must contain a version and timestamp header. On each update:

1. **Version:** Increment by `0.1` from the current value. Base version is `1.0` if none exists.
   - `1.0` → `1.1` → `1.2` → ... → `1.9` → `2.0`
2. **UpdatedAt:** Set to the current date in `DD/MM/YYYY` format.

If the header already exists in the file, update it in place. If it does not exist, prepend it at the very top of the file in the appropriate comment style:

**Markdown / db.md / Database.md:**
```markdown
<!-- Version: 1.1 | UpdatedAt: 11/06/2026 -->
```

**Plain text / db.txt:**
```
# Version: 1.1 | UpdatedAt: 11/06/2026
```

## Execution sequence

1. Locate targeted files in the project
2. Accept or read the connection string — never write it anywhere
3. Connect and introspect the live schema
4. Confirm connection with masked summary: `Connected to: <host> / <db_name> (credentials masked)`
5. Diff introspected schema against current doc content
6. Update each targeted file in place — formatting preserved
7. Increment version and update `UpdatedAt` in each file's header
8. Report a brief summary of what changed (new tables, removed columns, etc.)

## Post-update report format

After updating, always output a short summary:

```
Updated: Database.md (v1.3 — 11/06/2026)
  + Added: orders.discount_code (VARCHAR, nullable)
  ~ Changed: users.email — NOT NULL → NULL
  - Marked removed: legacy_sessions table
```

Use `+` for additions, `~` for changes, `-` for removals. If nothing changed, say so explicitly rather than silently skipping.

## Safety rules

- **Never** write the connection string to any file
- **Never** modify files outside the targeted list
- **Never** drop or delete existing content — use `[REMOVED]` markers instead
- **Never** reformat or restructure a file's layout unprompted
- If the introspection query fails (auth error, timeout, unreachable host), stop and report the error clearly — do not partially update files

## Checklist (mental)

```
- [ ] User explicitly asked to update DB docs
- [ ] Connection string accepted but never written to any file
- [ ] Schema introspected from live DB
- [ ] Masked connection summary reported to user
- [ ] Existing file formatting preserved
- [ ] New tables/columns appended in existing style
- [ ] Removed items marked [REMOVED], not deleted
- [ ] Version incremented by 0.1, UpdatedAt set to today
- [ ] Post-update summary reported
```