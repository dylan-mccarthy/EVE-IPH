# EVE Isk per Hour — UI Design Plan

## 1. Design Philosophy

The modernised UI must achieve two things simultaneously: look like a professional, contemporary desktop application and feel immediately familiar to users of the legacy WinForms version. The legacy UI is dense and utilitarian — it exposes every setting and every data point without hiding anything behind extra dialogs. The modern UI must preserve that information density and user control while using space, colour, and layout more deliberately.

**Guiding principles:**
- Never reduce the number of data points or controls visible in a workflow — reorganise them, not remove them.
- Prefer inline editing and progressive disclosure over navigating to a separate screen.
- Group related controls into visually distinct cards, not flat columns of checkboxes.
- Every data-loading action must have a clear loading state and a clear error state.
- Navigation between primary areas must be one click, never buried.

---

## 2. Design System

### 2.1 Colour Palette

The application uses EVE Online's dark-space aesthetic. All surfaces are dark blue-slate tones; accent colours draw from EVE's corporate gold and cyan.

| Role | Token | Hex | Usage |
|---|---|---|---|
| App Background | `--color-bg-app` | `#0D1821` | Window background, deepest layer |
| Surface Primary | `--color-surface-primary` | `#122033` | Main content cards, tab bodies |
| Surface Secondary | `--color-surface-secondary` | `#16263B` | Secondary cards, list rows (even) |
| Surface Elevated | `--color-surface-elevated` | `#1C2F47` | Hover states, selected rows |
| Border Subtle | `--color-border-subtle` | `#28415F` | Card borders, dividers |
| Border Active | `--color-border-active` | `#3A5A80` | Input focus rings, active tab indicator |
| Accent Gold | `--color-accent-gold` | `#C89B3C` | Primary action buttons, highlights |
| Accent Gold Hover | `--color-accent-gold-hover` | `#E0B050` | Button hover |
| Accent Cyan | `--color-accent-cyan` | `#4FC3F7` | Links, active selections, status good |
| Text Primary | `--color-text-primary` | `#E8EDF2` | Body text, labels |
| Text Secondary | `--color-text-secondary` | `#8BA4BE` | Helper text, column headers |
| Text Muted | `--color-text-muted` | `#4D6A85` | Placeholder text, disabled labels |
| Status Success | `--color-status-success` | `#4CAF7D` | Token healthy, job complete |
| Status Warning | `--color-status-warning` | `#F0A500` | Token expiring, partial data |
| Status Error | `--color-status-error` | `#E53935` | Token expired, auth failure |
| Status Info | `--color-status-info` | `#4FC3F7` | Loading, neutral info |
| Positive ISK | `--color-isk-positive` | `#4CAF7D` | Profit figures |
| Negative ISK | `--color-isk-negative` | `#E53935` | Loss figures |
| Neutral ISK | `--color-isk-neutral` | `#8BA4BE` | Break-even / informational ISK |

### 2.2 Typography

| Style | Font | Size | Weight | Usage |
|---|---|---|---|---|
| Heading Large | System UI / Segoe UI | 18px | 600 | Window title, tab section headings |
| Heading Medium | System UI / Segoe UI | 14px | 600 | Card headers, dialog titles |
| Heading Small | System UI / Segoe UI | 12px | 600 | Sub-section labels, filter group titles |
| Body | System UI / Segoe UI | 12px | 400 | Form labels, grid cell text |
| Body Small | System UI / Segoe UI | 11px | 400 | Helper text, status lines, timestamps |
| Mono | Consolas / Courier New | 11px | 400 | IDs, type IDs, raw numeric inputs |
| ISK Value | System UI / Segoe UI | 12px | 600 | All ISK amounts in result cards |

### 2.3 Spacing & Geometry

| Token | Value | Usage |
|---|---|---|
| `--space-xs` | 4px | Icon-to-label gap, tight row padding |
| `--space-sm` | 8px | Control internal padding, compact group spacing |
| `--space-md` | 12px | Row padding in data grids, form field gap |
| `--space-lg` | 16px | Card internal padding, section gap |
| `--space-xl` | 24px | Window edge margin, major section separation |
| `--radius-sm` | 4px | Buttons, inputs, tags |
| `--radius-md` | 8px | Cards, panels |
| `--radius-lg` | 12px | Modal dialogs, elevated overlays |
| Border width | 1px | All card and input borders |
| Minimum window | 960 × 640px | Hard minimum before layout degrades |
| Default window | 1200 × 780px | Comfortable default for all tabs |

### 2.4 Control Styles

**Buttons**

| Variant | Background | Border | Text | Usage |
|---|---|---|---|---|
| Primary | `--color-accent-gold` | none | `#1A1A1A` | One primary action per card (Analyse, Save, Connect) |
| Secondary | transparent | `--color-border-active` | `--color-text-primary` | Secondary actions (Refresh, Clear, Reset) |
| Danger | transparent | `--color-status-error` | `--color-status-error` | Delete, Remove |
| Ghost | transparent | none | `--color-accent-cyan` | Inline links, "Copy ID" style actions |

All buttons: 6px vertical padding, 14px horizontal padding, 4px border radius, 12px font.

**Inputs**

Background: `--color-surface-secondary`. Border: `--color-border-subtle`. Focus border: `--color-border-active`. Height: 28px for single-line, auto for multi-line. Placeholder text: `--color-text-muted`.

**Data Grids**

Rows: 28px height. Header row: `--color-surface-primary` background, `--color-text-secondary` header text, 600 weight. Even data rows: transparent. Odd data rows: `--color-surface-secondary` at 40% opacity. Selected row: `--color-surface-elevated` with `--color-border-active` left-edge accent (2px). Hover row: `--color-surface-elevated`. All columns are resizable. Sortable columns show a sort indicator icon.

**Tags / Badges**

Inline status tags (e.g. "T2", "Original", "Director") are 11px, 4px radius, 4px vertical padding, 8px horizontal padding with a slightly lighter version of the status colour as background and the status colour as text.

