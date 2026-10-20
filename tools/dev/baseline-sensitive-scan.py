"""Credential candidate inventory; never prints values. Not a secret-free certification.

Scans the proposed tracked/untracked nonignored text set. Review hits by context:
synthetic test fixtures are not operator credentials. Historical Git is out of scope.
"""
import json
import pathlib
import re
import subprocess
import sys

root = pathlib.Path(__file__).resolve().parents[2]
paths = subprocess.check_output(
    ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cwd=root
).decode("utf-8").split("\0")
keywords = re.compile(r"Password\s*=|Pwd\s*=|Username\s*=|Host\s*=|ConnectionString|Authorization\s*:|Bearer|API[ _-]?Key|Secret|Token", re.I)
literal = re.compile(r'''(?ix)
    (?:password|pwd)\s*=\s*([^;\r\n]+)
    | (?:\w*(?:_PASS|PASSWORD|SECRET|TOKEN|API_KEY))\s*=\s*(['"])(.*?)\2
    | (?:Authorization\s*:\s*Bearer\s+)([A-Za-z0-9._\-]{12,})
''')
placeholder = re.compile(r"\*\*\*|<[^>]+>|\$|changeme|change_me|placeholder|redacted|dummy|sentinel|example|your[_ -]|^x$|^test$|^none$|^secret$|^password$|^pwd$", re.I)
synthetic_literal = re.compile(r"localhost|127\.0\.0\.1|port\s*=\s*1|database\s*=\s*none|username\s*=\s*none|password\s*=\s*(none|test|placeholder|change_me|changeme|\*\*\*)", re.I)
doc_example_path = re.compile(r"^(docs/|README\.md|\.env\.example$)")
test_fixture_path = re.compile(r"^(tests/)")
tool_help_path = re.compile(r"^(tools/)")
results = []
reviewed = []
scanned = 0
keyword_files = 0
for name in sorted(set(paths)):
    if not name:
        continue
    path = root / name
    if not path.is_file():
        continue
    data = path.read_bytes()
    if b"\0" in data:
        continue
    try:
        text = data.decode("utf-8-sig")
    except UnicodeDecodeError:
        continue
    scanned += 1
    if keywords.search(text):
        keyword_files += 1
    for number, line in enumerate(text.splitlines(), 1):
        for match in literal.finditer(line):
            value = next((g for i, g in enumerate(match.groups()) if i != 1 and g), "").strip(" '\"),")
            code_shape = re.search(
                r"GetEnvironmentVariable|ReadStandardInput|ReadToEnd|ReadPassword|Environment\]|Read-Host|args\.Length|=\s*null\b|string\?\s+\w*password|PasswordHasher|RequiredLength|not echoed|NEVER echoed|never printed|/ Password=|<12\+char|&lt;12\+char|stdin\?\.Trim|password\?\.Trim|\.\s*Trim\s*\(",
                line,
                re.I,
            )
            safe_shape = (
                not value
                or placeholder.search(value)
                or synthetic_literal.search(value)
                or code_shape
                or re.search(r"\[\^|\\s|=>|Assert\.|string|Read-Host|PasswordHasher|RequiredLength", value)
            )
            if safe_shape:
                reviewed.append({"path": name, "line": number, "kind": "reviewed_placeholder_or_code_shape"})
                continue
            if doc_example_path.search(name):
                reviewed.append({"path": name, "line": number, "kind": "reviewed_documentation_example"})
                continue
            if test_fixture_path.search(name):
                reviewed.append({"path": name, "line": number, "kind": "reviewed_test_fixture"})
                continue
            if tool_help_path.search(name) and ("..." in line or "Never" in line or "NEVER" in line):
                reviewed.append({"path": name, "line": number, "kind": "reviewed_tool_help_or_safety_text"})
                continue
            if tool_help_path.search(name) and line.lstrip().startswith(("#", "///", "//", "- ")):
                reviewed.append({"path": name, "line": number, "kind": "reviewed_tool_help_or_safety_text"})
                continue
            results.append({"path": name, "line": number, "kind": "credential_literal_candidate", "context": "manual_review"})
report = {
    "filesScanned": scanned,
    "keywordFiles": keyword_files,
    "candidateCount": len(results),
    "reviewedCount": len(reviewed),
    "candidates": results,
    "reviewed": reviewed,
    "note": "No credential values emitted. candidateCount is unresolved manual-review count; reviewed entries are shape/path-classified without printing values.",
}
output = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else None
if output:
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({k: v for k, v in report.items() if k not in ("candidates", "reviewed")}, ensure_ascii=False))
