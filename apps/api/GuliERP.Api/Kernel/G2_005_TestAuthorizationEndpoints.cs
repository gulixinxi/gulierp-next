using GuliERP.Identity.Application.Authorization;

namespace GuliERP.Api.Kernel;

public static class G2_005_TestAuthorizationEndpoints
{
    public static void MapG2_005TestAuthorizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/__test/g2-005")
            .WithTags("G2-005-TestOnly-TestingEnvironment");

        group.MapGet("/company-resource/{companyId:long}", async (
                long companyId,
                long tenantId,
                IDataScopeAuthorizationService dataScope,
                CancellationToken ct) =>
            {
                var allowed = await dataScope.CanReadCompanyScopedAsync(
                    tenantId,
                    companyId,
                    DataScopeMode.CurrentCompany,
                    ct);

                return allowed
                    ? Results.Ok(new { ok = true, tenantId, companyId })
                    : Results.NotFound();
            })
            .RequireAuthorization(GuliErpAuthorizationPolicies.G2ProbeRead);
    }
}
