using System.Text.Json.Serialization;

namespace GuliERP.G3R1C.IdentityProvisioner;

internal sealed class ProvisionerSummary
{
    [JsonPropertyName("tenantId")]
    public long TenantId { get; set; }

    [JsonPropertyName("companyId")]
    public long CompanyId { get; set; }

    [JsonPropertyName("allSucceeded")]
    public bool AllSucceeded { get; set; }

    [JsonPropertyName("users")]
    public List<UserProvisionRecord> Users { get; set; } = new();
}

internal sealed class UserProvisionRecord
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("roleCode")]
    public string RoleCode { get; set; } = string.Empty;

    [JsonPropertyName("expectedPermissionCount")]
    public int ExpectedPermissionCount { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("userCreated")]
    public bool UserCreated { get; set; }

    [JsonPropertyName("userExisted")]
    public bool UserExisted { get; set; }

    [JsonPropertyName("passwordReset")]
    public bool PasswordReset { get; set; }

    [JsonPropertyName("roleCreated")]
    public bool RoleCreated { get; set; }

    [JsonPropertyName("roleExisted")]
    public bool RoleExisted { get; set; }

    [JsonPropertyName("roleWasCreated")]
    public bool RoleWasCreated { get; set; }

    [JsonPropertyName("roleClaimsAdded")]
    public int RoleClaimsAdded { get; set; }

    [JsonPropertyName("roleClaimCountAfter")]
    public int RoleClaimCountAfter { get; set; }

    [JsonPropertyName("assignmentCreated")]
    public bool AssignmentCreated { get; set; }

    [JsonPropertyName("assignmentAlreadyExisted")]
    public bool AssignmentAlreadyExisted { get; set; }

    [JsonPropertyName("extraRoleAssignmentsRemoved")]
    public List<long>? ExtraRoleAssignmentsRemoved { get; set; }

    [JsonPropertyName("extraClaimsKept")]
    public List<string>? ExtraClaimsKept { get; set; }

    [JsonPropertyName("companyMembershipCreated")]
    public bool CompanyMembershipCreated { get; set; }

    [JsonPropertyName("companyMembershipExisted")]
    public bool CompanyMembershipExisted { get; set; }

    [JsonPropertyName("companyMembershipReactivated")]
    public bool CompanyMembershipReactivated { get; set; }

    [JsonPropertyName("companyMembershipSetDefault")]
    public bool CompanyMembershipSetDefault { get; set; }
}
