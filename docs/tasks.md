# EVE-IPH Modernisation Tasks

This file turns the current Phase 11 priorities into small, achievable tasks. The sequencing here assumes [modernisation-plan.md](modernisation-plan.md) remains the single source of truth for the broader roadmap.

## Current Focus

Stabilise the application-service and persistence seams that the Avalonia shell depends on before investing in more feature UI.

## Milestone A: Corporation Capability Hardening

- [x] Define the modern corporation capability model.
  - Decide what must be persisted separately from token scopes.
  - Capture legacy requirements for assets, jobs, and blueprints in a single modern contract.
- [x] Add repository fields and migrations for corporation capability state.
  - Persist role-qualified access, not just the existence of a connection.
  - Preserve compatibility with existing character and corporation connection rows.
- [x] Add an ESI-backed capability resolver.
  - Resolve the data needed to determine whether the authenticated character actually has the required corporation roles.
  - Return deterministic failure states when scopes exist but roles do not.
- [x] Update corporation onboarding to validate capabilities during connect.
  - Fail clearly when the character lacks the required roles.
  - Persist the resolved capability state on success.
- [x] Update corporation refresh to revalidate capability state.
  - Downgrade or invalidate stored capabilities when the backing access changes.
  - Keep existing stored corporation data consistent with refreshed access.
- [x] Add focused tests for corporation capability resolution and revalidation.

## Milestone B: Corporation Blueprint Slice

- [x] Define corporation blueprint domain and persistence contracts.
  - Reuse or extend the existing owned blueprint model where possible.
  - Avoid separate UI-only shaping models at this stage.
- [x] Add the missing ESI adapter for corporation blueprints.
  - Map the raw ESI payload to the modern blueprint ownership model.
  - Ensure the adapter works with explicit authenticated character context.
- [x] Add repository and migration support for corporation-owned blueprints.
  - Support replace-or-refresh semantics similar to assets and jobs.
  - Keep personal and corporation ownership queryable through one coherent shape.
- [x] Add blueprint refresh orchestration.
  - Refresh corporation blueprints only when the stored capability state allows it.
  - Keep refresh failures isolated to the blueprint slice.
- [x] Add read-side query support for mixed blueprint ownership.
  - Make the resulting model usable by blueprint management and manufacturing.
- [x] Add integration and domain tests for corporation blueprint persistence and refresh.

## Milestone C: Screen-Facing Application Services

- [x] Inventory the current UI services that still combine orchestration, read shaping, and screen state assumptions.
  - Start with character management, assets, and industry jobs.
- [x] Define a query/command split for shell-facing services.
  - Queries should return stable screen-ready data contracts.
  - Commands should own refresh, connect, delete, and update workflows.
- [x] Refactor character and corporation management onto the new service split.
  - Keep token-status, onboarding, and deletion behavior out of view-models.
- [x] Refactor assets and industry jobs screens to depend only on query/command services.
  - Keep repository and refresh orchestration behind the application-service boundary.
- [x] Add focused tests for the new service contracts.
  - Validate empty, success, degraded, and partial-failure paths.

## Milestone D: Manufacturing Input Foundations

- [x] Inventory the manufacturing inputs that are still legacy-shaped or UI-blocking.
  - Structures, facility bonuses, blueprint ownership/edit state, and any remaining persisted overrides.
- [x] Define the persistence model for structures and facility configuration.
  - Cover what manufacturing and Upwell fitting both need.
- [x] Add repository and migration support for structures and facility settings.
- [x] Define the application/domain services that resolve manufacturing inputs from persisted state.
  - Keep the existing deterministic manufacturing calculators unchanged.
- [x] Add editable blueprint ownership/workflow support needed by the future blueprint-management and manufacturing tabs.
- [x] Add integration tests for structure/facility persistence and manufacturing input resolution.

## Milestone E: Cross-Cutting Validation

- [x] Add onboarding integration tests that cover:
  - character connect
  - corporation connect
  - capability downgrade or loss
  - mixed-owner refresh behavior
- [x] Add delete-path integration tests that cover:
  - character deletion cascade
  - corporation deletion cascade
  - cleanup of assets, jobs, and future blueprint rows
- [x] Add regression tests for mixed ownership queries.
  - Assets and blueprints should behave consistently for personal and corporation owners.
- [x] Run focused test projects after each slice.
- [x] Run a full `Release` solution build at the end of each milestone.

## After The Foundation Tranche

Only start the next UI-heavy slices after Milestones A-E are stable.

Recommended follow-on order:

1. Blueprint management and corporation blueprint views.
2. Manufacturing tab.
3. Upwell structure fitting and facility management.
4. Market prices and update workflows.
5. Shopping list.
6. Mining and reprocessing screens.
