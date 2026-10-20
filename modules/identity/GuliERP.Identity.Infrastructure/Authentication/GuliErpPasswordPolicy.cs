using GuliERP.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GuliERP.Identity.Infrastructure.Authentication;

/// <summary>One password rule for ERP, CRM, and mutating operator tools.</summary>
public static class GuliErpPasswordPolicy
{
    public const int MinimumLength = 15;
    public const int MaximumLength = 128;

    public static void Configure(PasswordOptions options)
    {
        options.RequiredLength = MinimumLength;
        options.RequireDigit = false;
        options.RequireLowercase = false;
        options.RequireUppercase = false;
        options.RequireNonAlphanumeric = false;
        options.RequiredUniqueChars = 1;
    }

    public static void AddValidator(IServiceCollection services) =>
        services.AddScoped<IPasswordValidator<GuliErpUser>, CommonPasswordValidator>();
}
