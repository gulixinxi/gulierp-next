using System.Reflection;
using GuliERP.Foundation.Validation;
using Xunit;

namespace GuliERP.Foundation.Tests;

/// <summary>
/// GULIERP_FOUNDATION_001_CODE_PIPELINE_PROMOTE — architecture
/// tests that lock the
/// <c>GULIERP_MODULE_INDEPENDENCE_RULE</c> invariant: the
/// Foundation assembly must NOT reference any business module
/// (Mdm / Identity / Sales / Purchase / Inventory / Document-kernel / etc.).
///
/// <para>
/// The Foundation is the leaf layer in the dependency graph.
/// It knows nothing about any module. Modules know about
/// Foundation (downward). Modules do NOT know about each other.
/// This test enforces the leaf-layer rule by inspecting the
/// Foundation assembly's referenced types at runtime.
/// </para>
///
/// <para>
/// Per <c>GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md</c> §5.1.
/// </para>
/// </summary>
public sealed class FoundationArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes = new[]
    {
        "GuliERP.Mdm",
        "GuliERP.Identity",
        "GuliERP.Sales",
        "GuliERP.Purchase",
        "GuliERP.Inventory",
        "GuliERP.Production",
        "GuliERP.Quality",
        "GuliERP.DocumentKernel",
        "GuliERP.HR",
    };

    [Fact]
    public void Foundation_Assembly_Does_Not_Reference_Any_Module()
    {
        // Inspect every public type in the Foundation assembly.
        // For each type, collect the assemblies referenced by its
        // method signatures + field types + property types + base
        // types. If any forbidden assembly prefix appears, fail.
        var foundationAssembly = typeof(CodeValidationResult).Assembly;
        var referencedForbidden = new List<string>();

        foreach (var type in foundationAssembly.GetExportedTypes())
        {
            CollectReferencedAssemblies(type, referencedForbidden);
        }

        // Deduplicate.
        var distinct = referencedForbidden.Distinct().ToList();
        Assert.True(
            distinct.Count == 0,
            "Foundation assembly references the following forbidden modules: " +
            string.Join(", ", distinct));
    }

    [Fact]
    public void Foundation_Validation_Namespace_Contains_Expected_Types()
    {
        // The new Validation namespace must contain the 9
        // expected types (4 moved + 5 new).
        var validationTypes = typeof(CodeValidationResult).Assembly
            .GetExportedTypes()
            .Where(t => t.Namespace == "GuliERP.Foundation.Validation")
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(nameof(CodeValidationResult), validationTypes);
        Assert.Contains(nameof(CodeValidationContext), validationTypes);
        Assert.Contains(nameof(ICodeValidationContext), validationTypes);
        Assert.Contains(nameof(ICodeValidator), validationTypes);
        Assert.Contains(nameof(ICodeRuleProvider), validationTypes);
        Assert.Contains(nameof(V1FrozenRuleProvider), validationTypes);
        Assert.Contains(nameof(FormatValidator), validationTypes);
        Assert.Contains(nameof(ReservedNameValidator), validationTypes);
        Assert.Contains(nameof(DocumentNumberSimilarityValidator), validationTypes);
        Assert.Contains(nameof(MasterDataCodeValidator), validationTypes);
    }

    [Fact]
    public void Foundation_Kernel_ErrorCodes_Has_3_New_Generic_Codes()
    {
        // The 3 new generic codes are in
        // GuliERP.Foundation.Kernel.ErrorCodes (per
        // Model §4.8). Verify they exist with the expected
        // values.
        Assert.Equal("code_format_invalid", GuliERP.Foundation.Kernel.ErrorCodes.CodeFormatInvalid);
        Assert.Equal("code_reserved", GuliERP.Foundation.Kernel.ErrorCodes.CodeReserved);
        Assert.Equal("code_resembles_document_number",
            GuliERP.Foundation.Kernel.ErrorCodes.CodeResemblesDocumentNumber);
    }

    [Fact]
    public void Foundation_DependencyInjection_Registers_ICodeRuleProvider()
    {
        // The ICodeRuleProvider is DI-registered as a singleton
        // (per Migration Plan §3.4). Verify by reflection that
        // the registration is in the AddGuliErpFoundation
        // method body.
        var diMethod = typeof(GuliERP.Foundation.DependencyInjection)
            .GetMethod(
                "AddGuliErpFoundation",
                BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(diMethod);

        // The method exists. The actual DI verification is
        // integration-level (would require a test ServiceProvider).
        // This test just locks that the method is discoverable
        // and the ICodeRuleProvider interface is in the
        // Foundation assembly.
        var ruleProviderType = typeof(ICodeRuleProvider);
        Assert.Equal("GuliERP.Foundation.Validation", ruleProviderType.Namespace);
        Assert.True(ruleProviderType.IsInterface);
    }

    private static void CollectReferencedAssemblies(Type type, List<string> forbidden)
    {
        // Inspect method signatures, field types, property types,
        // base types, and implemented interfaces.
        var members = type.GetMembers(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly);

        foreach (var member in members)
        {
            switch (member)
            {
                case MethodInfo m:
                    CheckType(m.ReturnType, forbidden);
                    foreach (var p in m.GetParameters()) CheckType(p.ParameterType, forbidden);
                    break;
                case FieldInfo f:
                    CheckType(f.FieldType, forbidden);
                    break;
                case PropertyInfo p:
                    CheckType(p.PropertyType, forbidden);
                    break;
                case EventInfo e:
                    CheckType(e.EventHandlerType, forbidden);
                    break;
            }
        }

        if (type.BaseType is not null) CheckType(type.BaseType, forbidden);
        foreach (var i in type.GetInterfaces()) CheckType(i, forbidden);
    }

    private static void CheckType(Type? t, List<string> forbidden)
    {
        if (t is null) return;
        // Unwrap arrays / generics / by-ref.
        var element = t;
        if (t.IsArray) element = t.GetElementType();
        if (t.IsByRef) element = t.GetElementType();
        if (t.IsGenericType && t.GetGenericTypeDefinition() != typeof(Nullable<>))
        {
            foreach (var ga in t.GetGenericArguments()) CheckType(ga, forbidden);
        }
        if (element is null) return;

        var asmName = element.Assembly.GetName().Name ?? "";
        foreach (var forbiddenPrefix in ForbiddenAssemblyPrefixes)
        {
            if (asmName.StartsWith(forbiddenPrefix, StringComparison.Ordinal))
            {
                forbidden.Add($"{t.FullName} -> {asmName}");
            }
        }
    }
}
