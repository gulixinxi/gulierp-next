using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GuliERP.Api.Kernel;

/// <summary>
/// G2-002R1: config-gated test-only endpoints. The host wires these
/// routes ONLY when the operator (or the WebApplicationFactory test
/// host) sets <c>GuliERP:TestEndpoints:Enable=true</c>. In the default
/// Production configuration the flag is false, so the routes do not
/// exist.
///
/// <para>
/// The two endpoints are designed to drive the real ASP.NET Core
/// pipeline so the integration tests can assert the full
/// <c>application/problem+json</c> contract end-to-end:
/// </para>
/// <list type="bullet">
///   <item><c>POST /__test/validation</c> — runs through the standard
///         <c>Results.ValidationProblem</c> path, producing a
///         RFC 7807 / 9457 <c>ValidationProblemDetails</c> with
///         <c>errors</c>. The <c>code</c> / <c>requestId</c> /
///         <c>traceId</c> GuliERP extensions are attached by a small
///         <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>
///         post-processor inside the endpoint.</item>
///   <item><c>GET /__test/throw</c> — throws an unhandled
///         <see cref="InvalidOperationException"/>, which is caught by
///         <see cref="FoundationExceptionHandler"/> and emitted as
///         500 + <c>code=internal_error</c>. Used to verify the
///         no-stack-frame / no-secret-leak contract.</item>
/// </list>
///
/// <para>
/// These endpoints are NOT part of the G2-002 public surface; they
/// exist only to give the verification tests a way to drive the
/// pipeline without polluting the Production URL space. Brief §15 #8
/// explicitly forbids <c>/throw</c> + <c>/test-error</c> +
/// <c>/debug-exception</c> in production code, and config-gating is
/// how we honor that constraint while still meeting brief §15 #9
/// (the validation 400 test) and the 500-boundary test.
/// </para>
/// </summary>
public static class TestEndpoints
{
    /// <summary>
    /// DTO for <c>POST /__test/validation</c>. Data-annotation
    /// attributes are used to document the contract; the endpoint
    /// itself does a manual check so it can produce a
    /// <c>Results.ValidationProblem(...)</c> response that
    /// ASP.NET Core's automatic model-validation path would not
    /// produce from this minimal API surface alone.
    /// </summary>
    public sealed class TestValidationRequest
    {
        /// <summary>Free-text name; must be non-empty.</summary>
        [Required]
        public string? Name { get; set; }

        /// <summary>Age in years; must be between 0 and 150.</summary>
        [Range(0, 150)]
        public int Age { get; set; }
    }
}
