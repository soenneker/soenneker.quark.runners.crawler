namespace Soenneker.Quark.Runners.Crawler;

/// <summary>
/// Represents the constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The suite repository.
    /// </summary>
    public const string SuiteRepository = "https://github.com/soenneker/soenneker.quark.suite.git";
    /// <summary>
    /// The crawled repository.
    /// </summary>
    public const string CrawledRepository = "https://github.com/soenneker/soenneker.quark.crawled.git";
    /// <summary>
    /// The components repository.
    /// </summary>
    public const string ComponentsRepository = "https://github.com/soenneker/soenneker.quark.crawled.components.git";
    /// <summary>
    /// The components url.
    /// </summary>
    public const string ComponentsUrl = "https://quark.soenneker.com/";
    /// <summary>
    /// The crawl base path. The custom domain serves the suite from its root.
    /// </summary>
    public const string CrawlBasePath = "";
    /// <summary>
    /// The commit message.
    /// </summary>
    public const string CommitMessage = "Refresh Quark component preview HTML";
}
