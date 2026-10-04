# Tests

xUnit v3 tests for all three projects, run with:

    dotnet test --project Tests

(`global.json` at the repo root switches `dotnet test` to Microsoft.Testing.Platform, required for xUnit v3 on
the .NET 10 SDK.)  Filter with e.g. `dotnet test --project Tests -- --filter-class Tests.Fennica3Tests.PageTests`
or `--filter-namespace Tests.Browser`.

## How it works

* Each test class gets its own copy of a SQLite database created by applying all migrations to an empty
  database and seeding it with a small fixed data set (`Infrastructure/TestData.cs`, IDs are constants there).
  Copies live in the system temp directory and are deleted at exit.  The real `pohjoiseen.db` is never touched.
* Fennica3 and KoTi run in-process via `WebApplicationFactory` (`Infrastructure/AppFactory.cs`), with
  the test database and a fake in-memory S3 bucket (`FakePictureStore`), which also serves the pictures over its
  public URL (`https://pictures.test/`) both to the apps (downloads for resizing) and to the browser.
  Environment is `Testing`, so `*.appsettings.Development.json` is not used.
* HTML is parsed with AngleSharp and checked with CSS selectors.  htmx requests in KoTi are made with the
  `HX-Request` header (`CreateHtmxClient()`), so partials are rendered as for htmx.

## Groups

* `Fennica3Tests/ContentFormatterTests.cs` — Markdown in, expected HTML out: links, pictures, galleries,
  asides, typography.
* `Fennica3Tests/PageTests.cs` — all public pages, RSS, post JSON, redirects, 404s, drafts.
* `KoTiTests/` — content create/edit/delete, lists, redirects, search (FTS triggers), publish, picture upload
  (formats, EXIF) and web sizes, API.  `LJCrosspostTests.cs`: LiveJournal crosspost HTML, Markdown in through
  the real ContentFormatter, expected LJ HTML out, plus the dialog endpoints.
* `DatabaseTests.cs` — no model changes without migration, search triggers exist after all migrations.
* `UnitTests/` — small pure functions.
* `Browser/` — Playwright in headless Chromium: Fennica3 maps/popups, galleries, keyboard navigation; KoTi
  editor save round trip (Monaco + web components), creating posts, uploading pictures, LJ crosspost dialog.  These use the system
  Chromium (`/usr/bin/chromium`, or set `CHROMIUM_PATH`), or one installed by Playwright
  (`pwsh Tests/bin/Debug/net10.0/playwright.ps1 install chromium`); skipped if there is none.  KoTi browser tests
  need the KoTi frontend built (`cd KoTi/Frontend && bun run build`).  All external requests (fonts, CDNs,
  map tiles, analytics) are blocked, JS errors fail the tests.
* `RealData/RealDatabaseTests.cs` — renders all posts, articles and books from a real database (read-only) and
  reports exceptions, lost text, missing pictures and unresolved `picture:` links; also converts all posts to
  LJ crosspost HTML (checks leftover markup, lost text, relative URLs, and lists the largest posts in the test
  output, see with `--show-live-output on`).  Skipped unless
  `POHJOISEEN_TEST_DB` is set:

      POHJOISEEN_TEST_DB=$PWD/pohjoiseen.db dotnet test --project Tests -- --filter-class Tests.RealData.RealDatabaseTests
