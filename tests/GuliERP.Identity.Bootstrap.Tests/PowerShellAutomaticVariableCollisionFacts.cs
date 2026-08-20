using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using Xunit;

namespace GuliERP.Identity.Bootstrap.Tests;

/// <summary>
/// G2-004V1R5 — PowerShell automatic-variable collision static
/// regression guard.
///
/// <para>
/// The G2-004 operator evidence harness has been hit twice
/// by PowerShell automatic-variable collisions:
/// </para>
///
/// <list type="bullet">
///   <item><b>$Host</b> collision in G2-004V1R3
///     (function-scope variable shadowed the read-only
///     automatic variable; runtime error
///     "Cannot overwrite variable Host because it is
///     read-only or constant.").</item>
///   <item><b>$Pid</b> collision in G2-004V1R5
///     (function parameter shadowed the automatic $PID;
///     PowerShell 7 refused the parameter binding).</item>
/// </list>
///
/// <para>
/// The two fixes were:
/// </para>
///
/// <list type="bullet">
///   <item>Rename the local variable / parameter to a
///         semantically clear name that does NOT collide
///         with any PowerShell automatic variable.</item>
///   <item>Add a static AST-based regression guard
///         (this test) that parses both operator scripts
///         and reports any future variable / parameter
///         name that matches an automatic variable.</item>
/// </list>
///
/// <para>
/// The test uses <c>[Parser]::ParseFile</c> (the standard
/// PowerShell AST) and walks:
/// </para>
///
/// <list type="bullet">
///   <item><c>ParameterAst</c> — function parameters</item>
///   <item><c>AssignmentTargetAst</c> — <c>=</c>, <c>+=</c>, etc.</item>
///   <item><c>ForEachStatementAst</c> — <c>foreach ($x in ...)</c></item>
///   <item><c>VariableExpressionAst</c> in <c>CommandAst</c>
///         — argument binding to a -Parameter on a
///         command (e.g. <c>Stop-Process -Id $foo</c> binds
///         <c>-Id</c> to the variable, not the variable
///         itself; we check the LHS of the assignment /
///         the parameter name itself, not the right-hand
///         side).</item>
/// </list>
///
/// <para>
/// The test asserts NO collision against the frozen
/// automatic-variable list (see <c>FrozenAutomaticVariables</c>).
/// On collision, the test fails with the file, line, and
/// variable name so the next maintainer can fix the
/// regression in seconds.
/// </para>
/// </summary>
public class PowerShellAutomaticVariableCollisionFacts
{
    /// <summary>
    /// The frozen list of PowerShell automatic variables
    /// that the harness MUST NOT use as parameter names,
    /// assignment targets, or foreach variables. Any name
    /// in this list (case-insensitive) is forbidden.
    /// </summary>
    public static readonly string[] FrozenAutomaticVariables = new[]
    {
        // Already-defective (real Operator evidence):
        "args",   // V1R3 shadowed in Start-HostProcess
        "Host",   // V1R3 shadowed in Invoke-Round1-HappyPath
        "PID",    // V1R5 shadowed as param in Register/Stop-OwnedHost

        // High-risk PowerShell automatic variables
        // (case-insensitive). Reading these is fine; using
        // them as parameter / assignment / loop variable
        // names is forbidden.
        "Error",
        "HOME",
        "input",
        "LASTEXITCODE",
        "Matches",
        "MyInvocation",
        "PROFILE",
        "PSBoundParameters",
        "PSCommandPath",
        "PSScriptRoot",
        "PSVersionTable",
        "PWD",
        "true",   // PS 7 reserved constants
        "false",  // PS 7 reserved constants
        "null",   // PS 7 reserved constants
    };

    /// <summary>
    /// The two PowerShell scripts the operator harness
    /// uses. Both are operator-side tooling (NOT part of
    /// the runtime / production code). Both MUST be free
    /// of automatic-variable collisions.
    /// </summary>
    private static readonly string[] HarnessScripts = new[]
    {
        "tools/dev/g2-004-operator-evidence.ps1",
        "tools/dev/g2-004-bootstrap-operator-user.ps1",
        "tools/dev/g2-005-operator-evidence.ps1",
    };

