<template>
  <!-- MdmListToolbar — reusable toolbar for MDM list pages
       Slot-based: left = search + filters, right = actions
       Pattern: gs-list-toolbar from SalesOrderList, extracted as reusable

       GULIERP_MDM_LIST_UI_STANDARD_FIX_01 (2026-09-02) — Wrap the
       shared .gs-list-toolbar in a flex container that forces
       search + filters + actions onto one row on a normal
       desktop, while still allowing wrap on truly narrow
       viewports. The internal `.mdm-toolbar-row` owns the
       layout so this change does not leak into SalesOrderList
       / PurchaseOrderList that also use .gs-list-toolbar. -->
  <div class="mdm-toolbar-row">
    <div class="gs-list-toolbar mdm-list-toolbar">
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
.mdm-toolbar-row {
  /* Force the search + filters + actions cluster to stay on
     one row inside the toolbar. The shared .gs-list-toolbar
     keeps its own flex-wrap: wrap (so SalesOrderList /
     PurchaseOrderList stay unchanged). */
  display: flex;
  width: 100%;
}

.mdm-toolbar-row .mdm-list-toolbar {
  width: 100%;
  flex-wrap: nowrap;
}

.mdm-toolbar-row .gs-toolbar-left,
.mdm-toolbar-row .gs-toolbar-right {
  flex-wrap: nowrap;
  min-width: 0;
}

.mdm-toolbar-row .gs-toolbar-left {
  flex: 1 1 auto;
}

.mdm-toolbar-row .gs-toolbar-right {
  flex: 0 0 auto;
}

.mdm-toolbar-search {
  flex: 0 1 var(--toolbar-search-wide);
  min-width: 220px;
  max-width: 100%;
}
</style>
