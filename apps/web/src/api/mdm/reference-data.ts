/**
 * MDM Reference Data API Client — Country + AdministrativeRegion.
 *
 * GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2 / Wave 4
 * (2026-08-28). The reference data is system-scoped (NOT
 * Tenant-scoped) and read-only from the runtime. The list
 * responses are intentionally small (Country ≤ 300 rows; Region
 * per-Country ≤ ~3 300 rows for CN).
 *
 * Endpoints (cookie auth via http.ts):
 *   GET /api/v1/mdm/reference/countries?keyword=...&includeInactive=...
 *   GET /api/v1/mdm/reference/countries/{code}
 *   GET /api/v1/mdm/reference/regions?countryCode=...&parentId=...&includeInactive=...
 *   GET /api/v1/mdm/reference/regions/{countryCode}/{code}
 *
 * Reuse: this client is intentionally generic so Warehouse /
 * Plant / any future address-bearing entity can pull the same
 * Country / Region data without redeclaring types.
 */
import { apiGet } from '../http';
import type {
  CountryListItem,
  Country,
  CountryOption,
  AdministrativeRegionListItem,
  AdministrativeRegion,
} from '../../types/mdm';

// ---------- Wire → UI converters ----------

/** Server returns Id as a long serialized as a JSON string (Snowflake/HiLo wire contract). */
function toCountry(item: CountryListItem): Country {
  return { ...item };
}

function toRegion(item: AdministrativeRegionListItem): AdministrativeRegion {
  return { ...item };
}

/** Compact option for el-select / el-cascader searchable selectors. */
export function toCountryOption(c: Country | CountryListItem): CountryOption {
  return {
    value: c.code,
    label: `${c.code} ${c.name}`,
    alpha3Code: c.alpha3Code,
    englishName: c.englishName,
    zhName: c.name,
  };
}

// ---------- API calls ----------

export async function listCountries(params?: {
  keyword?: string;
  includeInactive?: boolean;
}): Promise<Country[]> {
  const qp: Record<string, string | number | boolean | undefined> = {};
  if (params?.keyword) qp.keyword = params.keyword;
  if (params?.includeInactive !== undefined) qp.includeInactive = params.includeInactive;
  const result = await apiGet<CountryListItem[]>(
    '/api/v1/mdm/reference/countries',
    { params: qp as Record<string, string | number | boolean | undefined> },
  );
  return result.map(toCountry);
}

export async function getCountryByCode(code: string): Promise<Country | null> {
  if (!code) return null;
  try {
    const item = await apiGet<CountryListItem>(
      `/api/v1/mdm/reference/countries/${encodeURIComponent(code.trim().toUpperCase())}`,
    );
    return toCountry(item);
  } catch (e) {
    // 404 returns null; other errors propagate.
    if (e && typeof e === 'object' && 'status' in e && (e as { status?: number }).status === 404) {
      return null;
    }
    throw e;
  }
}

export async function listRegions(params: {
  countryCode: string;
  parentId?: string | null;
  includeInactive?: boolean;
}): Promise<AdministrativeRegion[]> {
  const qp: Record<string, string | number | boolean | undefined> = {
    countryCode: params.countryCode,
  };
  if (params.parentId != null) qp.parentId = params.parentId;
  if (params.includeInactive !== undefined) qp.includeInactive = params.includeInactive;
  const result = await apiGet<AdministrativeRegionListItem[]>(
    '/api/v1/mdm/reference/regions',
    { params: qp as Record<string, string | number | boolean | undefined> },
  );
  return result.map(toRegion);
}

export async function getRegionByCode(
  countryCode: string,
  code: string,
): Promise<AdministrativeRegion | null> {
  if (!countryCode || !code) return null;
  try {
    const item = await apiGet<AdministrativeRegionListItem>(
      `/api/v1/mdm/reference/regions/${encodeURIComponent(countryCode.trim().toUpperCase())}/${encodeURIComponent(code.trim())}`,
    );
    return toRegion(item);
  } catch (e) {
    if (e && typeof e === 'object' && 'status' in e && (e as { status?: number }).status === 404) {
      return null;
    }
    throw e;
  }
}
