# Product Vision

## Overview
This system is a production-grade **Accounting, Inventory, POS, and Retail Management Platform** designed
primarily for large malls in Palestine, with full support for small and mid-size retail shops.

It is not a demo. It is a real, scalable, commercially-deployed product.

---

## Core Mission
Enable retail businesses of all sizes to:
- Track every item, batch, and expiry date with full auditability
- Manage multi-branch operations from a single platform
- Run POS efficiently with barcode scanning and automatic FEFO batch selection
- Receive, move, and adjust stock with complete traceability
- Migrate from Excel-based legacy systems with structured, validated imports
- Gain real-time operational insight through dashboards and alerts
- Lay the foundation for AI-driven forecasting and anomaly detection

---

## Target Segments

### Segment 1: Large Malls
- Multiple branches and warehouses
- Many concurrent users (staff, cashiers, warehouse managers, accountants)
- High SKU count (10,000+)
- Critical need for batch/expiry tracking (pharmacy, food, cosmetics)
- Need Excel migration from existing ERP/spreadsheet systems
- Need audit trails for compliance and dispute resolution

### Segment 2: Small Retail Shops
- Single branch, single warehouse
- Simpler workflow with fewer required fields
- Manual item entry (no bulk import required initially)
- Easy POS with basic sales flow
- Low technical complexity on day one

---

## Strategic Pillars

### 1. Auditability First
Every stock movement, invoice, adjustment, and login is recorded with full context.
No silent changes. No shortcuts. Every mutation has a traceable author, timestamp, and reason.

### 2. Batch and Expiry Integrity
Items that track expiry must store batch data on receiving.
Expired batches are blocked from sale at the service layer, not just the UI.
FEFO is enforced programmatically.

### 3. Excel Migration as a First-Class Feature
Existing businesses carry years of data in Excel.
The import system supports validated, row-level error-reporting imports for items, stock, and suppliers.
Bad rows never silently pass. Import jobs are durable and reportable.

### 4. AI-Ready Data Architecture
The schema is designed to capture the signals AI models need:
- Time-series sales by item, branch, and batch
- Supplier lead times
- Near-expiry waste events
- Reorder patterns and stock turnover
AI modules will be added in a future phase. The data model will not need structural changes.

### 5. Dual-Mode Operation
Advanced features (batch tracking, multi-warehouse, import) are available but not forced on small shops.
Configuration per branch/item controls complexity exposure.

---

## What This System Is NOT
- Not a generic ERP
- Not a microservices mesh
- Not an over-engineered demo with stubs
- Not a system that bypasses business rules for convenience
- Not a frontend-only tool with no real backend logic

---

## Success Criteria for MVP
- A mall can receive stock, scan and sell items, and view alerts within one day of onboarding
- Expired items cannot be sold under any path through the system
- Every stock change has a movement record with a reference
- Import of 10,000 items from Excel completes with full error reporting
- System runs cleanly in Docker on-premise or cloud

