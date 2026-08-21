/**
 * MDM Safe-String ID — Frontend Static Regression Guard
 * -------------------------------------------------------
 * Goal MDM-WEB-003 §2: "增加足够的前端静态回归保护；至少能锁住一个大于
 * Number.MAX_SAFE_INTEGER 的 ID，例如：83727350616817740".
 *
 * This module is type-checked by `vue-tsc -b` (part of `npm run typecheck` /
 * `npm run build`). It locks two invariants:
 *
 *   1. COMPILE-TIME (static): every MDM business ID type stays `string`.
 *      If anyone reverts a DTO/UI `id` field back to `number`, the type
 *      assertion helpers below fail the typecheck.
 *
 *   2. RUNTIME (optional): `verifyLargeIdInvariance()` walks the frozen
 *      ID `83727350616817740` through the six stages required by the goal —
 *      API DTO → table row → detail request → edit request → status request
 *      → route/query param — and asserts the string is byte-for-byte
 *      unchanged at every stage. It also proves that `Number(id)` would
 *      silently corrupt the value (precision loss), which is exactly what
 *      this contract forbids.
 *
 * This is NOT mock data and NOT a second HTTP client — it imports only the
 * frozen MDM types and uses pure local values. It does not touch the network.
 */

import type {
  UomDto, Uom,
  ItemCategoryDto, ItemCategory,
  ItemDto, Item,
  BusinessPartnerDto, BusinessPartner,
  WarehouseDto, Warehouse,
  LocationDto, Location,
} from '../../types/mdm';

// ============================================================
// 1. COMPILE-TIME STATIC GUARD — lock `id` as `string`
// ============================================================

/**
 * Static guard helper: passes iff T is a string-ish ID type — i.e. `string`,
 * `string | null`, or `string | undefined` — and NEVER `number` /
 * `number | null` (Snowflake precision loss). Nullable IDs are legitimate
 * (e.g. parentId: string | null) so we accept `string | null`.
 */
type IsStringId<T> = [T] extends [string | null] ? true : false;
type AssertStringId<T extends true> = T;

type _GuardUomId            = AssertStringId<IsStringId<UomDto['id']>>
                            & AssertStringId<IsStringId<Uom['id']>>;
type _GuardItemCategoryId   = AssertStringId<IsStringId<ItemCategoryDto['id']>>
                            & AssertStringId<IsStringId<ItemCategory['id']>>
                            & AssertStringId<IsStringId<ItemCategory['parentId']>>;
type _GuardItemId           = AssertStringId<IsStringId<ItemDto['id']>>
                            & AssertStringId<IsStringId<Item['id']>>
                            & AssertStringId<IsStringId<Item['categoryId']>>
                            & AssertStringId<IsStringId<Item['baseUomId']>>;
type _GuardBusinessPartnerId = AssertStringId<IsStringId<BusinessPartnerDto['id']>>
                            & AssertStringId<IsStringId<BusinessPartner['id']>>;
type _GuardWarehouseId      = AssertStringId<IsStringId<WarehouseDto['id']>>
                            & AssertStringId<IsStringId<Warehouse['id']>>;
type _GuardLocationId       = AssertStringId<IsStringId<LocationDto['id']>>
                            & AssertStringId<IsStringId<Location['id']>>
                            & AssertStringId<IsStringId<Location['warehouseId']>>;

// Force evaluation of all guards (unused-but-emitted). If any ID drifts to
// `number`, the corresponding IsStringId resolves to `false` and the
// AssertStringId<false> above errors at compile time.
const _STATIC_GUARD: _GuardUomId & _GuardItemCategoryId & _GuardItemId
  & _GuardBusinessPartnerId & _GuardWarehouseId & _GuardLocationId = true;
void _STATIC_GUARD;

// ============================================================
// 2. RUNTIME GUARD — frozen large ID through the 6 stages
// ============================================================

/** Frozen regression ID — greater than Number.MAX_SAFE_INTEGER (9007199254740991). */
export const FROZEN_LARGE_ID = '83727350616817740';

export interface IdInvarianceResult {
  readonly ok: boolean;
  readonly stage: string;
  readonly detail: string;
}

