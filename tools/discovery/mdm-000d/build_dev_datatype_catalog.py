"""Build DEV Business Semantic Data Type Catalog from JU_DataType sample + schema.

Input: tools/discovery/mdm-000d/_normalized/ju-samples/dbo_JU_DataType.json (5 sample rows)
       tools/discovery/mdm-000d/_normalized/ju-columns.json (full column schema)
Output: tools/discovery/mdm-000d/_canonical/dev_business_data_type_catalog.json

We have 5 sample rows out of 18 total in dev. The 5 are 字符/整数/小数/时间/图像.
The other 13 are not in our sample. We document the gap honestly.
"""
import json
import os

SRC = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_normalized'
OUT = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_canonical\dev_business_data_type_catalog.json'


def main():
    # Load 5 sample rows of JU_DataType
    sample_path = os.path.join(SRC, 'ju-samples', 'dbo_JU_DataType.json')
    with open(sample_path, encoding='utf-8') as f:
        sample_data = json.load(f)
    sample_rows = sample_data.get('rows', [])

    # Load full schema of JU_DataType
    ju_cols_path = os.path.join(SRC, 'ju-columns.compact.json')
    with open(ju_cols_path, encoding='utf-8') as f:
        ju_cols = json.load(f)
    dt_schema = next((c for c in ju_cols if c.get('table') == 'JU_DataType'), None)
    if not dt_schema:
        raise SystemExit('JU_DataType schema not found')

    # Build catalog
    catalog = {
        'meta': {
            'source': 'DEV (SQL Server 2012 SP1, Onlyit-derived)',
            'source_evidence': 'D:\\guli\\gulierp\\docs\\reverse-engineering\\DEV_METADATA_REVERSE_ENGINEERING_REPORT.md',
            'extraction_tool': 'tools/dev-reverse/DevReverse.csproj (SELECT TOP 5)',
            'extraction_date': '2026-08-17',
            'producer': 'GOAL-P1-004B (closed)',
            'normalized_by': 'MDM-000D extraction script (this run)',
            'extraction_completeness': 'PARTIAL (5/18 sample rows, 13 unknown)',
            'gulierp_consumer': 'MDM-000D',
            'read_only': True,
            'decision_policy': 'Faithful record of DEV; no agent invention. Status PROPOSED for future canonical decision.'
        },
        'schema': {
            'fqtn': dt_schema.get('fqtn'),
            'rowcount_in_source': dt_schema.get('rowcount'),
            'columns': [
                {
                    'name': c['name'],
                    'sql_type': c['type'],
                    'max_length': c['max_length'],
                    'precision': c['precision'],
                    'scale': c['scale'],
                    'nullable': c['nullable'],
                }
                for c in dt_schema.get('columns', [])
            ]
        },
        'sample_rows_5_of_18': [
            {
                'DataTypeID': r.get('DataTypeID'),
                'DataTypeName_zh': r.get('DataTypeName'),
                'BaseType': r.get('BaseType'),
                'BaseLength': r.get('BaseLength'),
                'BasePrecision': r.get('BasePrecision'),
                'DefaultValue': r.get('DefaultValue'),
                'MatchPattern_zh': r.get('MatchPattern'),
                'Memo_zh': r.get('Memo'),
                'ISActive': r.get('ISActive'),
                'ResLvl': r.get('ResLvl'),
            }
            for r in sample_rows
        ],
        'inferred_data_types': [
            # Best-effort mapping from observed BaseType values. BaseType is an internal enum
            # in Onlyit; we have 5 examples. The 5 visible BaseType values are 1, 3, 4, 5.
            # We document the inference; we do NOT fabricate the 13 unknown rows.
            {
                'BaseType': 1,
                'observed_in_sample': True,
                'examples': [2],  # DataTypeID 2 = 字符
                'likely_semantic': 'TEXT / STRING (nvarchar)',
                'dotnet_inference': 'string',
                'postgres_inference': 'text / varchar(n)',
                'notes': 'Inferred from DataTypeID 2 (字符); BaseType=1 only ever paired with character data in our sample.'
            },
            {
                'BaseType': 3,
                'observed_in_sample': True,
                'examples': [3, 4],  # DataTypeID 3 = 整数, 4 = 小数
                'likely_semantic': 'NUMERIC (decimal / numeric)',
                'dotnet_inference': 'decimal / int (per BasePrecision)',
                'postgres_inference': 'numeric(precision, scale)',
                'notes': 'Integer (ID 3, BaseLength=32, Precision=0) and Decimal (ID 4, BaseLength=34, Precision=2) both share BaseType=3; Precision discriminator.'
            },
            {
                'BaseType': 4,
                'observed_in_sample': True,
                'examples': [5],  # DataTypeID 5 = 时间
                'likely_semantic': 'DATETIME / TIME',
                'dotnet_inference': 'DateTimeOffset',
                'postgres_inference': 'timestamptz / date',
                'notes': 'Inferred from DataTypeID 5 (时间).'
            },
            {
                'BaseType': 5,
                'observed_in_sample': True,
                'examples': [6],  # DataTypeID 6 = 图像
                'likely_semantic': 'BINARY (image / blob / attachment)',
                'dotnet_inference': 'byte[] / object storage ref',
                'postgres_inference': 'bytea OR object store pointer',
                'notes': 'GuliERP REJECT metadata-driven image column; should be Attachment entity + object store.'
            }
        ],
        'unknown_data_types_13_of_18': {
            'count': 13,
            'data_type_ids_known_sample_covers': [2, 3, 4, 5, 6],
            'reason_for_gap': 'DevReverse Program.cs only emits SELECT TOP 5 per table. The 13 unobserved DataTypeIDs are NOT in the sample.',
            'how_to_close_gap': [
                'Option A: Re-run DevReverse with TOP N=50 for JU_DataType only. (Requires reconnecting to dev DB; not done in MDM-000D per READ-ONLY policy.)',
                'Option B: Hand-derive from template-fields.json usage. JU_TemplateTableField.ComponentType + DataTypeID reference back to JU_DataType. We can list all DataTypeIDs actually used by the 6,339 fields. That gives the 18 IDs without their names. We do that below.'
            ],
            'data_type_id_usage_from_template_fields': None  # populated below
        }
    }

    # Discover DataTypeIDs actually used by template fields
    tf_path = os.path.join(SRC, 'template-fields.json')
    with open(tf_path, encoding='utf-8') as f:
        tf = json.load(f)
    used = {}
    for field in tf:
        did = field.get('DataTypeID')
        if did is None:
            continue
        used[did] = used.get(did, 0) + 1
    catalog['unknown_data_types_13_of_18']['data_type_id_usage_from_template_fields'] = dict(
        sorted(used.items(), key=lambda x: (x[0] is None, x[0]))
    )
    catalog['unknown_data_types_13_of_18']['note_on_usage'] = (
        'IDs in the sample (2,3,4,5,6) ARE in the usage map. Other IDs that appear here but are NOT in the sample are the 13 unknown. '
        'Their semantic meaning can be INFERRED from template-field.ComponentType+usage, but the names (DataTypeName_zh) remain unknown without re-extraction.'
    )

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump(catalog, f, ensure_ascii=False, indent=2)
    print(f'Wrote {OUT} ({os.path.getsize(OUT):,} B)')
    print(f'  - sample rows: {len(sample_rows)}')
    print(f'  - DataTypeIDs in template fields: {sorted(used.keys())}')


if __name__ == '__main__':
    main()
