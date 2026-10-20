namespace GuliERP.Crm.Application;

public static class CrmPolicies
{
    public const string LeadRead = "LeadRead";
    public const string LeadManage = "LeadManage";
    public const string LeadClaim = "LeadClaim";
    public const string LeadConvert = "LeadConvert";
    public const string LeadImport = "LeadImport";
    public const string LeadExport = "LeadExport";
    public const string CompanyRead = "CompanyRead";
    public const string CompanyManage = "CompanyManage";
    public const string CompanyAssign = "CompanyAssign";
    public const string CompanyExport = "CompanyExport";
    public const string CompanyImport = "CompanyImport";
    public const string ContactRead = "ContactRead";
    public const string ContactManage = "ContactManage";
    public const string DealRead = "DealRead";
    public const string DealManage = "DealManage";
    public const string ActivityRead = "ActivityRead";
    public const string ActivityManage = "ActivityManage";
    public const string LogRead = "LogRead";

    /// <summary>
    /// ERP客商候选匹配：同时要求 crm.company.read 和 mdm.business-partner.read。
    /// 这是跨模块只读查询，需要两个领域的读取权限。
    /// </summary>
    public const string CompanyErpMatch = "CompanyErpMatch";
}
