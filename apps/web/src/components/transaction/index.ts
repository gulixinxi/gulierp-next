// GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — barrel export.
// Editor pages (Phase B Sales / Phase D Purchase) consume from this barrel.

export { default as LineGrid } from './LineGrid.vue';
export type { LineGridRow } from './LineGrid.vue';

export { default as TotalsBar } from './TotalsBar.vue';

export { default as ConcurrencyConflictModal } from './ConcurrencyConflictModal.vue';

export { default as ItemSelectorPopover } from './ItemSelectorPopover.vue';
export { default as PartnerSelectorPopover } from './PartnerSelectorPopover.vue';
export { default as WarehouseSelectorPopover } from './WarehouseSelectorPopover.vue';

export type {
  TransactionLineRow,
  CurrencyCode,
  ConcurrencyConflictInfo,
} from './types';
