# ==============================================================================
# AutoUpdate-Fougerite.ps1
# Downloads the latest Fougerite release from GitHub, extracts it to the
# current folder (overwriting all files), and then copies the pre-patched
# uLink DLLs from rust_server_Data\Managed\Prepatched_AssemblyDLLwithULink
# into rust_server_Data\Managed.
# ==============================================================================

# Stop on unhandled terminating errors
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Fougerite Auto-Updater" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# ------------------------------------------------------------------------------
# Step 1 - Determine the script's working directory (where we will extract to)
# ------------------------------------------------------------------------------
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if ([string]::IsNullOrWhiteSpace($scriptDir)) {
    $scriptDir = (Get-Location).Path
}
Write-Host "[INFO] Working directory: $scriptDir" -ForegroundColor White

# ------------------------------------------------------------------------------
# Sanity check - ensure we are in the legacy server root folder
# ------------------------------------------------------------------------------
$serverExe = Join-Path $scriptDir "rust_server.exe"
if (-not (Test-Path $serverExe)) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host "  ERROR: rust_server.exe not found in this folder!" -ForegroundColor Red
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "  This script must be placed in the legacy Rust server root" -ForegroundColor Yellow
    Write-Host "  folder (the same folder that contains rust_server.exe)." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Current folder: $scriptDir" -ForegroundColor White
    Write-Host ""
    Write-Host "  Please move AutoUpdate-Fougerite.ps1 to the server root" -ForegroundColor Yellow
    Write-Host "  folder and run it again from there." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
Write-Host "[INFO] rust_server.exe found - correct server root folder confirmed." -ForegroundColor Green

# ------------------------------------------------------------------------------
# Legacy 32-bit Steam DLL check
# Fougerite now runs the Rust server under 64-bit; the old Steam shim DLLs
# must be removed or the server will fail to start correctly.
# ------------------------------------------------------------------------------
$legacyDlls = @("steam_api.dll", "steamclient.dll", "tier0_s.dll", "vstdlib_s.dll")
$foundLegacyDlls = @()
foreach ($dll in $legacyDlls) {
    $dllPath = Join-Path $scriptDir $dll
    if (Test-Path $dllPath) {
        $foundLegacyDlls += $dll
    }
}

if ($foundLegacyDlls.Count -gt 0) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Yellow
    Write-Host "  IMPORTANT: Legacy 32-bit Steam DLLs detected!" -ForegroundColor Yellow
    Write-Host "============================================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Fougerite is now capable of running the Rust server under" -ForegroundColor White
    Write-Host "  64-bit. The following legacy Steam DLL(s) are present in" -ForegroundColor White
    Write-Host "  this folder and MUST be deleted for the server to work:" -ForegroundColor White
    Write-Host ""
    foreach ($dll in $foundLegacyDlls) {
        Write-Host "    - $dll" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "  These files are no longer needed and keeping them will" -ForegroundColor White
    Write-Host "  cause conflicts with the 64-bit runtime." -ForegroundColor White
    Write-Host ""
    Write-Host "  Can this script delete them now? [Y] Yes / [N] No (exit)" -ForegroundColor Cyan
    Write-Host ""

    $deleteDllChoice = $null
    while ($null -eq $deleteDllChoice) {
        $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        switch ($key.Character.ToString().ToUpper()) {
            "Y" { $deleteDllChoice = $true;  Write-Host "  > Proceeding with deletion..." -ForegroundColor Green }
            "N" { $deleteDllChoice = $false; Write-Host "  > Aborted by user." -ForegroundColor Yellow }
            default { Write-Host "  Please press Y or N." -ForegroundColor Gray }
        }
    }

    if (-not $deleteDllChoice) {
        Write-Host ""
        Write-Host "  Please remove the listed DLL(s) manually and run the" -ForegroundColor Yellow
        Write-Host "  updater again." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Press any key to exit..." -ForegroundColor Gray
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }

    $deleteErrors = 0
    foreach ($dll in $foundLegacyDlls) {
        $dllPath = Join-Path $scriptDir $dll
        try {
            Remove-Item -Path $dllPath -Force
            Write-Host "    [DELETED] $dll" -ForegroundColor Green
        } catch {
            Write-Host "    [ERROR] Could not delete $dll : $_" -ForegroundColor Red
            $deleteErrors++
        }
    }

    if ($deleteErrors -gt 0) {
        Write-Host ""
        Write-Host "[ERROR] $deleteErrors file(s) could not be deleted." -ForegroundColor Red
        Write-Host "        Please delete them manually and run the updater again." -ForegroundColor Red
        Write-Host ""
        Write-Host "Press any key to exit..." -ForegroundColor Gray
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }

    Write-Host "[INFO] All legacy Steam DLLs removed successfully." -ForegroundColor Green
    Write-Host ""
}

