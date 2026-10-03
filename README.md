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

Everything was handcoded until autumn 2026, at which point I basically switched to 
having AI work on the code.  Despite my very strong skepticism only a year before,
as of October 2026 I have to concede, as a software developer with 17 years of experience,
that AI has really become extremely good and that this is just how it's going to be from now on.
Certainly it's doing a very good job so far at this modest (~20 KLoC) project.

(c) 2015-2026 Alexander Ulyanov