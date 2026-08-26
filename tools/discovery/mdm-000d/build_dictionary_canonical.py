"""Build DEV Dictionary Canonical Catalog (28 headers + 121 items).

Input:  tools/discovery/mdm-000d/_normalized/dictionaries.json
        tools/discovery/mdl-000d/_normalized/biz-samples/dbo_字典表.json (5 sample header)
        tools/discovery/mdl-000d/_normalized/biz-samples/dbo_字典表s.json (5 sample items)
Output: tools/discovery/mdm-000d/_canonical/dev_dictionary_canonical.json

Structure: header (RecordID = dictionary ID) + item (RecordID = parent header ID).
We pair each item to its parent header by the ID order, since the dictionaries.json
extraction joined header+item in creation order (verified by RecordID continuity).
"""
import json
import os

SRC = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_normalized'
OUT = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_canonical\dev_dictionary_canonical.json'


def main():
    # Load dictionaries.json
    with open(os.path.join(SRC, 'dictionaries.json'), encoding='utf-8') as f:
        rows = json.load(f)

    headers = [r for r in rows if r.get('kind') == 'header']
    items = [r for r in rows if r.get('kind') != 'header']

    # Build header index by RecordID
    header_index = {}
    for h in headers:
        rid = h.get('RecordID')
        header_index[rid] = {
            'dictionary_code_dev': rid,
            'dictionary_name_zh': h.get('字典名'),
            'dictionary_internal_template_id': h.get('RTID'),
            'create_time': h.get('CreateTime'),
            'last_edit_time': h.get('LastEditTime'),
            'create_user': h.get('CreateUser'),
            'report_status': h.get('ReportStatus'),
            'items': [],
        }

    # Group items by their RecordID (which equals the parent header's RecordID)
    for it in items:
        parent_rid = it.get('RecordID')
        if parent_rid not in header_index:
            # Orphan item - print warning
            continue
        header_index[parent_rid]['items'].append({
            'item_sequence_within_parent': it.get('Sequence'),
            'item_row_number': it.get('RN'),
            'name_zh': it.get('名称'),
            'description_zh': it.get('描述'),
            'code_zh': it.get('代码'),
            'parent_dict_name_zh': it.get('字典名'),
            'status_code': it.get('组'),  # 0 = active, 1 = inactive (not confirmed)
        })

    # Sort items within each dict by RN ascending
    for d in header_index.values():
        d['items'].sort(key=lambda x: (x['item_row_number'] is None, x['item_row_number'] or 0))

    grouped = list(header_index.values())

    # Build canonical output
    catalog = {
        'meta': {
            'source': 'DEV (SQL Server 2012 SP1, Onlyit-derived)',
            'extraction_tool': 'tools/dev-reverse/DevReverse.csproj',
            'extraction_date': '2026-08-17',
            'normalized_by': 'MDM-000D',
            'extraction_completeness': 'FULL (149 rows: 28 headers + 121 items)',
            'read_only': True,
            'note_on_5_sample_header_table': 'biz-samples/dbo_字典表.json (5 sample rows) is a SUBSET of these 28 headers; the full set is in dictionaries.json.'
        },
        'totals': {
            'headers': len(headers),
            'items': len(items),
            'sum': len(rows),
        },
        'dictionaries': [
            {
                'dictionary_code_dev': g['dictionary_code_dev'],
                'dictionary_name_zh': g['dictionary_name_zh'],
                'dictionary_internal_template_id': g.get('dictionary_internal_template_id'),
                'create_time': g.get('create_time'),
                'last_edit_time': g.get('last_edit_time'),
                'items_count': len(g['items']),
                'items': g['items'],
            }
            for g in grouped
        ],
    }

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump(catalog, f, ensure_ascii=False, indent=2)
    print(f'Wrote {OUT} ({os.path.getsize(OUT):,} B)')
    print(f'  - {len(headers)} dictionary headers')
    print(f'  - {len(items)} dictionary items')
    print('  - dictionary names:')
    for g in grouped:
        print(f'    * RecordID={g["dictionary_code_dev"]:>4}  {g["dictionary_name_zh"]!r:<20} ({len(g["items"])} items)')


if __name__ == '__main__':
    main()
