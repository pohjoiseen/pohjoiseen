Fennica3
========

This is **Fennica3**, a simple blog application that I, Alexander Ulyanov, wrote to display a blog that
I've been keeping about Finland (various localities, mostly minor, and nature, and some bits of history as well)
since 2015.  The blog is very picture-heavy, as individual posts can easily have over 100 pictures,
and I always provide my original 3000x2000 photos, along with downscaled web versions of course.  Pictures
can be displayed as carousels (with the simple Glider JS library), and the title page has a map (Leaflet-based)
with various places described in posts; these are the only JS parts, otherwise it's all plain HTML/CSS.
There is only one chronological blog, with no categories/tags/etc., although map points are divided by types/icons.
There is support for some global pages, little used, and some support for multiple language versions, although for
the time being these are unused and the only version is Russian, which is my mother tongue.

`Fennica` comes from the blog name, **Encyclopaedia Fennica**, https://fennica.pohjoiseen.fi/.
Fennica3 is the third major version of the blog.  The first Fennica was a custom static site generator
written in NodeJS and TypeScript in 2020.  It was quite slow and in early 2023 I replaced it with Fennica2,
also a static site generator but in C# and .NET 6.  This one worked a lot better, regenerating the entire website
in 5-7 sec (assuming all resized versions of all pictures were already present) with its >500 Markdown posts and >15000
original pictures.

However, organizing this much content by hand in the filesystem was also getting annoying; Git was also not a good
fit with a 36 GB repository, and overall there was a lot of waste of local disk space; in practice, all writing had
to be done on one and the same machine (my desktop home PC) anyway; custom static site generator (named Teos then) was
not terrible, but, since it was a non-standard solution and I rarely had to touch it, changing things was a bit
of a pain; and finally I wanted to reuse my previous KoTi project, which already has a database of photos, kept in
S3 storage (in practice DigitalOcean with its built-in CDN).  Therefore Fennica3 is a step away from static site
generators and back to more classic web applications.  Fennica3 takes posts from a sqlite database and pictures
from S3, both through Holvi library common with KoTi.  Editing is implemented in KoTi, and Fennica3 doesn't
itself has any user accounts etc.  It is deployed behind a nginx cache, which caches unconditionally all
requested pages until Fennica3's copy of the database is redeployed; so in practice it's not slower or more demanding
than a fully static version (and still deployed to the same smallest DigitalOcean droplet, with 1 GB RAM).

As such, it is a very simple ASP.NET MVC 9 application.  The most involved part is `ContentFormatter` which
converts Markdown/HTML of posts and other content for display, resolving internal links to other posts and pictures,
among other things.  Otherwise it's pretty much just a few controllers and Razor templates.

The client side of the application contained in `Client` changed a lot less since the first Fennica.  It's a simple
handcrafted design with a single CSS file.  As mentioned beforre, Javascript is used only for galleries and the map on 
the title page.  There is no JS framework used or needed.  There is, however, a webpack-based build process, and the
client side is written in TypeScript.

Alexander