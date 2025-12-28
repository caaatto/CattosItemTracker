using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CattosTracker.Services;

public class LuaParser
{
    public static Dictionary<string, Dictionary<string, object>>? ParseFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[LuaParser] File not found: {filePath}");
                return null;
            }

            var content = File.ReadAllText(filePath);
            Console.WriteLine($"[LuaParser] Read {content.Length} bytes from {filePath}");

            if (!content.Contains("CattosItemTracker_DB"))
            {
                Console.WriteLine("[LuaParser] Not a CattosItemTracker SavedVariables file");
                Console.WriteLine($"[LuaParser] File starts with: {content.Substring(0, Math.Min(100, content.Length))}");
                return null;
            }

            Console.WriteLine("[LuaParser] Found CattosItemTracker_DB, parsing...");

            var result = new Dictionary<string, Dictionary<string, object>>();

            // Find the characters table - match everything between ["characters"] = { and the closing }
            // We need to count braces to find the correct closing brace
            var charactersStartMatch = Regex.Match(content, @"\[""characters""\]\s*=\s*\{");

            if (!charactersStartMatch.Success)
            {
                Console.WriteLine("[LuaParser] Could not find characters table");
                return result;
            }

            // Extract the characters content by counting braces
            var startIndex = charactersStartMatch.Index + charactersStartMatch.Length;
            var charactersContent = ExtractBracedContent(content, startIndex);

            if (string.IsNullOrEmpty(charactersContent))
            {
                Console.WriteLine("[LuaParser] Characters table is empty");
                return result;
            }

            Console.WriteLine($"[LuaParser] Found characters content: {charactersContent.Length} chars");

            // Parse each character entry
            // We need to find character keys (format: Name-Realm) and skip nested tables
            var charPattern = @"\[""([^""]+)""\]\s*=\s*\{";
            var charMatches = Regex.Matches(charactersContent, charPattern);

            Console.WriteLine($"[LuaParser] Found {charMatches.Count} potential character entries");

            foreach (Match charMatch in charMatches)
            {
                var charKey = charMatch.Groups[1].Value;

                // Check if this looks like a character key (contains a dash)
                if (!charKey.Contains("-"))
                {
                    Console.WriteLine($"[LuaParser] Skipping non-character key: {charKey}");
                    continue;
                }

                Console.WriteLine($"[LuaParser] Processing character: {charKey}");

                // Extract character data
                var charStartIndex = charMatch.Index + charMatch.Length;
                var charData = ExtractBracedContent(charactersContent, charStartIndex);

                if (string.IsNullOrEmpty(charData))
                {
                    Console.WriteLine($"[LuaParser]   No data for {charKey}");
                    continue;
                }

                var characterDict = new Dictionary<string, object>();

                // Parse simple string values
                ParseStringValue(charData, "name", characterDict);
                ParseStringValue(charData, "realm", characterDict);
                ParseStringValue(charData, "class", characterDict);
                ParseStringValue(charData, "lastFingerprint", characterDict);

                // Parse numeric values
                ParseNumericValue(charData, "lastUpdate", characterDict);
                ParseNumericValue(charData, "level", characterDict);

                // Parse boolean values
                ParseBooleanValue(charData, "needsSync", characterDict);

                // Parse equipment table
                var equipmentMatch = Regex.Match(charData, @"\[""equipment""\]\s*=\s*\{");
                if (equipmentMatch.Success)
                {
                    var equipStartIndex = equipmentMatch.Index + equipmentMatch.Length;
                    var equipmentContent = ExtractBracedContent(charData, equipStartIndex);

                    if (!string.IsNullOrEmpty(equipmentContent))
                    {
                        var equipment = new Dictionary<string, int>();

                        // Parse equipment items
                        var equipPattern = @"\[""([^""]+)""\]\s*=\s*(\d+)";
                        var equipMatches = Regex.Matches(equipmentContent, equipPattern);

                        Console.WriteLine($"[LuaParser]   Found {equipMatches.Count} equipment items");

                        foreach (Match equipMatch in equipMatches)
                        {
                            var slot = equipMatch.Groups[1].Value;
                            var itemId = int.Parse(equipMatch.Groups[2].Value);
                            equipment[slot] = itemId;
                            Console.WriteLine($"[LuaParser]     {slot}: {itemId}");
                        }

                        characterDict["equipment"] = equipment;
                    }
                }

                result[charKey] = characterDict;
            }

            Console.WriteLine($"[LuaParser] Successfully parsed {result.Count} characters");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LuaParser] Error: {ex.Message}");
            Console.WriteLine($"[LuaParser] Stack: {ex.StackTrace}");
            return null;
        }
    }

    private static string ExtractBracedContent(string content, int startIndex)
    {
        if (startIndex >= content.Length) return "";

        int braceCount = 1;
        int endIndex = startIndex;

        while (endIndex < content.Length && braceCount > 0)
        {
            if (content[endIndex] == '{')
                braceCount++;
            else if (content[endIndex] == '}')
                braceCount--;

            if (braceCount > 0)
                endIndex++;
        }

        if (braceCount == 0)
        {
            return content.Substring(startIndex, endIndex - startIndex);
        }

        return "";
    }

    private static void ParseStringValue(string content, string key, Dictionary<string, object> dict)
    {
        var pattern = $@"\[""{key}""\]\s*=\s*""([^""]*)""";
        var match = Regex.Match(content, pattern);
        if (match.Success)
        {
            dict[key] = match.Groups[1].Value;
        }
    }

    private static void ParseNumericValue(string content, string key, Dictionary<string, object> dict)
    {
        var pattern = $@"\[""{key}""\]\s*=\s*(\d+)";
        var match = Regex.Match(content, pattern);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var value))
        {
            dict[key] = value;
        }
    }

    private static void ParseBooleanValue(string content, string key, Dictionary<string, object> dict)
    {
        var pattern = $@"\[""{key}""\]\s*=\s*(true|false)";
        var match = Regex.Match(content, pattern);
        if (match.Success)
        {
            dict[key] = match.Groups[1].Value == "true";
        }
    }
}