# Adversarial Multi-Agent Review — Katie's Garden

## Round 1 — 2026-05-31

Five specialist agents audited the codebase from conflicting incentives, then each
finding was cross-examined by the agent most likely to object before anything was
applied. This document is the audit trail: what each agent found, what survived
cross-examination, and what was deliberately deferred (with rationale).

### Agents

| Agent | Bias | Focus |
|-------|------|-------|
| 🔴 Pen Tester | adds guards | auth coverage, Stripe tampering, enumeration, secrets |
| 🟢 QA Automation | tests everything | coverage gaps on new helpers, abuse cases *(ran out of session budget mid-run; its mandate was absorbed into the verification pass)* |
| 🔵 Senior Front-End | adds UX/abstractions | a11y, error/loading states, double-submit, dead code |
| ⚫ Slop Remover | deletes / folds in | single-use abstractions, ceremonial files, duplication |
| 🟣 Backend Architect | flags correctness risk | EF/migration drift, idempotency races, transaction atomicity |

### Conflict-resolution rulebook

1. Security beats minimalism only with a demonstrated exploit path.
2. A test must assert behaviour an attacker or user would notice.
3. A front-end change must be user-visible or a measured win.
4. "It already exists" ends the debate.
5. Every new public type owes a test, or it gets folded into its caller.
6. Ties go to less code.

### Round 1 verdict table

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

### Round 1 confirmed correct (so future reviews don't re-litigate)

- `RequireAdmin()` is enforced on every mutating admin endpoint; no IDOR found on `/api/manage/*` or customer order access.
- No SQL injection / SSRF / path traversal; search ILIKE escapes `\ % _` before a parameterised query.
- Orchestrator is deterministic (no `DateTime.Now`/`Guid.NewGuid`/`Random`/I-O inside the orchestrator).
- Money fields are `numeric(10,2)` consistently across entity config, migration, and snapshot.
- No `new HttpClient`, no async-over-sync; `DbContext` is scoped; SMTP client is disposed.
- CSP/security headers and TLS config are solid.

### Note on the EF snapshot

`DeliverySettings.Id` is now mapped `ValueGeneratedNever()`. The column stays
`GENERATED BY DEFAULT AS IDENTITY` in the database (which accepts explicit inserts),
so no schema migration is required at runtime. The model snapshot is intentionally
left as-is; the next `dotnet ef migrations add` will pick up the mapping change.

---

## Round 2 — 2026-05-31

Rerun with additional focus on the **Slop Remover** and a new **Code Quality** agent.
All Round 1 deferred items were also reconsidered.

### Round 2 agents

| Agent | Bias | Focus |
|-------|------|-------|
| 🔴 Pen Tester | adds guards | auth consistency, race conditions |
| 🟢 QA Automation | tests everything | coverage gaps, vacuous assertions |
| 🔵 Senior Front-End | adds UX/abstractions | disposal patterns, a11y, error states |
| ⚫ Slop Remover (focus) | deletes / folds in | magic literals, private duplication, ceremonial usings |
| 🟣 Backend Architect | flags correctness risk | transaction atomicity, disposal leaks, FK integrity |
| 🟡 Code Quality | flags maintainability | bare catch blocks, async void, dead flags |

### Round 2 verdict table

