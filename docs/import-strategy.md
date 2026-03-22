# Excel Import Strategy

## Overview
Excel import is a **core adoption feature**, not a utility.

Most target businesses (malls and shops in Palestine) manage their inventory, item catalog,
and supplier lists in Excel or legacy ERP exports. The system must accept these files and
migrate them cleanly, with full validation and traceable error reporting.

---

## Import Types (MVP)

| Import Type | Description | Template Required |
|---|---|---|
| Item Master Import | Bulk create items from Excel | Yes |
| Opening Stock Import | Initial stock by warehouse and batch | Yes |
| Supplier Import | Bulk create suppliers | Yes (structure-ready) |
| Customer Import | Bulk create customers | Post-MVP |

---

## Design Principles

### 1. Validate Before Commit
No data is written to the database until the entire file has been validated.
If row 1 is valid and row 500 is invalid, **nothing** is inserted until all errors are fixed.
Exception: large files (>5000 rows) may use batched commit with explicit error reporting.

### 2. Row-Level Error Reporting
Every error is tied to an Excel row number.
The error message must be human-readable, not a stack trace.
Example: `Row 14: SKU "ITEM-001" already exists in the system.`

### 3. Durable Import Jobs
Each import attempt creates an `ImportJob` record.
Row-level results are stored in `ImportJobRows`.
Users can review the import result even hours after completion.

### 4. Template-Driven
Downloadable Excel templates are provided for each import type.
The system validates column presence and order.
Extra columns are ignored. Missing required columns = immediate rejection.

### 5. Duplicate Detection
Before inserting, the system checks for existing records with the same:
- SKU (items)
- Barcode (items, if provided)
- Supplier code (suppliers)
The user can choose: Skip duplicates / Update / Fail on duplicate.

---

## Item Import Template

| Column | Required | Notes |
|---|---|---|
| Name | Yes | |
| SKU | Yes | Must be unique |
| Barcode | No | Must be unique if provided |
| Category | Yes | Exact match or auto-create |
| Unit | Yes | Exact match required |
| Cost Price | No | Defaults to 0 |
| Sale Price | No | Defaults to 0 |
| Reorder Level | No | Defaults to 0 |
| Track Batch | No | Yes/No, defaults to No |
| Track Expiry | No | Yes/No, defaults to No |
| Min Expiry Days | No | Defaults to 0 |
| Description | No | |
| Active | No | Yes/No, defaults to Yes |

---

## Opening Stock Import Template

| Column | Required | Notes |
|---|---|---|
| SKU | Yes | Must exist in items |
| Warehouse Code | Yes | Must exist in warehouses |
| Batch Number | Conditional | Required if item tracks batch |
| Expiry Date | Conditional | Required if item tracks expiry |
| Production Date | No | |
| Quantity | Yes | Must be > 0 |
| Cost Per Unit | No | Defaults to item cost price |

---

## Import Processing Flow

```
User uploads Excel file
        │
        ▼
API receives file → creates ImportJob (Pending)
        │
        ▼
Background job picks up → validates all rows
        │
        ├── Errors found → store errors in ImportJobRows
        │                   set status = Failed / PartialSuccess
        │                   return error report
        │
        └── All valid → begin DB transaction
                          insert all records
                          create stock movements for opening stock
                          commit transaction
                          set status = Completed
                          return success summary
```

---

## Error Handling Examples

| Error | Message to User |
|---|---|
| Missing required column | `Column "SKU" is missing from the uploaded file.` |
| Duplicate SKU | `Row 23: SKU "ITEM-099" already exists.` |
| Unknown category | `Row 45: Category "Beverages > Juices" not found. Create it first or check spelling.` |
| Invalid date format | `Row 67: Expiry Date "31/13/2024" is not a valid date.` |
| Expiry required | `Row 88: Item "Milk 1L" tracks expiry but no Expiry Date was provided.` |
| Negative quantity | `Row 102: Quantity must be greater than zero.` |
| Unknown warehouse | `Row 120: Warehouse "WH-NORTH" does not exist in the system.` |

---

## Implementation Notes

### Excel Parsing
- Library: **EPPlus** (free for non-commercial with license, commercial license available)
- Format: `.xlsx` only (Excel 2007+)
- Max file size: 10MB default (configurable)
- Max rows: 50,000 (can be increased for enterprise tier)

### Background Processing
- Import parsing runs as a **Hangfire background job**
- User gets immediate response: `{ "importJobId": "...", "status": "Pending" }`
- Client polls `GET /api/import/jobs/{id}` for status
- WebSocket or SignalR push can be added in Phase 2

### Template Download
- Templates served as static `.xlsx` files from API
- `GET /api/import/templates/{type}` returns the template file

---

## Validation Rules Per Import Type

### Item Import Validation
1. Name not empty (max 300 chars)
2. SKU not empty, not duplicate in file or DB
3. Category name must match existing category (or create if configured)
4. Unit name must match existing unit
5. Cost/Sale prices must be non-negative numbers
6. Track Expiry = Yes → Min Expiry Days must be >= 0

### Opening Stock Validation
1. SKU must exist
2. Warehouse code must exist
3. Batch number required if item.track_batch = true
4. Expiry date required if item.track_expiry = true
5. Expiry date must be a future date (warn if within 30 days)
6. Quantity must be a positive number
7. No duplicate (SKU + Warehouse + BatchNumber) combinations in same import

