<template>
  <!--
    MdmTableRowActions — Shared MDM table action column (WEB-UX-SHELL-001).
    Renders a SINGLE-LINE row action cluster:
      ACTIVE row:   编辑 | 停用
      INACTIVE row: 编辑 | 启用
    No physical delete button. Master data is deactivated, never deleted.
    Uses text/link style buttons (not block primary buttons) to save width.
    CSS class .mdm-row-actions is defined globally in navigation.css so
    all MDM pages share the same nowrap + compact spacing.
  -->
  <div class="mdm-row-actions">
    <el-button text size="small" type="primary" @click="$emit('edit')">编辑</el-button>
    <span class="mdm-action-sep">|</span>
    <el-button
      v-if="isActive"
      text
      size="small"
      type="warning"
      @click="$emit('deactivate')"
    >停用</el-button>
    <el-button
      v-else
      text
      size="small"
      type="success"
      @click="$emit('activate')"
    >启用</el-button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { MasterDataStatus } from '../../types/mdm';

const props = defineProps<{
  status: MasterDataStatus;
}>();

defineEmits<{
  edit: [];
  activate: [];
  deactivate: [];
}>();

const isActive = computed(() => props.status === 'active');
</script>
