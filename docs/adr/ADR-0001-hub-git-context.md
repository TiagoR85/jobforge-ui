# ADR-0001: Hub compose with git-context builds pinned to release tags

**Status:** Accepted  
**Date:** 2026-10-14

## Decision

`docker-compose.yml` in this repository builds the orchestrator and the runner directly from their GitHub sources pinned to a release tag (`build.context: https://github.com/TiagoR85/jobforge-<service>.git#<tag>`) and builds the UI locally (`build: .`). Current pins: orchestrator `v1.1.1` (business metrics), runner `v1.0.0`. `POSTGRES_PASSWORD` falls back to `jobforge-dev` via `${POSTGRES_PASSWORD:-jobforge-dev}` so the hub `.env.example` carries exactly one key: `OIDC_CLIENT_SECRET=`.

## Context

The alternatives were: (a) a monorepo hosting all three services, (b) published images on GHCR referenced by tag, (c) git-context builds. A monorepo was rejected because the portfolio spec keeps one repository per service — independent CI, releases and READMEs are part of the study value. GHCR was rejected for the demo: pushing images and tagging them adds release plumbing without teaching anything new, and the orchestrator's own dev compose already proved the local-build pattern. Compose v2 natively supports remote git contexts, so (c) costs nothing extra.

## Consequences

- Clean clone → `.env` → `docker compose up` builds the whole family from source (DoD §19, verified end-to-end in the plan's Task 11).
- The pinned tag is the reproducibility contract: compose only picks up orchestrator/runner changes when the pin is moved to a new tag deliberately.
- Builds require GitHub availability at `up` time; Docker layer cache keeps repeat runs fast.
- Switching to GHCR later (backlog) is a two-line change per service.