**Checkboxes and Radio Buttons**

Use Avalonia's FluentTheme controls. Border: `--color-border-subtle`. Check colour: `--color-accent-gold`. Label text: `--color-text-primary`.

**Dropdowns / ComboBoxes**

Same input styling. Arrow indicator uses `--color-text-secondary`. Dropdown list uses `--color-surface-elevated` background with row hover effect.

**Progress / Loading**

Indeterminate spinner: small 16px ring in `--color-accent-cyan`. In-line loading text replaces data in the affected area until data arrives. Never block the whole window for tab-level data loads.

---

## 3. Shell Architecture

### 3.1 Window Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  TITLE BAR  [EVE Isk per Hour]    [status chip]    [─] [□] [×]  │
├─────────────────────────────────────────────────────────────────┤
│  SIDEBAR NAV  │  TAB CONTENT AREA                               │
│  (64px wide   │                                                 │
│   icon+label  │                                                 │
│   vertical    │                                                 │
│   tabs)       │                                                 │
│               │                                                 │
│               │                                                 │
│               │                                                 │
├───────────────┴─────────────────────────────────────────────────┤
│  STATUS BAR:  [Character: Navatalin ▼]  [SDE: loaded]  [clock] │
└─────────────────────────────────────────────────────────────────┘
```

**Navigation model:** Replace the flat horizontal tab strip with a collapsible left-side navigation bar. At full width the bar shows 64px icon + short label. A collapse toggle at the top shrinks it to 48px icon-only mode, recovering horizontal space for dense views. On minimum window widths (< 1000px) the bar collapses automatically.

This matches modern tooling (VS Code, JetBrains) and is more legible than 11 horizontal tabs. The primary colour of the active nav item uses `--color-accent-gold` left-border accent with `--color-surface-elevated` background. Inactive items use `--color-text-secondary` icon/label.

**Title bar:** Frameless custom title bar in `--color-bg-app`. Shows application name and a global status chip (e.g. "ESI connected", "Offline", "Importing"). Three window control buttons at right.

**Status bar:** Thin 24px strip at the bottom. Left: active character selector (dropdown, persists last-used character as context for data loads). Centre: SDE version and load state. Right: current UTC clock (relevant for cache expiry awareness).

### 3.2 Navigation Items (in order)

| Icon | Label | Keyboard | Legacy Equivalent |
|---|---|---|---|
| Person | Characters | Alt+1 | Manage Accounts |
| Blueprint | Blueprints | Alt+2 | Blueprints tab |
| Factory | Manufacturing | Alt+3 | Manufacturing List tab |
| Chart | Market Prices | Alt+4 | Update Prices tab |
| Cart | Shopping List | Alt+5 | Shopping List tab |
| Pickaxe | Mining | Alt+6 | Mining tab |
| Structure | Facilities | Alt+7 | Upwell Structure settings |
| Inventory | Assets | Alt+8 | Asset Viewer |
| Industry | Industry Jobs | Alt+9 | Industry Jobs sub-view |
| Atom | Research | Alt+0 | Datacores tab |
| Gear | Settings | Alt+S | Application Settings |

### 3.3 Global Context Header

Each tab content area begins with a thin global-context strip (32px) showing:
- **Active character** display name and corporation
- **Last refreshed** timestamp for this tab's data
- **Refresh** ghost button

This replaces the legacy per-tab status-bar pattern where "Character Loaded: Navatalin | Skills Loaded | No Items in Shopping List" was crammed into a flat status bar.

---

## 4. Component Library

### 4.1 InfoCard

A bordered card container used to group a logically related set of controls.

```
┌─ Card Title ────────────────────────────────────────────┐
│  [content]                                              │
└─────────────────────────────────────────────────────────┘
```

- Background: `--color-surface-primary`
- Border: 1px `--color-border-subtle`, 8px radius
- Title: 12px, 600 weight, `--color-text-secondary`
- Padding: 16px all sides, 8px below title

### 4.2 StatRow

An inline key-value display row used in result panels and summaries.

```
  Label text ........................... Value text
```

- Label: `--color-text-secondary`, 12px
- Value: `--color-text-primary`, 12px, 600 weight for ISK values
- Dots between: `--color-border-subtle`
- ISK values coloured using positive/negative/neutral tokens
- Row height: 22px

### 4.3 FilterBar

A horizontal strip sitting above a data grid, containing filter inputs.

```
  [Owner ▼]  [Search...        🔍]  [☐ BPCs only]  ←→ column layout
```

- Background: `--color-surface-secondary`
- Radius: 8px
- Padding: 8px 12px
- All controls inline, space-separated
- Counts label at right: "Showing 24 of 127"

### 4.4 ActionBar

A horizontal strip below or beside a data grid containing the primary actions for that grid.

```
  [Primary Action]  [Secondary Action]  [Danger Action]  →  [grid]
```

Placed below a grid, not floating over it.

### 4.5 StatusChip

A small inline badge for entity health status.

```
  ● Token valid    ● Token expiring    ● Token expired
```

- Size: 10px dot + 11px label
- Dot and label colour from status tokens
- No background fill — inline text pattern

### 4.6 ResultPanel

A distinct card used to display calculated output from an analysis (manufacturing, belt flip, reprocessing). Uses a two-column StatRow layout internally with a highlight border on the primary outcome.

```
┌─ Analysis Result ───────────────────────────────────────┐
│  Decision: ████ BUILD ████  (gold highlight border)     │
│  ─────────────────────────────────────────────────────  │
│  Build Cost .....................................  5.2 B │
│  Sale Value .....................................  7.8 B │
│  Profit .........................................  2.6 B │
│  ISK/Hour .......................................  1.1 B │
└─────────────────────────────────────────────────────────┘
```

### 4.7 LoadingOverlay

A non-blocking inline loading state replacing the data area of a card without covering the rest of the tab.

```
  ⟳  Loading market prices…
