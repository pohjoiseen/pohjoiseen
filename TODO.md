# TODO

Ideas and known gaps, roughly in order of payoff within each section.

## Performance (Fennica3)

* Post header background (`Post.cshtml`) and `og:image` use the full 3000x2000 original (`TitlePicture.Url`),
  several MB on every post view.  Use `Website2xUrl` for the background, keep the original only for click-through.
* Resized versions are WebP since 3.5, but pictures uploaded/resized earlier still have `.jpg`/`.png` ones.  Could
  convert those in the background (S3 keys and `Picture` URLs change, so the nginx cache needs a DB redeploy anyway).
  AVIF would save more but ImageSharp cannot write it.
* `ContentFormatter` calls `EnsureWebsiteVersionsExist` during public page rendering, i.e. Fennica3 may download
  an original from S3 and resize it inside a request on a 1 GB droplet, and needs S3 write credentials for it.
  Move to KoTi (on save/publish) so Fennica3 is read-only and credential-free (see also TODO in `HolviExtensions`).
* Self-host Font Awesome and Google Fonts: one less third-party dependency, faster, and Google Fonts from Google's
  CDN is a GDPR issue in the EU.  Font Awesome 5.14 is also old; could subset to the icons actually used.

## Fennica3 features

* Public search page — the FTS table is already there.  Would need to bypass nginx cache (or cache per query).
* `sitemap.xml`.
* Full content in RSS; also `LastUpdatedTime = Now` and enclosure length 0 make some readers treat everything
  as changed.
* Dark mode (`prefers-color-scheme`), colours as CSS variables first.
* `[text](picture:123)` links are not resolved (only `post:`, `article:`, `book:` are), post 51 has some.
* JSON-LD `BlogPosting` markup; "related/nearby posts" based on geo points; link from post to its location on map.

## KoTi

* Pictures UI is minimal (browse, fullscreen, upload).  Details view/editing, moving pictures between picture sets
  (folders), deleting, tags were only in the old React UI (removed in 3.4) and should still be re-added.
* Defense in depth beyond network-level privacy: no auth and no antiforgery tokens, so any page open in the browser
  could e.g. POST to `/Publish` or `/Posts/Create` (CSRF).  Enable ASP.NET antiforgery globally (htmx can send the
  token as a header) plus a simple single-user login or reverse-proxy basic auth.
* Pre-publish link checker: `post:`/`picture:`/`article:`/`book:` links to missing or draft items, pictures without
  web sizes; show on Publish page.  (`Tests/RealData/RealDatabaseTests.cs` does some of this already.)
* Search: `ё` and `е` are different for FTS (`remove_diacritics` only handles Latin script).  Could normalize
  both in the triggers and in the query.
* "What changed since last publish" on Publish page (`UpdatedAt` vs live DB mtime; note `UpdatedAt` is UTC).
* Warn on Home/Publish page if expected search triggers are missing from `sqlite_master` (EF table rebuilds drop
  them silently, see `Holvi/Migrations/20261001120000_SearchRebuild.cs`).
* Refactor backend, move code out of controllers.
* Think about how to structure the frontend, perhaps re-add a framework after all?  (not React)
* `KoTi/README.md` still says the old frontend is used for a few pages; it was removed in 3.4.

## Engineering

* Minimal CI: `dotnet build`, `bun run typecheck`, `dotnet test --project Tests` (needs Bun and Chromium).
* `XElement.Parse` in `ContentFormatter` has no error handling, so handwritten non-well-formed HTML breaks a page
  only at runtime.  `Tests/RealData` catches it for an existing database, a check on save in KoTi would be better.
* Continuous backups of the draft DB (Litestream, or nightly `VACUUM INTO` to Spaces) — the only copy of
  unpublished work.
* Both `Program.cs` files add `*.appsettings*.json` *after* the default config sources, so environment variables and
  `--Key=value` command-line args cannot override anything (e.g. `Holvi__DatabaseFile` is silently ignored).
  Re-add `AddEnvironmentVariables()` / `AddCommandLine(args)` after the JSON files.
* `PublishRunBun` targets run on every `dotnet build`, not only publish (because of
  `BeforeTargets="GenerateBuildCompressedStaticWebAssets"`), so building requires Bun and is slower.  Could be
  conditioned on publish / Release, or skipped when `wwwroot/js` is up to date.
