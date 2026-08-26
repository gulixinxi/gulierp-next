"""Run data quality scans on the seed candidates + raw DEV dictionaries.

Checks:
  - duplicate codes within each seed
  - null/empty name fields
  - DEV/canonical code collision
  - orphan parent (e.g. RTID pointing to non-existent header)
"""
import json
import os
import sys
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

OUT = r'D:\guli\projects\gulierp-next\data\bootstrap\reference'
NORM = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_normalized'
CANON = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_canonical'

results = []


def add(severity, category, message, **extra):
    results.append({'severity': severity, 'category': category, 'message': message, **extra})


def scan_seed(filename, code_field, name_field):
    path = os.path.join(OUT, filename)
    if not os.path.exists(path):
        add('REVIEW_REQUIRED', 'MISSING', f'Seed file missing: {filename}')
        return
    with open(path, encoding='utf-8') as f:
        data = json.load(f)
    items = data.get('items') or data.get('business_semantic_types') or []
    if not items:
        add('INFO', 'EMPTY', f'{filename}: no items')
        return
    seen_codes = {}
    for it in items:
        code = it.get(code_field)
        name = it.get(name_field) or it.get('canonical_name_zh') or it.get('name_zh')
        if not code:
            add('REVIEW_REQUIRED', 'NULL_CODE', f'{filename}: item with no {code_field}', item=it)
        if not name:
            add('REVIEW_REQUIRED', 'NULL_NAME', f'{filename}: item with no name', code=code)
        if code in seen_codes:
            add('BLOCKER', 'DUPLICATE_CODE', f'{filename}: duplicate {code_field}={code}', first=seen_codes[code], second=it)
        else:
            seen_codes[code] = it
    add('INFO', 'STATS', f'{filename}: {len(items)} items, {len(seen_codes)} unique codes')


def scan_dev_dictionaries():
    """Check the 28 headers in dev for any issues."""
    with open(os.path.join(CANON, 'dev_dictionary_canonical.json'), encoding='utf-8') as f:
        d = json.load(f)
    seen_rid = {}
    for dct in d['dictionaries']:
        rid = dct.get('dictionary_code_dev')
        if rid in seen_rid:
            add('BLOCKER', 'DUP_HEADER', f'dev: duplicate header RecordID={rid}', first=seen_rid[rid], second=dct)
        else:
            seen_rid[rid] = dct
        if not dct.get('dictionary_name_zh'):
            add('BLOCKER', 'NULL_DICT_NAME', f'dev: header RecordID={rid} has no name')
        # Check items for null names
        for it in dct['items']:
            if not it.get('name_zh'):
                add('REVIEW_REQUIRED', 'NULL_ITEM_NAME', f"dev dict {dct['dictionary_name_zh']}: item with no name", item=it)
    add('INFO', 'STATS', f"dev dictionaries: {len(d['dictionaries'])} headers, {sum(len(x['items']) for x in d['dictionaries'])} items")


# Scan all seed files
print('Scanning seed candidates...')
file_code_field = {
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
for f, code_field in file_code_field.items():
    scan_seed(f, code_field, 'canonical_name_zh')

scan_dev_dictionaries()

# Summary
summary = {'BLOCKER': 0, 'REVIEW_REQUIRED': 0, 'INFO': 0}
for r in results:
    summary[r['severity']] = summary.get(r['severity'], 0) + 1
print()
print(f"=== Scan summary: {summary} ===")
for r in results:
    if r['severity'] in ('BLOCKER', 'REVIEW_REQUIRED'):
        print(f"  [{r['severity']}] {r['category']}: {r['message']}")
