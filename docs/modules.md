# System Modules

## Module Map

```
┌─────────────────────────────────────────────────────────────┐
│                     Accounting System                        │
├────────────┬──────────────┬─────────────┬───────────────────┤
│   Auth &   │  Branches &  │  Catalog &  │   Purchasing &    │
│   Users    │  Warehouses  │  Inventory  │   Stock Receive   │
├────────────┼──────────────┼─────────────┼───────────────────┤
│  Sales &   │    Batch &   │   Alerts &  │  Excel Import &   │
│    POS     │   Expiry     │  Dashboard  │    Migration      │
├────────────┴──────────────┴─────────────┴───────────────────┤
│              Audit Log & System Diagnostics                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. Authentication & Authorization
**Scope:** Login, JWT issuance, role-based access control

| Feature | Description |
|---|---|
| Login | Username/password, JWT + refresh token |
| Users | Multi-branch user accounts with role assignment |
| Roles | Named roles (Admin, Manager, Cashier, Warehouse, Accountant) |
| Permissions | Granular per-module permissions stored in DB |
| Branch Scope | Users can be scoped to one or all branches |

---

## 2. Branches
**Scope:** Physical business locations

| Feature | Description |
|---|---|
| Branch management | Create/edit/deactivate branches |
| Branch configuration | Default warehouse, tax settings, POS settings |
| Multi-branch isolation | Data and reports isolated per branch |

---

## 3. Warehouses
**Scope:** Physical stock locations per branch

| Feature | Description |
|---|---|
| Warehouse management | Multiple warehouses per branch |
| Default warehouse | Each branch has a default sales warehouse |
| Stock isolation | Stock balances tracked per warehouse |

---

## 4. Item Catalog
**Scope:** The master product list

| Feature | Description |
|---|---|
| Item master | Name, SKU, barcode, category, unit, pricing |
| Categories | Hierarchical (parent/child) |
| Units | Unit of measure (piece, kg, liter, box) |
| Batch tracking flag | Per item: does this item require batch tracking? |
| Expiry tracking flag | Per item: does this item track expiry? |
| Reorder level | Minimum stock threshold for alerts |
| Active/inactive | Soft deactivation |
| Min expiry days | Minimum days before expiry allowed for sale |

---

## 5. Batch & Expiry Tracking
**Scope:** Granular lot-level inventory control

| Feature | Description |
|---|---|
| Batch creation | On purchase receiving for tracked items |
| Batch number | Supplier-assigned or system-generated |
| Production date | Optional, captured on receiving |
| Expiry date | Required for expiry-tracked items |
| Available quantity | Decremented on sales, adjusted on stock ops |
| Batch status | Active, Expired, Depleted, Recalled |
| FEFO enforcement | Earliest-expiry batch always consumed first |
| Expired block | Expired batches blocked from sale at service level |

---

## 6. Purchasing
**Scope:** Supplier management and stock receiving

| Feature | Description |
|---|---|
| Suppliers | Supplier master with contact and tax info |
| Purchase invoices | Header with supplier, branch, warehouse, dates |
| Purchase lines | Item, quantity, cost, batch/expiry data |
| Stock receiving | On invoice confirmation, stock movements are created |
| Batch creation | Automatically created per line for tracked items |

---

## 7. Sales / POS
**Scope:** Point-of-sale and sales invoicing

| Feature | Description |
|---|---|
| Sales invoices | Header with branch, warehouse, customer, payment |
| Sales lines | Item lookup by barcode or SKU |
| FEFO batch selection | Auto-selects earliest non-expired batch |
| Expired item block | Cannot add expired batch to sale |
| Payment methods | Cash, card, mixed (extensible) |
| Customer | Optional customer capture |

---

## 8. Stock Management
**Scope:** Movements, balances, adjustments

| Feature | Description |
|---|---|
| Stock movements ledger | Every in/out recorded with reference and type |
| Movement types | Purchase, Sale, Adjustment, Transfer, Opening |
| Stock balance | Materialized per item/warehouse for performance |
| Stock adjustments | Explicit reason-coded adjustments with audit |
| No silent edits | Quantity cannot be changed without a movement |

---

## 9. Alerts
**Scope:** Operational warnings and notifications

| Feature | Description |
|---|---|
| Low stock alert | When available quantity falls below reorder level |
| Near-expiry alert | Configurable days-before-expiry threshold |
| Expired stock alert | Batches that have passed expiry date |
| Alert severity | Info, Warning, Critical |
| Alert resolution | Mark as read/resolved with timestamp |

---

## 10. Dashboard
**Scope:** Operational summary view

| Feature | Description |
|---|---|
| Active alerts count | By type and severity |
| Recent sales summary | Today/week totals |
| Stock insights | Low stock items count |
| Recent transactions | Latest purchases and sales |

---

## 11. Excel Import / Migration
**Scope:** Legacy data migration and bulk loading

| Feature | Description |
|---|---|
| Item import | Bulk item master from Excel template |
| Opening stock import | Initial stock by warehouse and batch |
| Validation before commit | Row-level validation with error collection |
| Import job tracking | Durable job record with status and row errors |
| Error reporting | Human-readable per-row error messages |
| Duplicate detection | SKU/barcode duplicate checks before insert |

---

## 12. Audit Log
**Scope:** Immutable record of all important system actions

| Feature | Description |
|---|---|
| Entity change tracking | Old/new values for critical entities |
| Action logging | Create, Update, Delete, Login, Adjustment |
| User attribution | Who made the change |
| Timestamp | When the change occurred |
| IP tracking | Remote IP address capture |

---

## Future Modules (Not MVP)
- Warehouse Transfers
- Accounting / GL integration
- Customer Loyalty
- Reporting Engine (PDF/Excel export)
- Mobile App (manager view)
- AI Forecasting & Anomaly Detection
- Supplier Portal
- Multi-currency

