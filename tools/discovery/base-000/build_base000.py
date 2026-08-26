"""BASE-000 ERP Common Configuration Inventory & Gap Discovery.

Outputs:
  1. DATABASE_SOURCE_INVENTORY.json
  2. FOUNDATION_AND_CONFIGURATION_DISCOVERY_MATRIX.json
  3. USER_NOT_YET_CONSIDERED_FINDINGS.json
  4. P0_FOUNDATION_CANDIDATES.json
  5. IMPLEMENTATION_QUEUE.json
  6. (writes docs/architecture/GULIERP_FOUNDATION_AND_BUSINESS_CONFIGURATION_MASTER_CHECKLIST.md)

READ-ONLY. Does NOT modify production code, MDM-000D, or DBs.
"""
import json
import os
import re
import sys
import io
from datetime import datetime, timezone
from collections import Counter, defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

EXTRACTION_TIME = '2026-08-20T11:46:44+08:00'
SESSION = 'mvs_11a243eed8e544d6b19087711a392283'

OUT_BASE = r'D:\guli\projects\gulierp-next\tools\discovery\base-000\_canonical'
DOC_OUT = r'D:\guli\projects\gulierp-next\docs\architecture'
os.makedirs(OUT_BASE, exist_ok=True)
os.makedirs(DOC_OUT, exist_ok=True)

DEV_META = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_normalized'
DEV_CANON = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_canonical'
VOL_DOCS = r'D:\guli\projects\gulierp-next\docs\research\vol-pro'
MIG_DIR = r'D:\guli\projects\gulierp-next\modules'


def load(p):
    with open(p, encoding='utf-8') as f:
        return json.load(f)


def write(p, data):
    os.makedirs(os.path.dirname(p), exist_ok=True)
    with open(p, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    return os.path.getsize(p)


# =============================================================================
# 1. DATABASE_SOURCE_INVENTORY
# =============================================================================
print('=== Building DATABASE_SOURCE_INVENTORY ===')
source_inventory = {
    'meta': {
        'extraction_time': EXTRACTION_TIME,
        'session': SESSION,
        'goal': 'BASE-000',
        'policy': 'READ-ONLY; no source DB writes; no DDL/DML; connection tests only',
    },
    'sources': {
        'DEV': {
            'status': 'EXTRACTED_VIA_JSON_DUMP',
            'database': 'dev (SQL Server 2012 SP1, Onlyit-derived)',
            'access_method': 'Read 22 JSON dumps + 79 ju-samples + 12 biz-samples at D:\\guli\\gulierp\\docs\\reverse-engineering\\dev-meta\\',
            'completeness': 'PARTIAL (sample TOP 5 per table from prior reverse-engineering tool)',
            'read_only_maintained': True,
            'notes': [
                '22 JSON files (ju-meta, ju-columns, biz-tables, dictionaries, templates, template-fields, relations, formulas, actions, permissions, organizations, workflows, size, etc.)',
                '79 JU_ metadata table samples (5 rows each)',
                '12 business table samples (5 rows each)',
                'Cannot re-query live DB (network reachable but password not in agent session per G2-001 design  --  read-only DUMP files are the evidence)',
            ],
        },
        'ONLYIT': {
            'status': 'NOT_AVAILABLE_AS_INDEPENDENT_SOURCE',
            'database': 'N/A',
            'access_method': 'N/A',
            'notes': [
                'ONLYIT_INDEPENDENT_SOURCE_NOT_AVAILABLE',
                'dev (SQL Server 192.168.2.28) is the Onlyit-derived source per DEV_METADATA_REVERSE_ENGINEERING_REPORT.md line 6',
                'No separate Onlyit corpus was discoverable in this session',
                'Do not fabricate a third source',
            ],
        },
        'gulierp_adminnet_poc': {
            'status': 'NOT_ACCESSED_PASSWORD_REQUIRED',
            'database': 'gulierp_adminnet_poc',
            'host': '192.168.2.228:5432',
            'network_test': 'PASS (Test-NetConnection + psycopg2 connect probe)',
            'access_method': 'Requires PGPASSWORD env var or ConnectionStrings env; not available in agent session',
            'workaround': 'dev reverse-engineering JSON dumps (ju-meta.json etc.) describe the Admin.NET POC style metadata if needed',
        },
        'gulierp_g2_001': {
            'status': 'NOT_ACCESSED_PASSWORD_REQUIRED',
            'database': 'gulierp_g2_001',
            'host': '192.168.2.228:5432',
            'network_test': 'PASS',
            'workaround': 'Migration file 20260819103150_G2001_InitializeFoundationSchema.cs shows: only creates `foundation` schema; ZERO tables (G2-001 Foundation DbContext has no DbSet per G2-001 brief).',
        },
        'gulierp_g2_003_test': {
            'status': 'PARTIALLY_ACCESSED_VIA_MIGRATION_FILES',
            'database': 'gulierp_g2_003_test',
            'host': '192.168.2.228:5432',
            'network_test': 'PASS (live DB query requires password)',
            'access_method': 'Read migration files in modules/identity/GuliERP.Identity.Infrastructure/Migrations/',
            'current_tables_observed_via_migration': [
                'identity.AspNetRoles', 'identity.AspNetUsers', 'identity.AspNetRoleClaims', 'identity.AspNetUserClaims',
                'identity.AspNetUserLogins', 'identity.AspNetUserRoles', 'identity.AspNetUserTokens',
                'identity.gulierp_tenant', 'identity.gulierp_company', 'identity.gulierp_organization_unit',
                'identity.gulierp_plant',
                'identity.gulierp_user_company_membership', 'identity.gulierp_user_organization_membership',
                'identity.gulierp_user_role_assignment',
            ],
            'strict_read_only': True,
            'note': 'live DB query blocked; migration files are the source of truth for current schema. runtime row counts need Operator evidence.',
        },
        'VOL': {
            'status': 'PATTERN_REFERENCE_ONLY',
            'database': 'N/A (no runtime DB accessible)',
            'access_method': 'docs/research/vol-pro/*.md (22 documents)',
            'notes': [
                'VOL_DATA_VALUE_SOURCE = NONE (no specific DataType/Decimal/precision table found)',
                'VOL_PATTERN_SOURCE = AVAILABLE (Sys_Dictionary + Sys_DictionaryList pattern confirmed)',
                'no runtime source code import (per governance; Admin.NET/VOL forbidden as runtime dependency)',
            ],
        },
    },
    'summary': {
        'sources_total': 6,
        'sources_extracted': 1,  # DEV
        'sources_pattern_only': 1,  # VOL
        'sources_password_blocked': 3,  # 3 PG DBs
        'sources_not_available': 1,  # ONLYIT
        'evidence_via_migration_files': 1,  # g2_003_test
    },
}
write(os.path.join(OUT_BASE, 'DATABASE_SOURCE_INVENTORY.json'), source_inventory)
print(f'  Wrote DATABASE_SOURCE_INVENTORY.json')


# =============================================================================
# Helper: load all relevant DEV data
# =============================================================================
print()
print('=== Loading DEV data (22 JSON) ===')

dev_tables = load(os.path.join(DEV_META, 'tables.json'))           # 261 user tables
dev_ju_meta = load(os.path.join(DEV_META, 'ju-meta.json'))         # 151 JU_ tables
dev_biz_summary = load(os.path.join(DEV_META, 'biz-tables-summary.json'))  # 103 business tables
dev_ju_cols = load(os.path.join(DEV_META, 'ju-columns.compact.json'))   # JU_ columns
dev_biz_cols = load(os.path.join(DEV_META, 'biz-tables.compact.json'))  # business columns
dev_dict = load(os.path.join(DEV_META, 'dictionaries.json'))       # 149 dict rows
dev_dict_canon = load(os.path.join(DEV_CANON, 'dev_dictionary_canonical.json'))  # 28 headers + 121 items
dev_templates = load(os.path.join(DEV_META, 'templates.json'))     # 182 templates
dev_template_fields = load(os.path.join(DEV_META, 'template-fields.json'))  # 6339 fields
dev_relations = load(os.path.join(DEV_META, 'relations.json'))     # 126
dev_formulas = load(os.path.join(DEV_META, 'formulas.json'))       # rules
dev_actions = load(os.path.join(DEV_META, 'actions.json'))         # buttons/actions
dev_perms = load(os.path.join(DEV_META, 'permissions.json'))       # 14 users, 35 roles, 4 orgs, 80 posts, 1569+1146 perms
dev_orgs = load(os.path.join(DEV_META, 'organizations.json'))
dev_workflows = load(os.path.join(DEV_META, 'workflows.json'))
dev_size = load(os.path.join(DEV_META, 'size.json'))
dev_views = load(os.path.join(DEV_META, 'all-views.json'))
dev_procs = load(os.path.join(DEV_META, 'all-procs.json'))
dev_funcs = load(os.path.join(DEV_META, 'all-functions.json'))
dev_autocode = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_AutoCode.json'))
dev_data_type = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_DataType.json'))
dev_query = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_Query.json'))
dev_ju_select_rule = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_SelectRule.json'))
dev_ju_update_rule = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_UpdateRule.json'))
dev_ju_ctrl_rule = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_CtrlRule.json'))
dev_ju_view = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_View.json'))
dev_ju_view_field = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_ViewField.json'))
dev_ju_seed = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_Seed.json'))
dev_ju_combo = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_Combo.json'))
dev_ju_combo_data = load(os.path.join(DEV_META, 'ju-samples', 'dbo_JU_ComboDataItem.json'))
dev_biz_status = load(os.path.join(DEV_META, 'biz-samples', 'dbo_状态表.json'))  # 状态表 = status table
dev_biz_item = load(os.path.join(DEV_META, 'biz-samples', 'dbo_商品表.json'))  # 商品表 = item table
dev_biz_partner = load(os.path.join(DEV_META, 'biz-samples', 'dbo_往来表.json'))  # 往来表 = BP table
dev_biz_warehouse = load(os.path.join(DEV_META, 'biz-samples', 'dbo_系统表.json'))  # system table headers
dev_biz_category = load(os.path.join(DEV_META, 'biz-samples', 'dbo_分类表.json'))  # 分类表 = category

print(f'  Loaded: {len(dev_tables)} tables, {len(dev_templates)} templates, {len(dev_template_fields)} template-fields, {len(dev_relations)} relations')
print(f'          {len(dev_perms)} perms, {len(dev_dict_canon["dictionaries"])} dicts, {len(dev_views)} views, {len(dev_procs)} procs')

# Extract from migration: GULIERP_CURRENT Identity tables
def load_migration_tables(path):
    with open(path, encoding='utf-8') as f:
        text = f.read()
    return re.findall(r'migrationBuilder\.CreateTable\(\s*name:\s*"([^"]+)"(?:,\s*schema:\s*"([^"]+)")?', text)

mig_v1 = load_migration_tables(rf'{MIG_DIR}\identity\GuliERP.Identity.Infrastructure\Migrations\20260819150708_G2003_InitializeIdentitySchema.cs')
mig_v2 = load_migration_tables(rf'{MIG_DIR}\identity\GuliERP.Identity.Infrastructure\Migrations\20260819162500_G2003V2_AddIdentityReferentialIntegrity.cs')

# =============================================================================
# 2. FOUNDATION_AND_CONFIGURATION_DISCOVERY_MATRIX
# =============================================================================
print()
print('=== Building FOUNDATION_AND_CONFIGURATION_DISCOVERY_MATRIX ===')

discovery = []

def add(area, capability, chinese_name, category, source_evidence, decision, priority, target_phase, notes):
    discovery.append({
        'id': f'B-{len(discovery)+1:03d}',
        'area': area,
        'capability': capability,
        'chinese_name': chinese_name,
        'category': category,
        'source_evidence': source_evidence,
        'decision': decision,
        'priority': priority,
        'target_phase': target_phase,
        'notes': notes,
    })

# =================================================================
# A. Business Semantic Data Types (12 types from MDM-000D)
# =================================================================
add('01 Business Semantic Data Types', 'AMOUNT', '金额', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'JU_DataType DataTypeID=4 (小数), BaseType=3, BaseLength=34, BasePrecision=2, MatchPattern=金额,汇率,总计,小数',
     'GULIERP_CURRENT': 'Foundation.G2-002 has zero tables; no decimal metadata yet',
     'VOL': 'pattern reference only'},
    'ADAPT', 'P0_NOW', 'BASE-001',
    'Already proposed in MDM-000D semantic-data-type.json. numeric(20,4) HALF_EVEN currency-aware. Will affect all money columns in Sales/Purchase/Inventory.')

add('01 Business Semantic Data Types', 'UNIT_PRICE', '单价', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'shared with AMOUNT (JU_DataTypeID=4), not separated',
     'GULIERP_CURRENT': 'not yet modeled',
     'MDM-000D': 'PROPOSED in semantic-data-type.json, precision 6'},
    'PROPOSED', 'P0_NOW', 'BASE-001',
    'Distinguish from AMOUNT to support tax-precision calculations.')

add('01 Business Semantic Data Types', 'COST', '成本', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'no dedicated row; computed field',
     'MDM-000D': 'PROPOSED'},
    'PROPOSED', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Cost method (FIFO/Weighted Avg/Standard) determines field presence; defer definition until Inventory/Production.')

add('01 Business Semantic Data Types', 'QUANTITY', '数量', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'JU_DataTypeID=4; 商品表.数量=1.0; 克重=0.0; 1 decimal in DEV',
     'MDM-000D': 'PROPOSED, per-uom precision'},
    'ADAPT', 'P0_NOW', 'BASE-001',
    'DIMENSION_AWARE: precision varies by UOM (kg=0.001, EA=1, M=0.001). HALF_UP rounding.')

add('01 Business Semantic Data Types', 'TAX_RATE', '税率', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'no dedicated row; 阶梯类型 dictionary 按数量/按金额',
     'China tax': '13% / 9% / 6% / 3% VAT + historical 17%/13%'},
    'PROPOSED', 'P0_NOW', 'BASE-001',
    'numeric(6,4) HALF_EVEN. China VAT granularity 0.0001 (0.01%).')

add('01 Business Semantic Data Types', 'PERCENTAGE', '百分比', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'no dedicated row',
     'MDM-000D': 'PROPOSED'},
    'PROPOSED', 'P1_BEFORE_MODULE', 'SALES',
    'numeric(8,6) for discount/commission.')

add('01 Business Semantic Data Types', 'DISCOUNT_RATE', '折扣率', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'no dedicated row; 阶梯类型 dictionary 按数量/按金额',
     'MDM-000D': 'PROPOSED'},
    'PROPOSED', 'P1_BEFORE_MODULE', 'SALES',
    'numeric(6,4) HALF_UP. 0-1.00 or 0-100.')

