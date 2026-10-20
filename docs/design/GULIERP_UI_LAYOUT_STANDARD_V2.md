# GuliERP UI Layout Standard V2

Status: CANDIDATE V2. This standard calibrates layout, density, and table sizing on top of the frozen Enterprise Fiori Blue direction from `GULIERP_DESIGN_SYSTEM_V1.md`.

## 1. Scope

V2 is a system calibration, not a redesign. It keeps the existing shell, Element Plus stack, Enterprise Blue primary color, slate module rail, and compact ERP posture.

V2 supersedes older page-level micro rules where individual pages used one-off widths such as `width="137"` or local table padding overrides. Future pages should use the semantic tokens and shared presets described here.

## 2. Layout

- Primary rail remains `64px`.
- Secondary navigation remains fixed at `180px`.
- Topbar remains `52px`.
- Content uses the full desktop ERP workspace; list and document pages should not add a marketing-style max-width.
- Narrative/setup pages may use `--content-readable-max`.
- Page padding uses `--layout-page-pad-x` and `--layout-page-pad-y`.
- Section spacing uses `--layout-section-gap`.

Desktop targets:

| Width | Rule |
| --- | --- |
| 1366px | Preserve dense rows and avoid over-wide action/search areas. Tables may scroll horizontally when the information set requires it. |
| 1440px | Default calibration target. List toolbar, table, and pagination should read as one compact work surface. |
| 1920px | Use available table width for names/descriptions; do not center list pages in a decorative narrow column. |

## 3. Spacing

Use the existing 4px-based spacing scale. V2 adds semantic aliases:

| Token | Use |
| --- | --- |
| `--layout-page-pad-x` | Horizontal padding for list toolbar and pagination. |
| `--layout-page-pad-y` | Vertical padding for compact list toolbar. |
| `--layout-section-gap` | Gap between toolbar, table, and pagination surfaces. |
| `--layout-toolbar-gap` | Gap between search, filters, and actions. |
| `--layout-form-gap-x` | Horizontal gap for form/filter grids. |
| `--layout-form-gap-y` | Vertical gap for form/filter grids. |

## 4. Density

The default remains Comfortable ERP Density:

- Font: `13px`.
- Input/button height: `32px`.
- Table header: `36px`.
- Table row: `36px`; small table row: `32px`.
- Toolbar buttons stay compact, with icons where commands benefit from scanning.
- Avoid oversized headings, hero panels, large cards, and excessive vertical whitespace in operational pages.

## 5. Color Hierarchy

Keep the Fiori-inspired palette:

- Primary: `#0A6ED1`.
- Primary rail: `#354A5F`.
- Success: `#107E3E`.
- Warning: `#E9730C`.
- Danger: `#BB0000`.

Pages and shared components should reference semantic tokens only. V2 removes sample-page hard-coded status tints in favor of `--warning-bg`, `--warning-hover`, `--danger-bg`, `--danger-hover`, `--success-bg`, and primary aliases.

## 6. Table Column Presets

Use `apps/web/src/design-system/tableColumns.ts` for Vue table column widths. CSS variables with matching names live in `apps/web/src/design-system/tokens/sizing.css` for style-level references and documentation.

| Preset | Width | Intended use |
| --- | ---: | --- |
| `selection` | 40 | Checkbox selection. |
| `index` | 50 | Row index. |
| `status` | 88 | Status tags. |
| `code` | 132 | Master-data code. |
| `documentNo` | 160 | Sales/purchase document number. |
| `date` | 120 | Date without time. |
| `datetime` | 168 | Date and time. |
| `person` | 120 | Contact/person name. |
| `phone` | 132 | Phone number. |
| `email` | 196 | Email. |
| `taxNo` | 156 | Tax number. |
| `shortName` | 128 | Short name. |
| `type` | 112 | Type/category tag. |
| `uom` | 100 | Unit of measure. |
| `category` | 140 | Short category name. |
| `itemNature` | 104 | Item nature. |
| `spec` | 168 | Specification/model. |
| `money` | 136 | Amount columns, right-aligned. |
| `currency` | 84 | Currency code. |
| `nameMin` | 200 | General name min-width. |
| `partnerName` | 240 | Customer/supplier/business partner name. |
| `descriptionMin` | 220 | Flexible description. |
| `actions` | 136 | MDM row actions. |
| `documentActions` | 150 | Document list row actions. |

Rules:

- Use fixed semantic widths for identity, status, date, money, and action columns.
- Use `min-width` for names and descriptions so wide desktops give readable fields more room.
- Amount columns use monospace and right alignment.
- Action columns should remain single-line.

## 7. Forms

- MDM drawer default width is `--dialog-width-md` (`520px`).
- Form label width default is `104px`.
- Large or document forms may use `--dialog-width-lg`.
- Use two-column or grid layouts only when the form naturally groups fields; do not force dense multi-column forms for short MDM drawers.
- Validation messages and required markers continue to use Element Plus behavior with GuliERP tokens.

## 8. Dialogs And Drawers

| Token | Width | Use |
| --- | ---: | --- |
| `--dialog-width-sm` | 420px | Small confirmations or narrow setup dialogs. |
| `--dialog-width-md` | 520px | Standard MDM create/edit drawer. |
| `--dialog-width-lg` | 640px | Wider document/context forms. |

Dialog and drawer widths should be deliberate. Avoid one-off widths unless the page documents a real data-shape reason.

## 9. Toolbar

- Default list search width is `--toolbar-search-width` (`280px`).
- Wider search uses `--toolbar-search-wide` (`320px`) only when placeholder/content warrants it.
- Left side: search and filters.
- Right side: secondary commands, divider, primary create/action command.
- Toolbars wrap on narrower desktop widths instead of shrinking controls below readable sizes.

## 10. Page Structures

List page:

```text
Toolbar / Filters
DataTable
Pagination
```

Master detail page:

```text
PageHeader
Summary
Tabs / Sections
```

Document page:

```text
Document Header
Business Fields
Line Items
Totals / Status / Actions
```

## 11. Rollout Rule

V2 is proven first on three representative pages:

- `BusinessPartnerList.vue` for complex master data.
- `ItemList.vue` for high-frequency master data.
- `SalesOrderList.vue` for ERP document lists.

After operator visual acceptance, apply the same presets gradually to the remaining MDM, Sales, Purchase, and Inventory pages. Do not mix V2 rollout with unrelated business goals.
