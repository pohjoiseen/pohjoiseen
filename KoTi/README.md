## KoTi

**Not updated for v1.0 yet**

This is KoTi _(kohteiden tietokanta, Finn. points database)_, a privately deployed hobby project by
me, Alexander Ulyanov.  It is meant for me to track my own travels and explorations mainly of Finland
and to record information about various places of interest in the same place.  Ideally I would also upload
all my pictures there too, but that's not implemented yet.  The database is not publicly accessible and
won't be in the foreseeable future, but there's nothing secret about this source and I have
published it under GPL3.

This is a web application:

* Backend: C#, ASP.NET 6.  Currently extremely trivial CRUD backend
* Database: SQLite.  This won't be a multi-user project, I don't need anything special from the database,
  and SQLite is nice for backups and moving around
* Frontend: SPA application, TypeScript 4.8, React 18 with Tanstack Query for data layer, react-router for routing,
  Leaflet/react-leaflet for embedded map functionality, and plain old Bootstrap/Reactstrap as an UI library.

Screenshots of initial version as of 12.8.2023:

![](https://imgur.com/JB6Fwrw.jpg)
![](https://imgur.com/UCQpy4o.jpg)
![](https://imgur.com/kniN34O.jpg)

There are few comments and no docs or tests.  As I said, it is just a personal project.

Alexander
