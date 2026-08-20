using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Entities;
using GuliERP.DocumentKernel.Domain.Enums;
using Xunit;

namespace GuliERP.DocumentKernel.Tests;

/// <summary>
/// V1 contract lock tests. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §10
/// (manual override FORBIDDEN) and §22 (DocumentNo immutable after
/// generation). These tests do NOT call into PostgreSQL; they verify
/// the static V1 contract by reflecting on the public surface.
/// </summary>
public sealed class DocumentNumberV1ContractTests
{
    // -------------------------------------------------------------
    // §10 — Manual override is FORBIDDEN
    // -------------------------------------------------------------
    [Fact]
    public void IDocumentNumberService_Has_Only_GenerateAsync_No_SetDocumentNo_Method()
    {
        var publicMethods = typeof(IDocumentNumberService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToHashSet();

        // The ONLY public method is GenerateAsync. There must NOT
        // be a SetDocumentNo / UpdateDocumentNo / OverrideDocumentNo
        // method on the interface. If a future change adds one,
        // this test fails and forces a re-evaluation of §10.
        Assert.Single(publicMethods);
        Assert.Contains("GenerateAsync", publicMethods);

        foreach (var forbidden in new[]
        {
            "SetDocumentNo",
            "UpdateDocumentNo",
            "OverrideDocumentNo",
            "AssignDocumentNo",
            "ReassignDocumentNo",
            "ForceDocumentNo",
        })
        {
            Assert.DoesNotContain(forbidden, publicMethods);
        }
    }

    [Fact]
    public void DocumentNumberCounter_Has_No_Mutator_Methods()
    {
        // The V1 contract is that no business command exposes a
        // "Set DocumentNo" / "Override DocumentNo" method. The
        // entity is a POCO whose only declared methods are the
        // auto-property getters/setters (EF materialization). Any
        // method whose name starts with "Set" / "Update" /
        // "Override" / "Force" / "Assign" — and is NOT a property
        // setter (i.e. has more than 1 parameter) — is a contract
        // violation.
        var type = typeof(DocumentNumberCounter);
        var publicInstanceMethods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length >= 1)   // property setters take exactly 1
            .Select(m => m.Name)
            .ToList();
        // EF auto-property setters take exactly 1 parameter and
        // are named set_<PropertyName>. We assert there are NO
        // multi-parameter mutator methods (which would indicate a
        // business logic method, not a property setter).
        var multiParam = publicInstanceMethods
            .Where(n => n.StartsWith("set_") == false)   // exclude property setters
            .ToList();
        Assert.Empty(multiParam);
    }

    [Fact]
    public void DocumentType_Enum_Has_Exactly_8_Frozen_Values()
    {
        // Per §3 of the V1 numbering spec. The 8 DocumentType values
        // are FROZEN. Any change here is a new Goal.
        var values = System.Enum.GetValues<DocumentType>();
        Assert.Equal(8, values.Length);
        var expected = new[]
        {
            DocumentType.SalesOrder,
            DocumentType.PurchaseOrder,
            DocumentType.GoodsReceipt,
            DocumentType.Shipment,
            DocumentType.GoodsIssue,
            DocumentType.InventoryTransfer,
            DocumentType.InventoryAdjustment,
            DocumentType.ProductionOrder,
        };
        Assert.Equal(expected.OrderBy(v => (int)v), values.OrderBy(v => (int)v));
    }

    // -------------------------------------------------------------
    // §22 — DocumentNo is immutable after generation
    // -------------------------------------------------------------
    [Fact]
    public void DocumentNumberResult_DocumentNo_Is_InitOnly()
    {
        // DocumentNumberResult is a record. The DocumentNo property
        // must be init-only (not freely settable). A record with
        // positional params generates init-only accessors; the CIL
        // still has a set_<Property> method, but it carries the
        // IsExternalInit modreq that constrains it to object
        // initializers.
        var prop = typeof(DocumentNumberResult)
            .GetProperty(nameof(DocumentNumberResult.DocumentNo))!;
        var setter = prop.GetSetMethod(nonPublic: false);
        Assert.NotNull(setter);
        var modreqs = setter!.ReturnParameter.GetRequiredCustomModifiers();
        Assert.Contains(modreqs, m => m.FullName == typeof(IsExternalInit).FullName);
    }

    [Fact]
    public void DocumentNumberRequest_All_Properties_Are_InitOnly()
    {
        // All 6 properties of DocumentNumberRequest must be init-only.
        var props = typeof(DocumentNumberRequest).GetProperties();
        Assert.Equal(6, props.Length);
        foreach (var p in props)
        {
            var setter = p.GetSetMethod(nonPublic: false);
            Assert.NotNull(setter);
            var modreqs = setter!.ReturnParameter.GetRequiredCustomModifiers();
            Assert.Contains(modreqs, m => m.FullName == typeof(IsExternalInit).FullName);
        }
    }

    [Fact]
    public void DocumentNumberCounter_And_DocumentNumberIdempotency_Are_Sealed()
    {
        // The 2 entity types are sealed — no inheritance, no
        // domain-driven override. Mirrors the MDM-001 frozen
        // contract for entity sealing.
        Assert.True(typeof(DocumentNumberCounter).IsSealed);
        Assert.True(typeof(DocumentNumberIdempotency).IsSealed);
    }
}
