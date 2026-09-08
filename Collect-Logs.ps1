# ==============================================================================
# Collect-Logs.ps1
# Collects all relevant log files and crash dumps from the Rust legacy server
# folder and packages them into a single timestamped zip archive.
#
# Layout inside the zip:
#   crashdumps\          - folders containing crash.dmp + error.log +
#                          output_log.txt + report.ini (all four required)
#   logs\Save\Logs\      - all files from Save\Logs\              (if present)
#   logs\RustBuster\     - all files from Save\RustBuster2016Server\Logs\
#                          (if present)
#   logs\               - output_log.txt from rust_server_Data\   (if present)
# ==============================================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Fougerite Log Collector" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# ------------------------------------------------------------------------------
# Step 1 - Determine the script's working directory
# ------------------------------------------------------------------------------
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if ([string]::IsNullOrWhiteSpace($scriptDir)) {
    $scriptDir = (Get-Location).Path
}
Write-Host "[INFO] Working directory: $scriptDir" -ForegroundColor White

# ------------------------------------------------------------------------------
# Sanity check - ensure we are in the correct server root folder
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
    Write-Host "  Please move Collect-Logs.ps1 to the server root folder" -ForegroundColor Yellow
    Write-Host "  and run it again from there." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
Write-Host "[INFO] rust_server.exe found - correct server root folder confirmed." -ForegroundColor Green

# ------------------------------------------------------------------------------
# Step 2 - Prepare a temp staging folder and the output zip path
# ------------------------------------------------------------------------------
$timestamp  = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$zipName    = "ServerLogs_$timestamp.zip"
$zipPath    = Join-Path $scriptDir $zipName
$stagingDir = Join-Path $env:TEMP "FougeriteLogCollector_$timestamp"

Write-Host ""
Write-Host "[INFO] Output archive : $zipPath" -ForegroundColor White
Write-Host "[INFO] Staging folder : $stagingDir" -ForegroundColor White

if (Test-Path $stagingDir) {
    Remove-Item -Recurse -Force $stagingDir
}
New-Item -ItemType Directory -Path $stagingDir | Out-Null

$totalFilesCopied = 0

# ------------------------------------------------------------------------------
# Helper - copy a single file into a destination inside the staging folder,
#           creating subdirectories as needed.
# ------------------------------------------------------------------------------
function Stage-File {
    param(
        [string]$SourceFile,
        [string]$DestRelativePath   # relative path inside the staging folder
    )
    $dest = Join-Path $stagingDir $DestRelativePath
    $destDir = Split-Path -Parent $dest
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    Copy-Item -Path $SourceFile -Destination $dest -Force
    $script:totalFilesCopied++
    Write-Host "    [STAGED] $DestRelativePath" -ForegroundColor DarkGray
}

# ------------------------------------------------------------------------------
# Step 3 - Collect crash dumps
#           A crash dump is any subfolder of the script directory that contains
#           ALL FOUR of: crash.dmp, error.log, output_log.txt, report.ini
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Scanning for crash dump folders..." -ForegroundColor Yellow

$crashDumpFiles  = @("crash.dmp", "error.log", "output_log.txt", "report.ini")
$crashDumpsFound = 0

Get-ChildItem -Path $scriptDir -Directory | ForEach-Object {
    $folder = $_
    $allPresent = $true
    foreach ($f in $crashDumpFiles) {
        if (-not (Test-Path (Join-Path $folder.FullName $f))) {
            $allPresent = $false
            break
        }
    }

    if ($allPresent) {
        $crashDumpsFound++
        Write-Host "    [CRASH DUMP] $($folder.Name)" -ForegroundColor Magenta
        foreach ($f in $crashDumpFiles) {
            Stage-File -SourceFile (Join-Path $folder.FullName $f) `
                       -DestRelativePath "crashdumps\$($folder.Name)\$f"
        }
    }
}

if ($crashDumpsFound -eq 0) {
    Write-Host "    (no crash dump folders found)" -ForegroundColor DarkGray
} else {
    Write-Host "[INFO] $crashDumpsFound crash dump folder(s) staged." -ForegroundColor Green
}

# ------------------------------------------------------------------------------
# Step 4 - Collect Save\Logs\
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Scanning Save\Logs\ ..." -ForegroundColor Yellow

$saveLogsDir = Join-Path $scriptDir "Save\Logs"
if (Test-Path $saveLogsDir) {
    $saveLogFiles = Get-ChildItem -Path $saveLogsDir -File -Recurse
    if ($saveLogFiles.Count -eq 0) {
        Write-Host "    (folder exists but contains no files)" -ForegroundColor DarkGray
    } else {
        foreach ($file in $saveLogFiles) {
            $relative = $file.FullName.Substring($saveLogsDir.Length).TrimStart('\', '/')
            Stage-File -SourceFile $file.FullName `
                       -DestRelativePath "logs\Save\Logs\$relative"
        }
        Write-Host "[INFO] $($saveLogFiles.Count) file(s) staged from Save\Logs\." -ForegroundColor Green
    }
} else {
    Write-Host "    (Save\Logs\ does not exist - skipping)" -ForegroundColor DarkGray
}

