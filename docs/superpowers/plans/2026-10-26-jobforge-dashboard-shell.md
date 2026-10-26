# Console jobforge com shell do Dashboard — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir o console Bootstrap do `jobforge-ui` por um console com o shell e o design system do DashboardTheKingThePower, com visão geral, operação e interface PT-BR/EN.

**Architecture:** Blazor Web App (.NET 10) com Interactive Server global, sem BFF. Tailwind v4 com CSS compilado commitado. Shell copiado do Dashboard (repositório de origem somente leitura), componentes Razor adaptados ao namespace `JobForge.Ui`, localização via `IStringLocalizer` + `.resx`, gráficos com Blazor-ApexCharts.

**Tech Stack:** .NET 10.0.301, Blazor Web App, Tailwind CSS 4.3.x (CLI), Blazor-ApexCharts 7.0.0, xUnit + bUnit 2.11.3, WireMock.Net.

## Global Constraints

- .NET SDK `10.0.301` (global.json); `TreatWarningsAsErrors=true`.
- Gestão central de pacotes: qualquer `PackageReference` novo exige `PackageVersion` em `Directory.Packages.props`.
- **Não alterar** `D:\Projetos\DashboardTheKingThePower`, `jobforge-orchestrator` ou `jobforge-runner`.
- Nenhuma mudança de contrato com o orchestrator: `IJobForgeClient` mantém a assinatura atual.
- `data-testid` é contrato de teste (ADR-0003): nunca remover um testid existente sem substituir por equivalente.
- O segredo OIDC nunca aparece em markup, log ou teste que o imprima.
- Cultura padrão `pt-BR`; fallback EN. Toda string nova passa por `IStringLocalizer<SharedResources>`.
- CSS gerado (`wwwroot/css/tailwind.css`) é commitado; o `Dockerfile` não usa Node.
- Cobertura de linha ≥80% (gate do CI).
- Commits convencionais, um por task.

---

### Task 0: Pre-flight

**Files:** nenhum.

**Interfaces:**
- Consumes: nada.
- Produces: baseline verde e card no Hub.

- [ ] **Step 0.1: Confirmar baseline**

Run:
```powershell
cd D:\Projetos\jobforge-ui
dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false
```
Expected: todos os testes passam. Se falhar, parar e reportar antes de alterar código.

- [ ] **Step 0.2: Criar card no Hub (Kanban-first)**

Run:
```powershell
$projects = Invoke-RestMethod http://127.0.0.1:5002/api/projects -ErrorAction SilentlyContinue
$id = ($projects | Where-Object { $_.path -like '*jobforge-ui*' } | Select-Object -First 1).id
if ($id) {
  Invoke-RestMethod -Method Post http://127.0.0.1:5002/api/tasks -ContentType 'application/json' `
    -Body (@{ project_id = $id; title = 'Console com shell do Dashboard (v1.1.0)'; status = 'todo' } | ConvertTo-Json)
}
```
Expected: card criado. Se o Hub estiver desligado, registar "Hub indisponível" e continuar (não bloqueia).

---

### Task 1: Toolchain Tailwind + tokens

**Files:**
- Create: `src/JobForge.Ui/package.json`
- Create: `src/JobForge.Ui/package-lock.json` (gerado por `npm install`)
- Create: `src/JobForge.Ui/css/tailwind.input.css`
- Create: `src/JobForge.Ui/wwwroot/css/theme.css`
- Create: `src/JobForge.Ui/wwwroot/css/tailwind.css` (gerado, commitado)
- Create: `src/JobForge.Ui/wwwroot/js/theme.js`
- Create: `src/JobForge.Ui/wwwroot/js/boot.js`
- Modify: `src/JobForge.Ui/wwwroot/app.css`
- Modify: `src/JobForge.Ui/Components/App.razor`
- Delete: `src/JobForge.Ui/wwwroot/lib/bootstrap/` (árvore inteira)
- Test: `tests/JobForge.Ui.Tests/ThemeAssetsTests.cs`

**Interfaces:**
- Consumes: nada.
- Produces: funções JS globais `getTheme()`, `toggleTheme()`, `getAccent()`, `setAccent(string)`, `getSidebarCollapsed()`, `setSidebarCollapsed(bool)`, `getIsMobile()`, `addResizeListener(dotNetRef)`, `removeResizeListener()`, `addKeydownListener(dotNetRef)`; classes CSS de token (`--primary`, `--background`, `--card`, `--border`, `--muted-foreground`, `--sidebar`, `--sidebar-accent`, `--ring`, `--chart-1`…`--chart-5`, `--destructive`); classe `.dark`.

- [ ] **Step 1.1: Escrever o teste que falha**

Create `tests/JobForge.Ui.Tests/ThemeAssetsTests.cs`:
```csharp
namespace JobForge.Ui.Tests;

public sealed class ThemeAssetsTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JobForge.Ui.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }

    [Fact]
    public void Generated_tailwind_css_contains_design_tokens()
    {
        var path = Path.Combine(RepoRoot(), "src", "JobForge.Ui", "wwwroot", "css", "tailwind.css");
        Assert.True(File.Exists(path), $"missing {path}; run npm run build:css");

        var css = File.ReadAllText(path);
        Assert.True(css.Length > 1000, "tailwind output looks empty");
        Assert.Contains("--tw-", css);
    }

    [Fact]
    public void Theme_tokens_define_light_and_dark_surfaces()
    {
        var path = Path.Combine(RepoRoot(), "src", "JobForge.Ui", "wwwroot", "css", "theme.css");
        Assert.True(File.Exists(path));

        var css = File.ReadAllText(path);
        Assert.Contains("--primary", css);
        Assert.Contains("--sidebar", css);
        Assert.Contains(".dark", css);
    }

    [Fact]
    public void Theme_js_exposes_the_functions_the_shell_uses()
    {
        var path = Path.Combine(RepoRoot(), "src", "JobForge.Ui", "wwwroot", "js", "theme.js");
        Assert.True(File.Exists(path));

        var js = File.ReadAllText(path);
        foreach (var fn in new[]
                 {
                     "function getTheme", "function toggleTheme", "function getAccent", "function setAccent",
                     "function getSidebarCollapsed", "function setSidebarCollapsed", "function getIsMobile",
                     "function addResizeListener", "function removeResizeListener", "function addKeydownListener"
                 })
        {
            Assert.Contains(fn, js);
        }
    }
}
```

- [ ] **Step 1.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter ThemeAssetsTests`
Expected: FAIL — ficheiros inexistentes.

- [ ] **Step 1.3: Criar o pipeline Tailwind**

Create `src/JobForge.Ui/package.json`:
```json
{
  "name": "jobforge-ui",
  "private": true,
  "scripts": {
    "build:css": "tailwindcss -i css/tailwind.input.css -o wwwroot/css/tailwind.css --minify"
  },
  "devDependencies": {
    "@tailwindcss/cli": "^4.3.3",
    "tailwindcss": "^4.3.3"
  }
}
```

Create `src/JobForge.Ui/css/tailwind.input.css`:
```css
@import "tailwindcss";

/* dark mode por classe no <html class="dark"> */
@custom-variant dark (&:where(.dark, .dark *));

/* o extractor precisa de ver classes em razor, C# (mapas de variantes) e js */
@source "../**/*.razor";
@source "../**/*.cs";
@source "../wwwroot/js/*.js";
```

Run:
```powershell
cd D:\Projetos\jobforge-ui\src\JobForge.Ui
npm install
npm run build:css
```
Expected: `package-lock.json` criado; `wwwroot/css/tailwind.css` criado.

- [ ] **Step 1.4: Criar os tokens**

Create `src/JobForge.Ui/wwwroot/css/theme.css` (copiar os tokens de
`DashboardTheKingThePower/src/DashboardTheKingThePower/Styles/theme.css` e remover
`--popover`/`--accent` não usados; manter exatamente estes valores):
```css
:root {
  color-scheme: light;
  --primary: oklch(0.55 0.19 240);
  --primary-foreground: oklch(1 0 0);
  --background: oklch(0.985 0 0);
  --foreground: oklch(0.145 0 0);
  --card: oklch(1 0 0);
  --card-foreground: oklch(0.145 0 0);
  --secondary: oklch(0.96 0 0);
  --secondary-foreground: oklch(0.205 0 0);
  --muted: oklch(0.96 0 0);
  --muted-foreground: oklch(0.556 0 0);
  --destructive: oklch(0.577 0.245 27.325);
  --border: oklch(0.90 0 0);
  --input: oklch(0.90 0 0);
  --ring: oklch(0.55 0.19 240);
  --chart-1: oklch(0.55 0.19 240);
  --chart-2: oklch(0.55 0.19 160);
  --chart-3: oklch(0.55 0.19 280);
  --chart-4: oklch(0.55 0.19 350);
  --chart-5: oklch(0.55 0.19 50);
  --sidebar: oklch(0.145 0 0);
  --sidebar-foreground: oklch(0.985 0 0);
  --sidebar-primary: oklch(0.55 0.19 240);
  --sidebar-accent: oklch(0.205 0 0);
  --sidebar-border: oklch(0.205 0 0);
  --radius: 0.5rem;
}

.dark {
  color-scheme: dark;
  --background: oklch(0.13 0 0);
  --foreground: oklch(0.985 0 0);
  --card: oklch(0.16 0 0);
  --card-foreground: oklch(0.985 0 0);
  --secondary: oklch(0.22 0 0);
  --secondary-foreground: oklch(0.985 0 0);
  --muted: oklch(0.22 0 0);
  --muted-foreground: oklch(0.65 0 0);
  --border: oklch(0.25 0 0);
  --input: oklch(0.25 0 0);
  --ring: oklch(0.55 0.19 240);
  --chart-1: oklch(0.55 0.19 240);
  --chart-2: oklch(0.55 0.19 160);
  --chart-3: oklch(0.55 0.19 280);
  --chart-4: oklch(0.55 0.19 350);
  --chart-5: oklch(0.55 0.19 50);
  --sidebar: oklch(0.16 0 0);
  --sidebar-foreground: oklch(0.985 0 0);
  --sidebar-primary: oklch(0.55 0.19 240);
  --sidebar-accent: oklch(0.22 0 0);
  --sidebar-border: oklch(0.25 0 0);
}

body {
  margin: 0;
  font-family: Inter, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;
  background-color: var(--background);
  color: var(--foreground);
}
```

- [ ] **Step 1.5: Copiar o JS de tema**

Create `src/JobForge.Ui/wwwroot/js/theme.js` copiando de
`DashboardTheKingThePower/src/DashboardTheKingThePower/wwwroot/js/theme.js` **apenas**:
`getTheme`, `setTheme`, `initTheme`, `toggleTheme`, `getAccent`, `setAccent`, `initAccent`,
`addKeydownListener`, `getSidebarCollapsed`, `setSidebarCollapsed`, `getIsMobile`,
`addResizeListener`, `removeResizeListener`.
Não copiar `initKanbanDrag`, `destroyKanbanDrag` nem `_sortableInstances`.

Create `src/JobForge.Ui/wwwroot/js/boot.js`:
```js
initTheme();
initAccent();

document.addEventListener('DOMContentLoaded', function () {
    var dismiss = document.querySelector('#blazor-error-ui .dismiss');
    if (dismiss) {
        dismiss.addEventListener('click', function () {
            document.getElementById('blazor-error-ui').style.display = 'none';
        });
    }
});
```

- [ ] **Step 1.6: Ligar tudo no App.razor e limpar Bootstrap**

Replace `src/JobForge.Ui/Components/App.razor`:
```razor
<!DOCTYPE html>
<html lang="@System.Globalization.CultureInfo.CurrentUICulture.Name" class="dark">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <ResourcePreloader />
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="@Assets["css/tailwind.css"]" />
    <link rel="stylesheet" href="@Assets["css/theme.css"]" />
    <link rel="stylesheet" href="@Assets["app.css"]" />
    <link rel="stylesheet" href="@Assets["JobForge.Ui.styles.css"]" />
    <ImportMap />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>

<body>
    <Routes @rendermode="InteractiveServer" />
    <ReconnectModal />
    <script src="@Assets["js/theme.js"]"></script>
    <script src="@Assets["js/boot.js"]"></script>
    <script src="@Assets["_framework/blazor.web.js"]"></script>
</body>

</html>
```

Replace `src/JobForge.Ui/wwwroot/app.css`:
```css
html, body { height: 100%; }

#blazor-error-ui {
    display: none;
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    z-index: 1000;
    padding: 1rem;
    background: var(--card);
    border-top: 1px solid var(--border);
    color: var(--foreground);
}

#blazor-error-ui .dismiss { cursor: pointer; float: right; }

::-webkit-scrollbar { width: 8px; height: 8px; }
::-webkit-scrollbar-thumb { background: var(--border); border-radius: 999px; }
```

Run:
```powershell
cd D:\Projetos\jobforge-ui
git rm -r src/JobForge.Ui/wwwroot/lib/bootstrap
```

- [ ] **Step 1.7: Correr build e testes**

Run:
```powershell
cd D:\Projetos\jobforge-ui\src\JobForge.Ui
npm run build:css
cd D:\Projetos\jobforge-ui
dotnet build JobForge.Ui.slnx
dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter ThemeAssetsTests
```
Expected: build 0 erros; 2 testes PASS.

- [ ] **Step 1.8: Commit**

```powershell
git add src/JobForge.Ui/package.json src/JobForge.Ui/package-lock.json src/JobForge.Ui/css src/JobForge.Ui/wwwroot/css src/JobForge.Ui/wwwroot/js src/JobForge.Ui/wwwroot/app.css src/JobForge.Ui/Components/App.razor src/JobForge.Ui/wwwroot/lib tests/JobForge.Ui.Tests/ThemeAssetsTests.cs
git commit -m "feat(ui): tailwind pipeline and dashboard design tokens"
```

---

### Task 2: Localização PT-BR/EN

**Files:**
- Create: `src/JobForge.Ui/Resources/SharedResources.cs`
- Create: `src/JobForge.Ui/Resources/SharedResources.resx`
- Create: `src/JobForge.Ui/Resources/SharedResources.pt-BR.resx`
- Create: `src/JobForge.Ui/Localization/CultureCookie.cs`
- Modify: `src/JobForge.Ui/Program.cs`
- Modify: `tests/JobForge.Ui.Tests/UiTestContext.cs` (novo)
- Test: `tests/JobForge.Ui.Tests/LocalizationTests.cs`
- Test: `tests/JobForge.Ui.Tests/CultureCookieTests.cs`

**Interfaces:**
- Consumes: nada.
- Produces: `JobForge.Ui.SharedResources` (marker); `CultureCookie.BuildValue(string)`, `CultureCookie.IsSupported(string?)`, `CultureCookie.SafeRedirect(string?)`, `CultureCookie.Set(HttpResponse, string, bool)`; endpoint `GET /culture/set?c=<culture>&redirect=<path>`; `UiTestContext` base class para bUnit.

- [ ] **Step 2.1: Escrever os testes que falham**

Create `tests/JobForge.Ui.Tests/LocalizationTests.cs`:
```csharp
using System.Collections;
using System.Globalization;
using System.Resources;
using JobForge.Ui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace JobForge.Ui.Tests;

public sealed class LocalizationTests
{
    private static IStringLocalizer<SharedResources> Localizer()
        => new ServiceCollection()
            .AddLogging()
            .AddLocalization(o => o.ResourcesPath = "Resources")
            .BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<SharedResources>>();

    [Fact]
    public void Resolves_portuguese_and_english()
    {
        var localizer = Localizer();

        using (new CultureScope("pt-BR"))
        {
            Assert.Equal("Visão geral", localizer["nav_overview"].Value);
        }
        using (new CultureScope("en"))
        {
            Assert.Equal("Overview", localizer["nav_overview"].Value);
        }
    }

    [Fact]
    public void Every_english_key_has_a_portuguese_translation()
    {
        var assembly = typeof(SharedResources).Assembly;
        var neutral = new ResourceManager("JobForge.Ui.Resources.SharedResources", assembly)
            .GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var pt = new ResourceManager("JobForge.Ui.Resources.SharedResources", assembly)
            .GetResourceSet(new CultureInfo("pt-BR"), createIfNotExists: true, tryParents: true)!;

        foreach (DictionaryEntry entry in neutral)
        {
            Assert.False(string.IsNullOrWhiteSpace(pt.GetString((string)entry.Key)),
                $"missing pt-BR translation for {entry.Key}");
        }
    }
}
```

Create `tests/JobForge.Ui.Tests/CultureCookieTests.cs`:
```csharp
using JobForge.Ui.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace JobForge.Ui.Tests;

public sealed class CultureCookieTests
{
    [Theory]
    [InlineData("pt-BR", true)]
    [InlineData("en", true)]
    [InlineData("fr", false)]
    [InlineData(null, false)]
    public void Only_supported_cultures_are_accepted(string? culture, bool expected)
        => Assert.Equal(expected, CultureCookie.IsSupported(culture));

    [Theory]
    [InlineData("/jobs", "/jobs")]
    [InlineData("/executions?status=dead", "/executions?status=dead")]
    [InlineData("https://evil.test", "/")]
    [InlineData("//evil.test", "/")]
    [InlineData("", "/")]
    [InlineData(null, "/")]
    public void Redirect_is_restricted_to_local_paths(string? input, string expected)
        => Assert.Equal(expected, CultureCookie.SafeRedirect(input));

    [Fact]
    public void Cookie_value_round_trips()
    {
        var value = CultureCookie.BuildValue("pt-BR");
        var parsed = CookieRequestCultureProvider.ParseCookieValue(value);

        Assert.NotNull(parsed);
        Assert.Equal("pt-BR", parsed!.Cultures.First().Value);
    }
}
```

Create `tests/JobForge.Ui.Tests/UiTestContext.cs`:
```csharp
using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public abstract class UiTestContext : BunitContext
{
    protected UiTestContext()
    {
        Services.AddLocalization(o => o.ResourcesPath = "Resources");
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}

public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _ui = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

    public CultureScope(string name)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(name);
        CultureInfo.CurrentCulture = new CultureInfo(name);
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = _ui;
        CultureInfo.CurrentCulture = _culture;
    }
}
```

- [ ] **Step 2.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter "LocalizationTests|CultureCookieTests"`
Expected: FAIL — `SharedResources`/`CultureCookie` não existem.

- [ ] **Step 2.3: Criar o marker e os recursos**

Create `src/JobForge.Ui/Resources/SharedResources.cs`:
```csharp
namespace JobForge.Ui;

