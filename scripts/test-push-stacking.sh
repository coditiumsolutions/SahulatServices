#!/usr/bin/env bash
# Push stacking test: one real booking driven through accept -> start -> verify-completion, 10 s apart, so the
# device can be watched. Each push carries booking_id / request_id, so the tag "booking-{id}" should leave ONE banner
# showing the latest ("Job completed"). Everything the run creates is deleted by explicit ids at the end (also on
# error/Ctrl+C). See docs/notification-testing.md sections 3, 4 and 8.
#
# Needs: bash, curl, python, sqlcmd. Credentials come from the environment, never from this file:
#   ADMIN_USER, ADMIN_PASS          admin portal login
#   DBS, DBN, DBU, DBP              SQL Server host, database, user, password (same DB the BASE url serves)
# Optional: BASE (default https://sahulatghartak.com), CLIENT_UID (74), PROVIDER_UID (35), USER_ID (76), GAP (10)
#
# Before running: the device token for USER_ID must be registered as Client (the script aborts otherwise), the app
# open on the client side, and nobody else should be using account 76. Run against production only on purpose.
set -u
BASE="${BASE:-https://sahulatghartak.com}"
CLIENT_UID="${CLIENT_UID:-74}"; PROVIDER_UID="${PROVIDER_UID:-35}"; USER_ID="${USER_ID:-76}"; GAP="${GAP:-10}"
: "${ADMIN_USER:?}" "${ADMIN_PASS:?}" "${DBS:?}" "${DBN:?}" "${DBU:?}" "${DBP:?}"
JAR="$(mktemp)"; REQ=""; BK=""

sql()  { sqlcmd -S "$DBS" -d "$DBN" -U "$DBU" -P "$DBP" -C -h -1 -W -s'|' -Q "SET NOCOUNT ON; $1"; }
json() { python -c "import sys,json; d=json.load(sys.stdin); print(eval('d'+sys.argv[1]))" "$1"; }
post() { curl -sS -X POST "$BASE$1" -H 'Content-Type: application/json' -d "$2"; }

cleanup() {
  rm -f "$JAR"
  [ -z "$REQ" ] && return
  BKS="${BK:-0}"
  echo "== Cleanup: request $REQ, booking ${BK:-none} (explicit ids)"
  sql "BEGIN TRAN;
    DELETE FROM PaymentLedger WHERE BookingUID IN ($BKS);
    DELETE FROM BookingMaterialItems WHERE BookingUID IN ($BKS);
    DELETE FROM ServiceBookings WHERE RequestUID = $REQ;
    DELETE FROM UserNotifications WHERE RequestUid = $REQ;
    DELETE FROM AdminNotifications WHERE (Type='ServiceRequestCreated' AND RelatedEntityUID=$REQ) OR (Type='ProviderBookingCancelled' AND RelatedEntityUID IN ($BKS));
    DELETE FROM CustomerServiceRequests WHERE UID = $REQ;
    COMMIT;"
  echo "   after: $(sql "SELECT 'Req='+CAST(COUNT(*) AS varchar) FROM CustomerServiceRequests WHERE UID=$REQ")"
}
trap cleanup EXIT

echo "== Baseline"
sql "SELECT 'Req='+CAST((SELECT COUNT(*) FROM CustomerServiceRequests) AS varchar)+' Bk='+CAST((SELECT COUNT(*) FROM ServiceBookings) AS varchar)+' Led='+CAST((SELECT COUNT(*) FROM PaymentLedger) AS varchar)+' Inbox='+CAST((SELECT COUNT(*) FROM UserNotifications) AS varchar)+' Adm='+CAST((SELECT COUNT(*) FROM AdminNotifications) AS varchar)"
ROLES="$(sql "SELECT DISTINCT UserType FROM UserDeviceTokens WHERE UserId=$USER_ID")"
echo "   device token role(s) for user $USER_ID: $ROLES"
[ "$ROLES" = "Client" ] || { echo "ABORT: token must be registered as Client (log in / switch to customer in the app)"; exit 1; }

