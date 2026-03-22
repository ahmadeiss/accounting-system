# Database Schema

**Database:** PostgreSQL 15+
**ORM:** Entity Framework Core 8 with Fluent API configuration
**Naming:** snake_case for tables and columns (PostgreSQL convention)

---

## Schema Diagram (Entity Relationships)

```
users ──────────────────────────── roles
  │                                  │
  │                            role_permissions
  │                                  │
  │                            permissions
  │
branches ──── warehouses
  │                │
  │                └──── stock_balances ──── items ──── categories
  │                │                          │          units
  │                │                          │
  │                └──── item_batches ◄───────┘
  │                │         │
purchase_invoices ─┘    stock_movements
  │
purchase_invoice_lines ─── items
  │                          │
sales_invoices ────────── sales_invoice_lines
  │                          │
customers              item_batches
  │
audit_logs
import_jobs ──── import_job_rows
alerts
```

---

## Core Tables

### `users`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| username | varchar(100) | unique |
| email | varchar(200) | unique |
| password_hash | varchar(500) | bcrypt |
| first_name | varchar(100) | |
| last_name | varchar(100) | |
| role_id | uuid FK | → roles |
| branch_id | uuid FK? | null = access to all branches |
| is_active | bool | |
| last_login_at | timestamptz? | |
| created_at | timestamptz | |
| updated_at | timestamptz | |

### `roles`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(100) | unique (Admin, Manager, Cashier…) |
| description | varchar(500) | |
| is_system_role | bool | system roles cannot be deleted |
| created_at | timestamptz | |

### `permissions`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(200) | unique (items.create, sales.view…) |
| module | varchar(100) | |
| action | varchar(100) | |
| description | varchar(500) | |

### `role_permissions`
| Column | Type | Notes |
|---|---|---|
| role_id | uuid FK | composite PK |
| permission_id | uuid FK | composite PK |

### `branches`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(200) | |
| code | varchar(50) | unique |
| address | varchar(500) | |
| phone | varchar(50) | |
| is_active | bool | |
| created_at | timestamptz | |
| updated_at | timestamptz | |

### `warehouses`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(200) | |
| code | varchar(50) | unique |
| branch_id | uuid FK | → branches |
| is_default | bool | one default per branch |
| is_active | bool | |
| created_at | timestamptz | |

### `categories`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(200) | |
| parent_category_id | uuid FK? | self-referencing |
| is_active | bool | |

### `units`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(100) | unique |
| abbreviation | varchar(20) | |
| is_active | bool | |

### `items`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(300) | |
| sku | varchar(100) | unique |
| barcode | varchar(100) | unique nullable |
| category_id | uuid FK | → categories |
| unit_id | uuid FK | → units |
| description | text? | |
| cost_price | decimal(18,4) | last known cost |
| sale_price | decimal(18,4) | default sale price |
| reorder_level | decimal(18,4) | threshold for low-stock alert |
| track_batch | bool | batch number required on receiving |
| track_expiry | bool | expiry date required on receiving |
| min_expiry_days_before_sale | int | default 0 |
| is_active | bool | |
| image_url | varchar(500)? | |
| created_at | timestamptz | |
| updated_at | timestamptz | |

### `item_batches`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| item_id | uuid FK | → items |
| warehouse_id | uuid FK | → warehouses |
| batch_number | varchar(100) | |
| production_date | date? | |
| expiry_date | date? | required if item.track_expiry |
| received_quantity | decimal(18,4) | immutable after creation |
| available_quantity | decimal(18,4) | decremented on sale/adjustment |
| cost_per_unit | decimal(18,4) | cost at time of receiving |
| status | varchar(50) | Active, Expired, Depleted, Recalled |
| notes | text? | |
| created_at | timestamptz | |
| updated_at | timestamptz | |
| INDEX | (item_id, warehouse_id, expiry_date) | for FEFO queries |

### `suppliers`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(300) | |
| code | varchar(100) | unique |
| contact_person | varchar(200)? | |
| phone | varchar(50)? | |
| email | varchar(200)? | |
| address | text? | |
| tax_number | varchar(100)? | |
| lead_time_days | int | for future AI forecasting |
| is_active | bool | |
| notes | text? | |
| created_at | timestamptz | |

### `customers`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| name | varchar(300) | |
| phone | varchar(50)? | |
| email | varchar(200)? | |
| address | text? | |
| is_active | bool | |
| created_at | timestamptz | |

### `purchase_invoices`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| invoice_number | varchar(100) | unique, system-generated |
| supplier_id | uuid FK | → suppliers |
| branch_id | uuid FK | → branches |
| warehouse_id | uuid FK | → warehouses |
| invoice_date | date | |
| due_date | date? | |
| status | varchar(50) | Draft, Confirmed, Cancelled |
| sub_total | decimal(18,4) | |
| tax_amount | decimal(18,4) | |
| discount_amount | decimal(18,4) | |
| total_amount | decimal(18,4) | |
| paid_amount | decimal(18,4) | |
| notes | text? | |
| created_by_id | uuid FK | → users |
| created_at | timestamptz | |
| updated_at | timestamptz | |

