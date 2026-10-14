# ADR-0002: Confidential client with in-memory token + 5s polling

**Status:** Accepted  
**Date:** 2026-10-14

## Decision

The console authenticates as the confidential client `jobforge-console` (client_credentials, scopes `jobs:read` + `jobs:write`, secret exclusively from `OIDC_CLIENT_SECRET`, fail-fast at boot). The access token lives in server memory only, refreshed 60s before `expires_in`. The executions list polls the REST API every 5s (`Ui:RefreshSeconds`) — no SignalR, no refresh tokens.

## Context

Alternatives: a public/implicit client storing the token in the browser (rejected: XSS blast radius, no server secret), authorization-code flow with refresh tokens (rejected: the demo has no user identities — login is out of scope and refresh tokens are listed as backlog in spec §17), and push updates over SignalR (rejected explicitly by spec §12: "auto-refresh 5s (polling, sem SignalR)"). Polling matches the orchestrator's read model: `GET /v1/executions` is cheap, paginated and filterable.

## Consequences

- Zero token exposure in the browser; the UI is a pure API client (no JWT validation code of its own).
- Polling is bounded to the executions page (the loop is cancelled in `IAsyncDisposable`); the jobs list loads on demand.
- The API keeps one `JobExecution` row per execution, so `/executions/{id}` renders the lifecycle timeline (scheduled/started/finished + attempt/HTTP/duration/error) — per-attempt history is backlog, recorded in the README.
- Every deployment must inject the client secret; missing `OIDC_CLIENT_SECRET` fails the compose up with `:?set in .env`.