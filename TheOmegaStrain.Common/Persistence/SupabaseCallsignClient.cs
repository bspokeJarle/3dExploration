using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TheOmegaStrain.Common.Persistence
{
    public enum SupabaseCallsignReservationStatus
    {
        Reserved,
        Taken,
        Unavailable
    }

    /// <summary>
    /// Optional Supabase callsign registry. Missing network/table must never block local play.
    /// </summary>
    public static class SupabaseCallsignClient
    {
        private static readonly HttpClient DefaultHttp = CreateHttpClient();
        private static HttpClient Http = DefaultHttp;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        private static HttpClient CreateHttpClient() => new() { Timeout = TimeSpan.FromSeconds(3) };

        public static async Task<SupabaseCallsignReservationStatus> TryReserveAsync(string callsign)
        {
            if (!PersistenceSetup.IsSupabaseConfigured)
                return SupabaseCallsignReservationStatus.Unavailable;

            var normalized = PlayerNameFormatter.Normalize(callsign);
            if (string.IsNullOrWhiteSpace(normalized))
                return SupabaseCallsignReservationStatus.Unavailable;

            try
            {
                var existing = await FetchExistingAsync(normalized).ConfigureAwait(false);
                if (existing == null)
                    return SupabaseCallsignReservationStatus.Unavailable;

                if (existing.Value)
                    return SupabaseCallsignReservationStatus.Taken;

                return await InsertAsync(normalized).ConfigureAwait(false);
            }
            catch
            {
                return SupabaseCallsignReservationStatus.Unavailable;
            }
        }

        private static async Task<bool?> FetchExistingAsync(string normalized)
        {
            var escapedName = Uri.EscapeDataString(normalized);
            var url = $"{PersistenceSetup.SupabaseUrl}/rest/v1/{PersistenceSetup.SupabaseCallsignTableName}" +
                      $"?select=player_name&player_name=eq.{escapedName}&limit=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetSupabaseHeaders(request);

            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var rows = JsonSerializer.Deserialize<SupabaseCallsignRow[]>(body, JsonOptions);
            if (rows == null || rows.Length == 0)
                return false;

            for (int i = 0; i < rows.Length; i++)
            {
                if (string.Equals(
                    PlayerNameFormatter.Normalize(rows[i].PlayerName),
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<SupabaseCallsignReservationStatus> InsertAsync(string normalized)
        {
            var url = $"{PersistenceSetup.SupabaseUrl}/rest/v1/{PersistenceSetup.SupabaseCallsignTableName}";
            var json = JsonSerializer.Serialize(new SupabaseCallsignRow { PlayerName = normalized }, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            SetSupabaseHeaders(request);
            request.Headers.Add("Prefer", "return=minimal");

            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Conflict)
                return SupabaseCallsignReservationStatus.Taken;

            return response.IsSuccessStatusCode
                ? SupabaseCallsignReservationStatus.Reserved
                : SupabaseCallsignReservationStatus.Unavailable;
        }

        private static void SetSupabaseHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("apikey", PersistenceSetup.SupabaseAnonKey!);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", PersistenceSetup.SupabaseAnonKey!);
        }

        internal static void UseHttpClientForTests(HttpClient http)
        {
            Http = http;
        }

        internal static void ResetHttpClientForTests()
        {
            Http = DefaultHttp;
        }

        private sealed class SupabaseCallsignRow
        {
            public string? PlayerName { get; set; }
        }
    }
}
