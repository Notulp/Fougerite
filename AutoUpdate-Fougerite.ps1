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

function Get-RunningServer {
    $result = @()
    foreach ($p in @(Get-Process -Name "rust_server" -ErrorAction SilentlyContinue)) {
        $procPath = $null
        try { $procPath = $p.MainModule.FileName } catch { $procPath = $null }
        if ([string]::IsNullOrEmpty($procPath) -or $procPath -eq $serverExe) {
            $result += [pscustomobject]@{ Id = $p.Id; Path = $procPath }
        }
    }
    return ,$result
}

$runningServers = Get-RunningServer
while ($runningServers.Count -gt 0) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host "  ERROR: The Rust server is still running!" -ForegroundColor Red
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host ""
    foreach ($s in $runningServers) {
        $procLabel = if ([string]::IsNullOrEmpty($s.Path)) { "(path not readable - run as administrator to verify)" } else { $s.Path }
        Write-Host "    - PID $($s.Id)  $procLabel" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "  Files in use cannot be replaced, and updating a running" -ForegroundColor White
    Write-Host "  server would leave it half-updated. Stop the server first." -ForegroundColor White
    Write-Host ""
    Write-Host "  [R] Retry  (after stopping the server)" -ForegroundColor Cyan
    Write-Host "  [Q] Quit" -ForegroundColor Yellow
    Write-Host ""

    $runningChoice = $null
    while ($null -eq $runningChoice) {
        $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        switch ($key.Character.ToString().ToUpper()) {
            "R" { $runningChoice = "RETRY"; Write-Host "  > Checking again..." -ForegroundColor Cyan }
            "Q" { $runningChoice = "QUIT";  Write-Host "  > Aborted by user." -ForegroundColor Yellow }
            default { Write-Host "  Please press R or Q." -ForegroundColor Gray }
        }
    }

    if ($runningChoice -eq "QUIT") {
        exit 1
    }
    $runningServers = Get-RunningServer
}
Write-Host "[INFO] No running server instance detected for this folder." -ForegroundColor Green

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

Write-Host ""
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
Write-Host "  Backup before update" -ForegroundColor Magenta
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
Write-Host "  The update will overwrite files in this server folder." -ForegroundColor White
Write-Host "  A backup lets you roll back if something goes wrong." -ForegroundColor White
Write-Host ""
Write-Host "  [Y] Yes  (RECOMMENDED - zip the whole server folder into Backups\)" -ForegroundColor Green
Write-Host "  [N] No   (continue without a backup)" -ForegroundColor Yellow
Write-Host ""

$backupChoice = $null
while ($null -eq $backupChoice) {
    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    switch ($key.Character.ToString().ToUpper()) {
        "Y" { $backupChoice = $true;  Write-Host "  > You chose: Create a backup (recommended)." -ForegroundColor Green }
        "N" { $backupChoice = $false; Write-Host "  > You chose: No backup." -ForegroundColor Yellow }
        default { Write-Host "  Please press Y or N." -ForegroundColor Gray }
    }
}
Write-Host "------------------------------------------------------------" -ForegroundColor Magenta

