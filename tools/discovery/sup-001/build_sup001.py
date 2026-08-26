"""
Build SUP-001 outputs (per Codex-aligned constraints):

Constraint reminders (per task §7):
  candidateDecision ∈ {REUSE, ADAPT, REFERENCE_ONLY, UNKNOWN}
  NEVER use FROZEN, IMPLEMENTED, REJECT, NEW_BUILD, REUSE_PATTERN.

Constraint reminders (per task §6):
  RoundingMode when not found  -> literal string "ROUNDING_MODE_NOT_FOUND"
  Do NOT pick AwayFromZero / ToEven by experience.

Sources are READ-ONLY DEV reverse-engineering JSON dumps.
No live DB accessed. No G2-004 / Identity / G2-005 / MDM-000D / BASE-000 modified.
"""
import json
from pathlib import Path
from collections import Counter
from datetime import datetime, timezone

EXTRACTION_TIME = '2026-08-20T13:08:40+08:00'
SESSION = 'mvs_11a243eed8e544d6b19087711a392283'
GOAL = 'SUP-001'

BASE = Path(r'D:\guli\projects\gulierp-next\tools\discovery\sup-001')
OUT = BASE / '_canonical'
OUT.mkdir(parents=True, exist_ok=True)

DEV = Path(r'D:\guli\gulierp\docs\reverse-engineering\dev-meta')

# Allowed values (defensive — fail loudly if anyone adds REJECT/NEW_BUILD)
ALLOWED_DECISIONS = {'REUSE', 'ADAPT', 'REFERENCE_ONLY', 'UNKNOWN'}

# Sources
biz = json.loads((DEV / 'biz-tables.json').read_text(encoding='utf-8'))
autocode_sample = json.loads((DEV / 'ju-samples' / 'dbo__JU_AutoCode.json').read_text(encoding='utf-8'))
autocode_register = json.loads((DEV / 'ju-samples' / 'dbo__JU_AutoCodeRegister.json').read_text(encoding='utf-8'))
autocode_field = json.loads((DEV / 'ju-samples' / 'dbo__JU_AutoCodeField.json').read_text(encoding='utf-8'))
item_sample = json.loads((DEV / 'biz-samples' / 'dbo__商品表.json').read_text(encoding='utf-8'))


def check(decision):
    if decision not in ALLOWED_DECISIONS:
        raise ValueError(f"candidateDecision '{decision}' NOT in {ALLOWED_DECISIONS}")


