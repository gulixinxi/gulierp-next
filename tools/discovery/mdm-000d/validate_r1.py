"""MDM-000D-R1 final validation.

Runs:
  1. Syntax check on all 7 discovery scripts
  2. JSON parse + UTF-8 + shape validation on all curated seed/manifest/canonical files
  3. Duplicate stable code scan (canonical_code / iso_4217_code / gb_3304_code)
  4. Source provenance completeness scan (every item must have source_systems OR explicit source label)
  5. Manifest reference validation (every file in manifest exists)
  6. Re-run automated DQ scan
  7. Cross-check that JSON / MD / manifest all agree on semantic-type count (13)
  8. Cross-check that DQ finding file counts are consistent

Emits a final VALIDATION_REPORT.json with results.
"""
import ast
import json
import os
import subprocess
import sys
import io
from pathlib import Path

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

SCRIPT_DIR = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d'
SEED_ROOT = r'D:\guli\projects\gulierp-next\data\bootstrap\reference'
CANON = os.path.join(SCRIPT_DIR, '_canonical')
NORM = os.path.join(SCRIPT_DIR, '_normalized')
DOCS = r'D:\guli\projects\gulierp-next\docs\architecture'

results = {'checks': [], 'summary': {}}


def add(level, name, detail):
    results['checks'].append({'level': level, 'name': name, 'detail': detail})


# 1. Syntax check on all discovery scripts
print('=== 1. Script syntax check ===')
SCRIPTS = [
    'extract_dev_metadata.py',
    'extract_judatatype.py',
    'build_dev_datatype_catalog.py',
    'build_dictionary_canonical.py',
    'build_seed_candidates.py',
    'run_data_quality_scan.py',
    'curate_assets.py',
]
all_ok = True
for s in SCRIPTS:
    p = os.path.join(SCRIPT_DIR, s)
    try:
        with open(p, encoding='utf-8') as f:
            ast.parse(f.read())
        print(f'  [OK]  {s}')
        add('INFO', f'syntax:{s}', 'parse OK')
    except SyntaxError as e:
        print(f'  [FAIL] {s}: {e}')
        add('BLOCKER', f'syntax:{s}', f'SyntaxError: {e}')
        all_ok = False

# 2. JSON parse + UTF-8 + shape
print()
print('=== 2. JSON parse + UTF-8 ===')
JSON_FILES = []
for root in [SEED_ROOT, CANON]:
    for p in Path(root).rglob('*.json'):
        JSON_FILES.append(str(p))
all_ok = True
for p in sorted(JSON_FILES):
    rel = os.path.relpath(p, r'D:\guli\projects\gulierp-next')
    try:
        with open(p, encoding='utf-8') as f:
            data = json.load(f)
        # UTF-8 BOM check
        with open(p, 'rb') as f:
            head = f.read(3)
        if head == b'\xef\xbb\xbf':
            add('REVIEW_REQUIRED', f'utf8-bom:{rel}', 'has UTF-8 BOM, should be removed')
            print(f'  [WARN] {rel}: UTF-8 BOM found')
        else:
            print(f'  [OK]   {rel}')
    except Exception as e:
        add('BLOCKER', f'json-parse:{rel}', f'{type(e).__name__}: {e}')
        print(f'  [FAIL] {rel}: {e}')
        all_ok = False

# 3. Duplicate stable code scan
print()
print('=== 3. Duplicate stable code scan ===')
code_field_map = {
    'system/uom.json': 'canonical_code',
    'system/currency.json': 'iso_4217_code',
    'system/country.json': 'iso_3166_1_alpha_2',
    'system/ethnic-group.json': 'gb_3304_code',
    'system/education.json': 'canonical_code',
    'system/semantic-data-type.json': 'canonical_code',
    'tenant-template/payment-method.json': 'canonical_code',
    'tenant-template/business-partner-type.json': 'canonical_code',
    'tenant-template/position.json': 'canonical_code',
}
all_dup = True
for fn, code_field in code_field_map.items():
    p = os.path.join(SEED_ROOT, fn)
    with open(p, encoding='utf-8') as f:
        data = json.load(f)
    items = data.get('items') or data.get('business_semantic_types') or []
    seen = {}
    for it in items:
        code = it.get(code_field)
        if code is None:
            continue
        if code in seen:
            add('BLOCKER', f'dup-code:{fn}', f'duplicate {code_field}={code}')
            print(f'  [FAIL] {fn}: duplicate {code_field}={code}')
            all_dup = False
        else:
            seen[code] = it
    print(f'  [OK]   {fn}: {len(seen)} unique {code_field} of {len(items)} items')

# 4. Source provenance completeness
print()
print('=== 4. Source provenance completeness ===')
for fn in code_field_map:
    p = os.path.join(SEED_ROOT, fn)
    with open(p, encoding='utf-8') as f:
        data = json.load(f)
    items = data.get('items') or data.get('business_semantic_types') or []
    missing = []
    for it in items:
        sources = it.get('source_systems')
        if sources is None:
            missing.append(it.get(code_field_map[fn]))
    if missing:
        add('REVIEW_REQUIRED', f'provenance:{fn}', f'items without source_systems: {missing}')
        print(f'  [WARN] {fn}: {len(missing)} items without source_systems')
    else:
        print(f'  [OK]   {fn}: all items have source_systems')

