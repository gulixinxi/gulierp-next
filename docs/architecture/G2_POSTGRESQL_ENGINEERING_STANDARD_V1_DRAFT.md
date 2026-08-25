# G2 — PostgreSQL Engineering Standard V1 Draft

| Field | Value |
|---|---|
| Goal | G2 — PostgreSQL engineering standard: naming, schema, PK, FK, constraints, indexes, transactions, audit |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| Author | Mavis (single writer, database role) |
| Companion docs | `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` (TASK B), `G2_MODULE_RUNTIME_ARCHITECTURE_V1_DRAFT.md` (TASK D) |
| DEV anti-patterns explicitly banned | 0 FK, text-name relation, dynamic SQL, weak CHECK constraints, missing indexes on FKs, soft-delete without index |
| Future-MainDB candidates | (none V1) |
| SQL Server position | Future Migration Connector / Legacy Import Source only — not a primary DB |

---

## 1. Mission

Define a **complete, enforceable, opinionated** PostgreSQL engineering standard for GuliERP. The standard must be:

1. **Stronger** than DEV's actual practice (which had 0 FK and text-name relations).
2. **Compatible** with the G2 Foundation contract (multi-tenant row-level isolation, EF Core + Npgsql, modular migrations).
3. **Reviewable** by a human in one sitting.
4. **Testable** by a CI rule (e.g. linter, schema diff, custom check).

This document is the single source of truth for the database layer. Any deviation requires a written exception in this file with rationale.

---

## 2. PostgreSQL version + extensions

| Component | Version | Why |
|---|---|---|
| PostgreSQL | **16.x** (min 15) | `gen_random_uuid()`, improved partitioning, `MERGE` (V1.5), better `tsvector` |
| Extensions enabled per database | `pgcrypto` (UUID), `citext` (case-insensitive text), `ltree` (organization path), `pg_trgm` (LIKE acceleration), `btree_gin`/`btree_gist` (composite indexes) | None of these are "experimental"; all are bundled |
| Connection driver | **Npgsql 8.x** | EF Core 10 + Npgsql 8 is the .NET 10 baseline |
| Connection pool | `Maximum Pool Size = 100` (default), `Minimum Pool Size = 5` | Tuned per env in `appsettings.{Env}.json` |
| Statement timeout | `SET statement_timeout = '30s'` per connection (configurable per role) | Protects against runaway queries |
| Idle transaction timeout | `SET idle_in_transaction_session_timeout = '60s'` | Prevents stuck tx |

**Rationale for no exotic extensions** (e.g. Citus, TimescaleDB, PostgREST, pgvector): out of V1. V1 is plain PostgreSQL.

---

## 3. Naming conventions (hard rule)

### 3.1 Identifiers

| Object | Convention | Example | Notes |
|---|---|---|---|
| Database | `gulierp` (single DB V1) | — | Multi-DB V1.5+ |
| Schema | `public` V1 (one schema, multi-tenant by row) | — | Per-tenant schema V1.5+ |
| Table | `snake_case`, **plural** noun | `sales_orders`, `sales_order_lines`, `users` | |
| Column | `snake_case` | `tenant_id`, `created_at` | |
| Primary key column | `id` (BIGINT) or `{table_singular}_id` (BIGINT) | `id` for tables that "are" one entity; `{name}_id` for join tables | We use `id` consistently — see §6.1 |
| Foreign key column | `{referenced_table_singular}_id` | `customer_id`, `item_id` | The referenced table is singularized; this is the **DEV opposite** |
| Index | `idx_{table}_{col1}_{col2...}` | `idx_sales_orders_tenant_company_status` | Always prefixed `idx_` |
| Unique constraint | `uq_{table}_{col1}_{col2...}` | `uq_sales_orders_tenant_order_no` | |
| Check constraint | `ck_{table}_{description}` | `ck_sales_order_lines_qty_positive` | |
| Trigger | `trg_{table}_{action}_{when}` | `trg_sales_orders_set_updated_at_before_update` | |
| Function | `fn_{verb}_{noun}` | `fn_audit_write` | |
| View | `v_{purpose}` | `v_sales_order_open_balance` | |
| Materialized view | `mv_{purpose}` | `mv_inventory_monthly_turnover` | |
| Sequence | `seq_{table}_{col}` (or use `IDENTITY`) | `seq_sales_orders_id` | Prefer `IDENTITY` (see §6.1) |
| Enum (PG type) | `{domain}_{name}` (lowercase) | `sales_order_status`, `inventory_movement_type` | |
| Role | `gulierp_app`, `gulierp_migrator`, `gulierp_readonly` | — | See §11 |

