using SporeMaster.Core.Localization;

namespace SporeMaster.Tests;

public static class LocalizationTests
{
    [SporeTest("localizer ships English and Russian and falls back safely")]
    public static void Languages()
    {
        Assert.True(Localizer.AvailableLanguages.Contains("en"), "English must be available");
        Assert.True(Localizer.AvailableLanguages.Contains("ru"), "Russian must be available");

        Localizer english = Localizer.Load("en");
        Assert.Equal("en", english.Language);
        Assert.Equal("SporeMaster 3", english["app.name"]);
        Assert.Contains(english["cmd.list.summary"], "List");

        Localizer russian = Localizer.Load("ru");
        Assert.Equal("ru", russian.Language);
        Assert.Equal("SporeMaster 3", russian["app.name"]);

        // A few strings are actually translated (differ from English).
        Assert.True(english["cmd.list.summary"] != russian["cmd.list.summary"], "list summary translated");
        Assert.True(english["header.file"] != russian["header.file"], "header.file translated");
        Assert.True(english["common.error"] != russian["common.error"], "common.error translated");

        // Every key used by the CLI resolves in Russian too (never falls through to the key itself).
        foreach (string key in new[]
                 {
                     "cmd.info.summary", "cmd.unpack.summary", "cmd.diff.summary", "cmd.pdb.summary",
                     "cmd.anim.summary", "result.unpacked", "header.entries", "common.usage",
                 })
        {
            Assert.True(russian[key] != key, $"missing Russian translation for '{key}'");
        }

        // Unknown languages fall back to English.
        Localizer unknown = Localizer.Load("zz");
        Assert.Equal("en", unknown.Language);
    }
}
