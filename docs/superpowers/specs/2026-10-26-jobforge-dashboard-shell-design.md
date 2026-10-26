# Design — console jobforge com o shell do DashboardTheKingThePower

> Spec da primeira evolução do frontend do `jobforge-ui` (v1.1.0).
> Origem: diálogo de brainstorming de 2026-10-26.
> Repositório de referência visual: `DashboardTheKingThePower` — **somente leitura**, não é alterado.

## 1. Objetivo

Substituir o console Bootstrap atual por um console com o design system e o shell do
DashboardTheKingThePower, acrescentando visão geral e página de operação. O resultado
serve como **template por cópia** para as próximas aplicações: o shell vive dentro do
jobforge e pode ser copiado, sem dependência entre repositórios.

Critérios de sucesso:

- O console parece a mesma família visual do Dashboard (sidebar, header, dark mode,
  acentos, toasts, command palette), com marca jobforge.
- Existe uma página inicial com KPIs e gráficos reais (não dados fictícios).
- Existe uma página de operação que mostra a saúde da ligação ao orchestrator.
- Interface bilingue PT-BR/EN.
- Nenhuma alteração ao orchestrator, ao runner ou ao Dashboard.
- Gate de cobertura ≥80% mantém-se; `docker compose up` continua a funcionar.

## 2. Decisões

| # | Decisão | Motivo |
|---|---|---|
| 1 | Manter **Blazor Web App / Interactive Server** | O segredo OIDC é de cliente confidencial; WASM exigiria BFF. O shell do Dashboard é Razor + Tailwind, independente do render mode. |
| 2 | **Cópia documentada** do shell, não biblioteca partilhada | O Dashboard é privado e o jobforge é público/MIT; qualquer referência cruzada quebraria o clone limpo e o CI. Extrair RCL só quando existir um segundo consumidor real. |
| 3 | Dashboard **não é alterado** | Restrição explícita do utilizador. Serve de referência de código. |
| 4 | Tailwind v4 com **CSS compilado commitado** | Mantém o Docker sem Node; o build de CSS corre em job próprio de CI. |
| 5 | **Blazor-ApexCharts 7.0.0** | Mesma biblioteca do template; evita reimplementar eixos, tooltips e responsividade. É a única dependência nova. |
| 6 | i18n com `IStringLocalizer` + `.resx` | Recurso nativo do ASP.NET Core, testável, sem pacote adicional. |
| 7 | Overview calculado no cliente a partir dos endpoints existentes | Zero mudanças no backend. A janela é limitada e a UI diz isso. |

## 3. Arquitetura de componentes

Tudo dentro de `src/JobForge.Ui/`:

```
Components/
  Shell/            MainLayout, Sidebar, Header, BottomNav, NavRoute
  Ui/               Card, Button, Badge, Modal, ToastContainer, CommandPalette
  Charts/           AreaChart, DonutChart, Sparkline
  Theme/            ThemeToggle, AccentPicker
  Pages/            Overview, Jobs, NewJob, Executions, ExecutionDetail, Operations
  StatusBadge.razor (mantém-se, passa a usar Badge)
Localization/       SharedResources.resx, SharedResources.pt-BR.resx
Services/           ToastService, OverviewMetrics (puro)
Styles/theme.css    tokens (cores, radius, tipografia)
wwwroot/js/theme.js sidebar/tema/acento/resize
css/tailwind.input.css
package.json        scripts.build:css
wwwroot/css/tailwind.css (gerado e commitado)
```

Copiado do Dashboard com adaptações mínimas:

- `Layout/MainLayout.razor` → remove auth, timer de sessão e redirect para login.
- `Layout/Sidebar.razor`, `Header.razor`, `BottomNav.razor`, `NavRoute.cs`.
- `Components/ui/{Card,Button,Badge,Modal,ToastContainer,CommandPalette}.razor`.
- `Components/theme/{ThemeToggle,AccentPicker}.razor`.
- `Components/charts/{AreaChart,DonutChart,Sparkline}.razor` e `ChartDefaults.cs`.
- `Styles/theme.css` (tokens), `wwwroot/js/theme.js`, `Services/ToastService.cs`.

Não se copia: `Login`, `Register`, `AuthLayout`, `AuthStateProvider`, `UserService`,
`StorageService` de dados fictícios, páginas de e-commerce/CRM e respetivos modelos.

Adaptações obrigatórias na cópia:

- Namespaces `DashboardTheKingThePower.*` → `JobForge.Ui.*`.
- Strings hardcoded → chaves de localização.
- Componentes de demonstração com dados fixos (ex.: `AreaChart` com "Visão Geral")
  passam a receber séries por parâmetro.
- `MainLayout` deixa de depender de `AuthStateProvider`.
- Bootstrap sai do `App.razor`; entram `theme.css`, `tailwind.css`, Inter (mesmo link
  Google Fonts do Dashboard, com fallback `system-ui`) e `theme.js`.
- `Error.razor` e `NotFound.razor` mantêm-se, restilizados com o sistema novo;
  `ReconnectModal` mantém-se funcional.

## 4. Rotas e comportamento

| Rota | Conteúdo | Dados |
|---|---|---|
| `/` | Visão geral: 4 KPIs, área de execuções por hora, donut por status, 10 execuções recentes, atalhos | jobs + execuções + contagens por status |
| `/jobs` | Lista com busca por nome (cliente), último estado, Run now, Disable | `GET /v1/jobs` |
| `/jobs/new` | Formulário com presets de cron, pré-visualização das próximas 3 execuções (Cronos no servidor, UTC), validação de JSON, toast no sucesso | `POST /v1/jobs` |
| `/executions` | Tabela, filtro por status, paginação, refresh 5s, tempo relativo | `GET /v1/executions` |
| `/executions/{id}` | Timeline de ciclo de vida (mantém o desvio conhecido: uma linha por execução) | `GET /v1/executions/{id}` |
| `/executions?status=dead` | Mesma página; entrada "Dead letters" na nav (sem duplicar ecrã) | idem |
| `/operations` | Saúde live/ready, BaseUrl, ClientId, scopes, intervalo de refresh, link para `/metrics` | `/health/live`, `/health/ready`, config |

