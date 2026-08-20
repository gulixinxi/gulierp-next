<template>
  <!-- MdmFormDrawer — reusable create/edit form drawer
       Pattern: el-drawer + el-form with sticky footer
       Slot: default = form fields
       v-model: visible, model (form data) -->
  <el-drawer
    v-model="visible"
    :title="title"
    size="480px"
    :close-on-click-modal="false"
    :before-close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="model"
      :rules="rules"
      label-width="100px"
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
import type { FormRules } from 'element-plus';

withDefaults(defineProps<{
  title: string;
  rules?: FormRules;
  loading?: boolean;
  submitLabel?: string;
}>(), {
  loading: false,
  submitLabel: '保存',
});

const visible = defineModel<boolean>({ default: false });
const model = defineModel<Record<string, any>>('model', { required: true });

const emit = defineEmits<{
  submit: [];
}>();

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