# ------------------------------------------------------------------------------
# Step 5 - Collect Save\RustBuster2016Server\Logs\
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Scanning Save\RustBuster2016Server\Logs\ ..." -ForegroundColor Yellow

$rbLogsDir = Join-Path $scriptDir "Save\RustBuster2016Server\Logs"
if (Test-Path $rbLogsDir) {
    $rbLogFiles = Get-ChildItem -Path $rbLogsDir -File -Recurse
    if ($rbLogFiles.Count -eq 0) {
        Write-Host "    (folder exists but contains no files)" -ForegroundColor DarkGray
    } else {
        foreach ($file in $rbLogFiles) {
            $relative = $file.FullName.Substring($rbLogsDir.Length).TrimStart('\', '/')
            Stage-File -SourceFile $file.FullName `
                       -DestRelativePath "logs\RustBuster\$relative"
        }
        Write-Host "[INFO] $($rbLogFiles.Count) file(s) staged from Save\RustBuster2016Server\Logs\." -ForegroundColor Green
    }
} else {
    Write-Host "    (Save\RustBuster2016Server\Logs\ does not exist - skipping)" -ForegroundColor DarkGray
}

# ------------------------------------------------------------------------------
# Step 6 - Collect rust_server_Data\output_log.txt
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Scanning rust_server_Data\output_log.txt ..." -ForegroundColor Yellow

$dataOutputLog = Join-Path $scriptDir "rust_server_Data\output_log.txt"
if (Test-Path $dataOutputLog) {
    Stage-File -SourceFile $dataOutputLog `
               -DestRelativePath "logs\output_log.txt"
    Write-Host "[INFO] output_log.txt from rust_server_Data\ staged." -ForegroundColor Green
} else {
    Write-Host "    (rust_server_Data\output_log.txt does not exist - skipping)" -ForegroundColor DarkGray
}

# ------------------------------------------------------------------------------
# Step 7 - Abort early if nothing was collected
# ------------------------------------------------------------------------------
Write-Host ""
if ($totalFilesCopied -eq 0) {
    Write-Host "============================================================" -ForegroundColor Yellow
    Write-Host "  WARNING: No log files were found. Nothing to archive." -ForegroundColor Yellow
    Write-Host "============================================================" -ForegroundColor Yellow
    Write-Host ""
    Remove-Item -Recurse -Force $stagingDir -ErrorAction SilentlyContinue
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}

Write-Host "[INFO] Total files staged: $totalFilesCopied" -ForegroundColor Cyan

# ------------------------------------------------------------------------------
# Step 8 - Create the zip archive from the staging folder
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host "[INFO] Creating archive $zipName ..." -ForegroundColor Yellow

try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stagingDir, $zipPath)
} catch {
    Write-Host ""
    Write-Host "[ERROR] Failed to create the zip archive." -ForegroundColor Red
    Write-Host "        Details: $_" -ForegroundColor Red
    Write-Host ""
    Remove-Item -Recurse -Force $stagingDir -ErrorAction SilentlyContinue
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

# ------------------------------------------------------------------------------
# Step 9 - Clean up staging folder
# ------------------------------------------------------------------------------
Remove-Item -Recurse -Force $stagingDir -ErrorAction SilentlyContinue

# ------------------------------------------------------------------------------
# Done
# ------------------------------------------------------------------------------
$zipSize = [math]::Round((Get-Item $zipPath).Length / 1KB, 1)

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Log collection complete!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Archive : $zipPath" -ForegroundColor White
Write-Host "  Size    : $zipSize KB" -ForegroundColor White
Write-Host "  Files   : $totalFilesCopied" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
exit 0