# A category the provider serves, an address of the client, and an active title in that category.
CAT="$(sql "SELECT TOP 1 CategoryUID FROM ProviderCategories WHERE ProviderUID=$PROVIDER_UID ORDER BY PrimaryCategory DESC")"
ADDR="$(sql "SELECT TOP 1 UID FROM ClientAddresses WHERE ClientUID=$CLIENT_UID ORDER BY UID")"
TITLE_UID="$(sql "SELECT TOP 1 UID FROM ServiceTitles WHERE CategoryUID=$CAT AND IsActive=1 ORDER BY DisplayOrder")"
echo "   category=$CAT address=$ADDR serviceTitleUid=$TITLE_UID"

echo "== 1. Create request"
R="$(post /api/customer-service-requests "{\"clientUid\":$CLIENT_UID,\"categoryUid\":$CAT,\"clientAddressUid\":$ADDR,\"serviceTitle\":\"NOTIF TEST stacking\",\"serviceTitleUid\":$TITLE_UID,\"contactNo\":\"03000000000\",\"remarks\":\"NOTIF TEST stacking\"}")"
REQ="$(echo "$R" | json "['data']['uid']")" || { echo "create failed: $R"; exit 1; }
echo "   request UID $REQ"

echo "== 2. Assign provider $PROVIDER_UID (admin portal)"
T="$(curl -sS -c "$JAR" -b "$JAR" "$BASE/adminportal" | grep -o '__RequestVerificationToken" type="hidden" value="[^"]*' | head -1 | sed 's/.*value="//')"
curl -sS -c "$JAR" -b "$JAR" -o /dev/null -X POST "$BASE/Account/Login" --data-urlencode "Username=$ADMIN_USER" --data-urlencode "Password=$ADMIN_PASS" --data-urlencode "RememberMe=false" --data-urlencode "__RequestVerificationToken=$T"
T="$(curl -sS -c "$JAR" -b "$JAR" "$BASE/Admin/ServiceRequests/Assign/$REQ" | grep -o '__RequestVerificationToken" type="hidden" value="[^"]*' | head -1 | sed 's/.*value="//')"
[ -n "$T" ] || { echo "ABORT: admin login or assign form failed"; exit 1; }
curl -sS -c "$JAR" -b "$JAR" -o /dev/null -X POST "$BASE/Admin/ServiceRequests/Assign/$REQ" \
  --data-urlencode "__RequestVerificationToken=$T" -d "RequestUid=$REQ&CategoryUid=$CAT&ProviderUids=$PROVIDER_UID" \
  -d "EstimatedAmount=1000&VisitCharges=0&AdditionalCharges=0&Deductions=0&FinalAmount=1000&CustomerPaid=0" \
  -d "PaymentMode=CashToProvider&CommissionType=Percent&CommissionValue=10&CommissionAmount=100&ProviderEarning=900"
BK="$(sql "SELECT TOP 1 UID FROM ServiceBookings WHERE RequestUID=$REQ ORDER BY UID DESC")"
[ -n "$BK" ] || { echo "ABORT: no booking created by the assign step"; exit 1; }
echo "   booking UID $BK (assign pushes 'New job request' to the PROVIDER role only: no banner on a Client token)"

echo "== 3. Provider accepts   -> client push 'Provider accepted'"; post "/api/service-bookings/$BK/respond" "{\"providerUid\":$PROVIDER_UID,\"accept\":true}"; echo; sleep "$GAP"
echo "== 4. Provider starts    -> client push 'Job started'";       post "/api/service-bookings/$BK/start" "{\"providerUid\":$PROVIDER_UID}"; echo; sleep "$GAP"
PASS="$(sql "SELECT Passcode FROM ServiceBookings WHERE UID=$BK")"
echo "== 5. Verify completion  -> client push 'Job completed'"
post "/api/service-bookings/$BK/verify-completion" "{\"providerUid\":$PROVIDER_UID,\"passcode\":\"$PASS\",\"actualAmountPaid\":1000,\"labourAmount\":1000}"; echo

echo "== Inbox rows written for the client (expect accepted, started, completed):"
sql "SELECT Type+' | '+Title FROM UserNotifications WHERE RequestUid=$REQ AND UserType='Client' ORDER BY Id"
echo "== Order sent: respond(accept) -> start -> verify-completion, ${GAP}s apart. Booking UID $BK, request UID $REQ."
echo "   Expected on device: ONE banner for the booking, showing 'Job completed'. Cleanup runs now."
