# ADR-0003: data-testid as the stable test contract

**Status:** Accepted  
**Date:** 2026-10-14

## Decision

Every element the bUnit suite asserts on carries a `data-testid` from a fixed inventory (`job-row`, `status-badge`, `submit-button`, `execution-row`, `timeline-item`, …). Assertions target testids or structure scoped under them — never CSS classes or copy. `StatusBadge` additionally exposes `data-status`. Renaming/removing a testid requires updating the tests, this ADR's inventory and the README table in the same commit.

## Context

Alternatives: CSS-class selectors (break on every restyle), text selectors (break on copy edits), and role/accessible-name queries (right for accessibility assertions, insufficient for state like the current status). The pattern is inherited from `logpipe-ui` (its ADR-0003) and mandated by spec §12 for this family.

## Consequences

- Design and markup can change freely without touching the suite; testids are inert attributes with no UX or security impact.
- The inventory below is the contract:

| Page | testids |
|---|---|
| jobs `/` | `job-row`, `job-run-button`, `job-disable-button`, `job-last-execution`, `new-job-link`, `executions-link`, `jobs-empty`, `error-alert` |
| form `/jobs/new` | `job-name-input`, `job-url-input`, `job-payload-input`, `job-cron-input`, `submit-button`, `form-error`, `name-error`, `url-error`, `payload-error`, `cron-error` |
| list `/executions` | `execution-row`, `status-filter`, `pagination-prev`, `pagination-next`, `page-info`, `executions-empty`, `error-alert` |
| detail `/executions/{id}` | `detail-status`, `timeline-item`, `attempt`, `http-status`, `duration`, `error-text`, `execution-not-found`, `refresh-button` |
| shared | `status-badge` (with `data-status`) |