using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace UlsterTravelKioskApplication.Services
{
    // handles communication with Amadeus APIs
    public class APIHelper
    {
        private static readonly HttpClient _http = new HttpClient(); // shared HttpClient

        // Amadeus shut down their self-service API portal, so this endpoint is no longer functional.
        // Kept here as a placeholder. the routes flow falls back to routes.csv when credentials are absent (see APIProcessor), and delay predictions are now generated locally.
        private const string BaseUrl = "https://test.api.amadeus.com";


        // requests OAuth access token via api secret and key
        public async Task<string> GetAccessTokenAsync(string apiKey, string apiSecret)
        {
            apiKey = (apiKey ?? "").Trim();
            apiSecret = (apiSecret ?? "").Trim();

            var url = $"{BaseUrl}/v1/security/oauth2/token"; // OAuth token endpoint

            // form data required for the OAuth client credentials flow
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("grant_type", "client_credentials"),
                new KeyValuePair<string,string>("client_id", apiKey),
                new KeyValuePair<string,string>("client_secret", apiSecret)
            });

            // sends POST request to get token
            using var resp = await _http.PostAsync(url, form);
            string body = await resp.Content.ReadAsStringAsync();

            // if request fails, throws exception
            if (!resp.IsSuccessStatusCode)
                throw new Exception(
                    $"Token request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {Trim(body)}");

            // parse JSON response
            using var doc = JsonDocument.Parse(body);

            // extract access token property
            if (!doc.RootElement.TryGetProperty("access_token", out var tokenEl))
                throw new Exception(
                    $"Token response missing access_token. Body: {Trim(body)}");

            string token = tokenEl.GetString() ?? "";

            // ensures token is not empty
            if (string.IsNullOrWhiteSpace(token))
                throw new Exception(
                    $"Token was empty. Body: {Trim(body)}");

            return token;
        }

        // gets airport codes for selected airport
        public async Task<List<string>> GetAirportDirectDestinationsAsync(
            string accessToken,
            string departureAirportCode)
        {
            departureAirportCode = (departureAirportCode ?? "").Trim().ToUpper();

            // departure airport code must be valid IATA code (3 letters)
            if (departureAirportCode.Length != 3)
                return new List<string>();

            var url = $"{BaseUrl}/v1/airport/direct-destinations?departureAirportCode={departureAirportCode}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception(
                    $"Direct destinations failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {Trim(body)}");

            using var doc = JsonDocument.Parse(body);

            if (!doc.RootElement.TryGetProperty("data", out var dataEl))
                return new List<string>();

            var results = new List<string>();

            // ensures data ia an array (destination objects)
            if (dataEl.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var item in dataEl.EnumerateArray())
            {
                if (item.TryGetProperty("iataCode", out var codeEl))
                {
                    var code = (codeEl.GetString() ?? "").Trim().ToUpper();
                    if (code.Length == 3)
                        results.Add(code);
                }
            }

            return results;
        }

        // gets destination airport codes for selected airline
        public async Task<List<string>> GetAirlineDestinationsAsync(
            string accessToken,
            string airlineCode)
        {
            airlineCode = (airlineCode ?? "").Trim().ToUpper();

            // airlineCode (IATA) are between 2-3 letters long
            if (airlineCode.Length < 2 || airlineCode.Length > 3)
                return new List<string>();

            var url = $"{BaseUrl}/v1/airline/destinations?airlineCode={airlineCode}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception(
                    $"Airline destinations failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {Trim(body)}");

            using var doc = JsonDocument.Parse(body);

            if (!doc.RootElement.TryGetProperty("data", out var dataEl))
                return new List<string>();

            var results = new List<string>();

            if (dataEl.ValueKind != JsonValueKind.Array)
                return results;

            foreach (var item in dataEl.EnumerateArray())
            {
                // property names may differ between API responses
                string code = "";

                if (item.TryGetProperty("iataCode", out var iataEl))
                    code = iataEl.GetString() ?? "";

                if (string.IsNullOrWhiteSpace(code) && item.TryGetProperty("destination", out var destEl))
                    code = destEl.GetString() ?? "";

                if (string.IsNullOrWhiteSpace(code) && item.TryGetProperty("destinationCode", out var dest2El))
                    code = dest2El.GetString() ?? "";

                code = code.Trim().ToUpper();

                if (code.Length == 3)
                    results.Add(code);
            }

            return results;
        }


        // shortens long API responses
        private static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= 250 ? s : s.Substring(0, 250) + "...";
        }
    }
}