/// <summary>Marker type for IStringLocalizer; resources live in Resources/SharedResources*.resx.</summary>
public sealed class SharedResources { }
```

Create `src/JobForge.Ui/Resources/SharedResources.resx` (EN, neutro):
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="nav_overview" xml:space="preserve"><value>Overview</value></data>
  <data name="nav_jobs" xml:space="preserve"><value>Jobs</value></data>
  <data name="nav_new_job" xml:space="preserve"><value>New job</value></data>
  <data name="nav_executions" xml:space="preserve"><value>Executions</value></data>
  <data name="nav_dead_letters" xml:space="preserve"><value>Dead letters</value></data>
  <data name="nav_operations" xml:space="preserve"><value>Operations</value></data>
  <data name="brand_name" xml:space="preserve"><value>jobforge</value></data>
  <data name="brand_tagline" xml:space="preserve"><value>webhook scheduler</value></data>
  <data name="header_new_job" xml:space="preserve"><value>New job</value></data>
  <data name="header_language" xml:space="preserve"><value>Language</value></data>
  <data name="header_toggle_theme" xml:space="preserve"><value>Toggle theme</value></data>
  <data name="header_accent" xml:space="preserve"><value>Accent colour</value></data>
  <data name="header_open_palette" xml:space="preserve"><value>Search or jump to…</value></data>
  <data name="common_loading" xml:space="preserve"><value>Loading…</value></data>
  <data name="common_error" xml:space="preserve"><value>An unhandled error has occurred.</value></data>
  <data name="common_cancel" xml:space="preserve"><value>Cancel</value></data>
  <data name="common_confirm" xml:space="preserve"><value>Confirm</value></data>
  <data name="common_refresh" xml:space="preserve"><value>Refresh</value></data>
  <data name="common_never" xml:space="preserve"><value>never</value></data>
  <data name="common_all" xml:space="preserve"><value>All</value></data>
  <data name="common_close" xml:space="preserve"><value>Close</value></data>
  <data name="status_pending" xml:space="preserve"><value>pending</value></data>
  <data name="status_running" xml:space="preserve"><value>running</value></data>
  <data name="status_succeeded" xml:space="preserve"><value>succeeded</value></data>
  <data name="status_failed" xml:space="preserve"><value>failed</value></data>
  <data name="status_dead" xml:space="preserve"><value>dead</value></data>
  <data name="status_unknown" xml:space="preserve"><value>unknown</value></data>
  <data name="overview_title" xml:space="preserve"><value>Overview</value></data>
  <data name="overview_active_jobs" xml:space="preserve"><value>Active jobs</value></data>
  <data name="overview_success_rate" xml:space="preserve"><value>Success rate</value></data>
  <data name="overview_dead_letters" xml:space="preserve"><value>Dead letters</value></data>
  <data name="overview_avg_duration" xml:space="preserve"><value>Average duration</value></data>
  <data name="overview_window_note" xml:space="preserve"><value>Based on the latest {0} executions</value></data>
  <data name="overview_total_jobs" xml:space="preserve"><value>{0} total</value></data>
  <data name="overview_last_executions" xml:space="preserve"><value>Latest executions</value></data>
  <data name="overview_no_data" xml:space="preserve"><value>No executions yet. Create a job to see data here.</value></data>
  <data name="overview_chart_hourly" xml:space="preserve"><value>Executions per hour</value></data>
  <data name="overview_chart_status" xml:space="preserve"><value>By status</value></data>
  <data name="jobs_title" xml:space="preserve"><value>Jobs</value></data>
  <data name="jobs_search_placeholder" xml:space="preserve"><value>Search by name…</value></data>
  <data name="jobs_empty" xml:space="preserve"><value>No jobs yet — create the first one.</value></data>
  <data name="jobs_no_match" xml:space="preserve"><value>No jobs match the search.</value></data>
  <data name="jobs_run_now" xml:space="preserve"><value>Run now</value></data>
  <data name="jobs_disable" xml:space="preserve"><value>Disable</value></data>
  <data name="jobs_last_run" xml:space="preserve"><value>Last run</value></data>
  <data name="jobs_enabled" xml:space="preserve"><value>Enabled</value></data>
  <data name="jobs_disabled" xml:space="preserve"><value>Disabled</value></data>
  <data name="jobs_disable_title" xml:space="preserve"><value>Disable job</value></data>
  <data name="jobs_disable_body" xml:space="preserve"><value>Disable "{0}"? Scheduled runs stop until it is re-enabled.</value></data>
  <data name="jobs_disabled_toast" xml:space="preserve"><value>Job disabled.</value></data>
  <data name="jobs_name" xml:space="preserve"><value>Name</value></data>
  <data name="jobs_url" xml:space="preserve"><value>Webhook URL</value></data>
  <data name="jobs_cron" xml:space="preserve"><value>Cron</value></data>
  <data name="new_title" xml:space="preserve"><value>New job</value></data>
  <data name="new_name" xml:space="preserve"><value>Name</value></data>
  <data name="new_url" xml:space="preserve"><value>Webhook URL</value></data>
  <data name="new_payload" xml:space="preserve"><value>Payload JSON (optional)</value></data>
  <data name="new_cron" xml:space="preserve"><value>Cron (optional)</value></data>
  <data name="new_cron_hint" xml:space="preserve"><value>Cronos syntax, e.g. */5 * * * *. Leave empty for a one-shot job.</value></data>
  <data name="new_cron_preview" xml:space="preserve"><value>Next runs (UTC)</value></data>
  <data name="new_create" xml:space="preserve"><value>Create job</value></data>
  <data name="new_cancel" xml:space="preserve"><value>Cancel</value></data>
  <data name="new_name_required" xml:space="preserve"><value>Name is required.</value></data>
  <data name="new_url_invalid" xml:space="preserve"><value>A valid http(s) URL is required.</value></data>
  <data name="new_payload_invalid" xml:space="preserve"><value>Payload must be valid JSON.</value></data>
  <data name="new_cron_invalid" xml:space="preserve"><value>Invalid cron expression (Cronos syntax).</value></data>
  <data name="new_created_toast" xml:space="preserve"><value>Job created.</value></data>
  <data name="new_preset_5m" xml:space="preserve"><value>Every 5 min</value></data>
  <data name="new_preset_hourly" xml:space="preserve"><value>Hourly</value></data>
  <data name="new_preset_daily" xml:space="preserve"><value>Daily 09:00</value></data>
  <data name="new_preset_weekly" xml:space="preserve"><value>Mondays 09:00</value></data>
  <data name="exec_title" xml:space="preserve"><value>Executions</value></data>
  <data name="exec_filter" xml:space="preserve"><value>Status</value></data>
  <data name="exec_page_info" xml:space="preserve"><value>Page {0} of {1} ({2} total)</value></data>
  <data name="exec_empty" xml:space="preserve"><value>No executions match the current filter.</value></data>
  <data name="exec_scheduled" xml:space="preserve"><value>Scheduled</value></data>
  <data name="exec_job" xml:space="preserve"><value>Job</value></data>
  <data name="exec_status" xml:space="preserve"><value>Status</value></data>
  <data name="exec_attempt" xml:space="preserve"><value>Attempt</value></data>
  <data name="exec_http" xml:space="preserve"><value>HTTP</value></data>
  <data name="exec_prev" xml:space="preserve"><value>Previous</value></data>
  <data name="exec_next" xml:space="preserve"><value>Next</value></data>
  <data name="exec_detail_title" xml:space="preserve"><value>Execution</value></data>
  <data name="exec_timeline" xml:space="preserve"><value>Timeline</value></data>
  <data name="exec_scheduled_at" xml:space="preserve"><value>scheduled</value></data>
  <data name="exec_started_at" xml:space="preserve"><value>started</value></data>
  <data name="exec_finished_at" xml:space="preserve"><value>finished</value></data>
  <data name="exec_duration" xml:space="preserve"><value>duration</value></data>
  <data name="exec_not_found" xml:space="preserve"><value>Execution not found — it does not exist (or was purged).</value></data>
  <data name="exec_trace" xml:space="preserve"><value>trace</value></data>
  <data name="exec_job_id" xml:space="preserve"><value>job</value></data>
  <data name="ops_title" xml:space="preserve"><value>Operations</value></data>
  <data name="ops_health" xml:space="preserve"><value>Orchestrator health</value></data>
  <data name="ops_live" xml:space="preserve"><value>Liveness</value></data>
  <data name="ops_ready" xml:space="preserve"><value>Readiness</value></data>
  <data name="ops_base_url" xml:space="preserve"><value>API base URL</value></data>
  <data name="ops_client_id" xml:space="preserve"><value>OIDC client</value></data>
  <data name="ops_scopes" xml:space="preserve"><value>Scopes</value></data>
  <data name="ops_refresh" xml:space="preserve"><value>Executions refresh</value></data>
  <data name="ops_metrics" xml:space="preserve"><value>Open Prometheus metrics</value></data>
  <data name="ops_healthy" xml:space="preserve"><value>Healthy</value></data>
  <data name="ops_unhealthy" xml:space="preserve"><value>Unhealthy</value></data>
  <data name="ops_degraded" xml:space="preserve"><value>Degraded</value></data>
  <data name="ops_checking" xml:space="preserve"><value>Checking…</value></data>
  <data name="ops_seconds" xml:space="preserve"><value>{0} s</value></data>
  <data name="time_now" xml:space="preserve"><value>just now</value></data>
  <data name="time_seconds_ago" xml:space="preserve"><value>{0}s ago</value></data>
  <data name="time_minutes_ago" xml:space="preserve"><value>{0}m ago</value></data>
  <data name="time_hours_ago" xml:space="preserve"><value>{0}h ago</value></data>
  <data name="time_days_ago" xml:space="preserve"><value>{0}d ago</value></data>
  <data name="command_placeholder" xml:space="preserve"><value>Search pages and actions…</value></data>
  <data name="command_no_results" xml:space="preserve"><value>No results found.</value></data>
  <data name="command_pages" xml:space="preserve"><value>Pages</value></data>
  <data name="command_actions" xml:space="preserve"><value>Actions</value></data>
  <data name="command_new_job" xml:space="preserve"><value>Create a new job</value></data>
  <data name="command_refresh" xml:space="preserve"><value>Refresh data</value></data>
  <data name="command_toggle_theme" xml:space="preserve"><value>Toggle theme</value></data>
  <data name="command_hint_navigate" xml:space="preserve"><value>navigate</value></data>
  <data name="command_hint_select" xml:space="preserve"><value>select</value></data>
  <data name="command_hint_close" xml:space="preserve"><value>close</value></data>
</root>
```

Create `src/JobForge.Ui/Resources/SharedResources.pt-BR.resx` with the **same keys** and these values:
```
nav_overview=Visão geral        nav_jobs=Jobs                 nav_new_job=Novo job
nav_executions=Execuções        nav_dead_letters=Dead letters nav_operations=Operações
brand_name=jobforge             brand_tagline=agendador de webhooks
header_new_job=Novo job         header_language=Idioma        header_toggle_theme=Alternar tema
header_accent=Cor de destaque   header_open_palette=Buscar ou ir para…
common_loading=A carregar…      common_cancel=Cancelar         common_confirm=Confirmar
common_error=Ocorreu um erro inesperado.
common_refresh=Atualizar        common_never=nunca             common_all=Todos
common_close=Fechar
status_pending=pendente         status_running=em execução     status_succeeded=concluída
status_failed=falhou            status_dead=morta              status_unknown=desconhecido
overview_title=Visão geral      overview_active_jobs=Jobs ativos
overview_success_rate=Taxa de sucesso   overview_dead_letters=Dead letters
overview_avg_duration=Duração média
overview_window_note=Com base nas últimas {0} execuções
overview_total_jobs=total: {0}
overview_last_executions=Execuções recentes
overview_no_data=Ainda não há execuções. Cria um job para ver dados aqui.
overview_chart_hourly=Execuções por hora      overview_chart_status=Por estado
jobs_title=Jobs                 jobs_search_placeholder=Buscar por nome…
jobs_empty=Ainda não há jobs — cria o primeiro.
jobs_no_match=Nenhum job corresponde à busca.
jobs_run_now=Correr agora      jobs_disable=Desativar        jobs_last_run=Última execução
jobs_enabled=Ativo             jobs_disabled=Inativo
jobs_disable_title=Desativar job
jobs_disable_body=Desativar "{0}"? As execuções agendadas param até ser reativado.
jobs_disabled_toast=Job desativado.
jobs_name=Nome                 jobs_url=URL do webhook       jobs_cron=Cron
new_title=Novo job             new_name=Nome                 new_url=URL do webhook
new_payload=Payload JSON (opcional)   new_cron=Cron (opcional)
new_cron_hint=Sintaxe Cronos, ex.: */5 * * * *. Deixa vazio para execução única.
new_cron_preview=Próximas execuções (UTC)   new_create=Criar job   new_cancel=Cancelar
new_name_required=O nome é obrigatório.
new_url_invalid=É necessário um URL http(s) válido.
new_payload_invalid=O payload tem de ser JSON válido.
new_cron_invalid=Expressão cron inválida (sintaxe Cronos).
new_created_toast=Job criado.
new_preset_5m=A cada 5 min       new_preset_hourly=De hora a hora
new_preset_daily=Diário às 09:00 new_preset_weekly=Segundas às 09:00
exec_title=Execuções            exec_filter=Estado            exec_page_info=Página {0} de {1} ({2} no total)
exec_empty=Nenhuma execução corresponde ao filtro atual.
exec_scheduled=Agendada         exec_job=Job                  exec_status=Estado
exec_attempt=Tentativa          exec_http=HTTP
exec_prev=Anterior              exec_next=Seguinte
exec_detail_title=Execução      exec_timeline=Cronologia      exec_scheduled_at=agendada
exec_started_at=iniciada        exec_finished_at=terminada    exec_duration=duração
exec_not_found=Execução não encontrada — não existe (ou foi expurgada).
exec_trace=trace                exec_job_id=job
ops_title=Operações             ops_health=Saúde do orchestrator
ops_live=Disponibilidade        ops_ready=Prontidão
ops_base_url=URL base da API    ops_client_id=Cliente OIDC    ops_scopes=Scopes
ops_refresh=Atualização das execuções   ops_metrics=Abrir métricas Prometheus
ops_healthy=Saudável            ops_unhealthy=Indisponível    ops_degraded=Degradado
ops_checking=A verificar…       ops_seconds={0} s
time_now=agora                  time_seconds_ago=há {0}s      time_minutes_ago=há {0}m
time_hours_ago=há {0}h          time_days_ago=há {0}d
command_placeholder=Buscar páginas e ações…   command_no_results=Nenhum resultado.
command_pages=Páginas           command_actions=Ações
command_new_job=Criar um novo job    command_refresh=Atualizar dados
command_toggle_theme=Alternar tema
command_hint_navigate=navegar   command_hint_select=selecionar  command_hint_close=fechar
```

- [ ] **Step 2.4: Criar o helper de cookie**

Create `src/JobForge.Ui/Localization/CultureCookie.cs`:
```csharp
using Microsoft.AspNetCore.Localization;

namespace JobForge.Ui.Localization;

public static class CultureCookie
{
    public static string CookieName => CookieRequestCultureProvider.DefaultCookieName;

    public static bool IsSupported(string? culture)
        => culture is "pt-BR" or "en";

    public static string BuildValue(string culture)
        => CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));

    /// <summary>Only same-site absolute paths; blocks open redirects (//host).</summary>
    public static string SafeRedirect(string? redirect)
        => !string.IsNullOrWhiteSpace(redirect)
           && redirect.StartsWith('/')
           && !redirect.StartsWith("//")
            ? redirect
            : "/";

    public static void Set(HttpResponse response, string culture, bool secure)
        => response.Cookies.Append(CookieName, BuildValue(culture), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = secure
        });
}
```

- [ ] **Step 2.5: Ligar no Program**

Modify `src/JobForge.Ui/Program.cs`:
```csharp
using System.Globalization;
using JobForge.Ui.Localization;
using Microsoft.AspNetCore.Localization;
```
Depois de `builder.Services.Configure<UiOptions>(...)`:
```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supported = new[] { new CultureInfo("pt-BR"), new CultureInfo("en") };
    options.DefaultRequestCulture = new RequestCulture("pt-BR");
    options.SupportedCultures = supported;
    options.SupportedUICultures = supported;
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider { CookieName = CultureCookie.CookieName },
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});
```
Antes de `app.UseAntiforgery();`:
```csharp
app.UseRequestLocalization();
```
Antes de `app.MapRazorComponents<App>()`:
```csharp
app.MapGet("/culture/set", (HttpContext http, string c, string? redirect) =>
{
    if (!CultureCookie.IsSupported(c)) return Results.BadRequest(new { error = "unsupported culture" });
    CultureCookie.Set(http.Response, c, http.Request.IsHttps);
    return Results.LocalRedirect(CultureCookie.SafeRedirect(redirect));
});
```

- [ ] **Step 2.6: Correr e ver passar**

Run:
```powershell
dotnet build JobForge.Ui.slnx
dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter "LocalizationTests|CultureCookieTests"
```
Expected: PASS.

- [ ] **Step 2.7: Commit**

```powershell
git add src/JobForge.Ui/Resources src/JobForge.Ui/Localization src/JobForge.Ui/Program.cs tests/JobForge.Ui.Tests/LocalizationTests.cs tests/JobForge.Ui.Tests/CultureCookieTests.cs tests/JobForge.Ui.Tests/UiTestContext.cs
git commit -m "feat(ui): pt-BR/en localization with cookie culture endpoint"
```

---

### Task 3: UI primitives + StatusBadge

**Files:**
- Create: `src/JobForge.Ui/Components/Ui/Card.razor`
- Create: `src/JobForge.Ui/Components/Ui/Button.razor`
- Create: `src/JobForge.Ui/Components/Ui/Badge.razor`
- Create: `src/JobForge.Ui/Components/Ui/Modal.razor`
- Modify: `src/JobForge.Ui/Components/StatusBadge.razor`
- Modify: `src/JobForge.Ui/Components/_Imports.razor`
- Delete: `tests/JobForge.Ui.Tests/StatusBadgeTests.cs` (substitu�do por `PrimitivesTests`)
- Test: `tests/JobForge.Ui.Tests/PrimitivesTests.cs`

**Interfaces:**
- Consumes: `IStringLocalizer<SharedResources>`.
- Produces: `<Card Header Footer Class>`, `<Button Variant Size Disabled OnClick>`, `<Badge Variant>`, `<Modal IsOpen OnClose Title>`, `<StatusBadge Status>` (mantém `data-testid="status-badge"` e `data-status`).

- [ ] **Step 3.1: Escrever os testes**

