using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CattosTracker.Services;

public class CharacterScanner
{
    private readonly string _wowPath;

    public CharacterScanner(string wowPath)
    {
        _wowPath = wowPath.Replace('/', '\\');
    }

    public List<CharacterInfo> ScanAllCharacters()
    {
        var characters = new List<CharacterInfo>();

        try
        {
            var wtfPath = Path.Combine(_wowPath, "WTF", "Account");
            if (!Directory.Exists(wtfPath))
            {
                Console.WriteLine($"[CharacterScanner] WTF path not found: {wtfPath}");
                return characters;
            }

            // Find all account folders
            foreach (var accountDir in Directory.GetDirectories(wtfPath))
            {
                var accountName = Path.GetFileName(accountDir);
                if (accountName.StartsWith(".")) continue; // Skip hidden folders

                Console.WriteLine($"[CharacterScanner] Scanning account: {accountName}");

                // Find all server folders
                foreach (var serverDir in Directory.GetDirectories(accountDir))
                {
                    var serverName = Path.GetFileName(serverDir);
                    if (serverName == "SavedVariables") continue; // Skip SavedVariables folder

                    Console.WriteLine($"[CharacterScanner]   Server: {serverName}");

                    // Find all character folders
                    foreach (var charDir in Directory.GetDirectories(serverDir))
                    {
                        var charName = Path.GetFileName(charDir);
                        var charKey = $"{charName}-{serverName}";

                        Console.WriteLine($"[CharacterScanner]     Found character: {charKey}");

                        var charInfo = new CharacterInfo
                        {
                            AccountName = accountName,
                            ServerName = serverName,
                            CharacterName = charName,
                            CharacterKey = charKey,
                            FolderPath = charDir
                        };

                        // Try to get additional info from SavedVariables
                        var svPath = Path.Combine(accountDir, "SavedVariables", "CattosItemTracker.lua");
                        if (File.Exists(svPath))
                        {
                            var svContent = File.ReadAllText(svPath);
                            if (svContent.Contains($"\"{charKey}\""))
                            {
                                charInfo.HasSavedVariables = true;
                                Console.WriteLine($"[CharacterScanner]       Has SavedVariables data");
                            }
                        }

                        characters.Add(charInfo);
                    }
                }
            }

            Console.WriteLine($"[CharacterScanner] Total characters found: {characters.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CharacterScanner] Error: {ex.Message}");
        }

        return characters;
    }
}

public class CharacterInfo
{
    public string AccountName { get; set; } = "";
    public string ServerName { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string CharacterKey { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public bool HasSavedVariables { get; set; }
}