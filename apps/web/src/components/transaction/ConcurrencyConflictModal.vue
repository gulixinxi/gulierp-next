<!--
  GULIERP_TRANSACTION_DOCUMENT_LINE_GRID_V1 (Phase A) — shared transaction components.
  ConcurrencyConflictModal — Standard 409 conflict UX.

  Triggered when the server returns 409 concurrency_conflict. Surfaces
  actor + time + requestId and offers a single Refresh action. The user's
  local edit is preserved in the caller (the editor keeps the form state)
  so a manual refresh re-fetches and lets the user re-apply their changes.

  Spec: GULIERP_TRANSACTION_DOCUMENT_UX_CONTRACT_V1 §11.
-->
<template>
  <el-dialog
    :model-value="modelValue"
    title="此单据已被其他人修改"
    width="460"
    :close-on-click-modal="false"
    :close-on-press-escape="false"
    :show-close="false"
    align-center
    @update:model-value="onUpdateVisible"
  >
    <div class="tx-conflict-body">
      <el-icon class="tx-conflict-icon" :size="36" color="#E6A23C"><Warning /></el-icon>
      <p class="tx-conflict-line">
        <strong>{{ actorName || '其他用户' }}</strong>
        在
        <span class="tx-conflict-time">{{ formattedTime }}</span>
        修改了此单据。
      </p>
      <p class="tx-conflict-detail">你看到的可能不是最新版本。刷新可重新加载服务器最新内容，本地未保存的编辑会保留在编辑区。</p>
      <p v-if="requestId" class="tx-conflict-requestid">RequestId: {{ requestId }}</p>
    </div>

    <template #footer>
      <div class="tx-conflict-footer">
        <el-button @click="onCancel">取消</el-button>
        <el-button type="primary" @click="onRefresh">刷新以查看最新版本</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Warning } from '@element-plus/icons-vue';

const props = defineProps<{
  modelValue: boolean;
  actorName?: string;
  occurredAt?: string; // ISO 8601
  requestId?: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  refresh: [];
  cancel: [];
}>();

const formattedTime = computed(() => {
  if (!props.occurredAt) return '刚刚';
  const d = new Date(props.occurredAt);
  if (isNaN(d.getTime())) return props.occurredAt;
  return d.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
});

function onUpdateVisible(v: boolean) {
  emit('update:modelValue', v);
}

function onCancel() {
  emit('cancel');
  emit('update:modelValue', false);
}

function onRefresh() {
  emit('refresh');
  emit('update:modelValue', false);
}
</script>

<style scoped>
.tx-conflict-body {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 8px 0 0;
  gap: 8px;
}
.tx-conflict-icon {
  margin-bottom: 4px;
}
.tx-conflict-line {
  margin: 0;
  font-size: 14px;
  color: var(--text-primary);
  line-height: 1.6;
}
.tx-conflict-time {
  color: var(--warning-default);
  font-weight: 600;
  margin: 0 4px;
}
.tx-conflict-detail {
  margin: 0;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.6;
}
.tx-conflict-requestid {
  margin: 4px 0 0;
  font-size: 12px;
  color: var(--text-placeholder);
  font-family: var(--erp-mono-font);
}
.tx-conflict-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
