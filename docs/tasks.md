# EVE-IPH Modernisation Tasks

This file turns the current Phase 11 priorities into small, achievable tasks. The sequencing here assumes [modernisation-plan.md](modernisation-plan.md) remains the single source of truth for the broader roadmap.

## Current Focus

Continue Phase 11 after the completed blueprint, manufacturing, and facilities tranche by rebuilding the remaining production workflows on top of the current shell seams.

## Tranche Closeout

- [x] Revalidate Milestones F-H with focused milestone tests.
- [x] Revalidate the tranche with a full `Release` solution build.
- [x] Update the roadmap and short-horizon tracker to reflect the completed blueprint, manufacturing, and facilities tranche.

## Milestone I: Market Prices And Update Workflows

- [ ] Inventory the legacy market/update workflows that still need a first modern shell slice.
  - Bound the MVP to price refresh, update progress/state, and the first usable price display surface.
  - Keep deferred legacy breadth explicit: regional comparisons, historical graphs, and niche filters can follow later.
- [ ] Define query/command services for market data and update orchestration.
  - Keep refresh workflows and update-state transitions out of view-models.
  - Reuse existing domain and infrastructure seams where they already exist.
- [ ] Build the first Avalonia market/update view-model and view.
  - Support loading prices, running an update workflow, and surfacing deterministic status/error states.
  - Keep the first slice honest about any still-missing market derivations.
- [ ] Add focused tests for market/update query, command, and view-model behavior.
- [ ] Run focused validation and a milestone-end `Release` build.

## Milestone J: Shopping List Shell Integration

- [ ] Inventory the shopping-list workflows already supported by the extracted domains.
  - Start with build/buy outputs and the first persisted or exportable list shape.
  - Keep later convenience features separate from the first end-to-end shell slice.
- [ ] Define shopping-list screen contracts and commands.
  - Reuse manufacturing inputs and current ownership/facility workflows where needed.
  - Keep any persistence/export orchestration behind application services.
- [ ] Build the first Avalonia shopping-list view-model and view.
  - Support loading a list, projecting the first actionable rows, and handling empty/error states.
- [ ] Add focused tests for shopping-list shell behavior.
- [ ] Run focused validation and a milestone-end `Release` build.

## Milestone K: Mining And Reprocessing Shell Slice

- [ ] Inventory the first mining/reprocessing workflows that already have usable domain seams.
  - Prefer one narrow vertical slice over partial parity across both legacy tabs.
- [ ] Define query/command services for the chosen mining/reprocessing MVP.
- [ ] Build the first Avalonia mining/reprocessing view-model and view.
- [ ] Add focused tests for the mining/reprocessing shell slice.
- [ ] Run focused validation and a milestone-end `Release` build.

## After This Tranche

1. Settings refinements beyond the current shell status surface.
2. Character and corporation management refinements beyond the current connect/refresh/default/remove workflow.
3. Reassess startup/loading and update-distribution workflows after the remaining tabs are running.
