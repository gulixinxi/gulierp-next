namespace GuliERP.Sales.Application;

public static class SalesPolicies
{
    public const string Prefix = "GuliERP.Permission:";
    public const string SalesOrderRead = Prefix + SalesPermissions.SalesOrderRead;
    public const string SalesOrderManage = Prefix + SalesPermissions.SalesOrderManage;
}
