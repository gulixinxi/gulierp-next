# GULIERP_BASELINE_SENSITIVE_SCAN_ADJUDICATION_V1

Date: 2026-09-10
Scope: tracked plus nonignored untracked text files scanned by tools/dev/baseline-sensitive-scan.py.

Gate: PASS for unresolved literal candidates. This is not a historical Git scan and does not inspect ignored local logs or binaries.

## Summary

| Metric | Value |
|---|---:|
| filesScanned | 1105 |
| keywordFiles | 449 |
| unresolved candidateCount | 0 |
| reviewedCount | 319 |

The scanner emits path, line, and classification only. It does not print credential values.

## Reviewed Classification Counts

| Kind | Count |
|---|---:|
| reviewed_documentation_example | 18 |
| reviewed_placeholder_or_code_shape | 283 |
| reviewed_test_fixture | 3 |
| reviewed_tool_help_or_safety_text | 15 |

## Boundary

- reviewed_placeholder_or_code_shape covers placeholders, sentinel fixtures, localhost/bad-db fixtures, env names, password policy code, and variable/STDIN handling shapes.
- reviewed_documentation_example covers documentation examples after value suppression and shape classification.
- reviewed_test_fixture covers integration/test-only fixtures that intentionally exercise redaction or unavailable DB paths.
- reviewed_tool_help_or_safety_text covers comments/help text and operator instructions that state secrets are read from env/stdin or not echoed.
- Ignored local files, generated binaries, and historical Git blobs are out of scope for this local worktree scan.