```

- Centred within the card
- `--color-accent-cyan` spinner ring
- `--color-text-secondary` label text

### 4.8 EmptyState

Shown when a grid has no rows after loading completes.

```
  [Icon]
  No blueprints found.
  Adjust filters or add a character.
```

- Centred within the grid area
- Muted icon, secondary text colour
- Optional CTA ghost button

### 4.9 ModalDialog

Used for Add Character, confirmation prompts, onboarding flow, and import prompts.

- Background overlay: black at 60% opacity
- Dialog card: `--color-surface-elevated`, 12px radius
- Max width: 600px for standard dialogs, 760px for scope-selection dialogs
- Always has a clearly labelled close/cancel button
- Primary action at bottom-right, cancel at bottom-left

---

## 5. Tab / Screen Designs

### 5.1 Characters

**Purpose:** Connect EVE SSO characters and corporations; manage token health; set the active default character.

**Legacy baseline:** "Manage Accounts" modal with a flat character list and scope checkbox overlay; "Add Character" dialog with grouped scope checkboxes.

**Layout:**

```
┌─ Connected Characters ─────────────────────────────────────────┐
│  [+ Connect Character]                      [Refresh All]       │
│  ─────────────────────────────────────────────────────────     │
│  ● Navatalin    2 Days to Mine   ★ Default   ● Token valid  [⋮] │
│  ● AltChar      Caldari Navy     —            ● Token valid  [⋮] │
│                                                                  │
│  (empty state when no characters)                               │
└─────────────────────────────────────────────────────────────────┘

┌─ Connected Corporations ───────────────────────────────────────┐
│  [+ Add Corporation]                                            │
│  ─────────────────────────────────────────────────────────     │
│  2 Days to Mine   Via: Navatalin   Director   ● Corp token  [⋮] │
└─────────────────────────────────────────────────────────────────┘
```

**Character row — overflow menu [⋮] actions:**
- Set as Default
- Refresh Token
- View Scopes
- Remove Character

**Corporation row — overflow menu [⋮] actions:**
- Refresh Token
- Remove Corporation

**Add Character flow (modal):**

1. Scope selection step: checkboxes grouped into `Character`, `Corporation`, `Structures` sections with Select All / Deselect All shortcuts and the mandatory `Character Skills` scope locked enabled. Mirrors the legacy scope dialog exactly.
2. OAuth redirect step: "Log In with EVE Online" button (using official EVE SSO button style).
3. Confirmation step: shows character name and connected scopes; Close or Connect Another.

**Token health indicators:**

| State | Chip colour | Row style |
|---|---|---|
| Valid | Success green | Normal |
| Expiring within 24h | Warning amber | Amber left border |
| Expired / invalid | Error red | Red left border, italic |

**Component breakdown:**
- Two InfoCards (Characters, Corporations) stacked vertically
- Character rows: avatar icon placeholder (16px circle), name, corporation, default star, status chip, overflow menu button
- ActionBar above each card with primary Add button and secondary Refresh All

---

### 5.2 Blueprints

**Purpose:** View, search, and edit owned blueprints (BPOs and BPCs) across all connected characters and corporations.

**Legacy baseline:** The Blueprints tab on the main form was the primary workspace; a separate "Blueprint List" dialog provided a tree-browse to find blueprints by category and tech level.

**Layout:**

```
┌─ Filter Bar ───────────────────────────────────────────────────┐
│  [Owner ▼]  [Search blueprints…]  [☐ BPCs only]  Showing 24/127│
└─────────────────────────────────────────────────────────────────┘

┌─ Blueprint List (resizable, takes remaining height) ───────────┐
│  Name            ▲  Owner        Kind    ME    TE   Runs       │
│  Hornet I         │  Navatalin    BPO      10    20    —        │
│  Tengu Subsystem  │  Corp         BPO       6    12    —        │
│  Rifter           │  AltChar      BPC       0     0   10        │
│  ...                                                            │
└─────────────────────────────────────────────────────────────────┘

