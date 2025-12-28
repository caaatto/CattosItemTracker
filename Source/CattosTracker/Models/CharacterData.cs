using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CattosTracker.Models;

public class CharacterData
{
    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("realm")]
    public string Realm { get; set; } = string.Empty;

    [JsonPropertyName("snapshots")]
    public List<EquipmentSnapshot> Snapshots { get; set; } = new();

    public string CharacterName => !string.IsNullOrEmpty(Character) && Character.Contains('-')
        ? Character.Split('-')[0]
        : Character ?? "Unknown";

    public string RealmName => !string.IsNullOrEmpty(Character) && Character.Contains('-')
        ? Character.Split('-')[1]
        : "Unknown";

    public EquipmentSnapshot? LatestSnapshot => Snapshots != null && Snapshots.Count > 0
        ? Snapshots[^1]
        : null;
}

public class EquipmentSnapshot
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("datetime")]
    public string DateTime { get; set; } = string.Empty;

    [JsonPropertyName("equipment")]
    public Dictionary<string, int> Equipment { get; set; } = new();

    public int ItemCount => Equipment?.Count(kv => kv.Value > 0) ?? 0;
}
