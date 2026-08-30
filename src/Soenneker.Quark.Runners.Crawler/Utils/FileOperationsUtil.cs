using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using Soenneker.Git.Util.Abstract;
using Soenneker.Node.Util.Abstract;
using Soenneker.Playwrights.Crawler.Abstract;
using Soenneker.Playwrights.Crawler.Dtos;
using Soenneker.Playwrights.Crawler.Enums;
using Soenneker.Quark.Runners.Crawler.Utils.Abstract;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.Environment;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.Path.Abstract;
using Soenneker.Utils.Process.Abstract;

namespace Soenneker.Quark.Runners.Crawler.Utils;

public sealed class FileOperationsUtil : IFileOperationsUtil
{
    private readonly ILogger<FileOperationsUtil> _logger;
    private readonly IGitUtil _gitUtil;
    private readonly INodeUtil _nodeUtil;
    private readonly IPlaywrightCrawler _playwrightCrawler;
    private readonly IPathUtil _pathUtil;
    private readonly IDirectoryUtil _directoryUtil;
    private readonly IFileUtil _fileUtil;
    private readonly IProcessUtil _processUtil;

    public FileOperationsUtil(ILogger<FileOperationsUtil> logger, IGitUtil gitUtil, INodeUtil nodeUtil, IPlaywrightCrawler playwrightCrawler,
        IPathUtil pathUtil, IDirectoryUtil directoryUtil, IFileUtil fileUtil, IProcessUtil processUtil)
    {
        _logger = logger;
        _gitUtil = gitUtil;
        _nodeUtil = nodeUtil;
        _playwrightCrawler = playwrightCrawler;
        _pathUtil = pathUtil;
        _directoryUtil = directoryUtil;
        _fileUtil = fileUtil;
        _processUtil = processUtil;
    }