┌─ Edit Panel (shown when a blueprint is selected) ──────────────┐
│  Name: [Hornet I                  ]  Owner:  [Navatalin        ]│
│                                                                  │
│  ME: [10  ]  TE: [20  ]  Runs: [-1     ]  Qty: [1    ]         │
│                                                                  │
│                                       [Delete]  [Save Changes]  │
└─────────────────────────────────────────────────────────────────┘
```

**Blueprint tree picker (modal, replaces "Blueprint List" dialog):**

Triggered by a "Browse Blueprints" ghost button when creating or remapping blueprints. Shows a tree of SDE categories with inline text search and tech-level / size filter chips at the top. Double-click or Enter to select.

**Column definitions — blueprint grid:**

| Column | Width | Sortable | Notes |
|---|---|---|---|
| Name | flexible | yes | Item name from SDE |
| Owner | 120px | yes | Character or Corp name + icon |
| Kind | 70px | yes | BPO / BPC tag |
| ME | 50px | yes | 0–10 |
| TE | 50px | yes | 0–20 (even values) |
| Runs | 60px | yes | -1 for originals |
| Quantity | 60px | yes | Stack size |

**Interactions:**
- Single-click row → populates Edit Panel inline (no navigation away)
- Multi-select with Shift/Ctrl for bulk delete
- ME/TE inputs validate 0–10 and 0–20 respectively, Runs validates −1 or positive integer
- Save is enabled only when values differ from the persisted record
- Unsaved changes show a `●` indicator on the Edit Panel header

---

### 5.3 Manufacturing

**Purpose:** Analyse the profitability of manufacturing a blueprint — build cost, component sourcing, ISK/hour, and build-vs-buy decisions.

**Legacy baseline:** The Manufacturing List tab was extremely dense — blueprint filter tree on left, runs/facility/material inputs across top, component material list below, result summary alongside.

**Layout (two-column):**

```
┌─ Blueprint & Parameters ─────────────┐  ┌─ Analysis Result ───┐
│  Blueprint: [Select Blueprint…    ▼] │  │  (empty until run)  │
│  Facility:  [Select Facility…     ▼] │  │                     │
│  Runs:      [       1             ]  │  │                     │
│                                      │  │                     │
│  ┌─ Costs ──────────────────────────┐│  │                     │
│  │ Sale Price  [              0.00 ]││  │                     │
│  │ Raw Mat.    [              0.00 ]││  │                     │
│  │ Component   [              0.00 ]││  │                     │
│  │ Additional  [              0.00 ]││  │                     │
│  │ ☑ Sales Tax  ☑ Broker Fee       ││  │                     │
│  └──────────────────────────────────┘│  │                     │
│                    [Clear] [Analyse] │  │                     │
└──────────────────────────────────────┘  └─────────────────────┘
```

The result panel appears / refreshes when Analyse is triggered. It uses the ResultPanel component:

```
┌─ Manufacturing Analysis ────────────────────────────────────────┐
│  Decision  ████████ BUILD ████████                              │
│  ────────────────────────────────────────────────────────────  │
│  Build Cost .........................................  5,240 M  │
│  Component Cost .....................................  4,910 M  │
│  Additional Cost ....................................    330 M  │
│  Sale Value .........................................  7,800 M  │
│  Profit .............................................  2,560 M  │
│  Profit Margin ..............................................33% │
│  ISK/Hour ...........................................  1,120 M  │
│  Build Time .........................................  4h 22m   │
│  Sales Tax ................................................ 3.6% │
│  Broker Fee ..............................................  3.0% │
└─────────────────────────────────────────────────────────────────┘
```

Decision badge:
- BUILD: `--color-status-success` background on the decision badge
- BUY: `--color-accent-gold` background
- MARGINAL: `--color-status-warning` background

**Blueprint selector:** Searches across all owned blueprints by name. Shows owner, ME/TE alongside the name in the dropdown list. Typing filters the list.

**Facility selector:** Shows saved manufacturing facilities with their kind (Structure / Station), production type, and cost index. Shows a "(none selected)" option when empty.

**Component Materials expandable section:** Below the parameters card, a collapsible section shows the resolved material list for the current blueprint and run count. Columns: Material Name, Quantity Required, Unit Price (editable), Total Cost. This gives users the same visibility as the legacy "Component Materials" table without cluttering the primary parameters.

---

### 5.4 Market Prices

**Purpose:** Look up current market prices by type ID and/or item group; manage price watchlists; choose the market data source and region.

**Legacy baseline:** "Update Prices" tab — left panel with material category tree and filter checkboxes; right panel with Manufactured Items categories; bottom: single source / region / market structure configuration; price list inline.

**Layout (two-pane):**

```
┌─ Query Settings ─────────────────────────────────────────────┐
│  Source: [Tranquility ▼]    Region: [10000002      ] (ID)     │
│  Type IDs (comma-separated): [                            ]   │
│  [Load Saved Watchlist ▼]   [Reload Defaults]  [Load Prices] │
└──────────────────────────────────────────────────────────────┘

┌─ Price Results ──────────────────────────────────────────────┐
│  Name             ▲  Type ID  Min Sell     Max Buy   Average │
│  Tritanium           34       5.50         5.48      5.49    │
│  Pyerite             35      10.24        10.19     10.21    │
│  ...                                                         │
│                              [Download Prices] [Save Prices] │
└──────────────────────────────────────────────────────────────┘
```

**Watchlist management:** A secondary collapsible card below the results lets users view and edit saved category watchlists (the equivalent of the legacy "Save Settings" / categories pane). This keeps watchlist management discoverable without occupying primary space.

**Data source options:**
- Tranquility (ESI)
- EVEMarketer
- Fuzzworks

**Price Sources — region selector:** Accepts a numeric region ID directly (matching legacy behaviour). A small "common regions" chip bar offers quick-select for Forge, Domain, Heimatar to avoid requiring memorisation of IDs.

**Column definitions — price results grid:**

| Column | Width | Sortable |
|---|---|---|
| Name | flexible | yes |
| Type ID | 80px | yes |
| Min Sell | 110px | yes |
| Max Buy | 110px | yes |
| Average | 110px | yes |

---

### 5.5 Shopping List

**Purpose:** Manage the persistent shopping list of materials to acquire; view total cost.

**Legacy baseline:** Shopping List tab — simple list with item name, type ID, quantity, unit price, total. Clear list button and summary footer.

**Layout:**

```
┌─ Shopping List ──────────────────────────────────────────────┐
│  [Search items…]                        [Clear List] [Reload] │
│  ────────────────────────────────────────────────────────    │
│  Name              Type ID   Qty   Unit Price   Total Price   │
│  Tritanium         34        500   5.50         2,750.00      │
│  Mexallon          36      1,200  12.40        14,880.00      │
│  ...                                                  [×] row │
│  ────────────────────────────────────────────────────────    │
│  24 items    Total qty: 18,420     Total cost: 1,234,567 ISK  │
└──────────────────────────────────────────────────────────────┘
```

**Interactions:**
- Each row has an inline Remove (×) button at the far right — always visible, no need to select first.
- Sort by any column header.
- Summary footer is sticky at the bottom of the card regardless of scroll position.
- "Clear List" triggers a confirmation chip inline ("Are you sure? [Yes, clear] [Cancel]") rather than a modal dialog.

---

### 5.6 Mining & Reprocessing

**Purpose:** Calculate the belt-flip decision for a mining operation — whether raw ore or reprocessed minerals sells for more per hour.

**Legacy baseline:** Mining tab — very dense top section with ore location, ship config, drone settings, booster settings; results grid below; reprocessing yields sidebar. Reprocessing Plant was a separate modal dialog.

**Layout (two-column on wide windows, stacked on narrow):**

```
┌─ Belt Input ──────────────────────────────────┐
│  Belt Lines (paste from chat, pipe-delimited): │
│  ┌─────────────────────────────────────────┐   │
│  │ Veldspar | 500000                       │   │
│  │ Scordite | 300000                       │   │
│  └─────────────────────────────────────────┘   │
│                                                 │
│  Mining m³/hr: [      3000 ]                   │
│  Miners:       [         4 ]                   │
│  ☑ Calculate Per Miner                         │
│  ☑ Use Compressed Sale Values                  │
│                        [Calculate Belt Flip]    │
└─────────────────────────────────────────────────┘

