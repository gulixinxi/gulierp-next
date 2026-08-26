namespace GuliERP.G3R2B.IdentityProvisioner;

public sealed class ProvisionerSummary
{
    public long TenantId { get; set; }
    public long CompanyId { get; set; }
    public bool AllSucceeded { get; set; }
    public List<UserProvisionRecord> Users { get; set; } = new();
}

public sealed class UserProvisionRecord
{
    public string UserName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public int ExpectedPermissionCount { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }

    public bool UserCreated { get; set; }
    public bool UserExisted { get; set; }
    public bool PasswordReset { get; set; }

    public bool RoleCreated { get; set; }
    public bool RoleExisted { get; set; }
    public bool RoleWasCreated { get; set; }
    public int RoleClaimsAdded { get; set; }
    public int RoleClaimCountAfter { get; set; }
    public List<string>? ExtraClaimsKept { get; set; }

    public bool CompanyMembershipCreated { get; set; }
    public bool CompanyMembershipExisted { get; set; }
    public bool CompanyMembershipReactivated { get; set; }
    public bool CompanyMembershipSetDefault { get; set; }

    public List<long>? ExtraRoleAssignmentsRemoved { get; set; }
    public bool AssignmentCreated { get; set; }
    public bool AssignmentAlreadyExisted { get; set; }
}