    public async ValueTask Process(CancellationToken cancellationToken)
    {
        string tempRoot = await _pathUtil.GetUniqueTempDirectory("soenneker-quark-runners-crawler", true, cancellationToken);

        try
        {
            string crawledRepositoryDirectory = Path.Combine(tempRoot, "soenneker-quark-crawled");
            string componentsRepositoryDirectory = Path.Combine(tempRoot, "soenneker-quark-crawled-components");
            string crawlDirectory = Path.Combine(tempRoot, "crawl");
            string extractedDirectory = Path.Combine(tempRoot, "extracted");

            await CloneRepository(Constants.CrawledRepository, crawledRepositoryDirectory, cancellationToken);
            await CloneRepository(Constants.ComponentsRepository, componentsRepositoryDirectory, cancellationToken);

            await Crawl(crawlDirectory, cancellationToken);
            string crawlContentDirectory = await GetCrawlContentDirectory(crawlDirectory, cancellationToken);

            await ReplaceRepositoryContents(crawledRepositoryDirectory, crawlContentDirectory, cancellationToken);
            await CommitAndPush(crawledRepositoryDirectory, cancellationToken);

            await ExtractPreviewFamilies(crawlContentDirectory, extractedDirectory, cancellationToken);
            await ReplaceRepositoryContents(componentsRepositoryDirectory, extractedDirectory, cancellationToken);
            await CommitAndPush(componentsRepositoryDirectory, cancellationToken);
        }
        finally
        {
            try
            {
                await _directoryUtil.DeleteIfExists(tempRoot, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not remove temporary crawler directory {TempRoot}", tempRoot);
            }
        }
    }

    private async ValueTask CloneRepository(string repositoryUrl, string targetDirectory, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cloning {RepositoryUrl} into {TargetDirectory}", repositoryUrl, targetDirectory);
        await _gitUtil.Clone(repositoryUrl, targetDirectory, cancellationToken: cancellationToken);
    }

    private async ValueTask Crawl(string crawlDirectory, CancellationToken cancellationToken)
    {
        PlaywrightCrawlResult result = await _playwrightCrawler.Crawl(new PlaywrightCrawlOptions
        {
            Url = Constants.ComponentsUrl,
            SaveDirectory = crawlDirectory,
            Mode = PlaywrightCrawlMode.Full,
            MaxDepth = 10,
            MaxPages = 500,
            SameHostOnly = true,
            PrettyPrintHtml = true,
            ClearSaveDirectory = true,
            OverwriteExistingFiles = true,
            ContinueOnPageError = true,
            Headless = true,
            UseStealth = true,
            NavigationTimeoutMs = 60_000,
            PostNavigationDelayMs = 2_000
        }, cancellationToken);

        _logger.LogInformation("Crawl complete. PagesVisited: {PagesVisited}, HtmlFilesSaved: {HtmlFilesSaved}", result.PagesVisited, result.HtmlFilesSaved);

        if (result.PagesVisited == 0 || result.HtmlFilesSaved == 0)
            throw new InvalidOperationException("The crawl produced no HTML output; existing generated repositories will not be replaced.");
    }

    private async ValueTask<string> GetCrawlContentDirectory(string crawlDirectory, CancellationToken cancellationToken)
    {
        string basePathDirectory = Path.Combine(crawlDirectory, Constants.CrawlBasePath);

        if (await _directoryUtil.Exists(basePathDirectory, cancellationToken))
            return basePathDirectory;

        return crawlDirectory;
    }

    private async ValueTask ExtractPreviewFamilies(string crawlDirectory, string extractedDirectory, CancellationToken cancellationToken)
    {
        await _directoryUtil.DeleteIfExists(extractedDirectory, cancellationToken);
        await _directoryUtil.Create(extractedDirectory, cancellationToken: cancellationToken);

        var parser = new HtmlParser();
        var formatter = new PrettyMarkupFormatter();

        List<string> htmlFiles = await _directoryUtil.GetFilesByExtension(crawlDirectory, ".html", true, cancellationToken);

        foreach (string htmlFile in htmlFiles.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string html = await _fileUtil.Read(htmlFile, cancellationToken: cancellationToken);
            IDocument document = await parser.ParseDocumentAsync(html, cancellationToken);
            IHtmlCollection<IElement> previews = document.QuerySelectorAll("div[data-slot=\"preview\"]");

            if (previews.Length == 0)
                continue;

            string familyName = GetFamilyName(crawlDirectory, htmlFile);
            string outputPath = Path.Combine(extractedDirectory, familyName + ".html");

            var builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE html>");
            builder.AppendLine("<html>");
            builder.AppendLine("  <body>");

            foreach (IElement preview in previews)
            {
                await using var writer = new StringWriter();
                preview.ToHtml(writer, formatter);
                builder.AppendLine(IndentBlock(writer.ToString().Trim(), "    "));
                builder.AppendLine();
            }

            builder.AppendLine("  </body>");
            builder.AppendLine("</html>");

            await _fileUtil.Write(outputPath, builder.ToString().TrimEnd() + Environment.NewLine, cancellationToken: cancellationToken);
            _logger.LogInformation("Saved {Count} previews to {Path}", previews.Length, outputPath);
        }
    }

    private async ValueTask ReplaceRepositoryContents(string repositoryDirectory, string sourceDirectory, CancellationToken cancellationToken)
    {
        string repositoryRoot = Path.GetFullPath(repositoryDirectory);
        string sourceRoot = Path.GetFullPath(sourceDirectory);
        string gitDirectory = Path.Combine(repositoryRoot, ".git");

        if (!await _directoryUtil.Exists(gitDirectory, cancellationToken))
            throw new InvalidOperationException($"Refusing to replace a directory that is not a Git checkout: {repositoryRoot}");

        if (!await _directoryUtil.Exists(sourceRoot, cancellationToken))
            throw new InvalidOperationException($"Generated source directory does not exist: {sourceRoot}");

        List<string> sourcePaths = await _directoryUtil.GetFilesByExtension(sourceRoot, "", true, cancellationToken);

        if (sourcePaths.Count == 0)
            throw new InvalidOperationException($"Generated source directory is empty; existing repository content will not be deleted: {sourceRoot}");

        List<string> directories = await _directoryUtil.GetAllDirectories(repositoryDirectory, cancellationToken);

        foreach (string directory in directories)
        {
            string fullDirectory = Path.GetFullPath(directory);

            if (!string.Equals(Path.GetDirectoryName(fullDirectory), repositoryRoot, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(fullDirectory).Equals(".git", StringComparison.OrdinalIgnoreCase))
                continue;

            await _directoryUtil.Delete(fullDirectory, cancellationToken);
        }

        List<string> files = await _directoryUtil.GetFilesByExtension(repositoryDirectory, "", false, cancellationToken);

        foreach (string file in files)
        {
            await _fileUtil.Delete(file, cancellationToken: cancellationToken);
        }

        foreach (string sourcePath in sourcePaths)
        {
            string relativePath = Path.GetRelativePath(sourceRoot, sourcePath);
            string destination = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
            string repositoryPrefix = repositoryRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!destination.StartsWith(repositoryPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Generated path resolves outside the repository checkout: {relativePath}");

            string? destinationDirectory = Path.GetDirectoryName(destination);

            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                await _directoryUtil.Create(destinationDirectory, cancellationToken: cancellationToken);

            await _fileUtil.DeleteIfExists(destination, cancellationToken: cancellationToken);
            await _fileUtil.Copy(sourcePath, destination, cancellationToken: cancellationToken);
        }
    }

    private async ValueTask CommitAndPush(string repositoryDir, CancellationToken cancellationToken)
    {
        string token = EnvironmentUtil.GetVariableStrict("GH__TOKEN");
        string name = EnvironmentUtil.GetVariableStrict("GIT__NAME");
        string email = EnvironmentUtil.GetVariableStrict("GIT__EMAIL");

        await _gitUtil.CommitAndPush(repositoryDir, Constants.CommitMessage, token, name, email, cancellationToken);
    }

    private static string GetFamilyName(string crawlDirectory, string htmlFile)
    {
        string relativePath = Path.GetRelativePath(crawlDirectory, htmlFile);
        string withoutExtension = Path.ChangeExtension(relativePath, null) ?? relativePath;
        string normalized = withoutExtension.Replace(Path.DirectorySeparatorChar, '-').Replace(Path.AltDirectorySeparatorChar, '-');

        if (normalized.EndsWith("-index", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^"-index".Length];

        return normalized.Equals("index", StringComparison.OrdinalIgnoreCase) ? "components" : normalized;
    }

    private static string IndentBlock(string content, string indent)
    {
        string[] lines = content.Replace("\r\n", "\n").Split('\n');
        return string.Join(Environment.NewLine, lines.Select(line => indent + line));
    }
}
