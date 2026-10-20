namespace GuliERP.Crm.Application;

public static class CrmPermissions
{
    // 线索池
    public const string LeadRead = "crm.lead.read";
    public const string LeadManage = "crm.lead.manage";
    public const string LeadClaim = "crm.lead.claim";
    public const string LeadConvert = "crm.lead.convert";
    public const string LeadImport = "crm.lead.import";
    public const string LeadExport = "crm.lead.export";

    // 企业/客户主档
    public const string CompanyRead = "crm.company.read";
    public const string CompanyManage = "crm.company.manage";
    public const string CompanyAssign = "crm.company.assign";
    public const string CompanyExport = "crm.company.export";
    public const string CompanyImport = "crm.company.import";

    // 联系人
    public const string ContactRead = "crm.contact.read";
    public const string ContactManage = "crm.contact.manage";

    // 商机
    public const string DealRead = "crm.deal.read";
    public const string DealManage = "crm.deal.manage";

    // 跟进记录
    public const string ActivityRead = "crm.activity.read";
    public const string ActivityManage = "crm.activity.manage";

    // 导入导出日志
    public const string LogRead = "crm.log.read";
}
