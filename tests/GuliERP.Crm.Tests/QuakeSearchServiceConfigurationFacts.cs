using System.Net;
using GuliERP.Crm.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Crm.Tests;

public sealed class QuakeSearchServiceConfigurationFacts
{
    [Fact]
    public async Task MissingKey_DoesNotCallQuake()
    {
        var handler = new StubHandler();
        using var client = new HttpClient(handler);
        var service = new QuakeSearchService(client, new ConfigurationBuilder().Build(),
            NullLogger<QuakeSearchService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SearchAsync("example"));
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ConfiguredKey_IsSentInQuakeHeader()
    {
        var handler = new StubHandler();
        using var client = new HttpClient(handler);
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Quake:ApiKey"] = "test-token" }).Build();
        var service = new QuakeSearchService(client, config, NullLogger<QuakeSearchService>.Instance);

        var results = await service.SearchAsync("example");

        Assert.Empty(results);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("test-token", handler.Token);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string? Token { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            Token = request.Headers.GetValues("X-QuakeToken").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":0,\"data\":[]}")
            });
        }
    }
}
