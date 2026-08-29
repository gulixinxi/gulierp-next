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
        class="mdm-toolbar-search"
        :style="{ width: searchWidth }"
        @keyup.enter="emit('search')"
        @clear="emit('search')"
      />
      <slot name="filters" />
    </div>
    <div class="gs-toolbar-right">
      <slot name="actions" />
      <template v-if="showCreate">
        <el-divider direction="vertical" />
        <el-button type="primary" @click="emit('create')">
          <el-icon><Plus /></el-icon>
          {{ createLabel }}
        </el-button>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Search as SearchIcon, Plus } from '@element-plus/icons-vue';

const props = withDefaults(defineProps<{
  searchPlaceholder?: string;
  createLabel?: string;
  /**
   * G3-R1E: hide the Create button for read-only pages
   * (e.g. PaymentMethodList, which is a facade over the
   * standard Dictionary write endpoint). Defaults to true
   * to preserve the existing behavior on UomList / ItemList
   * / etc.
   */
  showCreate?: boolean;
  searchWidth?: string;
}>(), {
  searchPlaceholder: '搜索代码 / 名称',
  createLabel: '新建',
  showCreate: true,
  searchWidth: 'var(--toolbar-search-width)',
});

const searchModel = defineModel<string>('search', { default: '' });

const emit = defineEmits<{
  search: [];
  create: [];
}>();
</script>

<style scoped>
.mdm-toolbar-search {
  flex: 0 1 var(--toolbar-search-wide);
  min-width: 220px;
  max-width: 100%;
}
</style>
