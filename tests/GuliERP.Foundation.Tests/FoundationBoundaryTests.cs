using GuliERP.Foundation;
using Xunit;

namespace GuliERP.Foundation.Tests;

public sealed class FoundationBoundaryTests
{
    [Fact]
    public void PhaseOneBoundaryNamesCoreFoundationEntities()
    {
        var boundary = new FoundationBoundary();

        Assert.Contains("User", boundary.PhaseOneEntities);
        Assert.Contains("Tenant", boundary.PhaseOneEntities);
        Assert.Contains("Dictionary", boundary.PhaseOneEntities);
        Assert.DoesNotContain("SalesOrder", boundary.PhaseOneEntities);
    }

    [Fact]
    public void ReservedBoundaryNamesFutureGovernanceCapabilities()
    {
        var boundary = new FoundationBoundary();

        Assert.Contains("Workflow", boundary.ReservedEntities);
        Assert.Contains("FieldPolicy", boundary.ReservedEntities);
    }
}