# =================================================================
# 1. Document Numbering Evidence
# =================================================================
def build_numbering():
    # DateType interpretation is best-effort from 5/15 sample rows.
    # DateType=6 + SeedLength=6 observed in 订单ID; DateType=8 + SeedLength=3
    # observed in 销售订单号 / 收发单ID. Full enum NOT in evidence.
    DATETYPE_INTERPRETATION = {
        6: 'PROPOSED: yyyyMM (year + month) — observed SeedLength=6 (ref AutoCodeID=100 订单ID). Confidence MEDIUM; full DateType enum NOT in this evidence pack.',
        8: 'PROPOSED: yyyyMMdd (year + month + day) — observed SeedLength=3 (ref AutoCodeID=103 销售订单号, 104 收发单ID). Confidence MEDIUM; full DateType enum NOT in this evidence pack.',
        None: 'NULL in sample row. Interpretation UNKNOWN.',
    }

    items = []
    for r in autocode_sample['rows']:
        decision = 'REUSE' if r.get('ResLvl') == '框架' else 'REFERENCE_ONLY'
        check(decision)
        items.append({
            'sourceSystem': 'DEV',
            'sourceDatabase': 'SQL Server (per ju-meta.json)',
            'sourceTable': 'dbo.JU_AutoCode',
            'sourceColumn': '(row)',
            'sourceId': f"AutoCodeID={r['AutoCodeID']}",
            'sourceCode': str(r.get('Prefix', '') or ''),
            'sourceName': r.get('AutoCodeName', ''),
            'sourceRemark': r.get('Memo', '') or '(no memo)',
            'documentType': r.get('AutoCodeName', ''),
            'prefix': r.get('Prefix') or '',
            'dateTypeRaw': r.get('DateType'),
            'dateTypeInterpretation': DATETYPE_INTERPRETATION.get(
                r.get('DateType'),
                'UNKNOWN — DateType value not in observed sample (6, 8).',
            ),
            'dateTypeConfidence': 'MEDIUM' if r.get('DateType') in (6, 8) else 'UNKNOWN',
            'sequenceLength': r.get('SeedLength'),
            'sequenceStart': r.get('SeedStart'),
            'runBeforeSave': r.get('RunBeforeSave'),
            'allowMoreManualAllowed': (r.get('AllowMore') == 1),
            'allowBatch': r.get('AllowBatch'),
            'reuseType': r.get('ReuseType'),
            'resLvl': r.get('ResLvl', ''),
            'isActive': (r.get('ISActive') == 1),
            'createTime': r.get('CreateTime'),
            'example': None,
            'confidence': 'HIGH' if r.get('ResLvl') == '框架' else 'MEDIUM',
            'interpretation': (
                f"AutoCode {r['AutoCodeID']} '{r.get('AutoCodeName','')}': "
                f"prefix={r.get('Prefix') or '(empty)'} + "
                f"date-component (raw DateType={r.get('DateType')}) + "
                f"sequence of length {r.get('SeedLength')} starting at {r.get('SeedStart')}. "
                f"AllowMore={r.get('AllowMore')} means manual override is "
                f"{'allowed' if r.get('AllowMore') == 1 else 'NOT allowed'}. "
                f"ResLvl='{r.get('ResLvl','')}' indicates scope category in DEV."
            ),
            'candidateDecision': decision,
        })

    register_items = []
    for r in autocode_register['rows']:
        check('REUSE')
        register_items.append({
            'sourceSystem': 'DEV',
            'sourceTable': 'dbo.JU_AutoCodeRegister',
            'sourceId': f"AutoCodeID={r['AutoCodeID']}, PrimaryPart={r['PrimaryPart']}",
            'sourceCode': r['PrimaryPart'],
            'sourceName': f"Counter seed={r['CurrentSeed']}",
            'autoCodeId': r['AutoCodeID'],
            'currentSeed': r['CurrentSeed'],
            'primaryPart': r['PrimaryPart'],
            'interpretation': (
                f"Counter {r['AutoCodeID']} current seed={r['CurrentSeed']}; "
                f"primary part '{r['PrimaryPart']}' is the locked (prefix+date) portion. "
                f"Multiple PrimaryPart rows for same AutoCodeID = multi-period counter."
            ),
            'confidence': 'HIGH',
            'candidateDecision': 'REUSE',
        })

    field_items = []
    for r in autocode_field['rows']:
        check('REUSE')
        field_items.append({
            'sourceSystem': 'DEV',
            'sourceTable': 'dbo.JU_AutoCodeField',
            'sourceId': f"AutoCodeFieldID={r['AutoCodeFieldID']}",
            'sourceCode': r.get('FieldDispName', ''),
            'sourceName': f"FieldType={r['FieldType']}, FieldExpr={r.get('FieldExpr')}",
            'autoCodeId': r['AutoCodeID'],
            'fieldType': r['FieldType'],
            'fieldTypeInterpretation': (
                f"FieldType={r['FieldType']}: PROPOSED — observed 3 (literal constant, "
                f"per FieldExpr='{r.get('FieldExpr')}') and 4 (table/report field). "
                f"Full FieldType enum NOT in this evidence pack."
            ),
            'fieldExpr': r.get('FieldExpr'),
            'fieldDispName': r.get('FieldDispName'),
            'dispSeq': r.get('DispSeq'),
            'confidence': 'MEDIUM',
            'candidateDecision': 'REUSE',
        })

    return {
        'meta': {
            'goal': GOAL,
            'extraction_time': EXTRACTION_TIME,
            'session': SESSION,
            'source': 'DEV reverse-engineering JSON (READ-ONLY)',
            'policy': (
                'No live DB accessed. No G2-004 / Identity / G2-005 / MDM-000D / '
                'BASE-000 files modified. candidateDecision ∈ {REUSE, ADAPT, REFERENCE_ONLY, UNKNOWN} only.'
            ),
            'sample_size': len(autocode_sample['rows']),
            'register_sample_size': len(autocode_register['rows']),
            'field_sample_size': len(autocode_field['rows']),
            'total_autocode_estimated_in_DEV': 15,
            'evidence_gap': (
                'Only 5 of 15 AutoCode rows in JSON dump. Remaining 10 unknown. '
                'NOT expanded to full live DB per task §3 (no PG DB access) and §1 (no full reverse-engineering).'
            ),
        },
        'autocode': items,
        'autocode_register': register_items,
        'autocode_field': field_items,
        'observations': [
            {
                'id': 'NUM-OBS-001',
                'topic': 'NO_TENANT_SCOPE_COLUMN',
                'evidence': 'JU_AutoCode has no TenantId / CompanyId / PlantId column. Counter is system-global per DEV design.',
                'impact': 'Cross-tenant collision on SO/PO numbers if multiple tenants share one DB. (Cross-ref BASE-000 UNC-001.)',
                'candidateDecision': 'UNKNOWN',
                'decisionReasoning': (
                    "Dev-observed fact is 'no tenant scope'. Whether GuliERP should "
                    'add it is a CONVENTION decision — left UNKNOWN at evidence stage.'
                ),
            },
            {
                'id': 'NUM-OBS-002',
                'topic': 'NO_EXPLICIT_RESET_PERIOD_COLUMN',
                'evidence': 'JU_AutoCode has no explicit ResetPeriod column. Reset is implicit via DateType int (observed 6, 8) and PrimaryPart in JU_AutoCodeRegister.',
                'impact': 'Reset semantics encoded in DateType enum, not declarative. GuliERP may want explicit ResetPeriod (NEVER/YEARLY/MONTHLY/DAILY) — see BASE-000 P0-02.',
                'candidateDecision': 'UNKNOWN',
            },
            {
                'id': 'NUM-OBS-003',
                'topic': 'NO_DB_UNIQUE_CONSTRAINT_ON_DOCUMENT_NUMBER',
                'evidence': "size.json shows only 1 default constraint in entire DEV DB; no UNIQUE pattern observed on document number columns.",
                'impact': 'Race condition: two concurrent inserts could allocate same number. (Cross-ref BASE-000 UNC-004.)',
                'candidateDecision': 'UNKNOWN',
            },
            {
                'id': 'NUM-OBS-004',
                'topic': 'ALLOW_MORE_DEFAULT_ZERO',
                'evidence': 'AllowMore=0 in all 5 sampled rows.',
                'impact': 'DEV default is no manual override. If GuliERP wants the same default, REUSE the pattern; if different, ADAPT.',
                'candidateDecision': 'REUSE',
            },
            {
                'id': 'NUM-OBS-005',
                'topic': 'RUN_BEFORE_SAVE_VARIANTS',
                'evidence': 'RunBeforeSave observed 0 (sales/recv) and 2 (order/product) in 5 sampled rows.',
                'impact': 'Two timing variants. Exact semantics NOT in this evidence pack. Convention: domain service before Save is one option, not the only one.',
                'candidateDecision': 'ADAPT',
                'decisionReasoning': "Observed 2 variants in DEV with semantics not documented; GuliERP must decide which to keep.",
            },
        ],
        'gap_to_convention': {
            'p0_referenced': 'P0-02 Numbering Convention (BASE-002)',
            'must_decide': [
                'Tenant scope: GLOBAL (DEV pattern) vs PER_TENANT vs PER_PLANT',
                'Reset period: explicit enum vs implicit DateType',
                'Manual override: default block (DEV AllowMore=0) vs default allow',
                'Format pattern: how prefix + date + sequence compose',
                'Allocation atomicity: DB UNIQUE constraint vs app-level lock',
                'DateType enum: copy DEV (6, 8) and extend, or replace with explicit fields',
            ],
        },
    }