Create `tests/JobForge.Ui.Tests/PrimitivesTests.cs`:
```csharp
using JobForge.Ui.Components;
using JobForge.Ui.Components.Ui;

namespace JobForge.Ui.Tests;

public sealed class PrimitivesTests : UiTestContext
{
    [Theory]
    [InlineData("succeeded", "success")]
    [InlineData("failed", "warning")]
    [InlineData("dead", "error")]
    [InlineData("running", "info")]
    [InlineData("pending", "neutral")]
    [InlineData("weird", "neutral")]
    public void Status_badge_maps_status_to_variant(string status, string variant)
    {
        var cut = Render<StatusBadge>(p => p.Add(x => x.Status, status));
        var badge = cut.Find("[data-testid=status-badge]");

        Assert.Equal(status, badge.GetAttribute("data-status"));
        Assert.Contains($"data-variant=\"{variant}\"", cut.Markup);
    }

    [Fact]
    public void Button_renders_variant_and_handles_click()
    {
        var clicked = 0;
        var cut = Render<Button>(p => p
            .Add(x => x.Variant, "secondary")
            .Add(x => x.OnClick, () => clicked++));

        cut.Find("button").Click();
        Assert.Equal(1, clicked);
        Assert.Contains("bg-[var(--secondary)]", cut.Markup);
    }

    [Fact]
    public void Modal_only_renders_when_open()
    {
        var closed = Render<Modal>(p => p.Add(x => x.IsOpen, false));
        Assert.Empty(closed.FindAll("[data-testid=modal]"));

        var open = Render<Modal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.Title, "Disable job"));
        Assert.NotNull(open.Find("[data-testid=modal]"));
        Assert.Contains("Disable job", open.Markup);
    }
}
```

- [ ] **Step 3.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter PrimitivesTests`
Expected: FAIL — componentes inexistentes.

- [ ] **Step 3.3: Implementar**

Create `src/JobForge.Ui/Components/Ui/Card.razor` (copiado de
`DashboardTheKingThePower/src/DashboardTheKingThePower/Components/ui/Card.razor`, sem alterações
além do namespace implícito):
```razor
<div class="@($"rounded-xl border border-[var(--border)] bg-[var(--card)] text-[var(--card-foreground)] {Class}")">
    @if (Header is not null)
    {
        <div class="px-6 py-4 border-b border-[var(--border)]">@Header</div>
    }
    <div class="p-6">@ChildContent</div>
    @if (Footer is not null)
    {
        <div class="px-6 py-4 border-t border-[var(--border)]">@Footer</div>
    }
</div>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Header { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
    [Parameter] public string Class { get; set; } = "";
}
```

Create `src/JobForge.Ui/Components/Ui/Button.razor`:
```razor
<button type="@Type"
        class="@($"inline-flex items-center justify-center gap-2 rounded-lg text-sm font-medium transition-colors {VariantClass} {SizeClass}")"
        disabled="@Disabled"
        data-testid="@TestId"
        @onclick="OnClick">
    @ChildContent
</button>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Variant { get; set; } = "primary";
    [Parameter] public string Size { get; set; } = "md";
    [Parameter] public string Type { get; set; } = "button";
    [Parameter] public string? TestId { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    private string VariantClass => Variant switch
    {
        "primary" => "bg-[var(--primary)] text-[var(--primary-foreground)] hover:opacity-90",
        "secondary" => "bg-[var(--secondary)] text-[var(--secondary-foreground)] border border-[var(--border)]",
        "danger" => "bg-[var(--destructive)] text-white hover:opacity-90",
        "ghost" => "hover:bg-[var(--secondary)]",
        _ => "bg-[var(--primary)] text-[var(--primary-foreground)]"
    };

    private string SizeClass => Size switch
    {
        "sm" => "h-8 px-3 text-xs",
        "lg" => "h-12 px-6",
        _ => "h-10 px-4"
    };
}
```

Create `src/JobForge.Ui/Components/Ui/Badge.razor`:
```razor
<span data-variant="@Variant"
      class="@($"inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium {VariantClass}")">
    @ChildContent
</span>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Variant { get; set; } = "neutral";

    private string VariantClass => Variant switch
    {
        "success" => "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300",
        "warning" => "bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300",
        "error" => "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300",
        "info" => "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300",
        _ => "bg-[var(--secondary)] text-[var(--secondary-foreground)]"
    };
}
```

Create `src/JobForge.Ui/Components/Ui/Modal.razor`:
```razor
@if (IsOpen)
{
    <div class="fixed inset-0 z-50 flex items-center justify-center" data-testid="modal">
        <div class="absolute inset-0 bg-black/50" @onclick="OnClose"></div>
        <div class="relative mx-4 w-full max-w-lg rounded-xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-xl">
            @if (!string.IsNullOrWhiteSpace(Title))
            {
                <h2 class="mb-2 text-lg font-semibold">@Title</h2>
            }
            @ChildContent
            <div class="mt-6 flex justify-end gap-2">@Footer</div>
        </div>
    </div>
}

