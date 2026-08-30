<!--
  GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — shared transaction components.
  TotalsBar — Sticky bottom totals row for the transaction editor.

  Consumes the editor's `lines` array (TransactionLineRow[]) and renders:
    - line count
    - total quantity (sum of quantity in base UoM; V1 does not convert)
    - total net amount
    - total tax amount
    - total amount (grand total)

  Spec: GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1 §9.
-->
<template>
  <div class="tx-totals-bar">
    <div class="tx-totals-row">
      <div class="tx-totals-cell">
        <span class="tx-totals-label">行数</span>
        <span class="tx-totals-value">{{ lineCount }}</span>
      </div>
      <div class="tx-totals-cell">
        <span class="tx-totals-label">总数量</span>
        <span class="tx-totals-value tx-num">{{ formatQty(totalQuantity) }}</span>
      </div>
      <div class="tx-totals-cell">
        <span class="tx-totals-label">净额合计</span>
        <span class="tx-totals-value tx-num">{{ formatMoney(totalNetAmount) }}</span>
      </div>
      <div class="tx-totals-cell">
        <span class="tx-totals-label">税额合计</span>
        <span class="tx-totals-value tx-num">{{ formatMoney(totalTaxAmount) }}</span>
      </div>
      <div class="tx-totals-cell tx-totals-grand">
        <span class="tx-totals-label">价税合计</span>
        <span class="tx-totals-value tx-num tx-grand-value">{{ formatMoney(totalAmount) }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { TransactionLineRow } from './types';

const props = defineProps<{
  lines: TransactionLineRow[];
  currencyCode?: string;
}>();

const lineCount = computed(() => props.lines.length);

const totalQuantity = computed(() =>
  props.lines.reduce((acc, l) => acc + (Number.isFinite(l.quantity) ? l.quantity : 0), 0),
);

const totalNetAmount = computed(() =>
  props.lines.reduce((acc, l) => acc + (Number.isFinite(l.netAmount) ? l.netAmount : 0), 0),
);

const totalTaxAmount = computed(() =>
  props.lines.reduce((acc, l) => acc + (Number.isFinite(l.taxAmount) ? l.taxAmount : 0), 0),
);

const totalAmount = computed(() =>
  props.lines.reduce((acc, l) => acc + (Number.isFinite(l.totalAmount) ? l.totalAmount : 0), 0),
);

const currencySymbol = computed(() => {
  switch ((props.currencyCode || 'CNY').toUpperCase()) {
    case 'CNY':
      return '¥';
    case 'USD':
      return '$';
    case 'EUR':
      return '€';
    default:
      return '';
  }
});

function formatMoney(n: number): string {
  if (!Number.isFinite(n)) return `${currencySymbol.value}0.00`;
  // Display 2 decimals for amounts per GULIERP_NUMERIC_PRECISION_STANDARD_V1.
  return `${currencySymbol.value}${n.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')}`;
}

function formatQty(n: number): string {
  if (!Number.isFinite(n)) return '0.0000';
  // Display 4 decimals for quantities per GULIERP_NUMERIC_PRECISION_STANDARD_V1.
  return n.toFixed(4);
}
</script>

<style scoped>
.tx-totals-bar {
  position: sticky;
  bottom: 0;
  z-index: 5;
  background: var(--bg-elevated, #fff);
  border-top: 1px solid var(--border-default);
  padding: 10px 16px;
  box-shadow: 0 -1px 4px rgba(0, 0, 0, 0.04);
}
.tx-totals-row {
  display: flex;
  align-items: center;
  gap: 32px;
  flex-wrap: wrap;
  justify-content: flex-end;
}
.tx-totals-cell {
  display: flex;
  align-items: baseline;
  gap: 8px;
  min-width: 0;
}
.tx-totals-label {
  color: var(--text-muted);
  font-size: 13px;
  white-space: nowrap;
}
.tx-totals-value {
  color: var(--text-primary);
  font-size: 14px;
  font-weight: 600;
  white-space: nowrap;
}
.tx-totals-value.tx-num {
  font-family: var(--erp-mono-font);
  font-feature-settings: 'tnum';
}
.tx-totals-grand {
  padding-left: 16px;
  border-left: 1px solid var(--border-default);
  margin-left: 8px;
}
.tx-grand-value {
  color: var(--warning-default);
  font-size: 16px;
}
</style>