### `purchase_invoice_lines`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| purchase_invoice_id | uuid FK | → purchase_invoices |
| item_id | uuid FK | → items |
| quantity | decimal(18,4) | |
| unit_cost | decimal(18,4) | |
| discount_percent | decimal(5,2) | |
| tax_percent | decimal(5,2) | |
| line_total | decimal(18,4) | computed |
| batch_number | varchar(100)? | required if item.track_batch |
| production_date | date? | |
| expiry_date | date? | required if item.track_expiry |
| notes | text? | |

### `sales_invoices`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| invoice_number | varchar(100) | unique, system-generated |
| branch_id | uuid FK | → branches |
| warehouse_id | uuid FK | → warehouses |
| customer_id | uuid FK? | → customers (optional) |
| sale_date | timestamptz | |
| status | varchar(50) | Draft, Completed, Voided, Refunded |
| sub_total | decimal(18,4) | |
| tax_amount | decimal(18,4) | |
| discount_amount | decimal(18,4) | |
| total_amount | decimal(18,4) | |
| paid_amount | decimal(18,4) | |
| payment_method | varchar(50) | Cash, Card, Mixed |
| notes | text? | |
| created_by_id | uuid FK | → users |
| created_at | timestamptz | |

### `sales_invoice_lines`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| sales_invoice_id | uuid FK | → sales_invoices |
| item_id | uuid FK | → items |
| item_batch_id | uuid FK? | → item_batches (set for expiry-tracked items) |
| quantity | decimal(18,4) | |
| unit_price | decimal(18,4) | |
| discount_percent | decimal(5,2) | |
| tax_percent | decimal(5,2) | |
| line_total | decimal(18,4) | computed |

### `stock_movements`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| item_id | uuid FK | → items |
| item_batch_id | uuid FK? | → item_batches |
| warehouse_id | uuid FK | → warehouses |
| movement_type | varchar(50) | Purchase, Sale, Adjustment, Transfer, Opening, Return |
| quantity | decimal(18,4) | positive=in, negative=out |
| unit_cost | decimal(18,4) | cost at time of movement |
| reference_type | varchar(100) | PurchaseInvoice, SalesInvoice, StockAdjustment… |
| reference_id | uuid? | FK to referenced document |
| notes | text? | |
| movement_date | timestamptz | |
| created_by_id | uuid FK | → users |
| created_at | timestamptz | |
| INDEX | (item_id, warehouse_id, movement_date) | time-series queries |
| INDEX | (item_batch_id) | batch-level analytics |

### `stock_balances`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| item_id | uuid FK | composite unique with warehouse_id |
| warehouse_id | uuid FK | |
| quantity_on_hand | decimal(18,4) | always in sync with movements |
| last_updated | timestamptz | |
| UNIQUE | (item_id, warehouse_id) | one record per item per warehouse |

### `alerts`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| alert_type | varchar(100) | LowStock, NearExpiry, ExpiredStock, BatchRecalled |
| item_id | uuid FK? | → items |
| item_batch_id | uuid FK? | → item_batches |
| branch_id | uuid FK? | → branches |
| warehouse_id | uuid FK? | → warehouses |
| message | text | human-readable description |
| severity | varchar(50) | Info, Warning, Critical |
| is_read | bool | |
| is_resolved | bool | |
| created_at | timestamptz | |
| resolved_at | timestamptz? | |
| resolved_by_id | uuid FK? | → users |

### `audit_logs`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| entity_name | varchar(200) | |
| entity_id | varchar(100) | |
| action | varchar(100) | Create, Update, Delete, Login, Logout, Adjust |
| old_values | jsonb? | previous state |
| new_values | jsonb? | new state |
| user_id | uuid FK? | → users |
| ip_address | varchar(50)? | |
| timestamp | timestamptz | |
| additional_data | jsonb? | contextual info |

### `import_jobs`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| job_type | varchar(100) | ItemImport, OpeningStockImport, SupplierImport |
| file_name | varchar(500) | |
| status | varchar(50) | Pending, Processing, Completed, Failed, PartialSuccess |
| total_rows | int | |
| processed_rows | int | |
| success_rows | int | |
| error_rows | int | |
| error_summary | text? | |
| created_by_id | uuid FK | → users |
| created_at | timestamptz | |
| completed_at | timestamptz? | |

### `import_job_rows`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| import_job_id | uuid FK | → import_jobs |
| row_number | int | Excel row number for user reference |
| row_data | jsonb | parsed row as JSON |
| status | varchar(50) | Success, Failed, Skipped |
| error_message | text? | human-readable validation error |
| created_at | timestamptz | |

---

## Indexes Summary
- `items.sku` — unique
- `items.barcode` — unique nullable
- `item_batches.(item_id, warehouse_id, expiry_date)` — FEFO queries
- `stock_movements.(item_id, warehouse_id, movement_date)` — time-series
- `stock_balances.(item_id, warehouse_id)` — unique constraint
- `audit_logs.(entity_name, entity_id)` — lookup by entity
- `audit_logs.timestamp` — time-range queries

---

## AI-Ready Data Notes
The following tables provide the foundation for future AI modules:
- `stock_movements` with `movement_date` → time-series training data
- `item_batches.expiry_date` + `available_quantity` → waste risk signals
- `suppliers.lead_time_days` → procurement forecasting
- `purchase_invoice_lines` per supplier per item → lead time actuals
- `sales_invoice_lines` with date → demand history
- `alerts` history → anomaly patterns


