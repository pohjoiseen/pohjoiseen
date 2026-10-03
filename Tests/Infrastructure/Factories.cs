namespace Tests.Infrastructure;

public class Fennica3Factory : AppFactory<global::Fennica3.Fennica3>;

/// <summary>
/// Fennica3 as run inside KoTi for previews: drafts are visible.
/// </summary>
public class Fennica3WithDraftsFactory : Fennica3Factory
{
    protected override IDictionary<string, string?> GetSettings()
    {
        var settings = base.GetSettings();
        settings["Fennica3:WithDrafts"] = "true";
        return settings;
    }
}

public class KoTiFactory : AppFactory<KoTi.ModelFactories.PostViewModelFactory>
{
    /// <summary>
    /// Where the publish command copies the database to.
    /// </summary>
    public string LiveDatabasePath { get; } = TestDatabase.CreateCopy();

    protected override IDictionary<string, string?> GetSettings()
    {
        var settings = base.GetSettings();
        settings["KoTi:LiveDatabase"] = LiveDatabasePath;
        settings["KoTi:PublishCommand"] = $"cp '{DatabasePath}' '{LiveDatabasePath}' && echo published";
        return settings;
    }
}
