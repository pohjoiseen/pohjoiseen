This is **Holvi** _(Finnish "Vault")_, the backend library for KoTi and Fennica3 web applications.  It contains
little more than Entity Framework models/migrations/database context and code for accessing S3 for picture storage,
and also for resizing pictures.  It should be mostly self-explanatory.

Database updates during development should be run for this project; you
would just need to add path to the database file to `dotnet ef database update`
command.

Full-text search (`Search` FTS5 table) is maintained by SQLite triggers, created in migrations.  Beware that EF Core
rebuilds a SQLite table for some schema changes (foreign keys, column changes), and this silently drops all triggers
on that table; such migrations need to recreate them (see `SearchRebuild` migration).
