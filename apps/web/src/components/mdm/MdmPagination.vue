<template>
  <!-- MdmPagination — reusable pagination bar for MDM list pages
       Pattern: gs-list-pagination from SalesOrderList -->
  <div class="gs-list-pagination">
    <span class="gs-summary-text">
      共 <b>{{ total }}</b> 条<template v-if="selectedCount > 0"> · 已选 <b>{{ selectedCount }}</b> 条</template>
    </span>
    <el-pagination
      v-model:current-page="currentPageModel"
      v-model:page-size="pageSizeModel"
      :total="total"
      :page-sizes="pageSizes"
      layout="sizes, prev, pager, next, jumper"
      background
    />
  </div>
</template>

<script setup lang="ts">
const currentPageModel = defineModel<number>('currentPage', { default: 1 });
const pageSizeModel = defineModel<number>('pageSize', { default: 20 });

withDefaults(defineProps<{
  total: number;
  selectedCount?: number;
  pageSizes?: number[];
}>(), {
  selectedCount: 0,
  pageSizes: () => [20, 50, 100],
});
</script>
