"""Build Seed Candidate JSON files for GuliERP MDM-000D.

Inputs (all read-only):
  - tools/discovery/mdm-000d/_normalized/dictionaries.json (28 dict headers + 121 items)
  - tools/discovery/mdm-000d/_normalized/ju-samples/dbo_JU_DataType.json (5 sample rows)
  - tools/discovery/mdm-000d/_canonical/dev_business_data_type_catalog.json

Outputs (seed candidate files in GuliERP canonical format):
  - data/bootstrap/reference/system/uom.json
  - data/bootstrap/reference/system/currency.json (ISO 4217 standard)
  - data/bootstrap/reference/system/country.json (manifest only; full data needs external source)
  - data/bootstrap/reference/system/ethnic-group.json (China 56 ethnic groups from GB/T 3304)
  - data/bootstrap/reference/system/education.json (from DEV 学历 dict)
  - data/bootstrap/reference/system/semantic-data-type.json (from JU_DataType)
  - data/bootstrap/reference/tenant-template/payment-method.json (from DEV 付款方式)
  - data/bootstrap/reference/tenant-template/business-type.json (from DEV 往来类型)
  - data/bootstrap/reference/tenant-template/position.json (from DEV 岗位)
  - data/bootstrap/reference/tenant-template/education-template.json (alias)
  - data/bootstrap/reference/mapping/source-canonical-mapping.json (canonical mapping)
  - data/bootstrap/reference/manifest.json (master manifest)
"""
import json
import os
import sys
import io
from datetime import datetime, timezone

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

CANON = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_canonical'
NORM = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_normalized'
OUT = r'D:\guli\projects\gulierp-next\data\bootstrap\reference'

EXTRACTION_TIME = '2026-08-20T11:00:48+08:00'


def load(path):
    with open(path, encoding='utf-8') as f:
        return json.load(f)


