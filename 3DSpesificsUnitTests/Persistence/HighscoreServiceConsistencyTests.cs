using CommonUtilities.Persistence;
using System;
using System.IO;

namespace _3DSpesificsUnitTests.Persistence;

[TestClass]
public class HighscoreServiceConsistencyTests
{
    private string _originalLocalFolder = string.Empty;
    private string _testLocalFolder = string.Empty;
    private int _originalMaxEntries;

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _originalMaxEntries = PersistenceSetup.MaxHighscoreEntries;

        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainHighscoreTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.MaxHighscoreEntries = 100;
        PersistenceSetup.Initialize();
    }

    [TestCleanup]
    public void Cleanup()
    {
        PersistenceSetup.LocalFolder = _originalLocalFolder;
        PersistenceSetup.MaxHighscoreEntries = _originalMaxEntries;

        try
        {
            if (Directory.Exists(_testLocalFolder))
                Directory.Delete(_testLocalFolder, recursive: true);
        }
        catch
        {
        }
    }

    [TestMethod]
    public void TrySubmitScore_UpdatesExistingPlayerInsteadOfAddingDuplicate()
    {
        bool first = HighscoreService.TrySubmitScore("Jarle", 1000, 1);
        bool second = HighscoreService.TrySubmitScore("Jarle", 1500, 2);

        var list = HighscoreService.LoadLocalHighscores();
        var jarleEntries = list.Entries.FindAll(e => string.Equals(e.PlayerName, "Jarle", StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(first);
        Assert.IsTrue(second);
        Assert.AreEqual(1, jarleEntries.Count, "Same player should have exactly one highscore row.");
        Assert.AreEqual(1500L, jarleEntries[0].Score, "Existing row should be updated to higher score.");
    }

    [TestMethod]
    public void TrySubmitScore_TrimsPlayerNameAndStillUpdatesSameRecord()
    {
        bool first = HighscoreService.TrySubmitScore("Jarle", 1200, 1);
        bool second = HighscoreService.TrySubmitScore("  Jarle  ", 1300, 2);

        var list = HighscoreService.LoadLocalHighscores();
        var jarleEntries = list.Entries.FindAll(e => string.Equals(e.PlayerName, "Jarle", StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(first);
        Assert.IsTrue(second);
        Assert.AreEqual(1, jarleEntries.Count,
            "Whitespace variants of the same player name should not create duplicate rows.");
        Assert.AreEqual(1300L, jarleEntries[0].Score);
    }
}
