using System.Net;
using System.Text.Json;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Common.Persistence;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Tests.Persistence;

[TestClass]
public class PlayerCallsignServiceTests
{
    private string _originalLocalFolder = "";
    private string _testLocalFolder = "";
    private string? _originalSupabaseUrl;
    private string? _originalSupabaseAnonKey;
    private string _originalSupabaseTableName = "";
    private string _originalSupabaseCallsignTableName = "";

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _originalSupabaseUrl = PersistenceSetup.SupabaseUrl;
        _originalSupabaseAnonKey = PersistenceSetup.SupabaseAnonKey;
        _originalSupabaseTableName = PersistenceSetup.SupabaseTableName;
        _originalSupabaseCallsignTableName = PersistenceSetup.SupabaseCallsignTableName;

        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainCallsignTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.SupabaseUrl = null;
        PersistenceSetup.SupabaseAnonKey = null;
        PersistenceSetup.SupabaseTableName = "highscores";
        PersistenceSetup.SupabaseCallsignTableName = "player_callsigns";
        PersistenceSetup.Initialize();
        HighscoreService.SaveLocalHighscores(new HighscoreList());
    }

    [TestCleanup]
    public void Cleanup()
    {
        SupabaseCallsignClient.ResetHttpClientForTests();
        PersistenceSetup.LocalFolder = _originalLocalFolder;
        PersistenceSetup.SupabaseUrl = _originalSupabaseUrl;
        PersistenceSetup.SupabaseAnonKey = _originalSupabaseAnonKey;
        PersistenceSetup.SupabaseTableName = _originalSupabaseTableName;
        PersistenceSetup.SupabaseCallsignTableName = _originalSupabaseCallsignTableName;

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
    public void CreateSuggestion_ReturnsUppercaseCallsignThatFitsOverlay()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            string suggestion = PlayerCallsignGenerator.CreateSuggestion(new Random(seed));

            Assert.IsFalse(string.IsNullOrWhiteSpace(suggestion));
            Assert.AreEqual(suggestion.ToUpperInvariant(), suggestion);
            Assert.IsTrue(
                suggestion.Length <= ScreenOverlayState.MaxCallsignLength,
                $"{suggestion} should fit inside the name entry field.");
        }
    }

    [TestMethod]
    public void TryConfirmCallsign_RejectsLocalHighscoreDuplicate()
    {
        HighscoreService.SaveLocalHighscores(new HighscoreList
        {
            Entries = new List<HighscoreEntry>
            {
                CreateEntry("ACE PILOT")
            }
        });

        var result = PlayerCallsignService.TryConfirmCallsign("ace pilot");

        Assert.IsFalse(result.IsAccepted);
        Assert.AreEqual(PlayerCallsignService.LocalCallsignTakenMessage, result.ValidationMessage);
    }

    [TestMethod]
    public void TryConfirmCallsign_AllowsOwnPriorName()
    {
        HighscoreService.SaveLocalHighscores(new HighscoreList
        {
            Entries = new List<HighscoreEntry>
            {
                CreateEntry("ACE PILOT")
            }
        });

        var result = PlayerCallsignService.TryConfirmCallsign("ace pilot", "ACE PILOT");

        Assert.IsTrue(result.IsAccepted);
        Assert.AreEqual("ACE PILOT", result.Callsign);
    }

    [TestMethod]
    public void TryConfirmCallsign_AllowsOwnLocalProgressProfile()
    {
        File.WriteAllText(PersistenceSetup.GetPlayerProgressFilePath("Local Hero"), "not encrypted for this test");

        var result = PlayerCallsignService.TryConfirmCallsign("local hero");

        Assert.IsTrue(result.IsAccepted);
        Assert.AreEqual("LOCAL HERO", result.Callsign);
    }

    [TestMethod]
    public void TryConfirmCallsign_ReservesAvailableSupabaseName()
    {
        ConfigureSupabase();
        var handler = new CallsignRegistryHandler();
        SupabaseCallsignClient.UseHttpClientForTests(new HttpClient(handler));

        var result = PlayerCallsignService.TryConfirmCallsign("lunar braben");

        Assert.IsTrue(result.IsAccepted);
        Assert.AreEqual("LUNAR BRABEN", result.Callsign);
        CollectionAssert.Contains(handler.InsertedCallsigns, "LUNAR BRABEN");
    }

    [TestMethod]
    public void TryConfirmCallsign_RejectsTakenSupabaseName()
    {
        ConfigureSupabase();
        var handler = new CallsignRegistryHandler("LUNAR BRABEN");
        SupabaseCallsignClient.UseHttpClientForTests(new HttpClient(handler));

        var result = PlayerCallsignService.TryConfirmCallsign("lunar braben");

        Assert.IsFalse(result.IsAccepted);
        Assert.AreEqual(PlayerCallsignService.RemoteCallsignTakenMessage, result.ValidationMessage);
    }

    [TestMethod]
    public void TryConfirmCallsign_AllowsPlayWhenSupabaseRegistryIsUnavailable()
    {
        ConfigureSupabase();
        SupabaseCallsignClient.UseHttpClientForTests(new HttpClient(new UnavailableCallsignRegistryHandler()));

        var result = PlayerCallsignService.TryConfirmCallsign("solar vega");

        Assert.IsTrue(result.IsAccepted);
        Assert.AreEqual("SOLAR VEGA", result.Callsign);
    }

    [TestMethod]
    public void LoadReservedLocalCallsigns_UsesSavedProfilesAndHighscores()
    {
        File.WriteAllText(PersistenceSetup.GetPlayerProgressFilePath("Local Hero"), "not encrypted for this test");
        File.WriteAllText(PersistenceSetup.GetPlayerTutorialProgressFilePath("Tutorial Ace"), "{}");
        HighscoreService.SaveLocalHighscores(new HighscoreList
        {
            Entries = new List<HighscoreEntry>
            {
                CreateEntry("Score Ace")
            }
        });

        var reserved = PlayerCallsignService.LoadReservedLocalCallsigns();

        Assert.IsTrue(reserved.Contains("LOCAL HERO"));
        Assert.IsTrue(reserved.Contains("TUTORIAL ACE"));
        Assert.IsTrue(reserved.Contains("SCORE ACE"));
    }

    [TestMethod]
    public void SelectLocalProfileCallsign_CyclesThroughLocalProfiles()
    {
        PersistenceSetup.SaveLastPlayerName("Last Ace");
        File.WriteAllText(PersistenceSetup.GetPlayerProgressFilePath("Local Hero"), "not encrypted for this test");

        var next = PlayerCallsignService.SelectLocalProfileCallsign("Last Ace", 1);
        var previous = PlayerCallsignService.SelectLocalProfileCallsign(next, -1);

        Assert.AreEqual("LOCAL HERO", next);
        Assert.AreEqual("LAST ACE", previous);
    }

    private static void ConfigureSupabase()
    {
        PersistenceSetup.SupabaseUrl = "https://example.supabase.co";
        PersistenceSetup.SupabaseAnonKey = "test-key";
    }

    private static HighscoreEntry CreateEntry(string playerName)
    {
        return new HighscoreEntry
        {
            PlayerName = PlayerNameFormatter.Normalize(playerName),
            Score = 1000,
            WaveReached = 1,
            DateUtc = DateTime.UtcNow.ToString("o")
        };
    }

    private sealed class CallsignRegistryHandler : HttpMessageHandler
    {
        private readonly HashSet<string> _registered;

        public CallsignRegistryHandler(params string[] registered)
        {
            _registered = new HashSet<string>(
                registered.Select(PlayerNameFormatter.Normalize),
                StringComparer.OrdinalIgnoreCase);
        }

        public List<string> InsertedCallsigns { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                var requested = ExtractRequestedCallsign(request.RequestUri);
                string body = _registered.Contains(requested)
                    ? $"[{{\"player_name\":\"{requested}\"}}]"
                    : "[]";

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body)
                });
            }

            if (request.Method == HttpMethod.Post)
            {
                string json = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                var callsign = PlayerNameFormatter.Normalize(doc.RootElement.GetProperty("player_name").GetString());
                if (_registered.Contains(callsign))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict));

                _registered.Add(callsign);
                InsertedCallsigns.Add(callsign);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        }

        private static string ExtractRequestedCallsign(Uri? uri)
        {
            Assert.IsNotNull(uri);
            var marker = "player_name=eq.";
            var query = uri!.Query.TrimStart('?').Split('&');
            var nameFilter = query.First(part => part.StartsWith(marker, StringComparison.OrdinalIgnoreCase));
            return PlayerNameFormatter.Normalize(Uri.UnescapeDataString(nameFilter[marker.Length..]));
        }
    }

    private sealed class UnavailableCallsignRegistryHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