┌─ Belt Flip Result ────────────────────────────┐
│  Decision  ████ REFINE & SELL ████            │
│  ────────────────────────────────────────────  │
│  Raw Ore Value .......................  320.4 M │
│  Refined Value .......................  415.7 M │
│  Flip Benefit ........................   95.3 M │
│  Flip Time ...........................   2h 14m │
│  ISK/Hour ............................  214.0 M │
│  Total Volume ........................  800,000 m³│
└─────────────────────────────────────────────────┘
```

**Reprocessing yields configuration** is surfaced as a collapsible InfoCard below the belt input — matching the legacy "Reprocessing Settings" sidebar with:
- Facility Type (Structure / Station dropdown)
- Location / System
- Base Efficiency %
- Tax %
- Fitting: skill-level inputs for Ore/Ice/Moon/Scrap processing (0–5)
- Crystal type selection

This merges the Reprocessing Plant dialog and the Mining tab's right-side reprocessing panel into a single inline configuration section, eliminating the need for a modal just to configure yield settings.

---

### 5.7 Facilities

**Purpose:** Define and manage Upwell structures and manufacturing/reprocessing/research facility configurations used throughout the application.

**Legacy baseline:** "Upwell Structure Fitting" — a visual slot-fitting interface with item browser; facility settings were stored per-character inside Application Settings. Very complex, most feature-rich dialog in the legacy app.

**Layout (master-detail, two-column):**

```
┌─ Structures ──────────────────────┐ ┌─ Facility Configuration ───────────┐
│  [+ Add Structure]  [Refresh]     │ │  ┌─ Structure Identity ───────────┐ │
│  ────────────────────────────────  │ │  │ ID:    [                    ]  │ │
│  Athanor      Balle       [⋮]     │ │  │ Name:  [                    ]  │ │
│  Raitaru      Jita        [⋮]     │ │  │ Type:  [                    ]  │ │
│  Tatara       Amarr       [⋮]     │ │  │ Solar: [                    ]  │ │
│                                   │ │  │ Region:[                    ]  │ │
│                                   │ │  │ Owner: [                    ]  │ │
│                                   │ │  │ ☑ Manual Entry                 │ │
│                                   │ │  └───────────────────────────────┘ │
│                                   │ │                                     │
│                                   │ │  ┌─ Facility Settings ────────────┐ │
│                                   │ │  │ Kind:        [Manufacture   ▼] │ │
│                                   │ │  │ Character:   [Navatalin     ▼] │ │
│                                   │ │  │ Prod Type:   [Standard      ▼] │ │
│                                   │ │  │ Cost Index:  [          0.00 ] │ │
│                                   │ │  │ Activity Cost[          0.00 ] │ │
│                                   │ │  │ Tax Rate:    [          0.00 ] │ │
│                                   │ │  │ FW Level:    [             0 ] │ │
│                                   │ │  └───────────────────────────────┘ │
│                                   │ │                                     │
│                                   │ │  ┌─ Multiplier Overrides ─────────┐ │
│                                   │ │  │ Material: [1.00] Time: [1.00]  │ │
│                                   │ │  │ Cost:     [1.00]               │ │
│                                   │ │  └───────────────────────────────┘ │
│                                   │ │                                     │
│                                   │ │  ┌─ Module Type IDs ──────────────┐ │
│                                   │ │  │ [35660, 45548, ...            ] │ │
│                                   │ │  └───────────────────────────────┘ │
│                                   │ │                                     │
│                                   │ │        [Delete Facility] [Save]    │
└───────────────────────────────────┘ └─────────────────────────────────────┘
```

**Structure list row — overflow menu [⋮] actions:**
- Edit (selects it in detail panel)
- Delete Structure

**Validation:**
- Numeric fields show an inline error label (not a dialog) when the value is out of range.
- The Save button is disabled when required fields are empty or invalid.

**Scrolling:** The Facility Configuration column scrolls independently if the window is not tall enough. The structure list scrolls separately.

---

### 5.8 Assets

**Purpose:** Browse all assets held by connected characters and corporations; filter by owner, search by name; identify blueprint copies.

**Legacy baseline:** "Asset Viewer" — filter panel on right (item type, quantities, accounts), tree-like asset list on left.

**Layout:**

```
┌─ Filter Bar ─────────────────────────────────────────────────┐
│  [Owner ▼]  [Search assets or locations…]  [☐ BPCs only]     │
│  Sort: [Name ▼]                             Showing 412 items │
└──────────────────────────────────────────────────────────────┘

┌─ Asset List ─────────────────────────────────────────────────┐
│  Name              Group         Location        Flag   Qty   │
│  Hornet I          Combat Drone  Jita IV-4       Hangar  200  │
│  Rifter BPC        Frigate       Athanor (Balle) Hangar    1  │
│  ...                                                         │
│                                          [Refresh Assets]    │
└──────────────────────────────────────────────────────────────┘
```

**Column definitions:**

| Column | Width | Sortable |
|---|---|---|
| Name | flexible | yes |
| Group | 140px | yes |
| Location | 180px | yes |
| Flag | 90px | yes |
| Quantity | 80px | yes |
| Kind | 70px | yes (BPO/BPC/Item) |

**BPC detection:** Blueprint copy rows show a "BPC" tag in the Kind column using the tag component; BPOs show "BPO". Regular items show nothing in Kind.

---

### 5.9 Industry Jobs

**Purpose:** View active and recent industry jobs for connected characters and corporations; understand job status and completion timeline.

**Legacy baseline:** Industry Jobs was a sub-view within the main form's bottom panel; no dedicated screen.

**Layout:**

```
┌─ Summary ────────────────────────────────────────────────────┐
│  [Refresh Jobs]   15 active jobs · 3 complete · 1 paused    │
└──────────────────────────────────────────────────────────────┘

