namespace GuliERP.Purchase.Application;

public static class PurchasePolicies
{
    public const string Prefix = "GuliERP.Permission:";
    public const string PurchaseOrderRead = Prefix + PurchasePermissions.PurchaseOrderRead;
    public const string PurchaseOrderManage = Prefix + PurchasePermissions.PurchaseOrderManage;
}