    [Fact]
    public void NoAutomaticVariableCollisions_InOperatorHarnessScripts()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);

        var violations = new System.Collections.Generic.List<string>();
        var forbiddenSet = new System.Collections.Generic.HashSet<string>(
            FrozenAutomaticVariables, System.StringComparer.OrdinalIgnoreCase);

        foreach (var relPath in HarnessScripts)
        {
            var fullPath = Path.Combine(repoRoot!, relPath);
            Assert.True(File.Exists(fullPath),
                $"Harness script not found: {fullPath}");

            var tokens = System.Array.Empty<System.Management.Automation.Language.Token>();
            var errors = System.Array.Empty<System.Management.Automation.Language.ParseError>();
            var ast = Parser.ParseFile(fullPath, out tokens, out errors);

            // Surface any parse errors (these are FATAL —
            // the harness would not run at all).
            foreach (var err in errors)
            {
                violations.Add($"[PARSE-ERROR] {relPath}:{err.Extent.StartLineNumber} " +
                    $"{err.Message}");
            }

            // Walk the AST and collect all variable-like
            // binding sites (parameters, assignment LHS,
            // foreach LHS).
            var collector = new CollisionCollector(relPath, forbiddenSet, violations);
            ast.Visit(collector);
        }

        if (violations.Count > 0)
        {
            var msg = "PowerShell automatic-variable collision(s) detected in operator harness:\n  " +
                string.Join("\n  ", violations) + "\n\n" +
                "Rule: NEVER use a PowerShell automatic variable name as a\n" +
                "function parameter, assignment target, or foreach variable.\n" +
                "Rename to a semantically clear name (e.g. $ProcessId, $hostProcess).\n" +
                "Reading an automatic variable on the RHS is fine; this test\n" +
                "ONLY checks LHS / parameter names.";
            Assert.Fail(msg);
        }
    }

    private static string? FindRepoRoot()
    {
        // The test is in tests/GuliERP.Identity.Bootstrap.Tests.
        // Walk up to find the .csproj-containing solution dir.
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrEmpty(dir)) { return null; }
            if (File.Exists(Path.Combine(dir, "GuliERP.slnx"))) { return dir; }
        }
        return null;
    }

    /// <summary>
    /// AST visitor that records any parameter / assignment
    /// LHS / foreach variable whose name matches a
    /// forbidden PowerShell automatic variable (case-
    /// insensitive). RHS reads of automatic variables are
    /// NOT recorded (they are safe and idiomatic).
    /// </summary>
    private sealed class CollisionCollector : AstVisitor
    {
        private readonly string _relPath;
        private readonly System.Collections.Generic.HashSet<string> _forbidden;
        private readonly System.Collections.Generic.List<string> _violations;

        public CollisionCollector(
            string relPath,
            System.Collections.Generic.HashSet<string> forbidden,
            System.Collections.Generic.List<string> violations)
        {
            _relPath = relPath;
            _forbidden = forbidden;
            _violations = violations;
        }

        // Function parameter: e.g. `param([int]$Pid)`.
        public override AstVisitAction VisitParameter(ParameterAst parameterAst)
        {
            CheckName(parameterAst.Name.VariablePath.UserPath,
                parameterAst.Extent.StartLineNumber);
            return AstVisitAction.Continue;
        }

        // Assignment LHS: e.g. `$foo = 1` or `$foo += 1`.
        // The PowerShell AST exposes this as
        // AssignmentStatementAst with a `Left` of type
        // VariableExpressionAst (NOT a separate
        // AssignmentTargetAst class in the modern SDK).
        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            var var = assignmentStatementAst.Left as VariableExpressionAst;
            if (var != null)
            {
                CheckName(var.VariablePath.UserPath,
                    var.Extent.StartLineNumber);
            }
            return AstVisitAction.Continue;
        }

        // foreach ($x in ...) — the $x is the loop variable.
        public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst)
        {
            CheckName(forEachStatementAst.Variable.VariablePath.UserPath,
                forEachStatementAst.Variable.Extent.StartLineNumber);
            return AstVisitAction.Continue;
        }

        private void CheckName(string name, int lineNumber)
        {
            // Strip leading $ if present.
            if (string.IsNullOrEmpty(name)) { return; }
            if (name.StartsWith("$")) { name = name.Substring(1); }
            if (_forbidden.Contains(name))
            {
                _violations.Add(
                    $"[{_relPath}:{lineNumber}] name='$({name})' collides with " +
                    "PowerShell automatic variable");
            }
        }
    }
}