### 3.2 Identifiers — case and length

- All identifiers are **lowercase snake_case**. No CamelCase, no PascalCase, no kebab-case.
- Maximum length 63 (PostgreSQL hard limit on identifier length). The longest identifier in V1 is well under 50.
- Reserved keywords (e.g. `user`, `order`, `table`, `column`) are **never** used as identifiers. `user` → `users` (table) but `username` (column).

### 3.3 Migration file naming

`{timestamp}_{module}_{version}_{description}.cs`

Example: `20260819001_sales_1.0.0_initial.cs`

This is enforced by a CI check on the file name format.

---

## 4. Schema strategy

V1 uses **one schema** (`public`) with **row-level multi-tenancy**. Tables are NOT in per-tenant schemas. This is a deliberate choice for V1 simplicity.

- V1.5 introduces per-tenant schemas (one schema per tenant) for high-value customers who need physical isolation. The migration tool is the same; the schema is dynamic.
- V1.5 also introduces per-module schemas (e.g. `sales.*`, `inventory.*`) for cleanliness. V1 does not.

**Why one schema V1**: simpler migrations, simpler cross-module queries (when absolutely needed via Application.Contracts), simpler backup, simpler tooling. The cost is the risk of cross-tenant data leak, which is mitigated by the **mandatory** `tenant_id` column and the **mandatory** EF Core global query filter (see §10).

---

## 5. Data types

### 5.1 Primitive type mapping (C# ↔ PostgreSQL)