add('01 Business Semantic Data Types', 'EXCHANGE_RATE', '汇率', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'JU_DataTypeID=4 MatchPattern contains 汇率',
     'MDM-000D': 'ADAPT, precision 8'},
    'ADAPT', 'P0_NOW', 'BASE-001',
    'numeric(18,8) HALF_EVEN. FX 6-8 decimal precision required.')

add('01 Business Semantic Data Types', 'LENGTH', '长度', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'DEV has 克重/外箱尺寸/中盒尺寸/内盒尺寸 as strings',
     'ISO 80000-3': 'SI base unit m + mm/cm/km'},
    'PROPOSED', 'P1_BEFORE_MODULE', 'INVENTORY',
    'UOM=LENGTH dimension. mm precision floor.')

add('01 Business Semantic Data Types', 'WEIGHT', '重量', 'SEMANTIC_DATA_TYPE',
    {'DEV': '商品表.克重=0.0',
     'ISO 80000-4': 'SI base unit kg + g/t'},
    'ADAPT', 'P1_BEFORE_MODULE', 'INVENTORY',
    'UOM=MASS dimension. g precision floor.')

add('01 Business Semantic Data Types', 'AREA', '面积', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'no explicit type',
     'ISO 80000-3': 'm² + cm²/km²'},
    'PROPOSED', 'P2_WITH_MODULE', 'PRODUCTION',
    'UOM=AREA. Used in Production capacity calculations.')

add('01 Business Semantic Data Types', 'VOLUME', '体积', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'no explicit type',
     'ISO 80000-4': 'm³ + L'},
    'PROPOSED', 'P2_WITH_MODULE', 'PRODUCTION',
    'UOM=VOLUME. L precision 0.001.')

add('01 Business Semantic Data Types', 'TIME_DURATION', '时长', 'SEMANTIC_DATA_TYPE',
    {'DEV': '生产用时 0.0; 提前期 0',
     'ISO 8601': 'duration string + interval type'},
    'PROPOSED', 'P1_BEFORE_MODULE', 'INVENTORY',
    'TimeSpan / interval. Used in production hours, lead time, payment terms.')

# =================================================================
# B. Decimal Precision / Rounding
# =================================================================
add('02 Decimal / Precision / Rounding', 'CURRENCY_DEFAULT_PRECISION', '货币默认精度', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'JU_DataTypeID=4 BasePrecision=2, no separate scale column',
     'Industry': 'SAP/Oracle use 2-4 decimal places for currency; cross-currency needs HALF_EVEN'},
    'ADAPT', 'P0_NOW', 'BASE-001',
    'Default currency scale=2 (display), scale=4 (storage HALF_EVEN) for cross-currency aggregation safety.')

add('02 Decimal / Precision / Rounding', 'ROUNDING_MODE_DEFAULT', '默认舍入模式', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'JU_DataType BaseType=3 has no rounding mode; depends on client app',
     'ISO 20022 + banking standard': 'HALF_EVEN (banker\'s rounding) for currency; HALF_UP for quantity'},
    'NEW_BUILD', 'P0_NOW', 'BASE-001',
    'Per-type rounding policy: AMOUNT/EXCHANGE_RATE= HALF_EVEN; QUANTITY= HALF_UP; RATE/PERCENTAGE= HALF_EVEN.')

