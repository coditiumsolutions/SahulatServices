# Payment Module Test Report

Date: 2026-08-04
Scope: booking completion → ledger posting → provider wallet → provider payouts (`PaymentService.cs`, `BookingService.cs` completion paths, `ProviderWalletApiController`, `PaymentsController`).
Method: 20 scenario-based tests executed against a running local instance (`https://localhost:7265`) using real HTTP calls (curl) against both the admin portal (cookie + antiforgery auth) and the mobile REST API (`/api/service-bookings/*`, `/api/providers-wallet/*`), with live database state inspected via the wallet/ledger endpoints after each step. No test was accepted as passing on code inspection alone — every case below was actually run.

## Summary

| Result | Count |
|---|---|
| Passed | 18 |
| Bug found & fixed during this pass | 1 |
| Design gap flagged (not a crash, needs a product decision) | 1 |

## Bug found and fixed

### BUG-1: Commission/provider-earning desync on passcode-verified completion

**Where:** `BookingService.VerifyCompletionPasscodeAsync` (the endpoint the provider mobile app calls to complete a job — `POST /api/service-bookings/{id}/verify-completion`).

**What was wrong:** The method overwrote `booking.CustomerPaid` with the amount the provider says they actually collected, but left `CommissionAmount` and `ProviderEarning` untouched — those were still whatever `AssignProviderAsync` computed from the *estimated* price when the job was first assigned, days or minutes earlier, before it was known how much the client would actually pay.

**Impact:** Any job where the on-site price differed from the estimate — a very common case (extra parts, a discount, a price renegotiated on-site, or a genuinely partial payment) — posted ledger entries with a commission split that didn't match the cash that actually moved. Concretely: a job estimated at 1,000 with 10% commission, but where the provider actually collects 1,200, would still post `Commission=100` / `ProviderEarning=900` against a `CashCollect` debit of `-1,200` — the numbers no longer reconcile, and the company under- or over-charges its commission depending on which direction the price moved.

**Fix applied:** `VerifyCompletionPasscodeAsync` now treats the amount actually collected as the real final bill — it sets `FinalAmount = actualAmountPaid` and recomputes `CommissionAmount`/`ProviderEarning` from it using the booking's existing `CommissionType`/`CommissionValue`, the same formula `ApplyBookingTotals`/`ApplyAssignTotals` already use elsewhere. This is the same pattern already established in the rest of the codebase, just applied at the one place it was missing.

**Verified by:** TC-03 (price increased on-site: 1,000 estimate → 1,200 collected → commission correctly recalculated to 120, provider earning to 1,080) and TC-04 (partial payment case, see design gap below).

**File changed:** `HomeServicesPortal/Services/BookingService.cs` (`VerifyCompletionPasscodeAsync`).

## Design gap flagged (not fixed — needs a product decision)

### GAP-1: "Partial payment" and "final price was lower than estimated" are indistinguishable

Fixing BUG-1 above closes one hole but opens a semantic question the codebase doesn't currently answer: when a provider reports `actualAmountPaid` that's *less* than the booking's original estimate, there are two completely different real-world situations that produce the exact same API call:

1. The job simply cost less than estimated (e.g., a smaller repair than expected) — the client owes nothing further, this is genuinely the final bill.
2. The client only paid part of the agreed price and still owes the rest (a genuine partial payment) — the remaining amount is still owed and needs to be tracked and chased.

The current fix (and the pre-existing code before it) can't tell these apart — `FinalAmount` is always set equal to `actualAmountPaid`, so `CustomerRemaining` is always forced to 0 on completion. Tested directly: a 1,000 estimate with 400 actually collected completes with `finalAmount: 400, customerRemaining: 0` — the missing 600 is silently forgotten rather than tracked as still-owed. This was true before my fix too (the old code computed `CustomerRemaining` off the stale `FinalAmount` so it would have shown `600` owed, but then never gave staff a way to later update `CustomerPaid` again since the booking is now `Completed` — so the 600 was unrecoverable either way, just recorded differently).

This isn't something to silently guess an answer for — whether "partial payment on a fixed price" needs to survive as a trackable receivable is a product decision (there is already a `PersonLedgerAdd` manual-entry admin tool that could be the intended mechanism for staff to chase a shortfall by hand after the fact, which may be the intended design). Recommend deciding explicitly whether:
- (a) the mobile app should always require full payment to call verify-completion (client-side/API contract decision), or
- (b) a genuine partial-payment-with-remaining-debt flow needs a `paymentMode`/flag distinguishing "this is the final agreed price" from "client still owes X", with staff able to collect the remainder later via the existing manual ledger tool.

No code change was made for this — flagged for a decision.

