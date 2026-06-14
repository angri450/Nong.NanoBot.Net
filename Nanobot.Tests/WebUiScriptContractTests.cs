using System.Text.RegularExpressions;

namespace Nanobot.Tests;

public class WebUiScriptContractTests
{
    [Fact]
    public void AppJs_ElementsObject_DefinesEveryReferencedElementsProperty()
    {
        var appJs = File.ReadAllText(GetRepoFile("Nanobot.Web", "wwwroot", "app.js"));
        var elementsBlock = Regex.Match(appJs, @"const elements = \{([\s\S]*?)\n\};");

        Assert.True(elementsBlock.Success, "Could not locate the elements object in app.js.");

        var definedProperties = Regex.Matches(elementsBlock.Groups[1].Value, @"([A-Za-z0-9_]+)\s*:")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var referencedProperties = Regex.Matches(appJs, @"elements\.([A-Za-z0-9_]+)")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var missing = referencedProperties
            .Where(name => !definedProperties.Contains(name))
            .OrderBy(name => name)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Missing elements.* definitions in app.js: {string.Join(", ", missing)}");
    }

    [Fact]
    public void IndexHtml_ContainsEveryGetElementByIdTarget()
    {
        var appJs = File.ReadAllText(GetRepoFile("Nanobot.Web", "wwwroot", "app.js"));
        var indexHtml = File.ReadAllText(GetRepoFile("Nanobot.Web", "wwwroot", "index.html"));

        var requestedIds = GetRequestedElementIds(appJs);

        var presentIds = Regex.Matches(indexHtml, "id=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var missing = requestedIds
            .Where(id => !presentIds.Contains(id))
            .OrderBy(id => id)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"index.html is missing ids requested by app.js: {string.Join(", ", missing)}");
    }

    [Fact]
    public void IndexHtml_DefinesEveryGetElementByIdTargetBeforeAppScriptLoads()
    {
        var appJs = File.ReadAllText(GetRepoFile("Nanobot.Web", "wwwroot", "app.js"));
        var indexHtml = File.ReadAllText(GetRepoFile("Nanobot.Web", "wwwroot", "index.html"));

        var scriptIndex = indexHtml.IndexOf("<script src=\"/app.js\"></script>", StringComparison.Ordinal);

        Assert.True(scriptIndex >= 0, "index.html must load /app.js explicitly.");

        var lateIds = GetRequestedElementIds(appJs)
            .Where(id =>
            {
                var idIndex = indexHtml.IndexOf($"id=\"{id}\"", StringComparison.Ordinal);
                return idIndex < 0 || idIndex > scriptIndex;
            })
            .OrderBy(id => id)
            .ToList();

        Assert.True(
            lateIds.Count == 0,
            $"index.html defines ids after app.js loads: {string.Join(", ", lateIds)}");
    }

    [Fact]
    public void IndexHtml_DoesNotDefineDuplicateIds()
    {
        var indexHtml = File.ReadAllText(GetRepoFile("Nanobot.Web", "wwwroot", "index.html"));

        var duplicates = Regex.Matches(indexHtml, "id=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} ({group.Count()})")
            .OrderBy(id => id)
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            $"index.html contains duplicate ids: {string.Join(", ", duplicates)}");
    }

    private static HashSet<string> GetRequestedElementIds(string appJs)
    {
        return Regex.Matches(appJs, "document\\.getElementById\\(\"([^\"]+)\"\\)")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string GetRepoFile(params string[] parts)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            Path.Combine(parts)));
    }
}
