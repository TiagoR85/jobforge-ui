# ADR-0004 — Dashboard shell is copied, not shared

## Status
Accepted (2026-10-26)

## Context
The jobforge console needed the look and behaviour of DashboardTheKingThePower.
The dashboard repository is private (a sanitised mirror is public) and carries no
licence, while jobforge-ui is public and MIT. A project reference, submodule or
shared NuGet package would either break clean-clone builds and CI or force the
dashboard to be refactored — and the dashboard must remain untouched.

## Decision
Copy the shell primitives (layout, sidebar, header, theme tokens, toast, modal,
command palette) into `src/JobForge.Ui` and adapt namespaces, strings and auth
dependencies. The dashboard stays a read-only reference implementation.

## Consequences
- jobforge-ui builds from a clean clone with no cross-repository coupling.
- The copy can drift from the dashboard; updates are manual and deliberate.
- A shared component library is revisited only when a second real consumer exists;
  at that point this ADR is superseded.