@code {
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
```

Replace `src/JobForge.Ui/Components/StatusBadge.razor`:
```razor
@inject IStringLocalizer<SharedResources> L

<span data-testid="status-badge" data-status="@Status">
    <Badge Variant="@Variant">@Label</Badge>
</span>

@code {
    [Parameter, EditorRequired] public string? Status { get; set; }

    private string Variant => Status switch
    {
        "succeeded" => "success",
        "failed" => "warning",
        "dead" => "error",
        "running" => "info",
        _ => "neutral"
    };

    private string Label => Status is "pending" or "running" or "succeeded" or "failed" or "dead"
        ? L[$"status_{Status}"]
        : L["status_unknown"];
}
```

Modify `src/JobForge.Ui/Components/_Imports.razor` — append:
```razor
@using JobForge.Ui.Components.Ui
@using JobForge.Ui.Components.Shell
@using Microsoft.Extensions.Localization
@using JobForge.Ui
```

**Convenção Razor:** em atributos (`placeholder`, `title`, `aria-label`, `Title`) usar
`@(L["chave"])`; `@L["chave"]` só em conteúdo de elemento. Aspas aninhadas quebram o
atributo.

- [ ] **Step 3.4: Correr e ver passar**

Run:
```powershell
git rm tests/JobForge.Ui.Tests/StatusBadgeTests.cs
dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter PrimitivesTests
```
Expected: PASS.

- [ ] **Step 3.5: Commit**

```powershell
git add src/JobForge.Ui/Components/Ui src/JobForge.Ui/Components/StatusBadge.razor src/JobForge.Ui/Components/_Imports.razor tests/JobForge.Ui.Tests/PrimitivesTests.cs
git commit -m "feat(ui): card, button, badge and modal primitives"
```

---

### Task 4: Tema e toasts

**Files:**
- Create: `src/JobForge.Ui/Services/ToastService.cs`
- Create: `src/JobForge.Ui/Components/Ui/ToastContainer.razor`
- Create: `src/JobForge.Ui/Components/Theme/ThemeToggle.razor`
- Create: `src/JobForge.Ui/Components/Theme/AccentPicker.razor`
- Modify: `src/JobForge.Ui/Program.cs`
- Modify: `src/JobForge.Ui/Components/_Imports.razor`
- Test: `tests/JobForge.Ui.Tests/ToastTests.cs`

**Interfaces:**
- Consumes: `theme.js` (Task 1).
- Produces: `ToastService.ShowSuccess/ShowError/ShowWarning/ShowInfo(string)`, evento `OnShow`; `<ToastContainer />`; `<ThemeToggle />`; `<AccentPicker />` com `data-testid="accent-option"`.

- [ ] **Step 4.1: Escrever os testes**

Create `tests/JobForge.Ui.Tests/ToastTests.cs`:
```csharp
using JobForge.Ui.Components.Ui;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class ToastTests : UiTestContext
{
    [Fact]
    public void Show_success_renders_message()
    {
        var toasts = new ToastService();
        Services.AddSingleton(toasts);

        var cut = Render<ToastContainer>();
        toasts.ShowSuccess("Job created.");

        cut.WaitForAssertion(() => Assert.Contains("Job created.", cut.Markup));
    }

    [Fact]
    public void Service_raises_typed_event()
    {
        var toasts = new ToastService();
        string? message = null;
        string? type = null;
        toasts.OnShow += (m, t) => { message = m; type = t; };

        toasts.ShowError("boom");

        Assert.Equal("boom", message);
        Assert.Equal("error", type);
    }
}
```

- [ ] **Step 4.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter ToastTests`
Expected: FAIL.

- [ ] **Step 4.3: Implementar**

Create `src/JobForge.Ui/Services/ToastService.cs`:
```csharp
namespace JobForge.Ui.Services;

public sealed class ToastService
{
    public event Action<string, string>? OnShow;

    public void ShowSuccess(string message) => OnShow?.Invoke(message, "success");
    public void ShowError(string message) => OnShow?.Invoke(message, "error");
    public void ShowWarning(string message) => OnShow?.Invoke(message, "warning");
    public void ShowInfo(string message) => OnShow?.Invoke(message, "info");
}
```

Create `src/JobForge.Ui/Components/Ui/ToastContainer.razor` (baseado no do Dashboard,
com o loop de expiração a marshallar para o renderer — o original chama
`StateHasChanged` numa thread de fundo e falha em Blazor Server):
```razor
@implements IDisposable
@inject ToastService ToastService

@if (_toasts.Count > 0)
{
    <div class="pointer-events-none fixed right-4 top-4 z-[110] flex w-full max-w-sm flex-col gap-2">
        @foreach (var toast in _toasts)
        {
            <div data-testid="toast"
                 class="pointer-events-auto flex items-start gap-3 rounded-lg border p-4 shadow-lg @ContainerClass(toast.Type)">
                <div class="mt-0.5 flex-shrink-0">
                    <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                              d="@(toast.Type == "success"
                                  ? "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                                  : "M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z")" />
                    </svg>
                </div>
                <p class="min-w-0 flex-1 text-sm font-medium">@toast.Message</p>
                <button type="button" class="flex-shrink-0 text-[var(--muted-foreground)] hover:text-[var(--foreground)]"
                        @onclick="() => Remove(toast)">×</button>
            </div>
        }
    </div>
}

@code {
    private readonly List<ToastItem> _toasts = [];
    private CancellationTokenSource? _cts;

    protected override void OnInitialized()
    {
        ToastService.OnShow += HandleShow;
        _cts = new CancellationTokenSource();
        _ = ExpireLoopAsync(_cts.Token);
    }

    private void HandleShow(string message, string type)
        => _ = InvokeAsync(() =>
        {
            _toasts.Add(new ToastItem(message, type));
            StateHasChanged();
        });

    private async Task ExpireLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);
                var expired = _toasts.Where(t => DateTime.UtcNow - t.CreatedAt >= TimeSpan.FromSeconds(4)).ToList();
                if (expired.Count == 0) continue;

                await InvokeAsync(() =>
                {
                    foreach (var toast in expired) _toasts.Remove(toast);
                    StateHasChanged();
                });
            }
        }
        catch (OperationCanceledException)
        {
            // circuit closed
        }
    }

    private void Remove(ToastItem toast) => _toasts.Remove(toast);

    private static string ContainerClass(string type) => type switch
    {
        "success" => "border-green-200 bg-green-50 dark:border-green-800 dark:bg-green-950",
        "error" => "border-red-200 bg-red-50 dark:border-red-800 dark:bg-red-950",
        "warning" => "border-amber-200 bg-amber-50 dark:border-amber-800 dark:bg-amber-950",
        _ => "border-[var(--border)] bg-[var(--card)]"
    };

    private sealed record ToastItem(string Message, string Type)
    {
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    public void Dispose()
    {
        ToastService.OnShow -= HandleShow;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

Create `src/JobForge.Ui/Components/Theme/ThemeToggle.razor`:
```razor
@inject IJSRuntime JS
@inject IStringLocalizer<SharedResources> L

<button type="button"
        class="rounded-lg p-2 text-[var(--foreground)] hover:bg-[var(--secondary)]"
        title="@(L["header_toggle_theme"])"
        aria-label="@(L["header_toggle_theme"])"
        data-testid="theme-toggle"
        @onclick="ToggleAsync">
    <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
              d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
    </svg>
</button>

@code {
    private async Task ToggleAsync() => await JS.InvokeAsync<string>("toggleTheme");
}
```

Create `src/JobForge.Ui/Components/Theme/AccentPicker.razor`:
```razor
@inject IJSRuntime JS
@inject IStringLocalizer<SharedResources> L

<div class="relative">
    <button type="button"
            class="rounded-lg p-2 text-[var(--foreground)] hover:bg-[var(--secondary)]"
            title="@(L["header_accent"])"
            aria-label="@(L["header_accent"])"
            data-testid="accent-toggle"
            @onclick="() => _open = !_open">
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                  d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01" />
        </svg>
    </button>

    @if (_open)
    {
        <div class="absolute right-0 top-full z-50 mt-2 rounded-lg border border-[var(--border)] bg-[var(--card)] p-3 shadow-lg">
            <p class="mb-2 text-xs text-[var(--muted-foreground)]">@L["header_accent"]</p>
            <div class="flex gap-2">
                @foreach (var accent in Accents)
                {
                    <button type="button"
                            data-testid="accent-option"
                            data-accent="@accent.Key"
                            class="h-6 w-6 rounded-full border-2 @(_current == accent.Key ? "border-white ring-2 ring-[var(--ring)]" : "border-transparent")"
                            style="background-color: @accent.Value"
                            @onclick="() => SetAsync(accent.Key)">
                    </button>
                }
            </div>
        </div>
    }
</div>

@code {
    private static readonly Dictionary<string, string> Accents = new()
    {
        ["emerald"] = "oklch(0.55 0.19 160)",
        ["blue"] = "oklch(0.55 0.19 240)",
        ["violet"] = "oklch(0.55 0.19 280)",
        ["rose"] = "oklch(0.55 0.19 350)",
        ["orange"] = "oklch(0.55 0.19 50)",
        ["slate"] = "oklch(0.55 0.02 260)"
    };

    private bool _open;
    private string _current = "blue";

    protected override async Task OnInitializedAsync()
        => _current = await JS.InvokeAsync<string>("getAccent");

    private async Task SetAsync(string accent)
    {
        await JS.InvokeVoidAsync("setAccent", accent);
        _current = accent;
        _open = false;
    }
}
```

Modify `src/JobForge.Ui/Program.cs` — após `builder.Services.AddSingleton(TimeProvider.System);`:
```csharp
builder.Services.AddScoped<ToastService>();
```

Modify `src/JobForge.Ui/Components/_Imports.razor` — append:
```razor
@using JobForge.Ui.Components.Theme
```

- [ ] **Step 4.4: Correr e ver passar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter ToastTests`
Expected: PASS.

- [ ] **Step 4.5: Commit**

```powershell
git add src/JobForge.Ui/Services/ToastService.cs src/JobForge.Ui/Components/Ui/ToastContainer.razor src/JobForge.Ui/Components/Theme src/JobForge.Ui/Program.cs src/JobForge.Ui/Components/_Imports.razor tests/JobForge.Ui.Tests/ToastTests.cs
git commit -m "feat(ui): theme toggle, accent picker and toasts"
```

---

### Task 5: Shell (layout, sidebar, header, mobile)

> **Dependency:** execute Task 6 (command palette) **before** this task. `MainLayout`
> renders `<CommandPalette>` and the Header opens it; without the component the shell
> does not compile.

**Files:**
- Create: `src/JobForge.Ui/Components/Shell/NavRoute.cs`
- Create: `src/JobForge.Ui/Components/Shell/Sidebar.razor`
- Create: `src/JobForge.Ui/Components/Shell/Header.razor`
- Create: `src/JobForge.Ui/Components/Shell/BottomNav.razor`
- Modify: `src/JobForge.Ui/Components/Layout/MainLayout.razor`
- Delete: `src/JobForge.Ui/Components/Layout/NavMenu.razor`
- Modify: `src/JobForge.Ui/Components/_Imports.razor`
- Test: `tests/JobForge.Ui.Tests/ShellTests.cs`

**Interfaces:**
- Consumes: `ThemeToggle`, `AccentPicker`, `ToastContainer`, `NavRoute`.
- Produces: `<Sidebar IsCollapsed IsMobile OnToggle OnClose>`, `<Header OnToggleSidebar>`, `<BottomNav OnMenuClick>`, `NavRoute.IsActive(string, string)`.

- [ ] **Step 5.1: Escrever os testes**

Create `tests/JobForge.Ui.Tests/ShellTests.cs`:
```csharp
using JobForge.Ui.Components.Shell;
using Microsoft.AspNetCore.Components;

namespace JobForge.Ui.Tests;

public sealed class ShellTests : UiTestContext
{
    [Theory]
    [InlineData("/", "/", true)]
    [InlineData("/jobs", "/", false)]
    [InlineData("/jobs/abc", "/jobs", true)]
    [InlineData("/executions", "/executions", true)]
    [InlineData("/executions?status=dead", "/executions", true)]
    public void Nav_route_matches_exact_root_and_prefix(string path, string href, bool expected)
        => Assert.Equal(expected, NavRoute.IsActive(path, href));

    [Fact]
    public void Sidebar_renders_all_navigation_items()
    {
        var cut = Render<Sidebar>(p => p.Add(x => x.IsCollapsed, false));

        Assert.Equal(6, cut.FindAll("[data-testid^=nav-]").Count);
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-overview]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-jobs]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-new_job]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-executions]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-dead_letters]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-operations]"));
    }

    [Fact]
    public void Sidebar_marks_current_route_as_active()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/jobs");
        var cut = Render<Sidebar>(p => p.Add(x => x.IsCollapsed, false));

        Assert.Contains("bg-[var(--sidebar-accent)]", cut.Find("[data-testid=nav-jobs]").ClassList);
    }

    [Fact]
    public void Header_has_language_switcher_and_actions()
    {
        var cut = Render<Header>();

        Assert.NotEmpty(cut.FindAll("[data-testid=culture-select]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=new-job-link]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=theme-toggle]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=command-palette-button]"));
    }

    [Fact]
    public void Main_layout_renders_shell_and_body()
    {
        Services.AddSingleton(new JobForge.Ui.Services.ToastService());

        var cut = Render<JobForge.Ui.Components.Layout.MainLayout>(p => p
            .Add(x => x.Body, builder => builder.AddContent(0, "page-content")));

        Assert.NotEmpty(cut.FindAll("aside"));
        Assert.NotEmpty(cut.FindAll("header"));
        Assert.Contains("page-content", cut.Markup);
    }
}
```

Replace `tests/JobForge.Ui.Tests/SmokeTests.cs` (a nav mudou; o layout agora exige
`ToastService`):
```csharp
using JobForge.Ui.Components;
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class SmokeTests : UiTestContext
{
    [Fact]
    public void App_renders()
    {
        Services.AddSingleton<IJobForgeClient>(new FakeJobForgeClient());
        Services.AddSingleton(new ToastService());
        var cut = Render<Routes>();
        Assert.Contains("jobforge", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Error_page_renders()
    {
        var cut = Render<Error>();
        Assert.Contains("An error occurred", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NotFound_page_renders()
    {
        var cut = Render<NotFound>();
        Assert.Contains("does not exist", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Nav_links_to_the_family_pages()
    {
        Services.AddSingleton<IJobForgeClient>(new FakeJobForgeClient());
        Services.AddSingleton(new ToastService());
        var cut = Render<Routes>();
        var hrefs = cut.FindAll("a").Select(a => a.GetAttribute("href")).ToList();

        Assert.Contains("/", hrefs);
        Assert.Contains("/jobs", hrefs);
        Assert.Contains("/jobs/new", hrefs);
        Assert.Contains("/executions", hrefs);
        Assert.Contains("/executions?status=dead", hrefs);
        Assert.Contains("/operations", hrefs);
    }
}
```

- [ ] **Step 5.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter ShellTests`
Expected: FAIL.

- [ ] **Step 5.3: Implementar NavRoute**

Create `src/JobForge.Ui/Components/Shell/NavRoute.cs`:
```csharp
namespace JobForge.Ui.Components.Shell;

/// <summary>"/" matches only the root; every other href matches by prefix.</summary>
public static class NavRoute
{
    public static bool IsActive(string currentPath, string href)
        => href == "/"
            ? string.Equals(currentPath, "/", StringComparison.OrdinalIgnoreCase)
            : currentPath.StartsWith(href, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 5.4: Implementar Sidebar**

Create `src/JobForge.Ui/Components/Shell/Sidebar.razor`:
```razor
@inject NavigationManager Navigation
@inject IStringLocalizer<SharedResources> L

<aside class="@SidebarClass">
    <div class="flex items-center justify-between p-4">
        <div class="flex min-w-0 items-center gap-2">
            <div class="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg bg-[var(--primary)]">
                <svg class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
            </div>
            @if (!IsCollapsed)
            {
                <div class="min-w-0">
                    <span class="block truncate text-sm font-bold text-[var(--sidebar-foreground)]">@L["brand_name"]</span>
                    <span class="block truncate text-[10px] uppercase tracking-wider text-[var(--muted-foreground)]">@L["brand_tagline"]</span>
                </div>
            }
        </div>
        @if (!IsMobile)
        {
            <button type="button" class="rounded p-1 text-[var(--muted-foreground)] hover:bg-[var(--sidebar-accent)]"
                    data-testid="sidebar-toggle" @onclick="() => OnToggle.InvokeAsync()">
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="@(IsCollapsed ? "M9 5l7 7-7 7" : "M15 19l-7-7 7-7")" />
                </svg>
            </button>
        }
    </div>

    <nav class="flex-1 space-y-1 overflow-y-auto overflow-x-hidden px-2 py-2">
        @foreach (var item in Items)
        {
            <a href="@item.Href"
               data-testid="nav-@item.Key"
               class="@NavClass(item.Href)"
               @onclick="() => OnClose.InvokeAsync()">
                <svg class="h-5 w-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="@item.Icon" />
                </svg>
                @if (!IsCollapsed)
                {
                    <span>@L[item.LabelKey]</span>
                }
            </a>
        }
    </nav>

    <div class="border-t border-[var(--sidebar-border)] p-3 text-[10px] text-[var(--muted-foreground)]">
        @if (!IsCollapsed)
        {
            <span>jobforge v1.1.0</span>
        }
    </div>
</aside>

@if (IsMobile && !IsCollapsed)
{
    <div class="fixed inset-0 z-40 bg-black/50" @onclick="() => OnClose.InvokeAsync()"></div>
}

@code {
    [Parameter] public bool IsCollapsed { get; set; }
    [Parameter] public bool IsMobile { get; set; }
    [Parameter] public EventCallback OnToggle { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private static readonly (string Key, string Href, string LabelKey, string Icon)[] Items =
    [
        ("overview", "/", "nav_overview", "M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"),
        ("jobs", "/jobs", "nav_jobs", "M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"),
        ("new_job", "/jobs/new", "nav_new_job", "M12 4v16m8-8H4"),
        ("executions", "/executions", "nav_executions", "M4 6h16M4 12h16M4 18h10"),
        ("dead_letters", "/executions?status=dead", "nav_dead_letters", "M12 9v2m0 4h.01M5.07 19h13.86a2 2 0 001.73-3l-6.93-12a2 2 0 00-3.46 0l-6.93 12a2 2 0 001.73 3z"),
        ("operations", "/operations", "nav_operations", "M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z")
    ];

    private string CurrentPath => new Uri(Navigation.Uri).AbsolutePath;

    private string SidebarClass => IsMobile
        ? $"fixed left-0 top-0 z-50 flex h-screen w-[260px] flex-col border-r border-[var(--sidebar-border)] bg-[var(--sidebar)] text-[var(--sidebar-foreground)] transition-transform duration-300 {(IsCollapsed ? "-translate-x-full" : "translate-x-0")}"
        : $"fixed left-0 top-0 z-50 flex h-screen flex-col border-r border-[var(--sidebar-border)] bg-[var(--sidebar)] text-[var(--sidebar-foreground)] transition-all duration-300 {(IsCollapsed ? "w-[72px]" : "w-[260px]")}";

    private string NavClass(string href)
    {
        var active = NavRoute.IsActive(CurrentPath, href);
        var baseClass = "flex items-center gap-3 rounded-lg px-3 py-2 text-sm text-[var(--sidebar-foreground)] transition-all duration-300";
        var state = active
            ? "border-l-3 border-[var(--primary)] bg-[var(--sidebar-accent)] pl-2.5 text-[var(--primary)]"
            : "border-l-3 border-transparent hover:translate-x-0.5 hover:bg-[var(--sidebar-accent)]";
        var collapsed = IsCollapsed ? "justify-center border-l-0 px-0" : "";
        return $"{baseClass} {state} {collapsed}";
    }
}
```

- [ ] **Step 5.5: Implementar Header**

Create `src/JobForge.Ui/Components/Shell/Header.razor`:
```razor
@inject NavigationManager Navigation
@inject IStringLocalizer<SharedResources> L

<header class="sticky top-0 z-10 flex h-16 items-center gap-3 border-b border-[var(--border)] bg-[var(--background)] px-4 md:px-6">
    <button type="button" class="rounded-lg p-2 hover:bg-[var(--secondary)] md:hidden"
            data-testid="mobile-menu" @onclick="() => OnToggleSidebar.InvokeAsync()">
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 6h16M4 12h16M4 18h16" />
        </svg>
    </button>

    <button type="button"
            class="hidden h-10 flex-1 items-center gap-2 rounded-lg border border-[var(--border)] bg-[var(--secondary)] px-3 text-left text-sm text-[var(--muted-foreground)] md:flex md:max-w-md"
            data-testid="command-palette-button"
            @onclick="OpenPaletteAsync">
        <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <span class="flex-1">@L["header_open_palette"]</span>
        <kbd class="rounded border border-[var(--border)] bg-[var(--background)] px-1.5 py-0.5 text-[10px]">Ctrl+K</kbd>
    </button>

    <div class="ml-auto flex items-center gap-2">
        <a href="/jobs/new" data-testid="new-job-link"
           class="inline-flex h-10 items-center gap-2 rounded-lg bg-[var(--primary)] px-4 text-sm font-medium text-[var(--primary-foreground)] hover:opacity-90">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            <span class="hidden sm:inline">@L["header_new_job"]</span>
        </a>

        <label class="sr-only" for="culture-select">@L["header_language"]</label>
        <select id="culture-select" data-testid="culture-select"
                class="h-10 rounded-lg border border-[var(--border)] bg-[var(--card)] px-2 text-sm"
                value="@System.Globalization.CultureInfo.CurrentUICulture.Name"
                @onchange="ChangeCultureAsync">
            <option value="pt-BR">PT</option>
            <option value="en">EN</option>
        </select>

        <ThemeToggle />
        <AccentPicker />
    </div>
</header>

@code {
    [Parameter] public EventCallback OnToggleSidebar { get; set; }
    [Parameter] public EventCallback OnOpenPalette { get; set; }

    private async Task ChangeCultureAsync(ChangeEventArgs e)
    {
        var culture = e.Value?.ToString();
        if (!CultureCookie.IsSupported(culture))
        {
            return;
        }

        var current = new Uri(Navigation.Uri).PathAndQuery;
        Navigation.NavigateTo(
            $"/culture/set?c={Uri.EscapeDataString(culture!)}&redirect={Uri.EscapeDataString(current)}",
            forceLoad: true);
        await Task.CompletedTask;
    }
}
```
Requires `@using JobForge.Ui.Localization` in `_Imports.razor` (add it in this task).

No Header, trocar o `@onclick` do botão da palette por:
```razor
@onclick="() => OnOpenPalette.InvokeAsync()"
```
O `MainLayout` injeta a referência `<CommandPalette @ref="_palette" />` e passa
`OnOpenPalette="OpenPaletteAsync"`.

- [ ] **Step 5.6: Implementar BottomNav**

Create `src/JobForge.Ui/Components/Shell/BottomNav.razor`:
```razor
@inject NavigationManager Navigation
@inject IStringLocalizer<SharedResources> L

<nav class="fixed bottom-0 left-0 right-0 z-30 grid grid-cols-5 border-t border-[var(--border)] bg-[var(--card)] md:hidden">
    @foreach (var item in Items)
    {
        var active = NavRoute.IsActive(new Uri(Navigation.Uri).AbsolutePath, item.Href);
        <a href="@item.Href" data-testid="bottom-@item.Key"
           class="flex flex-col items-center gap-0.5 py-2 text-[10px] @(active ? "text-[var(--primary)]" : "text-[var(--muted-foreground)]")">
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="@item.Icon" />
            </svg>
            <span>@L[item.LabelKey]</span>
        </a>
    }
</nav>

@code {
    [Parameter] public EventCallback OnMenuClick { get; set; }

    private static readonly (string Key, string Href, string LabelKey, string Icon)[] Items =
    [
        ("overview", "/", "nav_overview", "M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"),
        ("jobs", "/jobs", "nav_jobs", "M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"),
        ("new_job", "/jobs/new", "nav_new_job", "M12 4v16m8-8H4"),
        ("executions", "/executions", "nav_executions", "M4 6h16M4 12h16M4 18h10"),
        ("operations", "/operations", "nav_operations", "M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z")
    ];
}
```

- [ ] **Step 5.7: Implementar MainLayout**

Replace `src/JobForge.Ui/Components/Layout/MainLayout.razor`:
```razor
@inherits LayoutComponentBase
@implements IDisposable
@inject IJSRuntime JS
@inject IStringLocalizer<SharedResources> L

<div class="min-h-screen bg-[var(--background)]">
    <Sidebar IsCollapsed="@(_mobile ? !_showMobileSidebar : _collapsed)" IsMobile="@_mobile" OnToggle="ToggleSidebar" OnClose="CloseSidebar" />
    <div class="@ContentClass">
        <Header OnToggleSidebar="ToggleMobileSidebar" OnOpenPalette="OpenPaletteAsync" />
        <main class="p-4 md:p-6">
            @Body
        </main>
    </div>
    @if (_mobile)
    {
        <BottomNav OnMenuClick="ToggleMobileSidebar" />
    }
    <CommandPalette @ref="_palette" />
    <ToastContainer />
</div>

<div id="blazor-error-ui" data-nosnippet>
    @L["common_error"]
    <a href="." class="reload">@L["common_refresh"]</a>
    <span class="dismiss">×</span>
</div>

@code {
    private bool _collapsed;
    private bool _mobile;
    private bool _showMobileSidebar;
    private bool _jsReady;
    private DotNetObjectReference<MainLayout>? _dotNetRef;
    private CommandPalette? _palette;

    private string ContentClass => _mobile
        ? "transition-all duration-300"
        : _collapsed ? "ml-[72px] transition-all duration-300" : "ml-[260px] transition-all duration-300";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_jsReady)
        {
            return;
        }

        _jsReady = true;
        try
        {
            _collapsed = await JS.InvokeAsync<bool>("getSidebarCollapsed");
            _mobile = await JS.InvokeAsync<bool>("getIsMobile");
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("addResizeListener", _dotNetRef);
            StateHasChanged();
        }
        catch (JSDisconnectedException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }
    }

    private async Task ToggleSidebar()
    {
        _collapsed = !_collapsed;
        try { await JS.InvokeVoidAsync("setSidebarCollapsed", _collapsed); } catch { }
    }

    private void ToggleMobileSidebar() => _showMobileSidebar = !_showMobileSidebar;

    private void CloseSidebar() => _showMobileSidebar = false;

    private async Task OpenPaletteAsync()
    {
        if (_palette is not null)
        {
            await _palette.OpenAsync();
        }
    }

    [JSInvokable]
    public void OnResize(bool isMobile)
    {
        _mobile = isMobile;
        _showMobileSidebar = false;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        try { _dotNetRef?.Dispose(); } catch { }
        _dotNetRef = null;
    }
}
```
Correção necessária: `_mobile` é usado no primeiro render antes do JS; `OnAfterRenderAsync` deve correr mesmo quando `_mobile` é `false` inicial. Remover `|| _mobile` do guard e usar `_jsReady` apenas. Adicionar `@implements IDisposable`.

- [ ] **Step 5.8: Correr e ver passar**

Pré-requisito: Task 6 já executada (o `CommandPalette` existe).

Run:
```powershell
dotnet build JobForge.Ui.slnx
dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter ShellTests
```
Expected: build 0 erros; ShellTests PASS.

- [ ] **Step 5.9: Commit**

```powershell
git add src/JobForge.Ui/Components/Shell src/JobForge.Ui/Components/Layout/MainLayout.razor src/JobForge.Ui/Components/_Imports.razor tests/JobForge.Ui.Tests/ShellTests.cs
git rm src/JobForge.Ui/Components/Layout/NavMenu.razor
git commit -m "feat(ui): dashboard shell with sidebar, header and mobile nav"
```

---

### Task 6: Command palette

**Files:**
- Create: `src/JobForge.Ui/Components/Ui/CommandPalette.razor`
- Test: `tests/JobForge.Ui.Tests/CommandPaletteTests.cs`

**Interfaces:**
- Consumes: `NavRoute`, `IStringLocalizer<SharedResources>`.
- Produces: `CommandPalette.OpenAsync()` público; `data-testid="command-palette"`, `command-input`, `command-item`.

- [ ] **Step 6.1: Escrever os testes**

Create `tests/JobForge.Ui.Tests/CommandPaletteTests.cs`:
```csharp
using JobForge.Ui.Components.Ui;
using Microsoft.AspNetCore.Components;

namespace JobForge.Ui.Tests;

public sealed class CommandPaletteTests : UiTestContext
{
    [Fact]
    public async Task Open_shows_pages_and_actions()
    {
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenAsync());

        Assert.NotNull(cut.Find("[data-testid=command-palette]"));
        Assert.True(cut.FindAll("[data-testid=command-item]").Count >= 8);
    }

    [Fact]
    public async Task Search_filters_items()
    {
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenAsync());

        cut.Find("[data-testid=command-input]").Input("operations");

        var items = cut.FindAll("[data-testid=command-item]");
        Assert.Single(items);
        Assert.Contains("operations", items[0].GetAttribute("data-href"));
    }

    [Fact]
    public async Task Selecting_navigates()
    {
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenAsync());

        cut.Find("[data-testid=command-input]").Input("jobs");
        cut.Find("[data-testid=command-item]").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/jobs", nav.Uri);
    }
}
```

- [ ] **Step 6.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter CommandPaletteTests`
Expected: FAIL.

- [ ] **Step 6.3: Implementar**

Create `src/JobForge.Ui/Components/Ui/CommandPalette.razor` (adaptado de
`DashboardTheKingThePower/src/DashboardTheKingThePower/Components/ui/CommandPalette.razor`,
sem ícones emoji e com itens do jobforge):
```razor
@inject IJSRuntime JS
@inject NavigationManager Navigation
@inject IStringLocalizer<SharedResources> L

@if (_open)
{
    <div class="fixed inset-0 z-[100] flex items-start justify-center pt-[20vh]" data-testid="command-palette">
        <div class="absolute inset-0 bg-black/60" @onclick="Close"></div>
        <div class="relative mx-4 w-full max-w-lg overflow-hidden rounded-xl border border-[var(--border)] bg-[var(--background)] shadow-2xl">
            <div class="flex items-center border-b border-[var(--border)] px-4">
                <svg class="h-5 w-5 flex-shrink-0 text-[var(--muted-foreground)]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
                <input @ref="_input" data-testid="command-input" type="text"
                       class="h-12 flex-1 bg-transparent px-3 text-sm focus:outline-none"
                       placeholder="@(L["command_placeholder"])"
                       value="@_term" @oninput="OnSearch" @onkeydown="OnKeyDown" />
                <kbd class="rounded border border-[var(--border)] bg-[var(--secondary)] px-1.5 py-0.5 text-[10px] text-[var(--muted-foreground)]">ESC</kbd>
            </div>

            <div class="max-h-80 overflow-y-auto p-2">
                @if (!_items.Any())
                {
                    <p class="py-8 text-center text-sm text-[var(--muted-foreground)]">@L["command_no_results"]</p>
                }
                else
                {
                    @foreach (var item in _items)
                    {
                        <button type="button" data-testid="command-item" data-href="@item.Href"
                                class="flex w-full items-center justify-between rounded-lg px-3 py-2.5 text-left text-sm hover:bg-[var(--secondary)]"
                                @onclick="() => Execute(item)">
                            <span>@L[item.LabelKey]</span>
                            <span class="text-[10px] uppercase text-[var(--muted-foreground)]">
                                @(item.Group == "pages" ? L["command_pages"] : L["command_actions"])
                            </span>
                        </button>
                    }
                }
            </div>
        </div>
    </div>
}

@code {
    private static readonly (string LabelKey, string Href, string Group)[] All =
    [
        ("nav_overview", "/", "pages"),
        ("nav_jobs", "/jobs", "pages"),
        ("nav_new_job", "/jobs/new", "pages"),
        ("nav_executions", "/executions", "pages"),
        ("nav_dead_letters", "/executions?status=dead", "pages"),
        ("nav_operations", "/operations", "pages"),
        ("command_new_job", "/jobs/new", "actions"),
        ("command_toggle_theme", "action:theme", "actions")
    ];

    private bool _open;
    private string _term = "";
    private ElementReference _input;
    private List<(string LabelKey, string Href, string Group)> _items = All.ToList();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var reference = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("addKeydownListener", reference);
        }
    }

    public async Task OpenAsync()
    {
        _open = true;
        _term = "";
        _items = All.ToList();
        StateHasChanged();
        await Task.Delay(50);
        try { await _input.FocusAsync(); } catch { }
    }

    [JSInvokable]
    public Task Open() => OpenAsync();

    private void Close()
    {
        _open = false;
        _term = "";
    }

    private void OnSearch(ChangeEventArgs e)
    {
        _term = e.Value?.ToString() ?? "";
        _items = string.IsNullOrWhiteSpace(_term)
            ? All.ToList()
            : All.Where(i => L[i.LabelKey].Value.Contains(_term, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            Close();
        }
    }

    private void Execute((string LabelKey, string Href, string Group) item)
    {
        Close();
        if (item.Href == "action:theme")
        {
            _ = JS.InvokeVoidAsync("toggleTheme");
            return;
        }

        Navigation.NavigateTo(item.Href);
    }
}
```

- [ ] **Step 6.4: Correr e ver passar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter CommandPaletteTests`
Expected: PASS.

- [ ] **Step 6.5: Commit**

```powershell
git add src/JobForge.Ui/Components/Ui/CommandPalette.razor tests/JobForge.Ui.Tests/CommandPaletteTests.cs
git commit -m "feat(ui): command palette with jobforge routes and actions"
```

---

### Task 7: Overview metrics and data loader

**Files:**
- Create: `src/JobForge.Ui/Services/OverviewMetrics.cs`
- Create: `src/JobForge.Ui/Services/OverviewData.cs`
- Test: `tests/JobForge.Ui.Tests/OverviewMetricsTests.cs`
- Modify: `tests/JobForge.Ui.Tests/FakeJobForgeClient.cs`

**Interfaces:**
- Consumes: `JobDto`, `ExecutionDto`, `ExecutionPage`, `IJobForgeClient`, `TimeProvider`.
- Produces: `record StatusCounts(int Pending, int Running, int Succeeded, int Failed, int Dead)`; `record HourBucket(string Label, int Count)`; `record OverviewMetrics(int TotalJobs, int ActiveJobs, double SuccessRate, int DeadLetters, TimeSpan? AverageDuration, IReadOnlyList<HourBucket> Hourly, StatusCounts ByStatus)` com `bool HasData`; `static OverviewMetrics From(IReadOnlyList<JobDto> jobs, IReadOnlyList<ExecutionDto> window, int deadTotal, DateTime nowUtc)`; `static Task<OverviewMetrics> OverviewData.LoadAsync(IJobForgeClient client, TimeProvider clock, CancellationToken ct)`.

- [ ] **Step 7.1: Ampliar o fake para responder por status**

Modify `tests/JobForge.Ui.Tests/FakeJobForgeClient.cs` — adicionar a propriedade e o ramo:
```csharp
public Dictionary<string, ExecutionPage> ExecutionsByStatus { get; } = new(StringComparer.OrdinalIgnoreCase);
public List<int> RequestedSizes { get; } = [];
```
Dentro de `GetExecutionsAsync`, logo após `GetExecutionsCalls++;`:
```csharp
RequestedSizes.Add(size);
```
Antes do `return Task.FromResult(Executions);`:
```csharp
if (status is not null && ExecutionsByStatus.TryGetValue(status, out var page))
{
    return Task.FromResult(page);
}
```
O ramo de erro continua primeiro.

- [ ] **Step 7.2: Escrever os testes**

Create `tests/JobForge.Ui.Tests/OverviewMetricsTests.cs`:
```csharp
using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class OverviewMetricsTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid JobId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static JobDto Job(bool enabled) => new(
        JobId, "job", "https://example.test/hook", null, "POST", null, 5, 30, enabled, null, null);

    private static ExecutionDto Execution(string status, int minuteOffset, int? durationSeconds = null)
    {
        var scheduled = Now.AddMinutes(-minuteOffset);
        return new ExecutionDto(Guid.NewGuid(), JobId, status, 1, 200, null, scheduled,
            durationSeconds is null ? null : scheduled,
            durationSeconds is null ? null : scheduled.AddSeconds(durationSeconds.Value),
            "trace");
    }

    [Fact]
    public void Empty_window_zeroes_everything()
    {
        var metrics = OverviewMetrics.From([Job(true)], [], 0, Now);

        Assert.Equal(1, metrics.ActiveJobs);
        Assert.Equal(0, metrics.SuccessRate);
        Assert.Null(metrics.AverageDuration);
        Assert.False(metrics.HasData);
        Assert.Equal(24, metrics.Hourly.Count);
    }

    [Fact]
    public void Success_rate_uses_completed_executions()
    {
        ExecutionDto[] window =
        [
            Execution("succeeded", 1), Execution("succeeded", 2), Execution("succeeded", 3),
            Execution("failed", 4), Execution("dead", 5)
        ];

        var metrics = OverviewMetrics.From([Job(true)], window, 5, Now);

        Assert.Equal(0.6, metrics.SuccessRate, 3);
        Assert.Equal(3, metrics.ByStatus.Succeeded);
        Assert.Equal(1, metrics.ByStatus.Failed);
        Assert.Equal(1, metrics.ByStatus.Dead);
        Assert.True(metrics.HasData);
    }

    [Fact]
    public void Average_duration_ignores_unfinished_executions()
    {
        ExecutionDto[] window =
        [
            Execution("succeeded", 1, 10),
            Execution("succeeded", 2, 20),
            Execution("running", 3)
        ];

        var metrics = OverviewMetrics.From([], window, 0, Now);

        Assert.Equal(TimeSpan.FromSeconds(15), metrics.AverageDuration);
    }

    [Fact]
    public void Hourly_buckets_cover_24_hours_ending_now()
    {
        var metrics = OverviewMetrics.From([], [Execution("succeeded", 0)], 0, Now);

        Assert.Equal(24, metrics.Hourly.Count);
        Assert.Equal("12:00", metrics.Hourly[^1].Label);
        Assert.Equal(1, metrics.Hourly[^1].Count);
        Assert.Equal(1, metrics.Hourly.Sum(b => b.Count));
    }

    [Fact]
    public void Dead_letters_uses_the_exact_total()
    {
        var metrics = OverviewMetrics.From([], [Execution("dead", 1)], 42, Now);
        Assert.Equal(42, metrics.DeadLetters);
    }

    [Fact]
    public async Task Loader_queries_jobs_window_and_dead_total()
    {
        var fake = new FakeJobForgeClient
        {
            Jobs = [Job(true)],
            Executions = new ExecutionPage(1, 100, 1, [Execution("succeeded", 1)]),
            ExecutionsByStatus = { ["dead"] = new ExecutionPage(1, 1, 7, []) }
        };

        var metrics = await OverviewData.LoadAsync(fake, TimeProvider.System, CancellationToken.None);

        Assert.Equal(1, metrics.ActiveJobs);
        Assert.Equal(7, metrics.DeadLetters);
        Assert.Equal([100, 1], fake.RequestedSizes);
    }
}
```

- [ ] **Step 7.3: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter OverviewMetricsTests`
Expected: FAIL.

- [ ] **Step 7.4: Implementar**

Create `src/JobForge.Ui/Services/OverviewMetrics.cs`:
```csharp
namespace JobForge.Ui.Services;

public sealed record StatusCounts(int Pending, int Running, int Succeeded, int Failed, int Dead);

public sealed record HourBucket(string Label, int Count);

public sealed record OverviewMetrics(
    int TotalJobs,
    int ActiveJobs,
    double SuccessRate,
    int DeadLetters,
    TimeSpan? AverageDuration,
    IReadOnlyList<HourBucket> Hourly,
    StatusCounts ByStatus)
{
    public bool HasData => ByStatus.Pending + ByStatus.Running + ByStatus.Succeeded
                           + ByStatus.Failed + ByStatus.Dead > 0;

    public static OverviewMetrics From(
        IReadOnlyList<JobDto> jobs,
        IReadOnlyList<ExecutionDto> window,
        int deadTotal,
        DateTime nowUtc)
    {
        var counts = new StatusCounts(
            window.Count(e => e.Status == "pending"),
            window.Count(e => e.Status == "running"),
            window.Count(e => e.Status == "succeeded"),
            window.Count(e => e.Status == "failed"),
            window.Count(e => e.Status == "dead"));

        var completed = counts.Succeeded + counts.Failed + counts.Dead;
        var successRate = completed == 0 ? 0 : (double)counts.Succeeded / completed;

        var durations = window
            .Where(e => e.StartedAt is not null && e.FinishedAt is not null)
            .Select(e => e.FinishedAt!.Value - e.StartedAt!.Value)
            .ToList();
        var average = durations.Count == 0
            ? (TimeSpan?)null
            : TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));

        var end = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0, DateTimeKind.Utc);
        var start = end.AddHours(-23);
        var hourly = Enumerable.Range(0, 24)
            .Select(i =>
            {
                var hour = start.AddHours(i);
                var count = window.Count(e =>
                {
                    var scheduled = e.ScheduledAt.Kind == DateTimeKind.Utc
                        ? e.ScheduledAt
                        : e.ScheduledAt.ToUniversalTime();
                    return scheduled >= hour && scheduled < hour.AddHours(1);
                });
                return new HourBucket(hour.ToString("HH:00"), count);
            })
            .ToList();

        return new OverviewMetrics(
            jobs.Count,
            jobs.Count(j => j.Enabled),
            successRate,
            deadTotal,
            average,
            hourly,
            counts);
    }
}
```

Create `src/JobForge.Ui/Services/OverviewData.cs`:
```csharp
namespace JobForge.Ui.Services;

