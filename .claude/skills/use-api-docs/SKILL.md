---
name: use-api-docs
description: >-
  Checks api.txt or api.md (whichever exists in the project root) before
  implementing or modifying any API integration in the Flutter app — HTTP
  calls, request/response models, API service classes, endpoints. This file
  is the source of truth for request bodies, response bodies (success/fail),
  base URLs (local/deployed), and which APIs are active. Triggers whenever
  the user asks to integrate, call, wire up, or consume an API/endpoint, add
  a new screen/feature that hits the backend, fix an API-related bug, or
  update request/response models. Also triggers when new endpoints are added
  or changed, in which case api.txt/api.md must be updated to stay in sync.
  Also triggers on the explicit activation phrases "use api.txt", "use
  @api.txt", "use api.md", and "use @api.md".
author: Rayder
version: 1.1.0
---

# Use API Docs

Before writing or editing any code that talks to the backend, read `api.txt` (or `api.md` if that's what the project uses) at the project root. It documents every active API's base URLs, request body, and success/fail response bodies — do not guess field names, types, or response shapes.

## When this applies

- Implementing a new API call (service method, repository, HTTP client usage)
- Building/updating a request or response model/DTO to match a backend contract
- Debugging a bug that involves parsing a response or building a request
- Adding a new screen or feature that consumes an existing or new endpoint

## Workflow

1. **Locate the file** — check for `api.txt` first, then `api.md`, at the project root. If neither exists, tell the user and ask them to point to the API documentation before proceeding.
2. **Read the frontmatter block** — at the top of the file, note: Version/Last Updated, Database, Local/Deployed Base URLs, DB Server, Format (e.g. REST over HTTPS, JSON bodies), Auth (the auth mechanism, or "None currently"), and the Maintenance Rules list (e.g. mirroring the file to a second path, versioning bump requirement). These govern how you read and update the rest of the file.
3. **Find the relevant endpoint** — search the file for the section matching the feature (e.g. `AUTH APIs`, `PROVIDER APIs`). Note the local and deployed base URLs, HTTP method, request body, and both success and fail response shapes.
4. **Match the contract exactly** — field names, casing, and nesting in Dart models/JSON serialization must mirror the documented request/response bodies exactly (including the common wrapper `{ success, message, data }` if the API uses it).
5. **Use the correct base URL** — respect the local vs. deployed URL distinction already established in the app's API client/config; don't hardcode a different one.
6. **Respect the Auth header** — if it says "None currently," don't add Authorization headers/tokens to requests; if it names a scheme, implement that scheme and don't invent a different one.
7. **If the endpoint isn't documented** — stop and ask the user for the request/response contract rather than inventing field names. Once confirmed, add the new endpoint to `api.txt`/`api.md` following its existing format (section header, method, local/deployed URLs, request body, success/fail bodies) so the doc stays authoritative.
8. **If you change an existing endpoint's contract** — update the corresponding entry in `api.txt`/`api.md` in the same change, per the file's own Maintenance Rules (mirror path, versioning) at its top.

## Constraints

- Never invent request/response fields not present in the doc or explicitly confirmed by the user.
- Never silently switch base URLs (local vs. deployed) from what the app's existing config uses.
- Never add auth headers/tokens beyond what the doc's Auth header specifies.
- Keep `api.txt`/`api.md` in sync whenever an API contract changes as part of the same task — don't leave it stale, and bump Version/Last Updated per the file's own maintenance rules.
