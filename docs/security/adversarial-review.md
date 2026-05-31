# Adversarial Multi-Agent Review — Katie's Garden

**Date:** 2026-05-31

Five specialist agents audited the codebase from conflicting incentives, then each
finding was cross-examined by the agent most likely to object before anything was
applied. This document is the audit trail: what each agent found, what survived
cross-examination, and what was deliberately deferred (with rationale).

## Agents

| Agent | Bias | Focus |
|-------|------|-------|
| 🔴 Pen Tester | adds guards | auth coverage, Stripe tampering, enumeration, secrets |
| 🟢 QA Automation | tests everything | coverage gaps on new helpers, abuse cases *(ran out of session budget mid-run; its mandate was absorbed into the verification pass)* |
| 🔵 Senior Front-End | adds UX/abstractions | a11y, error/loading states, double-submit, dead code |
| ⚫ Slop Remover | deletes / folds in | single-use abstractions, ceremonial files, duplication |
| 🟣 Backend Architect | flags correctness risk | EF/migration drift, idempotency races, transaction atomicity |

## Conflict-resolution rulebook

1. Security beats minimalism only with a demonstrated exploit path.
2. A test must assert behaviour an attacker or user would notice.
3. A front-end change must be user-visible or a measured win.
4. "It already exists" ends the debate.
5. Every new public type owes a test, or it gets folded into its caller.
6. Ties go to less code.

## Verdict table

| Finding | Verdict | Rationale |
|---------|---------|-----------|
| Stripe dedup row committed *before* processing → a transient fault permanently drops a paid order (Architect CRIT) | **Applied** | Record the processed-event row last; order-status check keeps reprocessing idempotent. |
| Webhook never checks `session.AmountTotal` vs `order.Total` (Pen HIGH) | **Applied** | Defence-in-depth against forged/mismatched events. |
| Webhook accepts events when `STRIPE_WEBHOOK_SECRET` empty (Pen CRIT, downgraded) | **Applied** | Empty secret is attacker-knowable; reject when unset. |
| Delivery-settings save is a silent no-op on a fresh DB; wrong fees forever (Architect HIGH) | **Applied** | `Add` the singleton row when missing; pin `Id` with `ValueGeneratedNever`. |
| Email stored raw at checkout, looked up lower-cased → guest lockout (Pen HIGH) | **Applied** | Normalise email at checkout. |
| `CheckoutRequestValidator` used `.EmailAddress()` while the rest use `EmailRegex.Pattern` (Slop) | **Applied** | Consistency + catches `user@tld`-style typos. Test cases added. |
| `CustomerOrderService` swallowed transient errors as "not found"; guest alert not announced (Front-End HIGH) | **Applied** | 404 vs transient distinguished; `role="alert"` added; `MyOrders` + guest lookup get retryable error states. |
| Search submit fired the query twice (Front-End MED) | **Applied** | Navigation triggers the single fetch; duplicate removed (−8 LOC). |
| Empty `Api/Middleware/` folder (Slop) | **Applied** | Deleted. |
| Order-number timing oracle; proposed fix was a `Random.Shared` delay (Pen HIGH) | **Rejected** | The proposed fix isn't constant-time; a 65 k/day space under a 10/min edge limit is not a practical oracle. Accepted risk. |
| `OrderVerification` UTF-8 vs ASCII byte encoding (Pen MED) | **Rejected** | No behavioural difference for digit strings. |
| `DiagnosticsFunction` injects API keys into health-check requests (Pen LOW) | **Rejected** | That is how the upstream APIs are called; endpoint is edge-rate-limited. |
| Admin list pages blank to the global error bar on fetch failure (Front-End HIGH) | **Deferred** | Real, but admin-only and degrades to a functional global error bar. Apply the `AdminDashboard` try/catch + Retry pattern to `OrderList`/`ProductList`/`CollectionList` next. |
| `CustomerOrderService.GetOrderAsync` still swallows transient as "not found" (Front-End HIGH) | **Deferred** | Interacts with the 30 s polling timer on `OrderDetail`; needs careful handling in the poll callback. |
| `Create/Update*Validator` and request-DTO duplication; deserialize-boilerplate helper (Slop, ~−50 LOC) | **Deferred** | Net-good, but folding two different-typed validators / editing 8 functions in the same pass as a large Stripe change risks the over-abstraction the project explicitly avoids. |
| Migration advisory-lock, orphan-`Pending` reaper, status-history transaction atomicity, oversell visibility flag, SMTP timeout/retry (Architect MED/HIGH) | **Deferred** | Larger architectural changes; tracked here rather than rushed into this diff. |

## Confirmed correct (so future reviews don't re-litigate)

- `RequireAdmin()` is enforced on every mutating admin endpoint; no IDOR found on `/api/manage/*` or customer order access.
- No SQL injection / SSRF / path traversal; search ILIKE escapes `\ % _` before a parameterised query.
- Orchestrator is deterministic (no `DateTime.Now`/`Guid.NewGuid`/`Random`/I-O inside the orchestrator).
- Money fields are `numeric(10,2)` consistently across entity config, migration, and snapshot.
- No `new HttpClient`, no async-over-sync; `DbContext` is scoped; SMTP client is disposed.
- CSP/security headers and TLS config are solid.

## Note on the EF snapshot

`DeliverySettings.Id` is now mapped `ValueGeneratedNever()`. The column stays
`GENERATED BY DEFAULT AS IDENTITY` in the database (which accepts explicit inserts),
so no schema migration is required at runtime. The model snapshot is intentionally
left as-is; the next `dotnet ef migrations add` will pick up the mapping change.