add('02 Decimal / Precision / Rounding', 'JSON_NUMERIC_SAFE', 'JSON 数字传输', 'SEMANTIC_DATA_TYPE',
    {'DEV': 'field is decimal in DB but serialised via ntext/image in some legacy fields',
     'GULIERP_CURRENT': 'System.Text.Json by default for decimal  --  OK'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'Confirm System.Text.Json default serialisation for decimal/numeric avoids scientific notation. Add JsonConverter if needed.')

# =================================================================
# C. Dictionary / Reference Data Convention
# =================================================================
add('03 Dictionary / Reference Data', 'DICTIONARY_REFERENCE_TABLE', '字典参考表', 'REFERENCE_DATA',
    {'DEV': '字典表 + 字典表s (28 headers + 121 items, see MDM-000D R1)',
     'MDM-000D': 'system/ + tenant-template/ seed candidates prepared'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'MDM-000',
    'Use ReferenceData entity pattern (per MDM-000 plan): ReferenceDataDefinition + ReferenceDataItem; sys vs tenant template.')

add('03 Dictionary / Reference Data', 'DICTIONARY_CATEGORY', '字典分类', 'REFERENCE_DATA',
    {'DEV': '28 dict headers (单位/商品属性/付款方式/学历/工序/工作中心/位置/部门/班组/...)',
     'GULIERP_CURRENT': 'not modeled'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'MDM-000',
    'ReferenceDataDefinition.Category enum or string: SYSTEM_REFERENCE / TENANT_TEMPLATE / MASTER_DATA.')

add('03 Dictionary / Reference Data', 'DICTIONARY_INHERITANCE', '字典继承', 'REFERENCE_DATA',
    {'DEV': 'no explicit inheritance in DEV (28 headers are flat)',
     'VOL': 'Sys_Dictionary + Sys_DictionaryList pattern (parent-child)'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'MDM-000',
    'Allow ReferenceData to inherit from another (e.g. 行业子类继承行业父类). If not needed, defer.')

add('03 Dictionary / Reference Data', 'DICTIONARY_HISTORY', '字典历史', 'REFERENCE_DATA',
    {'DEV': 'DEV dictionary has CreateTime + LastEditTime on each header',
     'Industry': 'SF/Odoo maintain change history for all dictionaries'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'MDM-000',
    'If a dictionary value changes, business data that referenced the old code may break. Audit log needed (IAuditWriter already planned in G2-002).')

# =================================================================
# D. UOM
# =================================================================
add('04 UOM', 'UOM_DIMENSION', '单位维度', 'REFERENCE_DATA',
    {'DEV': '13 unit items (本/套/张/台/个/PCS/EA/t/kg/g/m/m2/m3)  --  no dimension',
     'ISO 80000': 'LENGTH / MASS / VOLUME / TIME / COUNT / AREA / TEMPERATURE / ELECTRIC / CURRENCY'},
    'NEW_BUILD', 'P0_NOW', 'MDM-000',
    'Add UomDimension enum. Each Uom must declare its dimension. Mixed dimensions in single field is anti-pattern.')

add('04 UOM', 'UOM_CONVERSION', '单位换算', 'REFERENCE_DATA',
    {'DEV': '商品表.换算单位/换算系数/换算公式  --  per-item only, no global table',
     'Industry': 'UOM Conversion table: from_code + to_code + multiplier + valid_from/to'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Decision: GLOBAL conversion (kg<->g, m<->cm) or ITEM-LEVEL conversion (1 box = 12 ea for item A). DEV has item-level only; GuliERP should support both.')

add('04 UOM', 'BASE_UOM_PER_ITEM', '物料基本单位', 'REFERENCE_DATA',
    {'DEV': '商品表.单位 (single UOM, not base vs issue UOM)',
     'Industry': 'Item has BaseUom + multiple IssueUom + conversion ratios'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Each Item must declare ONE BaseUom (stocking Uom) and may have multiple IssueUom (sales Uom).')

# =================================================================
# E. Currency / Exchange Rate
# =================================================================
add('05 Currency / Exchange Rate', 'CURRENCY_TABLE', '币种表', 'REFERENCE_DATA',
    {'DEV': 'no dedicated currency table; 系统表.货币 field empty (0 chars)',
     'ISO 4217': '156 currencies, 3-char alphabetic + numeric code'},
    'REUSE_DATA', 'P1_BEFORE_MODULE', 'MDM-000',
    'MDM-000D R1 prepared system/currency.json (20 ISO 4217 currencies). Expand to 156 in V2.')

add('05 Currency / Exchange Rate', 'EXCHANGE_RATE_TABLE', '汇率表', 'REFERENCE_DATA',
    {'DEV': 'no exchange rate table; rates hardcoded per transaction',
     'Industry': 'Daily rates: from_currency + to_currency + rate + valid_from + valid_to + source'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'MDM-000',
    'ExchangeRate entity: need rate_date, source, manual override flag, rate_type (spot/avg/buying/selling).')

add('05 Currency / Exchange Rate', 'EXCHANGE_RATE_PROVIDER', '汇率提供方', 'REFERENCE_DATA',
    {'DEV': 'not modeled',
     'Industry': 'ECB / NB / XE.com / OpenExchangeRates'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'MDM-000',
    'Interface IExchangeRateProvider; pluggable; manual entry as fallback.')

# =================================================================
# F. Administrative Division
# =================================================================
add('06 Administrative Division', 'COUNTRY_TABLE', '国家表', 'REFERENCE_DATA',
    {'DEV': 'no country table',
     'ISO 3166-1': 'alpha-2/alpha-3/numeric + name_en/name_local'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'MDM-000',
    'MDM-000D R1 prepared system/country.json (0 items, schema only). Runtime load from external source.')

add('06 Administrative Division', 'ADMINISTRATIVE_DIVISION', '行政区划', 'REFERENCE_DATA',
    {'DEV': 'no region/province/city table',
     'GB/T 2260': 'PRC 行政区划代码 (yearly update)'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'MDM-000',
    'Province/City/County hierarchy. Tenant-level addresses must select from controlled vocabulary.')

add('06 Administrative Division', 'ADDRESS_FORMAT', '地址格式', 'REFERENCE_DATA',
    {'DEV': 'no standardized address format',
     'Industry': 'SF/Odoo use multi-line AddressLine1/AddressLine2/City/Region/Country/PostalCode (already in gulierp_plant)'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'MDM-000',
    'Reuse G2-003 Plant.AddressLine1/AddressLine2/City/Region/CountryCode/PostalCode shape for BusinessPartner.Address.')

# =================================================================
# G. Document Numbering
# =================================================================
add('07 Document Numbering', 'AUTO_NUMBER_GENERATOR', '单号生成器', 'NUMBERING',
    {'DEV': 'JU_AutoCode + JU_AutoCodeField + JU_AutoCodeRegister (15+5+12 rows)',
     'GULIERP_CURRENT': 'POC-003 IBusinessNumberGenerator implemented in Foundation.Kernel (BusinessNumberPattern)'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-002',
    'Foundation.Kernel already has IBusinessNumberGenerator + BusinessNumberPattern (POC-003). Keep contract. Audit prefix-date-seed structure.')

add('07 Document Numbering', 'NUMBER_PREFIX', '单号前缀', 'NUMBERING',
    {'DEV sample': '商品ID: Prefix=1, DateType=null, SeedLength=6; 客户ID: Prefix=2, DateType=null, SeedLength=4; etc.',
     'Observed': 'Prefix as fixed string (numbers like "1", "2", "25" for codes), DateType=6 means "year"; SeedLength is total digit count'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-002',
    'Document: prefix=SO, year=2026, serial=000001. Pattern format: {PREFIX}-{YYYY}{MM}-{SEQ:6}. DEV uses simpler "PREFIX + year + serial".')

add('07 Document Numbering', 'RESET_POLICY', '编号重置策略', 'NUMBERING',
    {'DEV': 'JU_AutoCode has no explicit reset field',
     'Industry': 'DAILY / MONTHLY / YEARLY / NEVER'},
    'NEW_BUILD', 'P0_NOW', 'BASE-002',
    'NumberingPattern.ResetPolicy enum. Default YEARLY for SO/PO/GR; NEVER for customer code.')

add('07 Document Numbering', 'MANUAL_NUMBER', '手工单号', 'NUMBERING',
    {'DEV': 'JU_AutoCode.AllowMore=0 means no manual',
     'Industry': 'Allow operator to enter manual number for migrated documents'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'Some legacy documents have manual numbers (e.g. imported from old ERP). Need ManualNumberAllowed flag + conflict check.')

add('07 Document Numbering', 'NUMBER_SCOPE', '编号范围', 'NUMBERING',
    {'DEV': 'JU_AutoCode has no TenantId/CompanyId field  --  global per system',
     'Industry': 'per-tenant or per-company sequence to avoid collision'},
    'NEW_BUILD', 'P0_NOW', 'BASE-002',
    'GuliERP must scope NumberSequence by TenantId (and optionally CompanyId for multi-company tenant). DEV mistake to copy.')

add('07 Document Numbering', 'NUMBER_DEDICATED_RANGE', '专用号段', 'NUMBERING',
    {'DEV': 'no concept of dedicated range',
     'Industry': 'Reserve a range for a special purpose (e.g. test data, integration)'},
    'DEFER', 'P3_DEFER', 'IMPLEMENT_WITH_MODULE',
    'Low priority. Can add later if integration testing needs.')

# =================================================================
# H. Master Data Coding
# =================================================================
add('08 Master Data Coding', 'ITEM_CODE_FORMAT', '物料编码', 'CODING',
    {'DEV sample': '商品表.品号 = "1000001" (pure numeric, 7 digits)',
     'Industry': 'Category-Prefix-Serial (e.g. FG-001, RM-002, SF-001)'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Item.Code format: STRUCTURED code OR GUID. Avoid pure sequential (DEV mistake). Recommend: Code+Name+CategoryCode hierarchy.')

add('08 Master Data Coding', 'BP_CODE_FORMAT', '业务伙伴编码', 'CODING',
    {'DEV sample': '往来表.往来号 = "20001" "20002" (pure numeric)',
     'MDM-000D': 'BPT_CUSTOMER / BPT_SUPPLIER etc.'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'MDM-000',
    'BP.Code structured: {TYPE_PREFIX}-{SERIAL} e.g. CUST-001, SUPP-001, SUB-001, LOG-001. Pure numeric is DEV mistake.')

add('08 Master Data Coding', 'WAREHOUSE_CODE_FORMAT', '仓库编码', 'CODING',
    {'DEV': '仓库表.仓库 (string)',
     'GULIERP_CURRENT': 'gulierp_plant has Code varchar(40)'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Warehouse.Code: WH-{PLANT}-{SEQ}. Need Location below Warehouse as separate entity.')

add('08 Master Data Coding', 'WORK_CENTER_CODE_FORMAT', '工作中心编码', 'CODING',
    {'DEV dictionary': '工作中心 dictionary (RecordID=71) has 3 items: 主线车间/配件车间/包装车间',
     'DEV': 'no structured code field'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'PRODUCTION',
    'WorkCenter.Code: WC-{PLANT}-{SEQ}. Promote from dictionary to first-class entity (per P1-005 plan).')

# =================================================================
# I. Tax Configuration
# =================================================================
add('09 Tax Configuration', 'TAX_CODE', '税码', 'TAX',
    {'DEV': 'no dedicated tax code table; 收支科目 has 5 types (资产/负债/权益/成本/损益)',
     'China VAT': '13% / 9% / 6% / 3% / exempt / zero-rated'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'TaxCode entity: code + name + rate + type (VAT/Consumption/Income/Withholding) + effective_from/to.')

add('09 Tax Configuration', 'TAX_INCLUSIVE_EXCLUSIVE', '含税/不含税', 'TAX',
    {'DEV': 'no explicit tax-inclusive flag on lines',
     'China accounting': 'amount can be tax-inclusive or tax-exclusive (per 小规模 vs 一般纳税人)'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'Critical: same numeric value means different amount based on inclusive flag. Each SalesOrderLine and PurchaseOrderLine needs TaxInclusive flag.')

add('09 Tax Configuration', 'TAX_ROUNDING', '税额舍入', 'TAX',
    {'DEV': 'no explicit',
     'China tax law': '税额四舍五入 to 0.01'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'Tax amount rounds to 2 decimals HALF_UP. TaxRate * Amount may produce 0.005 which must round to 0.01.')

add('09 Tax Configuration', 'INPUT_OUTPUT_TAX_SEPARATION', '进项销项税分离', 'TAX',
    {'DEV': 'single 总金额 field, no separation',
     'China accounting': '进项税 (input tax, recoverable) vs 销项税 (output tax, owed) must be separate GL accounts'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FINANCE',
    'TaxTransaction entity: input/output + VAT type + amount + period (会计期间). Finance module scope.')

# =================================================================
# J. Payment
# =================================================================
add('10 Payment', 'PAYMENT_METHOD', '付款方式', 'REFERENCE_DATA',
    {'DEV dictionary': '付款方式 5 items: 款到发货/货到付款/月结30天/月结60天/月结90天',
     'MDM-000D R1': 'tenant-template/payment-method.json (5 items, split into PM_* and PT_*)'},
    'REUSE_DATA', 'P0_NOW', 'MDM-000',
    'Already in MDM-000D. PaymentMethod (channel) and PaymentTerm (duration) split. 5 items seed.')

add('10 Payment', 'PAYMENT_TERM_DAYS', '账期天数', 'REFERENCE_DATA',
    {'DEV': '月结30天 (30 days), 月结60天 (60), 月结90天 (90)',
     'Industry': 'NET_0 / NET_15 / NET_30 / NET_45 / NET_60 / COD / PREPAID / EOM+15 / 2MFI etc.'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'SALES',
    'PaymentTerm as int days + semantic (NET, COD, PREPAID, EOM+N). Default NET_30.')

add('10 Payment', 'PAYMENT_TERM_DAY_OF_MONTH', '付款日期', 'REFERENCE_DATA',
    {'DEV': 'no concept of "15th of every month"',
     'Industry': 'EOM (end of month), 15th, specific day-of-month'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'SALES',
    'Some terms are "15th of next month". Add PaymentTermType enum: NET_DAYS / FIXED_DAY_OF_MONTH / END_OF_MONTH.')

add('10 Payment', 'OVERDUE_POLICY', '逾期政策', 'REFERENCE_DATA',
    {'DEV': 'no overdue handling',
     'Industry': 'late fee %, dunning letter, credit hold'},
    'DEFER', 'P3_DEFER', 'IMPLEMENT_WITH_MODULE',
    'Low priority. Defer to AR module.')

# =================================================================
# K. Delivery / Shipping
# =================================================================
add('11 Delivery / Shipping', 'INCOTERMS', '国际贸易术语', 'REFERENCE_DATA',
    {'DEV': 'no Incoterms',
     'Incoterms 2020': 'EXW / FOB / CIF / DAP / DDP etc.'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'SALES',
    'SalesOrder.IncotermCode (EXW/FOB/CIF/DAP/DDP). Per international trade; Chinese domestic may not need.')

add('11 Delivery / Shipping', 'SHIPPING_CARRIER', '承运商', 'REFERENCE_DATA',
    {'DEV': 'no carrier table',
     'Industry': 'SF Express / YTO / Yunda / JD Logistics / self-delivery'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'SALES',
    'BPT_LOGISTICS as BusinessPartner. Carrier-specific service level.')

add('11 Delivery / Shipping', 'SHIPMENT_STATUS', '发货状态', 'STATUS',
    {'DEV': '状态表 has 创建/已审核/已下单/已生产; not specifically shipment',
     'Industry': 'PENDING / PICKED / PACKED / SHIPPED / IN_TRANSIT / DELIVERED / RETURNED'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Shipment entity: ShipmentStatus enum. Foundation G2-002 has AggregateState pattern.')

# =================================================================
# L. Price / Discount
# =================================================================
add('12 Price / Discount', 'PRICE_LIST', '价格表', 'REFERENCE_DATA',
    {'DEV': '商品表.单价 (single field)',
     'Industry': 'PriceList (Standard / VIP / Volume / Promotional) with validity period'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'PriceList entity + PriceListLine (Item + Uom + MinQty + Price + valid_from/to). DEV mistake to have single price.')

add('12 Price / Discount', 'TIERED_PRICING', '阶梯价', 'REFERENCE_DATA',
    {'DEV dictionary': '阶梯类型 (RecordID=2978) has 2 items: 按数量/按金额',
     'Industry': 'Tier-based pricing: 1-9 @ 100, 10-99 @ 95, 100+ @ 90'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'PriceListLine can have TierType (BY_QUANTITY / BY_AMOUNT) + tiers[]. DEV has dictionary entry but no schema.')

add('12 Price / Discount', 'CURRENCY_IN_PRICE', '价格币种', 'REFERENCE_DATA',
    {'DEV': 'price in default currency only',
     'Industry': 'PriceList has CurrencyCode + multiple Currency-specific PriceList'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'PriceList.CurrencyCode. Item may have different price in different currencies.')

# =================================================================
# M. Warehouse / Location Type
# =================================================================
add('13 Warehouse / Location Type', 'WAREHOUSE_TYPE', '仓库类型', 'REFERENCE_DATA',
    {'DEV': '仓库表.仓库类型 (string)',
     'Industry': 'RAW / WIP / FINISHED / TRANSIT / QUARANTINE / VIRTUAL'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'WarehouseType enum. WIP separates raw material from finished goods for manufacturing.')

add('13 Warehouse / Location Type', 'LOCATION_TYPE', '库位类型', 'REFERENCE_DATA',
    {'DEV': 'no location table',
     'Industry': 'RECEIVING / STORAGE / PICKING / SHIPPING / QUARANTINE / INSPECTION'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'LocationType enum. Already designed in MDM-000 P1-005 spec.')

add('13 Warehouse / Location Type', 'BIN_LOT_TRACKING', '库位批次跟踪', 'REFERENCE_DATA',
    {'DEV': 'no concept',
     'Industry': 'Bin-level tracking vs Lot-level vs Both'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'WarehousePolicy: LotTracking, BinTracking, ExpiryTracking booleans per item/warehouse.')

# =================================================================
# N. Inventory Status
# =================================================================
add('14 Inventory Status', 'INVENTORY_STATUS', '库存状态', 'STATUS',
    {'DEV': '状态表 has 创建/已审核/已下单/已生产  --  but not specific inventory status',
     'Industry': 'AVAILABLE / RESERVED / PENDING_INSPECTION / BLOCKED / QUARANTINED / IN_TRANSIT / DAMAGED'},
    'NEW_BUILD', 'P0_NOW', 'INVENTORY',
    'InventoryBalance.OnHand/Reserved/Available/Blocked/Quarantined. Already designed in POC-002/003 plan.')

add('14 Inventory Status', 'INVENTORY_BLOCK_REASON', '库存锁定原因', 'STATUS',
    {'DEV': 'no block reason',
     'Industry': 'QualityHold / CustomerReturn / LegalHold / CycleCount'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'INVENTORY',
    'InventoryBlock entity: ItemId + WarehouseId + BlockReason + BlockedAt + BlockedBy + ReleasedAt.')

# =================================================================
# O. Inventory Transaction / Reason Code
# =================================================================
add('15 Inventory Transaction / Reason Code', 'INVENTORY_TX_REASON', '库存事务原因', 'STATUS',
    {'DEV': 'no dedicated reason code table',
     'MDM-000 plan': 'InventoryTxType (Receipt/Issue/Transfer/Adjust/Reserve/Unreserve) + InventoryTxReason (PO_GR/SO_SH/...)'},
    'NEW_BUILD', 'P0_NOW', 'INVENTORY',
    'InventoryTransaction.InventoryTxType + InventoryTxReason enums. Already designed in POC-002.')

add('15 Inventory Transaction / Reason Code', 'STOCKTAKE_VARIANCE_REASON', '盘点差异原因', 'STATUS',
    {'DEV': 'no',
     'Industry': 'EVAPORATION / SHRINKAGE / MISCOUNT / DAMAGE / EXPIRED'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'INVENTORY',
    'StockTake variance reason code. Audit trail required.')

add('15 Inventory Transaction / Reason Code', 'ADJUSTMENT_REQUIRES_APPROVAL', '调整需审批', 'RULE',
    {'DEV': 'manual',
     'Industry': 'Any inventory adjustment above threshold requires approval'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'IInventoryService.AdjustAsync(ApprovalId)  --  adjust must carry approval_id (per MDM-000 P1-005).')

# =================================================================
# P. Lot / Serial Number
# =================================================================
add('16 Lot / Serial Number', 'LOT_NUMBER', '批次号', 'REFERENCE_DATA',
    {'DEV': '批次库存 -- a view, not a real entity', 'Industry': 'LotNumber = string, ExpiryDate = date, ManufactureDate = date, SupplierLotCode = string'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'InventoryLot entity. LotNumber string (no internal structure). Track per (Item, Warehouse).')

add('16 Lot / Serial Number', 'SERIAL_NUMBER', '序列号', 'REFERENCE_DATA',
    {'DEV': 'no concept',
     'Industry': 'Each unit of an item has unique SN. Used for warranty, recall, traceability.'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'INVENTORY',
    'InventorySerialNumber entity. Optional per item (Item.IsSerialControlled = true).')

add('16 Lot / Serial Number', 'EXPIRY_DATE', '效期管理', 'REFERENCE_DATA',
    {'DEV': '克重 -- 0.0 default; no expiry',
     'Industry': 'FEFO (First-Expiry-First-Out) for food/pharma'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Lot.ExpiryDate + Item.IsExpiryControlled. Required for FEFO issuing.')

# =================================================================
# Q. Item Type / Category
# =================================================================
add('17 Item Type / Category', 'ITEM_TYPE', '物料类型', 'REFERENCE_DATA',
    {'DEV dictionary': '商品属性 (RecordID=98) 4 items: 自制/外购/委外加工/客供',
     'Industry': 'Stockable / Non-Stockable / Service / Phantom (BOM) / Subassembly'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Item.ItemType enum. Phantom needed for BOM subassemblies. Recommend 5 types.')

add('17 Item Type / Category', 'ITEM_CATEGORY', '物料分类', 'REFERENCE_DATA',
    {'DEV dictionary': '商品分类 (RecordID=2859) 5 items: 电器/车辆/蔬菜/肉类/海鲜 (sample data, not real categories)',
     'Industry': 'Multi-level category tree with parent_id'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'ItemCategory tree entity. Allow 3-4 levels deep. Per-tenant customizable.')

add('17 Item Type / Category', 'ITEM_DEFAULT_UOM', '物料默认单位', 'REFERENCE_DATA',
    {'DEV': '商品表.单位 -- string reference to UOM',
     'Industry': 'Each item has one base UOM (for stock) + multiple issue UOMs'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Item.BaseUomId (FK to Uom). Plus ItemUomConversion table for sales/purchase units.')

# =================================================================
# R. Item Attribute
# =================================================================
add('18 Item Attribute', 'ITEM_ATTRIBUTE_COLOR', '颜色', 'REFERENCE_DATA',
    {'DEV sample: 商品表': 'o explicit Color field; implicit via description',
     'Industry': 'Item attribute (Color, Size, Material, etc.) for SKU variants'},
    'DEFER', 'P2_WITH_MODULE', 'INVENTORY',
    'AttributeValue table. Defer until SKU variant engine is needed (per P1-005 plan deferred).')

add('18 Item Attribute', 'ITEM_ATTRIBUTE_MATERIAL', '材质', 'REFERENCE_DATA',
    {'DEV': '商品表.材料 -- exists as string, no controlled vocabulary',
     'Industry': 'Steel / Plastic / Wood / Aluminum etc.'},
    'DEFER', 'P2_WITH_MODULE', 'INVENTORY',
    'Same as color. Defer to SKU variant phase.')

add('18 Item Attribute', 'ITEM_ATTRIBUTE_SIZE', '规格', 'REFERENCE_DATA',
    {'DEV': '商品表.规格 -- exists as string',
     'Industry': 'size dimensions or SKUs'},
    'DEFER', 'P2_WITH_MODULE', 'INVENTORY',
    'Same. Defer to SKU variant phase.')

add('18 Item Attribute', 'ITEM_ATTRIBUTE_GRADE', '等级', 'REFERENCE_DATA',
    {'DEV dictionary': '物料级别 (RecordID=2749) 3 items: A/B/C',
     'Industry': 'Quality grade A/B/C for raw materials'},
    'REUSE_PATTERN', 'P2_WITH_MODULE', 'INVENTORY',
    'Could promote dictionary to ItemGrade. Low priority  --  defer.')

# =================================================================
# S. Packaging
# =================================================================
add('19 Packaging / Item UOM', 'PACKAGING_UNIT', '包装单位', 'REFERENCE_DATA',
    {'DEV': '商品表 -- no packaging field',
     'Industry': 'Box/Pallet/Container with quantity per package'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'INVENTORY',
    'ItemPackage entity: PackageType + QuantityPerPackage + Dimensions (L×W×H) + Weight.')

# =================================================================
# T. Barcode
# =================================================================
add('20 Barcode', 'BARCODE_FORMAT', '条码格式', 'REFERENCE_DATA',
    {'DEV': '商品表 -- no barcode field',
     'Industry': 'EAN-13 / EAN-8 / UPC-A / UPC-E / Code 128 / QR / DataMatrix'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'INVENTORY',
    'ItemBarcode entity: BarcodeType + Value + IsPrimary. Multiple barcodes per item possible.')

# =================================================================
# U. Business Partner Classification
# =================================================================
add('21 Business Partner Classification', 'BP_TYPE', '业务伙伴类型', 'REFERENCE_DATA',
    {'DEV dictionary': '往来类型 (RecordID=3497) 4 items: 客户/供应商/外协/物流',
     'MDM-000D R1': 'tenant-template/business-partner-type.json (4 items)'},
    'REUSE_DATA', 'P0_NOW', 'MDM-000',
    'Already in MDM-000D. 4 types seed.')

add('21 Business Partner Classification', 'BP_LEVEL', '业务伙伴级别', 'REFERENCE_DATA',
    {'DEV sample: 往来表': '别 (string)',
     'Industry': 'A / B / C / Strategic / Regular / Transactional'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'BP.Level enum or controlled vocab. Affects payment terms and credit limit.')

add('21 Business Partner Classification', 'BP_CREDIT_LIMIT', '信用额度', 'REFERENCE_DATA',
    {'DEV sample: 往来表.额度': '.0',
     'Industry': 'Credit limit + available + on-hold'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'BusinessPartnerCreditLimit: BPId + Currency + Limit + Used + Available. AR module.')

# =================================================================
# V. Address / Contact
# =================================================================
add('22 Address / Contact Type', 'ADDRESS_TYPE', '地址类型', 'REFERENCE_DATA',
    {'DEV sample: 往来表.地址': 'ingle string',
     'Industry': 'BILLING / SHIPPING / REGISTERED / CONTACT'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'MDM-000',
    'AddressType enum. Multiple addresses per BP.')

add('22 Address / Contact Type', 'CONTACT_TYPE', '联系方式', 'REFERENCE_DATA',
    {'DEV sample: 往来表': '话/传真/地址/联系人/邮箱 (string fields)',
     'Industry': 'PHONE / EMAIL / FAX / MOBILE / WECHAT / WEBSITE'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'MDM-000',
    'ContactType enum. Multiple contacts per BP.')

# =================================================================
# W. Workflow Status / Reason
# =================================================================
add('23 Workflow Status / Reason', 'WORKFLOW_STATUS', '工作流状态', 'STATUS',
    {'DEV sample: 状态表': '建/已审核/已下单/已生产 + 1 保留 (-1)',
     'GULIERP_CURRENT': 'Foundation G2-002 has no workflow state; per docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md G2-007+'},
    'DEFER', 'P2_WITH_MODULE', 'IMPLEMENT_WITH_MODULE',
    'Workflow status is module-specific (SO has Draft/Confirmed/Shipped/Closed; PO has its own). Use 3D model: DocumentStatus/ApprovalStatus/ExecutionStatus.')

add('23 Workflow Status / Reason', 'APPROVAL_REASON', '审批原因', 'STATUS',
    {'DEV': '品质异常审核意见 -- RecordID=3516, 7 items (报废且返厂/返工/报废无需返工/让步接收/退货/其它/部门知悉)',
     'Industry': 'Each approval action needs a reason code for audit'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'IAuditWriter (per G2-002) stores ApprovalReason. Foundation needs ApprovalReason registry per module.')

# =================================================================
# X. Print / Attachment / Document Type
# =================================================================
add('24 Print / Attachment / Document Type', 'PRINT_TEMPLATE', '打印模板', 'PRODUCTIVITY_METADATA',
    {'DEV': 'JU_Template: 182 templates, each with print layout',
     'VOL': 'PrintDesigner (visual template editor)'},
    'DEFER', 'P2_WITH_MODULE', 'IMPLEMENT_WITH_MODULE',
    'Print template engine is V1.5 per VOL pattern. V1 uses hard-coded HTML render.')

add('24 Print / Attachment / Document Type', 'ATTACHMENT_ENTITY', '附件实体', 'PRODUCTIVITY_METADATA',
    {'DEV': 'JU_TemplateFile.TemplateFile: image column (anti-pattern!)',
     'GULIERP_CURRENT': 'no attachment yet'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'Attachment entity + object store (per G2-002-002 plan). DEV image column REJECT.')

add('24 Print / Attachment / Document Type', 'DOCUMENT_TYPE', '单据类型', 'MASTER_DATA_CONVENTION',
    {'DEV': '模板 -- 182 templates (all docs)',
     'GULIERP_CURRENT': 'no enum yet'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'DocumentType enum: SO / PO / GR / GI / TRANSFER / MO / QC / etc. Used by NumberingPattern + Audit.')

# =================================================================
# Y. Import / Export
# =================================================================
add('25 Import / Export Metadata', 'IMPORT_FORMAT', '导入格式', 'PRODUCTIVITY_METADATA',
    {'DEV': '模板 -- no',
     'VOL': 'Excel Import auto-binding from data source'},
    'DEFER', 'P2_WITH_MODULE', 'IMPLEMENT_WITH_MODULE',
    'Excel import per VOL pattern. V1.5+ scope.')

add('25 Import / Export Metadata', 'EXPORT_FORMAT', '导出格式', 'PRODUCTIVITY_METADATA',
    {'VOL': 'Excel Export VERIFIED_IN_DEMO'},
    'DEFER', 'P2_WITH_MODULE', 'IMPLEMENT_WITH_MODULE',
    'V1.5+ scope per TASK F-16.')

# =================================================================
# Z. Work Calendar / Shift
# =================================================================
add('26 Work Calendar / Shift', 'WORK_CALENDAR', '工作日历', 'REFERENCE_DATA',
    {'DEV dictionary': '班次 (RecordID=3519) has 3 items but names are empty in sample',
     'Industry': 'Mon-Fri 8-17 / Mon-Sat 8-12 14-17 / Shift A (06-14) / Shift B (14-22) / Shift C (22-06)'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'PRODUCTION',
    'WorkCalendar entity: PlantId + ShiftCode + StartTime + EndTime + EffectiveFrom/To + RestDays. Already designed in POC-001/002.')

add('26 Work Calendar / Shift', 'PUBLIC_HOLIDAY', '法定节假日', 'REFERENCE_DATA',
    {'DEV': 'no',
     'China': '国务院办公厅每年发布'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'PRODUCTION',
    'PublicHoliday table: CountryCode + Date + Name + IsWorkday (some holidays have make-up work days).')

# =================================================================
# AA. WorkCenter / Plant Parameters
# =================================================================
add('27 WorkCenter / Plant Parameters', 'PLANT_DEFAULT_PARAMS', '工厂默认参数', 'MASTER_DATA_CONVENTION',
    {'GULIERP_CURRENT': 'gulierp_plant has Code/Name/Address/CountryCode/Timezone/CalendarCode',
     'Industry': 'default warehouse / default UOM / default tax per Plant'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'PlantPolicy: DefaultWarehouseId, DefaultUomId, DefaultTaxCode, DefaultCurrency, etc.')

add('27 WorkCenter / Plant Parameters', 'WORK_CENTER_TYPE', '工作中心类型', 'MASTER_DATA_CONVENTION',
    {'DEV dictionary': '工作中心 (RecordID=71) 3 items: 主线车间/配件车间/包装车间',
     'Industry': 'MACHINE / ASSEMBLY_LINE / INSPECTION / PACKAGING / WAREHOUSE'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'PRODUCTION',
    'WorkCenter.Type enum. Default: 4-5 types per industry.')

add('27 WorkCenter / Plant Parameters', 'WORK_CENTER_CAPACITY', '工作中心产能', 'MASTER_DATA_CONVENTION',
    {'DEV': 'no capacity field',
     'Industry': 'HoursPerDay / Efficiency / StandardCapacity'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'PRODUCTION',
    'WorkCenterCapacity: WorkCenterId + Date + HoursAvailable + HoursUsed + Efficiency. Production scheduling.')

# =================================================================
# BB. Quality
# =================================================================
add('28 Quality Types / Status', 'QC_STATUS', '质检状态', 'STATUS',
    {'DEV dictionary': '品质异常严重程度 (RecordID=3517) 3 items: 轻微/一般/严重',
     'DEV dictionary': '品质异常审核意见 (RecordID=3516) 7 items: 报废且返厂/返工/报废无需返工/让步接收/退货/其它/部门知悉'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'QCStatus enum: PENDING / PASSED / FAILED / WAIVED / ON_HOLD. Already designed in POC-002/003.')

add('28 Quality Types / Status', 'INSPECTION_TYPE', '检验类型', 'STATUS',
    {'DEV': 'no concept (IQC/IPQC/FQC all missing)',
     'Industry': 'IQC (Incoming) / IPQC (In-Process) / FQC (Final)'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'QUALITY',
    'InspectionType enum. Per MDM-000 plan deferred to V2 (Quality module).')

add('28 Quality Types / Status', 'INSPECTION_RESULT', '检验结果', 'STATUS',
    {'DEV': 'no', 'Industry': 'PASS / FAIL / WAIVE / REWORK / SCRAP'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'QUALITY',
    'Per inspection result enum. Defer to V2.')

# =================================================================
# CC. Cost Method
# =================================================================
add('29 Cost Method', 'COST_METHOD', '成本核算方法', 'REFERENCE_DATA',
    {'DEV': 'no',
     'Industry': 'STANDARD / MOVING_AVERAGE / FIFO / LIFO / WEIGHTED_AVERAGE'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Item.CostMethod. China accounting disallows LIFO. Default: MOVING_AVERAGE for manufacturing, STANDARD for retail.')

add('29 Cost Method', 'STANDARD_COST_VARIANCE', '标准成本差异', 'REFERENCE_DATA',
    {'DEV': 'no', 'Industry': 'Material Variance / Labor Variance / Overhead Variance'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FINANCE',
    'Defer to V2 (Finance).')

# =================================================================
# DD. Business Period
# =================================================================
add('30 Business Period', 'ACCOUNTING_PERIOD', '会计期间', 'REFERENCE_DATA',
    {'DEV': '系统表.会计年度 -- 0; 系统表.期间: 0',
     'Industry': 'MONTHLY (most common) / 13-PERIOD (retail)'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FINANCE',
    'AccountingPeriod: Year + Period + StartDate + EndDate + Status (Open/Closed/Locked). Finance module.')

add('30 Business Period', 'FISCAL_YEAR', '会计年度', 'REFERENCE_DATA',
    {'DEV': '系统表.会计年度 -- int',
     'China': 'Calendar year (Jan-Dec) is default; some companies use Apr-Mar fiscal year'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FINANCE',
    'Tenant.FiscalYearStartMonth. Default 1 (Jan).')

add('30 Business Period', 'PERIOD_CLOSE', '期间关账', 'REFERENCE_DATA',
    {'DEV': 'no', 'Industry': 'Hard close + soft close + reopen permission'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FINANCE',
    'Defer to V2.')

# =================================================================
# EE. Date / Time / Locale
# =================================================================
add('31 Date / Time / Locale', 'TIMEZONE_DEFAULT', '默认时区', 'FOUNDATION',
    {'GULIERP_CURRENT': 'gulierp_plant.Timezone varchar(64); Tenant not yet',
     'Industry': 'IANA timezone (Asia/Shanghai etc.)'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-003',
    'Foundation G2-002 uses DateTimeOffset + W3C TraceId. Tenant.Timezone needed. IANA names (Asia/Shanghai).')

add('31 Date / Time / Locale', 'DATE_FORMAT_DEFAULT', '日期格式', 'FOUNDATION',
    {'DEV': 'dates stored as datetime', 'Industry': 'yyyy-MM-dd ISO 8601; yyyy年MM月dd日 for CN display'},
    'NEW_BUILD', 'P0_NOW', 'BASE-003',
    'ISO 8601 for storage. Display per locale. System.Globalization used by .NET.')

add('31 Date / Time / Locale', 'LOCALE_DEFAULT', '默认区域', 'FOUNDATION',
    {'DEV': 'not modeled', 'Industry': 'zh-CN / en-US / ja-JP etc.'},
    'NEW_BUILD', 'P0_NOW', 'BASE-003',
    'Tenant.DefaultLocale. Affects number/date formatting, currency display.')

add('31 Date / Time / Locale', 'WEEK_START', '周起始日', 'FOUNDATION',
    {'DEV': 'not modeled', 'China': 'Monday (GB/T 7408); US: Sunday'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'Locale.WeekStart enum: MONDAY (CN/ISO) / SUNDAY (US). Affects week-based reports.')

# =================================================================
# FF. Effective Date / Versioning
# =================================================================
add('32 Effective Date / Versioning', 'EFFECTIVE_DATED_MASTER', '生效日期主数据', 'MASTER_DATA_CONVENTION',
    {'DEV': 'CreateTime + LastEditTime but no explicit valid_from/valid_to',
     'Industry': 'Price list / Tax rate / Customer terms have effective dating'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'Critical pattern: master data (Price, Tax, CustomerTerms, SupplierTerms) should be effective-dated.')

add('32 Effective Date / Versioning', 'SLOWLY_CHANGING_DIMENSION', 'SCD 维度', 'MASTER_DATA_CONVENTION',
    {'DEV': 'no',
     'Industry': 'SCD Type 2 (full history with valid_from/valid_to) is ERP standard'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FOUNDATION',
    'Decision: SCD Type 2 for Price/Tax/Customer. Type 1 for Item name corrections. Need helper base class.')

add('32 Effective Date / Versioning', 'BOM_VERSION', 'BOM 版本', 'MASTER_DATA_CONVENTION',
    {'DEV': 'BOM表 -- BOMStatus (0/1/2?) but no version',
     'Industry': 'BOM revisions with effective_from + status (Draft/Active/Obsolete)'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'PRODUCTION',
    'BomRevision entity per MDM-000 P1-005 plan. Already designed.')

# =================================================================
# GG. Parameter Scope
# =================================================================
add('33 Parameter Scope', 'PARAM_SCOPE_SYSTEM', '系统参数', 'FOUNDATION',
    {'GULIERP_CURRENT': 'ConnectionStrings__GuliERP + appsettings.json',
     'G2-002': 'config validation at startup'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-003',
    'G2-002 already validates connection string at host start. SYSTEM params: default precision, default rounding, default currency.')

add('33 Parameter Scope', 'PARAM_SCOPE_TENANT', '租户参数', 'FOUNDATION',
    {'GULIERP_CURRENT': 'gulierp_tenant created but no param table',
     'Industry': 'Per-tenant config (timezone, locale, fiscal year, default currency)'},
    'NEW_BUILD', 'P0_NOW', 'BASE-003',
    'TenantSetting table: TenantId + Key + Value + Scope. Foundation provides ISystemSetting abstraction.')

add('33 Parameter Scope', 'PARAM_SCOPE_COMPANY', '公司参数', 'FOUNDATION',
    {'GULIERP_CURRENT': 'gulierp_company has DefaultCurrency + Timezone',
     'Industry': 'Per-company config (different company in same tenant may have different timezone)'},
    'NEW_BUILD', 'P0_NOW', 'BASE-003',
    'CompanySetting table. DefaultCurrency/Timezone already in gulierp_company but no extension mechanism.')

add('33 Parameter Scope', 'PARAM_SCOPE_PLANT', '工厂参数', 'FOUNDATION',
    {'GULIERP_CURRENT': 'gulierp_plant.CalendarCode varchar(40)  --  calendar link is there',
     'Industry': 'Per-plant config (calendar, default UOM, default warehouse)'},
    'NEW_BUILD', 'P0_NOW', 'BASE-003',
    'PlantSetting table. Plant.Policy. Default: first phase hard-coded.')

add('33 Parameter Scope', 'PARAM_SCOPE_USER', '用户参数', 'FOUNDATION',
    {'GULIERP_CURRENT': 'no per-user setting',
     'Industry': 'user preferences (language, theme, default page size)'},
    'DEFER', 'P3_DEFER', 'IMPLEMENT_WITH_MODULE',
    'Per-user settings usually not business-critical. Defer to UX requirement.')

# =================================================================
# HH. Default Value Policy
# =================================================================
add('34 Default Value Policy', 'DEFAULT_WAREHOUSE', '默认仓库', 'MASTER_DATA_CONVENTION',
    {'DEV': 'no', 'Industry': 'Each Company/Plant has Default Warehouse for auto-issue'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'PlantPolicy.DefaultWarehouseId.')

add('34 Default Value Policy', 'DEFAULT_CURRENCY', '默认币种', 'MASTER_DATA_CONVENTION',
    {'GULIERP_CURRENT': 'gulierp_company.DefaultCurrency char(3)  --  already in schema',
     'Industry': 'Company default currency (tenant may have multiple)'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-003',
    'Already in Identity G2-003. Reuse.')

add('34 Default Value Policy', 'DEFAULT_TAX', '默认税率', 'MASTER_DATA_CONVENTION',
    {'DEV': 'no', 'Industry': 'Default tax code for new sales lines'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'CompanyPolicy.DefaultTaxCode. Affects SO/PO line entry UX.')

add('34 Default Value Policy', 'DEFAULT_UOM_PER_ITEM', '物料默认单位', 'MASTER_DATA_CONVENTION',
    {'DEV': '商品表.单位 -- string',
     'Industry': 'Item has BaseUom (stocking) + IssueUom (sales)'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'INVENTORY',
    'Item.BaseUomId per (Item, Plant).')

# =================================================================
# II. Metadata / Generator Contract
# =================================================================
add('35 Metadata / Generator Contract', 'BUSINESS_SEMANTIC_TYPE_METADATA', '业务语义类型元数据', 'PRODUCTIVITY_METADATA',
    {'MDM-000D R1': 'system/semantic-data-type.json (13 types, all PROPOSED)',
     'VOL': 'pattern reference only'},
    'REUSE_DATA', 'P0_NOW', 'BASE-001',
    'Already in MDM-000D. Generator metadata contract drafted (C# attribute).')

add('35 Metadata / Generator Contract', 'REFERENCEDATA_METADATA', '引用数据元数据', 'PRODUCTIVITY_METADATA',
    {'MDM-000D R1': 'ReferenceData concept (C# attribute) drafted',
     'VOL': 'Sys_Dictionary pattern'},
    'REUSE_PATTERN', 'P1_BEFORE_MODULE', 'MDM-000',
    '[ReferenceData("EDUCATION")] attribute on field → auto-generates Select lookup.')

add('35 Metadata / Generator Contract', 'GENERATOR_ENGINE', 'Generator 引擎', 'PRODUCTIVITY_METADATA',
    {'GULIERP_CURRENT': 'no generator; manual C# classes',
     'VOL': 'code-generated Sys_Dictionary.vue'},
    'DEFER', 'P3_DEFER', 'IMPLEMENT_WITH_MODULE',
    'Generator engine is G9+ per governance. V1 uses strong-typed C#.')

# =================================================================
# DISCOVERED_FROM_SOURCE (用户尚未考虑的内容)
# =================================================================

add('DISCOVERED_FROM_SOURCE', 'LOCK_OPTIMISTIC_VERSION', '乐观锁版本号', 'FOUNDATION',
    {'GULIERP_CURRENT': 'ConcurrencyVersion int (G2-001 + Identity G2-003)',
     'DEV': 'no explicit version field (DEV uses AllowBatch + ReuseType)'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-004',
    'ConcurrencyVersion already in every Identity table. Foundation G2-001 baseline. Apply to ALL master data + business tables.')

add('DISCOVERED_FROM_SOURCE', 'AUDIT_TRAIL_IAUDITWRITER', '审计写入', 'FOUNDATION',
    {'G2-002 POC-003': 'IAuditWriter (Foundation.Kernel)',
     'DEV: JU_SysLog (177 rows) + JU_SysDesignLog (349 rows)': 'separate design log is a DEV good idea',
     'DEV split: operation log vs design log separately': '(2 channels, design + operation)'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-004',
    'IAuditWriter per POC-003. Two log channels: OperationLog (state changes) + DesignLog (configuration changes). DEV pattern is good  --  KEEP it.')

add('DISCOVERED_FROM_SOURCE', 'FIELD_COMMENT_OBLIGATION', '字段注释强制', 'FOUNDATION',
    {'GULIERP_CURRENT': 'no pg_comment on columns',
     'DEV: SQL Server ms_description (0/151 JU_ tables have descriptions)': '0/151 JU_ tables have descriptions (also bad)'},
    'NEW_BUILD', 'P0_NOW', 'BASE-004',
    'Convention: every column must have pg_description comment. EF Core 10 supports comment migration. Architecture rule, not just style.')

add('DISCOVERED_FROM_SOURCE', 'TABLE_COMMENT_OBLIGATION', '表注释强制', 'FOUNDATION',
    {'GULIERP_CURRENT': 'no pg_comment on tables',
     'DEV: 0/151 JU_ tables have ms_description': 'DEV: 0/151 JU_ tables have ms_description'},
    'NEW_BUILD', 'P0_NOW', 'BASE-004',
    'Every table must have pg_description comment. Auto-generated if not provided (warning).')

add('DISCOVERED_FROM_SOURCE', 'AUTO_INC_VS_SNOWFLAKE', '主键生成策略', 'FOUNDATION',
    {'GULIERP_CURRENT': 'IdentityByDefaultColumn (PostgreSQL BIGINT identity)  --  for now',
     'G2-002 SnowflakeIdGenerator': '41+10+12 bits, epoch 2026-01-01  --  for distributed scaling'},
    'NEW_BUILD', 'P0_NOW', 'BASE-004',
    'Decision: per-table config. Snowflake for distributed tables (Order/Doc); serial for local-only. Already in G2-001/002.')

add('DISCOVERED_FROM_SOURCE', 'TRANSACTION_OUTBOX', '事务性发件箱', 'FOUNDATION',
    {'GULIERP_CURRENT': 'no',
     'Industry': 'Critical for distributed: events emitted in same DB transaction, then published'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'Outbox pattern for cross-module events. Deferred to G2-007+ per governance, but design contract now.')

add('DISCOVERED_FROM_SOURCE', 'SOFT_DELETE_CONVENTION', '软删除约定', 'FOUNDATION',
    {'GULIERP_CURRENT': 'Status int (Active/Inactive/Suspended)  --  soft delete via status flag',
     'DEV: ReportStatus + LockStatus + WorkflowStatus  --  3 separate status fields is anti-pattern': 'DEV: ReportStatus + LockStatus + WorkflowStatus  --  3 separate status fields is anti-pattern'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-004',
    'Use single AggregateState enum (per G2-002 plan). Soft delete = state=DELETED. NEVER physical delete for master data.')

add('DISCOVERED_FROM_SOURCE', 'NESTED_SET_VS_ADJACENCY_LIST', '组织树存储', 'FOUNDATION',
    {'GULIERP_CURRENT': 'gulierp_organization_unit + gulierp_plant both use parent_id adjacency list',
     'Industry': 'Adjacency list (simple) vs Nested Set (fast subtree query) vs Path Enumeration (variable)'},
    'REUSE_PATTERN', 'P0_NOW', 'FOUNDATION',
    'G2-003 already chose adjacency list + materialized path. Document the pattern; new trees (Category) follow same.')

add('DISCOVERED_FROM_SOURCE', 'TIME_RANGE_OVERLAP_RULE', '生效期重叠规则', 'FOUNDATION',
    {'GULIERP_CURRENT': 'no',
     'Industry': 'SCD Type 2: two versions of same item cannot have overlapping valid dates'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'DB-level CHECK constraint or trigger to prevent (item_id, valid_from, valid_to) overlap. Per SCD Type 2.')

add('DISCOVERED_FROM_SOURCE', 'SOFT_DELETE_AUDIT', '软删除审计', 'FOUNDATION',
    {'DEV': '状态表 has 删除/恢复 logic',
     'Industry': 'Soft delete must be in audit log + reversible for 30 days'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'SoftDelete entity: EntityType + EntityId + DeletedAt + DeletedBy + RestoredAt + RestoredBy. Mandatory for master data.')

add('DISCOVERED_FROM_SOURCE', 'STATUS_TRANSITION_VALIDATION', '状态机转换校验', 'FOUNDATION',
    {'GULIERP_CURRENT': 'Status int but no state machine',
     'DEV: Workflow (NAV) but no validation library': 'DEV: Workflow (NAV) but no validation library',
     'POC-003': 'SalesOrder has state machine (Draft/Confirmed/Closed/Cancelled)'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-004',
    'State machine library (Stateless or hand-rolled) per aggregate. POC-003 already implements for SalesOrder.')

add('DISCOVERED_FROM_SOURCE', 'INVENTORY_POSTING_RULE', '库存过账规则', 'FOUNDATION',
    {'GULIERP_CURRENT': 'no',
     'DEV: manual 出库过账 / 入库过账 ': 'anti-pattern',
     'POC-003 / MDM-000': 'IInventoryService.PostAsync (single entry point)'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-004',
    'InventoryPostingEngine (per G2-002 plan)  --  event-driven, not manual step. Already designed.')

add('DISCOVERED_FROM_SOURCE', 'CURRENCY_TRANSLATION_METHOD', '币种折算方法', 'FOUNDATION',
    {'DEV': '单价 (single currency)',
     'Industry': 'Spot rate / Average rate / Historical rate (for fixed assets)'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'CurrencyTranslationMethod enum: SPOT / AVERAGE / HISTORICAL. Default SPOT.')

add('DISCOVERED_FROM_SOURCE', 'ROUNDING_TOLERANCE', '舍入容差', 'FOUNDATION',
    {'DEV': '收支科目 with 0.01 precision',
     'Industry': 'Allow 0.01 tolerance for currency rounding (e.g. SO total vs sum of lines)'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'SALES',
    'Tolerance policy: 0.01 = always allow; 0.00 = strict (display warning).')

add('DISCOVERED_FROM_SOURCE', 'UOM_CONVERSION_DECIMAL_PRECISION', '换算精度', 'FOUNDATION',
    {'DEV': '换算系数 (string)',
     'Industry': '1 box = 12.5 ea (not always integer)'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'ConversionRatio should be decimal, not integer. precision=10, scale=4.')

add('DISCOVERED_FROM_SOURCE', 'TRIAL_BALANCE_POLICY', '试算平衡', 'FOUNDATION',
    {'DEV': 'no',
     'Industry': 'Debit must equal Credit in every journal entry. Hard fail.'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FINANCE',
    'Finance module V2.')

add('DISCOVERED_FROM_SOURCE', 'DUPLICATE_DOCUMENT_NUMBER_POLICY', '重号检测', 'FOUNDATION',
    {'DEV': '编号 (single field, no dedup check)',
     'Industry': 'NumberSequence must check uniqueness in DB (UNIQUE constraint) before insert'},
    'NEW_BUILD', 'P0_NOW', 'BASE-002',
    'DocumentNumber UNIQUE constraint per (DocType, Year, Tenant). DB-level guard. Already implicit in POC-003.')

add('DISCOVERED_FROM_SOURCE', 'GL_ACCOUNT_MAPPING', '总账科目映射', 'FOUNDATION',
    {'DEV dictionary': '收支科目 (RecordID=3097) 7 items + 科目类型 (RecordID=3437) 5 items',
     'China': '企业会计准则科目表 (specific chart of accounts)'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FINANCE',
    'China COA standard: 资产/负债/所有者权益/成本/损益. 6-digit COA. V2 Finance.')

add('DISCOVERED_FROM_SOURCE', 'ERP_CROSS_CUTTING_TENANT_SCOPE', 'Tenant Scope 优先级', 'FOUNDATION',
    {'G2-002 / G2-003': 'TenantId on every business table',
     'Industry': 'Tenant > Company > Plant > Organization hierarchy'},
    'REUSE_PATTERN', 'P0_NOW', 'BASE-004',
    'ICurrentTenant + ICurrentCompany contracts (G2-003) define scope chain. Plant is a Scope for Inventory/Production.')

add('DISCOVERED_FROM_SOURCE', 'DOCUMENT_NUMBER_GAP_DETECTION', '单号跳号检测', 'FOUNDATION',
    {'DEV': 'no', 'Industry': 'Number 2026-000001, 2026-000003  --  gap detected, admin investigates'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'IMPLEMENT_WITH_MODULE',
    'NumberingGapReport. Reports gaps. Audit trail of who/when used each number.')

add('DISCOVERED_FROM_SOURCE', 'POSTING_PERIOD_LOCK', '过账期间锁定', 'FOUNDATION',
    {'DEV': 'no', 'Industry': 'Once period is closed, no new postings allowed. Soft lock with override.'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FINANCE',
    'Period lock V2.')

add('DISCOVERED_FROM_SOURCE', 'GL_DOUBLE_ENTRY_VALIDATION', '复式记账校验', 'FOUNDATION',
    {'DEV': 'no', 'Industry': 'Sum(Debit) == Sum(Credit). Hard fail.'},
    'NEW_BUILD', 'P2_WITH_MODULE', 'FINANCE',
    'V2 Finance.')

add('DISCOVERED_FROM_SOURCE', 'LOCALIZATION_I18N', '国际化', 'FOUNDATION',
    {'DEV': '商品名称 stored in 单个 名称 field, no multi-lang',
     'Industry': 'Item.Name per locale; display current locale'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'FOUNDATION',
    'i18n strategy: name + name_en + name_local_xx fields, or separate Translation table. Foundation-level concern.')

add('DISCOVERED_FROM_SOURCE', 'BITEMPORAL_HISTORY', '双时态历史', 'FOUNDATION',
    {'DEV': 'LastEditTime (single timestamp)',
     'Industry': 'bitemporal: valid_time (business) + transaction_time (system) for full audit'},
    'DEFER', 'P3_DEFER', 'IMPLEMENT_WITH_MODULE',
    'Advanced pattern. Defer to V2/V3 when audit requirements demand.')

add('DISCOVERED_FROM_SOURCE', 'GLOBAL_SEARCH_FULLTEXT', '全局搜索', 'FOUNDATION',
    {'DEV': 'no', 'Industry': 'Search across Item.Name / BP.Name / DocNo with typo tolerance'},
    'DEFER', 'P3_DEFER', 'IMPLEMENT_WITH_MODULE',
    'PostgreSQL full-text search or ElasticSearch. V2.')

add('DISCOVERED_FROM_SOURCE', 'PERMISSION_FIELD_LEVEL', '字段级权限', 'FOUNDATION',
    {'G2-005 NOT STARTED': 'no',
     'DEV: HiddenFieldID is visibility, not real permission': 'DEV: HiddenFieldID is visibility, not real permission'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'G2-005',
    'Field-level read/write policy (G2-005 plan). Salesperson sees qty not price.')

add('DISCOVERED_FROM_SOURCE', 'DATA_SCOPE_FILTER', '数据范围过滤', 'FOUNDATION',
    {'G2-005': 'not started',
     'DEV: SpecViewRightFilter is expression string (anti-pattern)': 'DEV: SpecViewRightFilter is expression string (anti-pattern)'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'G2-005',
    'OrgScopedQuery<T> with Expression<Func<T,bool>> (per G2-002 plan).')

add('DISCOVERED_FROM_SOURCE', 'PRINT_FORM_NUMBERING', '打印表单号', 'FOUNDATION',
    {'DEV': '打印模板 has its own counter',
     'Industry': 'Printed form has its own number (different from DB id)'},
    'DEFER', 'P2_WITH_MODULE', 'IMPLEMENT_WITH_MODULE',
    'When print engine V1.5 ships.')

add('DISCOVERED_FROM_SOURCE', 'NEGATIVE_STOCK_CHECK', '负库存检查', 'FOUNDATION',
    {'DEV': 'no', 'Industry': 'Some companies allow negative stock (WIP); others block'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'PlantPolicy.AllowNegativeStock bool. Default false (block).')

add('DISCOVERED_FROM_SOURCE', 'BARCODE_LABEL_PRINT', '条码打印', 'FOUNDATION',
    {'DEV': 'no',
     'Industry': 'Label printer (Zebra) integration for warehouse labels'},
    'DEFER', 'P3_DEFER', 'IMPLEMENT_WITH_MODULE',
    'V2+ Inventory.')

add('DISCOVERED_FROM_SOURCE', 'CROSS_DOCK_OPERATION', '越库操作', 'FOUNDATION',
    {'DEV': 'no',
     'Industry': 'Goods received and shipped in same operation, no putaway'},
    'DEFER', 'P3_DEFER', 'IMPLEMENT_WITH_MODULE',
    'V2+ WMS.')

add('DISCOVERED_FROM_SOURCE', 'INVENTORY_PERIODIC_AVERAGE', '期间移动平均', 'FOUNDATION',
    {'DEV': 'no',
     'Industry': 'Moving Average Cost (MAC) per Item per Plant per Period'},
    'NEW_BUILD', 'P1_BEFORE_MODULE', 'INVENTORY',
    'ItemCostLedger: ItemId + PlantId + Period + QtyIn + QtyOut + CostIn + CostOut + AvgCost. V1.5 Inventory.')

# Write
write(os.path.join(OUT_BASE, 'FOUNDATION_AND_CONFIGURATION_DISCOVERY_MATRIX.json'), {
    'meta': {
        'extraction_time': EXTRACTION_TIME,
        'session': SESSION,
        'goal': 'BASE-000',
        'total_findings': len(discovery),
    },
    'findings': discovery,
})
print(f'  Wrote FOUNDATION_AND_CONFIGURATION_DISCOVERY_MATRIX.json ({len(discovery)} items)')

# =============================================================================
# 3. USER_NOT_YET_CONSIDERED_FINDINGS
# =============================================================================
print()
print('=== Building USER_NOT_YET_CONSIDERED_FINDINGS ===')

not_considered = [
    {
        'id': 'UNC-001', 'priority': 'HIGH', 'category': 'NUMBERING',
        'topic': 'NumberSequence per-tenant scope',
        'discovered_from': 'DEV JU_AutoCode has no TenantId  --  global counter per system',
        'why_user_hasnt_considered': 'G2-003 Identity uses TenantId on every business table, but DEV-style numbering is system-wide. User may have implicitly assumed system-wide.',
        'evidence': 'DEV reverse-engineering shows 0 number patterns with tenant scope',
        'impact': 'Two tenants in same DB would collide on SO/PO numbers → production data corruption',
        'recommendation': 'Add TenantId (and CompanyId) to NumberSequence; re-test cross-tenant uniqueness',
    },
    {
        'id': 'UNC-002', 'priority': 'HIGH', 'category': 'AUDIT',
        'topic': 'OperationLog vs DesignLog split',
        'discovered_from': 'DEV has JU_SysLog (operation) + JU_SysDesignLog (design)  --  separate tables',
        'why_user_hasnt_considered': 'G2-002 IAuditWriter covers operation log. User may have missed the design-config audit need.',
        'evidence': 'DEV has 349 design log rows vs 177 op log  --  heavy config changes need separate channel',
        'impact': 'Lost traceability on dictionary/numbering/tax changes',
        'recommendation': 'Add IDesignAuditWriter to Foundation.G2-002 (next iteration). DEV pattern is good  --  KEEP.',
    },
    {
        'id': 'UNC-003', 'priority': 'HIGH', 'category': 'CONVENTION',
        'topic': 'pg_description comment on EVERY column',
        'discovered_from': 'DEV has 0/151 JU_ tables with ms_description; GULIERP_CURRENT has no pg_description either',
        'why_user_hasnt_considered': 'G2-001 PostgreSQL Engineering Standard may not mandate column comments.',
        'evidence': 'DEV reverse-engineering: ms_description=null on all 4006 columns',
        'impact': 'Schema documentation rots. New dev onboards slower.',
        'recommendation': 'Add architecture rule: every column has pg_description; CI check fails build if missing.',
    },
    {
        'id': 'UNC-004', 'priority': 'HIGH', 'category': 'CONVENTION',
        'topic': 'Number UNIQUE constraint at DB level',
        'discovered_from': 'DEV has no unique constraint on number fields (0 FK + 0 CHECK pattern)',
        'why_user_hasnt_considered': 'NumberSequence application-level guard is implicit in POC-003. DB-level guard missing.',
        'evidence': 'DEV has 0 unique constraints documented (per size.json: only 1 default constraint)',
        'impact': 'Race condition: two concurrent inserts could create duplicate SO numbers',
        'recommendation': 'Add UNIQUE (TenantId, DocType, Year, Serial) at DB level.',
    },
    {
        'id': 'UNC-005', 'priority': 'HIGH', 'category': 'ROUNDING',
        'topic': 'Rounding mode per semantic type',
        'why_user_hasnt_considered': 'DEV uses single .NET default rounding. User may not have thought about per-type policy.',
        'evidence': 'JU_DataType has no rounding column; depends on .NET Math.Round default',
        'impact': 'Currency cross-aggregation loses 0.01 per sum → financial audit failure',
        'recommendation': 'Documented in semantic-data-type.json: AMOUNT/EXCHANGE_RATE=HALF_EVEN, QUANTITY=HALF_UP, RATE=HALF_EVEN.',
    },
    {
        'id': 'UNC-006', 'priority': 'HIGH', 'category': 'INVENTORY',
        'topic': 'InventoryPostingEngine is SINGLE entry point',
        'discovered_from': 'POC-002/POC-003 design (per G2-002 plan)',
        'why_user_hasnt_considered': 'DEV has 22 inventory tables with manual "出库过账" step (anti-pattern).',
        'evidence': 'DEV: 出库过账/入库过账 are manual UI steps  --  leads to inconsistent inventory',
        'impact': 'Inventory balance ≠ sum of transactions; reconciliation nightmare',
        'recommendation': 'IInventoryService.PostAsync as single entry. ALL other modules emit GoodsReceiptConfirmed/ShipmentConfirmed events → listener calls PostAsync. NEVER direct write.',
    },
    {
        'id': 'UNC-007', 'priority': 'MEDIUM', 'category': 'REFERENCE_DATA',
        'topic': 'UomDimension (COUNT/LENGTH/MASS/etc.) as required enum',
        'discovered_from': 'DEV has 13 UOM but no dimension metadata',
        'why_user_hasnt_considered': 'UOM is a flat list in DEV; user assumes same UOM works everywhere.',
        'evidence': 'DEV cannot catch bug: assigning kg item to m UOM',
        'impact': 'Cross-dimensional arithmetic bugs (e.g. unit price × kg) undetected',
        'recommendation': 'Uom.Dimension enum required. Item.BaseUomId (FK to Uom). Field metadata: BusinessSemanticType="QUANTITY" UomDimension="MASS" (Foundation contract).',
    },
    {
        'id': 'UNC-008', 'priority': 'MEDIUM', 'category': 'INVENTORY',
        'topic': 'Status: AVAILABLE / RESERVED / PENDING_INSPECTION / BLOCKED',
        'discovered_from': 'POC-002 InventoryBalance design',
        'why_user_hasnt_considered': 'DEV status表 has 创建/已审核/已下单/已生产  --  workflow statuses, not inventory balances.',
        'evidence': 'DEV has 0 inventory balance states',
        'impact': 'Cannot block stock for QC; cannot show reserved vs available separately',
        'recommendation': 'InventoryBalance.{OnHand, Reserved, Available, PendingInspection, Blocked, Quarantined} as 5 separate decimal fields.',
    },
    {
        'id': 'UNC-009', 'priority': 'MEDIUM', 'category': 'CODING',
        'topic': 'BP Code format structured (CUST-/SUPP-/SUB-/LOG-)',
        'discovered_from': 'DEV sample: 往来号=20001, 20002  --  pure numeric',
        'why_user_hasnt_considered': 'User may have seen numeric codes as "natural"',
        'evidence': 'DEV has 4 BP types but no code prefix differentiation',
        'impact': 'Same number space for Customer (20001) and Supplier (20002)  --  no type visible in code',
        'recommendation': 'BP.Code: {TYPE_PREFIX}-{SERIAL}. CUST-001, SUPP-001, SUB-001, LOG-001.',
    },
    {
        'id': 'UNC-010', 'priority': 'MEDIUM', 'category': 'INVENTORY',
        'topic': 'Lot expiry date + FEFO',
        'discovered_from': 'DEV has 克重 0.0 but no ExpiryDate',
        'why_user_hasnt_considered': 'For non-pharma/食品 companies, expiry not needed. But tenant configurable.',
        'evidence': 'DEV no ExpiryDate field in any table',
        'impact': 'Cannot do FEFO for food/chem; cannot recall expired lot',
        'recommendation': 'InventoryLot.ExpiryDate nullable; Item.IsExpiryControlled bool; PickingStrategy enum (FIFO/FEFO/LIFO).',
    },
    {
        'id': 'UNC-011', 'priority': 'MEDIUM', 'category': 'TENANT_SCOPE',
        'topic': 'Plant as cross-cutting scope (Inventory/Production)',
        'discovered_from': 'G2-003 has gulierp_plant; IPlantScoped marker interface reserved',
        'why_user_hasnt_considered': 'Tenant/Company/Organization are obvious. Plant as ERP scope is less obvious.',
        'evidence': 'POC-001/002 used TenantId only. IPlantScoped not yet wired.',
        'impact': 'Multi-plant tenant cannot separate inventory per plant',
        'recommendation': 'Apply IPlantScoped to InventoryTransaction/InventoryBalance/ProductionOrder in their respective modules.',
    },
    {
        'id': 'UNC-012', 'priority': 'MEDIUM', 'category': 'CONVENTION',
        'topic': 'Field-level precision annotation as code attribute',
        'discovered_from': 'POC-003 has SalesOrderLine.UnitPrice decimal without precision attr',
        'why_user_hasnt_considered': 'C# decimal type is unbounded; user may assume precision is implicit.',
        'evidence': 'G2-002 Foundation default is decimal(18,4); POC-003 not yet annotated',
        'impact': 'Generator cannot derive PG numeric(P,S) without manual map per field',
        'recommendation': '[DecimalPrecision(20, 6)] attribute on C# decimal properties. Generator reads attribute → PG DDL. Already in MDM-000D contract draft.',
    },
    {
        'id': 'UNC-013', 'priority': 'MEDIUM', 'category': 'FOUNDATION',
        'topic': 'Single AggregateState vs multi-status fields',
        'discovered_from': 'DEV: ReportStatus + LockStatus + WorkflowStatus  --  3 separate status fields on same entity',
        'why_user_hasnt_considered': 'Multi-status may seem flexible. But leads to ReportStatus=2 LockStatus=1 WorkflowStatus=3 (meaningless combinations).',
        'evidence': 'DEV: every template has 3 status fields. No consistency rules.',
        'impact': 'Untestable state combinations. Reports show inconsistent data.',
        'recommendation': 'Per G2-002 plan: 3D model DocumentStatus/ApprovalStatus/ExecutionStatus. NOT 3 fields on one entity. POC-003 already follows this.',
    },
    {
        'id': 'UNC-014', 'priority': 'MEDIUM', 'category': 'CODING',
        'topic': 'Item code structured (category-prefix-serial)',
        'discovered_from': 'DEV: 品号=1000001 (pure numeric)',
        'why_user_hasnt_considered': 'Numeric codes feel "clean"',
        'evidence': 'DEV has 1 sample item with 1000001; no category prefix',
        'impact': 'Cannot identify item category from code; searching/sorting is hard',
        'recommendation': 'Item.Code: {CategoryCode}-{Sequence}. FG-000001, RM-000001, SF-000001.',
    },
    {
        'id': 'UNC-015', 'priority': 'LOW', 'category': 'REFERENCE_DATA',
        'topic': 'Color / Material / Size as attribute, not UOM',
        'discovered_from': 'DEV sample: 商品表 has 材料 string but no Color',
        'why_user_hasnt_considered': 'User may think of Color/Material as ItemCategory.',
        'evidence': 'DEV has no Color dimension in UOM',
        'impact': 'If treated as UOM, cross-color arithmetic bugs',
        'recommendation': 'Color/Material/Size/Grade are ItemAttribute, NOT UOM. Defer to SKU variant phase per P1-005 plan.',
    },
    {
        'id': 'UNC-016', 'priority': 'LOW', 'category': 'WORKFLOW',
        'topic': 'Design Audit vs Operation Audit',
        'discovered_from': 'DEV has JU_SysLog + JU_SysDesignLog  --  separate',
        'why_user_hasnt_considered': 'One audit log seems enough; user may have missed the need for design-config audit specifically.',
        'evidence': '349 design log rows show config changes are frequent and need separate retention',
        'impact': 'Lost traceability on config changes',
        'recommendation': 'G2-002R2 may add IDesignAuditWriter. Per G2-002 plan, current IAuditWriter is for operations only.',
    },
    {
        'id': 'UNC-017', 'priority': 'LOW', 'category': 'CONVENTION',
        'topic': 'Photo / Attachment as entity, not image column',
        'discovered_from': 'DEV: JU_TemplateFile.TemplateFile is image column (anti-pattern)',
        'why_user_hasnt_considered': 'image column looks convenient',
        'evidence': 'DEV: image columns store base64 in DB; large payload slows backups',
        'impact': 'Backup size explosion, slow queries, no CDN, no access control',
        'recommendation': 'Attachment entity + object store (S3/MinIO). Reference by URL. Per G2-002-002 plan. REJECT image columns.',
    },
    {
        'id': 'UNC-018', 'priority': 'LOW', 'category': 'INVENTORY',
        'topic': 'Negative stock policy (allow vs block)',
        'discovered_from': 'DEV: no concept; some companies allow (WIP), others block',
        'why_user_hasnt_considered': 'User may assume "block" is universal',
        'evidence': 'DEV has no field to indicate policy',
        'impact': 'One-size policy breaks manufacturing (WIP needs negative)',
        'recommendation': 'PlantPolicy.AllowNegativeStock bool. Default false.',
    },
    {
        'id': 'UNC-019', 'priority': 'LOW', 'category': 'CALENDAR',
        'topic': 'Public holiday + make-up work day',
        'discovered_from': 'DEV: no concept',
        'why_user_hasnt_considered': 'Tenant may think their own calendar',
        'evidence': 'China 国务院 publishes yearly; companies follow with adjustments',
        'impact': 'Production planning off by 1-2 days per holiday',
        'recommendation': 'PublicHoliday table: CountryCode + Date + Name + IsWorkday. Defer to V2 Production.',
    },
    {
        'id': 'UNC-019b', 'priority': 'LOW', 'category': 'CALENDAR',
        'topic': 'i18n locale per tenant',
        'discovered_from': 'DEV no locale',
        'why_user_hasnt_considered': 'Default zh-CN assumed',
        'evidence': 'DEV has 14 users, no locale field',
        'impact': 'Multi-national tenant cannot have different display',
        'recommendation': 'Tenant.DefaultLocale + User.PreferredLocale.',
    },
    {
        'id': 'UNC-020', 'priority': 'LOW', 'category': 'PRINT',
        'topic': 'Printed form number vs DB ID',
        'discovered_from': 'DEV: 编号 separate from RecordID',
        'why_user_hasnt_considered': 'User may use DB ID as print number',
        'evidence': 'DEV: 单号 2511220018 vs RecordID 397/402  --  different sequences',
        'impact': 'Print number gaps confuse user; DB ID is implementation detail',
        'recommendation': 'DocumentNumber (business) separate from Id (technical). Already in POC-003.',
    },
    {
        'id': 'UNC-021', 'priority': 'LOW', 'category': 'AUDIT',
        'topic': 'SoftDelete entity with restore window',
        'discovered_from': 'DEV: 状态表 has 删除/恢复 logic but not as separate entity',
        'why_user_hasnt_considered': 'Soft delete via Status flag seems enough',
        'evidence': 'DEV status values not fully enumerated',
        'impact': 'Cannot restore 30-day-old delete; no audit of who/when',
        'recommendation': 'SoftDelete entity with DeletedAt/DeletedBy/RestoredAt. Required for master data.',
    },
    {
        'id': 'UNC-022', 'priority': 'LOW', 'category': 'CONVENTION',
        'topic': 'Code+Name+Description separation',
        'discovered_from': 'G2-001/002/003 already use Code+Name pattern',
        'why_user_hasnt_considered': 'DEV has 名称 + 备注 but no Code separation',
        'evidence': 'DEV: 商品表.品名 + 商品表.品号; but 字典 has only 名称 no Code',
        'impact': 'Code is implementation detail; Name is user-facing; mixing causes i18n issues',
        'recommendation': 'Every ReferenceData / MasterData has Code (technical, immutable) + Name (display, localizable) + Description (long text). Already in POC-002/003.',
    },
    {
        'id': 'UNC-023', 'priority': 'LOW', 'category': 'INVENTORY',
        'topic': 'Lot/Serial controlled flag per item',
        'discovered_from': 'DEV: no per-item flag',
        'why_user_hasnt_considered': 'Default to no-control simplifies',
        'evidence': 'DEV has 批次库存 view only',
        'impact': 'Cannot trace per-unit for recall; cannot enforce serialization for regulated items',
        'recommendation': 'Item.IsLotControlled, Item.IsSerialControlled, Item.IsExpiryControlled booleans.',
    },
    {
        'id': 'UNC-024', 'priority': 'LOW', 'category': 'INVENTORY',
        'topic': 'CostMethod per item (FIFO/MAC/Standard)',
        'discovered_from': 'DEV: no cost method',
        'why_user_hasnt_considered': 'Default moving average assumed',
        'evidence': 'DEV no Item.CostMethod field',
        'impact': 'Cannot support Standard Cost (manufacturing) or FIFO (regulated)',
        'recommendation': 'Item.CostMethod enum: MOVING_AVERAGE (default) / STANDARD / FIFO. Per-Plant override possible.',
    },
]
write(os.path.join(OUT_BASE, 'USER_NOT_YET_CONSIDERED_FINDINGS.json'), {
    'meta': {
        'extraction_time': EXTRACTION_TIME,
        'session': SESSION,
        'goal': 'BASE-000',
        'note': 'Findings the user has NOT explicitly considered but DEV/Industry evidence suggests should be on the radar. HIGH=act now, MEDIUM=before module, LOW=consider.',
    },
    'findings': not_considered,
    'by_priority': {
        'HIGH': [x for x in not_considered if x['priority'] == 'HIGH'],
        'MEDIUM': [x for x in not_considered if x['priority'] == 'MEDIUM'],
        'LOW': [x for x in not_considered if x['priority'] == 'LOW'],
    },
})
print(f'  Wrote USER_NOT_YET_CONSIDERED_FINDINGS.json ({len(not_considered)} items: HIGH={len([x for x in not_considered if x["priority"]=="HIGH"])}, MEDIUM={len([x for x in not_considered if x["priority"]=="MEDIUM"])}, LOW={len([x for x in not_considered if x["priority"]=="LOW"])})')


# =============================================================================
# 4. P0_FOUNDATION_CANDIDATES (≤15)
# =============================================================================
print()
print('=== Building P0_FOUNDATION_CANDIDATES ===')

p0 = [
    {
        'id': 'P0-01',
        'title': 'Semantic Data Type & Precision Convention (BASE-001)',
        'why_must_come_first': 'All money/qty/price fields need this. Wrong precision → financial audit failure. Wrong rounding → 0.01 lost per sum.',
        'rework_if_deferred': 'Every SalesOrder/PurchaseOrder/Inventory table would need precision annotation. If we set defaults wrong in POC-003, we change every column. Tax calculations (1.13 multiplications) need scale >= 4 for HALF_EVEN safety.',
        'cost': 'M',
        'effort_breakdown': '1-2 days: write semantic-data-type.json (already done in MDM-000D R1); add C# attribute library; 1 day: migrate POC-003 fields to use attribute; 0.5 day: unit tests',
        'deliverables': ['SemanticTypeRegistry', '[BusinessSemanticType] attribute', '[DecimalPrecision] attribute', 'Generator metadata contract'],
        'dependency': 'Foundation.G2-002 IAuditWriter (already in POC-003)',
        'evidence_files': ['data/bootstrap/reference/system/semantic-data-type.json', 'docs/architecture/MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING.md'],
    },
    {
        'id': 'P0-02',
        'title': 'Numbering Convention (BASE-002)',
        'why_must_come_first': 'DocumentNumber is identity of business documents. Wrong scope (system vs tenant) = production data corruption. Wrong UNIQUE constraint = race condition.',
        'rework_if_deferred': 'Every Doc table (SO/PO/GR/GI/MO/QC) needs NumberSequence + UNIQUE constraint. POC-003 has IBusinessNumberGenerator but no UNIQUE constraint. Migrations need re-run.',
        'cost': 'M',
        'effort_breakdown': '0.5 day: NumberSequence entity + DB UNIQUE constraint; 0.5 day: ResetPolicy enum + manual number support; 0.5 day: tenant scope; 1 day: integration test',
        'deliverables': ['NumberSequence entity', 'UNIQUE (TenantId, DocType, Year, Serial) DB constraint', 'NumberPattern registry', 'POC-003 refactor to use it'],
        'dependency': 'IBusinessNumberGenerator (Foundation.Kernel, already in POC-003)',
        'evidence_files': ['DEV ju-samples/dbo_JU_AutoCode.json (15+5+12 sample rows)'],
    },
    {
        'id': 'P0-03',
        'title': 'Parameter Scope Convention (BASE-003)',
        'why_must_come_first': 'Settings need to be scoped. System/Tenant/Company/Plant. Wrong scope = wrong data leaks between tenants/plants.',
        'rework_if_deferred': 'Each module needs TenantSetting/CompanySetting/PlantSetting. If we hardcode as column, refactor every read site.',
        'cost': 'S',
        'effort_breakdown': '0.5 day: ISystemSetting abstraction; 0.5 day: TenantSetting/CompanySetting/PlantSetting tables; 0.5 day: G2-002 config validation extension; 0.5 day: tests',
        'deliverables': ['ISystemSetting', 'ICompanySetting', 'IPlantSetting contracts', 'TenantSetting/CompanySetting/PlantSetting tables', 'Default currency/timezone/locale per scope'],
        'dependency': 'G2-003 ICurrentTenant/ICurrentCompany (already in place)',
        'evidence_files': ['G2-003 migration: gulierp_company has DefaultCurrency + Timezone already; gulierp_plant has Timezone + CountryCode + CalendarCode'],
    },
    {
        'id': 'P0-04',
        'title': 'Foundation Cross-Cutting Convention (BASE-004)',
        'why_must_come_first': 'Single source of truth for: SnowflakeId, ConcurrencyVersion, AuditLog, DesignLog, SoftDelete, StatusStateMachine, CommentObligation. Each is a small rule, but the SUM is huge if done per-module.',
        'rework_if_deferred': 'Each module would invent its own: id generation, version field, soft delete mechanism, status field. POC-003 already uses some, but not consistently enforced.',
        'cost': 'L',
        'effort_breakdown': '1 day: SnowflakeId (already in G2-001) + ConcurrencyVersion (already pattern); 1 day: AuditLog + DesignLog split; 1 day: SoftDelete entity; 0.5 day: status state machine library; 0.5 day: pg_description migration; 0.5 day: test + doc',
        'deliverables': ['ISoftDelete', 'AggregateState enum (G2-002)', 'IDesignAuditWriter', 'EF Core pg_description convention', 'Architecture test for column comments'],
        'dependency': 'Foundation.G2-001 + G2-002 (already verified)',
        'evidence_files': ['G2-001 verification report', 'G2-002 verification report', 'DEV SysLog + SysDesignLog split (UNC-002)'],
    },
    {
        'id': 'P0-05',
        'title': 'UOM Convention with Dimension + Conversion',
        'why_must_come_first': 'UOM is cross-module (Sales/PO/Inventory/Production). Wrong dimension metadata = kg vs m cross-dimension bugs. Conversion not modeled = WMS inefficiency.',
        'rework_if_deferred': 'SalesOrderLine.UomId, PurchaseOrderLine.UomId, Item.BaseUomId all FK to Uom. If dimension not enforced, each module invents its own guard.',
        'cost': 'S',
        'effort_breakdown': '0.5 day: UomDimension enum (8 dimensions); 0.5 day: UomConversion table (from_code+to_code+ratio+validity); 0.5 day: Item.BaseUomId; 0.5 day: tests',
        'deliverables': ['UomDimension enum', 'UomConversion entity', 'Item.BaseUomId', 'Field attribute [UomDimension]'],
        'dependency': 'MDM-000D R1 (system/uom.json 21 items seed)',
        'evidence_files': ['DEV ju-samples/dbo_JU_DataType.json (BaseType inferred)'],
    },
    {
        'id': 'P0-06',
        'title': 'Currency + ExchangeRate Convention',
        'why_must_come_first': 'Money without currency = nonsense. Cross-currency transactions need ExchangeRate with valid date.',
        'rework_if_deferred': 'Each module that has money needs CurrencyCode + ExchangeRateId. If not done early, backfill is painful.',
        'cost': 'S',
        'effort_breakdown': '0.5 day: Currency entity (ISO 4217 subset); 0.5 day: ExchangeRate entity; 0.5 day: ExchangeRateProvider interface; 0.5 day: tests',
        'deliverables': ['Currency entity', 'ExchangeRate entity', 'IExchangeRateProvider', 'ISOCurrencyRegistry (dev seed from MDM-000D R1)'],
        'dependency': 'MDM-000D R1 (system/currency.json 20 ISO 4217 items seed)',
        'evidence_files': ['DEV has no currency table; MDM-000D R1 prepared seed'],
    },
    {
        'id': 'P0-07',
        'title': 'Tax Code + Inclusive/Exclusive Convention',
        'why_must_come_first': 'Tax is on every SalesOrderLine and PurchaseOrderLine. Inclusive/Exclusive flag changes amount meaning.',
        'rework_if_deferred': 'SO/PO/GR/GI all need TaxCodeId + TaxInclusive flag. Add later = migrate every line.',
        'cost': 'M',
        'effort_breakdown': '0.5 day: TaxCode entity; 0.5 day: SalesOrderLine/POLine/GRLine fields; 1 day: tax calculation service with inclusive/exclusive; 0.5 day: tests',
        'deliverables': ['TaxCode entity', 'TaxCalculationService', 'TaxInclusive flag on lines', 'Tax rounding policy'],
        'dependency': 'P0-01 (semantic type for TAX_RATE)',
        'evidence_files': ['DEV 收支科目 dictionary (7 items, 5 types)  --  too coarse for tax'],
    },
    {
        'id': 'P0-08',
        'title': 'InventoryStatus Convention',
        'why_must_come_first': 'Available vs Reserved vs Blocked is core. Wrong = oversold or understocked.',
        'rework_if_deferred': 'InventoryBalance needs 5 separate decimal fields. POC-002 design already exists; this is to enforce + tests.',
        'cost': 'S',
        'effort_breakdown': '0.5 day: InventoryStatus enum; 0.5 day: InventoryBalance schema with 5 fields; 0.5 day: IInventoryService.PostAsync; 0.5 day: tests',
        'deliverables': ['InventoryStatus enum', 'InventoryBalance 5-field design', 'IInventoryService.PostAsync single entry'],
        'dependency': 'P0-04 (AggregateState + AuditWriter)',
        'evidence_files': ['MDM-000 P1-005 plan', 'POC-002/003 design'],
    },
    {
        'id': 'P0-09',
        'title': 'BP Code Format + Address/Contact Convention',
        'why_must_come_first': 'BP is shared across Sales/Purchase/Inventory. Code format is hard to backfill.',
        'rework_if_deferred': 'Customer / Supplier / Subcontractor / Logistics all share BP table. Wrong code format = rename campaign later.',
        'cost': 'S',
        'effort_breakdown': '0.5 day: BP.Code prefix (CUST-/SUPP-/SUB-/LOG-); 0.5 day: BPAddress entity (type=BILLING/SHIPPING/REGISTERED); 0.5 day: BPContact (type=PHONE/EMAIL/FAX); 0.5 day: tests',
        'deliverables': ['BP.Code prefix enforcement', 'BPAddress entity', 'BPContact entity', 'BPType enum (CUSTOMER/SUPPLIER/SUBCONTRACTOR/LOGISTICS)'],
        'dependency': 'MDM-000D R1 (tenant-template/business-partner-type.json 4 types seed)',
        'evidence_files': ['DEV 往来表 sample: 往来号=20001 (no prefix)'],
    },
    {
        'id': 'P0-10',
        'title': 'Item Master Convention (Type/Category/BaseUom)',
        'why_must_come_first': 'Item is shared. Type/Category/BaseUom are FK fields, not strings. Wrong = each module re-derives.',
        'rework_if_deferred': 'Item is in Inventory/Sales/Production/QC. If type is string, each module re-validates. Promote to enum + FK.',
        'cost': 'S',
        'effort_breakdown': '0.5 day: ItemType enum (5 types incl. Phantom); 0.5 day: ItemCategory tree entity; 0.5 day: Item.BaseUomId; 0.5 day: tests',
        'deliverables': ['ItemType enum', 'ItemCategory entity', 'Item.BaseUomId', 'Item.QualityInspectionRequired flag'],
        'dependency': 'P0-05 (UOM dimension), P0-08 (Inventory status) ',
        'evidence_files': ['DEV 商品属性 dictionary (4 items: 自制/外购/委外加工/客供); 商品分类 (5 items sample)'],
    },
    {
        'id': 'P0-11',
        'title': 'Document Status State Machine Library',
        'why_must_come_first': 'SO has Draft/Confirmed/Shipped/Closed. PO has Draft/Confirmed/Received/Closed. Without library, each entity rolls own state machine  --  bugs.',
        'rework_if_deferred': 'POC-003 already has SalesOrder state machine. Without library, future modules will copy-paste.',
        'cost': 'M',
        'effort_breakdown': '0.5 day: IAggregateStateMachine<T> abstraction; 0.5 day: state machine library (use Stateless or hand-rolled); 0.5 day: per-entity state config; 0.5 day: tests',
        'deliverables': ['IAggregateStateMachine', 'GenericStateMachine<T>', 'StateTransition table (audit of state changes)'],
        'dependency': 'P0-04 (AggregateState + AuditWriter)',
        'evidence_files': ['POC-003 SalesOrderState.cs'],
    },
    {
        'id': 'P0-12',
        'title': 'Field-Level Comment Obligation',
        'why_must_come_first': 'pg_description on every column. Cheap to do, hard to retrofit.',
        'rework_if_deferred': 'Future dev onboard slower. Schema documentation rots.',
        'cost': 'XS',
        'effort_breakdown': '0.25 day: EF Core convention for pg_description; 0.25 day: architecture test (ArchUnit) for column comments; 0.25 day: doc',
        'deliverables': ['IEntityCommentConfiguration convention', 'Architecture test', 'Migration to add missing comments'],
        'dependency': 'Foundation.G2-001 (already verified)',
        'evidence_files': ['DEV 0/151 JU_ tables have ms_description  --  anti-pattern'],
    },
    {
        'id': 'P0-13',
        'title': 'Concurrency Version Pattern Enforcement',
        'why_must_come_first': 'ConcurrencyVersion int already in G2-003 tables. Without enforcement, new tables may forget.',
        'rework_if_deferred': 'Each module may invent its own row version. POC-003 has it; future modules must follow.',
        'cost': 'XS',
        'effort_breakdown': '0.25 day: IVersioned interface; 0.25 day: EF Core convention; 0.25 day: architecture test',
        'deliverables': ['IVersioned interface', 'EF Core global convention', 'Architecture test'],
        'dependency': 'G2-003 Identity already has ConcurrencyVersion in every table',
        'evidence_files': ['Identity migration v1  --  every table has ConcurrencyVersion int'],
    },
    {
        'id': 'P0-14',
        'title': 'AggregateState Status (Replace 3-field status anti-pattern)',
        'why_must_come_first': 'DEV anti-pattern: ReportStatus + LockStatus + WorkflowStatus on same entity. Untestable combinations.',
        'rework_if_deferred': 'POC-003 already uses 3D model (DocumentStatus/ApprovalStatus/ExecutionStatus). Make this a Foundation contract.',
        'cost': 'S',
        'effort_breakdown': '0.5 day: DocumentStatus/ApprovalStatus/ExecutionStatus enums; 0.5 day: IDocumentState<T> contract; 0.5 day: tests',
        'deliverables': ['DocumentStatus enum', 'ApprovalStatus enum', 'ExecutionStatus enum', 'IDocumentState<T> contract'],
        'dependency': 'P0-11 (state machine library)',
        'evidence_files': ['G2-002 plan', 'POC-003 implementation'],
    },
    {
        'id': 'P0-15',
        'title': 'IDiscoverableAndTruncatable Configuration (Preflight)',
        'why_must_come_first': 'G2-004R1 had PowerShell $PID collision bug. Code generator pitfalls. Need a process to discover pitfalls before they bite.',
        'rework_if_deferred': 'P0-05..P0-14 each could be caught at discovery time instead of after implementation.',
        'cost': 'XS',
        'effort_breakdown': '0.5 day: pattern catalog of common ERP gotchas (DEV-derived); 0.5 day: preflight checklist template',
        'deliverables': ['PREFLIGHT_ERP_GOTCHAS.md', 'Each future goal reviews against it before kickoff'],
        'dependency': 'This BASE-000 itself',
        'evidence_files': ['G2-004R1 PID bug, G2-003V2 RI report'],
    },
]
write(os.path.join(OUT_BASE, 'P0_FOUNDATION_CANDIDATES.json'), {
    'meta': {
        'extraction_time': EXTRACTION_TIME,
        'session': SESSION,
        'goal': 'BASE-000',
        'note': 'P0 candidates are the must-do-before-module items. Total cost must NOT balloon into multi-week engineering. Cap at 15.',
        'cost_legend': {'XS': '0.5 day', 'S': '1-2 days', 'M': '3-5 days', 'L': '1-2 weeks'},
        'total_p0_count': len(p0),
    },
    'p0_candidates': p0,
})
print(f'  Wrote P0_FOUNDATION_CANDIDATES.json ({len(p0)} items)')


# =============================================================================
# 5. IMPLEMENTATION_QUEUE
# =============================================================================
print()
print('=== Building IMPLEMENTATION_QUEUE ===')

queue = [
    {
        'id': 'BASE-001',
        'name': 'Semantic Data Type & Precision Convention',
        'why': 'All money/qty/price fields need this. Wrong precision → financial audit failure. Already seeded in MDM-000D R1.',
        'dependency': 'G2-002 Foundation verified',
        'expected_duration_days': 3,
        'deliverable': '[BusinessSemanticType] + [DecimalPrecision] attributes; semantic-data-type.json fully accepted',
        'gate': 'BASE_001_SEMANTIC_TYPE_ACCEPTED',
        'implements_p0': 'P0-01',
    },
    {
        'id': 'BASE-002',
        'name': 'Numbering Convention',
        'why': 'Document numbers are business identity. Need UNIQUE constraint + tenant scope + ResetPolicy.',
        'dependency': 'G2-002 IBusinessNumberGenerator (POC-003), P0-01 (semantic for NUMBER)',
        'expected_duration_days': 2,
        'deliverable': 'NumberSequence entity, UNIQUE constraint, ResetPolicy enum, manual number support',
        'gate': 'BASE_002_NUMBERING_ACCEPTED',
        'implements_p0': 'P0-02',
    },
    {
        'id': 'BASE-003',
        'name': 'Parameter Scope Convention',
        'why': 'Settings must be scoped SYSTEM/TENANT/COMPANY/PLANT. Wrong scope = data leak.',
        'dependency': 'G2-003 ICurrentTenant/ICurrentCompany',
        'expected_duration_days': 2,
        'deliverable': 'ISystemSetting + TenantSetting/CompanySetting/PlantSetting',
        'gate': 'BASE_003_PARAMETER_SCOPE_ACCEPTED',
        'implements_p0': 'P0-03',
    },
    {
        'id': 'BASE-004',
        'name': 'Foundation Cross-Cutting Convention',
        'why': 'SnowflakeId + ConcurrencyVersion + AuditLog + DesignLog + SoftDelete + AggregateState. Each is small; together they prevent re-inventing in each module.',
        'dependency': 'G2-001 + G2-002',
        'expected_duration_days': 5,
        'deliverable': 'ISoftDelete, AggregateState, IDesignAuditWriter, pg_description convention',
        'gate': 'BASE_004_FOUNDATION_CROSS_CUTTING_ACCEPTED',
        'implements_p0': 'P0-04, P0-13, P0-14',
    },
    {
        'id': 'BASE-005',
        'name': 'UOM + Currency + Tax (Master Reference Data)',
        'why': 'UOM (P0-05) + Currency (P0-06) + Tax (P0-07) are all cross-module master data. Need dimension metadata, ISO 4217, inclusive/exclusive tax.',
        'dependency': 'BASE-001, BASE-003',
        'expected_duration_days': 4,
        'deliverable': 'Uom/UomConversion/UomDimension; Currency/ExchangeRate/IExchangeRateProvider; TaxCode/TaxCalculationService',
        'gate': 'BASE_005_REFERENCE_DATA_ACCEPTED',
        'implements_p0': 'P0-05, P0-06, P0-07',
    },
    {
        'id': 'BASE-006',
        'name': 'BusinessPartner + Item + Category (Master Data)',
        'why': 'BP (P0-09) + Item (P0-10) + Category are shared across modules. Code formats, address/contact structure, type taxonomy.',
        'dependency': 'BASE-005 (UOM, Currency, Tax)',
        'expected_duration_days': 4,
        'deliverable': 'BP/BPAddress/BPContact/Item/ItemCategory/ItemType',
        'gate': 'BASE_006_MASTER_DATA_ACCEPTED',
        'implements_p0': 'P0-09, P0-10',
    },
    {
        'id': 'BASE-007',
        'name': 'Warehouse + Location + InventoryStatus',
        'why': 'Inventory core. WarehousePolicy per Plant, LocationType, InventoryStatus 5-state balance.',
        'dependency': 'BASE-006 (Item), BASE-005 (UOM)',
        'expected_duration_days': 4,
        'deliverable': 'Warehouse/Location/InventoryBalance (5-state)/IInventoryService.PostAsync',
        'gate': 'BASE_007_INVENTORY_FOUNDATION_ACCEPTED',
        'implements_p0': 'P0-08',
    },
    {
        'id': 'BASE-008',
        'name': 'Document State Machine Library',
        'why': 'Reusable state machine for SO/PO/GR/MO/QC. POC-003 has it for SO; need to generalize.',
        'dependency': 'BASE-004 (AggregateState)',
        'expected_duration_days': 2,
        'deliverable': 'IAggregateStateMachine<T>, StateTransition audit',
        'gate': 'BASE_008_STATE_MACHINE_ACCEPTED',
        'implements_p0': 'P0-11',
    },
    {
        'id': 'BASE-009',
        'name': 'Field-Level Comment Obligation + Architecture Tests',
        'why': 'Cheap now, hard to retrofit. pg_description on every column. ArchUnit test for invariants.',
        'dependency': 'BASE-004',
        'expected_duration_days': 1,
        'deliverable': 'pg_description convention, ArchUnit tests, run in CI',
        'gate': 'BASE_009_COMMENT_OBLIGATION_VERIFIED',
        'implements_p0': 'P0-12, P0-15',
    },
    {
        'id': 'BASE-010',
        'name': 'Discovery Preflight Checklist (Process)',
        'why': 'Document common ERP gotchas (DEV-derived) for future goals to review pre-kickoff. Prevents P0-15 class of issues.',
        'dependency': 'BASE-000 itself',
        'expected_duration_days': 1,
        'deliverable': 'docs/process/PREFLIGHT_ERP_GOTCHAS.md, applied to next goal kickoff',
        'gate': 'BASE_010_PREFLIGHT_PUBLISHED',
        'implements_p0': 'P0-15',
    },
]
write(os.path.join(OUT_BASE, 'IMPLEMENTATION_QUEUE.json'), {
    'meta': {
        'extraction_time': EXTRACTION_TIME,
        'session': SESSION,
        'goal': 'BASE-000',
        'note': 'Sequence: BASE-001 → 002 → 003 → 004 (foundation) → 005 (reference) → 006 (master) → 007 (inventory) → 008 (state) → 009 (tests) → 010 (process). Parallel where deps allow.',
    },
    'queue': queue,
    'total_estimated_days': sum(q['expected_duration_days'] for q in queue),
})
print(f'  Wrote IMPLEMENTATION_QUEUE.json ({len(queue)} tasks, total {sum(q["expected_duration_days"] for q in queue)} days)')

print()
print('=== All 5 outputs written ===')
for f in ['DATABASE_SOURCE_INVENTORY', 'FOUNDATION_AND_CONFIGURATION_DISCOVERY_MATRIX', 'USER_NOT_YET_CONSIDERED_FINDINGS', 'P0_FOUNDATION_CANDIDATES', 'IMPLEMENTATION_QUEUE']:
    print(f'  tools/discovery/base-000/_canonical/{f}.json')
