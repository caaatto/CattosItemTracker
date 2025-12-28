using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CattosTracker.Models;

public class Config
{
    [JsonPropertyName("wow_path")]
    public string WowPath { get; set; } = @"G:/World of Warcraft/_classic_era_";

    [JsonPropertyName("auto_start")]
    public bool AutoStart { get; set; } = true;

    [JsonPropertyName("update_interval")]
    public int UpdateInterval { get; set; } = 3;
    [JsonPropertyName("api_url")]
    public string ApiUrl { get; set; } = "https://clip.jetzt/api/connect";

    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; } = null;

    [JsonPropertyName("enable_api")]
    public bool EnableApi { get; set; } = true;
    [JsonPropertyName("main_character")]
    public string? MainCharacter { get; set; } = null;

    [JsonPropertyName("only_send_main")]
    public bool OnlySendMain { get; set; } = false;



    private static readonly string LocalConfigPath = "config.json";
    private static readonly string RoamingConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CattosTracker",
        "config.json"
    );

    private static string ConfigPath
    {
        get
        {
            // Prefer local config for portability
            if (File.Exists(LocalConfigPath))
                return LocalConfigPath;

            // Otherwise use roaming folder
            return RoamingConfigPath;
        }
    }

    public static Config Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<Config>(json) ?? new Config();
            }
            catch
            {
                return new Config();
            }
        }

        var config = new Config();
        config.Save();
        return config;
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, options);

        // Ensure directory exists for roaming config
        var configPath = ConfigPath;
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(configPath, json);
        Console.WriteLine($"[Config] Saved to: {configPath}");
    }

    public bool IsWowPathValid()
    {
        if (string.IsNullOrWhiteSpace(WowPath))
            return false;

        // Check if it's a valid WoW installation by looking for WoW.exe
        var wowExe = Path.Combine(WowPath, "WowClassic.exe");
        var wowExeAlternative = Path.Combine(WowPath, "Wow.exe");

        return File.Exists(wowExe) || File.Exists(wowExeAlternative);
    }

    public List<string> FindAccounts()
    {
        var accountsPath = Path.Combine(WowPath, "WTF", "Account");
        if (!Directory.Exists(accountsPath))
            return new List<string>();

        var accounts = new List<string>();
        foreach (var dir in Directory.GetDirectories(accountsPath))
        {
            var dirName = Path.GetFileName(dir);
            if (dirName.StartsWith("."))
                continue;

            var savedVarsPath = Path.Combine(dir, "SavedVariables", "CattosItemTracker.lua");
            if (File.Exists(savedVarsPath))
            {
                accounts.Add(dirName);
            }
        }

        return accounts;
    }

    public string GetSavedVarsPath(string accountName)
    {
        // First check for IDAPI.lua (new addon)
        var idapiPath = Path.Combine(WowPath, "WTF", "Account", accountName, "SavedVariables", "IDAPI.lua");
        Console.WriteLine($"[GetSavedVarsPath] Checking for IDAPI.lua: {idapiPath}");
        if (File.Exists(idapiPath))
        {
            Console.WriteLine($"[GetSavedVarsPath] Found IDAPI.lua!");
            return idapiPath;
        }

        // Fall back to old CattosItemTracker.lua
        var cattosPath = Path.Combine(WowPath, "WTF", "Account", accountName, "SavedVariables", "CattosItemTracker.lua");
        Console.WriteLine($"[GetSavedVarsPath] Using CattosItemTracker.lua: {cattosPath}");
        Console.WriteLine($"[GetSavedVarsPath] File exists: {File.Exists(cattosPath)}");
        return cattosPath;
    }
}