# =================================================================
# 2. Master Data Coding Evidence
# =================================================================
def build_coding():
    relevant = ['商品表', '仓库表', 'BOM表', '字典表', '字典表s', '状态表', '员工表',
                '客户表', '供应商表', '外协表', '类别表', '品牌表', '项目表', '凭证表']
    by_table = {}
    for t in biz:
        if t['table'] in relevant:
            by_table[t['table']] = t

    items = []
    sample_row = item_sample['rows'][0]

    # Item coding
    if '商品表' in by_table:
        item_cols = {c['name']: c for c in by_table['商品表']['columns']}
        code_col_specs = [
            ('品号', 'PRIMARY', sample_row.get('品号')),
            ('品名', 'NAME', sample_row.get('品名')),
            ('拼音', 'AID_INDEX', sample_row.get('拼音')),
            ('分类', 'CATEGORY_LABEL', sample_row.get('分类')),
            ('分类ID', 'CATEGORY_REF', sample_row.get('分类ID')),
            ('客户品号', 'CROSS_REF_CUSTOMER', sample_row.get('客户料号')),
            ('供应商品号', 'CROSS_REF_SUPPLIER', sample_row.get('供应商料号')),
            ('客户料号', 'CROSS_REF_CUSTOMER_ALT', sample_row.get('客户料号')),
            ('客户货号', 'CROSS_REF_CUSTOMER', None),
            ('客户件号', 'CROSS_REF_CUSTOMER', None),
            ('客户代码', 'CROSS_REF_CUSTOMER', sample_row.get('客户号')),
            ('客户号', 'CROSS_REF_CUSTOMER', sample_row.get('客户号')),
            ('供应商代码', 'CROSS_REF_SUPPLIER', sample_row.get('供应号')),
            ('供应号', 'CROSS_REF_SUPPLIER', sample_row.get('供应号')),
            ('U8导入', 'LEGACY_EXTERNAL', sample_row.get('U8导入')),
            ('内控码', 'INTERNAL_CONTROL', sample_row.get('内控码')),
            ('版本号', 'VERSION', sample_row.get('版本号')),
        ]
        for cn, role, sample in code_col_specs:
            if cn not in item_cols:
                continue
            col = item_cols[cn]
            null_marker = sample in (None, '', '+') if sample is not None else True
            if role == 'PRIMARY':
                decision = 'REUSE'
            elif role in ('NAME', 'AID_INDEX', 'CATEGORY_REF', 'CATEGORY_LABEL'):
                decision = 'REUSE'
            elif role.startswith('CROSS_REF'):
                decision = 'REFERENCE_ONLY'  # cross-ref location needs GuliERP decision
            else:
                decision = 'ADAPT'
            check(decision)
            items.append({
                'sourceSystem': 'DEV',
                'sourceTable': 'dbo.商品表',
                'sourceColumn': cn,
                'role': role,
                'declaration': f"{col.get('type')}({col.get('max_length', 0)})",
                'nullable': col.get('nullable'),
                'sampleValue': (str(sample) if sample is not None else '(null)'),
                'isNullMarker': null_marker,
                'confidence': 'HIGH' if sample not in (None, '', '+') else 'MEDIUM',
                'interpretation': (
                    f"商品表.{cn} ({role}). Cross-ref BASE-000 UNC-009/014: separate "
                    f"PRIMARY (one per tenant) from CROSS_REFERENCE (many per source system)."
                ),
                'candidateDecision': decision,
            })

    # Warehouse coding
    if '仓库表' in by_table:
        wh_cols = {c['name']: c for c in by_table['仓库表']['columns']}
        for cn, role, decl in [
            ('仓库', 'PRIMARY', 'nvarchar(256) NOT NULL'),
            ('仓库类型', 'TYPE', 'nvarchar(256)'),
            ('仓库用途', 'PURPOSE', 'nvarchar(256)'),
        ]:
            if cn in wh_cols:
                check('ADAPT')
                items.append({
                    'sourceSystem': 'DEV',
                    'sourceTable': 'dbo.仓库表',
                    'sourceColumn': cn,
                    'role': role,
                    'declaration': decl,
                    'nullable': wh_cols[cn].get('nullable'),
                    'sampleValue': '(no sample row in DEV — 0 rows)',
                    'isNullMarker': None,
                    'confidence': 'MEDIUM',
                    'interpretation': (
                        f"仓库表.{cn}: Warehouse uses name-as-code. GuliERP may want "
                        f"separate Code + Name per BASE-000 UNC-022."
                    ),
                    'candidateDecision': 'ADAPT',
                })

    # Dictionary coding
    if '字典表' in by_table:
        check('REUSE')
        items.append({
            'sourceSystem': 'DEV',
            'sourceTable': 'dbo.字典表',
            'sourceColumn': '(header)',
            'role': 'DICTIONARY_HEADER',
            'declaration': 'RecordID bigint PK + 字典名 nvarchar(256) + 名称 nvarchar(256) + 助记码 nvarchar(256) + 父级ID + RTID',
            'nullable': None,
            'sampleValue': '28 dictionary headers in DEV',
            'isNullMarker': None,
            'confidence': 'HIGH',
            'interpretation': 'Dictionary header: 28 flat categories (学历/民族/职位/...). MDM-000D R1 already curated these into seed candidates.',
            'candidateDecision': 'REUSE',
        })
    if '字典表s' in by_table:
        check('REUSE')
        items.append({
            'sourceSystem': 'DEV',
            'sourceTable': 'dbo.字典表s',
            'sourceColumn': '编码/名称/助记码',
            'role': 'DICTIONARY_ITEM_CODE_NAME',
            'declaration': 'RecordID int PK + Sequence int + RN decimal(32,0) + 编码 nvarchar(256) + 名称 nvarchar(256) + 助记码 nvarchar(256)',
            'nullable': None,
            'sampleValue': '121 items in DEV',
            'isNullMarker': None,
            'confidence': 'HIGH',
            'interpretation': (
                'Dictionary item: Code + Name + 助记码 (memory aid). This is the only '
                'DEV table that explicitly separates Code (immutable) from Name (display). '
                'MDM-000D R1 uses this Code+Name pattern for all reference data.'
            ),
            'candidateDecision': 'REUSE',
        })

    # Voucher
    if '凭证表' in by_table:
        check('REUSE')
        items.append({
            'sourceSystem': 'DEV',
            'sourceTable': 'dbo.凭证表',
            'sourceColumn': '凭证号/凭证字/凭证字号',
            'role': 'VOUCHER_NUMBER_3PART',
            'declaration': '凭证号 nvarchar(256) NOT NULL + 凭证字 nvarchar(256) + 凭证字号 int',
            'nullable': None,
            'sampleValue': '(0 rows in DEV — schema only)',
            'isNullMarker': None,
            'confidence': 'MEDIUM',
            'interpretation': 'Voucher 3-part: 字 (type) + 号 (number) + 字号 (int suffix). China accounting convention.',
            'candidateDecision': 'REUSE',
        })

    # Status table
    if '状态表' in by_table:
        check('ADAPT')
        items.append({
            'sourceSystem': 'DEV',
            'sourceTable': 'dbo.状态表',
            'sourceColumn': '(row)',
            'role': 'STATUS_REFERENCE_GENERIC',
            'declaration': '17 cols, 13 rows',
            'nullable': None,
            'sampleValue': '13 status rows in DEV',
            'isNullMarker': None,
            'confidence': 'HIGH',
            'interpretation': (
                '状态表 is a single 17-col generic status reference. Per-document-type '
                'status machine (DocumentStatus enum) is the more modern pattern (see '
                'BASE-000 P0-11/P0-14). DEV single-table pattern may be adapted if '
                'GuliERP wants a flat cross-cutting status registry.'
            ),
            'candidateDecision': 'ADAPT',
        })

    # BP evidence gap
    if not any(t in by_table for t in ('客户表', '供应商表', '外协表')):
        check('UNKNOWN')
        items.append({
            'sourceSystem': 'DEV',
            'sourceTable': '(no dedicated BP table in DEV)',
            'sourceColumn': '(none)',
            'role': 'BP_MASTER_GAP',
            'declaration': 'No 客户表 / 供应商表 / 外协表 in DEV biz-tables.json',
            'nullable': None,
            'sampleValue': '(DEV has no dedicated BP entity)',
            'isNullMarker': None,
            'confidence': 'HIGH',
            'interpretation': (
                'BP info is scattered as fields on 商品表 (客户号 / 供应号 / 客户料号 / '
                '供应商品号). DEV has NO dedicated BP master. Whether GuliERP wants a '
                'dedicated BP entity is a CONVENTION decision — left UNKNOWN at '
                'evidence stage.'
            ),
            'candidateDecision': 'UNKNOWN',
        })

    return {
        'meta': {
            'goal': GOAL,
            'extraction_time': EXTRACTION_TIME,
            'session': SESSION,
            'source': 'DEV reverse-engineering JSON (READ-ONLY)',
            'policy': 'No live DB accessed; only JSON dumps',
            'tables_inspected': list(by_table.keys()),
            'candidateDecision_allowed': sorted(ALLOWED_DECISIONS),
        },
        'items': items,
        'observations': [
            {
                'id': 'CODE-OBS-001',
                'topic': 'ITEM_HAS_10PLUS_CODE_FIELDS',
                'evidence': '商品表 has 10+ code-like fields mixing primary + cross-reference.',
                'impact': 'GuliERP may want to separate PRIMARY (one per tenant) from CROSS_REFERENCE (many per source system).',
                'candidateDecision': 'UNKNOWN',
            },
            {
                'id': 'CODE-OBS-002',
                'topic': 'PURE_NUMERIC_PRIMARY_CODE',
                'evidence': "商品表 sample: 品号='1000001' (pure 7-digit numeric, no category info).",
                'impact': 'Numeric codes do not carry category. GuliERP may adopt structured prefix.',
                'candidateDecision': 'ADAPT',
                'decisionReasoning': 'Numeric style is REUSE; structured prefix is a design choice — ADAPT.',
            },
            {
                'id': 'CODE-OBS-003',
                'topic': "LITERAL_PLUS_AS_NULL_MARKER",
                'evidence': "商品表 sample: 客户号='+', 供应号='+' (literal + as null-marker).",
                'impact': 'Anti-pattern. GuliERP should use proper SQL NULL.',
                'candidateDecision': 'UNKNOWN',
                'decisionReasoning': 'Evidence documents the anti-pattern. Whether GuliERP rejects it is a CONVENTION decision — UNKNOWN at evidence stage.',
            },
            {
                'id': 'CODE-OBS-004',
                'topic': 'WAREHOUSE_NAME_AS_CODE',
                'evidence': "仓库表.仓库 nvarchar(256) NOT NULL used as code (name-as-code).",
                'impact': 'Name and code conflated. GuliERP may want separate Code + Name.',
                'candidateDecision': 'ADAPT',
            },
            {
                'id': 'CODE-OBS-005',
                'topic': 'DICTIONARY_HAS_CODE_AND_NAME',
                'evidence': '字典表s has 编码 (Code) + 名称 (Name) + 助记码 (memory aid).',
                'impact': 'DEV Dictionary is the only place with explicit Code+Name separation. MDM-000D R1 already uses this pattern.',
                'candidateDecision': 'REUSE',
            },
            {
                'id': 'CODE-OBS-006',
                'topic': 'NO_DEDICATED_BP_TABLE',
                'evidence': 'No 客户表 / 供应商表 / 外协表 in DEV. BP info lives as 商品表 fields.',
                'impact': 'DEV has no dedicated BP entity. Whether GuliERP wants one is a CONVENTION decision.',
                'candidateDecision': 'UNKNOWN',
            },
        ],
        'gap_to_convention': {
            'p0_referenced': 'P0-09 BP Code Format + Address/Contact (BASE-002) and P0-10 Item Master (BASE-002)',
            'must_decide': [
                'Item code: pure numeric (DEV pattern) vs structured FG-/RM-/SF- prefix',
                'BP code: pure numeric (DEV pattern) vs CUST-/SUPP-/SUB-/LOG- prefix',
                'Cross-reference codes: per-item (DEV pattern) vs per-BP entity',
                'Warehouse code: name-as-code (DEV pattern) vs separate Code+Name',
                'Dictionary code: per-dictionary-enum (DEV pattern) vs per-CONFIG+per-TENANT',
                'BP entity: NONE in DEV vs DEDICATED in GuliERP',
            ],
        },
    }