public static class OverviewData
{
    public const int WindowSize = 100;

    public static async Task<OverviewMetrics> LoadAsync(
        IJobForgeClient client,
        TimeProvider clock,
        CancellationToken ct = default)
    {
        var jobs = await client.GetJobsAsync(ct);
        var window = await client.GetExecutionsAsync(null, 1, WindowSize, ct);
        var dead = await client.GetExecutionsAsync("dead", 1, 1, ct);

        return OverviewMetrics.From(jobs, window.Items, dead.Total, clock.GetUtcNow().UtcDateTime);
    }
}
```

- [ ] **Step 7.5: Correr e ver passar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter OverviewMetricsTests`
Expected: PASS.

- [ ] **Step 7.6: Commit**

```powershell
git add src/JobForge.Ui/Services/OverviewMetrics.cs src/JobForge.Ui/Services/OverviewData.cs tests/JobForge.Ui.Tests/OverviewMetricsTests.cs tests/JobForge.Ui.Tests/FakeJobForgeClient.cs
git commit -m "feat(ui): overview metrics aggregation"
```

---

### Task 8: Overview page and charts

**Files:**
- Create: `src/JobForge.Ui/Components/Charts/ExecutionsAreaChart.razor`
- Create: `src/JobForge.Ui/Components/Charts/StatusDonutChart.razor`
- Create: `src/JobForge.Ui/Components/Pages/OverviewKpis.razor`
- Create: `src/JobForge.Ui/Components/Pages/Overview.razor`
- Modify: `src/JobForge.Ui/JobForge.Ui.csproj`
- Modify: `Directory.Packages.props`
- Modify: `src/JobForge.Ui/Program.cs`
- Modify: `src/JobForge.Ui/Components/_Imports.razor`
- Test: `tests/JobForge.Ui.Tests/OverviewKpisTests.cs`
- Test: `tests/JobForge.Ui.Tests/OverviewPageTests.cs`

**Interfaces:**
- Consumes: `OverviewMetrics`, `OverviewData`.
- Produces: `<OverviewKpis Metrics>` com `data-testid="kpi-active-jobs"`, `kpi-success-rate`, `kpi-dead-letters`, `kpi-avg-duration`, `overview-empty`; `<ExecutionsAreaChart Buckets>`, `<StatusDonutChart Counts>`.

- [ ] **Step 8.1: Escrever os testes**

Create `tests/JobForge.Ui.Tests/OverviewKpisTests.cs`:
```csharp
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class OverviewKpisTests : UiTestContext
{
    private static OverviewMetrics Metrics(int jobs, double rate, int dead, TimeSpan? avg) => new(
        jobs + 1, jobs, rate, dead, avg,
        Enumerable.Range(0, 24).Select(i => new HourBucket($"{i:00}:00", 0)).ToList(),
        new StatusCounts(0, 0, 3, 1, 1));

    [Fact]
    public void Renders_kpis_with_culture_aware_percentage()
    {
        using var _ = new CultureScope("pt-BR");
        var cut = Render<OverviewKpis>(p => p.Add(x => x.Metrics, Metrics(4, 0.75, 7, TimeSpan.FromSeconds(2.4))));

        Assert.Equal("4", cut.Find("[data-testid=kpi-active-jobs]").TextContent.Trim());
        Assert.Contains("75", cut.Find("[data-testid=kpi-success-rate]").TextContent);
        Assert.Equal("7", cut.Find("[data-testid=kpi-dead-letters]").TextContent.Trim());
        Assert.Contains("2,4", cut.Find("[data-testid=kpi-avg-duration]").TextContent);
    }

    [Fact]
    public void Empty_window_shows_note_instead_of_dashboards()
    {
        var empty = new OverviewMetrics(0, 0, 0, 0, null,
            Enumerable.Range(0, 24).Select(i => new HourBucket($"{i:00}:00", 0)).ToList(),
            new StatusCounts(0, 0, 0, 0, 0));

        var cut = Render<OverviewKpis>(p => p.Add(x => x.Metrics, empty));

        Assert.NotNull(cut.Find("[data-testid=overview-empty]"));
        Assert.Equal("0", cut.Find("[data-testid=kpi-active-jobs]").TextContent.Trim());
    }

    [Fact]
    public void Average_duration_is_dash_when_unknown()
    {
        var cut = Render<OverviewKpis>(p => p.Add(x => x.Metrics, Metrics(1, 0, 0, null)));
        Assert.Equal("—", cut.Find("[data-testid=kpi-avg-duration]").TextContent.Trim());
    }
}
```

Create `tests/JobForge.Ui.Tests/OverviewPageTests.cs` (o caso vazio não monta
`ApexChart`, por isso corre sem JS interop):
```csharp
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class OverviewPageTests : UiTestContext
{
    [Fact]
    public void Empty_window_renders_without_charts()
    {
        var fake = new FakeJobForgeClient
        {
            Jobs = [],
            Executions = new ExecutionPage(1, 100, 0, []),
            ExecutionsByStatus = { ["dead"] = new ExecutionPage(1, 1, 0, []) }
        };
        Services.AddSingleton<IJobForgeClient>(fake);

        var cut = Render<Overview>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=overview-empty]")));
        Assert.Equal("0", cut.Find("[data-testid=kpi-active-jobs]").TextContent.Trim());
    }
}
```

- [ ] **Step 8.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter OverviewKpisTests`
Expected: FAIL.

- [ ] **Step 8.3: Adicionar ApexCharts**

Modify `Directory.Packages.props` — adicionar:
```xml
<PackageVersion Include="Blazor-ApexCharts" Version="7.0.0" />
```
Modify `src/JobForge.Ui/JobForge.Ui.csproj` — adicionar no `ItemGroup` de pacotes:
```xml
<PackageReference Include="Blazor-ApexCharts" />
```
Modify `src/JobForge.Ui/Program.cs` — adicionar `using ApexCharts;` e, após `AddLocalization`:
```csharp
builder.Services.AddApexCharts();
```
Modify `src/JobForge.Ui/Components/_Imports.razor` — adicionar:
```razor
@using ApexCharts
@using JobForge.Ui.Components.Charts
```

- [ ] **Step 8.4: Implementar os gráficos**

Adicionar `@using System.Diagnostics.CodeAnalysis` e `@attribute [ExcludeFromCodeCoverage]`
no topo dos dois componentes de gráfico: são wrappers de render JS, verificados manualmente
no browser; a lógica testável está em `OverviewMetrics`.

Create `src/JobForge.Ui/Components/Charts/ExecutionsAreaChart.razor`:
```razor
@using ApexCharts

<ApexChart TItem="HourBucket" Options="_options" Height="260">
    <ApexPointSeries TItem="HourBucket"
                     Items="Buckets"
                     Name="@(L["overview_chart_hourly"])"
                     SeriesType="SeriesType.Area"
                     XValue="b => b.Label"
                     YValue="b => b.Count" />
</ApexChart>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<HourBucket> Buckets { get; set; } = [];
    [Inject] private IStringLocalizer<SharedResources> L { get; set; } = default!;

    private readonly ApexChartOptions<HourBucket> _options = new()
    {
        Chart = new Chart { Toolbar = new Toolbar { Show = false }, Animations = new Animations { Enabled = true, Speed = 400 } },
        Colors = ["var(--chart-1)"],
        Stroke = new Stroke { Curve = Curve.Smooth, Width = 2 },
        Grid = new Grid { BorderColor = "var(--border)", StrokeDashArray = 4 },
        Legend = new Legend { Show = false },
        DataLabels = new DataLabels { Enabled = false },
        Xaxis = new XAxis { Labels = new XAxisLabels { Style = new AxisLabelsStyle { Colors = "var(--muted-foreground)" } } },
        Yaxis = new List<YAxis> { new() { Labels = new YAxisLabels { Style = new AxisLabelsStyle { Colors = "var(--muted-foreground)" } } } },
        Tooltip = new Tooltip { Theme = Mode.Dark }
    };
}
```

Create `src/JobForge.Ui/Components/Charts/StatusDonutChart.razor`:
```razor
@using ApexCharts

<ApexChart TItem="Slice" Options="_options" Height="260">
    <ApexPointSeries TItem="Slice"
                     Items="Slices"
                     Name="Count"
                     SeriesType="SeriesType.Donut"
                     XValue="s => s.Label"
                     YValue="s => s.Count" />
</ApexChart>

@code {
    [Parameter, EditorRequired] public StatusCounts Counts { get; set; } = new(0, 0, 0, 0, 0);
    [Inject] private IStringLocalizer<SharedResources> L { get; set; } = default!;

    private sealed record Slice(string Label, int Count);

    private IReadOnlyList<Slice> Slices => new List<Slice>
    {
        new(L["status_succeeded"], Counts.Succeeded),
        new(L["status_failed"], Counts.Failed),
        new(L["status_dead"], Counts.Dead),
        new(L["status_running"], Counts.Running),
        new(L["status_pending"], Counts.Pending)
    };

    private readonly ApexChartOptions<Slice> _options = new()
    {
        Chart = new Chart { Animations = new Animations { Enabled = true, Speed = 400 } },
        Colors = ["var(--chart-2)", "var(--chart-5)", "var(--chart-4)", "var(--chart-1)", "var(--chart-3)"],
        Labels = ["succeeded", "failed", "dead", "running", "pending"],
        Legend = new Legend { Position = LegendPosition.Bottom },
        Stroke = new Stroke { Width = 2, Colors = ["var(--card)"] },
        DataLabels = new DataLabels { Enabled = true }
    };
}
```

- [ ] **Step 8.5: Implementar os KPIs e a página**

Create `src/JobForge.Ui/Components/Pages/OverviewKpis.razor`:
```razor
@inject IStringLocalizer<SharedResources> L

