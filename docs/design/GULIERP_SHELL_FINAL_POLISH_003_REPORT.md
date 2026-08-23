# GULIERP_SHELL_FINAL_POLISH_003 — REPORT

> Goal: pull the UserMenu back to a discrete ERP-style identity
> surface. GULIERP_SHELL_FINAL_POLISH_001 introduced a 32px
> gradient-blue avatar circle on the topbar trigger and a 48–56px
> gradient-blue avatar circle in the dropdown detail head. That was
> a SaaS / social-product pattern. GuliERP is a daily-driver
> manufacturing ERP — the topbar already exposes the user entry,
> and a 56px blue circle in the dropdown is visual noise. This
> milestone removes the avatar circles entirely and replaces them
> with a small 32px slate User icon (auxiliary, not a dominant
> gradient block). The dev-state tags `M2+` and `预留` are also
> removed from the menu items.
> Scope: `apps/web/src/components/layout/UserMenu.vue` ONLY. No
> ErpShell.vue, no Header, no Sidebar, no Design Token, no
> navigation.css, no color.css, no Backend, no C#, no Identity /
> Permission / Tenant / Database / API Contract changes.

Date: 2026-08-23
Status: **SHELL_FINAL_POLISH_003_VERIFIED_FOR_OPERATOR_REVIEW**

---

## 1. Files modified

| File                                                          | Change                                                                                              |
|---------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| `apps/web/src/components/layout/UserMenu.vue`                | Topbar trigger: removed `<el-avatar>` circle, replaced with a small 16px User icon. Dropdown detail head: removed 56px gradient-blue avatar circle, replaced with a 32px slate User icon (with subtle bg/border). Dropdown items: removed `M2+` and `预留` dev-state tags. Removed `initials` computed (was already removed in FINAL POLISH 001) and `roleLabel` computed. |
| `tests/GuliERP.Api.Tests/SalesRuntimeRegressionSourceFacts.cs` | `Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut` rewritten to lock the new contract: topbar icon is 16px (not 32px avatar), no 56px avatar anywhere, no `<el-avatar>` element, no `gs-user-avatar-icon` class, no `管理员` line, no `个人中心 (M2+)` / `修改密码 (预留)` literals, no `gs-user-menu-tag` chip class. The positive contract (UserFilled icon present, @handle line, etc.) is preserved. |

No other files were touched. The topbar logout button stays in
`ErpShell.vue` (untouched). The logout flow (ElMessageBox.confirm →
auth.signOut() → CSRF refresh + POST /auth/logout + state clear +
redirect to /login) is unchanged.

---

## 2. What was wrong, what was fixed

### 2.1 The avatar circle was the wrong metaphor for an ERP

The UserMenu in FINAL POLISH 001 rendered:

- **Topbar trigger:** a 32px gradient-blue circle
  (`linear-gradient(135deg, #0A6ED1, #085CAF)`) with a generic
  `UserFilled` icon. White text. White chevron.
- **Dropdown detail head:** a 56px gradient-blue circle
  (same gradient) with a larger `UserFilled` icon. The name
  beside it. The `@username` handle in monospace. The role chip
  "管理员" as a primary-tinted badge. Tenant + company meta.

The pattern is "social product profile chip" — appropriate for
Slack / Notion / Feishu. It is wrong for GuliERP because:

1. **Duplication.** The topbar already exposes the user entry. A
   second large colored surface in the dropdown repeats the
   identity, but adds no information.
2. **Color weight on chrome.** GuliERP is a long-hours back-office
   tool. The blue gradient on the avatar competes with the brand
   blue of the topbar. A daily-driver operator sees the avatar
   dozens of times per day; it should not feel like a notification
   badge.
3. **Role visibility is implicit elsewhere.** The "管理员" chip
   in the dropdown is redundant with the topbar trigger (which
   showed the same chip in FINAL POLISH 001) and was already
   dropped from the topbar by FINAL POLISH 001. Keeping it in the
   dropdown would put the role back in the visual hierarchy.

### 2.2 The new identity surface

**Topbar trigger** (FINAL POLISH 003):
```
┌─────────────────────────────┐
│ 👤 清清 ▼                    │   ← icon 16px, name 13px, chevron
└─────────────────────────────┘
   32px tall, 8px horizontal padding, 4px radius
   hover: var(--header-hover-bg) overlay
   No colored circle. No role chip. No gradient.
```

**Dropdown detail head** (FINAL POLISH 003):
```
┌──────────────────────────────────────┐
│ ┌────┐  清清                         │
│ │👤  │  @admin                       │
│ └────┘  租户：谷粒                    │
│         公司：谷粒信息                │
└──────────────────────────────────────┘
   32px icon in a 1px slate box (var(--border-subtle) border,
   var(--bg-subtle) bg) — subtle, NOT a gradient.
   Name is the visual anchor; icon is an auxiliary marker.
   No role chip. No gradient blue circle.
```

**Dropdown items** (FINAL POLISH 003):
```
个人中心
修改密码
```
   No `M2+` / `预留` chips. Production users do not see
   internal development-phase labels.

### 2.3 The dev-state tags

Before this milestone, the dropdown items read:

```
个人中心          [M2+]
修改密码          [预留]
```

`M2+` and `预留` are internal development-phase markers. A
production user logging into a daily-driver ERP should not see
"this feature is reserved" or "this is for milestone 2+". The
items are still disabled (click is a no-op) per the design
intent, but the labels are now clean.

