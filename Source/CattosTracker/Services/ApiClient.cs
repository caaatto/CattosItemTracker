using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CattosTracker.Models;

namespace CattosTracker.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string? _apiKey;
    private readonly string? _mainCharacter;
    private readonly bool _onlySendMain;

    public ApiClient(string apiUrl, string? apiKey = null, string? mainCharacter = null, bool onlySendMain = false)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _mainCharacter = mainCharacter;
        _onlySendMain = onlySendMain;

        // Create HttpClient with custom handler to handle SSL issues
        var handler = new HttpClientHandler();

        // For development/testing only - allows self-signed certificates
        // In production, you should properly configure SSL certificates
        if (_apiUrl.StartsWith("https://"))
        {
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        }

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        }

        Console.WriteLine($"[ApiClient] Initialized with URL: {_apiUrl}");
        Console.WriteLine($"[ApiClient] SSL validation bypassed: {_apiUrl.StartsWith("https://")}");
    }

    public async Task<bool> SendAllCharactersAsync(List<CharacterData> characters)
    {
        try
        {
            Console.WriteLine($"[ApiClient] SendAllCharactersAsync called with {characters.Count} characters");
            Console.WriteLine($"[ApiClient] API URL: {_apiUrl}");
            Console.WriteLine($"[ApiClient] API Key configured: {!string.IsNullOrEmpty(_apiKey)}");

            var payload = new List<object>();

            // ONLY send main character if configured
            var charactersToSend = _onlySendMain && !string.IsNullOrEmpty(_mainCharacter)
                ? characters.Where(c => c.Character == _mainCharacter).ToList()
                : characters;

            Console.WriteLine($"[ApiClient] Main character: {_mainCharacter ?? "none"}");
            Console.WriteLine($"[ApiClient] Only send main: {_onlySendMain}");
            Console.WriteLine($"[ApiClient] Characters to send: {charactersToSend.Count}");

            foreach (var character in charactersToSend)
            {
                var snapshot = character.LatestSnapshot;
                if (snapshot == null || snapshot.Equipment == null) continue;

                // Convert equipment to API format with standardized English slot names
                var equipment = new Dictionary<string, int>();

                // Mapping to ensure correct English slot names (supports German WoW clients)
                var slotMapping = new Dictionary<string, string>
                {
                    // English to English (when WoW is in English)
                    {"Head", "Head"},
                    {"Neck", "Neck"},
                    {"Shoulder", "Shoulder"},
                    {"Chest", "Chest"},
                    {"Waist", "Waist"},
                    {"Legs", "Legs"},
                    {"Feet", "Feet"},
                    {"Wrist", "Wrist"},
                    {"Hands", "Hands"},
                    {"Finger1", "Finger1"},
                    {"Finger2", "Finger2"},
                    {"Trinket1", "Trinket1"},
                    {"Trinket2", "Trinket2"},
                    {"Back", "Back"},
                    {"MainHand", "MainHand"},
                    {"OffHand", "OffHand"},
                    {"Ranged", "Ranged"},
                    {"Tabard", "Tabard"},
                    {"Shirt", "Shirt"},

                    // German to English (when WoW is in German)
                    {"Kopf", "Head"},
                    {"Hals", "Neck"},
                    {"Schulter", "Shoulder"},
                    {"Brust", "Chest"},
                    {"Taille", "Waist"},
                    {"Gürtel", "Waist"},
                    {"Beine", "Legs"},
                    {"Füße", "Feet"},
                    {"Handgelenke", "Wrist"},
                    {"Hände", "Hands"},
                    {"Ring1", "Finger1"},
                    {"Ring2", "Finger2"},
                    {"Schmuck1", "Trinket1"},
                    {"Schmuck2", "Trinket2"},
                    {"Rücken", "Back"},
                    {"Haupthand", "MainHand"},
                    {"Nebenhand", "OffHand"},
                    {"Schildhand", "OffHand"},
                    {"Distanz", "Ranged"},
                    {"Wappenrock", "Tabard"},
                    {"Hemd", "Shirt"}
                };

                foreach (var (slotName, itemId) in snapshot.Equipment)
                {
                    if (itemId > 0)
                    {
                        // Use mapped name if available, otherwise use original
                        var apiSlotName = slotMapping.ContainsKey(slotName) ? slotMapping[slotName] : slotName;
                        equipment[apiSlotName] = itemId;
                        Console.WriteLine($"[ApiClient]   Slot: {slotName} -> {apiSlotName} = {itemId}");
                    }
                }

                // Only send if equipment has items
                if (equipment.Count == 0) continue;

                // Map German class names to English
                var classMapping = new Dictionary<string, string>
                {
                    // German to English
                    {"Krieger", "Warrior"},
                    {"Paladin", "Paladin"},
                    {"Jäger", "Hunter"},
                    {"Schurke", "Rogue"},
                    {"Priester", "Priest"},
                    {"Schamane", "Shaman"},
                    {"Magier", "Mage"},
                    {"Hexenmeister", "Warlock"},
                    {"Druide", "Druid"},
                    {"Todesritter", "Death Knight"},
                    // English to English (passthrough)
                    {"Warrior", "Warrior"},
                    {"Hunter", "Hunter"},
                    {"Rogue", "Rogue"},
                    {"Priest", "Priest"},
                    {"Shaman", "Shaman"},
                    {"Mage", "Mage"},
                    {"Warlock", "Warlock"},
                    {"Druid", "Druid"},
                    {"Death Knight", "Death Knight"}
                };

                var englishClassName = character.Class;
                if (classMapping.ContainsKey(character.Class))
                {
                    englishClassName = classMapping[character.Class];
                }

                payload.Add(new
                {
                    character = character.Character,
                    characterName = character.CharacterName,
                    realm = character.RealmName,
                    className = englishClassName,
                    timestamp = snapshot.DateTime,
                    itemCount = equipment.Count,
                    equipment = equipment
                });
            }

            // Don't send if no characters with equipment
            if (payload.Count == 0)
            {
                Console.WriteLine("[ApiClient] No characters with equipment to send");
                return true;
            }

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"[ApiClient] Sending JSON payload ({json.Length} bytes):");
            Console.WriteLine($"[ApiClient] Full payload:\n{json}");
            Console.WriteLine($"[ApiClient] ============ END OF PAYLOAD ============");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"[ApiClient] Sending POST request to {_apiUrl}");
            Console.WriteLine($"[ApiClient] Request headers: API Key = {(_apiKey != null ? "SET" : "NOT SET")}");

            var response = await _httpClient.PostAsync(_apiUrl, content);

            Console.WriteLine($"[ApiClient] Response Status: {response.StatusCode}");
            Console.WriteLine($"[ApiClient] Response Headers: {response.Headers.ToString()}");

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[ApiClient] Response Body: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[ApiClient] API Error {response.StatusCode}: {responseContent}");
                return false;
            }

            Console.WriteLine($"[ApiClient] Successfully sent {payload.Count} characters to API");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Error: {ex.Message}");
            return false;
        }
    }
}
