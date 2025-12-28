# PowerShell Script to test reading SavedVariables

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Testing WoW SavedVariables Reading" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

$wowPath = "G:/World of Warcraft/_classic_era_"
$accountPath = "$wowPath/WTF/Account"

Write-Host "`nSearching for accounts in: $accountPath" -ForegroundColor Yellow

# Find all accounts
$accounts = Get-ChildItem -Path $accountPath -Directory | Where-Object { $_.Name -notlike ".*" }

foreach ($account in $accounts) {
    Write-Host "`nAccount: $($account.Name)" -ForegroundColor Green

    # Check for CattosItemTracker.lua
    $luaPath = "$($account.FullName)/SavedVariables/CattosItemTracker.lua"

    if (Test-Path $luaPath) {
        Write-Host "  Found: CattosItemTracker.lua" -ForegroundColor Green

        # Read and parse the file
        $content = Get-Content $luaPath -Raw

        # Find all character entries
        $matches = [regex]::Matches($content, '\["([^"]+)"\]\s*=\s*\{')

        foreach ($match in $matches) {
            $charName = $match.Groups[1].Value
            if ($charName -like "*-*") {
                Write-Host "    Character: $charName" -ForegroundColor Cyan

                # Check if this character has equipment
                if ($content -match "$charName.*?equipment.*?\{([^}]*)\}") {
                    $equipmentBlock = $matches[0].Groups[1].Value
                    if ($equipmentBlock -and $equipmentBlock.Trim().Length -gt 0) {
                        # Count equipment items
                        $itemCount = ([regex]::Matches($equipmentBlock, '=\s*\d+')).Count
                        Write-Host "      Equipment: $itemCount items" -ForegroundColor Green
                    } else {
                        Write-Host "      Equipment: Empty" -ForegroundColor Red
                    }
                } else {
                    Write-Host "      Equipment: None found" -ForegroundColor Red
                }
            }
        }
    } else {
        Write-Host "  No CattosItemTracker.lua found" -ForegroundColor Red
    }
}

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "If characters have equipment above, but the app" -ForegroundColor Yellow
Write-Host "doesn't show it, the auto-refresh is broken." -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Cyan