# ------------------------------------------------------------------------------
# Step 2 - Fetch the latest release metadata from GitHub API
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Fetching latest release information from GitHub..." -ForegroundColor Yellow

try {
    $apiUrl   = "https://api.github.com/repos/Notulp/Fougerite/releases/latest"
    $headers  = @{ "User-Agent" = "Fougerite-AutoUpdater/1.0" }
    $release  = Invoke-RestMethod -Uri $apiUrl -Headers $headers -UseBasicParsing
} catch {
    Write-Host ""
    Write-Host "[ERROR] Failed to fetch release information from GitHub." -ForegroundColor Red
    Write-Host "        Details: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

$tagName     = $release.tag_name
$releaseName = $release.name
Write-Host "[INFO] Latest release tag  : $tagName" -ForegroundColor Green
Write-Host "[INFO] Latest release name : $releaseName" -ForegroundColor Green

# Find the zip asset
$zipAsset = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1
if ($null -eq $zipAsset) {
    Write-Host ""
    Write-Host "[ERROR] No .zip asset found in the latest release!" -ForegroundColor Red
    Write-Host "        Assets available:" -ForegroundColor Red
    $release.assets | ForEach-Object { Write-Host "          - $($_.name)" -ForegroundColor Red }
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

$downloadUrl = $zipAsset.browser_download_url
$zipFileName = $zipAsset.name
$zipPath     = Join-Path $scriptDir $zipFileName
Write-Host "[INFO] Download URL        : $downloadUrl" -ForegroundColor Green
Write-Host "[INFO] Local zip file      : $zipPath" -ForegroundColor Green

# ------------------------------------------------------------------------------
# Step 3 - Download the zip
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Downloading $zipFileName ..." -ForegroundColor Yellow

try {
    $webClient = New-Object System.Net.WebClient
    $webClient.Headers.Add("User-Agent", "Fougerite-AutoUpdater/1.0")
    $webClient.DownloadFile($downloadUrl, $zipPath)
    $webClient.Dispose()
} catch {
    Write-Host ""
    Write-Host "[ERROR] Download failed." -ForegroundColor Red
    Write-Host "        Details: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

if (-not (Test-Path $zipPath)) {
    Write-Host ""
    Write-Host "[ERROR] Zip file not found after download: $zipPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

$zipSize = (Get-Item $zipPath).Length
Write-Host "[INFO] Download complete. File size: $([math]::Round($zipSize / 1MB, 2)) MB" -ForegroundColor Green

# ------------------------------------------------------------------------------
# Step 4 - Extract the zip to the working directory (overwrite all files)
#           Config/ini files inside Save\ are handled with a user prompt.
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Extracting $zipFileName to $scriptDir ..." -ForegroundColor Yellow
Write-Host "[INFO] All existing files will be overwritten (except Save cfg/ini - see below)." -ForegroundColor Yellow

# -- Ask the user upfront how to handle Save\*.cfg / *.ini files --
Write-Host ""
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
Write-Host "  Configuration files in the Save folder" -ForegroundColor Magenta
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
Write-Host "  The release contains .cfg and .ini files inside the Save\" -ForegroundColor White
Write-Host "  folder. These files may carry new default settings." -ForegroundColor White
Write-Host ""
Write-Host "  How would you like to handle them?" -ForegroundColor White
Write-Host ""
Write-Host "  [A] Overwrite ALL  (RECOMMENDED - update to latest defaults," -ForegroundColor Green
Write-Host "                      then tweak manually afterwards)" -ForegroundColor Green
Write-Host "  [S] Skip ALL       (keep your current files untouched)" -ForegroundColor Yellow
Write-Host "  [P] Prompt me      (decide file-by-file)" -ForegroundColor Cyan
Write-Host ""

$globalCfgChoice = $null
while ($null -eq $globalCfgChoice) {
    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    switch ($key.Character.ToString().ToUpper()) {
        "A" { $globalCfgChoice = "OVERWRITE_ALL"; Write-Host "  > You chose: Overwrite ALL config files (recommended)." -ForegroundColor Green }
        "S" { $globalCfgChoice = "SKIP_ALL";      Write-Host "  > You chose: Skip ALL config files." -ForegroundColor Yellow }
        "P" { $globalCfgChoice = "PROMPT";        Write-Host "  > You chose: Prompt me for each file." -ForegroundColor Cyan }
        default { Write-Host "  Please press A, S, or P." -ForegroundColor Gray }
    }
}
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
Write-Host ""

# -- Ask the user upfront how to handle example plugin folders --
Write-Host ""
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
Write-Host "  Example plugin folders" -ForegroundColor Magenta
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
Write-Host "  The release ships with three example/sample plugins:" -ForegroundColor White
Write-Host "    - Save\JsPlugins\PlayerLog\" -ForegroundColor White
Write-Host "    - Save\Magma\Drop++\" -ForegroundColor White
Write-Host "    - Save\PyPlugins\Advertise\" -ForegroundColor White
Write-Host ""
Write-Host "  If you already customised these plugins, overwriting them" -ForegroundColor White
Write-Host "  will discard your changes." -ForegroundColor White
Write-Host ""
Write-Host "  [A] Overwrite ALL  (RECOMMENDED - get the latest example code," -ForegroundColor Green
Write-Host "                      but you will lose any edits you made)" -ForegroundColor Green
Write-Host "  [S] Skip ALL       (keep your current plugin files untouched)" -ForegroundColor Yellow
Write-Host ""

$globalPluginChoice = $null
while ($null -eq $globalPluginChoice) {
    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    switch ($key.Character.ToString().ToUpper()) {
        "A" { $globalPluginChoice = "OVERWRITE_ALL"; Write-Host "  > You chose: Overwrite example plugin folders (recommended)." -ForegroundColor Green }
        "S" { $globalPluginChoice = "SKIP_ALL";      Write-Host "  > You chose: Skip example plugin folders." -ForegroundColor Yellow }
        default { Write-Host "  Please press A or S." -ForegroundColor Gray }
    }
}
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
Write-Host ""

# Helper: read a single-key Yes/No answer
function Read-YesNo {
    param([string]$Prompt)
    Write-Host $Prompt -ForegroundColor White -NoNewline
    $ans = $null
    while ($null -eq $ans) {
        $k = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        switch ($k.Character.ToString().ToUpper()) {
            "Y" { $ans = $true;  Write-Host " Y" -ForegroundColor Green }
            "N" { $ans = $false; Write-Host " N" -ForegroundColor Yellow }
            default { Write-Host "." -NoNewline }
        }
    }
    return $ans
}

try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    $totalEntries = $zip.Entries.Count
    $current      = 0
    $skippedCfg   = 0

    foreach ($entry in $zip.Entries) {
        $current++
        $destPath = Join-Path $scriptDir $entry.FullName

        # -- Directory entries --
        if ($entry.FullName.EndsWith("/") -or $entry.FullName.EndsWith("\")) {
            if (-not (Test-Path $destPath)) {
                New-Item -ItemType Directory -Path $destPath -Force | Out-Null
            }
            continue
        }

        # Ensure parent directory exists
        $destDir = Split-Path -Parent $destPath
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }

        # -- Check if this file belongs to an example plugin folder --
        $normalised = $entry.FullName.Replace("\", "/")
        $ext        = [System.IO.Path]::GetExtension($entry.Name).ToLower()
        $isExamplePlugin = ($normalised -match "(?i)^Save/JsPlugins/PlayerLog/") -or `
                           ($normalised -match "(?i)^Save/Magma/Drop\+\+/") -or `
                           ($normalised -match "(?i)^Save/PyPlugins/Advertise/")

        if ($isExamplePlugin) {
            if ($globalPluginChoice -eq "SKIP_ALL") {
                $skippedCfg++
                Write-Host "    [PLUGIN] Skipped  : $($entry.FullName)" -ForegroundColor Yellow
                continue
            } else {
                # OVERWRITE_ALL - fall through to normal extraction below
                Write-Host "    [PLUGIN] Overwriting: $($entry.FullName)" -ForegroundColor Gray
            }
        }

        # -- Check if this is a Save-folder config/ini file --
        $isSaveCfg  = ($normalised -match "(?i)^Save/") -and ($ext -eq ".cfg" -or $ext -eq ".ini")

        if ($isSaveCfg) {
            $fileExists = Test-Path $destPath
            $bytes      = $null   # reset per-iteration

            # Determine if content actually differs (when file already exists)
            $isDifferent = $false
            if ($fileExists) {
                $existingHash = (Get-FileHash -Path $destPath -Algorithm MD5).Hash
                # Read zip entry bytes to hash them
                $stream  = $entry.Open()
                $ms      = New-Object System.IO.MemoryStream
                $stream.CopyTo($ms)
                $stream.Dispose()
                $bytes   = $ms.ToArray()
                $ms.Dispose()
                $md5     = [System.Security.Cryptography.MD5]::Create()
                $zipHash = [BitConverter]::ToString($md5.ComputeHash($bytes)) -replace "-", ""
                $isDifferent = ($existingHash -ne $zipHash)
                $diffLabel   = if ($isDifferent) { "(DIFFERENT from your current file)" } else { "(same as current file)" }
            } else {
                $diffLabel = "(new file - does not exist yet)"
                $isDifferent = $true
            }

            $shouldWrite = $false
            switch ($globalCfgChoice) {
                "OVERWRITE_ALL" { $shouldWrite = $true }
                "SKIP_ALL"      { $shouldWrite = $false }
                "PROMPT" {
                    Write-Host ""
                    Write-Host "  [CFG] $($entry.FullName)" -ForegroundColor Cyan
                    Write-Host "        $diffLabel" -ForegroundColor $(if ($isDifferent) { "Yellow" } else { "Gray" })
                    if (-not $isDifferent) {
                        Write-Host "        File is identical - skipping automatically." -ForegroundColor Gray
                        $shouldWrite = $false
                    } else {
                        $shouldWrite = Read-YesNo "        Overwrite this file? [Y/N] "
                    }
                }
            }

            if ($shouldWrite) {
                # Write the already-read bytes (PROMPT path) or stream directly
                if ($globalCfgChoice -eq "PROMPT" -and $isDifferent -and $null -ne $bytes) {
                    [System.IO.File]::WriteAllBytes($destPath, $bytes)
                } else {
                    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destPath, $true)
                }
                Write-Host "    [CFG] Overwrote: $($entry.FullName)" -ForegroundColor Green
            } else {
                $skippedCfg++
                Write-Host "    [CFG] Skipped  : $($entry.FullName)" -ForegroundColor Yellow
            }
            continue
        }

        # -- Normal file - always overwrite --
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destPath, $true)

        if ($current % 20 -eq 0 -or $current -eq $totalEntries) {
            Write-Host "    Extracted $current / $totalEntries files..." -ForegroundColor Gray
        }
    }

    $zip.Dispose()
} catch {
    Write-Host ""
    Write-Host "[ERROR] Extraction failed." -ForegroundColor Red
    Write-Host "        Details: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

Write-Host "[INFO] Extraction complete. $totalEntries entries processed" `
          "($skippedCfg cfg/ini file(s) skipped)." -ForegroundColor Green

# ------------------------------------------------------------------------------
# Step 5 - Copy pre-patched uLink DLLs into the Managed folder
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Copying pre-patched uLink DLLs..." -ForegroundColor Yellow

$managedDir    = Join-Path $scriptDir "rust_server_Data\Managed"
$prepatchedDir = Join-Path $managedDir "Prepatched_AssemblyDLLwithULink"

if (-not (Test-Path $prepatchedDir)) {
    Write-Host ""
    Write-Host "[ERROR] Prepatched DLL folder not found: $prepatchedDir" -ForegroundColor Red
    Write-Host "        The zip may not have extracted correctly, or the folder structure has changed." -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

if (-not (Test-Path $managedDir)) {
    Write-Host ""
    Write-Host "[ERROR] Managed folder not found: $managedDir" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

$dllFiles = Get-ChildItem -Path $prepatchedDir -Filter "*.dll"
if ($dllFiles.Count -eq 0) {
    Write-Host ""
    Write-Host "[WARNING] No .dll files found in: $prepatchedDir" -ForegroundColor DarkYellow
} else {
    foreach ($dll in $dllFiles) {
        $dest = Join-Path $managedDir $dll.Name
        Write-Host "    Copying $($dll.Name) -> Managed\" -ForegroundColor Gray
        Copy-Item -Path $dll.FullName -Destination $dest -Force
    }
    Write-Host "[INFO] Copied $($dllFiles.Count) DLL(s) from Prepatched_AssemblyDLLwithULink to Managed." -ForegroundColor Green
}

# ------------------------------------------------------------------------------
# Step 6 - Cleanup: remove the downloaded zip
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Cleaning up downloaded zip file..." -ForegroundColor Yellow
try {
    Remove-Item -Path $zipPath -Force
    Write-Host "[INFO] Removed: $zipFileName" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Could not remove zip file: $_" -ForegroundColor DarkYellow
}

# ------------------------------------------------------------------------------
# Done
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Update complete! Fougerite $tagName is ready." -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
