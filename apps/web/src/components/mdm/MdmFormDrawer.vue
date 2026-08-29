<template>
  <!-- MdmFormDrawer — reusable create/edit form drawer
       Pattern: el-drawer + el-form with sticky footer
       Slot: default = form fields
       v-model: visible, model (form data) -->
  <el-drawer
    v-model="visible"
    :title="title"
    :size="drawerSize"
    :close-on-click-modal="false"
    :before-close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="model"
      :rules="rules"
      :label-width="labelWidth"
      label-position="right"
      size="default"
    >
      <slot />
    </el-form>

    <template #footer>
      <div class="mdm-form-footer">
        <el-button @click="visible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="emit('submit')">
          {{ submitLabel }}
        </el-button>
      </div>
    </template>
  </el-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FormRules } from 'element-plus';

const props = withDefaults(defineProps<{
  modelValue?: boolean;
  model: Record<string, any>;
  title: string;
  rules?: FormRules;
  loading?: boolean;
  submitLabel?: string;
  drawerSize?: string;
  labelWidth?: string;
}>(), {
  loading: false,
  submitLabel: '保存',
  drawerSize: 'var(--dialog-width-md)',
  labelWidth: '104px',
});

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  'update:model': [value: Record<string, any>];
  submit: [];
}>();

const visible = computed({
  get: () => props.modelValue ?? false,
  set: (value: boolean) => emit('update:modelValue', value),
});
const model = computed(() => props.model);

function handleClose(done: () => void) {
  done();
}
</script>

<style scoped>
.mdm-form-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
