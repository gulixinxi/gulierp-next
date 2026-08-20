# GuliERP PC Shell UX Pattern V1

| Field | Value |
|---|---|
| Gate | `GULIERP_PC_SHELL_UX_PATTERN_V1_VERIFIED · CLOSED` |
| Verified | Operator Browser acceptance (WEB-UX-SHELL-001) |
| Baseline commit | `feat(web): establish verified ERP shell UX baseline` |

---

## 1. Secondary Sidebar

| Property | Value |
|---|---|
| Default width | 160px |
| Min width | 136px |
| Max width | 220px |
| Persistence | `localStorage: erp.shell.secondaryWidth` |
| Clamp | Out-of-range stored values auto-clamp to default |
| Collapse | Supported (hide secondary, main content fills) |

Chinese 4–6 char menu labels render correctly at 136px minimum.

## 2. MDM Table Actions

### Single-line rule

All MDM list pages use the shared `MdmTableRowActions` component.
Actions render on a single line with `white-space: nowrap`.
No block-style primary buttons — text/link style only.

### Status-based actions

| Row status | Actions |
|---|---|
| ACTIVE | 编辑 \| 停用 |
| INACTIVE | 编辑 \| 启用 |

停用 requires secondary confirmation:
> 确定停用"XXX"吗？停用后将不能用于新的业务单据，历史数据不受影响。

### Action column width

Target: 120–150px. Must not wrap. Must not crowd.

### Future: more actions

When actions exceed 2, adopt `查看 | 编辑 | 更多 ▾` pattern.
Do not stack unlimited primary buttons.

## 3. MDM Delete Policy

**NO DEFAULT PHYSICAL DELETE BUTTON** for standard master data:

- UOM
- ItemCategory
- Item
- BusinessPartner (future)
- Warehouse (future)
- Location (future)

Use ACTIVE / INACTIVE status management instead.

### Future delete boundary (NOT IMPLEMENTED)

If physical delete is ever allowed:
- Only for records with no business references
- Must pass reference check
- Must require secondary confirmation
- Entry point: `More → Delete` (never a primary button)

## 4. Multi-Tab Workbench

### Batch close operations

| Operation | Behavior |
|---|---|
| Close Current | Close active tab (if closable). Activate nearest remaining. |
| Close Left | Close all closable tabs to the left. Keep current active. |
| Close Right | Close all closable tabs to the right. Keep current active. |
| Close Others | Close all closable tabs except current + pinned home. |
| Close All | Close all closable tabs. Activate pinned home. |

### Access methods

1. **Right-click context menu** on any tab
2. **"More" dropdown** (⋯ button) in tab bar actions

Context menu auto-closes on: click-outside, Esc, route change.
Menu position clamped to viewport.

## 5. Pinned Tab

| Property | Value |
|---|---|
| Semantic | `CANONICAL_ERP_HOME_OR_WORKBENCH` |
| Current temporary pinned route | `list-sales-order` |
| `closable` | `false` |

All batch close operations preserve tabs with `closable: false`.
When a real ERP Home / Workbench page is built, replace the pinned
route only — do not redesign the Multi-Tab mechanism.

## 6. Duplicate Tab Prevention

Same route click activates existing tab. No duplicate tab creation.
Tab ID derived from route: `list-{route.name}`.

## 7. Router ↔ Tab Synchronization

- Click tab → `router.push(tab.route)`
- Route change → find matching tab → activate (or auto-create if missing)
- Close active tab → navigate to new active tab
- Browser refresh → current route's tab auto-restored

## 8. Tab Overflow

Tab list container uses `overflow-x: auto` with thin scrollbar.
Tabs never break the Shell layout.

## 9. Responsive Boundary

Target: PC Web (1920×1080, 1366×768).
Mobile ERP is out of scope for V1.
