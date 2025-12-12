# Modernization Plan – Web Replacement (ASP.NET Core + React)

## Goal
Replace the legacy VB.NET WinForms application with a web-based application:
- Backend: ASP.NET Core minimal APIs + SQLite
- Frontend: React SPA (Vite + TypeScript)

The WinForms code remains reference-only while we reach parity.

## Guiding Principles
- **Parity-first:** reproduce legacy outcomes and workflows; optimize later.
- **Backend contracts drive UI:** stable DTOs/endpoints first, then React.
- **Explicit state:** avoid global mutable state patterns from WinForms.
- **Manual market sync:** Blueprints/manufacturing views read cached prices only; dedicated Market Data page performs refresh.
- **Server is source of truth for calculations:** manufacturing math runs on the backend; React renders results.

## Current Status (Checkpoint — December 12, 2025)
See `tasks.md` for the detailed phase checklist. High level:
- Phase 1–3 (Auth + character data pages) complete
- Phase 4.1–4.2 (Blueprints + market cache/refresh UI) complete
- Phase 4.3 (Manufacturing Calculator) is now server-authoritative:
  - Calculator is a thin client rendering server-calculated totals, profit, time, and IPH
  - Component + recursive raw breakdown included in the manufacturing response
  - Legacy-shaped parity increments implemented behind explicit request knobs:
    - facility material/time multipliers
    - product/material market-mode semantics (buy/sell order variants)
    - fees/taxes + job cost model (incl. adjusted-price EIV support)
    - profit cost basis selection (components vs raw)
    - opt-in auto Build/Buy components with optional excess sellback

## Roadmap (Phased)

### Phase 4: Manufacturing Calculator
**Target parity (legacy manufacturing columns/features include):** material cost, total cost, taxes/broker fees, job/installation cost, build time, ISK/hr, ROI, SVR/price trend signals, facility/system index modifiers, and T2 invention costs.

**Near-term milestones**
1. ✅ Move manufacturing calculations to the server (API becomes authoritative; UI becomes a thin client).
2. ✅ Migrate the calculator UI to call the manufacturing endpoint and render server-calculated breakdowns.
3. ✅ Show raw-material breakdown (recursive) and side-by-side totals (components vs raw) priced from cached market data.
4. ✅ Add manufacturing “extras” incrementally on the server: fees/taxes, build time, job/installation cost, and profit metrics.

**Next parity frontier (planned)**
1. Manual Build/Buy overrides (legacy BBList behavior) + tests
2. Deeper legacy toggles (e.g., “BP not owned” suggestions) if still desired
3. Shopping list integration using selected cost basis (components/raw/build-buy)

### Phase 5: Mining & Resources
Port the ore/ice/gas and reprocessing math used by the legacy app (e.g., reprocessing plant workflows).

### Phase 6: Shopping List & Planning
Aggregate materials across builds, export formats (multi-buy, CSV/SSV), and integrate with owned assets where applicable.

## Major Risks / Decisions
- **Market buy orders:** batch region orders appear to omit buy orders; cached `buyPrice` may remain 0. We treat sell prices as authoritative for material acquisition cost unless we add an alternate strategy.
- **Scope creep:** legacy has many knobs (facilities, indices, rigs, skills, invention, SVR); we’ll port in small verified increments.

## Success Criteria
- A user can do the end-to-end manufacturing workflow in the web app without opening WinForms:
  - pick blueprint → see component and raw material costs → see profit/IPH with relevant modifiers → generate a shopping list.
