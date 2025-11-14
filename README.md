This is the source code for `pohjoiseen.fi` websites.

* `Fennica3`: user-facing, very picture-heavy blog about Finland (https://fennica.pohjoiseen.fi/)
* `KoTi`: backoffice for Fennica3 and my private photo and location database.  Meant to be private,
  although should not hold anything truly sensitive
* `Holvi`: common backend for the previous two projects, for accessing their sqlite database and
  S3 storage for photos.

These are my main free-time projects (well, the biggest project is the actual blog content,
which these application support).  As such, the source is under GPL.  https://fennica.pohjoiseen.fi/
has been backed by this code since November 2025; before that in 2023-2025 it used a custom
.NET-based static site generator, before that a custom NodeJS-based static site generator,
and before that WordPress.  KoTi originated as a personal point of interest database in 2023,
but I never used it too much for that; added picture storage in 2024 and blog backoffice in 2025.
Holvi then separated from KoTi as a bit of common code between projects.  There are
somewhat more detailed READMEs in project directories.

(c) 2015-2025 Alexander Ulyanov