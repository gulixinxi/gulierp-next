<template>
  <!-- MdmStatusBadge — reusable status badge for all MDM master data
       Uses STATUS_OPTIONS from types/mdm.ts for consistent labeling -->
  <el-tag :type="tagType" size="small" effect="light" disable-transitions>
    {{ label }}
  </el-tag>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { STATUS_OPTIONS } from '../../types/mdm';
import type { MasterDataStatus } from '../../types/mdm';

const props = defineProps<{
  status: MasterDataStatus;
}>();

const { label, tagType } = (() => {
  const opt = STATUS_OPTIONS.find(o => o.value === props.status);
  return {
    label: opt?.label ?? props.status,
    tagType: opt?.tagType ?? ('info' as const),
  };
})();
</script>
