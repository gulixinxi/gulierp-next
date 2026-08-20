<template>
  <!-- MdmDetailDrawer — reusable read-only detail drawer
       Pattern: el-drawer with description-list layout
       Slots: default = main content, footer = actions -->
  <el-drawer
    v-model="visible"
    :title="title"
    size="520px"
    :before-close="handleClose"
  >
    <div class="mdm-detail">
      <!-- Status + meta header -->
      <div class="mdm-detail-header">
        <slot name="header" />
      </div>

      <!-- Description list -->
      <el-descriptions :column="2" border size="small" class="mdm-detail-desc">
        <slot name="descriptions" />
      </el-descriptions>

      <!-- Extra content (tabs, sections) -->
      <div class="mdm-detail-extra">
        <slot />
      </div>
    </div>

    <template #footer>
      <slot name="footer">
        <el-button @click="visible = false">关闭</el-button>
        <el-button type="primary" @click="emit('edit')">编辑</el-button>
      </slot>
    </template>
  </el-drawer>
</template>

<script setup lang="ts">
const visible = defineModel<boolean>({ default: false });

withDefaults(defineProps<{
  title?: string;
}>(), {
  title: '详情',
});

const emit = defineEmits<{ edit: [] }>();

function handleClose(done: () => void) {
  done();
}
</script>

<style scoped>
.mdm-detail-header {
  margin-bottom: 16px;
}
.mdm-detail-desc {
  margin-bottom: 16px;
}
.mdm-detail-extra {
  margin-top: 8px;
}
</style>
