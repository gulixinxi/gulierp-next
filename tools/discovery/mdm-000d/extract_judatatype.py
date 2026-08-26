import json
import os
import sys
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

ROOT = r'D:\guli\gulierp\docs\reverse-engineering\dev-meta'

def load_gbk_json(path):
    raw = open(path, 'rb').read()
    text = raw.decode('gbk', errors='replace')
    return json.loads(text)

def safe_load(path):
    try:
        return load_gbk_json(path), None
    except Exception as e:
        return None, str(e)[:80]

# ju-meta.json: full list of 151 JU tables
meta = load_gbk_json(os.path.join(ROOT, 'ju-meta.json'))
print(f"ju-meta.json: {type(meta).__name__}, entries={len(meta) if isinstance(meta, list) else 'dict'}")
if isinstance(meta, list):
    for m in meta[:20]:
        print(' ', m)
else:
    for k in list(meta.keys())[:20]:
        print(' ', k)
