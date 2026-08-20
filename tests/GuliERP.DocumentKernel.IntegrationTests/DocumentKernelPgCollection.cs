using Xunit;

namespace GuliERP.DocumentKernel.IntegrationTests;

/// <summary>
/// xUnit collection definition for DocumentKernel PG integration
/// tests. All integration test classes use
/// <c>[Collection("DocumentKernelPg")]</c> so they share a single
/// <see cref="DocumentKernelConnectionFixture"/> instance (one
/// schema-migrate per test run; not per test class).
/// </summary>
[CollectionDefinition("DocumentKernelPg")]
public sealed class DocumentKernelPgCollection : ICollectionFixture<DocumentKernelConnectionFixture>
{
    // Marker class; the actual collection wiring is via the
    // [CollectionDefinition] attribute above. xUnit instantiates
    // the fixture once per collection and shares it across all
    // test classes that opt into the collection.
}
