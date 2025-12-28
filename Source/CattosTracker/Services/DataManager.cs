using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CattosTracker.Models;

namespace CattosTracker.Services;

public class DataManager
{
    private const string DataFile = "equipment_history.json";
    private Dictionary<string, CharacterData> _history = new();

    public DataManager()
    {
        Load();
    }

    public void Load()
    {
        if (File.Exists(DataFile))
        {
            try
            {
                var json = File.ReadAllText(DataFile);
                _history = JsonSerializer.Deserialize<Dictionary<string, CharacterData>>(json)
                    ?? new Dictionary<string, CharacterData>();
            }
            catch
            {
                _history = new Dictionary<string, CharacterData>();
            }
        }
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_history, options);
        File.WriteAllText(DataFile, json);
    }

    public void AddSnapshot(string charKey, Dictionary<string, int> equipment, string className)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (!_history.ContainsKey(charKey))
        {
            _history[charKey] = new CharacterData
            {
                Character = charKey,
                Class = className,
                Realm = charKey.Contains('-') ? charKey.Split('-')[1] : "Unknown",
                Snapshots = new List<EquipmentSnapshot>()
            };
        }

        var charData = _history[charKey];

        // Update class if it was "Unknown" or different
        if (charData.Class == "Unknown" || charData.Class != className)
        {
            charData.Class = className;
        }

        // Check if equipment actually changed
        if (charData.Snapshots.Count > 0)
        {
            var lastSnapshot = charData.Snapshots[^1];
            if (EquipmentEquals(lastSnapshot.Equipment, equipment))
                return; // No change
        }

        var snapshot = new EquipmentSnapshot
        {
            Timestamp = timestamp,
            DateTime = dateTime,
            Equipment = new Dictionary<string, int>(equipment)
        };

        charData.Snapshots.Add(snapshot);

        // Keep only last 100 snapshots
        if (charData.Snapshots.Count > 100)
        {
            charData.Snapshots = charData.Snapshots.Skip(charData.Snapshots.Count - 100).ToList();
        }

        Save();
    }

    private bool EquipmentEquals(Dictionary<string, int> eq1, Dictionary<string, int> eq2)
    {
        if (eq1.Count != eq2.Count)
            return false;

        foreach (var kvp in eq1)
        {
            if (!eq2.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                return false;
        }

        return true;
    }

    public List<CharacterData> GetAllCharacters()
    {
        return _history.Values.OrderBy(c => c.RealmName).ThenBy(c => c.CharacterName).ToList();
    }

    public CharacterData? GetCharacter(string charKey)
    {
        return _history.TryGetValue(charKey, out var character) ? character : null;
    }
}
