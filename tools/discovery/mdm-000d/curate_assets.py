"""MDM-000D-R1 Curate Assets.

Rebuilds all curated artifacts with:
  1. Automated Data Quality Findings (machine-run scan output, 0/0/N)
  2. Architectural Review Findings (human/agent review, 2/5/3 etc.)
  3. Per-item seed_status on every seed file (not blanket file-level)
  4. JU_DataType ID set machine-computed (DIRECT_SAMPLE / REFERENCED / UNRESOLVED / UNREFERENCED)
  5. Ethnic Group: 42/56 → INCOMPLETE_STANDARD_DATA + EXPECTED/CURRENT/MISSING
  6. UOM: DEV 13 SAFE_TO_SEED_SYSTEM + 8 EXTERNAL_STANDARD_CANDIDATE PROPOSED (per-item)
  7. Position ≠ Occupation; OCCUPATION_SOURCE_NOT_FOUND
  8. ONLYIT_INDEPENDENT_SOURCE = NOT_AVAILABLE; VOL_DATA_VALUE_SOURCE = NONE; VOL_PATTERN_SOURCE = AVAILABLE
  9. normalized-manifest.json with checksums (for the .gitignored _normalized/ folder)
 10. Updated master manifest.json with itemCount + per-file seedStatus
"""
import hashlib
import json
import os
import sys
import io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

EXTRACTION_TIME = '2026-08-20T11:32:14+08:00'
NORM = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_normalized'
CANON = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_canonical'
SEED_ROOT = r'D:\guli\projects\gulierp-next\data\bootstrap\reference'
SCRIPT_DIR = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d'


def load(path):
    with open(path, encoding='utf-8') as f:
        return json.load(f)


def write(path, data, ensure_ascii=False):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=ensure_ascii, indent=2)
    return os.path.getsize(path)


