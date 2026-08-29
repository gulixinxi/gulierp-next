export const TABLE_COLUMN_PRESETS = {
  selection: 40,
  index: 48,
  status: 90,
  // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 4
  // (2026-08-28). per brief §三十三:
  //   Code  170 (no default truncation)
  //   Name  200 (high-priority read)
  //   ShortName 130
  //   Type  120
  //   Contact 110
  //   Phone 130
  //   Email 200 (ellipsis + tooltip)
  //   TaxId 180 (ellipsis + tooltip)
  //   Status 90
  //   UpdatedAt 165
  //   Actions 130 (fixed right)
  code: 170,
  documentNo: 160,
  date: 120,
  datetime: 165,
  person: 110,
  phone: 130,
  email: 200,
  taxNo: 180,
  shortName: 130,
  type: 120,
  uom: 100,
  category: 140,
  itemNature: 104,
  spec: 168,
  money: 136,
  currency: 84,
  nameMin: 200,
  partnerName: 200,
  descriptionMin: 220,
  actions: 130,
  documentActions: 150,
  // Wave 4. optional mnemonic column. Hidden by default for the
  // BusinessPartner list to keep the visible width manageable
  // (the search still covers it). The component decides
  // whether to mount this column.
  mnemonic: 110,
} as const;

export type TableColumnPresetKey = keyof typeof TABLE_COLUMN_PRESETS;