### 2.4 Logout — unchanged

The topbar logout button stays in `ErpShell.vue` (out of scope for
this milestone). The dropdown does NOT have a logout item, which
matches the spec: "如果当前已有" (only if currently exists). The
auth flow (ElMessageBox confirm → signOut helper → CSRF refresh +
POST /auth/logout + state clear + redirect to /login) is
untouched.

---

## 3. Visual / screenshot guide

The Operator should hard-refresh the browser (Ctrl+Shift+R) after
restarting the backend and verify the following points.

### 3.1 Topbar trigger

- 32px tall, 8px horizontal padding, 4px radius.
- A small (16px) `UserFilled` icon in white (slate-white when not
  hovered, full white on hover).
- The display name `清清` (or current user) in white, 13px, up to
  140px wide before ellipsis.
- An `ArrowDown` chevron in white.
- **No** colored circle. **No** role chip. **No** initials.
- Hover: `var(--header-hover-bg)` overlay (same as other topbar
  controls).

### 3.2 Dropdown

- White surface, 1px `--border-default` border, soft
  `0 4px 16px rgba(0, 0, 0, 0.08)` shadow.
- 260px min-width, 4px vertical padding.
- **Detail head** (top of dropdown):
  - 32px icon container (1px slate border, `--bg-subtle` bg,
    6px radius). The icon inside is a `UserFilled` glyph in
    `--text-secondary` (#5B738B) — subtle, NOT a blue gradient.
  - Right of the icon: name (15px / weight 600 / `--text-primary`
    #1D2D3E), `@username` handle (12px monospace muted
    `--text-muted` #6B7C8C), `租户：` line (12px
    `--text-secondary`), `公司：` line (12px
    `--text-secondary`).
  - **No** `管理员` line.
- **Items:** `个人中心` and `修改密码`. Each with a left icon
  (`User` and `Lock` respectively). Both disabled (click is a
  no-op for now; the implementations are M2+ future WorkItems).
  **No** `M2+` / `预留` chips.

### 3.3 Topbar logout — unchanged

- The standalone logout button between UserMenu and the fullscreen
  toggle still exists (defined in `ErpShell.vue`).
- White default text, danger color on hover / focus / loading.
- Click → ElMessageBox warning confirm → sign-out → CSRF refresh +
  POST /auth/logout + state clear → redirect to `/login`.

---

## 4. Build & test results

| Step                          | Result                                                              |
|-------------------------------|---------------------------------------------------------------------|
| `npm run typecheck` (vue-tsc) | exit 0, no errors                                                   |
| `npm run build` (vite)        | exit 0                                                              |
| `GuliERP.Api.Tests`           | 32/32 PASS (source-grep regression test PASS)                      |
| `GuliERP.Sales.Tests`         | 9/9 PASS                                                            |

### 4.1 Visual contract asserts (compiled dist)

| Check                                        | Result         |
|----------------------------------------------|----------------|
| `UserFilled` icon in compiled JS             | offset 227946 ✓ |
| `<el-avatar` element in compiled JS         | -1 (absent) ✓ |
| `size:16` (topbar icon) in compiled JS       | offset 1043570 ✓ |
| `size:32` (dropdown icon) in compiled JS     | offset 1042886 ✓ |

The compiled JS contains the new UserFilled icon at the expected
offsets and the no-avatar contract is enforced (no `<el-avatar`
anywhere in the dist).

The agent stopped the running dev-server backend (PID 46776) once
to unlock the API's `bin/Release/net10.0/*.dll` for the rebuild.
The backend was not restarted — Operator can restart via
`tools/dev/run-web-preview-backend.ps1`.

---

## 5. Out-of-scope (deliberately deferred)

- **SalesOrder views** — explicitly out of scope for this
  milestone per the brief "完成后停止. 不要进入 SalesOrder".
- **`<DsButton>` / `<DsCard>` / `<DsAvatar>` wrappers around
  Element Plus** — the FINAL POLISH 003 actually moves AWAY from
  using avatars; a wrapper component would be premature.
- **Re-introducing initials for short Chinese names** (the
  "first char" trick that was tried in FINAL MICRO FIX) — the
  product decision is no avatar. Initials do not come back.
- **Adding an avatar upload flow** for production (so the
  dropdown could show the user's real photo) — explicitly out of
  scope; would need a backend media endpoint + S3-style storage.

---

## 6. Final state

GULIERP_SHELL_FINAL_POLISH_003 — **VERIFIED_FOR_OPERATOR_REVIEW**

The UserMenu is now a discrete enterprise-ERP identity surface:

- Topbar: 16px User icon + display name + chevron. No colored
  circle, no role chip, no initials.
- Dropdown: white surface, 32px slate icon detail head, plain
  menu items (no dev tags).
- The sign-out flow stays where it was: the standalone topbar
  logout button (in `ErpShell.vue`).
- The source-grep regression test
  (`Shell_User_Menu_Exposes_Logout_And_Uses_Auth_SignOut`) now
  enforces the new contract: no `<el-avatar` element, no
  `gs-user-avatar-icon` class, no `管理员` line, no
  `个人中心 (M2+)` / `修改密码 (预留)` literals, no
  `gs-user-menu-tag` chip class.

After Operator visual verification, the next WorkItem should be
the per-page color audit (Phase 2 of PAGE_THEME_AUDIT) or any
remaining M2 feature work. SalesOrder UI rebaseline and any other
business development are explicitly OUT of scope for this
milestone.