| C# | PostgreSQL | Why |
|---|---|---|
| `string` (≤ 1KB) | `text` (no length limit) | The length is enforced at the application layer (validation) |
| `string` (codes, ≤ 40 char) | `varchar(40)` | Hard cap, indexed efficiently |
| `string` (names, ≤ 200 char) | `varchar(200)` | Hard cap |
| `string` (descriptions, free) | `text` | No limit |
| `int` | `integer` | |
| `long` | `bigint` | PKs are BIGINT (snowflake) |
| `decimal` (money) | `numeric(20, 4)` | 4 decimal places is the ERP convention (sub-cent rounding) |
| `decimal` (qty) | `numeric(20, 4)` | Same |
| `decimal` (tax rate) | `numeric(8, 6)` | Up to 999.999999 |
| `bool` | `boolean` | |
| `DateTime` (UTC) | `timestamptz` | **Always** UTC, **never** `timestamp` |
| `DateOnly` | `date` | For business dates (e.g. `DocumentDate`) |
| `TimeOnly` | `time` | Rare; only if a business field needs wall-clock without date |
| `Guid` | `uuid` | Not for PKs (see §6.1); for `id` columns of system tables (e.g. `RefreshToken.Id`) |
| `byte[]` | `bytea` | For binary blobs; usually `lo` (large object) for files |
| `enum` (C#) | Custom enum type | See §5.2 |
| `List<T>` / dictionary | `jsonb` | For flexible attributes; **never** for core data |
| Money amount | `numeric(20, 4)` + currency code column | See §5.3 |
| `ltree` (org path) | `ltree` | See §6.4 |
| Free text search | `tsvector` (column generated from `text`) | For future search |

### 5.2 Enums (PG custom type, mapped to C# enum)

```sql
CREATE TYPE sales_order_status AS ENUM (
    'Draft', 'Confirmed', 'Cancelled', 'Closed'
);

CREATE TYPE sales_order_approval_status AS ENUM (
    'NotRequired', 'Pending', 'Approved', 'Rejected', 'Withdrawn'
);

CREATE TYPE sales_order_execution_status AS ENUM (
    'Open', 'Partial', 'Fulfilled', 'Closed'
);
```

**Why custom types, not lookup tables**: enums are part of the contract (the DB will reject an invalid value at insert time), and they make the code self-documenting. A lookup table is more flexible but adds join cost and a `code`/`name` translation layer for every read. We use lookup tables for **business**-driven enums (e.g. `Dictionary.tax_rate`) but **PG enum types** for **system**-driven enums (e.g. `DocumentStatus`).

**Naming**: `{table_singular}_{column}` (e.g. `sales_order_status`). C# enum: `SalesOrderStatus` (PascalCase, matching the column 1:1). The convention is enforced by a code generator that produces both the PG type and the C# enum from one source of truth.

### 5.3 Money

- **Always** `numeric(20, 4)` for amounts. Never `real`, never `double`, never `money` (the PG `money` type has currency-implicit semantics that are wrong for multi-currency ERP).
- Amounts are stored as **unscaled** (the value is in the currency's minor unit, e.g. cents). 4 decimal places handles most currencies (CNY has 2 minor digits; some currencies have 3; 4 is a safe superset for the foreseeable future).
- Every amount column has a companion `currency_code char(3)` column. Multi-currency requires per-row currency, not per-table currency.
- **Rounding**: HALF_EVEN (banker's rounding) per the `numeric` default. Application code does not pre-round.
- All arithmetic in SQL goes through `numeric` operators; do not cast to `real` or `double` mid-calculation.

### 5.4 Timestamps

- Every business table has:
  - `created_at timestamptz NOT NULL DEFAULT now()`
  - `updated_at timestamptz NOT NULL DEFAULT now()` (maintained by trigger; see §12.2)
  - `created_by bigint NOT NULL` (FK to `users.id`, ON DELETE RESTRICT)
  - `updated_by bigint NOT NULL` (FK to `users.id`, ON DELETE RESTRICT)
- These are **immutable audit metadata**, separate from the `audit_entry` table (which is the action log).
- The application **never** writes to these directly; they are set by the EF Core `SaveChanges` interceptor and the `updated_at` trigger.

### 5.5 Soft delete

V1 does **not** use soft delete. Hard delete + audit trail.

- Why: a `WHERE deleted = false` on every query is a footgun; EF Core's global filter is a workaround, not a cure.
- If a future V1.5 introduces soft delete, it is a **per-table opt-in** with explicit `is_deleted` column and an explicit index.

---

## 6. Primary keys

### 6.1 Snowflake BIGINT vs UUID vs IDENTITY

| Strategy | Pros | Cons | GuliERP choice |
|---|---|---|---|
| **Snowflake BIGINT** (Twitter-style, 64-bit, time-ordered) | Compact index (8 bytes vs 16), time-ordered = better B-tree locality, easy to sort by creation, low collision risk | Needs a snowflake generator service; not human-readable | **Chosen for all business tables** |
| UUID v4 | Globally unique, no central generator, no info leak | 16-byte index, random = poor B-tree locality, not time-ordered | **Chosen only for system rows that benefit from being unpredictable** (e.g. `refresh_token.id`, `idempotency_key.key`) |
| UUID v7 (time-ordered) | Combines both | Requires PG 17 or app-side generation | V1.5 |
| `IDENTITY` (SERIAL) | Simple | Sequential, leaks business volume, hard to migrate, multi-instance issues | **Not used** for any business table |
| `text` (e.g. ULID) | Human-readable | String PK = bad index locality | **Not used** |

**Snowflake spec for GuliERP**:
- 64-bit integer
- Bits 63..22 = timestamp (ms since custom epoch `2024-01-01T00:00:00Z`)
- Bits 21..12 = machine id (10 bits, max 1024 machines)
- Bits 11..0 = sequence (12 bits, max 4096 per ms per machine)
- Generator service: `ISnowflakeGenerator` in `GuliERP.Foundation.Infrastructure`, injectable
- The snowflake value is the PK; the table has an `id` column of type `bigint NOT NULL PRIMARY KEY`
- The `created_at` is **derived** from the snowflake, not stored separately (saves space; saves drift). The `created_at` column is still present for clarity and for tables that may receive rows with manually-set snowflakes (e.g. import).

```sql
CREATE TABLE sales_orders (
    id              bigint PRIMARY KEY,             -- snowflake
    tenant_id       bigint NOT NULL,
    company_id      bigint NOT NULL,
    ...
    created_at      timestamptz NOT NULL,
    updated_at      timestamptz NOT NULL
);
```

### 6.2 Composite natural keys (rarely)

V1 forbids composite primary keys. Every table has a single `id bigint PRIMARY KEY`. Natural keys (e.g. `OrderNo`) are enforced via `UNIQUE` constraints, not as the PK.

### 6.3 Code fields (business numbers)

Codes like `OrderNo`, `ItemCode`, `CustomerCode` are **not** the PK. They are `varchar(40)` with `UNIQUE (tenant_id, code)` constraints. They are user-facing and may be edited in some business scenarios (e.g. "rename an item"); PKs are immutable.

---

## 7. Foreign keys (the DEV opposite)

DEV had **0 foreign keys**. GuliERP makes FKs **mandatory** for every reference.

```sql
CREATE TABLE sales_order_lines (
    id              bigint PRIMARY KEY,
    tenant_id       bigint NOT NULL,
    company_id      bigint NOT NULL,
    sales_order_id  bigint NOT NULL
        REFERENCES sales_orders (id) ON DELETE RESTRICT ON UPDATE RESTRICT,
    item_id         bigint NOT NULL
        REFERENCES items (id) ON DELETE RESTRICT ON UPDATE RESTRICT,
    uom_id          bigint NOT NULL
        REFERENCES units_of_measure (id) ON DELETE RESTRICT ON UPDATE RESTRICT,
    warehouse_id    bigint NOT NULL
        REFERENCES warehouses (id) ON DELETE RESTRICT ON UPDATE RESTRICT,
    ...
);
```

**Referential action policy**:
- `ON DELETE RESTRICT` is the **default**. You cannot delete a parent if a child exists. The application must soft-archive or reassign.
- `ON DELETE CASCADE` is **forbidden** except in 2 cases: (a) line tables where the parent doc is deleted, (b) join tables (e.g. `user_role`). The architecture test fails any other `CASCADE`.
- `ON DELETE SET NULL` is **forbidden**. Nulling a FK is a hidden data corruption.
- `ON UPDATE RESTRICT` is the **default** (we never change a PK).

**Migration safety**: adding an FK on a large table is expensive. The migration tool validates the data first (`NOT VALID` constraint), then `VALIDATE CONSTRAINT` in a separate step, so the lock is short.

### 7.1 Composite FKs (for tenant + company isolation)

Every business table has `tenant_id` and `company_id`. The FK to `tenants(id)` and `companies(id)` is **mandatory**:

```sql
tenant_id   bigint NOT NULL REFERENCES tenants (id) ON DELETE RESTRICT,
company_id  bigint NOT NULL REFERENCES companies (id) ON DELETE RESTRICT,
```

**Tenant must be unique per row**: a row's `company_id` must belong to the row's `tenant_id`. This is enforced by a `CHECK` constraint plus a composite index (see §9.2). Pure SQL cannot express "company belongs to tenant" without a trigger, so we use:

1. Composite FK `(tenant_id, company_id) REFERENCES companies (tenant_id, id)` — the FK includes tenant in the referenced columns. This is a **composite foreign key** that PostgreSQL supports.
2. The `companies` table has `UNIQUE (tenant_id, id)` (which is automatic since `id` is the PK, but we need a `UNIQUE (tenant_id, id)` to be referenceable). The PK is on `id` alone; the UNIQUE constraint on `(tenant_id, id)` is a separate index.

This is the **strongest** form of tenant isolation at the DB layer. The architecture test verifies that every business table has this composite FK.

---

## 8. Unique constraints

- Every business-natural key has a `UNIQUE` constraint, e.g. `UNIQUE (tenant_id, order_no)`.
- Compound uniqueness: e.g. `UNIQUE (tenant_id, company_id, customer_code)`.
- Partial unique: e.g. `UNIQUE (tenant_id, default_customer_id) WHERE is_default = true` — this is a **partial unique index** in PostgreSQL.
- Case-insensitive: when appropriate, use `citext` or normalize in application code. The DB does not silently case-fold.

---

## 9. Check constraints

### 9.1 Hard rules (the DEV opposite)

Every business table has CHECK constraints that the DEV project lacked. Examples:

```sql
-- sales_orders
CONSTRAINT ck_sales_orders_total_nonneg    CHECK (total_amount >= 0),
CONSTRAINT ck_sales_orders_lines_count     CHECK (line_count > 0),
CONSTRAINT ck_sales_orders_company_tenant  CHECK (company_id <> tenant_id OR true), -- placeholder; real check is composite FK

-- sales_order_lines
CONSTRAINT ck_sol_quantity_positive        CHECK (quantity > 0),
CONSTRAINT ck_sol_unit_price_nonneg        CHECK (unit_price >= 0),
CONSTRAINT ck_sol_amount_nonneg            CHECK (amount >= 0),
CONSTRAINT ck_sol_tax_rate_valid           CHECK (tax_rate >= 0 AND tax_rate <= 1),

-- users
CONSTRAINT ck_users_username_format        CHECK (username ~ '^[a-z][a-z0-9._-]{2,39}$'),
CONSTRAINT ck_users_email_format           CHECK (email ~ '^[^@]+@[^@]+\.[^@]+$'),
```

**Naming**: `ck_{table}_{description}`.

The architecture test fails any business table that lacks expected CHECKs (e.g. an amount column with no `>= 0` check).

### 9.2 Cross-row invariants

Some invariants cannot be expressed in CHECK (because they reference other tables). These go in **triggers**:

- "Sum of line amounts equals header total" → trigger on `sales_order_lines` AFTER INSERT/UPDATE/DELETE
- "A user's `default_company` must be in the user's `user_company` set" → trigger on `users`
- "Closing a SalesOrder requires all lines to be in Fulfilled state" → trigger on `sales_orders`

Triggers are written in **PL/pgSQL** (no external language). They raise `EXCEPTION` on violation; the application catches and translates to a domain error.

**Why not enforce all of this in application code only**: a single bug (e.g. a direct `psql` session during ops) would corrupt data. DB-level invariants are the **last line of defense**. Application code is the **first** line. The architecture test fails any code path that does not also rely on the DB constraint.

---

## 10. Indexes

### 10.1 What gets an index (mandatory)

| Pattern | Index |
|---|---|
| All FKs (including composite `tenant_id`) | `CREATE INDEX ON child (parent_id);` |
| `tenant_id` alone on every business table | `CREATE INDEX ON table (tenant_id);` (covered by the FK index, but explicit for clarity) |
| `(tenant_id, company_id, status)` on every doc table | Standard ERP index pattern |
| `(tenant_id, code)` on every dictionary-like table | Code lookup |
| `(tenant_id, created_at DESC)` on every list page | Time-ordered list |
| `(tenant_id, customer_id, document_date)` on sales | Per-customer history |
| `GIN (search_tsv)` on searchable text | Full-text search |
| `UNIQUE` constraint implies an index | Automatic |

### 10.2 What does NOT get an index (forbidden by review)

- Indexes on low-cardinality columns (e.g. `is_active boolean` alone).
- Indexes on `text` columns without `pg_trgm` or `tsvector` (they are useless for normal `=`).
- Redundant indexes (e.g. `(a, b)` and `(a)` when the leftmost-prefix covers the second).

### 10.3 Partial indexes

Common pattern: only some rows are "active" and need indexing:

```sql
CREATE INDEX idx_sales_orders_open
    ON sales_orders (tenant_id, customer_id, document_date)
    WHERE status = 'Confirmed';
```

### 10.4 Covering indexes (INCLUDE)

For hot read paths:

```sql
CREATE INDEX idx_sales_orders_listing
    ON sales_orders (tenant_id, company_id, document_date DESC)
    INCLUDE (id, order_no, status, total_amount);
```

### 10.5 EXPLAIN verification

Every code review of a query that runs more than once per second must include the `EXPLAIN (ANALYZE, BUFFERS)` output in the PR. The reviewer verifies:
- No `Seq Scan` on tables > 10k rows.
- No `Sort` on disk (`Sort Method: external merge`).
- No `Nested Loop` with inner > 1000 rows.

---

## 11. Roles and privileges

Three DB roles, with strict separation:

| Role | Purpose | Privileges |
|---|---|---|
| `gulierp_migrator` | Run EF Core migrations on boot | `CREATE`, `ALTER`, `DROP` on public; `OWNER` of all tables; **never used by the application at runtime** |
| `gulierp_app` | The application's normal runtime user | `SELECT`, `INSERT`, `UPDATE`, `DELETE` on all business tables; `SELECT` on `audit_entry`; **no** `DELETE`/`UPDATE` on `audit_entry`; **no** schema modification |
| `gulierp_readonly` | Reporting / BI | `SELECT` only on a curated set of views (not on tables) |

The application's connection string uses `gulierp_app`. The migrator is a separate connection string used **only** at boot, and the connection is closed before the request pipeline starts.

A CI test verifies:
- `gulierp_app` cannot `DROP TABLE` (expect permission denied).
- `gulierp_app` cannot `DELETE FROM audit_entry` (expect permission denied).
- `gulierp_app` cannot `ALTER TABLE` (expect permission denied).

If any of these succeed, the CI fails — a privilege escalation bug.

---

## 12. Triggers

### 12.1 Allowed triggers

- `updated_at` maintenance (`BEFORE UPDATE` → set `NEW.updated_at = now()`).
- Cross-row invariant enforcement (see §9.2).
- Snowflake generation (alternative: do it in the application; default is application-side).

### 12.2 Forbidden triggers

- Triggers that modify `audit_entry` directly — audit is written by the application only.
- Triggers that call external services.
- Triggers with `SECURITY DEFINER` that change privilege scope (a common source of bugs).

### 12.3 The `updated_at` trigger (standard)

```sql
CREATE OR REPLACE FUNCTION fn_set_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_sales_orders_set_updated_at
    BEFORE UPDATE ON sales_orders
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
```

The application also sets `updated_at` via the `SaveChanges` interceptor, **but** the trigger is the safety net for direct `psql` updates during ops.

---

## 13. Transactions and isolation

### 13.1 Default isolation: `READ COMMITTED`

V1 uses the default `READ COMMITTED`. This is **sufficient** for the following reasons:

- Tenant isolation is enforced by the global query filter, not by MVCC.
- Optimistic concurrency is enforced at the application level (snowflake + version), not by `SERIALIZABLE`.
- Single-doc atomicity is achieved by the per-request EF Core transaction (one `SaveChanges` per HTTP request).

`REPEATABLE READ` is used **only** for cross-doc consistency scenarios (e.g. "create SalesOrder and reserve stock in one transaction"). The application code that needs this opens a transaction explicitly.

`SERIALIZABLE` is **forbidden** in V1 (the retry cost is not worth it given the optimistic concurrency design).

### 13.2 Optimistic concurrency

- Every business table has `concurrency_version integer NOT NULL DEFAULT 1`.
- The EF Core entity is configured with `IsConcurrencyToken()`.
- `UPDATE ... WHERE id = @id AND concurrency_version = @expected_version`. If 0 rows affected, the application throws `ConcurrencyConflictException` → HTTP 409 envelope with code `CONCURRENCY_CONFLICT`.
- Every state-change increments the version (`UPDATE ... SET concurrency_version = concurrency_version + 1`).
- The version is included in every HTTP `PUT` / `POST` body for state transitions (the client receives it in the GET response and must echo it back).

### 13.3 Pessimistic locking (forbidden in V1)

`SELECT ... FOR UPDATE` is **forbidden** in V1. The application uses optimistic concurrency. V1.5 may introduce pessimistic locks for specific cross-doc scenarios (e.g. inventory posting).

### 13.4 Long transactions (forbidden)

No transaction may hold locks for more than 5 seconds. The application uses small, fast transactions. The `idle_in_transaction_session_timeout` is 60 seconds as a backstop, but a correctly-written app never approaches this.

---

## 14. Idempotency

- `Idempotency-Key` header on `POST` / `PUT` / `DELETE` (see TASK F §6).
- The `idempotency_key` table stores `(key, request_hash, response_status, response_body, expires_at)`.
- A request with the same key returns the cached response.
- TTL: 24 hours.
- The table is in `public` schema, partitioned by month, with a `pg_cron` job dropping expired partitions (V1.5+; manual in V1).

---

## 15. Audit columns (the application-level audit, separate from `audit_entry`)

Already covered in §5.4. The `audit_entry` table is the **action log**; the `created_at` / `updated_at` / `created_by` / `updated_by` columns are **row metadata**. Both are required.

---

## 16. Migrations

### 16.1 Migration ownership

- Migrations are **per-module**. Each module has its own `Migrations` folder.
- The Foundation owns only the **Foundation's** migrations (users, roles, tenants, etc.).
- A module cannot write a migration that touches another module's table.

### 16.2 Migration files

`{timestamp}_{module}_{version}_{description}.cs` (e.g. `20260819001_sales_1.0.0_initial.cs`).

### 16.3 Migration rules (hard)

1. **Forward-only** by default. A migration that requires a "down" must be split into two: one new migration that undoes the previous one.
2. **Idempotent** where possible. `CREATE INDEX IF NOT EXISTS`, `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`. (But table creation is not idempotent — it errors if the table exists; this is the desired safety.)
3. **Reversible** when destructive (every `DROP COLUMN` has a `down` migration that re-adds it).
4. **Lock-safe**: large operations use `CREATE INDEX CONCURRENTLY`, `ALTER TABLE ... ADD CONSTRAINT ... NOT VALID` then `VALIDATE CONSTRAINT`.
5. **Data-migration aware**: when a column is added with a non-null default, the migration backfills in batches.
6. **No raw SQL injection risk**: parameterized only. The migration code is reviewed like any other code.

### 16.4 Migration order

Migrations apply in **module dependency order**, then by `timestamp` within a module. The Foundation's migrations always run first. The `IModuleRegistry` orchestrates this (see TASK D §3.2).

---

## 17. Schema versioning

Each module records its current schema version in `module_record`:

```sql
CREATE TABLE module_record (
    id                      bigint PRIMARY KEY,
    module_id               varchar(40) NOT NULL,
    version                 varchar(40) NOT NULL,
    last_migration          varchar(255) NOT NULL,
    applied_at              timestamptz NOT NULL DEFAULT now(),
    enabled                 boolean NOT NULL DEFAULT true,
    UNIQUE (module_id, version)
);
```

The `IModuleRegistry.InitializeAsync()` reads this to determine which migrations to apply.

---

## 18. Backup and recovery (operational, not in code)

- Daily logical backup via `pg_dump` (compressed, 30-day retention).
- Weekly full base backup via `pg_basebackup`.
- WAL archiving for PITR.
- The application **must not** delete or modify backup files. Backup is an operational concern.

This is **not** enforced by the application, but the application **must not depend on** a specific backup policy. The application assumes the DB is durable.

---

## 19. Performance budgets

| Operation | Target | How to verify |
|---|---|---|
| Single-row PK lookup | < 5ms p99 | `EXPLAIN` + load test |
| Indexed range query (e.g. sales list) | < 50ms p99 | `EXPLAIN` + load test |
| Document create (with N lines) | < 200ms p99 | Load test |
| Report (10k rows aggregated) | < 2s p99 | Load test |
| Bulk import (10k rows) | < 30s p99 | Load test |
| Migration (full schema) | < 5min on empty DB | CI test |

A failure to meet a budget is a bug. The architecture test logs the budget and the load test result.

---

## 20. Anti-patterns we explicitly reject (the DEV opposite)

| Anti-pattern | Why rejected | GuliERP's stance |
|---|---|---|
| **0 foreign keys** | Data corruption, no referential integrity | **Banned**. Every reference is an FK. |
| **Text-name relations** (e.g. `customer_name text` instead of `customer_id bigint`) | Customer name changes; data drifts; no FK | **Banned**. Always FK + display name in the join query. |
| **Dynamic SQL** (e.g. `EXEC('SELECT * FROM ' + @table)`) | SQL injection; bypasses type system; no caching | **Banned**. Use EF Core. |
| **Weak or missing CHECK constraints** | Garbage data in production | **Banned**. Every numeric field has a CHECK. |
| **Missing indexes on FKs** | Slow joins; slow cascades | **Banned**. Every FK is indexed. |
| **Soft delete with no index** | Slow `WHERE deleted = false` filters | **V1 forbids soft delete**. V1.5 with discipline. |
| **`text` PKs** (e.g. ULID) | Index bloat; bad locality | **Banned**. PKs are `bigint` (snowflake). |
| **`money` type** | Currency-implicit; multi-currency broken | **Banned**. Use `numeric(20, 4)` + `currency_code`. |
| **`timestamp without time zone`** | Time zone ambiguity | **Banned**. Always `timestamptz`. |
| **`real` / `double` for money** | Floating point drift | **Banned**. Always `numeric`. |
| **`ON DELETE CASCADE` for "cleanup"** | Hides data loss; cascade can delete hundreds of rows | **Banned** except for line tables and join tables. |
| **`ON DELETE SET NULL`** | Hidden data corruption | **Banned**. |
| **Triggers that call external services** | DB becomes a distributed system node; not transactional | **Banned**. |
| **`SERIAL` for multi-instance** | Single sequence, single bottleneck | **Banned**. Use snowflake. |
| **Per-row triggers for "all rows of this table"** | Performance disaster | **Allowed only for `updated_at` and cross-row invariants** (see §12). |
| **Materialized views without a refresh policy** | Stale data, ops pain | **Banned** without an explicit refresh schedule. |
| **Schema modifications from application code** (`CREATE TABLE` at runtime) | DDL in transactions is fragile | **Banned**. Migrations only. |

---

## 21. Open questions for Operator / next G2 stage

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | Should we use `ltree` for `organization.path` or adjacency list with materialized path? | `ltree` (native, efficient) |
| Q2 | Should we add a `pg_partman` extension for time-based partitioning? | V1.5 (not V1) |
| Q3 | Should we use `pgcrypto`'s `gen_random_uuid()` for system PKs or app-side UUID v4? | `pgcrypto`'s `gen_random_uuid()` (no app code) |
| Q4 | Should we add `pg_stat_statements` for slow-query tracking? | Yes, on by default in non-prod; off by default in prod (operational) |
| Q5 | Should we add `auto_explain` for query plan logging? | Yes, with `log_min_duration = 500ms` |
| Q6 | Is `pg_dump` adequate, or do we want `barman`? | `pg_dump` V1; `barman` V1.5 |
| Q7 | Multi-currency: do amounts store as-is, or do we also store a `base_currency_amount`? | Store both: `amount` (transaction) + `base_amount` (tenant base) |

---

## 22. Stage 1 deliverable checklist (when implementation starts)

- [ ] `Dockerfile.postgres` (or docker-compose snippet) creates the DB with the three roles and the required extensions
- [ ] `GuliERP.Foundation.Infrastructure` migrations create the Foundation tables (users, roles, tenants, companies, organizations, audit_entry, refresh_token, idempotency_key, module_record, number_sequence, dictionary, dictionary_item)
- [ ] Every Foundation table has: `id bigint PK`, `tenant_id bigint NOT NULL FK`, `created_at`, `updated_at`, `created_by`, `updated_by`, `concurrency_version`, the relevant `CHECK` constraints
- [ ] `gulierp_app` cannot `DROP TABLE` / `DELETE FROM audit_entry` (CI test)
- [ ] Every FK is indexed (CI test enumerates FKs and verifies index existence)
- [ ] `EXPLAIN` of the "list sales orders for current tenant" query is `Index Scan` (CI test on a seeded DB)
- [ ] Migration up + down completes in < 5 min on empty DB
- [ ] Snowflake generator produces unique IDs across 2 simulated instances (CI test)

**No business tables in Stage 1.** Stage 1 is the substrate for them.

---

*End of G2 PostgreSQL Engineering Standard V1 Draft — Status: DRAFT. Companion: TASK B (Foundation), TASK C (Security), TASK D (Module Runtime), TASK F (API), TASK G (Approval).*