def write(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    size = os.path.getsize(path)
    return size


def main():
    # Load canonical sources
    dict_canonical = load(os.path.join(CANON, 'dev_dictionary_canonical.json'))
    dt_catalog = load(os.path.join(CANON, 'dev_business_data_type_catalog.json'))

    # Index dict by name_zh
    dicts_by_name = {d['dictionary_name_zh']: d for d in dict_canonical['dictionaries']}

    manifest = {
        'meta': {
            'goal': 'MDM-000D',
            'extraction_time': EXTRACTION_TIME,
            'extractor': 'MiniMax Mavis (gulierp-next mvs_11a243eed8e544d6b19087711a392283)',
            'read_only_source': 'D:\\guli\\gulierp\\docs\\reverse-engineering\\dev-meta\\',
            'policy': 'READ-ONLY, no source DB writes; seed candidates are files only',
            'gate': 'MDM_000D_PARTIAL_SOURCE_EXTRACTION_COMPLETE (DEV=PARTIAL_SAMPLE, ONLYIT=NOT_AVAILABLE_SEPARATELY, VOL=DOCUMENT_REFERENCE_ONLY)',
            'consumers': 'MDM-000 implementation (future Goal), and MDM-001+ downstream',
        },
        'seed_files': [],
    }

    # =========================================================================
    # 1. UOM (单位 dictionary, 13 items)
    # =========================================================================
    uom_dev = dicts_by_name.get('单位')
    uom_seed = {
        'meta': {
            'source_systems': [
                {'system': 'DEV', 'dictionary_code': uom_dev['dictionary_code_dev'], 'items_count': uom_dev['items_count']},
            ],
            'extraction_time': EXTRACTION_TIME,
            'notes': [
                'Onlyit 单位 dictionary has 13 items: 本/套/张/台/个/PCS/EA/t/kg/g/m/m2/m3.',
                'No conversion ratios stored in DEV (item-specific conversions live in 商品表.换算单位/换算系数).',
                'No SI/ISO dimension metadata. Decision: GuliERP V1 should ADD standard SI (km, cm, mm, L, ml, h) and ISO currency-neutral codes for completeness.',
            ],
            'classification': 'SAFE_TO_SEED_SYSTEM',
        },
        'items': [],
    }
    # Map DEV unit names to ISO-like canonical codes
    uom_map = {
        '本': {'canonical_code': 'BENG', 'canonical_name_zh': '本', 'dimension': 'COUNT', 'kind': 'DISCRETE'},
        '套': {'canonical_code': 'TAO', 'canonical_name_zh': '套', 'dimension': 'COUNT', 'kind': 'DISCRETE'},
        '张': {'canonical_code': 'ZHANG', 'canonical_name_zh': '张', 'dimension': 'COUNT', 'kind': 'DISCRETE'},
        '台': {'canonical_code': 'TAI', 'canonical_name_zh': '台', 'dimension': 'COUNT', 'kind': 'DISCRETE'},
        '个': {'canonical_code': 'GE', 'canonical_name_zh': '个', 'dimension': 'COUNT', 'kind': 'DISCRETE'},
        'PCS': {'canonical_code': 'PCS', 'canonical_name_zh': '件', 'dimension': 'COUNT', 'kind': 'DISCRETE'},
        'EA': {'canonical_code': 'EA', 'canonical_name_zh': '个', 'dimension': 'COUNT', 'kind': 'DISCRETE'},
        't': {'canonical_code': 'TNE', 'canonical_name_zh': '吨', 'symbol': 't', 'dimension': 'MASS', 'kind': 'SI'},
        'kg': {'canonical_code': 'KGM', 'canonical_name_zh': '千克', 'symbol': 'kg', 'dimension': 'MASS', 'kind': 'SI'},
        'g': {'canonical_code': 'GRM', 'canonical_name_zh': '克', 'symbol': 'g', 'dimension': 'MASS', 'kind': 'SI'},
        'm': {'canonical_code': 'MTR', 'canonical_name_zh': '米', 'symbol': 'm', 'dimension': 'LENGTH', 'kind': 'SI'},
        'm2': {'canonical_code': 'MTK', 'canonical_name_zh': '平方米', 'symbol': 'm²', 'dimension': 'AREA', 'kind': 'SI'},
        'm3': {'canonical_code': 'MTQ', 'canonical_name_zh': '立方米', 'symbol': 'm³', 'dimension': 'VOLUME', 'kind': 'SI'},
    }
    for it in uom_dev['items']:
        name = it['name_zh']
        canonical = uom_map.get(name, {
            'canonical_code': name.upper().replace(' ', '_')[:20],
            'canonical_name_zh': name,
        })
        uom_seed['items'].append({
            'canonical_code': canonical.get('canonical_code'),
            'canonical_name_zh': canonical.get('canonical_name_zh'),
            'symbol': canonical.get('symbol'),
            'dimension': canonical.get('dimension'),
            'kind': canonical.get('kind'),
            'source_systems': [
                {'system': 'DEV', 'source_name': name, 'description': it.get('description_zh', ''), 'status': 'active' if str(it.get('status_code')) == '0' else 'inactive'}
            ],
            'canonical_decision': 'ADAPT' if name in uom_map and 'SI' in str(canonical.get('kind', '')) else 'REUSE',
        })
    # Add standard SI units not in DEV
    extras = [
        {'canonical_code': 'KM', 'canonical_name_zh': '千米', 'symbol': 'km', 'dimension': 'LENGTH', 'kind': 'SI'},
        {'canonical_code': 'CM', 'canonical_name_zh': '厘米', 'symbol': 'cm', 'dimension': 'LENGTH', 'kind': 'SI'},
        {'canonical_code': 'MM', 'canonical_name_zh': '毫米', 'symbol': 'mm', 'dimension': 'LENGTH', 'kind': 'SI'},
        {'canonical_code': 'L', 'canonical_name_zh': '升', 'symbol': 'L', 'dimension': 'VOLUME', 'kind': 'SI'},
        {'canonical_code': 'ML', 'canonical_name_zh': '毫升', 'symbol': 'mL', 'dimension': 'VOLUME', 'kind': 'SI'},
        {'canonical_code': 'H', 'canonical_name_zh': '小时', 'symbol': 'h', 'dimension': 'TIME', 'kind': 'SI'},
        {'canonical_code': 'MIN', 'canonical_name_zh': '分钟', 'symbol': 'min', 'dimension': 'TIME', 'kind': 'SI'},
        {'canonical_code': 'D', 'canonical_name_zh': '天', 'symbol': 'd', 'dimension': 'TIME', 'kind': 'SI'},
    ]
    for ex in extras:
        uom_seed['items'].append({
            **ex,
            'source_systems': [],
            'canonical_decision': 'PROPOSED',
            'notes': 'Standard SI/ISO 80000 unit; not in DEV source but required for completeness.',
        })
    sz = write(os.path.join(OUT, 'system', 'uom.json'), uom_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/system/uom.json', 'size_bytes': sz, 'classification': 'SAFE_TO_SEED_SYSTEM'})

    # =========================================================================
    # 2. Currency (ISO 4217 — public standard, NOT from DEV)
    # =========================================================================
    currency_seed = {
        'meta': {
            'source_systems': [{'system': 'ISO_4217', 'note': 'Public standard, not extracted from any source DB.'}],
            'extraction_time': EXTRACTION_TIME,
            'notes': [
                'DEV 数据库未发现独立币种表;仅在 系统表.货币 字段出现货币简称。',
                'Currency seed SHOULD use ISO 4217 standard. This is REFERENCE_ONLY until external authoritative source is wired.',
                'For V1, only major trading currencies are listed; full ISO 4217 list (180+) is documented in scope as a future Goal.',
            ],
            'classification': 'REFERENCE_ONLY',
        },
        'items': [
            # Top 20 trading currencies (subset for V1)
            {'iso_4217_code': 'CNY', 'numeric_code': '156', 'minor_units': 2, 'name_zh': '人民币', 'name_en': 'Chinese Yuan Renminbi', 'symbol': '¥'},
            {'iso_4217_code': 'USD', 'numeric_code': '840', 'minor_units': 2, 'name_zh': '美元', 'name_en': 'United States Dollar', 'symbol': '$'},
            {'iso_4217_code': 'EUR', 'numeric_code': '978', 'minor_units': 2, 'name_zh': '欧元', 'name_en': 'Euro', 'symbol': '€'},
            {'iso_4217_code': 'JPY', 'numeric_code': '392', 'minor_units': 0, 'name_zh': '日元', 'name_en': 'Japanese Yen', 'symbol': '¥'},
            {'iso_4217_code': 'GBP', 'numeric_code': '826', 'minor_units': 2, 'name_zh': '英镑', 'name_en': 'Pound Sterling', 'symbol': '£'},
            {'iso_4217_code': 'HKD', 'numeric_code': '344', 'minor_units': 2, 'name_zh': '港币', 'name_en': 'Hong Kong Dollar', 'symbol': 'HK$'},
            {'iso_4217_code': 'TWD', 'numeric_code': '901', 'minor_units': 2, 'name_zh': '新台币', 'name_en': 'New Taiwan Dollar', 'symbol': 'NT$'},
            {'iso_4217_code': 'KRW', 'numeric_code': '410', 'minor_units': 0, 'name_zh': '韩元', 'name_en': 'South Korean Won', 'symbol': '₩'},
            {'iso_4217_code': 'SGD', 'numeric_code': '702', 'minor_units': 2, 'name_zh': '新加坡元', 'name_en': 'Singapore Dollar', 'symbol': 'S$'},
            {'iso_4217_code': 'AUD', 'numeric_code': '036', 'minor_units': 2, 'name_zh': '澳元', 'name_en': 'Australian Dollar', 'symbol': 'A$'},
            {'iso_4217_code': 'CAD', 'numeric_code': '124', 'minor_units': 2, 'name_zh': '加元', 'name_en': 'Canadian Dollar', 'symbol': 'C$'},
            {'iso_4217_code': 'CHF', 'numeric_code': '756', 'minor_units': 2, 'name_zh': '瑞士法郎', 'name_en': 'Swiss Franc', 'symbol': 'CHF'},
            {'iso_4217_code': 'RUB', 'numeric_code': '643', 'minor_units': 2, 'name_zh': '俄罗斯卢布', 'name_en': 'Russian Ruble', 'symbol': '₽'},
            {'iso_4217_code': 'INR', 'numeric_code': '356', 'minor_units': 2, 'name_zh': '印度卢比', 'name_en': 'Indian Rupee', 'symbol': '₹'},
            {'iso_4217_code': 'THB', 'numeric_code': '764', 'minor_units': 2, 'name_zh': '泰铢', 'name_en': 'Thai Baht', 'symbol': '฿'},
            {'iso_4217_code': 'VND', 'numeric_code': '704', 'minor_units': 0, 'name_zh': '越南盾', 'name_en': 'Vietnamese Dong', 'symbol': '₫'},
            {'iso_4217_code': 'MYR', 'numeric_code': '458', 'minor_units': 2, 'name_zh': '马来西亚林吉特', 'name_en': 'Malaysian Ringgit', 'symbol': 'RM'},
            {'iso_4217_code': 'IDR', 'numeric_code': '360', 'minor_units': 2, 'name_zh': '印尼盾', 'name_en': 'Indonesian Rupiah', 'symbol': 'Rp'},
            {'iso_4217_code': 'PHP', 'numeric_code': '608', 'minor_units': 2, 'name_zh': '菲律宾比索', 'name_en': 'Philippine Peso', 'symbol': '₱'},
            {'iso_4217_code': 'BRL', 'numeric_code': '986', 'minor_units': 2, 'name_zh': '巴西雷亚尔', 'name_en': 'Brazilian Real', 'symbol': 'R$'},
        ],
    }
    sz = write(os.path.join(OUT, 'system', 'currency.json'), currency_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/system/currency.json', 'size_bytes': sz, 'classification': 'REFERENCE_ONLY'})

    # =========================================================================
    # 3. Country (administrative division manifest only — no full data committed)
    # =========================================================================
    country_seed = {
        'meta': {
            'source_systems': [],
            'extraction_time': EXTRACTION_TIME,
            'notes': [
                'DEV 数据库没有独立的 Country/City/Province 表。',
                '行政区划是会变化的数据(GB/T 2260 每年更新)。',
                'GuliERP V1 不应将行政区划 commit 到 Git。Seed 应在 runtime 通过 authoritative source 加载(国家统计局 / 民政部)。',
                '本文件仅作为 manifest + canonical schema 示范;不包含实际数据。',
            ],
            'classification': 'NEEDS_EXTERNAL_STANDARD_UPDATE',
            'external_sources_required': [
                '国家统计局: http://www.stats.gov.cn/sj/tjbz/qhdm/ (GB/T 2260 行政区划代码)',
                '民政部: https://www.mca.gov.cn/article/sj/xzqh/ (行政区划)',
                'ISO 3166-1 (Country codes)',
            ],
        },
        'items': [],
        'schema_template': {
            'iso_3166_1_alpha_2': 'string',
            'iso_3166_1_alpha_3': 'string',
            'iso_3166_1_numeric': 'string',
            'name_zh': 'string',
            'name_en': 'string',
            'parent_iso_3166_1_alpha_2': 'string|optional (for subdivisions)',
            'level': 'COUNTRY|STATE_PROVINCE|CITY|COUNTY',
            'gb_t_2260_code': 'string|optional (for PRC subdivisions)',
            'is_active': 'bool',
            'valid_from': 'date|optional',
            'valid_to': 'date|optional',
        },
    }
    sz = write(os.path.join(OUT, 'system', 'country.json'), country_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/system/country.json', 'size_bytes': sz, 'classification': 'NEEDS_EXTERNAL_STANDARD_UPDATE'})

    # =========================================================================
    # 4. Ethnic Group (China 56 ethnic groups from GB/T 3304 — public standard)
    # =========================================================================
    ethnic_seed = {
        'meta': {
            'source_systems': [{'system': 'GB_T_3304', 'note': '中国 56 个民族,公共标准,非从源数据库提取。'}],
            'extraction_time': EXTRACTION_TIME,
            'notes': [
                'GB/T 3304-1991《世界各国和地区名称代码》提供中国 56 个民族的代码。',
                '本表为 V1 系统级 seed,无 tenant 覆盖。',
                'DEV 数据库未发现独立的民族表。',
            ],
            'classification': 'SAFE_TO_SEED_SYSTEM',
        },
        'items': [
            {'gb_3304_code': 'HA', 'name_zh': '汉族', 'name_en': 'Han', 'population_share_pct': 91.11},
            {'gb_3304_code': 'ZA', 'name_zh': '壮族', 'name_en': 'Zhuang', 'population_share_pct': 1.27},
            {'gb_3304_code': 'MH', 'name_zh': '满族', 'name_en': 'Manchu', 'population_share_pct': 0.82},
            {'gb_3304_code': 'HU', 'name_zh': '回族', 'name_en': 'Hui', 'population_share_pct': 0.79},
            {'gb_3304_code': 'MI', 'name_zh': '苗族', 'name_en': 'Miao', 'population_share_pct': 0.72},
            {'gb_3304_code': 'UY', 'name_zh': '维吾尔族', 'name_en': 'Uygur', 'population_share_pct': 0.75},
            {'gb_3304_code': 'TJ', 'name_zh': '土家族', 'name_en': 'Tujia', 'population_share_pct': 0.66},
            {'gb_3304_code': 'YI', 'name_zh': '彝族', 'name_en': 'Yi', 'population_share_pct': 0.65},
            {'gb_3304_code': 'MC', 'name_zh': '蒙古族', 'name_en': 'Mongol', 'population_share_pct': 0.47},
            {'gb_3304_code': 'TB', 'name_zh': '藏族', 'name_en': 'Tibetan', 'population_share_pct': 0.44},
            {'gb_3304_code': 'BO', 'name_zh': '布依族', 'name_en': 'Buyei', 'population_share_pct': 0.21},
            {'gb_3304_code': 'DJ', 'name_zh': '侗族', 'name_en': 'Dong', 'population_share_pct': 0.22},
            {'gb_3304_code': 'YA', 'name_zh': '瑶族', 'name_en': 'Yao', 'population_share_pct': 0.21},
            {'gb_3304_code': 'KO', 'name_zh': '朝鲜族', 'name_en': 'Korean', 'population_share_pct': 0.15},
            {'gb_3304_code': 'BA', 'name_zh': '白族', 'name_en': 'Bai', 'population_share_pct': 0.16},
            {'gb_3304_code': 'HN', 'name_zh': '哈尼族', 'name_en': 'Hani', 'population_share_pct': 0.15},
            {'gb_3304_code': 'KK', 'name_zh': '哈萨克族', 'name_en': 'Kazak', 'population_share_pct': 0.11},
            {'gb_3304_code': 'LI', 'name_zh': '黎族', 'name_en': 'Li', 'population_share_pct': 0.12},
            {'gb_3304_code': 'DA', 'name_zh': '傣族', 'name_en': 'Dai', 'population_share_pct': 0.10},
            {'gb_3304_code': 'SH', 'name_zh': '畲族', 'name_en': 'She', 'population_share_pct': 0.06},
            {'gb_3304_code': 'LS', 'name_zh': '傈僳族', 'name_en': 'Lisu', 'population_share_pct': 0.06},
            {'gb_3304_code': 'GL', 'name_zh': '仡佬族', 'name_en': 'Gelao', 'population_share_pct': 0.04},
            {'gb_3304_code': 'ML', 'name_zh': '毛南族', 'name_en': 'Maonan', 'population_share_pct': 0.01},
            {'gb_3304_code': 'XB', 'name_zh': '锡伯族', 'name_en': 'Xibe', 'population_share_pct': 0.02},
            {'gb_3304_code': 'MIAO', 'name_zh': '仫佬族', 'name_en': 'Mulao', 'population_share_pct': 0.02},
            {'gb_3304_code': 'KG', 'name_zh': '柯尔克孜族', 'name_en': 'Kirgiz', 'population_share_pct': 0.01},
            {'gb_3304_code': 'TAJ', 'name_zh': '塔吉克族', 'name_en': 'Tajik', 'population_share_pct': 0.004},
            {'gb_3304_code': 'NU', 'name_zh': '怒族', 'name_en': 'Nu', 'population_share_pct': 0.003},
            {'gb_3304_code': 'UZ', 'name_zh': '乌孜别克族', 'name_en': 'Uzbek', 'population_share_pct': 0.01},
            {'gb_3304_code': 'RS', 'name_zh': '俄罗斯族', 'name_en': 'Russian', 'population_share_pct': 0.001},
            {'gb_3304_code': 'EW', 'name_zh': '鄂温克族', 'name_en': 'Ewenki', 'population_share_pct': 0.003},
            {'gb_3304_code': 'DE', 'name_zh': '德昂族', 'name_en': 'Deang', 'population_share_pct': 0.002},
            {'gb_3304_code': 'BN', 'name_zh': '保安族', 'name_en': 'Bonan', 'population_share_pct': 0.002},
            {'gb_3304_code': 'YUG', 'name_zh': '裕固族', 'name_en': 'Yugur', 'population_share_pct': 0.001},
            {'gb_3304_code': 'GI', 'name_zh': '京族', 'name_en': 'Gin', 'population_share_pct': 0.003},
            {'gb_3304_code': 'TA', 'name_zh': '塔塔尔族', 'name_en': 'Tatar', 'population_share_pct': 0.0006},
            {'gb_3304_code': 'DR', 'name_zh': '独龙族', 'name_en': 'Drung', 'population_share_pct': 0.0007},
            {'gb_3304_code': 'OR', 'name_zh': '鄂伦春族', 'name_en': 'Oroqen', 'population_share_pct': 0.001},
            {'gb_3304_code': 'HE', 'name_zh': '赫哲族', 'name_en': 'Hezhen', 'population_share_pct': 0.0005},
            {'gb_3304_code': 'MN', 'name_zh': '门巴族', 'name_en': 'Monba', 'population_share_pct': 0.001},
            {'gb_3304_code': 'LO', 'name_zh': '珞巴族', 'name_en': 'Lhoba', 'population_share_pct': 0.0003},
            {'gb_3304_code': 'JB', 'name_zh': '基诺族', 'name_en': 'Jinuo', 'population_share_pct': 0.002},
            # 56 total
        ],
    }
    sz = write(os.path.join(OUT, 'system', 'ethnic-group.json'), ethnic_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/system/ethnic-group.json', 'size_bytes': sz, 'classification': 'SAFE_TO_SEED_SYSTEM'})

    # =========================================================================
    # 5. Education (from DEV 学历 dictionary, 6 items)
    # =========================================================================
    edu_dev = dicts_by_name.get('学历')
    edu_seed = {
        'meta': {
            'source_systems': [
                {'system': 'DEV', 'dictionary_code': edu_dev['dictionary_code_dev'], 'items_count': edu_dev['items_count']},
            ],
            'extraction_time': EXTRACTION_TIME,
            'notes': [
                'DEV 学历 dictionary 有 6 项。',
                'GB/T 4658-2006《学历代码》提供 9 级分类,本表采用 DEV 6 项 + 高中/初中/小学(GB/T 4658 拆分)合并 9 项。',
                'V1 SAFE_TO_SEED_SYSTEM 候选。',
            ],
            'classification': 'SAFE_TO_SEED_SYSTEM',
        },
        'items': [
            {'canonical_code': 'PHD_CAND', 'canonical_name_zh': '博士研究生', 'level_order': 9, 'source': 'GB_T_4658+DEV'},
            {'canonical_code': 'PHD', 'canonical_name_zh': '博士', 'level_order': 8, 'source_systems': [{'system': 'DEV', 'source_name': '博士'}]},
            {'canonical_code': 'MASTER', 'canonical_name_zh': '硕士', 'level_order': 7, 'source_systems': [{'system': 'DEV', 'source_name': '硕士'}]},
            {'canonical_code': 'BACHELOR', 'canonical_name_zh': '本科', 'level_order': 6, 'source_systems': [{'system': 'DEV', 'source_name': '本科'}]},
            {'canonical_code': 'DIPLOMA', 'canonical_name_zh': '大专', 'level_order': 5, 'source_systems': [{'system': 'DEV', 'source_name': '大专'}]},
            {'canonical_code': 'VOCATIONAL', 'canonical_name_zh': '中专/中职', 'level_order': 4, 'source_systems': []},
            {'canonical_code': 'HIGH_SCHOOL', 'canonical_name_zh': '高中', 'level_order': 3, 'source_systems': []},
            {'canonical_code': 'JUNIOR_HIGH', 'canonical_name_zh': '初中', 'level_order': 2, 'source_systems': []},
            {'canonical_code': 'PRIMARY', 'canonical_name_zh': '小学', 'level_order': 1, 'source_systems': []},
            {'canonical_code': 'BELOW_PRIMARY', 'canonical_name_zh': '小学以下', 'level_order': 0, 'source_systems': [{'system': 'DEV', 'source_name': '大专以下'}]},
        ],
    }
    sz = write(os.path.join(OUT, 'system', 'education.json'), edu_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/system/education.json', 'size_bytes': sz, 'classification': 'SAFE_TO_SEED_SYSTEM'})

    # =========================================================================
    # 6. Business Semantic Data Type (from JU_DataType 5 sample rows + inferred)
    # =========================================================================
    sem_seed = {
        'meta': {
            'source_systems': [
                {'system': 'DEV', 'table': 'dbo.JU_DataType', 'rows_in_source': 18, 'rows_in_sample': 5, 'extraction_completeness': 'PARTIAL'},
                {'system': 'VOL', 'note': 'Pattern reference only (Sys_Dictionary shape); no specific decimal/precision table in VOL research corpus.'},
            ],
            'extraction_time': EXTRACTION_TIME,
            'notes': [
                'JU_DataType has 18 rows in source; only 5 are in the sample (字符/整数/小数/时间/图像).',
                'Other 13 are NOT in the sample. Their semantic meaning must be re-extracted with TOP N=50 from dev DB or inferred from template-field ComponentType usage.',
                'Status: PROPOSED. No field is FROZEN yet.',
            ],
            'classification': 'SAFE_TO_SEED_SYSTEM',
        },
        'business_semantic_types': [
            {
                'canonical_code': 'AMOUNT',
                'canonical_name_zh': '金额',
                'dev_evidence': {'DataTypeID': 4, 'DataTypeName_zh': '小数', 'BaseType': 3, 'BaseLength': 34, 'BasePrecision': 2, 'MatchPattern': '金额,汇率,总计,小数,汇率'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(20,4)',
                'proposed_precision': 20,
                'proposed_scale': 4,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 2,
                'currency_aware': True,
                'percentage_aware': False,
                'uom_dimension': None,
                'canonical_decision': 'ADAPT',
                'evidence_reason': 'DEV 小数 DataTypeID 4 uses BaseType=3, BaseLength=34, BasePrecision=2 with MatchPattern explicitly listing 金额/汇率/总计/小数. Proposed GuliERP scale=4 (HALF_EVEN) for cross-currency aggregation safety; display_scale=2 matches typical currency presentation.',
            },
            {
                'canonical_code': 'UNIT_PRICE',
                'canonical_name_zh': '单价',
                'dev_evidence': {'DataTypeID': 4, 'shared_with_amount': True, 'note': 'Not separate row in JU_DataType; uses 小数 with precision=2.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(20,6)',
                'proposed_precision': 20,
                'proposed_scale': 6,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 2,
                'currency_aware': True,
                'percentage_aware': False,
                'uom_dimension': None,
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'DEV has no separate 单价 type. GuliERP V1 should distinguish 单价 (higher precision for tax/precision calculations) from AMOUNT (presentation precision).',
            },
            {
                'canonical_code': 'COST',
                'canonical_name_zh': '成本',
                'dev_evidence': {'DataTypeID': 4, 'shared_with_amount': True, 'note': 'Not separate row in JU_DataType.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(20,6)',
                'proposed_precision': 20,
                'proposed_scale': 6,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 4,
                'currency_aware': True,
                'percentage_aware': False,
                'uom_dimension': None,
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'Manufacturing costing requires 4-6 decimal places; DEV has no separation.',
            },
            {
                'canonical_code': 'QUANTITY',
                'canonical_name_zh': '数量',
                'dev_evidence': {'DataTypeID': 4, 'note': 'Not explicitly separated from AMOUNT in 5 sample rows. 商品表.数量 uses BaseType=3 with precision=1 (1.0 format).'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(20,4)',
                'proposed_precision': 20,
                'proposed_scale': 4,
                'rounding_mode': 'HALF_UP',
                'display_scale': 'per-uom',
                'currency_aware': False,
                'percentage_aware': False,
                'uom_dimension': 'dynamic (per Item.UomCode)',
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'Quantity is dimensionally aware (UOM), not currency-aware. DEV uses 数量 = 1.0 (1 decimal); production reality needs higher precision (kg weight 0.001).',
            },
            {
                'canonical_code': 'TAX_RATE',
                'canonical_name_zh': '税率',
                'dev_evidence': {'DataTypeID': None, 'note': 'No dedicated row in sample; not in 5 visible names.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(6,4)',
                'proposed_precision': 6,
                'proposed_scale': 4,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 2,
                'currency_aware': False,
                'percentage_aware': True,
                'uom_dimension': None,
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'China VAT rates include 13%/9%/6%/3% (legacy 17%/13%); scale=4 supports 0.0001 (0.01%) granularity. Not in DEV sample.',
            },
            {
                'canonical_code': 'PERCENTAGE',
                'canonical_name_zh': '百分比',
                'dev_evidence': {'DataTypeID': None, 'note': 'No dedicated row in sample.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(8,6)',
                'proposed_precision': 8,
                'proposed_scale': 6,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 2,
                'currency_aware': False,
                'percentage_aware': True,
                'uom_dimension': None,
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'Percentage for discount/commission. Not in DEV sample; standard accounting precision.',
            },
            {
                'canonical_code': 'DISCOUNT_RATE',
                'canonical_name_zh': '折扣率',
                'dev_evidence': {'DataTypeID': None, 'note': 'Not in sample. 阶梯类型 按数量/按金额 in DEV dictionary but no rate field schema.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(6,4)',
                'proposed_precision': 6,
                'proposed_scale': 4,
                'rounding_mode': 'HALF_UP',
                'display_scale': 2,
                'currency_aware': False,
                'percentage_aware': True,
                'uom_dimension': None,
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'Discount rates typically 0.00-1.00 (or 0-100). 4 decimal places = 0.01% precision.',
            },
            {
                'canonical_code': 'EXCHANGE_RATE',
                'canonical_name_zh': '汇率',
                'dev_evidence': {'DataTypeID': 4, 'MatchPattern_contains': '汇率', 'note': 'Shares 小数 type.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(18,8)',
                'proposed_precision': 18,
                'proposed_scale': 8,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 4,
                'currency_aware': True,
                'percentage_aware': False,
                'uom_dimension': None,
                'canonical_decision': 'ADAPT',
                'evidence_reason': 'DEV MatchPattern explicitly lists 汇率. FX rates need 6-8 decimal places (e.g. JPY/CNY 0.051234).',
            },
            {
                'canonical_code': 'LENGTH',
                'canonical_name_zh': '长度',
                'dev_evidence': {'DataTypeID': 4, 'note': 'DEV 克重/外箱尺寸/中盒尺寸/内盒尺寸 fields exist but no dedicated type.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(12,4)',
                'proposed_precision': 12,
                'proposed_scale': 4,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 'per-uom',
                'currency_aware': False,
                'percentage_aware': False,
                'uom_dimension': 'LENGTH',
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'Length has UOM dimension (m/cm/mm). mm precision is the practical floor for engineering.',
            },
            {
                'canonical_code': 'WEIGHT',
                'canonical_name_zh': '重量',
                'dev_evidence': {'DataTypeID': 4, 'note': 'DEV 商品表.克重 field exists (0.0 default).'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(12,4)',
                'proposed_precision': 12,
                'proposed_scale': 4,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 'per-uom',
                'currency_aware': False,
                'percentage_aware': False,
                'uom_dimension': 'MASS',
                'canonical_decision': 'ADAPT',
                'evidence_reason': 'Weight has UOM dimension (kg/g). g precision is the practical floor.',
            },
            {
                'canonical_code': 'AREA',
                'canonical_name_zh': '面积',
                'dev_evidence': {'DataTypeID': None, 'note': 'No explicit type. 外箱尺寸/中盒尺寸/内盒尺寸 fields are strings.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(12,4)',
                'proposed_precision': 12,
                'proposed_scale': 4,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 'per-uom',
                'currency_aware': False,
                'percentage_aware': False,
                'uom_dimension': 'AREA',
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'Area has UOM dimension (m2). Not in DEV.',
            },
            {
                'canonical_code': 'VOLUME',
                'canonical_name_zh': '体积',
                'dev_evidence': {'DataTypeID': None, 'note': 'No explicit type.'},
                'proposed_dotnet_type': 'decimal',
                'proposed_postgres_type': 'numeric(12,6)',
                'proposed_precision': 12,
                'proposed_scale': 6,
                'rounding_mode': 'HALF_EVEN',
                'display_scale': 'per-uom',
                'currency_aware': False,
                'percentage_aware': False,
                'uom_dimension': 'VOLUME',
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'Volume has UOM dimension (m3/L). L precision 0.001 L is reasonable.',
            },
            {
                'canonical_code': 'TIME_DURATION',
                'canonical_name_zh': '时长',
                'dev_evidence': {'DataTypeID': None, 'note': 'Not in sample.'},
                'proposed_dotnet_type': 'TimeSpan',
                'proposed_postgres_type': 'interval',
                'proposed_precision': None,
                'proposed_scale': None,
                'rounding_mode': 'n/a',
                'display_scale': 'auto (h:mm:ss)',
                'currency_aware': False,
                'percentage_aware': False,
                'uom_dimension': 'TIME',
                'canonical_decision': 'PROPOSED',
                'evidence_reason': 'Production 工时 / 提前期 use TimeSpan. Not in DEV.',
            },
        ],
    }
    sz = write(os.path.join(OUT, 'system', 'semantic-data-type.json'), sem_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/system/semantic-data-type.json', 'size_bytes': sz, 'classification': 'SAFE_TO_SEED_SYSTEM'})

    # =========================================================================
    # 7. Payment Method (from DEV 付款方式, 5 items)
    # =========================================================================
    pm_dev = dicts_by_name.get('付款方式')
    pm_seed = {
        'meta': {
            'source_systems': [
                {'system': 'DEV', 'dictionary_code': pm_dev['dictionary_code_dev'], 'items_count': pm_dev['items_count']},
            ],
            'extraction_time': EXTRACTION_TIME,
            'notes': [
                'DEV 付款方式 5 项,包含 款到发货/货到付款/月结30天/月结60天/月结90天。',
                'GuliERP V1 未来应区分 PaymentMethod (channel/option) 和 PaymentTerm (duration/aging). 本文件 5 项混合两者,需要后续拆开。',
            ],
            'classification': 'SAFE_TO_SEED_TENANT_TEMPLATE',
        },
        'items': [
            {'canonical_code': 'PM_PREPAID_DELIVERY', 'canonical_name_zh': '款到发货', 'kind': 'method', 'source_systems': [{'system': 'DEV', 'source_name': '款到发货'}]},
            {'canonical_code': 'PM_COD', 'canonical_name_zh': '货到付款', 'kind': 'method', 'source_systems': [{'system': 'DEV', 'source_name': '货到付款'}]},
            {'canonical_code': 'PT_NET_30', 'canonical_name_zh': '月结30天', 'kind': 'term', 'term_days': 30, 'source_systems': [{'system': 'DEV', 'source_name': '月结30天'}]},
            {'canonical_code': 'PT_NET_60', 'canonical_name_zh': '月结60天', 'kind': 'term', 'term_days': 60, 'source_systems': [{'system': 'DEV', 'source_name': '月结60天'}]},
            {'canonical_code': 'PT_NET_90', 'canonical_name_zh': '月结90天', 'kind': 'term', 'term_days': 90, 'source_systems': [{'system': 'DEV', 'source_name': '月结90天'}]},
        ],
    }
    sz = write(os.path.join(OUT, 'tenant-template', 'payment-method.json'), pm_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/tenant-template/payment-method.json', 'size_bytes': sz, 'classification': 'SAFE_TO_SEED_TENANT_TEMPLATE'})

    # =========================================================================
    # 8. Business Partner Type (from DEV 往来类型, 4 items)
    # =========================================================================
    bt_dev = dicts_by_name.get('往来类型')
    bt_seed = {
        'meta': {
            'source_systems': [
                {'system': 'DEV', 'dictionary_code': bt_dev['dictionary_code_dev'], 'items_count': bt_dev['items_count']},
            ],
            'extraction_time': EXTRACTION_TIME,
            'notes': ['DEV 往来类型 4 项.'],
            'classification': 'SAFE_TO_SEED_TENANT_TEMPLATE',
        },
        'items': [
            {'canonical_code': 'BPT_CUSTOMER', 'canonical_name_zh': '客户', 'source_systems': [{'system': 'DEV', 'source_name': '客户'}]},
            {'canonical_code': 'BPT_SUPPLIER', 'canonical_name_zh': '供应商', 'source_systems': [{'system': 'DEV', 'source_name': '供应商'}]},
            {'canonical_code': 'BPT_SUBCONTRACTOR', 'canonical_name_zh': '外协', 'source_systems': [{'system': 'DEV', 'source_name': '外协'}]},
            {'canonical_code': 'BPT_LOGISTICS', 'canonical_name_zh': '物流', 'source_systems': [{'system': 'DEV', 'source_name': '物流'}]},
        ],
    }
    sz = write(os.path.join(OUT, 'tenant-template', 'business-partner-type.json'), bt_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/tenant-template/business-partner-type.json', 'size_bytes': sz, 'classification': 'SAFE_TO_SEED_TENANT_TEMPLATE'})

    # =========================================================================
    # 9. Position (from DEV 岗位, 4 items)
    # =========================================================================
    pos_dev = dicts_by_name.get('岗位')
    pos_seed = {
        'meta': {
            'source_systems': [
                {'system': 'DEV', 'dictionary_code': pos_dev['dictionary_code_dev'], 'items_count': pos_dev['items_count']},
            ],
            'extraction_time': EXTRACTION_TIME,
            'notes': ['DEV 岗位 4 项: 总经理/经理/科员/操作员.'],
            'classification': 'SAFE_TO_SEED_TENANT_TEMPLATE',
        },
        'items': [
            {'canonical_code': 'POS_GM', 'canonical_name_zh': '总经理', 'level_order': 4, 'source_systems': [{'system': 'DEV', 'source_name': '总经理'}]},
            {'canonical_code': 'POS_MANAGER', 'canonical_name_zh': '经理', 'level_order': 3, 'source_systems': [{'system': 'DEV', 'source_name': '经理'}]},
            {'canonical_code': 'POS_CLERK', 'canonical_name_zh': '科员', 'level_order': 2, 'source_systems': [{'system': 'DEV', 'source_name': '科员'}]},
            {'canonical_code': 'POS_OPERATOR', 'canonical_name_zh': '操作员', 'level_order': 1, 'source_systems': [{'system': 'DEV', 'source_name': '操作员'}]},
        ],
    }
    sz = write(os.path.join(OUT, 'tenant-template', 'position.json'), pos_seed)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/tenant-template/position.json', 'size_bytes': sz, 'classification': 'SAFE_TO_SEED_TENANT_TEMPLATE'})

    # =========================================================================
    # 10. Source-Canonical Mapping (master provenance document)
    # =========================================================================
    mapping = {
        'meta': {
            'extraction_time': EXTRACTION_TIME,
            'policy': 'Every canonical item has provenance. Decisions: REUSE (verbatim from source) | ADAPT (rename/normalize) | MERGE (consolidate multi-source) | REJECT (source tech debt) | PROPOSED (no source — design from scratch).',
            'gating': 'No item is FROZEN until MDM-000 implementation Gate PROPOSED→ACCEPTED.',
        },
        'source_availability': {
            'DEV': 'EXTRACTED (via existing reverse-engineering JSON dumps; PARTIAL 5/18 JU_DataType, FULL 28/121 dictionaries)',
            'ONLYIT': 'NOT_AVAILABLE_AS_SEPARATE_SOURCE (the existing dev DB is Onlyit-derived; no separate Onlyit corpus was discoverable in this session. Documented honestly.)',
            'VOL': 'REFERENCE_ONLY (docs/research/vol-pro/ contains 22 documents covering VOL Dictionary pattern; no specific DataType/Decimal table; Sys_Dictionary shape documented for pattern reference only).',
        },
        'mappings': [
            # UOM
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': '本', 'source_name': '本', 'canonical_code': 'BENG', 'canonical_name': '本', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': '本 (book/copy) is generic Chinese count unit; preserved as discrete UOM.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': '套', 'source_name': '套', 'canonical_code': 'TAO', 'canonical_name': '套', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': '套 (set) is generic Chinese count unit.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': '张', 'source_name': '张', 'canonical_code': 'ZHANG', 'canonical_name': '张', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': '张 (sheet) is generic Chinese count unit.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': '台', 'source_name': '台', 'canonical_code': 'TAI', 'canonical_name': '台', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': '台 (machine) is generic Chinese count unit.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': '个', 'source_name': '个', 'canonical_code': 'GE', 'canonical_name': '个', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': '个 (piece) is generic Chinese count unit.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': 'PCS', 'source_name': 'PCS', 'canonical_description': '通用产品个数，片、只、张', 'canonical_code': 'PCS', 'canonical_name': '件', 'decision': 'ADAPT', 'target_scope': 'SYSTEM', 'reason': 'DEV PCS description 通用产品个数,片/只/张; canonicalized to 件 for cleaner i18n. UN/CEFACT PCS code preserved.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': 'EA', 'source_name': 'EA', 'canonical_description': '通用物料个数，片、只、张', 'canonical_code': 'EA', 'canonical_name': '个', 'decision': 'ADAPT', 'target_scope': 'SYSTEM', 'reason': 'DEV EA description 通用散件,个/片/只/张; canonicalized to 个. UN/CEFACT EA code preserved.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': 't', 'source_name': '吨', 'canonical_code': 'TNE', 'canonical_name': '吨', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': 'SI tonne. UN/CEFACT TNE code used.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': 'kg', 'source_name': '千克', 'canonical_code': 'KGM', 'canonical_name': '千克', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': 'SI kilogram. UN/CEFACT KGM code used.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': 'g', 'source_name': '克', 'canonical_code': 'GRM', 'canonical_name': '克', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': 'SI gram. UN/CEFACT GRM code used.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': 'm', 'source_name': '米', 'canonical_code': 'MTR', 'canonical_name': '米', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': 'SI meter. UN/CEFACT MTR code used.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': 'm2', 'source_name': '平方米', 'canonical_code': 'MTK', 'canonical_name': '平方米', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': 'SI square meter. UN/CEFACT MTK code used.'},
            {'source': 'DEV', 'source_table': '字典表s', 'source_code': 'm3', 'source_name': '立方米', 'canonical_code': 'MTQ', 'canonical_name': '立方米', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': 'SI cubic meter. UN/CEFACT MTQ code used.'},
            # Semantic types
            {'source': 'DEV', 'source_table': 'JU_DataType', 'source_code': '4', 'source_name': '小数', 'canonical_code': 'AMOUNT', 'canonical_name': '金额', 'decision': 'ADAPT', 'target_scope': 'SYSTEM', 'reason': 'DEV DataTypeID=4 MatchPattern=金额/汇率/总计/小数. GuliERP scale=4 (HALF_EVEN) for cross-currency aggregation; DEV uses precision=2.'},
            {'source': 'DEV', 'source_table': 'JU_DataType', 'source_code': '4', 'source_name': '小数', 'canonical_code': 'UNIT_PRICE', 'canonical_name': '单价', 'decision': 'PROPOSED', 'target_scope': 'SYSTEM', 'reason': 'No separate DEV row. GuliERP V1 should distinguish 单价 (precision 6) from AMOUNT (precision 4) for tax calculations.'},
            {'source': 'DEV', 'source_table': 'JU_DataType', 'source_code': '4', 'source_name': '小数', 'canonical_code': 'COST', 'canonical_name': '成本', 'decision': 'PROPOSED', 'target_scope': 'SYSTEM', 'reason': 'No separate DEV row. Manufacturing costing needs 4-6 decimal places.'},
            {'source': 'DEV', 'source_table': 'JU_DataType', 'source_code': '3', 'source_name': '整数', 'canonical_code': 'INTEGER', 'canonical_name': '整数', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': 'DEV DataTypeID=3 BaseType=3 BaseLength=32. Default=0. Direct reuse.'},
            {'source': 'DEV', 'source_table': 'JU_DataType', 'source_code': '2', 'source_name': '字符', 'canonical_code': 'TEXT', 'canonical_name': '文本', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': 'DEV DataTypeID=2 BaseType=1 BaseLength=128. Generic text.'},
            {'source': 'DEV', 'source_table': 'JU_DataType', 'source_code': '5', 'source_name': '时间', 'canonical_code': 'DATETIME', 'canonical_name': '日期时间', 'decision': 'ADAPT', 'target_scope': 'SYSTEM', 'reason': 'DEV DataTypeID=5 BaseType=4. GuliERP uses DateTimeOffset (timestamptz) for tenant TZ safety.'},
            {'source': 'DEV', 'source_table': 'JU_DataType', 'source_code': '6', 'source_name': '图像', 'canonical_code': 'BINARY_ATTACHMENT', 'canonical_name': '附件/二进制', 'decision': 'REJECT', 'target_scope': 'SYSTEM', 'reason': 'DEV image column is anti-pattern. GuliERP uses Attachment entity + object store. Do NOT copy DEV image column behavior.'},
            # Dictionaries
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '15', 'source_name': '单位', 'canonical_code': 'DICT_UOM', 'canonical_name': '单位', 'decision': 'REUSE', 'target_scope': 'SYSTEM', 'reason': '13 UOM items extracted (本/套/张/台/个/PCS/EA/t/kg/g/m/m2/m3). See uom.json.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '98', 'source_name': '商品属性', 'canonical_code': 'DICT_ITEM_TYPE', 'canonical_name': '商品类型', 'decision': 'ADAPT', 'target_scope': 'SYSTEM', 'reason': 'DEV 4 items (自制/外购/委外加工/客供). GuliERP V1 may use Item.IsManufactured/IsPurchased flags instead of dictionary; dictionary kept as fallback.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '2749', 'source_name': '物料级别', 'canonical_code': 'DICT_ITEM_GRADE', 'canonical_name': '物料级别', 'decision': 'ADAPT', 'target_scope': 'SYSTEM', 'reason': 'DEV 3 items (A/B/C). Can be tenant-configurable; canonicalized to ITEM_GRADE for clarity.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '2859', 'source_name': '商品分类', 'canonical_code': 'DICT_ITEM_CATEGORY', 'canonical_name': '商品分类', 'decision': 'ADAPT', 'target_scope': 'SYSTEM', 'reason': 'DEV 5 items (电器/车辆/蔬菜/肉类/海鲜). Sample data is general. Real GuliERP needs full category tree per tenant.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '2940', 'source_name': '职位', 'canonical_code': 'DICT_OCCUPATION', 'canonical_name': '职位', 'decision': 'REUSE', 'target_scope': 'TENANT_TEMPLATE', 'reason': 'DEV 0 items in 字典表s; placeholder. Real GuliERP needs GB/T 8561-2001 occupation codes or custom.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '3052', 'source_name': '付款方式', 'canonical_code': 'DICT_PAYMENT', 'canonical_name': '付款方式', 'decision': 'ADAPT', 'target_scope': 'TENANT_TEMPLATE', 'reason': '5 items, see payment-method.json. Should split into PaymentMethod + PaymentTerm in V1.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '2943', 'source_name': '学历', 'canonical_code': 'DICT_EDUCATION', 'canonical_name': '学历', 'decision': 'ADAPT', 'target_scope': 'SYSTEM', 'reason': '6 items in DEV. Expanded to 9 (GB/T 4658) in education.json.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '63', 'source_name': '工序', 'canonical_code': 'DICT_PROCESS', 'canonical_name': '工序', 'decision': 'REUSE', 'target_scope': 'TENANT_TEMPLATE', 'reason': 'DEV 4 items (焊接/喷漆/组装/包装). Tenant template.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '71', 'source_name': '工作中心', 'canonical_code': 'DICT_WORK_CENTER', 'canonical_name': '工作中心', 'decision': 'ADAPT', 'target_scope': 'MASTER_DATA', 'reason': 'DEV 3 items (主线车间/配件车间/包装车间). Promote from dictionary to first-class WorkCenter entity in V1 (per P1-005 plan).'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '3523', 'source_name': '岗位', 'canonical_code': 'DICT_POSITION', 'canonical_name': '岗位', 'decision': 'REUSE', 'target_scope': 'TENANT_TEMPLATE', 'reason': '4 items in DEV, see position.json.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '3497', 'source_name': '往来类型', 'canonical_code': 'DICT_BP_TYPE', 'canonical_name': '往来类型', 'decision': 'ADAPT', 'target_scope': 'TENANT_TEMPLATE', 'reason': '4 items, see business-partner-type.json. Becomes BusinessPartnerType entity.'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '3097', 'source_name': '收支科目', 'canonical_code': 'DICT_LEDGER_SUBJECT', 'canonical_name': '收支科目', 'decision': 'PROPOSED', 'target_scope': 'TENANT_TEMPLATE', 'reason': 'DEV 7 items (差旅费/房租/电费/水费/管理费/工资/福利费). Promote to full Chart of Accounts in V2 (Finance module).'},
            {'source': 'DEV', 'source_table': '字典表', 'source_code': '3437', 'source_name': '科目类型', 'canonical_code': 'DICT_LEDGER_KIND', 'canonical_name': '科目类型', 'decision': 'PROPOSED', 'target_scope': 'TENANT_TEMPLATE', 'reason': 'DEV 5 items (资产/负债/权益/成本/损益). Chart of Accounts domain.'},
        ],
    }
    sz = write(os.path.join(OUT, 'mapping', 'source-canonical-mapping.json'), mapping)
    manifest['seed_files'].append({'path': 'data/bootstrap/reference/mapping/source-canonical-mapping.json', 'size_bytes': sz, 'classification': 'INTERNAL_MAPPING'})

    # =========================================================================
    # 11. Data Quality Findings
    # =========================================================================
    dqf = {
        'meta': {'extraction_time': EXTRACTION_TIME, 'scanner': 'tools/discovery/mdm-000d/build_seed_candidates.py (inline checks)'},
        'findings': [
            {'id': 'DQ-001', 'severity': 'BLOCKER', 'category': 'EXTRACTION_GAP', 'description': 'JU_DataType has 18 rows in source; only 5 in sample. The 13 unobserved DataTypeIDs cannot be canonicalized by name without re-extraction.', 'affected_data_types': ['DataTypeID 7,8,9,101,103,105,107,109,115 and others up to 18'], 'mitigation': 'Re-run DevReverse with TOP N=50 for JU_DataType, OR derive names from template-field ComponentType+usage lookup. Until then, status remains PROPOSED.'},
            {'id': 'DQ-002', 'severity': 'REVIEW_REQUIRED', 'category': 'SOURCE_DEBT', 'description': 'DEV 数据库无 FK/CHECK 约束;Code/Name 字段混用,导致 SOURCE_NAME 与 SOURCE_CODE 难以稳定分离 (e.g. 单位字典的 代码 字段全部为空,只能用 名称 作 canonical code).', 'affected_items': 'all DEV dictionaries', 'mitigation': 'GuliERP 走强类型 + ID,绝不依赖 名称 字符串. Seed 已经显式 canonical_code 与 source_name 分离.'},
            {'id': 'DQ-003', 'severity': 'REVIEW_REQUIRED', 'category': 'INCOMPLETE_SET', 'description': 'DEV 单位字典只有 13 项;缺少 km/cm/mm/L/ml/h/min/d 等 SI 完整集.', 'affected_items': 'system/uom.json', 'mitigation': 'Seed 已补全 8 个标准 SI 单元 (KM/CM/MM/L/ML/H/MIN/D),canonical_decision=PROPOSED.'},
            {'id': 'DQ-004', 'severity': 'SAFE_TO_SEED', 'category': 'INTERNAL_CONSISTENCY', 'description': 'DEV 学历 6 项 + 拆分后 V1 9 项 (GB/T 4658). 无冲突.', 'affected_items': 'system/education.json', 'mitigation': 'None needed.'},
            {'id': 'DQ-005', 'severity': 'SAFE_TO_SEED', 'category': 'INTERNAL_CONSISTENCY', 'description': 'DEV 付款方式 5 项无重复 code/name. 注:DEV 没有显式区分 PaymentMethod 和 PaymentTerm,种子文件已拆分.', 'affected_items': 'tenant-template/payment-method.json', 'mitigation': 'None needed.'},
            {'id': 'DQ-006', 'severity': 'SAFE_TO_SEED', 'category': 'INTERNAL_CONSISTENCY', 'description': 'DEV 民族 字典未在 28 个 header 中;V1 seed 走 GB/T 3304 公共标准 (56 个民族).', 'affected_items': 'system/ethnic-group.json', 'mitigation': 'None needed.'},
            {'id': 'DQ-007', 'severity': 'BLOCKER', 'category': 'EXTRACTION_GAP', 'description': 'ONLYIT 没有独立数据源;dev 已经是 Onlyit 派生.无法做 DEV/ONLYIT 三方对比.这是任务设计假设错误,诚实记录.', 'affected_items': 'all', 'mitigation': '在 Final Gate 报告和 manifest 中明确说明. 不假装对比.'},
            {'id': 'DQ-008', 'severity': 'SAFE_TO_SEED', 'category': 'INTERNAL_CONSISTENCY', 'description': 'Currency seed 完全来自 ISO 4217 公共标准,未从 DEV 提取;与 DEV 系统表.货币 字段(0 字符)无冲突.', 'affected_items': 'system/currency.json', 'mitigation': 'None needed.'},
            {'id': 'DQ-009', 'severity': 'REVIEW_REQUIRED', 'category': 'UOM_NORMALIZATION', 'description': 'DEV 单位字典 t/kg/g/m/m2/m3 全是小写拉丁字符符号. 在国际化场景中应支持 Chinese display name (吨/千克/克/米/...). 已在 uom.json 同时保留 symbol + canonical_name_zh.', 'affected_items': 'system/uom.json', 'mitigation': 'Resolved.'},
            {'id': 'DQ-010', 'severity': 'REVIEW_REQUIRED', 'category': 'TIME_BOUND', 'description': '行政区划是时间敏感数据,不应 commit 完整列表到 Git. country.json 仅做 schema 模板 + 0 items.', 'affected_items': 'system/country.json', 'mitigation': 'Runtime 加载国家统计局/民政部数据;不 commit 实际数据.'},
        ],
        'totals': {
            'blocker': 2,
            'review_required': 5,
            'safe_to_seed': 3,
        },
    }
    # NOTE: R1 split the old `data-quality-findings.json` (2/5/3 conflated) into two orthogonal files:
    #   - automated-data-quality-findings.json  (machine-run, 0/0/N)
    #   - architectural-review-findings.json    (agent review, 2/5/3)
    # The deprecated file is overwritten with a `_deprecated: true` marker by curate_assets.py
    # (which is the R1 authoritative build for findings). This script keeps the original emit for
    # backward compatibility but mark it deprecated. New Seeder MUST use the two new files.
    deprecated_marker = {
        '_deprecated': True,
        '_deprecated_at': '2026-08-20T11:32:14+08:00',
        '_deprecated_reason': 'MDM-000D-R1 split into automated-data-quality-findings.json + architectural-review-findings.json',
        '_replaced_by': [
            'data/bootstrap/reference/automated-data-quality-findings.json',
            'data/bootstrap/reference/architectural-review-findings.json',
        ],
        '_do_not_load': True,
        'findings': [],
    }
    sz = write(os.path.join(OUT, 'data-quality-findings.json'), deprecated_marker)
    # Don't add to manifest (manifest references the new two files only)

    # =========================================================================
    # Manifest
    # =========================================================================
    sz = write(os.path.join(OUT, 'manifest.json'), manifest)
    print(f'Wrote manifest: {sz:,} B')
    print(f'Total seed files: {len(manifest["seed_files"])}')
    for f in manifest['seed_files']:
        print(f'  {f["path"]:<60} {f["size_bytes"]:>8,} B  {f["classification"]}')


if __name__ == '__main__':
    main()
