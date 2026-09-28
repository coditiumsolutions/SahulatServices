# Maps / GPS Implementation — Current State & Next Steps

Status as of 2026-09-23. Covers Phase 1 (ASP.NET Core API & DB) and Phase 2
(web admin-portal test harness), both complete and deployed. Phase 3
(Flutter mobile integration) has **not** started — see "Next steps" below.

## $0-cost strategy (why things are built this way)

The backend is the single source of truth for all location logic; the
frontend (Flutter/web) is purely a visual canvas. No paid mapping APIs are
used anywhere in this feature:

- **Map rendering** — free `google_maps_flutter` (mobile, not yet wired) /
  Leaflet.js via CDN (web test harness). No Google Maps JS API key needed.
- **Reverse geocoding** — OpenStreetMap Nominatim (free, rate-limited to
  ~1 req/s), not Google's paid Geocoding API.
- **Distance calculation** — Haversine great-circle formula in C#
  (`Helpers/DistanceHelper.cs`), not SQL spatial types/NetTopologySuite, not
  Google's paid Distance Matrix API.
- **Real-time location broadcast** — SignalR (self-hosted, part of the app),
  not a third-party push/realtime service.

## What was built (Phase 1 — backend)

### Data model

`Providers` table gained three nullable columns (migration
`20260824112339_AddProviderLiveLocation`), mapped on `AppDbContext` only
(the legacy `SahulatAppDbContext` is untouched):

- `Latitude decimal(10,7)`
- `Longitude decimal(10,7)`
- `LocationUpdatedOn datetime`

Only **last-known-position** is stored — no location history table. This was
a deliberate scope decision (see `AskUserQuestion` answers in the original
planning session), not a technical limitation; a history table is a natural
Phase 4 addition if trip replay/analytics is ever needed.

### Reverse geocoding

- `Options/NominatimOptions.cs` — `BaseUrl`, `UserAgent` (must identify the
  app per Nominatim's usage policy — currently
  `SahulatGharTak/1.0 (contact: coditiumsolutions@gmail.com)`),
  `TimeoutSeconds`, `MinRequestIntervalMs` (1100ms default).
- `Services/NominatimService.cs` — typed `HttpClient`
  (`AddHttpClient<INominatimService, NominatimService>`), throttled via a
  static `SemaphoreSlim` + last-request timestamp so concurrent callers
  never exceed Nominatim's usage policy regardless of request volume.
- `Controllers/Api/GeocodingApiController.cs` —
  `GET /api/geocoding/reverse?lat=&lng=`, anonymous access.

### Distance & nearby search

- `Helpers/DistanceHelper.cs` — static Haversine formula,
  `HaversineDistanceKm(lat1, lng1, lat2, lng2)`.
