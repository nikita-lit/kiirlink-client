using System.Xml.Linq;

namespace KiirLink.Tests;

public sealed class LocalizationResourceTests
{
    [Fact]
    public void AllCulturesContainTheSameNonEmptyKeys()
    {
        var localizationDirectory = Path.Combine(AppContext.BaseDirectory, "Localization");
        var resourceFiles = Directory.GetFiles(localizationDirectory, "AppResources*.resx");

        Assert.Equal(3, resourceFiles.Length);

        var resources = resourceFiles.ToDictionary(
            path => Path.GetFileName(path)!,
            ReadResources);
        var defaultKeys = resources["AppResources.resx"].Keys.Order().ToArray();

        foreach (var (fileName, values) in resources)
        {
            Assert.Equal(defaultKeys, values.Keys.Order().ToArray());
            Assert.All(values, item =>
                Assert.False(string.IsNullOrWhiteSpace(item.Value), $"{fileName}: {item.Key} is empty"));
        }
    }

    private static Dictionary<string, string> ReadResources(string path) =>
        XDocument.Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(
                element => element.Attribute("name")!.Value,
                element => element.Element("value")?.Value ?? string.Empty);
}
