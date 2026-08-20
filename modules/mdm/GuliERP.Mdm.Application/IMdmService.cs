using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Application;

/// <summary>
/// MDM-001 Application service contract. Orchestrates the 3
/// V1 master data entities (UOM, ItemCategory, Item) with:
/// <list type="bullet">
///   <item>Tenant scope enforcement (UOM is system-scoped; Item /
///         ItemCategory are tenant-scoped via IMultiTenant).</item>
///   <item>Code canonicalization (trim + uppercase) and uniqueness.</item>
///   <item>ItemCategory self / cycle detection.</item>
///   <item>Cross-tenant safety on every GetById / Update.</item>
///   <item>Audit fields populated from <c>ICurrentUser.Id</c>.</item>
///   <item>Optimistic concurrency via <c>ConcurrencyVersion</c>.</item>
/// </list>
///
/// <para>
/// All "not found in tenant scope" paths return <c>null</c> (the
/// endpoint maps to 404). Cross-tenant access returns <c>null</c>
/// too — never an error that would leak resource existence.
/// </para>
/// </summary>
public interface IMdmService
{
    // ---------------- UOM (system-scope) ----------------

    Task<PagedResult<UomDto>> ListUomsAsync(
        ListQuery query, CancellationToken ct = default);

    Task<UomDto?> GetUomByIdAsync(long id, CancellationToken ct = default);

    Task<UomDto> CreateUomAsync(
        CreateUomRequest request, CancellationToken ct = default);

    Task<UomDto?> UpdateUomAsync(
        long id, UpdateUomRequest request, CancellationToken ct = default);

    // ---------------- ItemCategory (tenant-scope) ----------------

    Task<PagedResult<ItemCategoryDto>> ListItemCategoriesAsync(
        ListQuery query, long? parentId, CancellationToken ct = default);

    Task<ItemCategoryDto?> GetItemCategoryByIdAsync(
        long id, CancellationToken ct = default);

    Task<ItemCategoryDto> CreateItemCategoryAsync(
        CreateItemCategoryRequest request, CancellationToken ct = default);

    Task<ItemCategoryDto?> UpdateItemCategoryAsync(
        long id, UpdateItemCategoryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Validate that an ItemCategory (identified by <paramref name="id"/>)
    /// exists in the current Tenant. Returns the resolved CategoryId, or
    /// <c>null</c> when the category does not exist or belongs to a
    /// different Tenant. Used by <c>CreateItemAsync</c> /
    /// <c>UpdateItemAsync</c> to enforce Item-to-Category tenant safety.
    /// </summary>
    Task<long?> ResolveItemCategoryInCurrentTenantAsync(
        long id, CancellationToken ct = default);

    // ---------------- Item (tenant-scope) ----------------

    Task<PagedResult<ItemDto>> ListItemsAsync(
        ListQuery query,
        long? categoryId,
        ItemNature? itemNature,
        CancellationToken ct = default);

    Task<ItemDto?> GetItemByIdAsync(long id, CancellationToken ct = default);

    Task<ItemDto> CreateItemAsync(
        CreateItemRequest request, CancellationToken ct = default);

    Task<ItemDto?> UpdateItemAsync(
        long id, UpdateItemRequest request, CancellationToken ct = default);
}
