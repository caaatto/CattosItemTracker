using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using CattosTracker.Models;
using CattosTracker.Services;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace CattosTracker.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly Config _config;
    private readonly DataManager _dataManager;
    private readonly ApiClient? _apiClient;
    private DispatcherTimer? _updateTimer;
    private readonly object _lockObject = new object();
    private Window? _mainWindow;

    private bool _isMonitoring;
    private string _statusText = "Gestoppt";
    private string _statusColor = "#999999";
    private CharacterData? _selectedCharacter;
    private ObservableCollection<RealmGroup> _realms = new();
    private ObservableCollection<CharacterData> _allCharacters = new();
    private string _lastSentEquipmentHash = "";

    public MainWindowViewModel()
    {
        Console.WriteLine("[CONSTRUCTOR] ========== APP STARTING ==========");

        _config = Config.Load();
        _dataManager = new DataManager();

        // Initialize API Client if enabled
        if (_config.EnableApi)
        {
            _apiClient = new ApiClient(_config.ApiUrl, _config.ApiKey, _config.MainCharacter, _config.OnlySendMain);
        }

        // Initialize empty collections first
        LoadCharacters();

        // FORCE START MONITORING - NO CONDITIONS
        Console.WriteLine("[CONSTRUCTOR] FORCING StartMonitoring()");
        StatusText = "Starting...";
        StatusColor = "#FFA500";

        // Start monitoring after a short delay to ensure UI is ready
        Task.Run(async () =>
        {
            await Task.Delay(1000); // Wait 1 second for UI
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine("[CONSTRUCTOR] Now calling StartMonitoring()");
                StartMonitoring();
            });
        });
    }

    public void SetMainWindow(Window window)
    {
        _mainWindow = window;

        // Only check WoW path if not already monitoring
        if (!IsMonitoring && !_config.IsWowPathValid())
        {
            Console.WriteLine("[SetMainWindow] Window set, WoW path invalid - prompting user");
            Dispatcher.UIThread.InvokeAsync(async () => await CheckWowPathAndStartAsync());
        }
        else if (IsMonitoring)
        {
            Console.WriteLine("[SetMainWindow] Already monitoring - skipping initialization");
        }
    }

    private async Task CheckWowPathAndStartAsync()
    {
        // Check if WoW path is valid
        if (!_config.IsWowPathValid())
        {
            Console.WriteLine("[CheckWowPath] WoW installation not found at: " + _config.WowPath);

            if (_mainWindow != null)
            {
                // Show folder selection dialog
                var storage = _mainWindow.StorageProvider;
                var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Wähle dein World of Warcraft Verzeichnis",
                    AllowMultiple = false
                });

                if (result != null && result.Count > 0)
                {
                    var selectedPath = result[0].Path.LocalPath;
                    Console.WriteLine($"[CheckWowPath] User selected: {selectedPath}");

                    // Update config with new path
                    _config.WowPath = selectedPath;
                    _config.Save();

                    // Verify it's a valid WoW installation
                    if (_config.IsWowPathValid())
                    {
                        Console.WriteLine("[CheckWowPath] Valid WoW installation found!");
                        StatusText = "WoW-Installation gefunden!";
                        StatusColor = "#00FF00";
                        StartMonitoring();
                    }
                    else
                    {
                        Console.WriteLine("[CheckWowPath] Selected folder is not a valid WoW installation");
                        StatusText = "Ungültiges WoW-Verzeichnis!";
                        StatusColor = "#FF6B6B";
                    }
                }
                else
                {
                    Console.WriteLine("[CheckWowPath] User cancelled folder selection");
                    StatusText = "Keine WoW-Installation gewählt";
                    StatusColor = "#FF6B6B";
                }
            }
            else
            {
                Console.WriteLine("[CheckWowPath] MainWindow not set, cannot show dialog");
                StatusText = "WoW-Installation nicht gefunden";
                StatusColor = "#FF6B6B";
            }
        }
        else
        {
            Console.WriteLine("[CheckWowPath] Valid WoW installation found at: " + _config.WowPath);
            StartMonitoring();
        }
    }

    // Simple method to set main character without complex command binding

    // Manual refresh method that can be called
    public void ManualRefresh()
    {
        Console.WriteLine("[ManualRefresh] User requested manual refresh");
        Task.Run(async () => await RefreshDataAsync());
    }

    // Force sync to API
    public async void ForceSync()
    {
        Console.WriteLine("[ForceSync] User requested force sync to API");

        if (_apiClient == null || !_config.EnableApi)
        {
            Console.WriteLine("[ForceSync] API not enabled or client not configured");
            StatusText = "API nicht aktiviert!";
            StatusColor = "#FF6B6B";
            return;
        }

        try
        {
            StatusText = "Sende Daten an Server...";
            StatusColor = "#FFA500";

            var allCharacters = _dataManager.GetAllCharacters();
            Console.WriteLine($"[ForceSync] Sending {allCharacters.Count} characters to API");

            // Force send by resetting the hash
            _lastSentEquipmentHash = "";

            var success = await _apiClient.SendAllCharactersAsync(allCharacters);

            if (success)
            {
                // Update hash so we don't resend unnecessarily
                _lastSentEquipmentHash = ComputeEquipmentHash(allCharacters);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText = "Sync erfolgreich!";
                    StatusColor = "#4CAF50";
                });

                Console.WriteLine("[ForceSync] Data sent successfully!");

                // Reset status after 3 seconds
                await Task.Delay(3000);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText = IsMonitoring ? "Aktiv - Tracking läuft!" : "Gestoppt";
                    StatusColor = IsMonitoring ? "#4CAF50" : "#999999";
                });
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText = "Sync fehlgeschlagen!";
                    StatusColor = "#FF6B6B";
                });
                Console.WriteLine("[ForceSync] Failed to send data");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ForceSync] Error: {ex.Message}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = "Sync Fehler!";
                StatusColor = "#FF6B6B";
            });
        }
    }

    public void SetAsMain()
    {
        try
        {
            if (SelectedCharacter == null)
            {
                Console.WriteLine("[SetAsMain] No character selected");
                return;
            }

            Console.WriteLine($"[SetAsMain] Setting main to: {SelectedCharacter.Character}");

            // Update config
            _config.MainCharacter = SelectedCharacter.Character;
            _config.Save();

            // Update UI
            this.RaisePropertyChanged(nameof(MainCharacterName));

            Console.WriteLine($"[SetAsMain] Main character updated successfully: {SelectedCharacter.Character}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] SetAsMain failed: {ex.Message}");
        }
    }

    public string MainCharacterName => string.IsNullOrEmpty(_config.MainCharacter)
        ? "Kein Main ausgewählt"
        : _config.MainCharacter;

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set => this.RaiseAndSetIfChanged(ref _isMonitoring, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public string StatusColor
    {
        get => _statusColor;
        set => this.RaiseAndSetIfChanged(ref _statusColor, value);
    }

    public ObservableCollection<RealmGroup> Realms
    {
        get => _realms;
        set => this.RaiseAndSetIfChanged(ref _realms, value);
    }

    public ObservableCollection<CharacterData> AllCharacters
    {
        get => _allCharacters;
        set => this.RaiseAndSetIfChanged(ref _allCharacters, value);
    }

    public CharacterData? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCharacter, value);
            // Update equipment display when selection changes
            this.RaisePropertyChanged(nameof(EquipmentText));
        }
    }

    public string EquipmentText
    {
        get
        {
            try
            {
                Console.WriteLine("[EquipmentText] Getting equipment text...");

                if (SelectedCharacter == null)
                {
                    Console.WriteLine("[EquipmentText] No character selected");
                    return "🐱 Wähle einen Character aus der Liste!\n\nTipp: Nach Equipment-Änderungen in WoW\n'/reload' machen, damit die App aktualisiert!";
                }

                Console.WriteLine($"[EquipmentText] Character: {SelectedCharacter.Character}");
                Console.WriteLine($"[EquipmentText] Class: {SelectedCharacter.Class ?? "null"}");

                var snapshot = SelectedCharacter.LatestSnapshot;
                if (snapshot == null)
                {
                    Console.WriteLine("[EquipmentText] No snapshot available");
                    return $"🐱 {SelectedCharacter.Character}\n\nKeine Equipment-Daten vorhanden.\n\nEquippe Items in WoW und mach /reload!";
                }

                if (snapshot.Equipment == null || snapshot.Equipment.Count == 0)
                {
                    Console.WriteLine("[EquipmentText] No equipment in snapshot");
                    return $"🐱 {SelectedCharacter.Character}\n\nKeine Equipment-Daten vorhanden.\n\nEquippe Items in WoW und mach /reload!";
                }

                Console.WriteLine($"[EquipmentText] Building text for {snapshot.Equipment.Count} items...");

                var text = $"🐱 {SelectedCharacter.Character}\n";
                text += new string('═', 50) + "\n\n";
                text += $"Klasse: {SelectedCharacter.Class ?? "Unknown"}\n";
                text += $"Update: {snapshot.DateTime ?? "Unknown"}\n\n";

            var slotNames = new Dictionary<string, string>
            {
                {"Head", "Kopf"}, {"Neck", "Hals"}, {"Shoulder", "Schultern"},
                {"Chest", "Brust"}, {"Waist", "Taille"}, {"Legs", "Beine"},
                {"Feet", "Füße"}, {"Wrist", "Handgelenke"}, {"Hands", "Hände"},
                {"Finger1", "Ring 1"}, {"Finger2", "Ring 2"},
                {"Trinket1", "Schmuck 1"}, {"Trinket2", "Schmuck 2"},
                {"Back", "Rücken"}, {"MainHand", "Haupthand"}, {"OffHand", "Schildhand"},
                {"Ranged", "Distanz"}, {"Tabard", "Wappenrock"}
            };

                Console.WriteLine("[EquipmentText] Processing equipment slots...");
                foreach (var (slot, germanName) in slotNames)
                {
                    try
                    {
                        if (snapshot.Equipment != null && snapshot.Equipment.TryGetValue(slot, out var itemId) && itemId > 0)
                        {
                            text += $"{germanName,-18} Item-ID: {itemId}\n";
                        }
                    }
                    catch (Exception slotEx)
                    {
                        Console.WriteLine($"[ERROR] Processing slot {slot}: {slotEx.Message}");
                    }
                }

                text += "\n" + new string('─', 50) + "\n";
                text += $"Total: {snapshot.ItemCount} Items equipped";

                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] EquipmentText failed: {ex.Message}");
                return "🐱 Fehler beim Anzeigen der Daten";
            }
        }
    }

    private void StartMonitoring()
    {
        Console.WriteLine("[StartMonitoring] ============ STARTING MONITORING ============");

        IsMonitoring = true;
        StatusText = "Aktiv - Tracking läuft!";
        StatusColor = "#4CAF50";

        // Load data immediately
        Console.WriteLine("[StartMonitoring] Calling RefreshDataAsync() NOW");
        _ = RefreshDataAsync();

        // Set up timer for periodic updates - SIMPLE
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5) // Hard-coded 5 seconds
        };
        _updateTimer.Tick += async (s, e) =>
        {
            Console.WriteLine($"[TIMER TICK] {DateTime.Now:HH:mm:ss}");
            await RefreshDataAsync();
        };
        _updateTimer.Start();

        Console.WriteLine($"[StartMonitoring] Timer started - will tick every 5 seconds");
        Console.WriteLine("[StartMonitoring] ============ MONITORING ACTIVE ============");
    }

    private void StopMonitoring()
    {
        IsMonitoring = false;
        StatusText = "Gestoppt";
        StatusColor = "#999999";

        _updateTimer?.Stop();
        _updateTimer = null;
    }

    private async Task RefreshDataAsync()
    {
        Console.WriteLine($"[RefreshDataAsync] ========== REFRESH STARTING at {DateTime.Now:HH:mm:ss} ==========");

        try
        {
            // SIMPLIFY: Just read the one SavedVariables file directly
            string luaPath = @"G:\World of Warcraft\_classic_era_\WTF\Account\126652900#2\SavedVariables\CattosItemTracker.lua";
            Console.WriteLine($"[RefreshDataAsync] Reading: {luaPath}");

            if (!System.IO.File.Exists(luaPath))
            {
                Console.WriteLine("[RefreshDataAsync] ERROR: SavedVariables file not found!");
                return;
            }

            // Parse the Lua file
            var data = LuaParser.ParseFile(luaPath);

            if (data == null)
            {
                Console.WriteLine("[RefreshDataAsync] ERROR: Failed to parse SavedVariables!");
                return;
            }

            Console.WriteLine($"[RefreshDataAsync] Found {data.Count} characters");

            // Update each character
            foreach (var (charKey, charData) in data)
            {
                Console.WriteLine($"[RefreshDataAsync] Processing: {charKey}");

                // Get equipment
                Dictionary<string, int> equipment = new Dictionary<string, int>();
                if (charData.TryGetValue("equipment", out var equipObj) && equipObj is Dictionary<string, int> eq)
                {
                    equipment = eq;
                    Console.WriteLine($"[RefreshDataAsync]   Equipment: {equipment.Count} items");
                }
                else
                {
                    Console.WriteLine($"[RefreshDataAsync]   Equipment: Empty");
                }

                // Get class
                string className = "Unknown";
                if (charData.TryGetValue("class", out var classObj) && classObj is string cls)
                {
                    className = cls;
                }
                Console.WriteLine($"[RefreshDataAsync]   Class: {className}");

                // Save to DataManager
                _dataManager.AddSnapshot(charKey, equipment, className);
            }

            Console.WriteLine("[RefreshDataAsync] Data saved to equipment_history.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RefreshDataAsync] CRITICAL ERROR: {ex.Message}");
            Console.WriteLine($"[RefreshDataAsync] Stack: {ex.StackTrace}");
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Console.WriteLine("[RefreshDataAsync] Updating UI...");

            // Store selected character
            var selectedChar = SelectedCharacter?.Character;

            // Force complete reload of characters
            LoadCharacters();

            // Re-select character if it was selected
            if (!string.IsNullOrEmpty(selectedChar))
            {
                Console.WriteLine($"[RefreshDataAsync] Re-selecting character: {selectedChar}");
                var updated = _dataManager.GetCharacter(selectedChar);
                if (updated != null)
                {
                    // Force update by setting to null first
                    SelectedCharacter = null;
                    SelectedCharacter = updated;

                    // Force all UI properties to update
                    this.RaisePropertyChanged(nameof(SelectedCharacter));
                    this.RaisePropertyChanged(nameof(EquipmentText));
                    this.RaisePropertyChanged(nameof(AllCharacters));
                    this.RaisePropertyChanged(nameof(Realms));

                    Console.WriteLine($"[RefreshDataAsync] Forced UI update for {selectedChar}");
                }
            }

            // Update status
            StatusText = $"Aktualisiert: {DateTime.Now:HH:mm:ss}";
            StatusColor = "#4CAF50";

            Console.WriteLine("[RefreshDataAsync] ========== REFRESH COMPLETE ==========");
        });

        // Send data to API if enabled and changed
        if (_apiClient != null && _config.EnableApi)
        {
            try
            {
                Console.WriteLine("[RefreshDataAsync] Checking if API should be called...");
                Console.WriteLine($"[RefreshDataAsync] API Client exists: {_apiClient != null}");
                Console.WriteLine($"[RefreshDataAsync] API Enabled: {_config.EnableApi}");

                var allCharacters = _dataManager.GetAllCharacters();
                Console.WriteLine($"[RefreshDataAsync] Total characters to send: {allCharacters.Count}");

                var currentHash = ComputeEquipmentHash(allCharacters);
                Console.WriteLine($"[RefreshDataAsync] Current hash: {currentHash.Substring(0, Math.Min(50, currentHash.Length))}...");
                Console.WriteLine($"[RefreshDataAsync] Last sent hash: {(_lastSentEquipmentHash ?? "null").Substring(0, Math.Min(50, (_lastSentEquipmentHash ?? "").Length))}...");

                if (currentHash != _lastSentEquipmentHash)
                {
                    Console.WriteLine("[RefreshDataAsync] Equipment changed, sending to API...");
                    var success = await _apiClient.SendAllCharactersAsync(allCharacters);
                    if (success)
                    {
                        _lastSentEquipmentHash = currentHash;
                        Console.WriteLine("[RefreshDataAsync] Equipment data sent to API successfully!");
                    }
                    else
                    {
                        Console.WriteLine("[RefreshDataAsync] Failed to send data to API");
                    }
                }
                else
                {
                    Console.WriteLine("[RefreshDataAsync] No equipment changes, skipping API call");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RefreshDataAsync] API Error: {ex.Message}");
                Console.WriteLine($"[RefreshDataAsync] Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            Console.WriteLine($"[RefreshDataAsync] API not called - Client: {_apiClient != null}, Enabled: {_config.EnableApi}");
        }
    }

    private void LoadCharacters()
    {
        try
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.InvokeAsync(() => LoadCharacters());
                return;
            }

            var characters = _dataManager.GetAllCharacters();
            Console.WriteLine($"[LoadCharacters] Found {characters.Count} characters in history");

            // Update flat list for ListBox
            AllCharacters = new ObservableCollection<CharacterData>(characters);

            var realmGroups = characters
                .GroupBy(c => c.RealmName)
                .Select(g => new RealmGroup
                {
                    RealmName = g.Key,
                    Characters = new ObservableCollection<CharacterData>(g.ToList())
                })
                .ToList();

            Console.WriteLine($"[LoadCharacters] Grouped into {realmGroups.Count} realms");

            // Create new collection to avoid threading issues
            Realms = new ObservableCollection<RealmGroup>(realmGroups);

            // Auto-select main character if configured, otherwise first character
            if (SelectedCharacter == null)
            {
                // Try to select the main character first
                if (!string.IsNullOrEmpty(_config.MainCharacter))
                {
                    var mainChar = characters.FirstOrDefault(c => c.Character == _config.MainCharacter);
                    if (mainChar != null)
                    {
                        SelectedCharacter = mainChar;
                        Console.WriteLine($"[LoadCharacters] Auto-selected main: {SelectedCharacter.Character}");
                    }
                }

                // If no main or main not found, select first
                if (SelectedCharacter == null && realmGroups.Any() && realmGroups.First().Characters.Any())
                {
                    SelectedCharacter = realmGroups.First().Characters.First();
                    Console.WriteLine($"[LoadCharacters] Auto-selected first: {SelectedCharacter.Character}");
                }

                this.RaisePropertyChanged(nameof(EquipmentText));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] LoadCharacters failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    
    private string ComputeEquipmentHash(List<CharacterData> characters)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var character in characters.OrderBy(c => c.Character))
        {
            sb.Append(character.Character);
            sb.Append(":");
            if (character.LatestSnapshot?.Equipment != null)
            {
                foreach (var item in character.LatestSnapshot.Equipment.OrderBy(kv => kv.Key))
                {
                    sb.Append($"{item.Key}={item.Value},");
                }
            }
            sb.Append(";");
        }
        return sb.ToString();
    }
}
public class RealmGroup
{
    public string RealmName { get; set; } = string.Empty;
    public ObservableCollection<CharacterData> Characters { get; set; } = new();
}