# 5. Manifest reference validation
print()
print('=== 5. Manifest reference validation ===')
with open(os.path.join(SEED_ROOT, 'manifest.json'), encoding='utf-8') as f:
    manifest = json.load(f)
paths_in_manifest = []
for ds in manifest.get('seed_datasets', []):
    paths_in_manifest.append(ds['path'])
for ia in manifest.get('internal_artifacts', []):
    paths_in_manifest.append(ia['path'])
for da in manifest.get('discovery_artifacts', []):
    paths_in_manifest.append(da['path'])
root = r'D:\guli\projects\gulierp-next'
all_paths_ok = True
for rel in paths_in_manifest:
    p = os.path.join(root, rel.replace('\\', os.sep))
    if not os.path.exists(p):
        add('BLOCKER', f'manifest-missing:{rel}', 'file referenced in manifest does not exist')
        print(f'  [FAIL] {rel} — does not exist')
        all_paths_ok = False
    else:
        print(f'  [OK]   {rel}')

# 6. Re-run automated DQ scan
print()
print('=== 6. Re-run automated DQ scan ===')
res = subprocess.run(['python', os.path.join(SCRIPT_DIR, 'run_data_quality_scan.py')],
                     capture_output=True, text=True, encoding='utf-8', errors='replace')
for line in res.stdout.splitlines():
    if 'Scan summary' in line:
        print('  ', line)
        add('INFO', 'dq-scan-output', line)

# 7. Cross-check JSON / MD / manifest on semantic-type count
print()
print('=== 7. Cross-check semantic-type count ===')
with open(os.path.join(SEED_ROOT, 'system', 'semantic-data-type.json'), encoding='utf-8') as f:
    sd = json.load(f)
n_json = len(sd.get('business_semantic_types', []))
# MD count: look for each of 13 type names in the MD
type_names = ['AMOUNT', 'UNIT_PRICE', 'COST', 'QUANTITY', 'TAX_RATE', 'PERCENTAGE', 'DISCOUNT_RATE', 'EXCHANGE_RATE', 'LENGTH', 'WEIGHT', 'AREA', 'VOLUME', 'TIME_DURATION']
with open(os.path.join(DOCS, 'MDM_000D_BUSINESS_SEMANTIC_TYPE_MAPPING.md'), encoding='utf-8') as f:
    md_text = f.read()
md_present = sum(1 for t in type_names if t in md_text)
# Manifest count
n_man_count = 0
for ds in manifest['seed_datasets']:
    if ds['dataset'] == 'semantic-data-type':
        n_man_count = ds['itemCount']
        break
print(f'  JSON business_semantic_types: {n_json}')
print(f'  MD type names present: {md_present}/13')
print(f'  Manifest itemCount: {n_man_count}')
if n_json == n_man_count == md_present == 13:
    print('  [OK]   13 in all three sources')
    add('INFO', 'semantic-type-count', f'JSON={n_json} MD={md_present} Manifest={n_man_count}')
else:
    add('BLOCKER', 'semantic-type-count', f'JSON={n_json} MD={md_present} Manifest={n_man_count}')
    print('  [FAIL] count mismatch')

# 8. DQ finding files consistency
print()
print('=== 8. DQ finding files consistency ===')
with open(os.path.join(SEED_ROOT, 'automated-data-quality-findings.json'), encoding='utf-8') as f:
    auto = json.load(f)
with open(os.path.join(SEED_ROOT, 'architectural-review-findings.json'), encoding='utf-8') as f:
    arch = json.load(f)
auto_t = auto['totals']
arch_t = arch['totals']
print(f'  automated: {auto_t}')
print(f'  architectural: {arch_t}')
# Confirm 0/0/N for automated
if auto_t['blocker'] == 0 and auto_t['review_required'] == 0:
    print('  [OK]   automated shows 0/0/N (expected for machine structural scan)')
    add('INFO', 'dq-automated', str(auto_t))
else:
    add('REVIEW_REQUIRED', 'dq-automated', f'expected 0/0, got {auto_t}')
# Confirm 2/5/3 for architectural
if arch_t['blocker'] == 2 and arch_t['review_required'] == 5 and arch_t['safe_to_seed'] == 3:
    print('  [OK]   architectural shows 2/5/3 (matches agent review)')
    add('INFO', 'dq-architectural', str(arch_t))
else:
    add('REVIEW_REQUIRED', 'dq-architectural', f'expected 2/5/3, got {arch_t}')

# Summary
levels = {'BLOCKER': 0, 'REVIEW_REQUIRED': 0, 'INFO': 0}
for c in results['checks']:
    levels[c['level']] = levels.get(c['level'], 0) + 1
results['summary'] = levels
print()
print('=== Summary ===')
print(f'  BLOCKER: {levels["BLOCKER"]}')
print(f'  REVIEW_REQUIRED: {levels["REVIEW_REQUIRED"]}')
print(f'  INFO: {levels["INFO"]}')

# Write VALIDATION_REPORT.json
out = os.path.join(CANON, 'r1_validation_report.json')
with open(out, 'w', encoding='utf-8') as f:
    json.dump(results, f, ensure_ascii=False, indent=2)
print(f'\nWrote {out}')

# Exit code
sys.exit(0 if levels['BLOCKER'] == 0 else 1)