<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
    <Card>
        <p class="text-sm text-[var(--muted-foreground)]">@L["overview_active_jobs"]</p>
        <p class="mt-1 text-2xl font-bold" data-testid="kpi-active-jobs">@Metrics.ActiveJobs</p>
        <p class="mt-1 text-xs text-[var(--muted-foreground)]">@L["overview_total_jobs", Metrics.TotalJobs]</p>
    </Card>
    <Card>
        <p class="text-sm text-[var(--muted-foreground)]">@L["overview_success_rate"]</p>
        <p class="mt-1 text-2xl font-bold" data-testid="kpi-success-rate">@Metrics.SuccessRate.ToString("P0")</p>
    </Card>
    <Card>
        <p class="text-sm text-[var(--muted-foreground)]">@L["overview_dead_letters"]</p>
        <p class="mt-1 text-2xl font-bold" data-testid="kpi-dead-letters">@Metrics.DeadLetters</p>
    </Card>
    <Card>
        <p class="text-sm text-[var(--muted-foreground)]">@L["overview_avg_duration"]</p>
        <p class="mt-1 text-2xl font-bold" data-testid="kpi-avg-duration">
            @(Metrics.AverageDuration is null ? "—" : Metrics.AverageDuration.Value.TotalSeconds.ToString("0.0") + " s")
        </p>
    </Card>
</div>

@if (!Metrics.HasData)
{
    <Card Class="mt-4">
        <p class="text-sm text-[var(--muted-foreground)]" data-testid="overview-empty">@L["overview_no_data"]</p>
    </Card>
}

@code {
    [Parameter, EditorRequired] public OverviewMetrics Metrics { get; set; } = default!;
}
```

Create `src/JobForge.Ui/Components/Pages/Overview.razor`:
```razor
@page "/"
@implements IAsyncDisposable
@inject IJobForgeClient Client
@inject TimeProvider Clock
@inject IStringLocalizer<SharedResources> L

<PageTitle>jobforge — @L["overview_title"]</PageTitle>

<div class="mb-6 flex flex-wrap items-center justify-between gap-3">
    <div>
        <h1 class="text-2xl font-bold">@L["overview_title"]</h1>
        <p class="text-sm text-[var(--muted-foreground)]">@L["overview_window_note", OverviewData.WindowSize]</p>
    </div>
    <Button Variant="secondary" Size="sm" TestId="overview-refresh" OnClick="LoadAsync">@L["common_refresh"]</Button>
</div>

@if (_error is not null)
{
    <div class="mb-4 rounded-lg border border-[var(--destructive)] p-3 text-sm" data-testid="error-alert">@_error</div>
}

@if (_metrics is null)
{
    <p class="text-sm text-[var(--muted-foreground)]">@L["common_loading"]</p>
}
else
{
    <OverviewKpis Metrics="_metrics" />

    @if (_metrics.HasData)
    {
        <div class="mt-6 grid gap-4 lg:grid-cols-2">
            <Card>
                <Header><h2 class="font-semibold">@L["overview_chart_hourly"]</h2></Header>
                <ExecutionsAreaChart Buckets="_metrics.Hourly" />
            </Card>
            <Card>
                <Header><h2 class="font-semibold">@L["overview_chart_status"]</h2></Header>
                <StatusDonutChart Counts="_metrics.ByStatus" />
            </Card>
        </div>
    }

    <Card Class="mt-6">
        <Header><h2 class="font-semibold">@L["overview_last_executions"]</h2></Header>
        <div class="overflow-x-auto">
            <table class="w-full text-sm">
                <thead>
                    <tr class="text-left text-xs uppercase text-[var(--muted-foreground)]">
                        <th class="pb-2">@L["exec_scheduled"]</th>
                        <th class="pb-2">@L["exec_job"]</th>
                        <th class="pb-2">@L["exec_status"]</th>
                        <th class="pb-2">@L["exec_attempt"]</th>
                        <th class="pb-2">@L["exec_http"]</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var execution in _recent)
                    {
                        <tr class="border-t border-[var(--border)]" data-testid="overview-execution-row">
                            <td class="py-2">
                                <a class="hover:underline" href="@($"/executions/{execution.Id}")">
                                    @execution.ScheduledAt.ToLocalTime().ToString("g")
                                </a>
                            </td>
                            <td class="py-2 font-mono text-xs">@execution.JobId.ToString()[..8]</td>
                            <td class="py-2"><StatusBadge Status="@execution.Status" /></td>
                            <td class="py-2">@execution.Attempt</td>
                            <td class="py-2">@(execution.HttpStatusCode?.ToString() ?? "—")</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </Card>
}

@code {
    private const int RefreshSeconds = 30;
    private OverviewMetrics? _metrics;
    private IReadOnlyList<ExecutionDto> _recent = [];
    private string? _error;
    private bool _loading;
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        _cts = new CancellationTokenSource();
        await LoadAsync();
        _ = RefreshLoopAsync(_cts.Token);
    }

    private async Task LoadAsync()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            _metrics = await OverviewData.LoadAsync(Client, Clock, _cts?.Token ?? CancellationToken.None);
            var page = await Client.GetExecutionsAsync(null, 1, 10, _cts?.Token ?? CancellationToken.None);
            _recent = page.Items;
            _error = null;
        }
        catch (JobForgeApiException ex)
        {
            _error = ex.Message;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task RefreshLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(RefreshSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await InvokeAsync(LoadAsync);
            }
        }
        catch (OperationCanceledException)
        {
            // circuit closed
        }
        catch (ObjectDisposedException)
        {
            // circuit disposed between ticks
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
    }
}
```

- [ ] **Step 8.6: Correr e ver passar**

Run:
```powershell
dotnet build JobForge.Ui.slnx
dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter OverviewKpisTests
```
Expected: build 0 erros, testes PASS.

- [ ] **Step 8.7: Verificação manual dos gráficos**

Run:
```powershell
cd D:\Projetos\jobforge-ui
dotnet run --project src/JobForge.Ui
```
Abrir `http://localhost:8096`, confirmar no browser: gráfico de área com 24 barras/horas, donut com as 5 fatias, nenhum watermark "APEXCHARTS" (features premium não usadas).

- [ ] **Step 8.8: Commit**

```powershell
git add Directory.Packages.props src/JobForge.Ui/JobForge.Ui.csproj src/JobForge.Ui/Program.cs src/JobForge.Ui/Components/_Imports.razor src/JobForge.Ui/Components/Charts src/JobForge.Ui/Components/Pages/Overview.razor src/JobForge.Ui/Components/Pages/OverviewKpis.razor tests/JobForge.Ui.Tests/OverviewKpisTests.cs
git commit -m "feat(ui): overview page with KPIs and apexcharts"
```

---

### Task 9: Jobs page

**Files:**
- Create: `src/JobForge.Ui/Components/Pages/Jobs.razor` (move de `/` para `/jobs`)
- Delete: conteúdo antigo de `Jobs.razor` substituído
- Modify: `tests/JobForge.Ui.Tests/JobsPageTests.cs`
- Test: `tests/JobForge.Ui.Tests/JobsPageTests.cs`

**Interfaces:**
- Consumes: `IJobForgeClient`, `ToastService`, `Modal`, `StatusBadge`.
- Produces: rota `/jobs`; mantém `job-row`, `job-run-button`, `job-disable-button`, `new-job-link`, `jobs-empty`; adiciona `jobs-search`, `jobs-no-match`, `confirm-disable`.

- [ ] **Step 9.1: Atualizar os testes**

Replace `tests/JobForge.Ui.Tests/JobsPageTests.cs`:
```csharp
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class JobsPageTests : UiTestContext
{
    private static readonly Guid JobId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private FakeJobForgeClient Register(FakeJobForgeClient? fake = null)
    {
        fake ??= new FakeJobForgeClient
        {
            Jobs =
            [
                new JobDto(JobId, "send-report", "https://example.test/hook", null,
                    "GET", "*/5 * * * *", 3, 30, true,
                    Guid.Parse("22222222-2222-2222-2222-222222222222"), "succeeded")
            ]
        };
        Services.AddSingleton<IJobForgeClient>(fake);
        Services.AddSingleton(new ToastService());
        return fake;
    }

    [Fact]
    public async Task Renders_job_row_with_last_status_badge()
    {
        Register();
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid=job-row]")));

        Assert.Contains("send-report", cut.Markup);
        Assert.Contains("*/5 * * * *", cut.Markup);
        var badge = cut.Find("[data-testid=job-row]").QuerySelector("[data-testid=status-badge]")!;
        Assert.Equal("succeeded", badge.GetAttribute("data-status"));
        Assert.NotEmpty(cut.FindAll("[data-testid=job-run-button]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=job-disable-button]"));
    }

    [Fact]
    public async Task Empty_list_shows_empty_state()
    {
        Register(new FakeJobForgeClient { Jobs = [] });
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=jobs-empty]")));
    }

    [Fact]
    public async Task Search_filters_by_name()
    {
        Register(new FakeJobForgeClient
        {
            Jobs =
            [
                new JobDto(JobId, "send-report", "https://a.test", null, "GET", null, 5, 30, true, null, null),
                new JobDto(Guid.NewGuid(), "cleanup", "https://b.test", null, "GET", null, 5, 30, true, null, null)
            ]
        });
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-testid=job-row]").Count));

        cut.Find("[data-testid=jobs-search]").Input("clean");

        Assert.Single(cut.FindAll("[data-testid=job-row]"));
        Assert.Contains("cleanup", cut.Markup);
    }

    [Fact]
    public async Task Api_failure_shows_error_alert()
    {
        Register(new FakeJobForgeClient { JobsError = new JobForgeApiException(500, "orchestrator exploded") });
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Contains("orchestrator exploded", cut.Markup));

        Assert.NotNull(cut.Find("[data-testid=error-alert]"));
    }

    [Fact]
    public async Task Run_now_navigates_to_execution_detail()
    {
        var fake = Register();
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=job-run-button]")));

        cut.Find("[data-testid=job-run-button]").Click();
        await Task.Yield();

        Assert.Equal(JobId, fake.LastRunJobId);
        Assert.EndsWith($"/executions/{fake.RunResult.Id}",
            Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Disable_requires_confirmation_then_reloads()
    {
        var fake = Register();
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=job-disable-button]")));

        cut.Find("[data-testid=job-disable-button]").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=confirm-disable]")));

        cut.Find("[data-testid=confirm-disable]").Click();
        await Task.Yield();

        Assert.Equal(JobId, fake.LastDisableJobId);
        Assert.Equal(2, fake.GetJobsCalls);
    }
}
```

- [ ] **Step 9.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter JobsPageTests`
Expected: FAIL (rota/confirm/testids novos).

- [ ] **Step 9.3: Implementar**

Replace `src/JobForge.Ui/Components/Pages/Jobs.razor`:
```razor
@page "/jobs"
@inject IJobForgeClient Client
@inject NavigationManager Nav
@inject ToastService Toasts
@inject IStringLocalizer<SharedResources> L

<PageTitle>jobforge — @L["jobs_title"]</PageTitle>

<div class="mb-6 flex flex-wrap items-center justify-between gap-3">
    <h1 class="text-2xl font-bold">@L["jobs_title"]</h1>
    <div class="flex items-center gap-2">
        <input data-testid="jobs-search" class="h-10 w-56 rounded-lg border border-[var(--border)] bg-[var(--card)] px-3 text-sm"
               placeholder="@(L["jobs_search_placeholder"])" @bind="_search" @bind:event="oninput" />
        <a href="/jobs/new" data-testid="new-job-link"
           class="inline-flex h-10 items-center rounded-lg bg-[var(--primary)] px-4 text-sm font-medium text-[var(--primary-foreground)]">
            @L["header_new_job"]
        </a>
    </div>
</div>

@if (_error is not null)
{
    <div class="mb-4 rounded-lg border border-[var(--destructive)] p-3 text-sm" data-testid="error-alert">@_error</div>
}

@if (_loading && _jobs is null)
{
    <p class="text-sm text-[var(--muted-foreground)]">@L["common_loading"]</p>
}
else if (_jobs is { Count: 0 })
{
    <Card><p class="text-sm text-[var(--muted-foreground)]" data-testid="jobs-empty">@L["jobs_empty"]</p></Card>
}
else if (Filtered.Count == 0)
{
    <Card><p class="text-sm text-[var(--muted-foreground)]" data-testid="jobs-no-match">@L["jobs_no_match"]</p></Card>
}
else
{
    <Card>
        <div class="overflow-x-auto">
            <table class="w-full text-sm">
                <thead>
                    <tr class="text-left text-xs uppercase text-[var(--muted-foreground)]">
                        <th class="pb-2">@L["jobs_name"]</th>
                        <th class="pb-2">@L["jobs_url"]</th>
                        <th class="pb-2">@L["jobs_cron"]</th>
                        <th class="pb-2">@L["jobs_enabled"]</th>
                        <th class="pb-2">@L["jobs_last_run"]</th>
                        <th class="pb-2"></th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var job in Filtered)
                    {
                        <tr class="border-t border-[var(--border)]" data-testid="job-row">
                            <td class="py-2 font-medium">@job.Name</td>
                            <td class="max-w-[22rem] truncate py-2 text-[var(--muted-foreground)]">@job.WebhookUrl</td>
                            <td class="py-2 font-mono text-xs">@(job.Cron ?? "—")</td>
                            <td class="py-2">
                                <Badge Variant="@(job.Enabled ? "success" : "neutral")">
                                    @(job.Enabled ? L["jobs_enabled"] : L["jobs_disabled"])
                                </Badge>
                            </td>
                            <td class="py-2">
                                @if (job.ExecutionId is not null && job.ExecutionStatus is not null)
                                {
                                    <a href="@($"/executions/{job.ExecutionId}")" data-testid="job-last-execution">
                                        <StatusBadge Status="@job.ExecutionStatus" />
                                    </a>
                                }
                                else
                                {
                                    <span class="text-[var(--muted-foreground)]">@L["common_never"]</span>
                                }
                            </td>
                            <td class="py-2 text-right">
                                @if (job.Enabled)
                                {
                                    <Button Variant="secondary" Size="sm" TestId="job-run-button"
                                            Disabled="@(_busyId == job.Id)" OnClick="() => RunAsync(job.Id)">
                                        @L["jobs_run_now"]
                                    </Button>
                                    <Button Variant="danger" Size="sm" TestId="job-disable-button"
                                            Disabled="@(_busyId == job.Id)" OnClick="() => AskDisable(job)">
                                        @L["jobs_disable"]
                                    </Button>
                                }
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </Card>
}

<Modal IsOpen="_disableTarget is not null" Title="@(L["jobs_disable_title"])"
       OnClose="() => { _disableTarget = null; }">
    <p class="text-sm text-[var(--muted-foreground)]">
        @L["jobs_disable_body", _disableTarget?.Name ?? ""]
    </p>
    <Footer>
        <Button Variant="secondary" OnClick="() => { _disableTarget = null; }">@L["common_cancel"]</Button>
        <Button Variant="danger" TestId="confirm-disable" OnClick="ConfirmDisableAsync">@L["jobs_disable"]</Button>
    </Footer>
</Modal>

@code {
    private List<JobDto>? _jobs;
    private bool _loading;
    private string? _error;
    private string _search = "";
    private Guid? _busyId;
    private JobDto? _disableTarget;

    private IReadOnlyList<JobDto> Filtered => _jobs is null
        ? []
        : _jobs.Where(j => string.IsNullOrWhiteSpace(_search)
                           || j.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)).ToList();

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _jobs = (await Client.GetJobsAsync()).ToList();
            _error = null;
        }
        catch (JobForgeApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private void AskDisable(JobDto job) => _disableTarget = job;

    private async Task ConfirmDisableAsync()
    {
        if (_disableTarget is null) return;
        var id = _disableTarget.Id;
        _disableTarget = null;
        _busyId = id;
        try
        {
            await Client.DisableJobAsync(id);
            Toasts.ShowSuccess(L["jobs_disabled_toast"]);
            await LoadAsync();
        }
        catch (JobForgeApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busyId = null;
        }
    }

    private async Task RunAsync(Guid jobId)
    {
        _busyId = jobId;
        try
        {
            var execution = await Client.RunJobAsync(jobId);
            Nav.NavigateTo($"/executions/{execution.Id}");
        }
        catch (JobForgeApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busyId = null;
        }
    }
}
```

- [ ] **Step 9.4: Correr e ver passar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter JobsPageTests`
Expected: PASS.

- [ ] **Step 9.5: Commit**

```powershell
git add src/JobForge.Ui/Components/Pages/Jobs.razor tests/JobForge.Ui.Tests/JobsPageTests.cs
git commit -m "feat(ui): jobs page with search and disable confirmation"
```

---

### Task 10: New job page

**Files:**
- Create: `src/JobForge.Ui/Services/CronPreview.cs`
- Modify: `src/JobForge.Ui/Components/Pages/NewJob.razor`
- Modify: `tests/JobForge.Ui.Tests/NewJobPageTests.cs`

**Interfaces:**
- Consumes: `Cronos`, `IJobForgeClient`, `ToastService`.
- Produces: `static IReadOnlyList<DateTime> CronPreview.Next(string cron, DateTime fromUtc, int count)`; mantém `job-name-input`, `job-url-input`, `job-payload-input`, `job-cron-input`, `submit-button`, `form-error`; adiciona `cron-preview`, `preset-cron`.

- [ ] **Step 10.1: Escrever os testes**

