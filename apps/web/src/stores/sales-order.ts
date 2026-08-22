import { defineStore } from 'pinia';
import { ref } from 'vue';
import {
  cancelSalesOrder,
  confirmSalesOrder,
  createSalesOrder,
  getSalesOrder,
  listSalesOrders,
  updateSalesOrder,
} from '../api/sales-order';
import type {
  CreateSalesOrderRequest,
  SalesOrderDto,
  SalesOrderListItemDto,
  SalesOrderListParams,
  UpdateSalesOrderRequest,
} from '../types/sales-order';

export const useSalesOrderStore = defineStore('sales-orders', () => {
  const items = ref<SalesOrderListItemDto[]>([]);
  const totalCount = ref(0);
  const page = ref(1);
  const pageSize = ref(20);
  const loading = ref(false);
  const current = ref<SalesOrderDto | null>(null);

  async function fetchList(params: SalesOrderListParams = {}) {
    loading.value = true;
    try {
      const result = await listSalesOrders({ page: page.value, pageSize: pageSize.value, ...params });
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
    current.value = await getSalesOrder(id);
    return current.value;
  }

  async function createDraft(request: CreateSalesOrderRequest) {
    current.value = await createSalesOrder(request);
    return current.value;
  }

  async function updateDraft(id: string, request: UpdateSalesOrderRequest) {
    current.value = await updateSalesOrder(id, request);
    return current.value;
  }

  async function confirm(id: string) {
    current.value = await confirmSalesOrder(id);
    return current.value;
  }

  async function cancel(id: string) {
    current.value = await cancelSalesOrder(id);
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
