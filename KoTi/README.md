## KoTi

This is KoTi _(originally "kohteiden tietokanta", Finn. points database)_, a privately deployed hobby project by
me, Alexander Ulyanov.  Its functions are:

1. Interface to a large database of photos uploaded to S3 storage
2. Database of points of interest subdivided by country/database/area
3. Backoffice for Fennica3 blog editing, using the same database of photos as in point 1. **(IN PROGRESS)**

PoI database was the original function, hence the app name, and I originally planned it for my personal use only,
but in practice I'm just way too lazy to maintain such a database just for myself.  This part is the most complete
but also the least maintained.  However, I plan to use it in Fennica3 in the future as well.

The application is not meant to be publicly accessible future, but there's nothing secret about this source and
I have  published it under GPL3.

This is a web application:

* Backend: C#, ASP.NET 9.  Currently extremely trivial CRUD backend
* Database: SQLite.  This won't be a multi-user project, I don't need anything special from the database,
  and SQLite is nice for backups and moving around
* Frontend: SPA application, TypeScript 4.8, React 18 with Tanstack Query for data layer, react-router for routing,
  Leaflet/react-leaflet for embedded map functionality, and plain old Bootstrap/Reactstrap as an UI library.

Database and S3 code is contained in Holvi project (and used also by Fennica3).

There are few comments and no docs or tests.  As I said, it is just a personal project.

Alexander