Modify `tests/JobForge.Ui.Tests/NewJobPageTests.cs` (mantém os testes existentes,
ajusta a rota de destino e acrescenta os novos casos):
```csharp
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class NewJobPageTests : UiTestContext
{
    private FakeJobForgeClient Register(FakeJobForgeClient? fake = null)
    {
        fake ??= new FakeJobForgeClient();
        Services.AddSingleton<IJobForgeClient>(fake);
        Services.AddSingleton(new ToastService());
        return fake;
    }

    private static void FillValidForm(IRenderedComponent<NewJob> cut)
    {
        cut.Find("[data-testid=job-name-input]").Change("demo");
        cut.Find("[data-testid=job-url-input]").Change("https://example.test/hook");
    }

    [Fact]
    public void Cron_preview_returns_next_three_occurrences_utc()
    {
        var next = CronPreview.Next("0 9 * * *", new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), 3);

        Assert.Equal(3, next.Count);
        Assert.Equal(new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc), next[0]);
        Assert.Equal(new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc), next[1]);
        Assert.Equal(new DateTime(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc), next[2]);
    }

    [Fact]
    public void Cron_preview_returns_empty_for_invalid_expression()
        => Assert.Empty(CronPreview.Next("not a cron", DateTime.UtcNow, 3));

    [Fact]
    public void Renders_three_preview_rows_for_daily_cron()
    {
        Register();
        var cut = Render<NewJob>();

        cut.Find("[data-testid=job-cron-input]").Change("0 9 * * *");

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid=cron-preview]").Count));
    }

    [Fact]
    public void Preset_fills_the_cron_input()
    {
        Register();
        var cut = Render<NewJob>();

        cut.FindAll("[data-testid=preset-cron]")[0].Click();

        Assert.Equal("*/5 * * * *", cut.Find("[data-testid=job-cron-input]").GetAttribute("value"));
    }

    [Fact]
    public async Task Empty_submit_shows_field_errors_without_calling_api()
    {
        var fake = Register();
        var cut = Render<NewJob>();
        await Task.Yield();

        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.NotNull(cut.Find("[data-testid=name-error]"));
        Assert.NotNull(cut.Find("[data-testid=url-error]"));
        Assert.Null(fake.LastCreateRequest);
    }

    [Fact]
    public async Task Invalid_cron_and_payload_are_rejected_client_side()
    {
        var fake = Register();
        var cut = Render<NewJob>();
        await Task.Yield();
        FillValidForm(cut);

        cut.Find("[data-testid=job-cron-input]").Change("not-a-cron");
        cut.Find("[data-testid=job-payload-input]").Change("{oops");
        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.NotNull(cut.Find("[data-testid=cron-error]"));
        Assert.NotNull(cut.Find("[data-testid=payload-error]"));
        Assert.Null(fake.LastCreateRequest);
    }

    [Fact]
    public async Task Valid_submit_creates_job_with_fresh_idempotency_key_and_navigates()
    {
        var fake = Register();
        var cut = Render<NewJob>();
        await Task.Yield();
        FillValidForm(cut);
        cut.Find("[data-testid=job-cron-input]").Change("*/5 * * * *");

        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.NotNull(fake.LastCreateRequest);
        Assert.Equal("demo", fake.LastCreateRequest!.Name);
        Assert.Equal("https://example.test/hook", fake.LastCreateRequest.WebhookUrl);
        Assert.Equal("*/5 * * * *", fake.LastCreateRequest.Cron);
        Assert.Matches("^[0-9a-f]{32}$", fake.LastIdempotencyKey);
        Assert.EndsWith("/jobs", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Api_failure_shows_form_error_and_keeps_form()
    {
        var fake = Register();
        fake.CreateError = new JobForgeApiException(400, "cron invalid");
        var cut = Render<NewJob>();
        await Task.Yield();
        FillValidForm(cut);

        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.Contains("cron invalid", cut.Find("[data-testid=form-error]").TextContent);
        Assert.Empty(cut.FindAll("[data-testid=name-error]"));
    }

    [Fact]
    public async Task Submit_button_is_disabled_while_creating()
    {
        var fake = Register();
        fake.CreateGate = new TaskCompletionSource();
        var cut = Render<NewJob>();
        await Task.Yield();
        FillValidForm(cut);

        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.True(cut.Find("[data-testid=submit-button]").HasAttribute("disabled"));

        fake.CreateGate.SetResult();
        await Task.Yield();
        Assert.NotNull(fake.LastCreateRequest);
    }
}
```

- [ ] **Step 10.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter NewJobPageTests`
Expected: FAIL.

- [ ] **Step 10.3: Implementar CronPreview**

Create `src/JobForge.Ui/Services/CronPreview.cs`:
```csharp
using Cronos;

namespace JobForge.Ui.Services;

public static class CronPreview
{
    public static IReadOnlyList<DateTime> Next(string cron, DateTime fromUtc, int count)
    {
        if (string.IsNullOrWhiteSpace(cron) || count <= 0)
        {
            return [];
        }

        try
        {
            var expression = CronExpression.Parse(cron.Trim());
            var result = new List<DateTime>(count);
            var cursor = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
            for (var i = 0; i < count; i++)
            {
                var occurrence = expression.GetNextOccurrence(cursor, TimeZoneInfo.Utc);
                if (occurrence is null) break;
                var value = DateTime.SpecifyKind(occurrence.Value, DateTimeKind.Utc);
                result.Add(value);
                cursor = value;
            }
            return result;
        }
        catch (CronFormatException)
        {
            return [];
        }
    }
}
```

- [ ] **Step 10.4: Implementar a página**

Replace `src/JobForge.Ui/Components/Pages/NewJob.razor` com:
```razor
@page "/jobs/new"
@using System.Text.Json
@using Cronos
@inject IJobForgeClient Client
@inject NavigationManager Nav
@inject ToastService Toasts
@inject IStringLocalizer<SharedResources> L

<PageTitle>jobforge — @L["new_title"]</PageTitle>

<h1 class="mb-6 text-2xl font-bold">@L["new_title"]</h1>

@if (_formError is not null)
{
    <div class="mb-4 rounded-lg border border-[var(--destructive)] p-3 text-sm" data-testid="form-error">@_formError</div>
}

<Card Class="max-w-2xl">
    <div class="space-y-4">
        <div>
            <label class="mb-1 block text-sm font-medium" for="job-name">@L["new_name"]</label>
            <input id="job-name" data-testid="job-name-input"
                   class="h-10 w-full rounded-lg border border-[var(--border)] bg-[var(--card)] px-3 text-sm"
                   @bind="_name" />
            @if (_nameError is not null)
            {
                <p class="mt-1 text-xs text-[var(--destructive)]" data-testid="name-error">@_nameError</p>
            }
        </div>

        <div>
            <label class="mb-1 block text-sm font-medium" for="job-url">@L["new_url"]</label>
            <input id="job-url" data-testid="job-url-input"
                   class="h-10 w-full rounded-lg border border-[var(--border)] bg-[var(--card)] px-3 text-sm"
                   @bind="_url" />
            @if (_urlError is not null)
            {
                <p class="mt-1 text-xs text-[var(--destructive)]" data-testid="url-error">@_urlError</p>
            }
        </div>

        <div>
            <label class="mb-1 block text-sm font-medium" for="job-payload">@L["new_payload"]</label>
            <textarea id="job-payload" data-testid="job-payload-input" rows="4"
                      class="w-full rounded-lg border border-[var(--border)] bg-[var(--card)] px-3 py-2 font-mono text-sm"
                      @bind="_payload"></textarea>
            @if (_payloadError is not null)
            {
                <p class="mt-1 text-xs text-[var(--destructive)]" data-testid="payload-error">@_payloadError</p>
            }
        </div>

        <div>
            <label class="mb-1 block text-sm font-medium" for="job-cron">@L["new_cron"]</label>
            <input id="job-cron" data-testid="job-cron-input"
                   class="h-10 w-full rounded-lg border border-[var(--border)] bg-[var(--card)] px-3 font-mono text-sm"
                   @bind="_cron" @bind:after="UpdatePreview" />
            <p class="mt-1 text-xs text-[var(--muted-foreground)]">@L["new_cron_hint"]</p>
            <div class="mt-2 flex flex-wrap gap-2">
                <Button Variant="secondary" Size="sm" TestId="preset-cron" OnClick="() => ApplyPreset("*/5 * * * *")">@L["new_preset_5m"]</Button>
                <Button Variant="secondary" Size="sm" TestId="preset-cron" OnClick="() => ApplyPreset("0 * * * *")">@L["new_preset_hourly"]</Button>
                <Button Variant="secondary" Size="sm" TestId="preset-cron" OnClick="() => ApplyPreset("0 9 * * *")">@L["new_preset_daily"]</Button>
                <Button Variant="secondary" Size="sm" TestId="preset-cron" OnClick="() => ApplyPreset("0 9 * * 1")">@L["new_preset_weekly"]</Button>
            </div>
            @if (_cronError is not null)
            {
                <p class="mt-1 text-xs text-[var(--destructive)]" data-testid="cron-error">@_cronError</p>
            }
            @if (_preview.Count > 0)
            {
                <div class="mt-3 rounded-lg bg-[var(--secondary)] p-3">
                    <p class="text-xs font-semibold uppercase text-[var(--muted-foreground)]">@L["new_cron_preview"]</p>
                    <ul class="mt-1 space-y-1 text-sm">
                        @foreach (var occurrence in _preview)
                        {
                            <li data-testid="cron-preview" class="font-mono">@occurrence.ToString("u")</li>
                        }
                    </ul>
                </div>
            }
        </div>
    </div>

    <Footer>
        <a href="/jobs" class="inline-flex h-10 items-center rounded-lg px-4 text-sm">@L["new_cancel"]</a>
        <Button TestId="submit-button" Disabled="_saving" OnClick="SubmitAsync">@L["new_create"]</Button>
    </Footer>
</Card>

