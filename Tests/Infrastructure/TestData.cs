using Holvi;
using Holvi.Models;

namespace Tests.Infrastructure;

/// <summary>
/// Small, fixed data set every test database starts with.  IDs are fixed so tests can refer to them.
/// </summary>
public static class TestData
{
    public const string Url = FakePictureStore.PublicUrl;

    // pictures
    public const int LandscapeId = 1;   // 3000x2000, website sizes exist
    public const int SmallPngId = 2;    // 800x600, too small for website sizes
    public const int PortraitId = 3;    // 2000x3000, website sizes exist
    public const int Only1xId = 4;      // 1000x700, only 1x website size
    public const int CoatId = 5;        // coat of arms
    public const int NoWebSizesId = 6;  // 2400x1600, website sizes not created yet, original is in fake storage
    public const int Landscape2Id = 7;  // 1500x1000, same aspect ratio as Landscape, website sizes exist

    // books
    public const int LaplandBookId = 1;
    public const int DraftBookId = 2;
    public const int BigBookId = 3;  // more than a page of posts
    public const int BigBookPostCount = 26;

    // posts
    public const int HelsinkiId = 1;
    public const int TurkuId = 2;
    public const int DraftPostId = 3;
    public const int InariId = 4;        // in Lapland book, order 1
    public const int KilpisjarviId = 5;  // in Lapland book, order 2
    public const int HelsinkiEnId = 6;   // English version of Helsinki
    public const int FillerPostCount = 30;

    // articles
    public const int AboutId = 1;
    public const int BlogIntroId = 2;
    public const int DraftArticleId = 3;

    public const string HelsinkiPath = "/ru/2020/05/01/helsinki/";
    public const string TurkuPath = "/ru/2020/06/15/turku/";
    public const string DraftPostPath = "/ru/2020/07/01/draft-post/";
    public const string InariPath = "/ru/lapland/inari/";
    public const string KilpisjarviPath = "/ru/lapland/kilpisjarvi/";
    public const string HelsinkiEnPath = "/en/2020/05/01/helsinki/";

    public const string HelsinkiContent = """
        ## История города

        Хельсинки основан в 1550 году -- это [Турку](post:2) был столицей раньше.

        ![Вид на город](picture:1)

        <!--gallery-->
        ![](picture:1)
        ![](picture:7)
        <!--/gallery-->

        <!--aside-->
        См. также [о проекте](article:1).
        <!--/aside-->
        """;

    public static Picture Picture(int id, string filename, int width, int height, bool websiteSizes, bool only1x = false)
    {
        var hash = $"hash{id}";
        var name = Path.GetFileNameWithoutExtension(filename);
        var ext = Path.GetExtension(filename);
        return new Picture
        {
            Id = id,
            Filename = filename,
            Hash = hash,
            Url = $"{Url}{hash}/{filename}",
            ThumbnailUrl = $"{Url}{hash}/{name}.t.jpg",
            DetailsUrl = $"{Url}{hash}/{name}.d.jpg",
            Width = width,
            Height = height,
            Size = width * height / 10,
            UploadedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PhotographedAt = new DateTime(2019, 8, 1, 12, 0, 0, DateTimeKind.Utc),
            WebsiteSizesExist = websiteSizes,
            Website1xUrl = websiteSizes ? $"{Url}{hash}/{name}.1x{ext}" : null,
            Website2xUrl = websiteSizes && !only1x ? $"{Url}{hash}/{name}.2x{ext}" : null,
        };
    }

    private static Picture[] Pictures() =>
    [
        Picture(LandscapeId, "landscape.jpg", 3000, 2000, true),
        Picture(SmallPngId, "small.png", 800, 600, false),
        Picture(PortraitId, "portrait.jpg", 2000, 3000, true),
        Picture(Only1xId, "only1x.jpg", 1000, 700, true, only1x: true),
        Picture(CoatId, "coat.png", 300, 340, false),
        Picture(NoWebSizesId, "big.jpg", 2400, 1600, false),
        Picture(Landscape2Id, "landscape2.jpg", 1500, 1000, true)
    ];

