// SalesOrder mock data store — in-memory mutation of seed data
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { seedSalesOrders, recomputeHeader } from '../mock/sales-order';
import type { SalesOrder, SalesOrderLine } from '../types/sales-order';

export const useSalesOrderStore = defineStore('sales-orders', () => {
  const orders = ref<SalesOrder[]>(JSON.parse(JSON.stringify(seedSalesOrders)));

  function list(): SalesOrder[] {
    return orders.value;
  }

  function getById(id: string): SalesOrder | undefined {
    return orders.value.find(o => o.id === id);
  }

  function upsert(so: SalesOrder): void {
    recomputeHeader(so);
    const idx = orders.value.findIndex(o => o.id === so.id);
    if (idx >= 0) {
      orders.value[idx] = so;
    } else {
      orders.value.unshift(so);
    }
  }

  function remove(id: string): void {
    orders.value = orders.value.filter(o => o.id !== id);
  }

  function clone(soId: string, newNo: string): SalesOrder | undefined {
    const src = getById(soId);
    if (!src) return undefined;
    const clone: SalesOrder = JSON.parse(JSON.stringify(src));
    clone.id = `tmp-${Date.now()}`;
    clone.salesOrderNo = newNo;
    clone.documentStatus = 'Draft';
    clone.approvalStatus = 'NotSubmitted';
    clone.executionStatus = 'NotStarted';
    clone.approvedBy = undefined;
    clone.approvedAt = undefined;
    clone.concurrencyVersion = 1;
    clone.lines.forEach((l, i) => {
      l.lineNo = i + 1;
      l.executedQuantity = 0;
      l.openQuantity = l.quantity;
      l.reservedQuantity = 0;
      l.cancelledQuantity = 0;
      l.lineStatus = 'Open';
    });
    clone.downstream = [];
    clone.approvalHistory = [];
    clone.attachments = [];
    clone.auditLog = [{
      id: `${clone.id}-a1`,
      action: 'CopyCreate',
      actionLabel: '复制新建',
      actor: '吴海',
      actorRole: 'SalesManager',
      at: new Date().toISOString().replace('T', ' ').slice(0, 19),
      remark: `源单据 ${src.salesOrderNo}`
    }];
    recomputeHeader(clone);
    return clone;
  }

  function applyAction(
    soId: string,
    action: 'save' | 'submit' | 'approve' | 'reject' | 'withdraw' | 'cancel' | 'close' | 'resubmit',
    reason?: string
  ): SalesOrder | undefined {
    const so = getById(soId);
    if (!so) return undefined;
    const now = new Date().toISOString().replace('T', ' ').slice(0, 19);
    const actor = '吴海';
    const role = 'SalesManager';
    switch (action) {
      case 'save':
        so.updatedAt = now;
        so.concurrencyVersion += 1;
        so.auditLog.push({ id: `${so.id}-a${so.auditLog.length + 1}`, action: 'Save', actionLabel: '保存', actor, actorRole: role, at: now });
        break;
      case 'submit':
        so.documentStatus = 'Active';
        so.approvalStatus = 'Pending';
        so.updatedAt = now;
        so.concurrencyVersion += 1;
        so.auditLog.push({ id: `${so.id}-a${so.auditLog.length + 1}`, action: 'Submit', actionLabel: '提交', actor, actorRole: role, at: now });
        so.approvalHistory.push({ id: `ap-${so.id}-${Date.now()}`, step: '销售经理审批', approver: '吴海', approverRole: 'SalesManager', status: 'Pending', at: now });
        break;
      case 'approve':
        so.approvalStatus = 'Approved';
        so.approvedBy = actor;
        so.approvedAt = now;
        so.updatedAt = now;
        so.concurrencyVersion += 1;
        so.auditLog.push({ id: `${so.id}-a${so.auditLog.length + 1}`, action: 'Approve', actionLabel: '审核通过', actor, actorRole: role, at: now });
        if (so.approvalHistory.length) {
          const last = so.approvalHistory[so.approvalHistory.length - 1];
          last.status = 'Approved';
          last.at = now;
          last.comment = '同意';
        }
        break;
      case 'reject':
        so.approvalStatus = 'Rejected';
        so.updatedAt = now;
        so.concurrencyVersion += 1;
        so.auditLog.push({ id: `${so.id}-a${so.auditLog.length + 1}`, action: 'Reject', actionLabel: '驳回', actor, actorRole: role, at: now, reason });
        if (so.approvalHistory.length) {
          const last = so.approvalHistory[so.approvalHistory.length - 1];
          last.status = 'Rejected';
          last.at = now;
          last.comment = reason;
        }
        break;
      case 'withdraw':
        so.approvalStatus = 'Withdrawn';
        so.updatedAt = now;
        so.concurrencyVersion += 1;
        so.auditLog.push({ id: `${so.id}-a${so.auditLog.length + 1}`, action: 'Withdraw', actionLabel: '撤回', actor, actorRole: role, at: now, reason });
        break;
      case 'resubmit':
        so.approvalStatus = 'Pending';
        so.updatedAt = now;
        so.concurrencyVersion += 1;
        so.auditLog.push({ id: `${so.id}-a${so.auditLog.length + 1}`, action: 'Resubmit', actionLabel: '重新提交', actor, actorRole: role, at: now });
        so.approvalHistory.push({ id: `ap-${so.id}-${Date.now()}`, step: '销售经理审批', approver: '吴海', approverRole: 'SalesManager', status: 'Pending', at: now });
        break;
      case 'cancel':
        so.documentStatus = 'Cancelled';
        so.updatedAt = now;
        so.concurrencyVersion += 1;
        so.auditLog.push({ id: `${so.id}-a${so.auditLog.length + 1}`, action: 'Cancel', actionLabel: '取消', actor, actorRole: role, at: now, reason });
        break;
      case 'close':
        so.documentStatus = 'Closed';
        so.updatedAt = now;
        so.concurrencyVersion += 1;
        so.auditLog.push({ id: `${so.id}-a${so.auditLog.length + 1}`, action: 'Close', actionLabel: '关闭', actor, actorRole: role, at: now, reason });
        break;
    }
    return so;
  }

  return { orders, list, getById, upsert, remove, clone, applyAction };
});