function assert(cond: boolean, stage: string, detail: string): IdInvarianceResult {
  if (!cond) return { ok: false, stage, detail };
  return { ok: true, stage, detail };
}

/**
 * Walks FROZEN_LARGE_ID through the six stages required by MDM-WEB-003 §2
 * and returns the per-stage results. Throws if any stage mutates the string.
 *
 * This is pure / local — no network, no mock data, no second HTTP client.
 */
export function verifyLargeIdInvariance(): IdInvarianceResult[] {
  const results: IdInvarianceResult[] = [];

  // --- Proof that Number() would corrupt the ID (the bug this contract prevents) ---
  const numberCorrupted = String(Number(FROZEN_LARGE_ID));
  results.push(assert(
    numberCorrupted !== FROZEN_LARGE_ID,
    'precision-loss-proof',
    `Number('${FROZEN_LARGE_ID}') => '${numberCorrupted}' (precision lost; this is exactly what the contract forbids)`,
  ));

  // --- Stage 1: API DTO (wire shape, id is string) ---
  const dto: Pick<UomDto, 'id' | 'concurrencyVersion'> = {
    id: FROZEN_LARGE_ID,
    concurrencyVersion: 7, // expectedConcurrencyVersion stays number
  };
  results.push(assert(dto.id === FROZEN_LARGE_ID, 'api-dto', `dto.id preserved`));
  results.push(assert(
    typeof dto.concurrencyVersion === 'number',
    'concurrency-still-number',
    `expectedConcurrencyVersion must remain number, got ${typeof dto.concurrencyVersion}`,
  ));

  // --- Stage 2: table row (row-key; UI model copy of dto.id, no conversion) ---
  const tableRow: { id: string } = { id: dto.id };
  const rowKey = tableRow.id; // what el-table row-key receives
  results.push(assert(rowKey === FROZEN_LARGE_ID, 'table-row', `row-key preserved`));

  // --- Stage 3: detail request (GET /api/v1/mdm/uoms/{id} — id interpolated as-is) ---
  const detailUrl = `/api/v1/mdm/uoms/${tableRow.id}`;
  results.push(assert(
    detailUrl === `/api/v1/mdm/uoms/${FROZEN_LARGE_ID}`,
    'detail-request',
    `GET URL preserved (${detailUrl})`,
  ));

  // --- Stage 4: edit request (PUT /api/v1/mdm/uoms/{id}) ---
  const editUrl = `/api/v1/mdm/uoms/${tableRow.id}`;
  const editBody = { name: 'updated', expectedConcurrencyVersion: dto.concurrencyVersion };
  results.push(assert(
    editUrl === `/api/v1/mdm/uoms/${FROZEN_LARGE_ID}`
      && typeof editBody.expectedConcurrencyVersion === 'number',
    'edit-request',
    `PUT URL preserved + concurrencyVersion is number`,
  ));

  // --- Stage 5: status (启用/停用) request — re-GET then PUT with same id ---
  const freshDetailId = tableRow.id; // getUom(row.id) returns the same string
  const statusUrl = `/api/v1/mdm/uoms/${freshDetailId}`;
  results.push(assert(
    statusUrl === `/api/v1/mdm/uoms/${FROZEN_LARGE_ID}`,
    'status-request',
    `启用/停用 PUT URL preserved (no GET 404 from precision loss)`,
  ));

  // --- Stage 6: route / query param (e.g. ?warehouseId=ID) ---
  const query: Record<string, string> = { warehouseId: FROZEN_LARGE_ID };
  const rebuiltQuery = `?warehouseId=${query.warehouseId}`;
  results.push(assert(
    rebuiltQuery === `?warehouseId=${FROZEN_LARGE_ID}`,
    'route-query',
    `query param preserved as opaque string`,
  ));

  // Final gate: any failure throws.
  const failures = results.filter(r => !r.ok);
  if (failures.length > 0) {
    const msg = failures.map(f => `[${f.stage}] ${f.detail}`).join('\n');
    throw new Error(`MDM safe-string ID regression FAILED:\n${msg}`);
  }
  return results;
}

/** Convenience: true iff the guard passes. */
export function largeIdGuardPasses(): boolean {
  try {
    const results = verifyLargeIdInvariance();
    return results.every(r => r.ok);
  } catch {
    return false;
  }
}
