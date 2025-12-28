# CattosItemTracker

WoW Classic Equipment Tracker with automatic API synchronization.

## Features

- Automatic equipment tracking for WoW Classic characters
- Real-time synchronization every 5 seconds
- API integration for equipment data sharing
- Support for both English and German WoW clients
- Multi-character support with realm grouping

## Setup

1. Copy `config.example.json` to `config.json`
2. Configure your WoW path in `config.json`
3. Set your API key (if using API features)
4. Run the application

## Configuration

Edit `config.json`:
- `wow_path`: Path to your WoW Classic installation
- `auto_start`: Start monitoring automatically
- `update_interval`: Refresh interval in seconds
- `api_url`: API endpoint for data synchronization
- `api_key`: Your API authentication key
- `enable_api`: Enable/disable API features
- `main_character`: Your main character name
- `only_send_main`: Only send main character data to API

## Building

```bash
dotnet publish Source/CattosTracker/CattosTracker.csproj -c Release -r win-x64 --self-contained
```

## WoW Addon

The companion WoW addon tracks equipment changes and saves them to SavedVariables.

## Requirements

- .NET 8.0 Runtime
- Windows x64
- WoW Classic