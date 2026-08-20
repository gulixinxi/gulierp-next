<template>
  <!-- MdmListToolbar — reusable toolbar for MDM list pages
       Slot-based: left = search + filters, right = actions
       Pattern: gs-list-toolbar from SalesOrderList, extracted as reusable -->
  <div class="gs-list-toolbar">
    <div class="gs-toolbar-left">
      <el-input
        v-model="searchModel"
        :placeholder="searchPlaceholder"
        clearable
        :prefix-icon="SearchIcon"
        style="width: 260px"
        @keyup.enter="emit('search')"
        @clear="emit('search')"
      />
      <slot name="filters" />
    </div>
    <div class="gs-toolbar-right">
      <slot name="actions" />
      <el-divider direction="vertical" />
      <el-button type="primary" @click="emit('create')">
        <el-icon><Plus /></el-icon>
        {{ createLabel }}
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Search as SearchIcon, Plus } from '@element-plus/icons-vue';

const props = withDefaults(defineProps<{
  searchPlaceholder?: string;
  createLabel?: string;
}>(), {
  searchPlaceholder: '搜索代码 / 名称',
  createLabel: '新建',
});

const searchModel = defineModel<string>('search', { default: '' });

const emit = defineEmits<{
  search: [];
  create: [];
}>();
</script>
