# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

Three .NET 10 projects in a single solution (`Pohjoiseen.sln`), plus their TypeScript client apps:

- **Holvi/** — shared library: EF Core SQLite `HolviDbContext`, EF migrations, `PictureStorage` (S3/DigitalOcean Spaces), `PictureUpload`, and the domain models (`Article`, `Book`, `Post`, `Picture`, `PictureSet`, `Tag`, `Redirect`). All DB migrations live here; both apps consume its `AddHolviServices`.
- **Fennica3/** — public blog at fennica.pohjoiseen.fi. ASP.NET MVC + Razor. The `ContentFormatter` class is the critical piece: it renders Markdown/HTML and resolves internal `post:XXX` / `picture:XXX` links and picture markup (figure/figcaption, srcset). Client assets under `Fennica3/Client/` (TypeScript, Bun, Leaflet + Glider for the map and galleries — no JS framework).
- **KoTi/** — private backoffice (blog editor). ASP.NET MVC + Razor + Htmx + Web Components. Imports Fennica3 as a project reference **and registers it as an MVC ApplicationPart** (see `KoTi/Program.cs`) so KoTi can serve Fennica3 previews directly. Has **two concurrent frontends**:
  - `KoTi/Frontend/` (Bun, TypeScript, Htmx, vanilla Web Components, Leaflet, Monaco) — the new server-rendered UI. Built to `KoTi/wwwroot/frontend/` (git-ignored). Entry point: `koti.ts` registers all custom elements.
  - `KoTi/ClientApp/` — legacy React/CRA app still used for pictures UI and redirects; being removed. See `KoTi/TODO.txt`.

## Fennica3 ↔ KoTi relationship

KoTi fully embeds Fennica3 for previews: in `KoTi/Program.cs`, `typeof(Fennica3.Fennica3).Assembly` is added as an `AssemblyPart`, and Fennica3's static files are served via a `CompositeFileProvider` pointing at `../Fennica3/wwwroot`. The `FennicaExtensions.AddFennicaServices` localization provider detects when the active controller is *not* from the Fennica3 assembly and forces `en-US` — this is intentional so KoTi's own pages don't get Fennica3's Russian/English/Finnish locale routing.

## Database

Single SQLite file `pohjoiseen.db` at repo root (git-ignored). In production there are two copies: a "live" copy for Fennica3 and a "draft" copy for KoTi. Configured via `Holvi:DatabaseFile` in each app's appsettings.

All EF migrations live in `Holvi/Migrations/`. Apply migrations either via `dotnet ef database update` from the `Holvi/` directory (pointing at the correct DB file), or — in production, where the .NET SDK isn't installed — by running the KoTi executable with the `migrate` argument (handled in `KoTi/Program.cs`).

## Build & run

The projects target **.NET 10** and use `LangVersion=default`.

- Build everything: `dotnet build Pohjoiseen.sln`
- Run the public blog: `dotnet run --project Fennica3`
- Run the backoffice: `dotnet run --project KoTi`
- Apply migrations in production: `KoTi migrate` (built executable; see `Program.cs`)

Client builds use **Bun** (required). Each client has its own:

- `cd Fennica3/Client && bun install && bun run build` (or `bun run watch` for dev). `bun run typecheck` runs `tsc --noEmit`. Build output: `Fennica3/wwwroot/{js,css}/`.
- `cd KoTi/Frontend && bun install && bun run build` (or `bun run watch`). Output: `KoTi/wwwroot/frontend/`.
- `cd KoTi/ClientApp && npm install && npm start` — legacy React dev server (CRA); `npm run build` for production.

Both Bun-based client builds are wired into MSBuild via `PublishRunBun` targets in the `.csproj` files, so `dotnet publish` runs `bun install && bun run build` automatically. For Fennica3 the target runs `BeforeTargets="GenerateBuildCompressedStaticWebAssets"` (so compressed assets include the freshly built JS/CSS — do not move it back to `AfterTargets`).

## Appsettings convention

Each app loads `{ProjectName}.appsettings.json` **and** `{ProjectName}.appsettings.{Environment}.json` explicitly (see `Program.cs`). `*appsettings.Development.json` is git-ignored. After build, MSBuild targets in `KoTi.csproj` delete any `appsettings*.json` copied into the output dir and re-copy the project's files — don't rely on the default ASP.NET appsettings-copy behavior.

Secrets (S3 access key etc.) are expected via .NET user secrets or the environment-specific appsettings file, not committed.

## Content model quirks

- Posts store `CoatsOfArms` and `Geo` (with nested `Links`) as JSON-mapped owned entities (see `HolviDbContext.OnModelCreating`).
- Fennica3 supports multiple languages (`ru`, `en`, `fi`) via the `language` route parameter; Russian is the primary/default and the only one with meaningful content today.
- KoTi's `AbstractContentController<TEntity, TViewModel, TFormViewComponent>` is the base for all content edit flows (Posts, Articles, Books) with stock `Edit`/`Save`/`CopyToLanguage` actions.
- Fennica3 is deployed behind an nginx cache that holds pages until the DB is redeployed, so the app doesn't implement HTTP caching of its own.

## Conventions

- No test suite, no linter config beyond the CRA defaults in the legacy React app. Type-check the Fennica3 client with `bun run typecheck` before shipping client changes.
- Commit messages in this repo are freeform and typically prefixed with the project they touch (`Fennica3:`, `KoTi:`). Multiple unrelated changes in one commit is the norm here — see recent git log.
- Code is sparsely commented; the README files in each project directory are the primary prose documentation.
