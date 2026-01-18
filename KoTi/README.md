## KoTi

This is KoTi _(originally "kohteiden tietokanta", Finn. points database)_, the backoffice part for
https://fennica.pohjoiseen.fi/ blog.  It contains interfaces to:

1. A large database of photos uploaded to S3 storage
2. A database of points of interest subdivided by country/database/area
3. Fennica3 blog editing, allowing inserting of photos from point 1

PoI database was the original function, hence the app name, and I originally planned it for my personal use only,
but in practice I'm just way too lazy to maintain such a database just for myself.  This part is the least used and
maintained at the moment.  However, I plan to use it in Fennica3 in the future as well.

The application is not meant to be publicly accessible, but there's nothing secret about this source and
I have published it under GPL3.  There's nothing super secret about the content I used it for.  The app doesn't
handle authentication (for the moment), rather, I deployed it in such a way it should be only accessible for me.

The technology stack is:

* Backend: C#, ASP.NET 10.  Currently extremely trivial CRUD backend
* Database: SQLite.  This won't be a multi-user project, I don't need anything special from the database,
  and SQLite is nice for backups and moving around.  There are two copies of database in fact, "live" for Fennica3
  and "draft" for KoTi
* Frontend (older): SPA application, TypeScript 4.8, React 18 with Tanstack Query for data layer, react-router for 
  routing, Leaflet/react-leaflet for embedded map functionality, and plain old Bootstrap/Reactstrap as an UI library.
* Frontend (newer, so far only for blog editor): mostly server-rendered application, using Htmx and vanilla JS,
  with Web Components, and also hand-written CSS.  Bun is required for building.  This has been a bit of an experiment
  for me, I haven't coded anything in a classic server-rendered way in a while.  It is messy in places, but still
  I kind of like it more than the SPA part and I plan to rewrite the rest of the frontend in the same way.

Database and S3 code is contained in Holvi project (and used also by Fennica3).  Fennica3 is also compiled entirely
as part of this project as well, to handle previews.

There are few comments and no docs or tests.  As I said, it is just a personal project.

Alexander
