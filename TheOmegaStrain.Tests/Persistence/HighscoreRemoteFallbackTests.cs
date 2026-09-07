using TheOmegaStrain.Game.World;
using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Common.Persistence;
using TheOmegaStrain.Domain;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TheOmegaStrain.Tests.Persistence;

[TestClass]
public class HighscoreRemoteFallbackTests
{
    private static readonly JsonSerializerOptions SupabaseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private string _originalLocalFolder = "";
    private string _testLocalFolder = "";
    private string _testApplicationFolder = "";
    private int _originalMaxEntries;
    private string? _originalSupabaseUrl;
    private string? _originalSupabaseAnonKey;
    private string _originalSupabaseTableName = "";
    private TimeSpan _originalRemoteFetchCooldown;
    private string? _originalApplicationFolderOverride;

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _originalMaxEntries = PersistenceSetup.MaxHighscoreEntries;
        _originalSupabaseUrl = PersistenceSetup.SupabaseUrl;
        _originalSupabaseAnonKey = PersistenceSetup.SupabaseAnonKey;
        _originalSupabaseTableName = PersistenceSetup.SupabaseTableName;
        _originalRemoteFetchCooldown = PersistenceSetup.RemoteFetchCooldown;
        _originalApplicationFolderOverride = PersistenceSetup.ApplicationFolderOverrideForTests;

        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainRemoteHighscoreTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.ApplicationFolderOverrideForTests = null;
        PersistenceSetup.MaxHighscoreEntries = 100;
        PersistenceSetup.SupabaseUrl = null;
        PersistenceSetup.SupabaseAnonKey = null;
        PersistenceSetup.SupabaseTableName = "highscores";
        PersistenceSetup.RemoteFetchCooldown = TimeSpan.FromMinutes(10);
        HighscoreService.ResetRemoteFetchStateForTests();
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.WorldFade = new WorldFadeState();
        GameState.ObjectIdCounter = 0;
        GameState.DeltaTime = 0f;
        PersistenceSetup.Initialize();
        HighscoreService.SaveLocalHighscores(new HighscoreList());
    }

    [TestCleanup]
    public void Cleanup()
    {
        HighscoreService.ResetRemoteFetchStateForTests();
        SupabaseHighscoreClient.ResetHttpClientForTests();
        PersistenceSetup.LocalFolder = _originalLocalFolder;
        PersistenceSetup.MaxHighscoreEntries = _originalMaxEntries;
        PersistenceSetup.SupabaseUrl = _originalSupabaseUrl;
        PersistenceSetup.SupabaseAnonKey = _originalSupabaseAnonKey;
        PersistenceSetup.SupabaseTableName = _originalSupabaseTableName;
        PersistenceSetup.RemoteFetchCooldown = _originalRemoteFetchCooldown;
        PersistenceSetup.ApplicationFolderOverrideForTests = _originalApplicationFolderOverride;

        try
        {
            if (Directory.Exists(_testLocalFolder))
                Directory.Delete(_testLocalFolder, recursive: true);

            if (Directory.Exists(_testApplicationFolder))
                Directory.Delete(_testApplicationFolder, recursive: true);
        }
        catch
        {
        }
    }

    [TestMethod]
    public void BuildBody_UsesRemoteScoresWhenSupabaseIsConfigured()
    {
        ConfigureSupabase();
        HighscoreService.FetchRemoteTopScoresAsync = _ =>
            Task.FromResult<List<HighscoreEntry>?>(new List<HighscoreEntry>
            {
                CreateEntry("REMOTEACE", 54950, 104)
            });

        string body = HighscoreOverlayFormatter.BuildBody();

        Assert.IsTrue(body.Contains("REMOTEACE"));
        Assert.IsFalse(body.Contains("No highscores recorded yet"));

        var cached = HighscoreService.LoadLocalHighscores();
        Assert.AreEqual(1, cached.Entries.Count);
        Assert.AreEqual("REMOTEACE", cached.Entries[0].PlayerName);
    }

    [TestMethod]
    public void BuildBody_UsesLocalScoresWhenRemoteFetchFails()
    {
        ConfigureSupabase();
        HighscoreService.SaveLocalHighscores(new HighscoreList
        {
            Entries = new List<HighscoreEntry>
            {
                CreateEntry("LOCALACE", 12345, 12)
            }
        });
        HighscoreService.FetchRemoteTopScoresAsync = _ =>
            Task.FromException<List<HighscoreEntry>?>(new HttpRequestException("offline"));

        string body = HighscoreOverlayFormatter.BuildBody();

        Assert.IsTrue(body.Contains("LOCALACE"));
        Assert.IsFalse(body.Contains("No highscores recorded yet"));
    }

    [TestMethod]
    public void GetTopScores_RetriesRemoteFetchAfterNullResponse()
    {
        ConfigureSupabase();
        int callCount = 0;
        HighscoreService.FetchRemoteTopScoresAsync = _ =>
        {
            callCount++;
            if (callCount == 1)
                return Task.FromResult<List<HighscoreEntry>?>(null);

            return Task.FromResult<List<HighscoreEntry>?>(new List<HighscoreEntry>
            {
                CreateEntry("RETRYACE", 22222, 22)
            });
        };

        var first = HighscoreService.GetTopScores(25);
        var second = HighscoreService.GetTopScores(25);

        Assert.AreEqual(0, first.Count);
        Assert.AreEqual(2, callCount);
        Assert.AreEqual(1, second.Count);
        Assert.AreEqual("RETRYACE", second[0].PlayerName);
    }

    [TestMethod]
    public void SupabaseHighscoreClient_ParsesSnakeCaseRows()
    {
        const string body = """
        [
          {
            "player_name": "charliea",
            "score": 54950,
            "wave_reached": 4,
            "total_kills": 104,
            "total_shots_fired": 391,
            "total_deaths": 47,
            "accuracy": 0.265985,
            "date_utc": "2026-04-19T19:59:23.1512993Z"
          }
        ]
        """;

        var entries = SupabaseHighscoreClient.ParseHighscoreRows(body);

        Assert.IsNotNull(entries);
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual("CHARLIEA", entries[0].PlayerName);
        Assert.AreEqual(54950, entries[0].Score);
        Assert.AreEqual(104, entries[0].TotalKills);
    }

    [TestMethod]
    public async Task PushEntryAsync_UpdatesExistingRemoteRowInsteadOfPostingDuplicate()
    {
        ConfigureSupabase();
        var handler = new RecordingHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse("""
                [
                  {
                    "id": 42,
                    "player_name": "charlieb",
                    "score": 5000,
                    "wave_reached": 1,
                    "total_kills": 4,
                    "total_shots_fired": 20,
                    "total_deaths": 1,
                    "accuracy": 0.2,
                    "date_utc": "2026-05-31T12:00:00Z"
                  }
                ]
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        SupabaseHighscoreClient.UseHttpClientForTests(new HttpClient(handler));

        await SupabaseHighscoreClient.PushEntryAsync(new HighscoreEntry
        {
            PlayerName = "CharlieB",
            Score = 6000,
            WaveReached = 2,
            TotalKills = 8,
            TotalShotsFired = 30,
            TotalDeaths = 1,
            Accuracy = 8f / 30f,
            DateUtc = "2026-05-31T12:05:00Z"
        });

        Assert.AreEqual(2, handler.Requests.Count);
        Assert.AreEqual(HttpMethod.Get, handler.Requests[0].Method);
        Assert.AreEqual(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.IsTrue(handler.Requests[1].Url.Contains("id=eq.42"));
        Assert.IsFalse(handler.Requests.Any(request => request.Method == HttpMethod.Post),
            "Existing remote players must be updated instead of inserted again.");
        Assert.IsTrue(handler.Requests[1].Body!.Contains("\"player_name\":\"CHARLIEB\""));
        Assert.IsTrue(handler.Requests[1].Body!.Contains("\"score\":6000"));
        Assert.IsTrue(handler.Requests[1].Body!.Contains("\"total_kills\":8"));
    }

    [TestMethod]
    public async Task PushEntryAsync_DoesNotDowngradeExistingRemoteScore()
    {
        ConfigureSupabase();
        var handler = new RecordingHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse("""
                [
                  {
                    "id": 24,
                    "player_name": "CHARLIEB",
                    "score": 9000,
                    "wave_reached": 5,
                    "total_kills": 50,
                    "total_shots_fired": 150,
                    "total_deaths": 3,
                    "accuracy": 0.333,
                    "date_utc": "2026-05-31T12:10:00Z"
                  }
                ]
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        SupabaseHighscoreClient.UseHttpClientForTests(new HttpClient(handler));

        await SupabaseHighscoreClient.PushEntryAsync(new HighscoreEntry
        {
            PlayerName = "charlieb",
            Score = 6000,
            WaveReached = 2,
            TotalKills = 8,
            TotalShotsFired = 30,
            TotalDeaths = 1,
            Accuracy = 8f / 30f,
            DateUtc = "2026-05-31T12:15:00Z"
        });

        Assert.AreEqual(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.IsTrue(handler.Requests[1].Body!.Contains("\"score\":9000"),
            "A lower score from a different machine must not overwrite the remote best score.");
        Assert.IsFalse(handler.Requests[1].Body!.Contains("\"score\":6000"));
        Assert.IsFalse(handler.Requests.Any(request => request.Method == HttpMethod.Post));
    }

    [TestMethod]
    public async Task PushEntryAsync_InsertsWhenRemotePlayerDoesNotExist()
    {
        ConfigureSupabase();
        var handler = new RecordingHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
                return JsonResponse("[]");

            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        SupabaseHighscoreClient.UseHttpClientForTests(new HttpClient(handler));

        await SupabaseHighscoreClient.PushEntryAsync(CreateEntry("NewAce", 34500, 34));

        Assert.AreEqual(2, handler.Requests.Count);
        Assert.AreEqual(HttpMethod.Get, handler.Requests[0].Method);
        Assert.AreEqual(HttpMethod.Post, handler.Requests[1].Method);
        Assert.IsTrue(handler.Requests[1].Body!.Contains("\"player_name\":\"NEWACE\""));
        Assert.IsTrue(handler.Requests[1].Body!.Contains("\"score\":34500"));
    }

    [TestMethod]
    public async Task PushEntryAsync_SecondSubmitForSamePlayerUpdatesSingleRemoteRow()
    {
        ConfigureSupabase();
        var handler = new InMemorySupabaseHighscoreHandler();
        SupabaseHighscoreClient.UseHttpClientForTests(new HttpClient(handler));

        await SupabaseHighscoreClient.PushEntryAsync(CreateEntry("CharlieB", 1000, 2));
        await SupabaseHighscoreClient.PushEntryAsync(CreateEntry(" charlieb ", 1600, 8));

        var rows = handler.RowsFor("CHARLIEB");

        Assert.AreEqual(1, rows.Count,
            "Submitting a better score for the same Supabase player must update the existing row, not insert a duplicate.");
        Assert.AreEqual("CHARLIEB", rows[0].PlayerName);
        Assert.AreEqual(1600L, rows[0].Score);
        Assert.AreEqual(8, rows[0].TotalKills);
        Assert.AreEqual(1, handler.PostCount);
        Assert.AreEqual(1, handler.PatchCount);
    }

    [TestMethod]
    public async Task PushEntryAsync_DoesNotBlindInsertWhenRemoteLookupFails()
    {
        ConfigureSupabase();
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        SupabaseHighscoreClient.UseHttpClientForTests(new HttpClient(handler));

        await SupabaseHighscoreClient.PushEntryAsync(CreateEntry("CharlieB", 34500, 34));

        Assert.AreEqual(1, handler.Requests.Count);
        Assert.AreEqual(HttpMethod.Get, handler.Requests[0].Method);
        Assert.IsFalse(handler.Requests.Any(request => request.Method == HttpMethod.Post),
            "A failed remote lookup must not create a fresh highscore row for a possibly existing player.");
    }

    [TestMethod]
    public async Task PushEntryAsync_MergesDuplicateRemoteRowsIntoBestExistingRow()
    {
        ConfigureSupabase();
        var handler = new RecordingHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse("""
                [
                  {
                    "id": 12,
                    "player_name": "CHARLIEB",
                    "score": 7000,
                    "wave_reached": 3,
                    "total_kills": 0,
                    "total_shots_fired": 0,
                    "total_deaths": 0,
                    "accuracy": 0,
                    "date_utc": "2026-05-31T12:10:00Z"
                  },
                  {
                    "id": 11,
                    "player_name": "charlieb",
                    "score": 5000,
                    "wave_reached": 2,
                    "total_kills": 44,
                    "total_shots_fired": 120,
                    "total_deaths": 2,
                    "accuracy": 0.366,
                    "date_utc": "2026-05-31T12:00:00Z"
                  }
                ]
                """);
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        SupabaseHighscoreClient.UseHttpClientForTests(new HttpClient(handler));

        await SupabaseHighscoreClient.PushEntryAsync(new HighscoreEntry
        {
            PlayerName = "CharlieB",
            Score = 6000,
            WaveReached = 4,
            TotalKills = 0,
            TotalShotsFired = 0,
            TotalDeaths = 0,
            Accuracy = 0,
            DateUtc = "2026-05-31T12:20:00Z"
        });

        Assert.AreEqual(HttpMethod.Patch, handler.Requests[1].Method);
        Assert.IsTrue(handler.Requests[1].Url.Contains("id=eq.12"),
            "The highest remote score row should be kept as the canonical row.");
        Assert.IsTrue(handler.Requests[1].Body!.Contains("\"score\":7000"));
        Assert.IsTrue(handler.Requests[1].Body!.Contains("\"total_kills\":44"),
            "Merging duplicate remote rows must preserve useful stat values.");
        Assert.IsFalse(handler.Requests.Any(request => request.Method == HttpMethod.Post));
    }

    [TestMethod]
    public void RefreshCurrentPageIfHighscorePage_RebuildsStaleIntroHighscoreBody()
    {
        ConfigureSupabase();
        HighscoreService.FetchRemoteTopScoresAsync = _ =>
            Task.FromResult<List<HighscoreEntry>?>(new List<HighscoreEntry>
            {
                CreateEntry("REFRESHACE", 44444, 44)
            });
        var overlay = new ScreenOverlayState();
        overlay.AddPage("RETROMESH SYSTEM INITIALIZING", "THE OMEGA STRAIN", "story", "footer");
        overlay.AddPage("RETROMESH // FIELD MANUAL", "TACTICAL BRIEFING", "tips", "footer");
        overlay.AddPage(
            "RETROMESH // HALL OF FAME",
            "TOP PILOTS",
            "No highscores recorded yet.\n\nBe the first pilot to make history!",
            "footer");
        overlay.CurrentPage = 2;
        overlay.ApplyPageContent();

        bool refreshed = HighscoreOverlayFormatter.RefreshCurrentPageIfHighscorePage(overlay);

        Assert.IsTrue(refreshed);
        Assert.IsTrue(overlay.Body.Contains("REFRESHACE"));
        Assert.IsTrue(overlay.Pages[2][2].Contains("REFRESHACE"));
        Assert.IsFalse(overlay.Body.Contains("No highscores recorded yet"));
    }

    [TestMethod]
    public void BuildBody_ShowsNoMoreThanTwentyTextLines()
    {
        HighscoreService.SaveLocalHighscores(new HighscoreList
        {
            Entries = Enumerable.Range(1, 25)
                .Select(i => CreateEntry($"PILOT{i:00}", 26000 - i, i))
                .ToList()
        });

        string body = HighscoreOverlayFormatter.BuildBody(count: 25);

        Assert.IsTrue(body.Contains("PILOT18"));
        Assert.IsFalse(body.Contains("PILOT19"));
        Assert.AreEqual(20, body.Split('\n').Length);
        Assert.AreEqual(25, HighscoreService.LoadLocalHighscores().Entries.Count);
    }

    [TestMethod]
    public void WorldStartup_InitializesPersistenceBeforeIntroOverlayBuildsHighscores()
    {
        File.WriteAllText(
            Path.Combine(_testLocalFolder, "secrets.json"),
            """
            {
              "SupabaseUrl": "https://example.supabase.co",
              "SupabaseAnonKey": "test-anon-key"
            }
            """);
        PersistenceSetup.SupabaseUrl = null;
        PersistenceSetup.SupabaseAnonKey = null;
        HighscoreService.ResetRemoteFetchStateForTests();
        HighscoreService.FetchRemoteTopScoresAsync = _ =>
            Task.FromResult<List<HighscoreEntry>?>(new List<HighscoreEntry>
            {
                CreateEntry("STARTUPACE", 33333, 33)
            });

        _ = new GameWorld();

        Assert.IsTrue(PersistenceSetup.IsSupabaseConfigured);
        var highscorePage = GameState.ScreenOverlayState.Pages.Single(page => page[1] == "TOP PILOTS");
        Assert.IsTrue(highscorePage[2].Contains("STARTUPACE"));
        Assert.IsFalse(highscorePage[2].Contains("No highscores recorded yet"));
    }

    [TestMethod]
    public void Initialize_LoadsSupabaseConfigFromApplicationFolderWhenAppDataSecretsAreMissing()
    {
        ConfigureTestApplicationFolder();
        File.WriteAllText(
            PersistenceSetup.OnlineServicesFilePath,
            """
            {
              "SupabaseUrl": "https://steam-build.supabase.co",
              "SupabaseAnonKey": "steam-build-anon-key"
            }
            """);
        PersistenceSetup.SupabaseUrl = null;
        PersistenceSetup.SupabaseAnonKey = null;

        PersistenceSetup.Initialize();

        Assert.AreEqual("https://steam-build.supabase.co", PersistenceSetup.SupabaseUrl);
        Assert.AreEqual("steam-build-anon-key", PersistenceSetup.SupabaseAnonKey);
    }

    [TestMethod]
    public void Initialize_PrefersAppDataSecretsOverApplicationFolderConfig()
    {
        ConfigureTestApplicationFolder();
        File.WriteAllText(
            PersistenceSetup.SecretsFilePath,
            """
            {
              "SupabaseUrl": "https://appdata.supabase.co",
              "SupabaseAnonKey": "appdata-anon-key"
            }
            """);
        File.WriteAllText(
            PersistenceSetup.OnlineServicesFilePath,
            """
            {
              "SupabaseUrl": "https://steam-build.supabase.co",
              "SupabaseAnonKey": "steam-build-anon-key"
            }
            """);
        PersistenceSetup.SupabaseUrl = null;
        PersistenceSetup.SupabaseAnonKey = null;

        PersistenceSetup.Initialize();

        Assert.AreEqual("https://appdata.supabase.co", PersistenceSetup.SupabaseUrl);
        Assert.AreEqual("appdata-anon-key", PersistenceSetup.SupabaseAnonKey);
    }

    [TestMethod]
    public void Initialize_LoadsLegacySecretsFromApplicationFolderWhenOnlineServicesConfigIsMissing()
    {
        ConfigureTestApplicationFolder();
        File.WriteAllText(
            PersistenceSetup.ApplicationSecretsFilePath,
            """
            {
              "SupabaseUrl": "https://legacy-steam.supabase.co",
              "SupabaseAnonKey": "legacy-steam-anon-key"
            }
            """);
        PersistenceSetup.SupabaseUrl = null;
        PersistenceSetup.SupabaseAnonKey = null;

        PersistenceSetup.Initialize();

        Assert.AreEqual("https://legacy-steam.supabase.co", PersistenceSetup.SupabaseUrl);
        Assert.AreEqual("legacy-steam-anon-key", PersistenceSetup.SupabaseAnonKey);
    }

    private static void ConfigureSupabase()
    {
        PersistenceSetup.SupabaseUrl = "https://example.supabase.co";
        PersistenceSetup.SupabaseAnonKey = "test-anon-key";
    }

    private void ConfigureTestApplicationFolder()
    {
        _testApplicationFolder = Path.Combine(
            Path.GetTempPath(),
            "OmegaStrainApplicationConfigTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testApplicationFolder);
        PersistenceSetup.ApplicationFolderOverrideForTests = _testApplicationFolder;
    }

    private static HighscoreEntry CreateEntry(string name, long score, int kills)
    {
        return new HighscoreEntry
        {
            PlayerName = name,
            Score = score,
            WaveReached = 1,
            TotalKills = kills,
            DateUtc = new DateTime(2026, 5, 31, 12, 0, 0, DateTimeKind.Utc).ToString("o")
        };
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        public List<RecordedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string? body = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.ToString() ?? "",
                body));

            return responseFactory(request);
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Url, string? Body);

    private sealed class InMemorySupabaseHighscoreHandler : HttpMessageHandler
    {
        private readonly List<RemoteHighscoreRow> rows = new();
        private long nextId = 1;

        public int PostCount { get; private set; }
        public int PatchCount { get; private set; }

        public List<RemoteHighscoreRow> RowsFor(string playerName)
        {
            var normalized = PlayerNameFormatter.Normalize(playerName);
            return rows
                .Where(row => string.Equals(row.PlayerName, normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
                return JsonResponse(BuildLookupResponse(request.RequestUri));

            if (request.Method == HttpMethod.Post)
            {
                PostCount++;
                var row = await ReadRowAsync(request).ConfigureAwait(false);
                row.Id = nextId++;
                row.PlayerName = PlayerNameFormatter.Normalize(row.PlayerName);
                rows.Add(row);
                return new HttpResponseMessage(HttpStatusCode.Created);
            }

            if (request.Method == HttpMethod.Patch)
            {
                PatchCount++;
                var incoming = await ReadRowAsync(request).ConfigureAwait(false);
                var id = ExtractIdFilter(request.RequestUri);
                var existing = rows.Single(row => row.Id == id);
                existing.PlayerName = PlayerNameFormatter.Normalize(incoming.PlayerName);
                existing.Score = incoming.Score;
                existing.WaveReached = incoming.WaveReached;
                existing.TotalKills = incoming.TotalKills;
                existing.TotalShotsFired = incoming.TotalShotsFired;
                existing.TotalDeaths = incoming.TotalDeaths;
                existing.Accuracy = incoming.Accuracy;
                existing.DateUtc = incoming.DateUtc;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        private string BuildLookupResponse(Uri? requestUri)
        {
            var requestedName = ExtractPlayerNameFilter(requestUri);
            var matches = rows.Where(row =>
                string.Equals(row.PlayerName, requestedName, StringComparison.OrdinalIgnoreCase));

            return JsonSerializer.Serialize(matches, SupabaseJsonOptions);
        }

        private static async Task<RemoteHighscoreRow> ReadRowAsync(HttpRequestMessage request)
        {
            var body = request.Content == null
                ? ""
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<RemoteHighscoreRow>(body, SupabaseJsonOptions)
                   ?? new RemoteHighscoreRow();
        }

        private static string ExtractPlayerNameFilter(Uri? requestUri)
        {
            const string prefix = "player_name=ilike.";
            var query = requestUri?.Query.TrimStart('?') ?? "";
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return PlayerNameFormatter.Normalize(Uri.UnescapeDataString(part[prefix.Length..]));
            }

            return "";
        }

        private static long ExtractIdFilter(Uri? requestUri)
        {
            const string prefix = "id=eq.";
            var query = requestUri?.Query.TrimStart('?') ?? "";
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    long.TryParse(part[prefix.Length..], out var id))
                {
                    return id;
                }
            }

            return -1;
        }
    }

    private sealed class RemoteHighscoreRow
    {
        public long Id { get; set; }
        public string? PlayerName { get; set; }
        public long Score { get; set; }
        public int WaveReached { get; set; }
        public int TotalKills { get; set; }
        public int TotalShotsFired { get; set; }
        public int TotalDeaths { get; set; }
        public float Accuracy { get; set; }
        public string? DateUtc { get; set; }
    }
}
