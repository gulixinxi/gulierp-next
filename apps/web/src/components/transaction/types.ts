// GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A — shared transaction components).
// Shared TypeScript types for the transaction-document line grid.
// Both SalesOrder and PurchaseOrder editors consume this generic shape.
// The editor pages (Phase B / Phase D) map their domain DTOs to / from
// TransactionLineRow at the boundary.

/**
 * A single transaction-document line in its editor-friendly shape.
 * Used by LineGrid, TotalsBar, and the editor pages.
 *
 * Money fields (netAmount / taxAmount / totalAmount) are derived from
 * (quantity * unitPrice * (1 - discountRate)) per
 * GULIERP_NUMERIC_PRECISION_STANDARD_V1 §6.
 *
 * Snapshot fields (itemCode / itemName / specification / uomCode / uomName)
 * are filled when an item or UoM is picked; they preserve the human-readable
 * label at the time the line was created so a later master-data rename
 * does not retroactively rewrite the document.
 */
export interface TransactionLineRow {
  /** Client-side unique key. UUIDv4 from the editor. */
  clientId: string;
  /** Server-assigned line number (1..N) once the document is saved. */
  lineNo: number;
  /** Live reference to Item master. Snapshot fields below are derived. */
  itemId: string | null;
  itemCode?: string;
  itemName?: string;
  specification?: string;
  /** Live reference to UoM master. Default = Item.baseUomId. */
  uomId: string | null;
  uomCode?: string;
  uomName?: string;
  /** Quantity > 0. Required. */
  quantity: number;
  /** Tax-excluded unit price. >= 0. */
  unitPrice: number;
  /** 0..1. Optional, default 0. */
  discountRate: number;
  /** 0..1. Optional, default 0. P0 does NOT auto-derive from Item or BP. */
  taxRate: number;
  /** Derived. Round(quantity * unitPrice * (1 - discountRate)). */
  netAmount: number;
  /** Derived. Round(netAmount * taxRate). */
  taxAmount: number;
  /** Derived. netAmount + taxAmount. */
  totalAmount: number;
  /** Line-level remark. Optional. */
  remarks?: string;
}

/** Currency code. V1 only CNY. Kept as a prop for forward-compat. */
export type CurrencyCode = 'CNY' | string;

/** A single concurrency-conflict payload (409 response). */
export interface ConcurrencyConflictInfo {
  actorName: string;
  occurredAt: string; // ISO 8601
  requestId?: string;
  /** The latest server version. The client may pass this to the editor. */
  latestConcurrencyVersion?: number;
}
