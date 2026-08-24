# G2_005_STDIN_HANG_ROOT_CAUSE_POSTMORTEM

## 1. Phenomenon

Full G2-005 Harness hung in `GuliERP.Identity.Bootstrap.Tests`.

Observed behavior:

- Step1 Build PASS.
- Step2 Foundation migration PASS.
- Step3 Identity migration PASS.
- Step4 entered `Bootstrap.Tests`.
- `dotnet test` reached test discovery.
- TRX did not land.
- The process waited until the harness timeout.

Focused Bootstrap runs passed independently, so the first visible symptom was misleading: the test project itself was not globally broken.

## 2. Investigation Path

The investigation intentionally narrowed one variable at a time:

1. `ProcessStartInfo` / stdout wrapper.
2. Child environment contract.
3. MSBuild build-server state.
4. `Tee-Object` migration pipeline.
5. Migration child-process residue.
6. PowerShell lifecycle.
7. Migration database state.
8. `blame-hang` / sequence XML to locate the exact testcase.

The first seven checks ruled out common harness-level causes. The final diagnostic step was decisive: `blame-hang` / sequence XML showed the run had entered a specific Bootstrap diagnostic testcase rather than hanging before testcase execution.

## 3. Final Root Cause

`tools/GuliERP.Identity.Bootstrap/Program.cs` used `Console.In.ReadToEndAsync()` directly.

In the full harness, `dotnet test` inherited a non-EOF stdin handle. When the Bootstrap diagnostic test entered the CLI path, `Console.In.ReadToEndAsync()` waited indefinitely for EOF. That left the testhost unable to exit and prevented TRX creation.

Root cause:

`Console.In.ReadToEndAsync()` + inherited non-EOF stdin in full harness = testhost hang.

## 4. Fix Summary

The CLI stdin contract was changed:

- Read stdin only when stdin is redirected.
- Read stdin when tests explicitly provide a `StringReader`.
- Treat inherited interactive stdin as empty optional input.
- Preserve Bootstrap safety guards.
- Do not weaken authentication, database, or operator evidence checks.

This keeps normal CLI redirected-input scenarios working while preventing non-interactive testhost runs from waiting forever on inherited stdin.

## 5. Verification Results

Verified after the fix:

- `GuliERP.Identity.Bootstrap.Tests`: 64/64 PASS.
- Full G2-005 Harness Step4: 377/377 PASS.
- Step5 Runtime authorization wiring: PASS.
- Step6 Production boundary: PASS.
- Operator evidence: ALL CHECKS PASS.

Final local commits:

- `269bbc0 fix(bootstrap): avoid inherited stdin hang in operator harness`
- `a1c4cdf docs(verification): close G2-005 operator evidence`

## 6. Lessons Learned

- Do not over-read "test discovery hang" as proof that the harness wrapper is wrong.
- Harness hangs are not always harness bugs.
- CLI tests that touch `Console.In` are high-risk in `dotnet test`, especially under parent harnesses.
- When TRX does not land and discovery completed, move quickly to `blame-hang` / sequence XML to identify the last testcase.
- Temporary diagnostic parameters and probes must be removed before closure.
- Evidence closure must distinguish focused pass, full harness pass, and operator runtime pass.

## 7. Scope Boundary

This postmortem is documentation only.

- No production code changed by this document.
- No tests changed by this document.
- No database schema changed.
- No migrations changed.
- No new G2-005 work started.