┌─ Job List ───────────────────────────────────────────────────┐
│  Blueprint        Activity     Installer   Scope   Status    │
│  Tengu Hull       Manufacture  Navatalin   Corp    Running   │
│  T2 Component     Research     Navatalin   Char    Complete  │
│  ...                                                         │
└──────────────────────────────────────────────────────────────┘
```

**Status tags use status colours:**
- Running → `--color-status-info`
- Complete → `--color-status-success`
- Paused → `--color-status-warning`
- Cancelled → `--color-text-muted`

**Column definitions:**

| Column | Width | Sortable |
|---|---|---|
| Blueprint | flexible | yes |
| Activity | 110px | yes |
| Installer | 120px | yes |
| Scope | 80px | yes |
| State | 90px | yes |
| Status | 100px | yes |

---

### 5.10 Research Agents

**Purpose:** View active research agents across connected characters; see datacores being generated, their value, and points per day.

**Legacy baseline:** Datacores tab — split into "Datacores Skills" list on left, "Agent Research Records" on right, with faction/standing tree navigation.

**Layout:**

```
┌─ Summary ────────────────────────────────────────────────────┐
│  [Refresh]   8 agents active · Generating 14 datacore types  │
└──────────────────────────────────────────────────────────────┘

┌─ Research Agent List ────────────────────────────────────────┐
│  Agent Name        Datacore         Location   Pts/Day  Qty  Value│
│  Aiken Estemaire   Caldari Starship  Jita       3.2     24   2.4M │
│  ...                                                             │
└──────────────────────────────────────────────────────────────────┘
```

**Column definitions:**

| Column | Width | Sortable |
|---|---|---|
| Agent Name | 160px | yes |
| Datacore | flexible | yes |
| Location | 150px | yes |
| Points/Day | 80px | yes |
| Current Qty | 80px | yes |
| Current Value | 110px | yes |

---

### 5.11 Settings

**Purpose:** Configure startup behaviour, data sources, static data, and application preferences.

**Legacy baseline:** "Application Settings" dialog — one large modal with many checkboxes spread across 4+ groups. Many settings were rarely changed but all were equally visible.

**Layout — scrollable single-column of InfoCards:**

```
┌─ Application Database ──────────────────────────────────────┐
│  Status:  ● Loaded from C:\Users\...\EVE-IPH\AppData.db     │
│  Path:    C:\Users\...\EVE-IPH\AppData.db                   │
│                                [Import Legacy Database] [?]  │
└──────────────────────────────────────────────────────────────┘