# =================================================================
# 3. Precision / Rounding Evidence
# =================================================================
def build_precision():
    decimal_patterns = Counter()
    decimal_by_table = {}
    for t in biz:
        for c in t.get('columns', []):
            ct = c.get('type', '').lower()
            if ct in ('decimal', 'numeric', 'money', 'smallmoney'):
                pr = c.get('precision', 0)
                sc = c.get('scale', 0)
                key = f"decimal({pr},{sc})"
                decimal_patterns[key] += 1
                decimal_by_table.setdefault(t['table'], []).append({
                    'column': c['name'],
                    'type': ct,
                    'precision': pr,
                    'scale': sc,
                })

    SEMANTIC_MAP = {
        '金额': 'AMOUNT',
        '总金额': 'AMOUNT',
        '单价': 'UNIT_PRICE',
        '成本': 'COST',
        '数量': 'QUANTITY',
        '克重': 'WEIGHT',
        '重量': 'WEIGHT',
        '体积': 'VOLUME',
        '汇率': 'EXCHANGE_RATE',
        '标准成本': 'COST',
    }

    items = []
    for table_name, cols in decimal_by_table.items():
        for c in cols:
            sem = None
            for k, v in SEMANTIC_MAP.items():
                if k in c['column']:
                    sem = v
                    break
            # RoundingMode is NOT found in any DEV column/table/lookup
            rounding_mode = 'ROUNDING_MODE_NOT_FOUND'
            if c['scale'] in (2, 3) and sem:
                decision = 'REUSE'
            elif c['scale'] in (4, 6):
                decision = 'REUSE'
            else:
                decision = 'REFERENCE_ONLY'
            check(decision)
            items.append({
                'sourceSystem': 'DEV',
                'sourceTable': f'dbo.{table_name}',
                'sourceColumn': c['column'],
                'sqlType': c['type'],
                'sqlPrecision': c['precision'],
                'sqlScale': c['scale'],
                'sqlDeclaration': f"{c['type']}({c['precision']},{c['scale']})",
                'semanticTypeInferred': sem or 'UNKNOWN',
                'roundingModeInferred': rounding_mode,
                'roundingModeNote': (
                    "RoundingMode is 'ROUNDING_MODE_NOT_FOUND' in DEV: no column/table/lookup "
                    "carries rounding mode. .NET Math.Round default (AwayFromZero) is the "
                    "implicit client behavior — NOT used here as evidence. Convention must "
                    "DECLARE per semantic type."
                ),
                'confidence': 'HIGH' if c['precision'] > 0 else 'LOW',
                'candidateDecision': decision,
            })

    summary = {
        'total_decimal_columns_across_103_biz_tables': len(items),
        'by_declaration_top10': dict(decimal_patterns.most_common(10)),
        'rounding_mode_evidence': 'ROUNDING_MODE_NOT_FOUND — no DEV source carries RoundingMode.',
    }

    return {
        'meta': {
            'goal': GOAL,
            'extraction_time': EXTRACTION_TIME,
            'session': SESSION,
            'source': 'DEV reverse-engineering JSON (READ-ONLY)',
            'policy': 'No live DB accessed',
            'tables_inspected': len(biz),
            'total_decimal_columns': len(items),
            'candidateDecision_allowed': sorted(ALLOWED_DECISIONS),
        },
        'summary': summary,
        'items': items,
        'observations': [
            {
                'id': 'PREC-OBS-001',
                'topic': 'DEV_USES_DECIMAL_34_X',
                'evidence': 'Top patterns: decimal(34,2) / decimal(34,3) / decimal(34,4) / decimal(34,6) / decimal(32,3) / decimal(36,4).',
                'impact': 'DEV uses 34/32/36 digits of precision (SQL Server default for unannotated decimal).',
                'candidateDecision': 'REFERENCE_ONLY',
                'decisionReasoning': 'Over-precision. GuliERP may want tighter (20,X) or (18,X) — a design decision.',
            },
            {
                'id': 'PREC-OBS-002',
                'topic': 'AMOUNT_IS_SCALE_2',
                'evidence': '金额, 总金额, 标准成本, 借, 贷, CNY借, CNY贷 all decimal(34,2).',
                'impact': '2 decimal places for money matches China accounting standard (0.01 CNY).',
                'candidateDecision': 'REUSE',
            },
            {
                'id': 'PREC-OBS-003',
                'topic': 'UNIT_PRICE_INCONSISTENT',
                'evidence': "商品表.单价 decimal(36,4) vs 报价表s.单价 decimal(34,6) — same semantic, two different scales.",
                'impact': 'Inconsistency in DEV. GuliERP must DECLARE ONE per semantic type.',
                'candidateDecision': 'UNKNOWN',
                'decisionReasoning': 'Pick-one decision belongs to GuliERP Convention.',
            },
            {
                'id': 'PREC-OBS-004',
                'topic': 'QUANTITY_IS_SCALE_3',
                'evidence': '数量, 外箱数, 长/宽/高, 母件 all decimal(34,3) or decimal(32,3).',
                'impact': '3 decimals for quantity matches kg (0.001 kg = 1 g).',
                'candidateDecision': 'REUSE',
            },
            {
                'id': 'PREC-OBS-005',
                'topic': 'BOM_QTY_IS_SCALE_6',
                'evidence': 'BOM表.数量 decimal(34,6).',
                'impact': 'BOM usage quantity at 6 decimals (0.000001 unit per finished good).',
                'candidateDecision': 'REUSE',
            },
            {
                'id': 'PREC-OBS-006',
                'topic': 'EXCHANGE_RATE_IS_SCALE_6',
                'evidence': '凭证表s.汇率 decimal(34,6).',
                'impact': '6 decimals for exchange rate matches standard FX convention.',
                'candidateDecision': 'REUSE',
            },
            {
                'id': 'PREC-OBS-007',
                'topic': 'ROUNDING_MODE_NOT_FOUND',
                'evidence': 'No DEV source carries RoundingMode. .NET Math.Round default (AwayFromZero) is implicit client behavior, NOT used as evidence here.',
                'impact': 'GuliERP must DECLARE rounding per semantic type. (Cross-ref BASE-000 UNC-005.)',
                'candidateDecision': 'UNKNOWN',
            },
            {
                'id': 'PREC-OBS-008',
                'topic': 'SAMPLE_DEMONSTRATES_STORAGE_PRECISION',
                'evidence': '商品表.工价 = 0.0000, 数量 = 1.000 — store at full declared precision, display via client formatting.',
                'impact': 'EF Core mapping: precision must match SQL numeric(P,S) to avoid silent truncation.',
                'candidateDecision': 'REUSE',
            },
        ],
        'gap_to_convention': {
            'p0_referenced': 'P0-01 Semantic Data Type & Precision Convention (BASE-001)',
            'must_decide': [
                'AMOUNT: scale 2 (matches DEV pattern) vs scale 4 (HALF_EVEN safety for tax math)',
                'UNIT_PRICE: 4 (商品表 pattern) vs 6 (报价表s pattern) — pick one',
                'QUANTITY: scale 3 (kg pattern) vs 6 (BOM pattern) — distinguish StockQty vs BOMUsageQty',
                'RoundingMode: per-semantic-type declaration required (ROUNDING_MODE_NOT_FOUND in DEV)',
                'Rounding moments: line / document / tax / posting — 4 separate rules',
            ],
        },
    }


