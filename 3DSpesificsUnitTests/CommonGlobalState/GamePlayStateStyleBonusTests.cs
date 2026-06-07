using CommonUtilities.CommonSetup;
using Domain;

namespace _3DSpesificsUnitTests.CommonGlobalState;

[TestClass]
public class GamePlayStateStyleBonusTests
{
    private int _originalCap;
    private int _originalCleanLoopScore;

    [TestInitialize]
    public void Setup()
    {
        _originalCap = GameSetup.PlanetStyleBonusScoreCap;
        _originalCleanLoopScore = GameSetup.CleanLoopStyleBonusScore;
    }

    [TestCleanup]
    public void Cleanup()
    {
        GameSetup.PlanetStyleBonusScoreCap = _originalCap;
        GameSetup.CleanLoopStyleBonusScore = _originalCleanLoopScore;
    }

    [TestMethod]
    public void AwardStyleBonus_AddsToScoreAndCapsPerPlanet()
    {
        GameSetup.PlanetStyleBonusScoreCap = 500;
        var gameplay = new GamePlayState { SceneIndex = 1 };

        int first = gameplay.AwardStyleBonus(300);
        int second = gameplay.AwardStyleBonus(300);

        Assert.AreEqual(300, first);
        Assert.AreEqual(200, second);
        Assert.AreEqual(500L, gameplay.Score);
        Assert.AreEqual(500, gameplay.PlanetStyleBonusScore);
        Assert.AreEqual(0, gameplay.PlanetStyleBonusRemaining);
    }

    [TestMethod]
    public void AwardStyleBonus_WhenSceneChanges_StartsFreshPlanetBudget()
    {
        GameSetup.PlanetStyleBonusScoreCap = 500;
        var gameplay = new GamePlayState { SceneIndex = 1 };

        gameplay.AwardStyleBonus(500);
        gameplay.SceneIndex = 2;
        int awarded = gameplay.AwardStyleBonus(100);

        Assert.AreEqual(100, awarded);
        Assert.AreEqual(600L, gameplay.Score);
        Assert.AreEqual(100, gameplay.PlanetStyleBonusScore);
        Assert.AreEqual(2, gameplay.PlanetStyleBonusSceneIndex);
    }

    [TestMethod]
    public void RestoreCheckpoint_RestoresCheckpointStyleBonusBudget()
    {
        var gameplay = new GamePlayState { SceneIndex = 3 };

        gameplay.AwardStyleBonus(400);
        gameplay.SaveCheckpoint();
        gameplay.AwardStyleBonus(250);

        gameplay.RestoreCheckpoint();

        Assert.AreEqual(400L, gameplay.Score);
        Assert.AreEqual(400, gameplay.PlanetStyleBonusScore);
        Assert.AreEqual(3, gameplay.PlanetStyleBonusSceneIndex);
    }
}