| Finding | Agent | Verdict | Applied to |
|---------|-------|---------|------------|
| `"StatusChanged"` string literal in 3 files — silent typo causes orchestrator to hang forever | Architect/Slop | **Applied** | Extracted to `OrchestrationEvents.StatusChanged` in `OrderOrchestratorInput.cs` |
| `audit.LogAsync(actor, actor, ...)` — OAuth `UserDetails` is a username, not an email | Quality | **Applied** | `OrderService.RecordTransitionAsync`: `actorEmail: null, actorName: actor` |
| `AnonymiseAsync` wrote PII erasure and audit evidence in separate transactions | Architect CRIT | **Applied** | `OrderService.AnonymiseAsync`: single `SaveChangesAsync` with inline `AuditLog` |
| Refund: Stripe called before DB save; retry would issue second refund | Architect HIGH | **Applied** | `AdminOrderFunction.RefundOrder`: RepeatableRead tx, re-check status, handle `charge_already_refunded` |
| `migrateDb` not disposed when `MigrateAsync` throws | Architect MED | **Applied** | `Program.cs`: `ownedMigrateDb` variable + `finally { await ownedMigrateDb.DisposeAsync() }` |
| `OrderLine.ProductId` → `products.id` FK missing from EF config | Architect MED | **Applied** | `AppDbContext.cs`: `HasOne<Product>().WithMany()…OnDelete(Restrict)`; `AdminProductFunction.Delete` pre-checks `OrderLines.Any` |
| Bare `catch {}` in `AdminProductFunction`, `AdminCollectionFunction`, `CheckoutFunction` swallows `OperationCanceledException` | Quality | **Applied** | Changed to `catch (JsonException)` + added `using System.Text.Json` |
| `private const string PostgresUniqueViolation = "23505"` duplicated in `SubscribeFunction` (Npgsql already has `PostgresErrorCodes.UniqueViolation`) | Slop | **Applied** | Deleted private const; use `Npgsql.PostgresErrorCodes.UniqueViolation` in both callers |
| `5 * 1024 * 1024` (5 MB limit) in three files | Slop | **Applied** | `Shared/Constants.cs: MaxImageFileSizeBytes`; editors + API function updated |
| `AdminImageFunction` uses `SwaAuth.IsAdmin()` directly; all other admin functions use `RequireAdmin()` | Quality/Slop | **Applied** | Standardised to `if (req.RequireAdmin() is { } deny) return deny;` |
| `CustomerOrderService.GetOrderAsync` swallows all exceptions as null (Round 1 deferred) | Front-End HIGH | **Applied** | Now throws on transient; distinguishes 404 from error; poll callback catches exceptions |
| `SaveNotesAsync` always showed success toast regardless of HTTP response | Quality | **Applied** | `Admin/OrderDetail.razor`: check `response.IsSuccessStatusCode` |
| `_updatingStatus`, `_refunding` not reset in `finally` — stuck-busy on exception | Front-End | **Applied** | `Admin/OrderDetail.razor`: `try/finally` on all three button handlers |
| `_saving` not in `finally` in `ProductEditor` and `CollectionEditor` | Front-End | **Applied** | `try/finally { _saving = false; }` in both editors |
| `ShopIndex.OnInitializedAsync` no try/catch — blank page on transient failure | Front-End | **Applied** | Added `_loadError` + `role="alert"` error state |
| `DeliverySettingsPage.OnInitializedAsync` no try/catch | Front-End | **Applied** | Added `_loadError` + `MudAlert` error state |
| JSON-LD in `ShopProduct.razor` didn't escape backslashes before quotes | Quality | **Applied** | Added `.Replace("\\", "\\\\")` before `.Replace("\"", "\\\"")` |
| Checkout labels had no `for`/`id` pairing on 6 fields | Front-End/a11y | **Applied** | `CheckoutPage.razor`: `id` on inputs, `for` on labels |
| Timer callback in `OrderDetail.razor` could fire after disposal | Front-End | **Applied** | Added `_disposed` flag; poll callback guarded |
| Timer callback in `ToastNotification.razor` could fire after disposal | Front-End | **Applied** | Added `_disposed` flag; both callback levels guarded |
| `async void ToggleSearch()` in `NavMenu.razor` | Quality | **Applied** | Changed to `async Task` |
| `SearchResultsPage` foreach missing `@key` | Front-End | **Applied** | Added `@key="p.Id"` |
| Radio group in `CartPage` not wrapped in `<fieldset>`/`<legend>` | Front-End/a11y | **Applied** | Wrapped delivery options in `<fieldset><legend>` |
| MudBlazor.min.css (584 KB) loaded globally from `index.html` | Front-End/Perf | **Applied** | Moved to `AdminLayout.razor` `<HeadContent>` — only loaded for admin pages |
| `MigrationIntegrationTests` `>= 0` assertions always pass (vacuous) | QA | **Applied** | Removed redundant assertions; intent preserved by query throwing on missing table |
| `OrderNumberHelperTests`: 3 of 4 tests fully subsumed by regex test | Slop/QA | **Applied** | Removed `StartsWithKgPrefix`, `TodayDateMatches`, `HasFourCharHexSuffix` |
| `CreateProductRequestValidatorTests` missing ImageUrls boundary cases | QA | **Applied** | Added `SevenImages_Fails` and `SixImages_Passes` |
| `CreateCollectionRequestValidatorTests` missing `DescriptionExactly2000Chars_Passes` | QA | **Applied** | Added |

### Round 2 deferred

| Finding | Rationale |
|---------|-----------|
| Admin list pages (`OrderList`/`ProductList`/`CollectionList`) blank on fetch failure | Admin-only; degrades to global error bar. Isolated cleanup pass needed. |
| `CustomerOrderService.GetOrderAsync` interaction with 30 s poll on `OrderDetail` (Round 1 deferred context) | Poll now catches exceptions; the 404-vs-error distinction on the polling path is a UX decision about whether to stop polling on error. |
| Validator test parametrisation (−170 LOC) | Separate cleanup pass; doesn't touch business logic. |
| `AuditService` DbContext factory isolation | Architectural — factory-scoped EF context is a separate concern. |
| `Responses.cs` inline | Stylistic; no correctness impact. |
| DTO class/record consistency | Cosmetic; no build or runtime impact. |

### Round 2 confirmed correct

- All 210 unit tests pass after Round 2 changes.
- `Npgsql.PostgresErrorCodes.UniqueViolation` already existed — our private const was the slop; Npgsql's class is the canonical source.
- `OrchestrationEvents.StatusChanged` constant eliminates the silent-hang risk; the constant lives alongside the event DTOs it relates to.
- GDPR erasure atomicity: PII wipe and audit evidence now share a single `SaveChangesAsync`, so the evidence row cannot be absent when data is gone.
