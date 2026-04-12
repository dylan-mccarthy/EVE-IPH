# EVE-IPH Modernisation Tasks

This file turns the current Phase 11 priorities into small, achievable tasks. The sequencing here assumes [modernisation-plan.md](modernisation-plan.md) remains the single source of truth for the broader roadmap.

## Current Focus

Close out the remaining Phase 11 shell refinements now that the market, shopping-list, and mining/reprocessing production workflows are running in the Avalonia host.

## Tranche Closeout

- [x] Revalidate Milestones I-K with focused milestone tests.
- [x] Revalidate the tranche with the full Avalonia UI test suite and a full `Release` solution build.
- [x] Update the roadmap and short-horizon tracker to reflect the completed market, shopping-list, and mining/reprocessing tranche.

## Milestone L: Settings And Shell State Refinements

- [x] Inventory the shell surfaces that still expose placeholder or purely descriptive status text.
  - Start with the `Settings` tab cards and any remaining update/startup/onboarding shell dialogs.
  - Keep packaging/distribution decisions separate from the first round of shell-state cleanup.
- [x] Define query/command services for the chosen settings-shell refinements.
  - Keep settings persistence and shell-state transitions behind application services.
- [x] Build the first settings/shell refinement slice in Avalonia.
  - Replace at least one placeholder settings/status path with a real workflow or a stateful shell surface.
- [x] Add focused tests for the new settings-shell behavior.
- [x] Run focused validation for the first settings-shell startup preference slice.
- [x] Revalidate Milestone L with a milestone-end `Release` build after the remaining shell refinements land.
  - `dotnet build EVE-IPH-Modern.slnx -c Release` passed on 2026-04-12.

## Milestone M: Character And Corporation Management Refinements

- [x] Inventory the highest-friction gaps in the current character and corporation shell workflows.
  - Prioritize the next refinement that changes real user flow, not cosmetic cleanup.
  - Reuse the existing character-management query/command seams wherever they already own the behavior.
- [x] Define the next character/corporation refinement contracts and commands.
  - Keep corporation capability, token-health, and refresh-state rules out of the view-model layer.
- [x] Build the next character/corporation refinement slice in Avalonia.
  - Target one end-to-end workflow improvement beyond the current connect/refresh/default/remove baseline.
- [x] Add focused tests for that refinement slice.
- [x] Run focused validation for the first character/corporation refinement slice.
- [x] Revalidate Milestone M with a milestone-end `Release` build after the remaining character/corporation refinements land.
  - `dotnet build EVE-IPH-Modern.slnx -c Release` passed on 2026-04-12.

## Milestone N: Startup Loading And Update Workflow Reassessment

- [x] Inventory the startup/loading and update-distribution decisions that are still unresolved now that the core production tabs are live.
  - Separate shell UX changes from packaging/release-pipeline changes.
- [x] Define the shell-facing contracts needed for the chosen reassessment outcome.
  - Prefer a narrow executable improvement over speculative architecture work.
- [x] Implement the first startup/loading or update-shell refinement.
  - Keep the scope honest about what belongs in-app versus in packaging/distribution tooling.
- [x] Add focused tests for the chosen reassessment slice.
- [x] Run focused validation and a milestone-end `Release` build.
  - Shell-owned update checks/apply actions now replace the old placeholder dialog, while release-feed configuration remains an external packaging/distribution concern surfaced clearly in-app.
  - `dotnet test tests/EVE.IPH.UI.Avalonia.Tests/EVE.IPH.UI.Avalonia.Tests.csproj -c Release --filter MainWindowViewModelTests` passed on 2026-04-12.
  - `dotnet build EVE-IPH-Modern.slnx -c Release` passed on 2026-04-12.

## After This Tranche

1. Reassess the remaining parity gaps against the legacy UI once the shell refinements above are complete.
2. Prepare the final Phase 11 closeout and Phase 12 decommissioning checklist.
3. Decide whether any remaining workflow gaps should be filled in Avalonia before the legacy project is archived.