if ($backupChoice) {
    $backupDir       = Join-Path $scriptDir "Backups"
    $backupPrefix    = $backupDir.TrimEnd('\') + '\'
    $backupTimestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
    $backupPath      = Join-Path $backupDir "FougeriteBackup_$backupTimestamp.zip"
    $backupActivity  = "Backing up server folder"

    Write-Host ""
    Write-Host "[INFO] Scanning server folder for backup..." -ForegroundColor Yellow

    try {
        $backupFiles = @(Get-ChildItem -Path $scriptDir -File -Recurse -Force | Where-Object {
            -not $_.FullName.StartsWith($backupPrefix, [System.StringComparison]::OrdinalIgnoreCase) -and
                    $_.FullName -ne $zipPath
        })
        $backupTotalBytes = [double](($backupFiles | Measure-Object -Property Length -Sum).Sum)

        Write-Host "[INFO] $($backupFiles.Count) file(s), $([math]::Round($backupTotalBytes / 1MB, 2)) MB to back up." -ForegroundColor White
        Write-Host "[INFO] Creating backup $backupPath ..." -ForegroundColor Yellow

        if (-not (Test-Path $backupDir)) {
            New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
        }
        Add-Type -AssemblyName System.IO.Compression
        Add-Type -AssemblyName System.IO.Compression.FileSystem

        $buffer          = New-Object byte[] 1MB
        $backupDoneBytes = 0
        $backupFileCount = 0
        $lastPercent     = -1
        $backupZip = [System.IO.Compression.ZipFile]::Open($backupPath, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($file in $backupFiles) {
                $entryName = $file.FullName.Substring($scriptDir.Length).TrimStart('\', '/').Replace('\', '/')
                $zipEntry  = $backupZip.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Fastest)
                try { $zipEntry.LastWriteTime = $file.LastWriteTime } catch { }

                $inStream  = New-Object System.IO.FileStream -ArgumentList $file.FullName, ([System.IO.FileMode]::Open), ([System.IO.FileAccess]::Read), ([System.IO.FileShare]::ReadWrite)
                $outStream = $zipEntry.Open()
                try {
                    while (($read = $inStream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                        $outStream.Write($buffer, 0, $read)
                        $backupDoneBytes += $read
                        $percent = if ($backupTotalBytes -gt 0) { [int][math]::Floor($backupDoneBytes * 100 / $backupTotalBytes) } else { 100 }
                        if ($percent -ne $lastPercent) {
                            $status = "$percent% - $([math]::Round($backupDoneBytes / 1MB, 1)) / $([math]::Round($backupTotalBytes / 1MB, 1)) MB - file $($backupFileCount + 1) of $($backupFiles.Count)"
                            Write-Progress -Activity $backupActivity -Status $status -PercentComplete $percent -CurrentOperation $entryName
                            $lastPercent = $percent
                        }
                    }
                } finally {
                    $outStream.Dispose()
                    $inStream.Dispose()
                }
                $backupFileCount++
            }
        } finally {
            $backupZip.Dispose()
            Write-Progress -Activity $backupActivity -Completed
        }
    } catch {
        Write-Progress -Activity $backupActivity -Completed
        Write-Host ""
        Write-Host "[ERROR] Backup failed. The update was NOT applied." -ForegroundColor Red
        Write-Host "        Details: $_" -ForegroundColor Red
        Write-Host ""
        Remove-Item -Path $backupPath -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
        Write-Host "Press any key to exit..." -ForegroundColor Gray
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }

    $backupSize = (Get-Item $backupPath).Length
    Write-Host "[INFO] Backup complete. $backupFileCount file(s), $([math]::Round($backupSize / 1MB, 2)) MB" -ForegroundColor Green
}

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
Write-Host "  [P] Prompt me      (decide file-by-file, with a diff of each change)" -ForegroundColor Cyan
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
Write-Host "  The release ships with example/sample plugins:" -ForegroundColor White
Write-Host "    - Save\JsPlugins\PlayerLog\" -ForegroundColor White
Write-Host "    - Save\JsPlugins\Drop++\" -ForegroundColor White
Write-Host "    - Save\PyPlugins\Advertise\" -ForegroundColor White
Write-Host "    - Save\LuaPlugins\Test\" -ForegroundColor White
Write-Host "    - Modules\HelloScriptTest\" -ForegroundColor White
Write-Host ""
Write-Host "  These are useful for learning the basics but are NOT needed" -ForegroundColor White
Write-Host "  on a normal production server. It is recommended to SKIP them" -ForegroundColor White
Write-Host "  unless you are setting up for the first time or want the latest" -ForegroundColor White
Write-Host "  example code. Overwriting will discard any edits you made." -ForegroundColor White
Write-Host ""
Write-Host "  [S] Skip ALL       (RECOMMENDED for production - keep your" -ForegroundColor Green
Write-Host "                      current plugin files untouched)" -ForegroundColor Green
Write-Host "  [A] Overwrite ALL  (useful for first-time setup / learning;" -ForegroundColor Yellow
Write-Host "                      you will lose any edits you made)" -ForegroundColor Yellow
Write-Host ""

$globalPluginChoice = $null
while ($null -eq $globalPluginChoice) {
    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    switch ($key.Character.ToString().ToUpper()) {
        "S" { $globalPluginChoice = "SKIP_ALL";      Write-Host "  > You chose: Skip example plugin folders (recommended for production)." -ForegroundColor Green }
        "A" { $globalPluginChoice = "OVERWRITE_ALL"; Write-Host "  > You chose: Overwrite example plugin folders." -ForegroundColor Yellow }
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

function Get-TextLines {
    param([byte[]]$Bytes)
    $lines  = New-Object System.Collections.Generic.List[string]
    $ms     = New-Object System.IO.MemoryStream -ArgumentList (,$Bytes)
    $reader = New-Object System.IO.StreamReader -ArgumentList $ms, ([System.Text.Encoding]::UTF8), $true
    while ($null -ne ($line = $reader.ReadLine())) {
        $lines.Add($line)
    }
    $reader.Dispose()
    return ,$lines.ToArray()
}

function Show-ConfigDiff {
    param(
        [byte[]]$OldBytes,
        [byte[]]$NewBytes,
        [string]$OldLabel,
        [string]$NewLabel,
        [int]$Context = 3
    )
    $old = @(Get-TextLines -Bytes $OldBytes)
    $new = @(Get-TextLines -Bytes $NewBytes)

    $pre = 0
    while ($pre -lt $old.Count -and $pre -lt $new.Count -and $old[$pre] -ceq $new[$pre]) {
        $pre++
    }
    $suf = 0
    while ($suf -lt ($old.Count - $pre) -and $suf -lt ($new.Count - $pre) -and
            $old[$old.Count - 1 - $suf] -ceq $new[$new.Count - 1 - $suf]) {
        $suf++
    }

    $n  = $old.Count - $pre - $suf
    $m  = $new.Count - $pre - $suf
    $dp = New-Object 'int[,]' ($n + 1), ($m + 1)
    for ($i = $n - 1; $i -ge 0; $i--) {
        for ($j = $m - 1; $j -ge 0; $j--) {
            if ($old[$pre + $i] -ceq $new[$pre + $j]) {
                $dp[$i, $j] = $dp[($i + 1), ($j + 1)] + 1
            } elseif ($dp[($i + 1), $j] -ge $dp[$i, ($j + 1)]) {
                $dp[$i, $j] = $dp[($i + 1), $j]
            } else {
                $dp[$i, $j] = $dp[$i, ($j + 1)]
            }
        }
    }

    $ops = New-Object System.Collections.Generic.List[object]
    for ($k = 0; $k -lt $pre; $k++) {
        $ops.Add([pscustomobject]@{ Kind = ' '; OldNo = $k + 1; NewNo = $k + 1; Text = $old[$k] })
    }
    $i = 0
    $j = 0
    while ($i -lt $n -or $j -lt $m) {
        if ($i -lt $n -and $j -lt $m -and $old[$pre + $i] -ceq $new[$pre + $j]) {
            $ops.Add([pscustomobject]@{ Kind = ' '; OldNo = $pre + $i + 1; NewNo = $pre + $j + 1; Text = $old[$pre + $i] })
            $i++
            $j++
        } elseif ($j -lt $m -and ($i -ge $n -or $dp[$i, ($j + 1)] -gt $dp[($i + 1), $j])) {
            $ops.Add([pscustomobject]@{ Kind = '+'; OldNo = ''; NewNo = $pre + $j + 1; Text = $new[$pre + $j] })
            $j++
        } else {
            $ops.Add([pscustomobject]@{ Kind = '-'; OldNo = $pre + $i + 1; NewNo = ''; Text = $old[$pre + $i] })
            $i++
        }
    }
    for ($k = 0; $k -lt $suf; $k++) {
        $oi = $old.Count - $suf + $k
        $ni = $new.Count - $suf + $k
        $ops.Add([pscustomobject]@{ Kind = ' '; OldNo = $oi + 1; NewNo = $ni + 1; Text = $old[$oi] })
    }

    $changes = @($ops | Where-Object { $_.Kind -ne ' ' }).Count
    if ($changes -eq 0) {
        return 0
    }

    $show = New-Object 'bool[]' $ops.Count
    for ($k = 0; $k -lt $ops.Count; $k++) {
        if ($ops[$k].Kind -ne ' ') {
            $from = [math]::Max(0, $k - $Context)
            $to   = [math]::Min($ops.Count - 1, $k + $Context)
            for ($x = $from; $x -le $to; $x++) { $show[$x] = $true }
        }
    }

    Write-Host "        --- $OldLabel" -ForegroundColor Red
    Write-Host "        +++ $NewLabel" -ForegroundColor Green
    $gap = $false
    for ($k = 0; $k -lt $ops.Count; $k++) {
        if (-not $show[$k]) {
            if (-not $gap) {
                Write-Host "        ..." -ForegroundColor DarkGray
                $gap = $true
            }
            continue
        }
        $gap  = $false
        $op   = $ops[$k]
        $line = "        {0,5} {1,5} {2} {3}" -f $op.OldNo, $op.NewNo, $op.Kind, $op.Text
        switch ($op.Kind) {
            '-'     { Write-Host $line -ForegroundColor Red }
            '+'     { Write-Host $line -ForegroundColor Green }
            default { Write-Host $line -ForegroundColor DarkGray }
        }
    }
    Write-Host "        $changes line(s) changed." -ForegroundColor Yellow
    return $changes
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

        # -- Compute normalised path and example-plugin flag up front --
        $normalised = $entry.FullName.Replace("\", "/")
        $isExamplePlugin = ($normalised -match "(?i)(^|/)Save/JsPlugins/PlayerLog/") -or `
                           ($normalised -match "(?i)(^|/)Save/JsPlugins/Drop\+\+/") -or `
                           ($normalised -match "(?i)(^|/)Save/PyPlugins/Advertise/") -or `
                           ($normalised -match "(?i)(^|/)Save/LuaPlugins/Test/") -or `
                           ($normalised -match "(?i)(^|/)Modules/HelloScriptTest/")

        # -- Directory entries --
        if ($entry.FullName.EndsWith("/") -or $entry.FullName.EndsWith("\")) {
            # Skip creating plugin directories when the user chose to skip plugins
            if ($isExamplePlugin -and $globalPluginChoice -eq "SKIP_ALL") {
                continue
            }
            if (-not (Test-Path $destPath)) {
                New-Item -ItemType Directory -Path $destPath -Force | Out-Null
            }
            continue
        }

        # -- Check if this file belongs to an example plugin folder --
        $ext = [System.IO.Path]::GetExtension($entry.Name).ToLower()
        # ($isExamplePlugin already computed above for this entry)

        if ($isExamplePlugin) {
            if ($globalPluginChoice -eq "SKIP_ALL") {
                $skippedCfg++
                Write-Host "    [PLUGIN] Skipped  : $($entry.FullName)" -ForegroundColor Yellow
                continue   # <-- skip BEFORE the parent-dir is created
            } else {
                # OVERWRITE_ALL - fall through to normal extraction below
                Write-Host "    [PLUGIN] Overwriting: $($entry.FullName)" -ForegroundColor Gray
            }
        }

        # Ensure parent directory exists (only reached for files we will actually extract)
        $destDir = Split-Path -Parent $destPath
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }

        # -- Check if this is a Save-folder config/ini file --
        $isSaveCfg  = ($normalised -match "(?i)(^|/)Save/") -and ($ext -eq ".cfg" -or $ext -eq ".ini")

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
                        if ($fileExists) {
                            $changedLines = Show-ConfigDiff -OldBytes ([System.IO.File]::ReadAllBytes($destPath)) `
                                                            -NewBytes $bytes `
                                                            -OldLabel "current : $destPath" `
                                                            -NewLabel "release : $($entry.FullName)"
                            if ($changedLines -eq 0) {
                                Write-Host "        Text is identical - only line endings or encoding differ." -ForegroundColor Gray
                            }
                        }
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
