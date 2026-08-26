import { defineStore } from 'pinia';
import { ref } from 'vue';
import {
  cancelPurchaseOrder,
  confirmPurchaseOrder,
  createPurchaseOrder,
  getPurchaseOrder,
  listPurchaseOrders,
  updatePurchaseOrder,
} from '../api/purchase-order';
import type {
  CreatePurchaseOrderRequest,
  PurchaseOrderDto,
  PurchaseOrderListItemDto,
  PurchaseOrderListParams,
  UpdatePurchaseOrderRequest,
} from '../types/purchase-order';

export const usePurchaseOrderStore = defineStore('purchase-orders', () => {
  const items = ref<PurchaseOrderListItemDto[]>([]);
  const totalCount = ref(0);
  const page = ref(1);
  const pageSize = ref(20);
  const loading = ref(false);
  const current = ref<PurchaseOrderDto | null>(null);

  async function fetchList(params: PurchaseOrderListParams = {}) {
    loading.value = true;
    try {
      const result = await listPurchaseOrders({ page: page.value, pageSize: pageSize.value, ...params });
      items.value = result.items;
      totalCount.value = result.totalCount;
      page.value = result.page;
      pageSize.value = result.pageSize;
      return result;
    } finally {
      loading.value = false;
    }
  }

  async function fetchDetail(id: string) {
    current.value = await getPurchaseOrder(id);
    return current.value;
  }

  async function createDraft(request: CreatePurchaseOrderRequest) {
    current.value = await createPurchaseOrder(request);
    return current.value;
  }

  async function updateDraft(id: string, request: UpdatePurchaseOrderRequest) {
    current.value = await updatePurchaseOrder(id, request);
    return current.value;
  }

  async function confirm(id: string) {
    current.value = await confirmPurchaseOrder(id);
    return current.value;
  }

  async function cancel(id: string) {
    current.value = await cancelPurchaseOrder(id);
    return current.value;
  }

  return {
    items,
    totalCount,
    page,
    pageSize,
    loading,
    current,
    fetchList,
    fetchDetail,
    createDraft,
    updateDraft,
    confirm,
    cancel,
  };
});