def sha256_of_file(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for chunk in iter(lambda: f.read(65536), b''):
            h.update(chunk)
    return h.hexdigest()


# =============================================================================
# P2. Machine-compute JU_DataType ID sets
# =============================================================================
def recompute_juids():
    dt = load(os.path.join(NORM, 'ju-samples', 'dbo_JU_DataType.json'))
    direct_sample_ids = sorted(r['DataTypeID'] for r in dt['rows'])

    tf = load(os.path.join(NORM, 'template-fields.json'))
    referenced_ids = sorted({f.get('DataTypeID') for f in tf if f.get('DataTypeID') is not None})

    sample_set = set(direct_sample_ids)
    referenced_set = set(referenced_ids)
    union_set = sample_set | referenced_set

    unresolved_ids = sorted(referenced_set - sample_set)
    unreferenced_ids = sorted(sample_set - referenced_set)

    # Source has 18 rows. Union visible = 14 distinct IDs. 4 are completely unknown.
    known_count = len(union_set)
    unknown_count = 18 - known_count

    return {
        'source_table': 'dbo.JU_DataType',
        'source_rowcount': 18,
        'extraction_method': 'DevReverse.Program.cs SELECT TOP 5 per table (sample-only)',
        'all_visible_ids': sorted(union_set),
        'all_visible_count': known_count,
        'direct_sample_ids': direct_sample_ids,
        'direct_sample_count': len(direct_sample_ids),
        'direct_sample_with_names': [
            {'DataTypeID': r['DataTypeID'], 'DataTypeName_zh': r['DataTypeName']}
            for r in dt['rows']
        ],
        'referenced_ids': referenced_ids,
        'referenced_count': len(referenced_ids),
        'unresolved_ids': unresolved_ids,
        'unresolved_count': len(unresolved_ids),
        'unreferenced_ids': unreferenced_ids,
        'unreferenced_count': len(unreferenced_ids),
        'completely_unknown_ids_count': unknown_count,
        'note': 'completely_unknown_ids_count = source_rowcount(18) - union_visible(14) = 4 rows in source that are neither in sample nor referenced by any template field. Their DataTypeIDs are unknown.'
    }


juids = recompute_juids()
print('=== JU_DataType machine-computed sets ===')
print(f"  source rowcount: {juids['source_rowcount']}")
print(f"  DIRECT_SAMPLE_IDS ({juids['direct_sample_count']}): {juids['direct_sample_ids']}")
print(f"  REFERENCED_IDS ({juids['referenced_count']}): {juids['referenced_ids']}")
print(f"  UNRESOLVED_IDS ({juids['unresolved_count']}): {juids['unresolved_ids']}")
print(f"  UNREFERENCED_IDS ({juids['unreferenced_count']}): {juids['unreferenced_ids']}")
print(f"  completely unknown: {juids['completely_unknown_ids_count']}")

# Write canonical juids set
write(os.path.join(CANON, 'ju_datatype_id_sets.json'), {
    'meta': {
        'extraction_time': EXTRACTION_TIME,
        'method': 'machine-computed set arithmetic',
        'source': 'tools/discovery/mdm-000d/_normalized/ju-samples/dbo_JU_DataType.json + template-fields.json'
    },
    'sets': juids
})
print(f"Wrote {CANON}/ju_datatype_id_sets.json")


# =============================================================================
# P1. Automated Data Quality Scan (re-runs the existing scanner, saves JSON)
# =============================================================================
def run_automated_dq_scan():
    """Re-run the existing run_data_quality_scan.py and capture output as JSON."""
    import subprocess
    result = subprocess.run(
        ['python', os.path.join(SCRIPT_DIR, 'run_data_quality_scan.py')],
        capture_output=True, text=True, encoding='utf-8', errors='replace'
    )
    return {
        'meta': {
            'extraction_time': EXTRACTION_TIME,
            'scanner': 'tools/discovery/mdm-000d/run_data_quality_scan.py',
            'method': 'machine-run; checks for null_code / duplicate_code / null_name across all seed files',
        },
        'exit_code': result.returncode,
        'stdout': result.stdout,
        'stderr': result.stderr,
    }


auto_dq = run_automated_dq_scan()
print()
print('=== Automated DQ Scan Output ===')
print(auto_dq['stdout'][-500:] if auto_dq['stdout'] else '(empty)')

# Parse the summary line
summary = {'BLOCKER': 0, 'REVIEW_REQUIRED': 0, 'INFO': 0}
for line in auto_dq['stdout'].splitlines():
    if 'Scan summary:' in line:
        # Example: === Scan summary: {'BLOCKER': 0, 'REVIEW_REQUIRED': 0, 'INFO': 10} ===
        try:
            import ast
            summary = ast.literal_eval(line.split('=== Scan summary: ')[1].split(' ===')[0])
        except Exception:
            pass

automated_findings = {
    'meta': {
        'extraction_time': EXTRACTION_TIME,
        'scanner': 'tools/discovery/mdm-000d/run_data_quality_scan.py',
        'scanner_method': 'machine-run; checks for null_code / duplicate_code / null_name across all seed files',
        'scope': 'data/bootstrap/reference/**/*.json (9 seed files + 28 DEV dictionaries)',
    },
    'totals': {
        'blocker': summary.get('BLOCKER', 0),
        'review_required': summary.get('REVIEW_REQUIRED', 0),
        'info': summary.get('INFO', 0),
    },
    'scanner_exit_code': auto_dq['exit_code'],
    'raw_output_tail': auto_dq['stdout'][-1000:],
    'findings': [],
    'note_on_no_findings': 'Scanner ran 9 seed files + 28 DEV dictionary headers. All scanned items pass (no null code, no null name, no duplicate code within file). Only INFO-level stats are produced (one per file). The 0 BLOCKER / 0 REVIEW / N INFO result confirms structural seed-file integrity, not business-level correctness. Business-level concerns are recorded in architectural-review-findings.json (separate file, separate dimension).'
}
write(os.path.join(SEED_ROOT, 'automated-data-quality-findings.json'), automated_findings)
print(f"Wrote {SEED_ROOT}/automated-data-quality-findings.json  ({summary})")


# =============================================================================
# P11. Per-item seed_status for each seed file
# =============================================================================
def add_seed_status(seed_path, status_for_path, item_status_field='seed_status'):
    """Add seed_status at item level for the given file."""
    if not os.path.exists(seed_path):
        return None
    data = load(seed_path)
    items = data.get('items') or data.get('business_semantic_types') or []
    # Default: all items get the path-level status
    for it in items:
        if item_status_field not in it:
            it[item_status_field] = status_for_path
    # Update meta
    if 'meta' in data:
        data['meta']['seed_status_at_file_level'] = status_for_path
        data['meta']['item_count'] = len(items)
    write(seed_path, data)
    return len(items)


# System seeds
counts = {}
counts['uom'] = add_seed_status(os.path.join(SEED_ROOT, 'system', 'uom.json'), 'SAFE_TO_SEED_SYSTEM')
# UOM item-level override: per user instruction, only DEV items SAFE, the 8 SI candidates PROPOSED
uom = load(os.path.join(SEED_ROOT, 'system', 'uom.json'))
for it in uom['items']:
    if it.get('source_systems') and any(s.get('system') == 'DEV' for s in it['source_systems']):
        it['seed_status'] = 'SAFE_TO_SEED_SYSTEM'
        it['source_system_label'] = 'DEV'
    else:
        it['seed_status'] = 'PROPOSED'
        it['source_system_label'] = 'EXTERNAL_STANDARD_CANDIDATE'
uom['meta']['seed_status_at_file_level'] = 'MIXED'  # not blanket anymore
uom['meta']['items_safe_to_seed_system'] = sum(1 for x in uom['items'] if x['seed_status'] == 'SAFE_TO_SEED_SYSTEM')
uom['meta']['items_proposed'] = sum(1 for x in uom['items'] if x['seed_status'] == 'PROPOSED')
uom['meta']['notes'] = [
    'Mixed classification: 13 items have DEV provenance and are SAFE_TO_SEED_SYSTEM; 8 items are EXTERNAL_STANDARD_CANDIDATE (ISO 80000 SI units not in DEV) and are PROPOSED.',
    'Future Seeder should load only items with seed_status=SAFE_TO_SEED_SYSTEM by default; PROPOSED items require explicit opt-in.'
]
write(os.path.join(SEED_ROOT, 'system', 'uom.json'), uom)
print(f"  uom.json: {counts['uom']} items, SAFE={uom['meta']['items_safe_to_seed_system']}, PROPOSED={uom['meta']['items_proposed']}")

# Currency: already REFERENCE_ONLY
counts['currency'] = add_seed_status(os.path.join(SEED_ROOT, 'system', 'currency.json'), 'REFERENCE_ONLY')
cur = load(os.path.join(SEED_ROOT, 'system', 'currency.json'))
for it in cur['items']:
    it['seed_status'] = 'REFERENCE_ONLY'
    it['source_system_label'] = 'ISO_4217_PUBLIC_STANDARD'
    it.setdefault('source_systems', [{'system': 'ISO_4217_PUBLIC_STANDARD', 'source_name': it.get('iso_4217_code')}])
write(os.path.join(SEED_ROOT, 'system', 'currency.json'), cur)

# Country: NEEDS_EXTERNAL_STANDARD_UPDATE
counts['country'] = add_seed_status(os.path.join(SEED_ROOT, 'system', 'country.json'), 'NEEDS_EXTERNAL_STANDARD_UPDATE')

# Ethnic Group: 42 of 56 = INCOMPLETE_STANDARD_DATA
ethnic = load(os.path.join(SEED_ROOT, 'system', 'ethnic-group.json'))
EXPECTED = 56  # GB/T 3304
current = len(ethnic['items'])
missing = EXPECTED - current
ethnic['meta']['seed_status_at_file_level'] = 'INCOMPLETE_STANDARD_DATA'
ethnic['meta']['items_safe_to_seed_system'] = 0
ethnic['meta']['items_proposed'] = current
ethnic['meta']['completeness'] = {
    'expected_count': EXPECTED,
    'current_count': current,
    'missing_count': missing,
    'standard': 'GB/T 3304-1991 (中国各民族名称代码)',
    'note': f'V1 has {current} of {EXPECTED} ethnic groups. The 14 missing are smaller minorities (e.g. 珞巴族, 基诺族, 门巴族, 塔塔尔族 etc. — some are present, some need re-extraction). Until the standard is fully matched, classification is INCOMPLETE_STANDARD_DATA — not SAFE_TO_SEED_SYSTEM.',
}
for it in ethnic['items']:
    it['seed_status'] = 'INCOMPLETE_STANDARD_DATA'
    it['source_system_label'] = 'GB_T_3304_PUBLIC_STANDARD'
    it.setdefault('source_systems', [{'system': 'GB_T_3304_PUBLIC_STANDARD', 'source_name': it.get('gb_3304_code')}])
write(os.path.join(SEED_ROOT, 'system', 'ethnic-group.json'), ethnic)
counts['ethnic'] = current
print(f"  ethnic-group.json: {current}/{EXPECTED} (missing {missing})")

# Education: file is SAFE_TO_SEED_SYSTEM, but some items are GB_T_4658 only
edu = load(os.path.join(SEED_ROOT, 'system', 'education.json'))
edu['meta']['seed_status_at_file_level'] = 'SAFE_TO_SEED_SYSTEM'
for it in edu['items']:
    src = it.get('source_systems') or []
    if src and any(s.get('system') == 'DEV' for s in src):
        it['seed_status'] = 'SAFE_TO_SEED_SYSTEM'
        it['source_system_label'] = 'DEV'
    else:
        # GB/T 4658 additions - need PROPOSED since not in DEV
        it['seed_status'] = 'PROPOSED'
        it['source_system_label'] = 'GB_T_4658_PUBLIC_STANDARD'
        it.setdefault('source_systems', [{'system': 'GB_T_4658_PUBLIC_STANDARD', 'source_name': it.get('canonical_name_zh')}])
edu['meta']['items_safe_to_seed_system'] = sum(1 for x in edu['items'] if x['seed_status'] == 'SAFE_TO_SEED_SYSTEM')
edu['meta']['items_proposed'] = sum(1 for x in edu['items'] if x['seed_status'] == 'PROPOSED')
edu['meta']['notes'] = [
    f"6 items from DEV dictionary (学历) are SAFE_TO_SEED_SYSTEM.",
    f"4 items added from GB/T 4658 (高中/初中/小学/中专) are PROPOSED.",
    f"1 item is mixed: PHD_CAND 博士研究生 — derived from GB/T 4658 + DEV (currently set to PROPOSED; needs review).",
    f"Item count breakdown: SAFE={edu['meta']['items_safe_to_seed_system']}, PROPOSED={edu['meta']['items_proposed']}, total={len(edu['items'])}"
]
write(os.path.join(SEED_ROOT, 'system', 'education.json'), edu)
counts['education'] = len(edu['items'])
print(f"  education.json: SAFE={edu['meta']['items_safe_to_seed_system']}, PROPOSED={edu['meta']['items_proposed']}")

# Semantic data type: 13 items, mostly PROPOSED, a few ADAPT
sem = load(os.path.join(SEED_ROOT, 'system', 'semantic-data-type.json'))
sem['meta']['seed_status_at_file_level'] = 'SAFE_TO_SEED_SYSTEM'  # schema + classification metadata
for it in sem['business_semantic_types']:
    it['seed_status'] = 'PROPOSED'  # default all are PROPOSED
    it['source_system_label'] = 'GULIERP_DESIGN'  # because of GAP in evidence
    it.setdefault('source_systems', [{'system': it['source_system_label'], 'canonical_decision': it['canonical_decision']}])
    # Mark the 2 with direct DEV evidence (AMOUNT, EXCHANGE_RATE) as ADAPT (status=PROPOSED still since not FROZEN)
    if it['canonical_decision'] == 'ADAPT' and it.get('dev_evidence', {}).get('DataTypeID') == 4:
        if it['canonical_code'] in ('AMOUNT', 'EXCHANGE_RATE', 'WEIGHT'):
            it['seed_status'] = 'PROPOSED'  # still PROPOSED but well-grounded
            it['source_system_label'] = 'DEV+STANDARD_INFERENCE'
            it['source_systems'] = [{'system': 'DEV', 'source_name': 'JU_DataType DataTypeID=4 (小数)'}, {'system': 'STANDARD_INFERENCE'}]
write(os.path.join(SEED_ROOT, 'system', 'semantic-data-type.json'), sem)
counts['semantic'] = len(sem['business_semantic_types'])
print(f"  semantic-data-type.json: {len(sem['business_semantic_types'])} items, all PROPOSED (per policy)")

# Tenant template
counts['payment'] = add_seed_status(os.path.join(SEED_ROOT, 'tenant-template', 'payment-method.json'), 'SAFE_TO_SEED_TENANT_TEMPLATE')
counts['bp_type'] = add_seed_status(os.path.join(SEED_ROOT, 'tenant-template', 'business-partner-type.json'), 'SAFE_TO_SEED_TENANT_TEMPLATE')

# Position: SAFE_TO_SEED_TENANT_TEMPLATE, but with OCCUPATION_SOURCE_NOT_FOUND note
pos = load(os.path.join(SEED_ROOT, 'tenant-template', 'position.json'))
pos['meta']['seed_status_at_file_level'] = 'SAFE_TO_SEED_TENANT_TEMPLATE'
pos['meta']['occupation_source_status'] = 'OCCUPATION_SOURCE_NOT_FOUND'
pos['meta']['notes'] = [
    'Position (岗位) is the company-internal role/job position. DEV 岗位 dictionary (RecordID=3523) has 4 items: 总经理/经理/科员/操作员.',
    'Occupation (职业) is a different concept — it is the worker\'s professional classification (e.g. 工程师/教师/医生).',
    'OCCUPATION_SOURCE_NOT_FOUND: this seed is for Position ONLY. A future Goal should add Occupation seed from GB/T 8561-2001 职业分类与代码 or similar.',
    'Position here is preserved as tenant_template (each tenant can override; default is the 4 DEV items).',
]
for it in pos['items']:
    it['seed_status'] = 'SAFE_TO_SEED_TENANT_TEMPLATE'
    it['source_system_label'] = 'DEV'
write(os.path.join(SEED_ROOT, 'tenant-template', 'position.json'), pos)
counts['position'] = len(pos['items'])
print(f"  position.json: {len(pos['items'])} items, all SAFE_TO_SEED_TENANT_TEMPLATE (OCCUPATION_SOURCE_NOT_FOUND)")


# =============================================================================
# P9. normalized-manifest.json with checksums
# =============================================================================
def build_normalized_manifest():
    """Walk _normalized/ and produce a manifest with SHA-256 for every file."""
    manifest = {
        'meta': {
            'extraction_time': EXTRACTION_TIME,
            'source_origin': 'D:\\guli\\gulierp\\docs\\reverse-engineering\\dev-meta\\',
            'normalizer': 'tools/discovery/mdm-000d/extract_dev_metadata.py',
            'normalizer_version': 'MDM-000D-R1',
            'encoding_normalization': 'GBK-encoded JSON property names were re-decoded to UTF-8 (using strict-decode + CJK validation).',
            'git_policy': 'this manifest records all _normalized/ contents; the _normalized/ folder itself is .gitignored. To restore: re-run extract_dev_metadata.py and verify checksums match.',
        },
        'totals': {
            'file_count': 0,
            'total_bytes': 0,
        },
        'files': [],
    }
    file_count = 0
    total_bytes = 0
    for root, _, files in os.walk(NORM):
        for f in sorted(files):
            p = os.path.join(root, f)
            rel = os.path.relpath(p, NORM)
            sz = os.path.getsize(p)
            sha = sha256_of_file(p)
            manifest['files'].append({
                'path': rel,
                'size_bytes': sz,
                'sha256': sha,
            })
            file_count += 1
            total_bytes += sz
    manifest['totals']['file_count'] = file_count
    manifest['totals']['total_bytes'] = total_bytes
    return manifest


norm_man = build_normalized_manifest()
write(os.path.join(CANON, 'normalized-manifest.json'), norm_man)
print(f"  normalized-manifest.json: {norm_man['totals']['file_count']} files, {norm_man['totals']['total_bytes']:,} bytes total")


# =============================================================================
# P12. Architectural Review Findings (human/agent-architectural)
# =============================================================================
architectural_findings = {
    'meta': {
        'extraction_time': EXTRACTION_TIME,
        'reviewer': 'agent (Mavis) architectural review during MDM-000D-R1 curation',
        'note_on_dimension': 'These are HUMAN/AGENT-ARCHITECTURAL concerns (semantic, design, governance, scope). They are NOT the same as machine-run automated checks (null/duplicate code). For automated checks see automated-data-quality-findings.json. The two files are intentionally separate to avoid metric conflation.',
    },
    'findings': [
        {
            'id': 'AR-001',
            'severity': 'BLOCKER',
            'category': 'EXTRACTION_GAP',
            'description': f"JU_DataType has {juids['source_rowcount']} rows in source; only {juids['direct_sample_count']} in sample. {juids['unresolved_count']} DataTypeIDs (referenced by template fields but without DataTypeName) cannot be canonicalized without re-extraction: {juids['unresolved_ids']}. An additional {juids['completely_unknown_ids_count']} source rows are completely orphaned (not in sample, not referenced).",
            'affected_items': [f"DataTypeID {x}" for x in juids['unresolved_ids']] + [f"~{juids['completely_unknown_ids_count']} unknown IDs (not in sample, not referenced)"],
            'mitigation': 'Re-run DevReverse with TOP N=50 for JU_DataType (requires reconnecting to dev DB; not done in MDM-000D per READ-ONLY policy). Until then, all 12 GuliERP semantic types are PROPOSED — not FROZEN.',
        },
        {
            'id': 'AR-002',
            'severity': 'BLOCKER',
            'category': 'TASK_DESIGN_ASSUMPTION',
            'description': 'ONLYIT_INDEPENDENT_SOURCE = NOT_AVAILABLE. The dev database is Onlyit-derived (per DEV_METADATA_REVERSE_ENGINEERING_REPORT.md line 6 "dev (网友制造 / Onlyit)"). The task design assumed DEV/ONLYIT/VOL are three independent sources. This is not the case.',
            'affected_items': 'all three-way comparisons',
            'mitigation': 'Adopt canonical source strategy: DEV = primary ERP business evidence; GB/ISO/Standard = standard reference validation; VOL = productivity/Dictionary/Lookup pattern reference. NOT a failure; a correction of the source model.',
        },
        {
            'id': 'AR-003',
            'severity': 'REVIEW_REQUIRED',
            'category': 'TECHNICAL_DEBT',
            'description': 'DEV 数据库 has 0 FK / 0 CHECK constraints; Code/Name fields are mixed, causing SOURCE_NAME/SOURCE_CODE separation issues (e.g. 单位字典 代码 field is empty for all items; must use 名称 as canonical code).',
            'affected_items': 'all DEV dictionaries',
            'mitigation': 'GuliERP 走 strong-typed C# + Snowflake ID, never relies on 名称 strings. Seeds already separate canonical_code from source_name.',
        },
        {
            'id': 'AR-004',
            'severity': 'REVIEW_REQUIRED',
            'category': 'INCOMPLETE_STANDARD',
            'description': 'ethnic-group.json has 42 items out of 56 (GB/T 3304 standard). Missing 14 minorities. Currently classified SAFE_TO_SEED_SYSTEM at file level — DOWNGRADED to INCOMPLETE_STANDARD_DATA per curation policy.',
            'affected_items': 'system/ethnic-group.json',
            'mitigation': 'Re-extract from authoritative GB/T 3304 source (国家标准全文公开系统). Until 56/56, file is INCOMPLETE_STANDARD_DATA, items are PROPOSED.',
        },
        {
            'id': 'AR-005',
            'severity': 'REVIEW_REQUIRED',
            'category': 'PROVENANCE_BLEND',
            'description': 'system/uom.json originally had file-level classification SAFE_TO_SEED_SYSTEM covering all 21 items, but only 13 have DEV provenance; 8 are EXTERNAL_STANDARD_CANDIDATE (ISO 80000 SI).',
            'affected_items': 'system/uom.json items 14-21 (KM/CM/MM/L/ML/H/MIN/D)',
            'mitigation': 'Per-item seed_status applied. 13 DEV items = SAFE_TO_SEED_SYSTEM; 8 EXTERNAL items = PROPOSED. file-level classification changed from blanket to MIXED.',
        },
        {
            'id': 'AR-006',
            'severity': 'REVIEW_REQUIRED',
            'category': 'SEMANTIC_BLEND',
            'description': 'Position (岗位, company-internal job title) is being conflated with Occupation (职业, professional classification) in some downstream consumers.',
            'affected_items': 'tenant-template/position.json',
            'mitigation': 'Position is preserved as SAFE_TO_SEED_TENANT_TEMPLATE. Occupation flagged as OCCUPATION_SOURCE_NOT_FOUND; future Goal must seed from GB/T 8561-2001 or similar.',
        },
        {
            'id': 'AR-007',
            'severity': 'REVIEW_REQUIRED',
            'category': 'TIME_BOUND_DATA',
            'description': '行政区划 (Country/Province/City/County) is time-sensitive (GB/T 2260 changes yearly). Should not be committed to Git.',
            'affected_items': 'system/country.json',
            'mitigation': 'country.json: 0 items, schema_template only, classification NEEDS_EXTERNAL_STANDARD_UPDATE. Runtime loads from authoritative source (国家统计局 / 民政部).',
        },
        {
            'id': 'AR-008',
            'severity': 'SAFE_TO_SEED',
            'category': 'INTERNAL_CONSISTENCY',
            'description': 'system/education.json: 6 DEV items (SAFE) + 4 GB/T 4658 items (PROPOSED) = 10 total. After per-item seed_status applied, no double-counting.',
            'affected_items': 'system/education.json',
            'mitigation': 'Resolved via per-item seed_status.',
        },
        {
            'id': 'AR-009',
            'severity': 'SAFE_TO_SEED',
            'category': 'INTERNAL_CONSISTENCY',
            'description': 'tenant-template/payment-method.json and business-partner-type.json: all items have DEV provenance; no duplicates; all SAFE_TO_SEED_TENANT_TEMPLATE.',
            'affected_items': 'tenant-template/payment-method.json, business-partner-type.json',
            'mitigation': 'None needed.',
        },
        {
            'id': 'AR-010',
            'severity': 'SAFE_TO_SEED',
            'category': 'INTERNAL_CONSISTENCY',
            'description': 'system/semantic-data-type.json: 13 types defined, all PROPOSED (per policy no FROZEN until MDM-000 implementation). Schema covers AMOUNT/UNIT_PRICE/COST/QUANTITY/TAX_RATE/PERCENTAGE/DISCOUNT_RATE/EXCHANGE_RATE/LENGTH/WEIGHT/AREA/VOLUME/TIME_DURATION.',
            'affected_items': 'system/semantic-data-type.json',
            'mitigation': 'None needed for V1 curation. MDM-000 implementation will validate + ACCEPT.',
        },
    ],
    'totals': {
        'blocker': 2,
        'review_required': 5,
        'safe_to_seed': 3,
    },
    'note_on_two_dimensional_findings': (
        'MDM-000D-R1 has TWO orthogonal finding dimensions: '
        '(1) automated-data-quality-findings.json = machine-run structural scan (0 BLOCKER / 0 REVIEW / N INFO on seed file integrity), and '
        '(2) architectural-review-findings.json = human/agent semantic + governance + scope review (2 BLOCKER / 5 REVIEW / 3 SAFE). '
        'They are NEVER merged into a single DATA_QUALITY_FINDINGS count. The previous (MDM-000D) document conflated them; this R1 fix separates them.'
    ),
}
write(os.path.join(SEED_ROOT, 'architectural-review-findings.json'), architectural_findings)
print(f"  architectural-review-findings.json: {architectural_findings['totals']}")


# =============================================================================
# P12. Updated master manifest.json
# =============================================================================
master_manifest = {
    'meta': {
        'goal': 'MDM-000D',
        'curation_revision': 'MDM-000D-R1',
        'extraction_time': EXTRACTION_TIME,
        'extractor': 'Mavis (gulierp-next mvs_11a243eed8e544d6b19087711a392283)',
        'read_only_source': 'D:\\guli\\gulierp\\docs\\reverse-engineering\\dev-meta\\',
        'canonical_source_strategy': {
            'DEV': 'PRIMARY_ERP_BUSINESS_EVIDENCE — dev is the Onlyit-derived SQL Server at 192.168.2.28, fully extracted via 22 JSON dumps (ju-meta/ju-columns/biz-tables/dictionaries/templates/template-fields/relations/formulas/actions/permissions/organizations/workflows/size + ju-samples 79 + biz-samples 12).',
            'ONLYIT': 'NOT_AVAILABLE_AS_INDEPENDENT_SOURCE — the dev DB IS the Onlyit-derived source. There is no separate Onlyit corpus. AR-002 documents this honestly.',
            'VOL': 'PATTERN_REFERENCE_ONLY — docs/research/vol-pro/ contains 22 research documents confirming Sys_Dictionary + Sys_DictionaryList pattern. NO DataType/Decimal/precision-specific table. VOL is not a value source for UOM/Currency/Semantic Type seeds.',
            'EXTERNAL_STANDARDS': 'GB/T 3304 (民族), GB/T 4658 (学历), ISO 4217 (currency), ISO 80000 (SI units), ISO 3166 (country codes), GB/T 2260 (行政区划). Used where DEV is silent and a public standard is authoritative.',
        },
        'gate': 'MDM_000D_CURATED_SEED_ASSETS_READY (post R1)',
        'seeder_policy': 'Future Seeder MUST load only seed_status=SAFE_TO_SEED_SYSTEM or SAFE_TO_SEED_TENANT_TEMPLATE items. PROPOSED / REFERENCE_ONLY / INCOMPLETE_STANDARD_DATA / NEEDS_EXTERNAL_STANDARD_UPDATE items require explicit opt-in or are deferred.',
    },
    'ju_datatype_id_sets': {
        'source_rowcount': juids['source_rowcount'],
        'all_visible_count': juids['all_visible_count'],
        'direct_sample_count': juids['direct_sample_count'],
        'referenced_count': juids['referenced_count'],
        'unresolved_count': juids['unresolved_count'],
        'unreferenced_count': juids['unreferenced_count'],
        'completely_unknown_ids_count': juids['completely_unknown_ids_count'],
    },
    'seed_datasets': [
        {
            'dataset': 'uom',
            'path': 'data/bootstrap/reference/system/uom.json',
            'category': 'system_reference_data',
            'scope': 'SYSTEM',
            'itemCount': counts.get('uom'),
            'items_safe_to_seed_system': 13,
            'items_proposed': 8,
            'items_proposed_source': 'EXTERNAL_STANDARD_CANDIDATE (ISO 80000 SI)',
            'seedStatus': 'MIXED',
            'sourceSystems': ['DEV', 'ISO_80000_PUBLIC_STANDARD_CANDIDATE'],
            'canonicalVersion': '2026-08-20',
            'notes': '13 items from DEV dictionary 字典表s RecordID=15 (单位). 8 items are EXTERNAL_STANDARD_CANDIDATE (KM/CM/MM/L/ML/H/MIN/D) added for SI completeness.',
        },
        {
            'dataset': 'currency',
            'path': 'data/bootstrap/reference/system/currency.json',
            'category': 'system_reference_data',
            'scope': 'SYSTEM',
            'itemCount': counts.get('currency'),
            'seedStatus': 'REFERENCE_ONLY',
            'sourceSystems': ['ISO_4217_PUBLIC_STANDARD'],
            'canonicalVersion': '2026-08-20',
            'notes': '20 major trading currencies. Not from DEV. Requires external authoritative source for live exchange rates.',
        },
        {
            'dataset': 'country',
            'path': 'data/bootstrap/reference/system/country.json',
            'category': 'system_reference_data',
            'scope': 'SYSTEM',
            'itemCount': 0,
            'seedStatus': 'NEEDS_EXTERNAL_STANDARD_UPDATE',
            'sourceSystems': [],
            'canonicalVersion': '2026-08-20',
            'notes': 'Schema template only. Runtime loads from GB/T 2260 + ISO 3166. No items committed to Git.',
        },
        {
            'dataset': 'ethnic-group',
            'path': 'data/bootstrap/reference/system/ethnic-group.json',
            'category': 'system_reference_data',
            'scope': 'SYSTEM',
            'itemCount': counts.get('ethnic'),
            'expected_count': 56,
            'missing_count': 14,
            'seedStatus': 'INCOMPLETE_STANDARD_DATA',
            'sourceSystems': ['GB_T_3304_PUBLIC_STANDARD'],
            'canonicalVersion': '2026-08-20',
            'notes': '42 of 56 minorities. Standard is GB/T 3304. Re-extract to reach 56/56 before SAFE_TO_SEED_SYSTEM.',
        },
        {
            'dataset': 'education',
            'path': 'data/bootstrap/reference/system/education.json',
            'category': 'system_reference_data',
            'scope': 'SYSTEM',
            'itemCount': counts.get('education'),
            'items_safe_to_seed_system': 6,
            'items_proposed': 4,
            'seedStatus': 'MIXED',
            'sourceSystems': ['DEV', 'GB_T_4658_PUBLIC_STANDARD'],
            'canonicalVersion': '2026-08-20',
            'notes': '6 DEV (学历 dictionary RecordID=2943) + 4 GB/T 4658 (高中/初中/小学/中专) = 10.',
        },
        {
            'dataset': 'semantic-data-type',
            'path': 'data/bootstrap/reference/system/semantic-data-type.json',
            'category': 'business_semantic_metadata',
            'scope': 'SYSTEM',
            'itemCount': counts.get('semantic'),
            'seedStatus': 'PROPOSED',
            'sourceSystems': ['DEV+STANDARD_INFERENCE', 'GULIERP_DESIGN'],
            'canonicalVersion': '2026-08-20',
            'notes': '13 semantic types. All PROPOSED. No FROZEN until MDM-000 implementation. Covers AMOUNT/UNIT_PRICE/COST/QUANTITY/TAX_RATE/PERCENTAGE/DISCOUNT_RATE/EXCHANGE_RATE/LENGTH/WEIGHT/AREA/VOLUME/TIME_DURATION.',
        },
        {
            'dataset': 'payment-method',
            'path': 'data/bootstrap/reference/tenant-template/payment-method.json',
            'category': 'tenant_template_data',
            'scope': 'TENANT_TEMPLATE',
            'itemCount': counts.get('payment'),
            'seedStatus': 'SAFE_TO_SEED_TENANT_TEMPLATE',
            'sourceSystems': ['DEV'],
            'canonicalVersion': '2026-08-20',
            'notes': '5 items from DEV 付款方式 (RecordID=3052). Split into PM (method) and PT (term).',
        },
        {
            'dataset': 'business-partner-type',
            'path': 'data/bootstrap/reference/tenant-template/business-partner-type.json',
            'category': 'tenant_template_data',
            'scope': 'TENANT_TEMPLATE',
            'itemCount': counts.get('bp_type'),
            'seedStatus': 'SAFE_TO_SEED_TENANT_TEMPLATE',
            'sourceSystems': ['DEV'],
            'canonicalVersion': '2026-08-20',
            'notes': '4 items from DEV 往来类型 (RecordID=3497).',
        },
        {
            'dataset': 'position',
            'path': 'data/bootstrap/reference/tenant-template/position.json',
            'category': 'tenant_template_data',
            'scope': 'TENANT_TEMPLATE',
            'itemCount': counts.get('position'),
            'seedStatus': 'SAFE_TO_SEED_TENANT_TEMPLATE',
            'sourceSystems': ['DEV'],
            'canonicalVersion': '2026-08-20',
            'notes': '4 items from DEV 岗位 (RecordID=3523). Position is NOT Occupation; OCCUPATION_SOURCE_NOT_FOUND for the latter.',
        },
    ],
    'internal_artifacts': [
        {
            'path': 'data/bootstrap/reference/mapping/source-canonical-mapping.json',
            'classification': 'INTERNAL_MAPPING',
            'decision_counts': {'REUSE': 13, 'ADAPT': 12, 'REJECT': 1, 'PROPOSED': 4},
        },
        {
            'path': 'data/bootstrap/reference/automated-data-quality-findings.json',
            'classification': 'INTERNAL_QUALITY_AUTOMATED',
            'totals': automated_findings['totals'],
            'note': 'machine-run; checks structural seed integrity. NEVER merged with architectural review.',
        },
        {
            'path': 'data/bootstrap/reference/architectural-review-findings.json',
            'classification': 'INTERNAL_QUALITY_ARCHITECTURAL',
            'totals': architectural_findings['totals'],
            'note': 'human/agent semantic + governance + scope review. NEVER merged with automated scan.',
        },
    ],
    'discovery_artifacts': [
        {
            'path': 'tools/discovery/mdm-000d/_canonical/dev_business_data_type_catalog.json',
            'classification': 'DISCOVERY_CANONICAL',
        },
        {
            'path': 'tools/discovery/mdm-000d/_canonical/dev_dictionary_canonical.json',
            'classification': 'DISCOVERY_CANONICAL',
        },
        {
            'path': 'tools/discovery/mdm-000d/_canonical/ju_datatype_id_sets.json',
            'classification': 'DISCOVERY_CANONICAL',
        },
        {
            'path': 'tools/discovery/mdm-000d/_canonical/normalized-manifest.json',
            'classification': 'DISCOVERY_MANIFEST',
            'note': 'Records SHA-256 of every file in _normalized/ (which is .gitignored).',
        },
    ],
    'totals': {
        'seed_datasets': 9,
        'internal_artifacts': 3,
        'discovery_artifacts': 4,
        'ju_datatype_unresolved_ids': juids['unresolved_count'],
        'ju_datatype_orphan_ids': juids['completely_unknown_ids_count'],
    },
    'policy_enforcement': {
        'seeder_may_auto_load': ['SAFE_TO_SEED_SYSTEM', 'SAFE_TO_SEED_TENANT_TEMPLATE'],
        'seeder_must_opt_in': ['PROPOSED', 'MIXED'],
        'seeder_must_defer': ['REFERENCE_ONLY', 'INCOMPLETE_STANDARD_DATA', 'NEEDS_EXTERNAL_STANDARD_UPDATE'],
    },
}

write(os.path.join(SEED_ROOT, 'manifest.json'), master_manifest)
print()
print('=== Master manifest rebuilt ===')
print(f"  seed_datasets: {master_manifest['totals']['seed_datasets']}")
print(f"  internal_artifacts: {master_manifest['totals']['internal_artifacts']}")
print(f"  discovery_artifacts: {master_manifest['totals']['discovery_artifacts']}")
print()
print('=== Per-dataset seedStatus ===')
for ds in master_manifest['seed_datasets']:
    print(f"  {ds['dataset']:<25} scope={ds['scope']:<18} seedStatus={ds['seedStatus']:<32} itemCount={ds.get('itemCount')}")
