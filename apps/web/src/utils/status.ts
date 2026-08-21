// 3D status visualization mapping (DEC-STATUS-001)
import type { DocumentStatus, ApprovalStatus, ExecutionStatus } from '../types/sales-order';

export type TagType = 'info' | 'success' | 'warning' | 'danger' | 'primary';

export const documentStatusMap: Record<DocumentStatus, { label: string; tag: TagType; color: string }> = {
  Draft: { label: '草稿', tag: 'info', color: '#909399' },
  Active: { label: '生效', tag: 'success', color: '#67c23a' },
  Closed: { label: '已关闭', tag: 'info', color: '#606266' },
  Cancelled: { label: '已取消', tag: 'danger', color: '#909399' }
};

export const approvalStatusMap: Record<ApprovalStatus, { label: string; tag: TagType; color: string }> = {
  NotSubmitted: { label: '未提交', tag: 'info', color: '#909399' },
  Pending: { label: '待审批', tag: 'warning', color: '#e6a23c' },
  Approved: { label: '已通过', tag: 'success', color: '#67c23a' },
  Rejected: { label: '已驳回', tag: 'danger', color: '#f56c6c' },
  Withdrawn: { label: '已撤回', tag: 'info', color: '#909399' }
};

export const executionStatusMap: Record<ExecutionStatus, { label: string; tag: TagType; color: string }> = {
  NotStarted: { label: '未执行', tag: 'info', color: '#909399' },
  Partial: { label: '部分执行', tag: 'warning', color: '#e6a23c' },
  Completed: { label: '已完成', tag: 'success', color: '#67c23a' }
};

export function fmtMoney(v: number, decimals = 2): string {
  if (v === undefined || v === null || Number.isNaN(v)) return '0.00';
  return Number(v).toLocaleString('zh-CN', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  });
}

export function fmtRate(v: number, decimals = 2): string {
  if (!v) return '0%';
  return (v * 100).toFixed(decimals) + '%';
}

export function fmtDate(d?: string): string {
  if (!d) return '';
  if (d.length >= 10) return d.slice(0, 10);
  return d;
}

export function fmtDateTime(d?: string): string {
  if (!d) return '';
  return d;
}

// Action availability — per spec §6, gated by 3D status
export interface ActionAvailability {
  save: boolean; submit: boolean; approve: boolean; reject: boolean;
  withdraw: boolean; resubmit: boolean; cancel: boolean; close: boolean;
  print: boolean; copy: boolean; generateShipment: boolean; edit: boolean;
}

export function computeActions(so: {
  documentStatus: DocumentStatus;
  approvalStatus: ApprovalStatus;
  executionStatus: ExecutionStatus;
}): ActionAvailability {
  const { documentStatus, approvalStatus, executionStatus } = so;
  const isTerminal = documentStatus === 'Closed' || documentStatus === 'Cancelled';
  return {
    save: documentStatus === 'Draft',
    submit: documentStatus === 'Draft' && approvalStatus === 'NotSubmitted',
    approve: documentStatus === 'Active' && approvalStatus === 'Pending',
    reject: documentStatus === 'Active' && approvalStatus === 'Pending',
    withdraw: documentStatus === 'Active' && approvalStatus === 'Pending',
    resubmit: documentStatus === 'Active' && (approvalStatus === 'Rejected' || approvalStatus === 'Withdrawn'),
    cancel: !isTerminal && (documentStatus === 'Draft' || documentStatus === 'Active'),
    close: documentStatus === 'Active' && executionStatus === 'Completed',
    print: true,
    copy: true,
    generateShipment: documentStatus === 'Active' && approvalStatus === 'Approved',
    edit: documentStatus === 'Draft'
  };
}
