<template>
  <!-- MdmEmptyState — reusable empty state for MDM list tables -->
  <div class="mdm-empty">
    <el-icon :size="40" :color="'var(--text-disabled, #94A3B8)'">
      <component :is="icon" />
    </el-icon>
    <p>{{ message }}</p>
    <el-button v-if="showCreate" type="primary" @click="emit('create')">
      <el-icon><Plus /></el-icon>
      {{ createLabel }}
    </el-button>
  </div>
</template>

<script setup lang="ts">
import { Plus, Document } from '@element-plus/icons-vue';
import { markRaw } from 'vue';

const props = withDefaults(defineProps<{
  message?: string;
  showCreate?: boolean;
  createLabel?: string;
  icon?: any;
}>(), {
  message: '暂无数据',
  showCreate: true,
  createLabel: '新建',
  icon: () => markRaw(Document),
});

const emit = defineEmits<{ create: [] }>();
</script>

<style scoped>
.mdm-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 32px 0;
}
.mdm-empty p {
  color: var(--text-muted, #64748B);
  font-size: 14px;
}
</style>
