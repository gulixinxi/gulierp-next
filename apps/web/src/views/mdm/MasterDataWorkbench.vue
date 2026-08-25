<template>
  <section class="mdm-workbench">
    <header class="mdm-workbench__header">
      <div>
        <h1>主数据中心</h1>
        <p>维护企业基础资料、物料资料、仓库库位与往来单位</p>
      </div>
    </header>

    <div class="mdm-workbench__grid" aria-label="主数据模块入口">
      <RouterLink
        v-for="module in primaryModules"
        :key="module.route"
        class="mdm-workbench-card"
        :to="module.route"
      >
        <span class="mdm-workbench-card__icon" aria-hidden="true">
          <el-icon><component :is="module.icon" /></el-icon>
        </span>
        <span class="mdm-workbench-card__body">
          <span class="mdm-workbench-card__title">{{ module.title }}</span>
          <span class="mdm-workbench-card__desc">{{ module.description }}</span>
        </span>
        <span class="mdm-workbench-card__status">{{ module.status }}</span>
      </RouterLink>
    </div>

    <section v-if="deferredModules.length > 0" class="mdm-workbench__deferred" aria-label="后续接入模块">
      <h2>后续接入</h2>
      <div class="mdm-workbench__deferred-list">
        <div v-for="item in deferredModules" :key="item.title" class="mdm-workbench-deferred">
          <span>{{ item.title }}</span>
          <span>{{ item.reason }}</span>
        </div>
      </div>
    </section>
  </section>
</template>

<script setup lang="ts">
import type { Component } from 'vue';
import {
  Box,
  Document,
  Files,
  Goods,
  OfficeBuilding,
  ScaleToOriginal,
  Tickets,
  UserFilled,
} from '@element-plus/icons-vue';

interface WorkbenchModule {
  title: string;
  description: string;
  route: string;
  status: string;
  icon: Component;
}

interface DeferredModule {
  title: string;
  reason: string;
}

const primaryModules: WorkbenchModule[] = [
  {
    title: '计量单位',
    description: '维护单位代码、量纲、类型与启停状态',
    route: '/mdm/uoms',
    status: '真实 API',
    icon: ScaleToOriginal,
  },
  {
    title: '物料分类',
    description: '维护分类层级、父级关系与启停状态',
    route: '/mdm/item-categories',
    status: '真实 API',
    icon: Files,
  },
  {
    title: '物料资料',
    description: '维护物料代码、基础单位、分类与物料属性',
    route: '/mdm/items',
    status: '真实 API',
    icon: Goods,
  },
  {
    title: '往来单位',
    description: '维护客户、供应商与往来单位基础信息',
    route: '/mdm/business-partners',
    status: '真实 API',
    icon: OfficeBuilding,
  },
  {
    title: '员工档案',
    description: '按公司查看员工号、姓名、部门、关联用户与状态',
    route: '/mdm/employees',
    status: '真实 API',
    icon: UserFilled,
  },
  {
    title: '基础字典',
    description: '维护系统通用选项集、状态、分类等基础枚举数据',
    route: '/mdm/dictionaries',
    status: '真实 API',
    icon: Tickets,
  },
  {
    title: '编号规则',
    description: '维护单据类型前缀、日期格式、流水长度与启停状态',
    route: '/mdm/numbering-rules',
    status: '真实 API',
    icon: Document,
  },
  {
    title: '仓库',
    description: '维护公司范围内的仓库档案与地址信息',
    route: '/mdm/warehouses',
    status: '真实 API',
    icon: Box,
  },
  {
    title: '库位',
    description: '维护仓库下的库位、通道、货架与启停状态',
    route: '/mdm/locations',
    status: '真实 API',
    icon: Files,
  },
];

const deferredModules: DeferredModule[] = [];
</script>

<style scoped>
.mdm-workbench {
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-height: 100%;
  padding: 20px 24px 28px;
  background: var(--bg-page);
  color: var(--text-primary);
}

.mdm-workbench__header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 4px;
  border-bottom: 1px solid var(--border-subtle);
}

.mdm-workbench__header h1 {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
  line-height: 1.25;
  letter-spacing: 0;
}

.mdm-workbench__header p {
  margin: 8px 0 0;
  color: var(--text-secondary);
  font-size: 14px;
  line-height: 1.6;
}

.mdm-workbench__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 12px;
}

.mdm-workbench-card {
  display: grid;
  grid-template-columns: 38px minmax(0, 1fr) auto;
  align-items: center;
  gap: 12px;
  min-height: 96px;
  padding: 16px;
  color: inherit;
  text-decoration: none;
  background: var(--bg-container);
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
  transition: border-color 0.16s ease, box-shadow 0.16s ease, transform 0.16s ease;
}

.mdm-workbench-card:hover,
.mdm-workbench-card:focus-visible {
  border-color: var(--primary-default);
  box-shadow: var(--shadow-sm);
  outline: none;
  transform: translateY(-1px);
}

.mdm-workbench-card__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  color: var(--primary-default);
  background: var(--primary-bg);
  border: 1px solid var(--border-subtle);
  border-radius: 8px;
}

.mdm-workbench-card__icon .el-icon {
  font-size: 18px;
}

.mdm-workbench-card__body {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 6px;
}

.mdm-workbench-card__title {
  overflow: hidden;
  color: var(--text-primary);
  font-size: 16px;
  font-weight: 600;
  line-height: 1.35;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.mdm-workbench-card__desc {
  display: -webkit-box;
  overflow: hidden;
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.45;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.mdm-workbench-card__status {
  align-self: start;
  padding: 3px 8px;
  color: var(--success-default);
  background: var(--success-bg);
  border: 1px solid var(--border-subtle);
  border-radius: 999px;
  font-size: 12px;
  line-height: 1.35;
  white-space: nowrap;
}

.mdm-workbench__deferred {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.mdm-workbench__deferred h2 {
  margin: 0;
  color: var(--text-primary);
  font-size: 16px;
  font-weight: 600;
  line-height: 1.4;
}

.mdm-workbench__deferred-list {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 10px;
}

.mdm-workbench-deferred {
  display: flex;
  min-height: 58px;
  flex-direction: column;
  justify-content: center;
  gap: 4px;
  padding: 12px 14px;
  background: var(--bg-container);
  border: 1px dashed var(--border-default);
  border-radius: 8px;
}

.mdm-workbench-deferred span:first-child {
  color: var(--text-primary);
  font-weight: 600;
  line-height: 1.4;
}

.mdm-workbench-deferred span:last-child {
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.45;
}

@media (max-width: 640px) {
  .mdm-workbench {
    padding: 16px;
  }

  .mdm-workbench-card {
    grid-template-columns: 34px minmax(0, 1fr);
  }

  .mdm-workbench-card__status {
    grid-column: 2;
    justify-self: start;
  }
}
</style>