# =================================================================
# Write outputs
# =================================================================
numbering = build_numbering()
coding = build_coding()
precision = build_precision()

(OUT / 'document-numbering-evidence.json').write_text(
    json.dumps(numbering, ensure_ascii=False, indent=2), encoding='utf-8')
(OUT / 'master-data-coding-evidence.json').write_text(
    json.dumps(coding, ensure_ascii=False, indent=2), encoding='utf-8')
(OUT / 'precision-rounding-evidence.json').write_text(
    json.dumps(precision, ensure_ascii=False, indent=2), encoding='utf-8')

# Defensive final scan: every candidateDecision must be in allowed set
def scan_decisions(obj, path=''):
    if isinstance(obj, dict):
        if 'candidateDecision' in obj:
            v = obj['candidateDecision']
            if v not in ALLOWED_DECISIONS:
                raise ValueError(f"FORBIDDEN candidateDecision '{v}' at {path}")
        for k, v in obj.items():
            scan_decisions(v, f'{path}.{k}')
    elif isinstance(obj, list):
        for i, v in enumerate(obj):
            scan_decisions(v, f'{path}[{i}]')

for name, d in [('numbering', numbering), ('coding', coding), ('precision', precision)]:
    scan_decisions(d, name)
    print(f"  {name}: all candidateDecision values are in {sorted(ALLOWED_DECISIONS)}")

print("\nWrote:")
for p in sorted(OUT.glob('*.json')):
    print(f"  {p.name}  {p.stat().st_size}B")