    public static void Seed(HolviDbContext db)
    {
        db.Pictures.AddRange(Pictures());

        db.Books.AddRange(
            new Book
            {
                Id = LaplandBookId, Language = "ru", Name = "lapland", Title = "Лапландия",
                ContentMD = "Книга о **Лапландии**.", TitlePictureId = LandscapeId
            },
            new Book { Id = DraftBookId, Language = "ru", Name = "secret-book", Title = "Секретная книга", Draft = true },
            new Book { Id = BigBookId, Language = "ru", Name = "archipelago", Title = "Архипелаг" });

        db.Posts.AddRange(
            new Post
            {
                Id = HelsinkiId, Language = "ru", Name = "helsinki", Date = new DateOnly(2020, 5, 1),
                Title = "Хельсинки", Description = "Столица **Финляндии** и её крупнейший город.",
                ContentMD = HelsinkiContent,
                TitlePictureId = LandscapeId,
                DateDescription = "Фото 2019 года", LocationDescription = "Южная Финляндия",
                Address = "Pohjoisesplanadi 11", PublicTransport = "Трамвай 2",
                CoatsOfArms = [new Post.CoatOfArms { Url = $"picture:{CoatId}" }],
                Geo =
                [
                    new Post.GeoPoint
                    {
                        Title = "Сенатская площадь", Description = "Главная площадь, см. **собор**",
                        Lat = 60.1695, Lng = 24.9525, Zoom = 3, Icon = "church", Maps = ["index"],
                        TitleImage = $"picture:{PortraitId}",
                        Links = [new Post.Link { Label = "Турку", Path = $"post:{TurkuId}" }]
                    },
                    new Post.GeoPoint { Lat = 60.2, Lng = 24.9, Maps = ["osm"] }
                ]
            },
            new Post
            {
                Id = TurkuId, Language = "ru", Name = "turku", Date = new DateOnly(2020, 6, 15),
                Title = "Турку", Description = "Бывшая столица.",
                ContentMD = """
                    Турку -- старейший город. Назад в [Хельсинки](post:1#история-города).

                    <!--gallery-->
                    ![Собор](picture:1)
                    ![Замок](picture:7)
                    ![Река](picture:3)
                    <!--/gallery-->
                    """,
                Geo = [new Post.GeoPoint { Lat = 60.45, Lng = 22.27, Maps = ["index"] }]
            },
            new Post
            {
                Id = DraftPostId, Language = "ru", Name = "draft-post", Date = new DateOnly(2020, 7, 1),
                Title = "Черновик", Draft = true, ContentMD = "Ещё не готово.",
                Geo = [new Post.GeoPoint { Lat = 61.5, Lng = 23.8, Maps = ["index"] }]
            },
            new Post
            {
                Id = InariId, Language = "ru", Name = "inari", Date = new DateOnly(2021, 1, 10),
                Title = "Инари", BookId = LaplandBookId, Order = 1, ContentMD = "Озеро Инари."
            },
            new Post
            {
                Id = KilpisjarviId, Language = "ru", Name = "kilpisjarvi", Date = new DateOnly(2021, 1, 5),
                Title = "Килписъярви", BookId = LaplandBookId, Order = 2, ContentMD = "Гора Саана."
            },
            new Post
            {
                Id = HelsinkiEnId, Language = "en", Name = "helsinki", Date = new DateOnly(2020, 5, 1),
                Title = "Helsinki", Description = "Capital of Finland.",
                ContentMD = "See also [Turku](post:2)."
            });

        for (var i = 1; i <= FillerPostCount; i++)
        {
            db.Posts.Add(new Post
            {
                Id = 100 + i, Language = "ru", Name = $"filler-{i:00}", Date = new DateOnly(2019, 1, i),
                Title = $"Заполнитель {i}", ContentMD = $"Текст {i}."
            });
        }

        for (var i = 1; i <= BigBookPostCount; i++)
        {
            db.Posts.Add(new Post
            {
                Id = 200 + i, Language = "ru", Name = $"island-{i:00}", Date = new DateOnly(2022, 1, i),
                Title = $"Остров {i}", BookId = BigBookId, Order = i
            });
        }

        db.Articles.AddRange(
            new Article { Id = AboutId, Language = "ru", Name = "about", Title = "О проекте", ContentMD = "## Кто я\n\nАвтор." },
            new Article { Id = BlogIntroId, Language = "ru", Name = "_blog-intro", Title = "Интро", ContentMD = "Добро пожаловать в **блог**." },
            new Article { Id = DraftArticleId, Language = "ru", Name = "draft-article", Title = "Черновик статьи", Draft = true, ContentMD = "..." });

        db.Redirects.AddRange(
            new Redirect { UrlFrom = "/old/helsinki", UrlTo = $"post:{HelsinkiId}#история-города" },
            new Redirect { UrlFrom = "/old/about/", UrlTo = $"article:{AboutId}" },
            new Redirect { UrlFrom = "/old/external", UrlTo = "https://example.com/x" });

        db.SaveChanges();
    }

    /// <summary>
    /// Pictures in storage: small placeholders for everything, and a real original for one that may need resizing.
    /// </summary>
    public static void SeedStore(FakePictureStore store)
    {
        var placeholder = TestImages.Jpeg(30, 20);
        foreach (var picture in Pictures())
        {
            foreach (var url in new[] { picture.Url, picture.ThumbnailUrl, picture.DetailsUrl, picture.Website1xUrl, picture.Website2xUrl })
            {
                if (url is not null)
                {
                    store.Put(url[Url.Length..], placeholder);
                }
            }
        }
        store.Put($"hash{NoWebSizesId}/big.jpg", TestImages.Jpeg(2400, 1600));
    }
}