@code {
    private string _name = "";
    private string _url = "";
    private string _payload = "";
    private string _cron = "";
    private bool _saving;
    private string? _nameError;
    private string? _urlError;
    private string? _payloadError;
    private string? _cronError;
    private string? _formError;
    private IReadOnlyList<DateTime> _preview = [];

    private void ApplyPreset(string cron)
    {
        _cron = cron;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        _preview = CronPreview.Next(_cron, DateTime.UtcNow, 3);
        _cronError = string.IsNullOrWhiteSpace(_cron) || _preview.Count > 0
            ? null
            : L["new_cron_invalid"];
    }

    private async Task SubmitAsync()
    {
        _nameError = _urlError = _payloadError = _cronError = _formError = null;

        if (string.IsNullOrWhiteSpace(_name))
        {
            _nameError = L["new_name_required"];
        }
        if (!Uri.TryCreate(_url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _urlError = L["new_url_invalid"];
        }
        if (!string.IsNullOrWhiteSpace(_payload))
        {
            try
            {
                JsonDocument.Parse(_payload).Dispose();
            }
            catch (JsonException)
            {
                _payloadError = L["new_payload_invalid"];
            }
        }
        if (!string.IsNullOrWhiteSpace(_cron))
        {
            try
            {
                CronExpression.Parse(_cron);
            }
            catch (CronFormatException)
            {
                _cronError = L["new_cron_invalid"];
            }
        }

        if (_nameError is not null || _urlError is not null
            || _payloadError is not null || _cronError is not null)
        {
            return;
        }

        _saving = true;
        try
        {
            await Client.CreateJobAsync(
                new CreateJobRequest(
                    _name.Trim(),
                    _url.Trim(),
                    string.IsNullOrWhiteSpace(_payload) ? null : _payload,
                    null,
                    string.IsNullOrWhiteSpace(_cron) ? null : _cron.Trim(),
                    null,
                    null),
                Guid.NewGuid().ToString("N"));
            Toasts.ShowSuccess(L["new_created_toast"]);
            Nav.NavigateTo("/jobs");
        }
        catch (JobForgeApiException ex)
        {
            _formError = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }
}
```


- [ ] **Step 10.5: Correr e ver passar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter NewJobPageTests`
Expected: PASS.

- [ ] **Step 10.6: Commit**

```powershell
git add src/JobForge.Ui/Services/CronPreview.cs src/JobForge.Ui/Components/Pages/NewJob.razor tests/JobForge.Ui.Tests/NewJobPageTests.cs
git commit -m "feat(ui): new job form with cron presets and preview"
```

---

### Task 11: Executions list and detail

**Files:**
- Create: `src/JobForge.Ui/Services/RelativeTime.cs`
- Modify: `src/JobForge.Ui/Components/Pages/Executions.razor`
- Modify: `src/JobForge.Ui/Components/Pages/ExecutionDetail.razor`
- Test: `tests/JobForge.Ui.Tests/ExecutionsPageTests.cs`
- Test: `tests/JobForge.Ui.Tests/ExecutionDetailPageTests.cs`
- Test: `tests/JobForge.Ui.Tests/RelativeTimeTests.cs`

**Interfaces:**
- Consumes: `IJobForgeClient`, `UiOptions`, `TimeProvider`.
- Produces: `static (string Key, int? Value) RelativeTime.Format(DateTime timestampUtc, DateTime nowUtc)`.

- [ ] **Step 11.1: Escrever os testes**

Create `tests/JobForge.Ui.Tests/RelativeTimeTests.cs`:
```csharp
using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class RelativeTimeTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, "time_now", null)]
    [InlineData(10, "time_seconds_ago", 10)]
    [InlineData(120, "time_minutes_ago", 2)]
    [InlineData(7200, "time_hours_ago", 2)]
    [InlineData(172800, "time_days_ago", 2)]
    public void Formats_by_threshold(int secondsAgo, string key, int? value)
    {
        var result = RelativeTime.Format(Now.AddSeconds(-secondsAgo), Now);
        Assert.Equal(key, result.Key);
        Assert.Equal(value, result.Value);
    }
}
```

Modify `tests/JobForge.Ui.Tests/ExecutionsPageTests.cs`: trocar
`: BunitContext` por `: UiTestContext` e, no m�todo `Register`, acrescentar
`Services.AddSingleton(TimeProvider.System);` (a p�gina passa a injetar `TimeProvider`).
Acrescentar o teste:
```csharp
[Fact]
public async Task Rows_expose_relative_time_and_link_to_detail()
{
    var fake = new FakeJobForgeClient
    {
        Executions = new ExecutionPage(1, 20, 1,
        [
            new ExecutionDto(Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "succeeded", 1, 200, null,
                DateTime.UtcNow.AddMinutes(-2), null, null, "trace")
        ])
    };
    Services.AddSingleton<IJobForgeClient>(fake);

    var cut = Render<Executions>();
    await Task.Yield();

    cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=execution-row]")));
    Assert.NotEmpty(cut.FindAll("[data-testid=execution-age]"));
    var link = cut.Find("[data-testid=execution-link]");
    Assert.Contains("/executions/33333333", link.GetAttribute("href"));
}
```

Modify `tests/JobForge.Ui.Tests/ExecutionDetailPageTests.cs`: trocar
`: BunitContext` por `: UiTestContext` (o `StatusBadge` agora usa localiza��o).

- [ ] **Step 11.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter "RelativeTimeTests|ExecutionsPageTests"`
Expected: FAIL.

- [ ] **Step 11.3: Implementar RelativeTime**

Create `src/JobForge.Ui/Services/RelativeTime.cs`:
```csharp
namespace JobForge.Ui.Services;

public static class RelativeTime
{
    public static (string Key, int? Value) Format(DateTime timestampUtc, DateTime nowUtc)
    {
        var value = nowUtc - (timestampUtc.Kind == DateTimeKind.Utc ? timestampUtc : timestampUtc.ToUniversalTime());
        if (value.TotalSeconds < 5) return ("time_now", null);
        if (value.TotalMinutes < 1) return ("time_seconds_ago", (int)value.TotalSeconds);
        if (value.TotalHours < 1) return ("time_minutes_ago", (int)value.TotalMinutes);
        if (value.TotalDays < 1) return ("time_hours_ago", (int)value.TotalHours);
        return ("time_days_ago", (int)value.TotalDays);
    }
}
```

- [ ] **Step 11.4: Restilizar a lista**

Modify `src/JobForge.Ui/Components/Pages/Executions.razor` — substituir o markup por:
```razor
@page "/executions"
@implements IAsyncDisposable
@inject IJobForgeClient Client
@inject NavigationManager Nav
@inject IOptions<UiOptions> UiOpt
@inject TimeProvider Clock
@inject IStringLocalizer<SharedResources> L

<PageTitle>jobforge — @L["exec_title"]</PageTitle>

<div class="mb-6 flex flex-wrap items-end justify-between gap-3">
    <h1 class="text-2xl font-bold">@L["exec_title"]</h1>
    <div class="flex items-end gap-2">
        <div>
            <label class="mb-1 block text-xs text-[var(--muted-foreground)]" for="status-filter">@L["exec_filter"]</label>
            <select id="status-filter" data-testid="status-filter"
                    class="h-10 rounded-lg border border-[var(--border)] bg-[var(--card)] px-3 text-sm"
                    value="@(Status ?? "")" @onchange="OnStatusChanged">
                <option value="">@L["common_all"]</option>
                <option value="pending">@L["status_pending"]</option>
                <option value="running">@L["status_running"]</option>
                <option value="succeeded">@L["status_succeeded"]</option>
                <option value="failed">@L["status_failed"]</option>
                <option value="dead">@L["status_dead"]</option>
            </select>
        </div>
        <Button Variant="secondary" TestId="executions-refresh" OnClick="LoadAsync">@L["common_refresh"]</Button>
    </div>
</div>

@if (_error is not null)
{
    <div class="mb-4 rounded-lg border border-[var(--destructive)] p-3 text-sm" data-testid="error-alert">@_error</div>
}

@if (_loading && _pageResult is null)
{
    <p class="text-sm text-[var(--muted-foreground)]">@L["common_loading"]</p>
}
else if (_pageResult is not null && _pageResult.Items.Count == 0)
{
    <Card><p class="text-sm text-[var(--muted-foreground)]" data-testid="executions-empty">@L["exec_empty"]</p></Card>
}
else if (_pageResult is not null)
{
    <p class="mb-3 text-sm text-[var(--muted-foreground)]" data-testid="page-info">
        @L["exec_page_info", _page, TotalPages, _pageResult.Total]
    </p>
    <Card>
        <div class="overflow-x-auto">
            <table class="w-full text-sm">
                <thead>
                    <tr class="text-left text-xs uppercase text-[var(--muted-foreground)]">
                        <th class="pb-2">@L["exec_scheduled"]</th>
                        <th class="pb-2">@L["exec_job"]</th>
                        <th class="pb-2">@L["exec_status"]</th>
                        <th class="pb-2">@L["exec_attempt"]</th>
                        <th class="pb-2">@L["exec_http"]</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var execution in _pageResult.Items)
                    {
                        var age = RelativeTime.Format(execution.ScheduledAt, Clock.GetUtcNow().UtcDateTime);
                        <tr class="border-t border-[var(--border)]" data-testid="execution-row">
                            <td class="py-2">
                                <a class="hover:underline" data-testid="execution-link" href="@($"/executions/{execution.Id}")">
                                    @execution.ScheduledAt.ToLocalTime().ToString("g")
                                    <span class="ml-2 text-xs text-[var(--muted-foreground)]" data-testid="execution-age">
                                        @(age.Value is null ? L[age.Key] : L[age.Key, age.Value.Value])
                                    </span>
                                </a>
                            </td>
                            <td class="py-2 font-mono text-xs">@execution.JobId.ToString()[..8]</td>
                            <td class="py-2"><StatusBadge Status="@execution.Status" /></td>
                            <td class="py-2">@execution.Attempt</td>
                            <td class="py-2">@(execution.HttpStatusCode?.ToString() ?? "—")</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </Card>
    <div class="mt-4 flex gap-2">
        <Button Variant="secondary" Size="sm" TestId="pagination-prev" Disabled="@(_page <= 1)" OnClick="PrevAsync">@L["exec_prev"]</Button>
        <Button Variant="secondary" Size="sm" TestId="pagination-next" Disabled="@(!HasMore)" OnClick="NextAsync">@L["exec_next"]</Button>
    </div>
}
```
Manter integralmente o bloco `@code` existente (constante `PageSize = 20`, `Status`, `TotalPages`, `HasMore`, `RefreshLoopAsync`, `LoadAsync`, `OnStatusChanged`, `NextAsync`, `PrevAsync`, `DisposeAsync`).

- [ ] **Step 11.5: Restilizar o detalhe**

Modify `src/JobForge.Ui/Components/Pages/ExecutionDetail.razor` — manter `@code` e trocar o markup:
```razor
@page "/executions/{Id:guid}"
@using System.Globalization
@inject IJobForgeClient Client
@inject IStringLocalizer<SharedResources> L

<PageTitle>jobforge — @L["exec_detail_title"]</PageTitle>

<h1 class="mb-6 text-2xl font-bold">@L["exec_detail_title"]</h1>

@if (_loading && _execution is null)
{
    <p class="text-sm text-[var(--muted-foreground)]">@L["common_loading"]</p>
}
else if (_error is not null && _execution is null)
{
    <div class="rounded-lg border border-[var(--destructive)] p-3 text-sm" data-testid="error-alert">@_error</div>
}
else if (_execution is null)
{
    <Card><p class="text-sm" data-testid="execution-not-found">@L["exec_not_found"]</p></Card>
}
else
{
    <Card Class="max-w-3xl">
        <div class="mb-4 flex flex-wrap items-center gap-3">
            <span data-testid="detail-status"><StatusBadge Status="@_execution.Status" /></span>
            <span class="text-sm text-[var(--muted-foreground)]">@L["exec_attempt"] <strong data-testid="attempt">@_execution.Attempt</strong></span>
            @if (_execution.HttpStatusCode is not null)
            {
                <span class="text-sm text-[var(--muted-foreground)]">HTTP <strong data-testid="http-status">@_execution.HttpStatusCode</strong></span>
            }
            @if (Duration is not null)
            {
                <span class="text-sm text-[var(--muted-foreground)]">@L["exec_duration"] <strong data-testid="duration">@Duration</strong></span>
            }
            <Button Variant="secondary" Size="sm" TestId="refresh-button" OnClick="LoadAsync">@L["common_refresh"]</Button>
        </div>

        <div class="divide-y divide-[var(--border)] rounded-lg border border-[var(--border)]">
            <div class="flex justify-between px-4 py-2 text-sm" data-testid="timeline-item">
                <span class="text-[var(--muted-foreground)]">@L["exec_scheduled_at"]</span>
                <span>@_execution.ScheduledAt.ToLocalTime().ToString("g")</span>
            </div>
            @if (_execution.StartedAt is not null)
            {
                <div class="flex justify-between px-4 py-2 text-sm" data-testid="timeline-item">
                    <span class="text-[var(--muted-foreground)]">@L["exec_started_at"]</span>
                    <span>@_execution.StartedAt.Value.ToLocalTime().ToString("g")</span>
                </div>
            }
            @if (_execution.FinishedAt is not null)
            {
                <div class="flex justify-between px-4 py-2 text-sm" data-testid="timeline-item">
                    <span class="text-[var(--muted-foreground)]">@L["exec_finished_at"]</span>
                    <span>@_execution.FinishedAt.Value.ToLocalTime().ToString("g")</span>
                </div>
            }
        </div>

        @if (_execution.Error is not null)
        {
            <div class="mt-4 rounded-lg border border-[var(--destructive)] p-3 text-sm" data-testid="error-text">@_execution.Error</div>
        }

        <p class="mt-4 text-xs text-[var(--muted-foreground)]">
            @L["exec_job_id"] <span class="font-mono">@_execution.JobId</span>
            · @L["exec_trace"] <span class="font-mono">@_execution.TraceId</span>
        </p>
    </Card>
}
```

- [ ] **Step 11.6: Correr e ver passar**

Run:
```powershell
dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter "RelativeTimeTests|ExecutionsPageTests|ExecutionDetailPageTests"
```
Expected: PASS.

- [ ] **Step 11.7: Commit**

```powershell
git add src/JobForge.Ui/Services/RelativeTime.cs src/JobForge.Ui/Components/Pages/Executions.razor src/JobForge.Ui/Components/Pages/ExecutionDetail.razor tests/JobForge.Ui.Tests/RelativeTimeTests.cs tests/JobForge.Ui.Tests/ExecutionsPageTests.cs
git commit -m "feat(ui): executions list and detail with dashboard styling"
```

---

### Task 12: Operations page

**Files:**
- Create: `src/JobForge.Ui/Components/Pages/Operations.razor`
- Test: `tests/JobForge.Ui.Tests/OperationsPageTests.cs`

**Interfaces:**
- Consumes: `HealthCheckService`, `IOptions<JobForgeOptions>`, `IOptions<UiOptions>`.
- Produces: rota `/operations`; `data-testid="ops-status"`, `ops-base-url`, `ops-metrics-link`.

- [ ] **Step 12.1: Escrever os testes**

Create `tests/JobForge.Ui.Tests/OperationsPageTests.cs`:
```csharp
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JobForge.Ui.Tests;

public sealed class OperationsPageTests : UiTestContext
{
    private void Register(HealthStatus status, string secret = "super-secret")
    {
        Services.AddHealthChecks()
            .AddCheck("orchestrator", () => new HealthCheckResult(status, "checked"));
        Services.AddSingleton(Options.Create(new JobForgeOptions
        {
            BaseUrl = "http://localhost:8094/",
            ClientId = "jobforge-console",
            ClientSecret = secret,
            Scope = "jobs:read jobs:write"
        }));
        Services.AddSingleton(Options.Create(new UiOptions { RefreshSeconds = 5 }));
    }

    [Fact]
    public void Healthy_orchestrator_is_shown_with_config()
    {
        Register(HealthStatus.Healthy);
        var cut = Render<Operations>();

        cut.WaitForAssertion(() => Assert.Contains("Healthy", cut.Find("[data-testid=ops-status]").TextContent));
        Assert.Contains("http://localhost:8094", cut.Find("[data-testid=ops-base-url]").TextContent);
        Assert.Contains("http://localhost:8094/metrics", cut.Find("[data-testid=ops-metrics-link]").GetAttribute("href"));
    }

    [Fact]
    public void Unhealthy_orchestrator_is_reported()
    {
        Register(HealthStatus.Unhealthy);
        var cut = Render<Operations>();

        cut.WaitForAssertion(() => Assert.Contains("Unhealthy", cut.Find("[data-testid=ops-status]").TextContent));
    }

    [Fact]
    public void Never_renders_the_client_secret()
    {
        Register(HealthStatus.Healthy);
        var cut = Render<Operations>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=ops-base-url]")));
        Assert.DoesNotContain("super-secret", cut.Markup);
    }
}
```

- [ ] **Step 12.2: Correr e ver falhar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter OperationsPageTests`
Expected: FAIL.

- [ ] **Step 12.3: Implementar**

Create `src/JobForge.Ui/Components/Pages/Operations.razor`:
```razor
@page "/operations"
@using Microsoft.Extensions.Diagnostics.HealthChecks
@inject HealthCheckService Health
@inject IOptions<JobForgeOptions> Options
@inject IOptions<UiOptions> UiOpt
@inject IStringLocalizer<SharedResources> L

<PageTitle>jobforge — @L["ops_title"]</PageTitle>

<h1 class="mb-6 text-2xl font-bold">@L["ops_title"]</h1>

<div class="grid gap-4 lg:grid-cols-2">
    <Card>
        <Header><h2 class="font-semibold">@L["ops_health"]</h2></Header>
        @if (_report is null)
        {
            <p class="text-sm text-[var(--muted-foreground)]">@L["ops_checking"]</p>
        }
        else
        {
            <p class="text-lg font-semibold" data-testid="ops-status">@StatusLabel(_report.Status)</p>
            <ul class="mt-3 space-y-2 text-sm">
                @foreach (var entry in _report.Entries)
                {
                    <li class="flex items-center justify-between gap-3" data-testid="ops-check">
                        <span class="font-medium">@entry.Key</span>
                        <Badge Variant="@Variant(entry.Value.Status)">@StatusLabel(entry.Value.Status)</Badge>
                    </li>
                }
            </ul>
            <Button Variant="secondary" Size="sm" TestId="ops-refresh" OnClick="CheckAsync">@L["common_refresh"]</Button>
        }
    </Card>

    <Card>
        <Header><h2 class="font-semibold">@L["ops_title"]</h2></Header>
        <dl class="space-y-3 text-sm">
            <div class="flex justify-between gap-4">
                <dt class="text-[var(--muted-foreground)]">@L["ops_base_url"]</dt>
                <dd class="font-mono" data-testid="ops-base-url">@TrimmedBaseUrl</dd>
            </div>
            <div class="flex justify-between gap-4">
                <dt class="text-[var(--muted-foreground)]">@L["ops_client_id"]</dt>
                <dd class="font-mono">@Options.Value.ClientId</dd>
            </div>
            <div class="flex justify-between gap-4">
                <dt class="text-[var(--muted-foreground)]">@L["ops_scopes"]</dt>
                <dd class="font-mono">@Options.Value.Scope</dd>
            </div>
            <div class="flex justify-between gap-4">
                <dt class="text-[var(--muted-foreground)]">@L["ops_refresh"]</dt>
                <dd class="font-mono">@L["ops_seconds", UiOpt.Value.RefreshSeconds]</dd>
            </div>
        </dl>
        <a class="mt-4 inline-block text-sm text-[var(--primary)] hover:underline"
           data-testid="ops-metrics-link" target="_blank" rel="noopener noreferrer"
           href="@($"{TrimmedBaseUrl}/metrics")">@L["ops_metrics"]</a>
    </Card>
</div>

@code {
    private HealthReport? _report;

    private string TrimmedBaseUrl => Options.Value.BaseUrl.TrimEnd('/');

    private static string Variant(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "success",
        HealthStatus.Degraded => "warning",
        _ => "error"
    };

    private string StatusLabel(HealthStatus status)
        => L[$"ops_{status.ToString().ToLowerInvariant()}"];

    protected override Task OnInitializedAsync() => CheckAsync();

    private async Task CheckAsync() => _report = await Health.CheckHealthAsync();
}
```

- [ ] **Step 12.4: Correr e ver passar**

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false --filter OperationsPageTests`
Expected: PASS.

- [ ] **Step 12.5: Commit**

```powershell
git add src/JobForge.Ui/Components/Pages/Operations.razor tests/JobForge.Ui.Tests/OperationsPageTests.cs
git commit -m "feat(ui): operations page with health and connection details"
```

---

### Task 13: CI, docs and release

**Files:**
- Modify: `.github/workflows/ci.yml`
- Create: `docs/adr/ADR-0004-dashboard-shell-copy.md`
- Modify: `README.md`
- Modify: `src/JobForge.Ui/JobForge.Ui.csproj`
- Modify: `tests/JobForge.Ui.Tests/*.cs` (trocar `: BunitContext` por `: UiTestContext`)

**Interfaces:**
- Consumes: tudo.
- Produces: CI com `css-check`; documentação e versão `1.1.0`.

- [ ] **Step 13.0: Restilizar as páginas de erro**

Replace `src/JobForge.Ui/Components/Pages/Error.razor`:
```razor
@page "/Error"
@using System.Diagnostics
@inject IStringLocalizer<SharedResources> L

<PageTitle>jobforge — @L["error_title"]</PageTitle>

<Card Class="max-w-xl">
    <h1 class="text-xl font-bold text-[var(--destructive)]">@L["error_title"]</h1>
    <p class="mt-2 text-sm text-[var(--muted-foreground)]">@L["error_body"]</p>
    @if (ShowRequestId)
    {
        <p class="mt-3 text-xs">@L["error_request_id"]: <code class="font-mono">@RequestId</code></p>
    }
    <a href="/" class="mt-4 inline-block text-sm text-[var(--primary)] hover:underline">@L["not_found_back"]</a>
</Card>

@code {
    [CascadingParameter] private HttpContext? HttpContext { get; set; }
    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized()
        => RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
```

Replace `src/JobForge.Ui/Components/Pages/NotFound.razor`:
```razor
@page "/not-found"
@layout MainLayout
@inject IStringLocalizer<SharedResources> L

<PageTitle>jobforge — @L["not_found_title"]</PageTitle>

<Card Class="max-w-xl">
    <h1 class="text-xl font-bold">@L["not_found_title"]</h1>
    <p class="mt-2 text-sm text-[var(--muted-foreground)]">@L["not_found_body"]</p>
    <a href="/" class="mt-4 inline-block text-sm text-[var(--primary)] hover:underline">@L["not_found_back"]</a>
</Card>
```

Adicionar aos dois `.resx` (valores EN no neutro, PT no `pt-BR`):

| Chave | EN | PT-BR |
|---|---|---|
| `error_title` | Error | Erro |
| `error_body` | An error occurred while processing your request. | Ocorreu um erro ao processar o pedido. |
| `error_request_id` | Request ID | ID do pedido |
| `not_found_title` | Not found | Não encontrado |
| `not_found_body` | The page you are looking for does not exist. | A página que procuras não existe. |
| `not_found_back` | Back to overview | Voltar à visão geral |

- [ ] **Step 13.1: Migrar todos os testes para `UiTestContext`**

Em `tests/JobForge.Ui.Tests/`, substituir `: BunitContext` por `: UiTestContext` em
todos os ficheiros **exceto** `UiTestContext.cs` (que herda de `BunitContext` de propósito):
```powershell
Get-ChildItem tests/JobForge.Ui.Tests/*.cs |
  Where-Object { $_.Name -ne 'UiTestContext.cs' } |
  ForEach-Object {
    $text = Get-Content $_.FullName -Raw
    if ($text -match ': BunitContext') {
      Set-Content -Path $_.FullName -Value ($text -replace ': BunitContext', ': UiTestContext') -Encoding utf8
    }
  }
```
Remover `using Bunit;` redundante onde o compilador avisar (warnings são erros).

Run: `dotnet test tests/JobForge.Ui.Tests -p:CollectCoverage=false`
Expected: todos os testes PASS.

- [ ] **Step 13.2: Adicionar ao CI**

Modify `.github/workflows/ci.yml` — acrescentar job:
```yaml
  css-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: "22"
          cache: npm
          cache-dependency-path: src/JobForge.Ui/package-lock.json
      - name: Install
        working-directory: src/JobForge.Ui
        run: npm ci
      - name: Build CSS
        working-directory: src/JobForge.Ui
        run: npm run build:css
      - name: Ensure committed CSS is up to date
        run: git diff --exit-code -- src/JobForge.Ui/wwwroot/css/tailwind.css
```

- [ ] **Step 13.3: Escrever o ADR**

Create `docs/adr/ADR-0004-dashboard-shell-copy.md`:
```markdown
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
```

- [ ] **Step 13.4: Atualizar o README**

Modify `README.md`:
1. Na linha de descrição, trocar "Blazor Server console" por "Blazor console with the DashboardTheKingThePower shell".
2. Substituir a tabela `## Routes` por:
```markdown
| Route | Content |
|---|---|
| `/` | overview: KPIs, hourly executions chart, status donut, latest executions |
| `/jobs` | job definitions, name search, last execution badge, **Run now**, **Disable** (confirmed) |
| `/jobs/new` | schedule form with cron presets and next-run preview (Cronos, UTC) |
| `/executions` | paginated table, status filter (`?status=` syncs to the URL), 5s auto-refresh |
| `/executions/{id}` | lifecycle timeline, attempt, HTTP status, duration, error |
| `/executions?status=dead` | dead-letters view (same page — no duplicate screen) |
| `/operations` | orchestrator health, API base URL, OIDC client, scopes, Prometheus link |
```
3. Adicionar após `## Configuration`:
```markdown
Language: PT-BR (default) and EN, switched in the header and persisted in a cookie.
Styling: Tailwind CSS 4 compiled locally (`npm run build:css` in `src/JobForge.Ui`);
the generated stylesheet is committed, so the container image needs no Node.
Charts: Blazor-ApexCharts (basic area/donut features — no premium licence required).
```
4. Adicionar após a nota de idioma/tema:
```markdown
Reusing this console for another application: deploy another container with
`JobForge__BaseUrl`, `JobForge__ClientId` and `JobForge__ClientSecret` pointing at that
orchestrator (the OIDC client needs `jobs:read` and `jobs:write`). No code change needed.
```
5. Em `## Decisions`, adicionar:
```markdown
- [ADR-0004](docs/adr/ADR-0004-dashboard-shell-copy.md) — shell copied from the dashboard, no shared library
```
6. Em `## Running tests`, acrescentar:
```powershell
cd src/JobForge.Ui; npm ci; npm run build:css   # only when changing styles
```
7. Em `## Known deviation`, manter o texto atual.

- [ ] **Step 13.5: Versão**

Modify `src/JobForge.Ui/JobForge.Ui.csproj` — adicionar dentro do primeiro `<PropertyGroup>`:
```xml
<Version>1.1.0</Version>
```

- [ ] **Step 13.6: Verificação final**

Run:
```powershell
cd D:\Projetos\jobforge-ui
dotnet build JobForge.Ui.slnx
dotnet test tests/JobForge.Ui.Tests
cd src/JobForge.Ui; npm run build:css; cd ../..
git status --short
```
Expected: build 0 avisos; testes PASS com cobertura ≥80%; `git status` sem alterações em `tailwind.css` (CSS atualizado).
Se a cobertura ficar abaixo de 80%, adicionar testes de página/painel para os componentes
novos — não baixar o gate. A única exclusão permitida são os dois wrappers ApexCharts,
já anotados com `[ExcludeFromCodeCoverage]`.

Verificação manual (Docker):
```powershell
Copy-Item .env.example .env   # definir OIDC_CLIENT_SECRET
docker compose up --build -d
curl http://localhost:8096/health/live
```
Abrir `http://localhost:8096`:
- trocar PT/EN e confirmar que a escolha persiste após reload;
- alternar tema e accent, confirmar persistência;
- criar job com cron `*/5 * * * *` e ver as 3 próximas execuções;
- correr agora e ver a execução em `/executions`;
- confirmar os dois gráficos na overview;
- abrir `/operations` e confirmar saúde + link `/metrics`.

- [ ] **Step 13.7: Commit e tag**

```powershell
git add .github/workflows/ci.yml docs/adr/ADR-0004-dashboard-shell-copy.md README.md src/JobForge.Ui/JobForge.Ui.csproj tests/JobForge.Ui.Tests
git commit -m "chore: ci css check, docs and v1.1.0"
git tag -a v1.1.0 -m "v1.1.0 — dashboard shell console"
```
Não fazer push nem push da tag sem confirmação do utilizador.

---

## Self-Review

**Cobertura do spec:**
- Shell visual + tokens → Tasks 1, 3, 4, 5.
- Rotas (overview, jobs, new, executions, detail, dead letters, operations) → Tasks 8–12.
- i18n PT/EN + seletor + cookie → Task 2 e Header (Task 5).
- Gráficos → Task 8 (ApexCharts, verificação manual declarada).
- Overview a partir dos endpoints existentes e janela de 100 → Task 7.
- Operação sem segredo → Task 12 (teste explícito).
- Testes, cobertura, CI css-check → Tasks 1–13.
- ADR-0004, README, versão 1.1.0 → Task 13.
- Dashboard intocado → nenhuma task escreve fora de `jobforge-ui`.

**Riscos residuais:**
- ApexCharts exige verificação manual no browser (aceite pelo utilizador); os testes automatizados cobrem a agregação e o painel de KPIs.
- A ordem das Tasks 5 e 6 está invertida de propósito (a palette tem de existir para o `MainLayout` compilar).
- `data-testid` antigos que deixarem de existir devem ser substituídos por equivalentes na mesma task.

