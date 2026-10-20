import { downloadSpreadsheet } from './spreadsheet';

/**
 * 通用导出 Excel
 * @param data 数据数组
 * @param columns 列定义：{ key: 字段名, label: 表头 }
 * @param filename 文件名（不含.xlsx）
 */
export async function exportToExcel<T extends Record<string, any>>(
  data: T[],
  columns: Array<{ key: string; label: string }>,
  filename: string
) {
  if (!data || data.length === 0) {
    alert('没有可导出的数据');
    return;
  }
  const rows = [columns.map(col => col.label), ...data.map(item =>
    columns.map(col => formatValue(item[col.key], col.key))
  )];
  await downloadSpreadsheet(
    rows,
    columns.map(col => Math.max(col.label.length * 2, 15)),
    `${filename}_${new Date().toISOString().slice(0, 10)}.xlsx`
  );
}

function formatValue(value: any, key: string): string {
  if (value === null || value === undefined || value === '') return '';
  // 枚举值转换
  if (key === 'status') {
    return ['待认领', '已认领', '已转化', '已废弃'][value] || String(value);
  }
  if (key === 'rating') {
    return ['A', 'B', 'C', 'D'][value] || String(value);
  }
  if (key === 'industry') {
    return ['机械制造', '纺织服装', '食品加工', '化工', '建材', '农业种植', '商贸流通', '服务业'][value] || '其他';
  }
  if (key === 'subjectType') {
    const map: Record<number, string> = { 0: '企业', 1: '协会/商会', 2: '服务中心', 3: '专业服务', 4: '园区/基地', 5: '政府机关', 6: '事业单位', 7: '学历教育', 8: '教育培训', 10: '个体商户/门店', 99: '其他' };
    return map[value] || '其他';
  }
  if (key === 'source') {
    return ['Excel导入', '政府公示', '行业协会', '招投标', '地图', '自媒体', '转介绍', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '其他'][value] || '其他';
  }
  if (key === 'stage') {
    return ['初步接触', '需求确认', '方案报价', '商务谈判', '成交', '丢单'][value] || String(value);
  }
  if (key === 'category') {
    return ['企业建站', '网站优化', 'AI应用', 'GEO优化', '数字员工', '地图标注', '抖音地址添加', '门店信息完善'][value] || '其他';
  }
  if (key === 'customerStatus') {
    return ['待联系', '跟进中', '已报价', '已成交', '暂搁', '无效'][value] || String(value);
  }
  // 日期格式化
  if (typeof value === 'string' && value.includes('T')) {
    return value.slice(0, 19).replace('T', ' ');
  }
  return String(value);
}
