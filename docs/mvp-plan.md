# MVP Plan

## Goal
Deliver a working, production-ready MVP that enables a mall or store to:
1. Set up branches, warehouses, and item catalog
2. Receive stock from suppliers with full batch/expiry capture
3. Sell items via POS with FEFO enforcement
4. See active alerts for low stock and expiring batches
5. Migrate existing data from Excel files
6. View full audit trail of all operations

---

## MVP Sprints (Estimated)

### Sprint 1 — Foundation (Week 1-2)
**Goal:** Working backend with auth and organization structure

- [x] Solution and project setup
- [x] Domain entities defined
- [x] EF Core + PostgreSQL connected
- [x] Migrations runnable
- [ ] Auth: login, JWT, refresh token
- [ ] Users CRUD with role assignment
- [ ] Roles and permissions seeded
- [ ] Branches CRUD
- [ ] Warehouses CRUD
- [ ] Health endpoint
- [ ] Swagger running
- [ ] Docker compose working

**Deliverable:** Team can log in, manage branches and warehouses

---

### Sprint 2 — Item Catalog (Week 2-3)
**Goal:** Full item catalog management

- [ ] Categories (hierarchical)
- [ ] Units of measure
- [ ] Items CRUD (all fields including track_batch, track_expiry)
- [ ] Item search by name, SKU, barcode
- [ ] Item import from Excel (validated)
- [ ] Import job tracking with row-level errors

**Deliverable:** Catalog of 10,000+ items can be loaded from Excel

---

### Sprint 3 — Purchasing & Stock Receiving (Week 3-4)
**Goal:** Full purchase flow with stock creation

- [ ] Suppliers CRUD
- [ ] Purchase invoice create/edit (Draft)
- [ ] Purchase invoice confirm → triggers stock receive
- [ ] Batch creation on receiving (for tracked items)
- [ ] StockMovement records created on confirm
- [ ] StockBalance updated in same transaction
- [ ] Opening stock import from Excel

**Deliverable:** Stock can be received and is immediately reflected in balances

---

### Sprint 4 — Sales / POS (Week 4-5)
**Goal:** Sales flow with FEFO enforcement

- [ ] Customer management (optional on sale)
- [ ] Sales invoice create
- [ ] Item lookup by barcode/SKU
- [ ] FEFO batch auto-selection
- [ ] Expired batch blocking
- [ ] Sales confirm → stock deduction
- [ ] Stock movement recorded per line
- [ ] Payment method capture
- [ ] Receipt summary

**Deliverable:** Cashier can scan items, complete sale, stock is decremented

---

### Sprint 5 — Alerts & Dashboard (Week 5-6)
**Goal:** Operational awareness

- [ ] Background job: scan low stock
- [ ] Background job: scan near-expiry and expired batches
- [ ] Alert records created automatically
- [ ] Alert resolution workflow
- [ ] Dashboard API: alert counts, recent sales, stock insights
- [ ] Basic frontend dashboard

**Deliverable:** Manager sees real-time operational alerts

---

### Sprint 6 — Frontend Core (Week 6-8)
**Goal:** Working web interface

- [ ] React + TypeScript + Vite setup
- [ ] Auth screens (login)
- [ ] Branch and warehouse management UI
- [ ] Item catalog UI with search
- [ ] Purchase invoice UI
- [ ] POS sales screen (barcode scan)
- [ ] Alert panel
- [ ] Dashboard
- [ ] Excel import UI with result display

**Deliverable:** Full end-to-end flow usable from browser

---

### Sprint 7 — Hardening (Week 8-9)
**Goal:** Production readiness

- [ ] Integration tests for critical flows
- [ ] Unit tests for FEFO, batch selection, stock math
- [ ] Error handling review
- [ ] Performance test with 10k items
- [ ] Audit log review
- [ ] Docker production build
- [ ] Environment configuration cleanup
- [ ] Basic deployment documentation

**Deliverable:** System is stable, tested, deployable

---

## Critical Business Rules Enforced in MVP
1. Expired batches cannot be sold (enforced in SalesService)
2. FEFO batch selection is automatic and cannot be bypassed via API
3. Stock quantity changes only through StockMovement records
4. Every purchase confirmation creates traceable movement records
5. Import jobs validate before committing — no partial silent inserts
6. Audit log written for every invoice confirmation and stock adjustment

---

## Deferred to Post-MVP
| Feature | Reason |
|---|---|
| Warehouse transfers | Foundation ready, UI deferred |
| Accounting / GL | Requires dedicated accounting module |
| PDF invoices | Not blocking for MVP |
| Mobile app | Phase 2 |
| AI forecasting | Phase 3 |
| Multi-currency | Not required for Palestine launch |
| Customer loyalty | Post-MVP |
| Supplier portal | Post-MVP |
| Full reporting engine | Basic dashboard covers MVP needs |