- `Services/ProviderLocationQueryService.cs` — `FindNearbyProvidersAsync`
  pulls filtered candidates (by `IsAvailable`/`IsVerified`, both `true` by
  default) into memory and applies Haversine there, since the distance
  formula can't be translated into a SQL predicate without spatial types.
  Acceptable at current provider counts; revisit if the provider table grows
  large enough that in-memory filtering becomes a bottleneck (see "Next
  steps").
- `Controllers/Api/ProviderLocationsApiController.cs` —
  `GET /api/provider-locations/nearby`, `GET /api/provider-locations/distance`
  (both anonymous), `PUT /api/provider-locations/{providerUid}`
  (authorized, provider-scoped).

### Live location updates — idempotency

Both write paths (SignalR `PushLocation` and the REST `PUT` fallback) share
one idempotent code path in `ProviderLocationQueryService.UpdateCurrentLocationAsync`:

- The client supplies `clientTimestampUtc` (the device's GPS-fix time, not
  server receive time).
- The write is a single conditional `ExecuteUpdateAsync` guarded by
  `WHERE Uid = @providerUid AND (LocationUpdatedOn IS NULL OR
  LocationUpdatedOn < @clientTimestampUtc)` — an atomic compare-and-swap,
  not a read-modify-write.
- A stale, duplicate, or out-of-order update (retry, race between two
  network paths, delayed packet) affects 0 rows and is treated as a
  **successful no-op**, not an error.
- A broadcast to SignalR subscribers only fires when the write actually
  applied (`rows > 0`) — no-ops never emit a `LocationUpdated` event, so
  clients never see a position regress or a duplicate event for the same
  fix.
- A 5-minute max clock-skew guard rejects `clientTimestampUtc` values too
  far from server time (protects against a broken device clock silently
  freezing a provider's position forever, since every future real update
  would look "older" than the bad one).

This was verified both by code review and by manually replaying the SQL
pattern against the live DB: baseline reset → newer update applied
(`rows_affected=1`) → stale update correctly no-op'd → duplicate/same-timestamp
retry correctly no-op'd.

### Real-time broadcast (SignalR)

- `Hubs/LocationTrackingHub.cs` — `[Authorize]`. Methods: `PushLocation`,
  `JoinBookingGroup`, `LeaveBookingGroup`. Groups are keyed
  `booking-{BookingUid}`, so only clients watching a specific booking receive
  that provider's updates.
- Auth: standard JWT bearer, but read from an `access_token` query string
  parameter instead of the `Authorization` header — WebSocket handshakes
  can't set custom headers from browser/mobile SignalR clients. This is
  scoped narrowly in `Program.cs`'s `OnMessageReceived` handler to paths
  starting with `/hubs/location` only, so it doesn't weaken auth anywhere
  else in the app.
- Hub endpoint: `/hubs/location`.

Full request/response contracts, including the idempotency behavior (what a
retry does, what a race between two callers does), are documented in
`api.txt` under **MAPS / GPS APIs** — that section is the source of truth
for integrating a client, not this file.

## What was built (Phase 2 — admin test harness)

- `Controllers/MapsTestController.cs` +
  `Views/MapsTest/Index.cshtml` / `_MapsTestScripts.cshtml` —
  `/Admin/MapsTest`, restricted to `Super Admin`, `Admin`, `Dispatcher`,
  `Customer Support`.
- Four panels, all exercising the real API/hub (no mocks):
  1. Pin drop → reverse geocode.
  2. Nearby-provider search on a map.
  3. Live tracking subscribe (join a booking group, watch a marker move via
     SignalR).
  4. Simulate a provider location push, including a "Resend Last" button
     specifically to demonstrate the idempotent no-op behavior end-to-end.
- Built with Leaflet.js + `@microsoft/signalr`, both via CDN — no
  npm/webpack pipeline was introduced for this.
- This exists purely to validate the backend without needing the Flutter
  app built first. It is an internal admin tool, not a customer-facing
  feature — don't extend its scope beyond testing.

## Deployment notes

- Production config (`appsettings.Production.json` on the GCP VM) needs the
  same `Nominatim` block as local `appsettings*.json`. This file is
  excluded from the CI/CD rsync by design (server-side config persists
  across deploys), so it was synced manually via SCP — already done as of
  this feature's initial deploy. If the Nominatim settings ever change,
  remember to update the production file by hand too; the pipeline will not
  do it.
- No new environment variables or secrets were introduced — Nominatim
  requires no API key.

## Next steps (Phase 3 and beyond — not started)

1. **Flutter mobile integration** (Phase 3, explicitly out of scope for the
   backend work done so far — hand this to whichever agent/session owns the
   Flutter app):
   - Wire `google_maps_flutter` for rendering.
   - Call `GET /api/geocoding/reverse` for address display.
   - Call `GET /api/provider-locations/nearby` for client-side "providers
     near me" search.
   - Provider app: connect to `/hubs/location` via `@microsoft/signalr`'s
     Dart/Flutter equivalent (`signalr_netcore` or similar), call
     `JoinBookingGroup`/`PushLocation` on an interval while a job is active,
     with the REST `PUT /api/provider-locations/{providerUid}` as a
     fallback when the socket is down.
   - Client app: join the relevant booking group, listen for
     `LocationUpdated`, move a marker.
   - Reuse `api.txt`'s MAPS / GPS APIs section as the integration spec —
     don't re-derive contracts from controller code.
2. **Location history** — currently out of scope by design (last-known-position
   only). If trip replay, ETA-from-history, or analytics become a
   requirement, this needs a new table (e.g. `ProviderLocationHistory`)
   rather than repurposing the `Providers` columns, and its own retention/
   cleanup policy so it doesn't grow unbounded.
3. **Nearby-search scalability** — `FindNearbyProvidersAsync` filters in
   memory after a SQL pull. Fine at current scale; if the provider table
   grows large, consider either a bounding-box pre-filter in SQL (cheap,
   avoids full Haversine on obviously-far rows) before the in-memory
   Haversine pass, or (only if truly necessary) introducing spatial indexing
   — but that reopens the "$0-cost, no spatial types" tradeoff made for this
   phase, so don't reach for it prematurely.
4. **ETA / route-aware distance** — current "distance" is straight-line
   (Haversine), not road distance or ETA. If the product ever needs real
   ETA, that requires a routing API and breaks the $0-cost constraint this
   feature was built under — flag that tradeoff explicitly to the user
   before implementing rather than assuming it's wanted.
5. **Rate-limit resilience** — Nominatim's free tier is a shared public
   resource with a strict usage policy. The current throttle
   (`MinRequestIntervalMs`) protects against this app exceeding it, but
   there's no fallback/cache if Nominatim itself is down or blocks the
   `UserAgent`. Consider a short-lived cache (e.g. round coordinates to
   ~100m and cache reverse-geocode results for a few hours) if reverse
   geocoding volume grows.