Regras:

- `StatusBadge` mapeia `pending` (cinza), `running` (azul), `succeeded` (verde),
  `failed` (âmbar), `dead` (vermelho).
- Ações destrutivas (Disable) pedem confirmação via `Modal`.
- Erros de API aparecem como toast + alerta inline; nunca derrubam a página.
- O segredo OIDC nunca é renderizado, nem em `/operations`.
- Refresh do overview: 30s + botão manual. Refresh de execuções: 5s (`Ui:RefreshSeconds`).
- Guarda de reentrância em todos os loops de refresh.

## 5. Agregação do overview

`GET /v1/executions` limita `size` a 100 e devolve `{ page, size, total, items }`.

- KPIs:
  - **Jobs ativos** = jobs com `enabled = true`.
  - **Taxa de sucesso** = `succeeded / (succeeded + failed + dead)` na janela de 100.
  - **Dead letters** = `Total` de `GET /v1/executions?status=dead&size=1`.
  - **Duração média** = média de `finishedAt - startedAt` nas execuções concluídas da janela.
- Gráfico de área: execuções agrupadas por hora, cobrindo no máximo as últimas 24h
  da janela de 100, com legenda honesta "últimas 100 execuções".
- Donut: contagem por status na janela.
- Tudo numa classe pura `OverviewMetrics.From(...)` com testes unitários.
- Estados vazios explícitos (sem jobs, sem execuções) em vez de gráficos vazios.

## 6. Localização

- `builder.Services.AddLocalization()` + `app.UseRequestLocalization(...)`.
- Culturas suportadas: `pt-BR` (padrão) e `en`.
- Deteção: cookie `.AspNetCore.Culture` → `Accept-Language` → `pt-BR`.
- Seletor no header chama `GET /culture/set?c=<culture>&redirect=<path>`; o endpoint
  grava o cookie e redireciona (reload cria novo circuit com a cultura certa).
- `<html lang>` vem da cultura atual.
- Números, datas e percentagens formatados com `CultureInfo.CurrentCulture`.
- Chaves em `SharedResources.resx` (EN, fallback) e `SharedResources.pt-BR.resx`.

## 7. Build e CI

- `src/JobForge.Ui/package.json` + `package-lock.json` com `build:css` (`@tailwindcss/cli`).
- `css/tailwind.input.css` com `@source` para `.razor`, `.cs`, `App.razor` e JS.
- `wwwroot/css/tailwind.css` é gerado e **commitado**; o Docker não precisa de Node.
- Novo job `css-check` no `ci.yml`: `setup-node`, `npm ci`, `npm run build:css`,
  falha se `git diff --exit-code` acusar divergência.
- `Directory.Packages.props` ganha `Blazor-ApexCharts` 7.0.0.
- `Dockerfile` inalterado (o CSS já vai no contexto de build).

## 8. Testes

Existentes: mantêm-se; o contrato é `data-testid` (ADR-0003), não classes CSS.

Novos testes:

- `OverviewMetricsTests`: janela vazia, taxa de sucesso, duração média, agrupamento por hora,
  divisão por zero.
- `OverviewPageTests`: renderiza KPIs, estados vazios, erro de API.
- `OperationsPageTests`: orchestrator saudável e indisponível; segredo ausente do markup.
- `LocalizationTests`: chave resolve em `pt-BR` e `en`; fallback EN; cultura muda formatação.
- `ShellTests`: nav ativa, toggle de tema, command palette abre/fecha.
- `CultureEndpointTests`: a função que grava o cookie define a cultura certa e preserva
  o redirect (unitário, sem `WebApplicationFactory`).

Sem containers: bUnit + `FakeJobForgeClient` + WireMock onde já é usado.

## 9. Entrega

- ADR-0004 (EN): shell copiado do Dashboard, origem read-only, sem biblioteca partilhada.
- README atualizado: rotas, stack, i18n, como apontar para outro orchestrator.
- Versão **1.1.0**, tag anotada `v1.1.0`, release no GitHub.
- Sem `CHANGELOG.md` retroativo (o repo nunca teve; a release é o registo).

## 10. Fora de escopo

- Multi-tenant / noção de "aplicação" no backend.
- Replay de DLQ (exigiria endpoint novo no orchestrator).
- Login de utilizador final no console.
- WebSockets/SignalR para execuções em tempo real (mantém-se polling).
- Biblioteca partilhada entre repositórios.
- Alterações ao Dashboard, orchestrator ou runner.
- Histórico completo de execuções (limite de 100 da API).

## 11. Riscos

| Risco | Mitigação |
|---|---|
| Divergência futura entre shell copiado e Dashboard | Aceite; ADR-0004 documenta o custo. |
| ApexCharts em Server piscar no polling | Só atualizar a série quando os dados mudam. |
| CSS commitado dessincronizado | Job `css-check` falha o CI em divergência. |
| Resx mal preenchido numa cultura | Teste que percorre todas as chaves e falha em falta. |
| Overview induzir em erro sobre o histórico | Rótulo explícito da janela de 100 execuções. |
| Aumento do bundle/JS do console | Aceite; ApexCharts é a lib do template e corre no cliente. |
