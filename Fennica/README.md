Encyclopaedia Fennica v2 - source
=================================

This is the source code that powers the [Encyclopaedia Fennica](https://fennica.pohjoiseen.fi/) website.

**This is only the source, all the actual content (and images) is contained in a submodule.  The content submodule
is self-hosted, as it is way too big for GitHub.  You can however clone it, there is read-only HTTP access to the
repository.  The license for content part is CC BY-ND 4.0, that is, it is allowed to redistribute it, but with
no modification.**

This is the second version of the project.  The [first version](https://github.com/pohjoiseen/fennica.pohjoiseen.fi)
was written in TypeScript and NodeJS.  Written in 2020 and somewhat developed over the years, it always remained
little more than a prototype, quite messy and also slow.  This is a completely redesigned and rewritten version
in C# and .NET 6, in use from February 2023.  It decouples the website itself (Fennica2 project) from the static
site generator/server (Teos project), so that the latter can potentially be reused.  I am planning in particular
to rewrite my pohjoiseen.fi website (currently a static snapshot of a WordPress website) using Teos as well.

This project is released under MIT license in hopes it could be useful for someone, but for the most part I still
did it for my own needs and Teos, while reusable, is still opinionated and also resides in the same repository
for the time being.  It does not have tests or documentation (although the code is decently commented and mostly
straightforward).

The generator builds website content from `Fennica/Content` dir into a snapshot in `Fennica/Build` dir which can
then be served completely statically.  The generator distinguishes between static files and content files.
Static files are usually images; they get updated if stale (images in particular get resized), but otherwise we
do not touch them.  Content files are usually Markdown; they are read, parsed and remain in memory on launch.
After loading, there are two more separate steps: routes generation and formatting (HTML postprocessing).
In dev server mode, the generator watches the content dir and updates state in memory accordingly.  HTML is
rendered using .NET's Razor templating.

See the entry point and the Teos configuration in `Fennica/Program.cs`, that should be a decent starting point.
The central part of Teos library is `Teos/TeosEngine.cs`.

In the future potentially I might have some caching and dependency tracking, but for now my current website
with about 400 posts and 8000 images, is loaded and regenerated in <10 seconds on my machine, and there is little
need for further optimization.  Especially since static site generation is highly parallelizable and we
use `Parallel.ForEachAsync()` rather liberally.

The project also has CSS and JS generated with webpack and TypeScript.  JS is used for image galleries and maps,
but not for the general functioning of the website; Teos is generally completely JS-agnostic.  The frontend part
fully resides in `Fennica/Client`.  Run `npm run dev`/`npm run build` there as necessary.

Alexander
