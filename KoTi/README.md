## KoTi

This is KoTi, the backoffice part for https://fennica.pohjoiseen.fi/ blog.

KoTi was originally meant as an unrelated project, a simple points of interest database UI (and the name was
meant as _kohteiden tietokanta_, Finn. points database).  Soon I added pictures uploading to an S3 bucket
and linking those with PoIs.  The whole thing was meant originally for private personal use, but I never
got around to adding much data here.  I have over 80,000 pictures from my travels mostly around Finland,
and tagging them all would be dreadfully boring and time-consuming.  In the autumn of 2025 I decided to rewrite
my blog from a custom static site generator to a more classic CMS-like solution, and I put blog editing UI
into KoTi too, since it conveniently already had all pictures here.  In late 2025 I started rewriting the original
React-based UI to a mostly server-rendered one, using Htmx and Web Components.  I meant to someday add
the PoI database as a public feature to the blog, but in early 2026 I realized that it's a simply impossible task
for one man to maintain an up-to-date PoI database for entire Finland, even if I someday actually finish the initial
version.  Thus I removed all original PoI stuff, and now this is pretty much just a blog editor.

The application is not meant to be publicly accessible, but there's nothing secret about this source and
I have published it under GPL3.  There's nothing super secret about the content I used it for.  The app doesn't
handle authentication (for the moment), rather, I deployed it in such a way it should be only accessible for me.

The technology stack is:

* Backend: C#, ASP.NET MVC 10, Razor templating
* Database: SQLite.  This won't be a multi-user project, I don't need anything special from the database,
  and SQLite is nice for backups and moving around.  There are two copies of database in fact, "live" for Fennica3
  and "draft" for KoTi
* Frontend: mostly a server-rendered application, using Htmx and vanilla JS,
  with Web Components, and also hand-written CSS.  Bun is required for building.

The new frontend has been a bit of an experiment for me, I haven't coded anything in a classic server-rendered way
in a while.  To be honest it ended up rather messy, blog editing UI is sufficiently complicated that a frontend
framework would still be of good use here, and basic Web Components are just a bit too low-level.  Doing sophisticated
things with Htmx also easily starts to feel kludgy.  The backend also got messy as well, most of the code currently sits
right in the controllers.  A refactor would be nice.  But first I should at least finish removing the old frontend,
which is still used for a few pages.

Database and S3 code is contained in Holvi project (and used also by Fennica3).  Fennica3 is also compiled entirely
as part of this project as well, to handle previews.

There are few comments and no docs or tests.  As I said, it is just a personal project.

Alexander