## Full test log

All tests run against live data on the local dev instance; provider 3 ("Shahid", Electrician) was the primary test subject since it had pre-existing ledger history to check regressions against.

| # | Scenario | Steps | Expected | Actual | Result |
|---|---|---|---|---|---|
| TC-01 | Cash job, full payment == estimate | Assign (1000, Cash, 10%) → accept → verify-completion(1000) | JobEarning +900, CashCollect −1000, wallet balance −100 | Exactly as expected | ✅ PASS |
| TC-02 | Online job, full payment == estimate | Assign (1000, Online, 10%) → accept → verify-completion(1000) | CustPayment credit to Company, JobEarning +900 to provider, ProviderPayout row Pending | Wallet +900, `pendingPayoutTotal: 900` | ✅ PASS |
| TC-03 | Cash job, price increased on-site (1000 est. → 1200 collected) | Assign (1000, Cash, 10%) → accept → verify-completion(1200) | `FinalAmount`/commission recalculated off 1200, not the stale 1000 estimate | `finalAmount:1200, commissionAmount:120, providerEarning:1080` | ✅ PASS (bug fixed, see BUG-1) |
| TC-04 | Cash job, partial payment (1000 est. → 400 collected) | Assign (1000, Cash, 10%) → accept → verify-completion(400) | Commission recalculated off actual 400, no stale-estimate desync | `finalAmount:400, commissionAmount:40, providerEarning:360, customerRemaining:0` — correct math, but the missing 600 vanishes rather than being tracked as owed | ⚠️ Passes on the bug fix, but surfaces GAP-1 |
| TC-05 | Online job, mixed with existing cash debt | Assign (1000, Online, 10%) → accept → verify-completion(1000), on a provider who already had negative balance from TC-01/03/04 | JobEarning credit posts correctly on top of existing debt entries; wallet balance = sum of everything | Wallet balance 640.00 (= 900 − 260 combined cash debt), `pendingPayoutTotal: 900` | ✅ PASS |
| TC-06 | Job completed with zero payment collected | Assign (300, Cash) → accept → verify-completion(0) | Booking completes; no ledger rows posted (both `CommissionAmount>0` and `CustomerPaid>0` guards are false) — no crash, no phantom entries | `finalAmount:0, commissionAmount:0, providerEarning:0`, zero ledger rows for the booking | ✅ PASS — but see note below |
| TC-07 | Duplicate completion attempt (same booking, called twice) | verify-completion(500) → verify-completion(500) again with same passcode | Second call rejected; ledger has exactly one JobEarning + one CashCollect row, not two | Second call: `"This booking is not awaiting completion."`; exactly 2 ledger rows for the booking | ✅ PASS |
| TC-08 | Wrong passcode | verify-completion with an incorrect 4-digit code | Rejected, booking status unchanged | `"Incorrect passcode."`, no state change | ✅ PASS |
| TC-09 | Provider rejects assignment | respond(accept:false) | Booking → Rejected; parent request reverts to Pending; request becomes re-assignable | Confirmed status flip and that `/Assign/{id}` loads again (200, not redirected away as already-booked) | ✅ PASS |
| TC-10 | Pay a provider with zero balance and no pending payouts | POST PersonLedger/{id}/Pay | Rejected with a clear message, no ledger entry posted | `"No pending payout and no positive balance to pay out."`, no debit posted | ✅ PASS |
| TC-11 | (merged into TC-01/12) | — | — | — | — |
| TC-12 | Payout nets a mixed positive (online) + negative (cash debt) balance | Provider had −100, −220, −260 (three cash jobs) and +900 (one online job) simultaneously; balance 640 | Pay Now disburses exactly 640, not 900, and not each component separately | Posted a single `Payout` debit of exactly −640.00, balance → 0.00, the online `ProviderPayout` row marked Paid | ✅ PASS — this is the core "automatically deducts its remaining cut" feature working correctly |
| TC-13 | Percent commission > 100% | Assign with `CommissionValue=150`, `Percent` | Rejected server-side, no booking created | Request stayed `Pending`, no new booking row created (rejected by `[Range(0, double.MaxValue)]` + `AssignProviderAsync`'s own `>100` guard) | ✅ PASS |
| TC-14 | Fixed commission amount exceeding the final bill | Assign with `Fixed` commission of 500 on a 300 bill | Rejected server-side (`ValidateBookingTotals`: commission cannot exceed final bill) | Request stayed `Pending`, no booking created | ✅ PASS |
| TC-15 | Negative commission value | Assign with `CommissionValue=-10` | Rejected — `[Range(0, double.MaxValue)]` data annotation blocks it at model-binding, before any custom logic runs | Request stayed `Pending`, no booking created | ✅ PASS |
| TC-16 | Wallet API for a nonexistent/non-provider id | `GET /api/providers-wallet/999` (and ids belonging to clients, not providers) | 404 with `ApiResponse.Fail` | `{"success":false,"message":"Provider not found.","data":null}` | ✅ PASS |
| TC-17 | Wallet balance vs admin Person Ledger balance consistency | Compare `GET /api/providers-wallet/3` against `/Admin/Payments/PersonLedger/3` after a run of mixed transactions | Both numbers identical (both derive from the same `PaymentLedger` rows) | Both showed exactly −1,080.00 | ✅ PASS |
| TC-18 | Admin directly edits a booking to `Status=Completed`, bypassing the passcode flow entirely | `/Admin/Bookings/Edit/{id}` with `Status=Completed`, saved twice in a row | Ledger posts exactly once on the first save; the second save (already Completed) does not duplicate entries | Exactly 2 ledger rows (1 JobEarning + 1 CashCollect) present after both saves | ✅ PASS — the `alreadyPosted` guard in `RecordBookingCompletionAsync` correctly protects this path too, not just the passcode path |
| TC-19 | IDOR: wrong provider tries to accept/reject someone else's booking | `respond` with a `providerUid` that doesn't own the booking | Rejected, generic "not found" (doesn't leak booking existence) | `{"success":false,"message":"Booking not found.","data":null}` | ✅ PASS |
| TC-20 | IDOR: wrong provider tries to verify completion using the correct (leaked/guessed) passcode | `verify-completion` with the right passcode but wrong `providerUid` | Rejected — ownership is checked before the passcode is even compared | `{"success":false,"message":"Booking not found.","data":null}` | ✅ PASS |
| TC-21 | Negative `actualAmountPaid` at completion | `verify-completion` with `actualAmountPaid: -50` | Rejected cleanly, booking state untouched | `"Amount paid cannot be negative."`, booking still `Accepted` | ✅ PASS |
| TC-22 | Pay-out attempt on a provider with a large negative balance and no pending payouts | Provider owed the company 1,080 net from a single large cash job (2000 bill, 50% commission) | Rejected, no debit posted, balance stays exactly as it was (not zeroed, not made more negative) | `"No pending payout and no positive balance to pay out."`, balance unchanged at −1,080.00 | ✅ PASS |

Note on TC-06: this is not a crash or a data-integrity bug — the system behaves consistently (no money in, no ledger entries, no error). It is flagged here only because it means a provider could report `actualAmountPaid: 0` on a job that was actually paid in cash off the books, and the system has no way to detect or flag that as suspicious. This is a fraud/trust concern rather than a software bug, and is out of scope for this pass, but worth the team's awareness.

## Areas verified safe by design (code-read, not independently re-tested beyond what's above)

- **Idempotency**: `RecordBookingCompletionAsync` guards on `_db.PaymentLedgers.AnyAsync(l => l.BookingUid == booking.Uid)` before posting anything, so no code path — passcode completion, direct admin edit, or the periodic `SyncCompletedBookingsAsync` catch-up sync — can double-post for the same booking. Confirmed live via TC-07 and TC-18 from two different entry points.
- **Payout netting** (`PayProviderAsync`) always caps the disbursed amount at `Math.Max(0, balance)`, so it can never pay out more than the provider's actual net-positive balance, and it always marks pending payout rows Paid even when the disbursed cash is 0 (fully offset by debt) — verified in TC-12.
- **Validation layering**: business-rule checks in `AssignProviderAsync`/`ValidateBookingTotals` (commission ≤ final bill, percent ≤ 100) sit behind a first line of defense from `[Range(0, double.MaxValue)]` data annotations on the view models, so malformed input is rejected at model-binding before custom logic even runs.

## Not tested (out of scope for this pass, flagged for awareness)

- **True concurrent completion race**: `RecordBookingCompletionAsync`'s idempotency check is a check-then-act read followed by a separate `SaveChangesAsync` — under genuine concurrent requests (two simultaneous `verify-completion` calls, or a `verify-completion` racing the periodic sync job) there is a theoretical window for a duplicate post. This requires true parallel requests to reproduce and was not exercised by this pass's sequential curl-based testing.
- **`CommissionRule` entity application**: this pass confirms (again) that the `CommissionRule` CRUD system is entirely disconnected from actual booking commission calculation — bookings use their own `CommissionType`/`CommissionValue` fields set at assignment time, not any `CommissionRule` row. Not a bug introduced by this pass, just re-confirmed as pre-existing and worth remembering if `CommissionRule` is ever wired up later.
