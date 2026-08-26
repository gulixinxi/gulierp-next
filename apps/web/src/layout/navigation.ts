export type ShellModuleKey =
  | 'home'
  | 'basic'
  | 'mdm'
  | 'sales'
  | 'purchase'
  | 'inventory'
  | 'production'
  | 'quality'
  | 'system';

export interface ShellNavigationItem {
  id: string;
  label: string;
  icon: string;
  route?: string;
  action?: string;
  permissionKey?: string;
  disabled?: boolean;
  placeholder?: string;
  tabTitle?: string;
  tabKind?: 'list';
}

export interface ShellNavigationGroup {
  label: string;
  items: ShellNavigationItem[];
}

export interface ShellNavigationModule {
  key: ShellModuleKey;
  label: string;
  shortLabel: string;
  icon: string;
  permissionKey?: string;
  disabled?: boolean;
  placeholder?: string;
  groups: ShellNavigationGroup[];
}

export const shellNavigation: ShellNavigationModule[] = [
  {
    key: 'home',
    label: '工作台',
    shortLabel: '工作台',
    icon: 'HomeFilled',
    groups: [
      {
        label: '工作台',
        items: [
          { id: 'home-overview', label: '业务概览', icon: 'DataLine', disabled: true, placeholder: '业务概览待开发' },
        ],
      },
    ],
  },
  {
    key: 'basic',
    label: '基础数据',
    shortLabel: '基础',
    icon: 'Tools',
    groups: [
      {
        label: '基础数据',
        items: [
          { id: 'list-mdm-business-partners', label: '业务伙伴', icon: 'OfficeBuilding', route: '/mdm/business-partners', tabTitle: '业务伙伴' },
          { id: 'list-mdm-customers', label: '客户档案', icon: 'OfficeBuilding', route: '/mdm/customers', tabTitle: '客户档案' },
          { id: 'list-mdm-suppliers', label: '供应商', icon: 'Avatar', route: '/mdm/suppliers', tabTitle: '供应商' },
          // G3-R1D (N-1): 基础数据 > 员工档案 now points to the real
          // /mdm/employees page (the 主数据 > 员工档案 already does).
          // The page was previously marked "待开发" because the 基础数据
          // module was a placeholder before the EmployeeList page was wired
          // to the real /api/v1/organization/companies/{id}/employees API.
          { id: 'list-mdm-employees', label: '员工档案', icon: 'UserFilled', route: '/mdm/employees', tabTitle: '员工档案' },
          { id: 'list-mdm-warehouses', label: '仓库', icon: 'Box', route: '/mdm/warehouses', tabTitle: '仓库' },
          { id: 'list-mdm-locations', label: '库位', icon: 'Files', route: '/mdm/locations', tabTitle: '库位' },
        ],
      },
    ],
  },
  {
    key: 'mdm',
    label: '主数据',
    shortLabel: '主数据',
    icon: 'Files',
    groups: [
      {
        label: '主数据',
        items: [
          { id: 'mdm-workbench', label: '主数据中心', icon: 'Menu', route: '/mdm', tabTitle: '主数据中心' },
          { id: 'list-mdm-uoms', label: '计量单位', icon: 'ScaleToOriginal', route: '/mdm/uoms', tabTitle: '计量单位' },
          { id: 'list-mdm-item-categories', label: '物料分类', icon: 'Files', route: '/mdm/item-categories', tabTitle: '物料分类' },
          { id: 'list-mdm-items', label: '商品档案', icon: 'Goods', route: '/mdm/items', tabTitle: '商品档案' },
          { id: 'list-mdm-employees', label: '员工档案', icon: 'UserFilled', route: '/mdm/employees', tabTitle: '员工档案' },
          { id: 'list-mdm-dictionaries', label: '基础字典', icon: 'Tickets', route: '/mdm/dictionaries', tabTitle: '基础字典' },
          { id: 'list-mdm-numbering-rules', label: '编号规则', icon: 'Document', route: '/mdm/numbering-rules', tabTitle: '编号规则' },
        ],
      },
    ],
  },
  {
    key: 'sales',
    label: '销售管理',
    shortLabel: '销售',
    icon: 'Sell',
    groups: [
      {
        label: '销售单据',
        items: [
          { id: 'list-sales-order', label: '销售订单', icon: 'Tickets', route: '/sales/orders', tabTitle: '销售订单' },
          { id: 'sales-quotation', label: '报价单', icon: 'Document', disabled: true, placeholder: '报价单待开发' },
          { id: 'sales-delivery', label: '发货单', icon: 'Van', disabled: true, placeholder: '发货单待开发' },
          { id: 'sales-invoice', label: '销售发票', icon: 'Money', disabled: true, placeholder: '销售发票待开发' },
        ],
      },
      {
        label: '销售报表',
        items: [
          { id: 'sales-performance', label: '销售业绩', icon: 'DataLine', disabled: true, placeholder: '销售业绩待开发' },
          { id: 'sales-aging', label: '客户账龄', icon: 'TrendCharts', disabled: true, placeholder: '客户账龄待开发' },
        ],
      },
    ],
  },
  {
    key: 'purchase',
    label: '采购管理',
    shortLabel: '采购',
    icon: 'ShoppingCart',
    groups: [{ label: '采购管理', items: [{ id: 'purchase-placeholder', label: '采购功能', icon: 'ShoppingCart', disabled: true, placeholder: '采购功能待开发' }] }],
  },
  {
    key: 'inventory',
    label: '库存管理',
    shortLabel: '库存',
    icon: 'Box',
    groups: [{ label: '库存管理', items: [{ id: 'inventory-placeholder', label: '库存功能', icon: 'Box', disabled: true, placeholder: '库存功能待开发' }] }],
  },
  {
    key: 'production',
    label: '生产管理',
    shortLabel: '生产',
    icon: 'Goods',
    groups: [{ label: '生产管理', items: [{ id: 'production-placeholder', label: '生产功能', icon: 'Goods', disabled: true, placeholder: '生产功能待开发' }] }],
  },
  {
    key: 'quality',
    label: '质量管理',
    shortLabel: '质量',
    icon: 'Coin',
    groups: [{ label: '质量管理', items: [{ id: 'quality-placeholder', label: '质量功能', icon: 'Coin', disabled: true, placeholder: '质量功能待开发' }] }],
  },
  {
    key: 'system',
    label: '系统设置',
    shortLabel: '系统',
    icon: 'Setting',
    groups: [
      {
        label: '系统管理',
        items: [
          { id: 'list-system-enterprise-organization', label: '企业组织', icon: 'OfficeBuilding', route: '/system/enterprise-organization', tabTitle: '企业组织' },
        ],
      },
    ],
  },
];

export function findNavigationItemByRoute(path: string): ShellNavigationItem | undefined {
  return shellNavigation
    .flatMap(module => module.groups.flatMap(group => group.items))
    .filter(item => item.route === path || (item.route !== '/' && path.startsWith(`${item.route}/`)))
    .sort((a, b) => (b.route?.length ?? 0) - (a.route?.length ?? 0))[0];
}

export function findNavigationModuleByRoute(path: string): ShellNavigationModule | undefined {
  return shellNavigation.find(module =>
    module.groups.some(group =>
      group.items.some(item => item.route === path || (item.route !== '/' && path.startsWith(`${item.route}/`)))));
}