┌─ Static Data (SDE) ─────────────────────────────────────────┐
│  Build: Tranquility 21.03.1 · Imported: 2026-03-15 14:22    │
│  Source: [https://...sde.jeveassets.com/latest.db   ]       │
│                                        [Re-import SDE]       │
└──────────────────────────────────────────────────────────────┘

┌─ Startup Preferences ───────────────────────────────────────┐
│  ☑ Check for program updates on startup                     │
│  ☑ Auto-load Assets                                         │
│  ☑ Auto-load Blueprints                                     │
│  ☑ Auto-load Market prices                                  │
│  ☑ Auto-load Cost Indices                                   │
│  ☑ Auto-load Structures                                     │
│                                   [Save Startup Preferences] │
└──────────────────────────────────────────────────────────────┘

┌─ Price Settings ────────────────────────────────────────────┐
│  Default Source:  [Tranquility ▼]                           │
│  Default Region:  [10000002    ]                            │
│  ☐ Do not overwrite manual price updates                    │
│  ☑ Automatically update SVR on BP tab                       │
│  Fuzzworks Refresh Interval (minutes): [10]                 │
└──────────────────────────────────────────────────────────────┘

┌─ Build Defaults ────────────────────────────────────────────┐
│  Default ME: [0]   Default TE: [0]                          │
│  ☐ Default Build/Buy                                        │
│  ☑ Suggest Build when BP not owned                          │
│  ☑ Build when not enough items on market                    │
│  ☐ Always Buy Fuel Blocks                                   │
│  ☐ Always Buy R.A.M.s                                       │
└──────────────────────────────────────────────────────────────┘

┌─ Character Options ─────────────────────────────────────────┐
│  ☐ Alpha Account (25% tax)                                  │
│  ☐ Max Alpha Skills (Dummy)                                 │
│  ☐ Use Active Skills                                        │
│                                                             │
│  Broker Corp Standing:    [5.00]                            │
│  Broker Faction Standing: [5.00]                            │
│                                                             │
│  Implants:                                                  │
│  Manufacturing Beancounter: [None ▼]                        │
│  Reprocessing Beancounter:  [None ▼]                        │
│  Copy Beancounter:          [None ▼]                        │
└──────────────────────────────────────────────────────────────┘

┌─ Shopping List Options ─────────────────────────────────────┐
│  ☑ Include Invention Materials                              │
│  ☑ Include Copy Materials                                   │
└──────────────────────────────────────────────────────────────┘

┌─ Proxy Settings ────────────────────────────────────────────┐
│  Address: [                           ]                     │
│  Port:    [0                          ]                     │
└──────────────────────────────────────────────────────────────┘

┌─ Advanced ──────────────────────────────────────────────────┐
│  [Show Onboarding Dialog]   [Show Update Dialog]            │
│  [Adjust Default Tax Rates] [Edit Rates]                    │
└──────────────────────────────────────────────────────────────┘
```

The settings screen preserves every setting from the legacy Application Settings dialog, reorganised into labelled cards that are easier to scan and scroll through without needing to resize a fixed-dimension modal.

---

## 6. User Interaction Patterns

### 6.1 Data Loading

All data loads are asynchronous and non-blocking at the window level. The affected card or grid shows a LoadingOverlay (spinner + message) while its data is being fetched. Once loaded, the overlay is replaced by the data. If a load fails, the overlay becomes an error state with the error message and a Retry button.

**Pattern:**
1. User triggers load (tab switch, Refresh button, filter change)
2. Affected area shows LoadingOverlay immediately
3. Data arrives → LoadingOverlay replaced by data
4. Error → LoadingOverlay replaced by error + Retry

**Never** disable the rest of the UI during a data load. If a user switches tabs mid-load, that tab's load continues in the background.

### 6.2 Inline Editing

No workflow requires navigating to a separate editor screen. Editing happens:
- **In-place row editing** (e.g. Blueprint edit panel): selecting a row populates a panel below or beside the list.
- **Modal dialogs** only for multi-step flows (Add Character OAuth) or destructive confirmations.
- **Inline validation** appears as a red border + helper text below the field. Not a popup.

### 6.3 Confirmations

Destructive actions (Delete, Remove, Clear List) use an inline confirmation chip pattern rather than a modal:

```
  [Delete] clicked → row shows: "Remove Navatalin? [Confirm] [Cancel]"
```

This keeps context visible (the user can see what they are deleting) without blocking the rest of the UI.

### 6.4 Keyboard Navigation

| Shortcut | Action |
|---|---|
| Alt+1…0, Alt+S | Navigate to tab |
| Tab / Shift+Tab | Move between controls |
| Enter | Confirm selection in dropdown; activate focused button |
| Escape | Cancel inline edit; close modal |
| Ctrl+R | Refresh current tab |
| Ctrl+F | Focus search/filter input on current tab |
| Delete | Delete selected row in a grid (where applicable) |
| F5 | Refresh current tab (mirrors legacy) |

### 6.5 Copy Behaviours

ISK values and IDs in result panels and data grids support right-click → Copy. Type IDs show a small clipboard icon on hover that copies the ID. This preserves the legacy "InGame Links in Copy Text" behaviour.

### 6.6 Empty and Error States

Every list and result area has defined empty and error states using the EmptyState component. Messages are specific to the context:

| Screen | Empty state message |
|---|---|
| Characters | "No characters connected. Click Connect Character to get started." |
| Blueprints | "No blueprints found. Connect a character or adjust the filter." |
| Assets | "No assets loaded. Click Refresh Assets or check character connections." |
| Shopping List | "Shopping list is empty. Add items from the Manufacturing or Market screens." |
| Industry Jobs | "No active jobs found. Ensure characters are connected and click Refresh." |

---

## 7. Process Flows

### 7.1 First Run / Onboarding

```
App launch
    │
    ▼
Is existing database present?
    │ No
    ▼
Onboarding Dialog (modal)
    ├── Option A: Import Legacy Database → File picker → Import → Restart prompt
    └── Option B: Start Fresh → dismiss dialog
    │
    ▼
SDE check: Is static data loaded?
    │ No
    ▼
"Static data not found" banner in Settings tab
User clicks Re-import SDE
    │
    ▼
SDE import progress shown inline in Settings card
    │
    ▼
Characters tab (prompted if no characters connected)
```

### 7.2 Connect Character

```
Characters tab → [+ Connect Character]
    │
    ▼
Add Character Modal — Step 1: Scope Selection
    ├── Character group checkboxes (Standings, Research Agents, Assets, Ship & Location, Blueprints, Industry Jobs)
    ├── Corporation group checkboxes (Membership & Roles, Divisions, Assets, Blueprints, Industry Jobs)
    ├── Structures group checkboxes (Structure Information, Structure Markets)
    ├── [Select All] / [Deselect All]
    └── Character Skills scope locked ON
    │
    ▼
[LOG IN with EVE Online] button
    │
    ▼
Browser opens to EVE SSO (OAuth2 PKCE)
User authenticates in browser
    │
    ▼
App receives callback, exchanges code for tokens
    │
    ▼
Modal — Step 3: Confirmation
    "Connected as Navatalin (2 Days to Mine)"
    Token scopes listed
    [Connect Another Character] [Close]
    │
    ▼
Characters list refreshes, new character appears with ● Token valid
```

### 7.3 Manufacturing Analysis

```
Manufacturing tab
    │
    ▼
Select Blueprint (dropdown, searches owned blueprints by name)
    │
    ▼
Select Facility (dropdown, shows saved facilities)
    │
    ▼
Enter Runs (default 1)
    │
    ▼
Enter Sale Price, or leave 0 to use market price
Enter Raw Material / Component Material / Additional costs if overriding
Toggle Sales Tax and Broker Fee checkboxes
    │
    ▼
[Analyse]
    │
    ▼
ResultPanel updates with:
    - Decision (BUILD / BUY / MARGINAL)
    - All cost breakdown lines
    - Profit, margin, ISK/hour
    - Build time
    │
    ▼
User adjusts any parameter → ResultPanel clears (stale state)
    │
    ▼
[Analyse] again
```

### 7.4 Load Market Prices

```
Market Prices tab
    │
    ▼
Select source (Tranquility / EVEMarketer / Fuzzworks)
Enter region ID or select common region chip
Enter type IDs (comma separated) OR select saved watchlist
    │
    ▼
[Load Prices]
    │
    ▼
Grid shows LoadingOverlay
    │
    ▼
Results appear in grid
    │
    ▼
Optional: [Download Prices] to persist to cache
Optional: [Save Prices] to update saved watchlist
```

### 7.5 Belt Flip Calculation

```
Mining tab
    │
    ▼
Paste belt lines (pipe-delimited) into text area
Set Mining m³/hr and Miner count
Check/uncheck "Calculate Per Miner" and "Use Compressed Sale Values"
(Optionally expand Reprocessing Yields card and set facility / skill levels)
    │
    ▼
[Calculate Belt Flip]
    │
    ▼
ResultPanel updates:
    - Decision (RAW / REFINE & SELL / COMPRESS & SELL)
    - Raw Ore Value
    - Refined / Compressed Value
    - Flip Benefit, Flip Time
    - ISK/Hour, Total Volume
```

### 7.6 Settings Save

```
Settings tab — user changes any preference
    │
    ▼
Relevant card's [Save] button becomes enabled (highlighted)
    │
    ▼
User clicks [Save ...]
    │
    ▼
Settings written to JSON
Button shows "Saved ✓" for 2 seconds then returns to normal
No page navigation or restart required (except DB import which triggers restart prompt)
```

---

## 8. Responsive Behaviour

The application is a desktop-first tool but must resize gracefully.

| Window Width | Layout Change |
|---|---|
| ≥ 1200px | Full two-column layouts in Manufacturing, Facilities; sidebar shows icon + label |
| 960–1199px | Two-column layouts remain; sidebar collapses to icon-only automatically |
| < 960px | Two-column layouts stack vertically; horizontal scrolling permitted in grids; sidebar stays icon-only |

Data grids never truncate columns silently — they scroll horizontally with a visible scrollbar. Column resizing is always available.

All InfoCards have a minimum width of 280px. Below that, the layout falls back to single-column stacking.

---

## 9. Accessibility

- All interactive controls are reachable by Tab / Shift+Tab keyboard navigation.
- Focus rings are always visible (never hidden by CSS/style).
- All status information conveyed by colour is also conveyed by text (not colour-only).
- Minimum touch target size: 28px × 28px (desktop mouse application but benefits drag-and-drop and stylus users).
- Screen reader: all InfoCard titles, grid column headers, and button labels have meaningful text; decorative icons have empty alt text.
- Contrast: all text/background combinations meet WCAG AA 4.5:1 minimum for body text and 3:1 for large text.

---

## 10. Legacy Feature Parity Checklist

This checklist maps every visible control in the legacy screenshots to its location in the modern UI. Nothing is removed; everything is relocated or preserved in-place.

| Legacy Feature | Legacy Location | Modern Location |
|---|---|---|
| Character list | Manage Accounts modal | Characters tab → Connected Characters card |
| Add Character (OAuth + scopes) | Add Character modal | Characters tab → Add Character modal (same) |
| Set Default Character | Manage Accounts modal | Characters tab → row overflow menu |
| Delete Character | Manage Accounts modal | Characters tab → row overflow menu |
| Refresh Token | Manage Accounts modal | Characters tab → row overflow menu |
| Access/Refresh token display | Manage Accounts detail panel | Characters tab → row overflow menu → View Scopes |
| Blueprint tree browse | Blueprint List modal | Blueprints tab → Browse Blueprints modal |
| Blueprint ME/TE edit | Blueprint List / main form | Blueprints tab → Edit Panel inline |
| Blueprint filter by owner | Main form toolbar | Blueprints tab → Filter Bar |
| Manufacturing facility select | Main form toolbar | Manufacturing tab → Facility dropdown |
| Manufacturing run count | Main form toolbar | Manufacturing tab → Runs input |
| Component material list | Main form lower section | Manufacturing tab → expandable Components section |
| Manufacturing result output | Main form right section | Manufacturing tab → ResultPanel |
| Sales Tax / Broker Fee toggles | Main form | Manufacturing tab → Costs card checkboxes |
| Market price source select | Update Prices tab | Market Prices tab → Source dropdown |
| Market region select | Update Prices tab | Market Prices tab → Region ID input |
| Material category tree (prices) | Update Prices tab left panel | Market Prices tab → Load Saved Watchlist |
| Manufactured items categories | Update Prices tab right panel | Market Prices tab → Load Saved Watchlist |
| Download Prices | Update Prices tab | Market Prices tab → Download Prices button |
| Shopping list items | Shopping List tab | Shopping List tab (preserved) |
| Shopping list total | Shopping List tab footer | Shopping List tab → sticky summary footer |
| Ore location options | Mining tab | Mining tab → Belt Input card |
| Ship/drone mining config | Mining tab | Mining tab → (deferred to future milestone; ore volume input used) |
| Mining belt flip result | Mining tab | Mining tab → ResultPanel |
| Reprocessing facility type | Reprocessing Plant modal | Mining tab → Reprocessing Yields card (inline) |
| Reprocessing skills | Reprocessing Plant → Skills tab | Mining tab → Reprocessing Yields card (inline) |
| Reprocessing item input | Reprocessing Plant modal | Mining tab → Belt Input (ore quantities inferred from belt lines) |
| Reprocessing output | Reprocessing Plant modal | Mining tab → ResultPanel |
| Structure fitting (slots) | Upwell Structure Fitting modal | Facilities tab → Facility Configuration (module type IDs field) |
| Fuel block settings | Upwell Structure Fitting | Facilities tab (fuel block costs included in facility cost index) |
| Asset filter / search | Asset Viewer modal | Assets tab → Filter Bar |
| Asset tree | Asset Viewer modal | Assets tab → Asset List grid |
| Research agent list | Datacores tab | Research tab → Research Agent List |
| Datacore generation data | Datacores tab | Research tab → Research Agent List (Pts/Day, Qty, Value cols) |
| Application settings (all) | Application Settings modal | Settings tab → card-per-section layout |
| Startup preferences | Application Settings → Startup Options | Settings tab → Startup Preferences card |
| Price settings | Application Settings → various | Settings tab → Price Settings card |
| Build defaults | Application Settings → Build Settings | Settings tab → Build Defaults card |
| Proxy settings | Application Settings | Settings tab → Proxy Settings card |
| Implant settings | Application Settings | Settings tab → Character Options card |
| SDE version/import | Application Settings area | Settings tab → Static Data card |
| Database import | Main form header / splash | Settings tab → Application Database card |
| Loading screen / splash | Application startup | Removed (async tab-level loading replaces modal splash) |
