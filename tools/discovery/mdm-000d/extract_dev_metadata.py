"""
MDM-000D DEV Metadata Extractor (READ-ONLY).

Source: D:\guli\gulierp\docs\reverse-engineering\dev-meta\
Database: dev (SQL Server 2012 SP1, Onlyit-derived per reverse-engineering report)
Extraction tool already produced JSON dumps (read-only here).

This script:
  1. Reads every JSON dump under dev-meta/
  2. Decodes GBK-encoded JSON keys (PowerShell/cmd garbled display only — file content
     is UTF-8 with GBK key bytes — that is, the original tool wrote CJK key names as
     raw GBK bytes inside JSON property names; we re-decode them).
  3. Emits normalized UTF-8 JSON in tools/discovery/mdm-000d/_normalized/ for downstream
     comparison, mapping, and seed-candidate generation.
  4. NEVER mutates source. Source directory is read-only by policy (AGENT_WORK_RULES.md).

Usage:
    python tools/discovery/mdm-000d/extract_dev_metadata.py
"""
import json
import os
import sys
import io
import shutil

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

SRC_ROOT = r'D:\guli\gulierp\docs\reverse-engineering\dev-meta'
OUT_ROOT = r'D:\guli\projects\gulierp-next\tools\discovery\mdm-000d\_normalized'


def _try_decode_key(k):
    """Smartly decode a JSON key that may be UTF-8 (correct) or GBK (mis-decoded).

    The dev extraction tool produced mixed encoding: some keys are properly UTF-8
    (e.g. 名称 = e5 90 8d e7 a7 b0), others are raw GBK bytes that the JSON loader
    treated as latin-1 (e.g. 字典名 = d7 d6 b5 e4 c3 fb).

    Strategy: try strict GBK decode; if it produces printable CJK text, use it;
    otherwise keep the original (UTF-8).
    """
    if not isinstance(k, str) or not any(ord(c) > 127 for c in k):
        return k
    # Re-encode str to bytes via latin-1 (1:1 char-to-byte mapping for 0x00-0xFF)
    try:
        raw_bytes = k.encode('latin-1')
        # Try GBK strict first
        decoded = raw_bytes.decode('gbk', errors='strict')
        # Validate: must contain CJK characters (no replacement chars)
        if '\ufffd' not in decoded and any(0x4E00 <= ord(c) <= 0x9FFF for c in decoded):
            return decoded
    except Exception:
        pass
    # Already UTF-8
    return k


def _fix_keys(obj):
    if isinstance(obj, dict):
        new = {}
        for k, v in obj.items():
            new[_try_decode_key(k)] = _fix_keys(v)
        return new
    if isinstance(obj, list):
        return [_fix_keys(x) for x in obj]
    return obj


def load_json(path):
    raw = open(path, 'rb').read()
    try:
        text = raw.decode('utf-8')
    except UnicodeDecodeError:
        text = raw.decode('gbk', errors='replace')
    return _fix_keys(json.loads(text))


def safe_load(path):
    try:
        return load_json(path), None
    except Exception as e:
        return None, f'{type(e).__name__}: {str(e)[:120]}'


def main():
    if os.path.exists(OUT_ROOT):
        shutil.rmtree(OUT_ROOT)
    os.makedirs(OUT_ROOT, exist_ok=True)
    print(f'Output: {OUT_ROOT}')

    # Top-level metadata files
    top_files = [
        'size.json', 'tables.json', 'primary-keys.json',
        'ju-meta.json', 'biz-tables-summary.json',
        'dictionaries.json',
        'menus.json', 'templates.json', 'template-fields.json',
        'relations.json', 'formulas.json', 'actions.json',
        'permissions.json', 'organizations.json', 'workflows.json',
        'all-views.json', 'all-procs.json', 'all-functions.json',
        'workflow-content.json',
    ]
    for fn in top_files:
        p = os.path.join(SRC_ROOT, fn)
        if not os.path.exists(p):
            print(f'  [skip] {fn} (not found)')
            continue
        data, err = safe_load(p)
        if err:
            print(f'  [fail] {fn}: {err}')
            # Still copy raw for inspection
            with open(os.path.join(OUT_ROOT, fn + '.raw'), 'wb') as f:
                f.write(open(p, 'rb').read())
            continue
        out_path = os.path.join(OUT_ROOT, fn)
        with open(out_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        size = os.path.getsize(out_path)
        kind = type(data).__name__
        if isinstance(data, list):
            print(f'  [ok]  {fn} ({kind}, {len(data)} rows, {size:,} B)')
        else:
            print(f'  [ok]  {fn} ({kind}, {size:,} B)')

    # ju-columns.json is huge (685 KB) - load but write compact
    p = os.path.join(SRC_ROOT, 'ju-columns.json')
    if os.path.exists(p):
        data, err = safe_load(p)
        if err:
            print(f'  [fail] ju-columns.json: {err}')
        else:
            # Write compact JSON to keep size manageable
            out_path = os.path.join(OUT_ROOT, 'ju-columns.compact.json')
            with open(out_path, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False)
            print(f'  [ok]  ju-columns.json ({len(data)} tables, {os.path.getsize(out_path):,} B)')

    # biz-tables.json (570 KB)
    p = os.path.join(SRC_ROOT, 'biz-tables.json')
    if os.path.exists(p):
        data, err = safe_load(p)
        if err:
            print(f'  [fail] biz-tables.json: {err}')
        else:
            out_path = os.path.join(OUT_ROOT, 'biz-tables.compact.json')
            with open(out_path, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False)
            print(f'  [ok]  biz-tables.json ({len(data)} tables, {os.path.getsize(out_path):,} B)')

    # Sample directories
    for sub in ['ju-samples', 'biz-samples']:
        sub_in = os.path.join(SRC_ROOT, sub)
        sub_out = os.path.join(OUT_ROOT, sub)
        if not os.path.isdir(sub_in):
            continue
        os.makedirs(sub_out, exist_ok=True)
        # Use os.scandir to handle GBK filenames
        count_ok = 0
        count_fail = 0
        for entry in os.scandir(sub_in):
            data, err = safe_load(entry.path)
            if err:
                count_fail += 1
                continue
            # Sanitize filename: use fqtn from inside if available
            fqtn = data.get('fqtn', entry.name) if isinstance(data, dict) else entry.name
            safe_name = fqtn.replace('.', '_') + '.json'
            out_path = os.path.join(sub_out, safe_name)
            with open(out_path, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            count_ok += 1
        print(f'  [ok]  {sub}/  {count_ok} files normalized, {count_fail} failed')

    print('Done.')


if __name__ == '__main__':
    main()
