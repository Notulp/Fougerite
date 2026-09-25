### Guide
Maintenance scripts: `AutoUpdate-Fougerite.ps1` and `Collect-Logs.ps1`

### Description
Fougerite ships two standalone PowerShell scripts at the root of this repository/release to help server
owners with day-to-day maintenance:

- **`AutoUpdate-Fougerite.ps1`** - downloads and installs the latest Fougerite release directly from GitHub.
- **`Collect-Logs.ps1`** - packages all relevant logs/crash dumps into a single zip, ready to attach when
  asking for support.

Both scripts must be placed **directly in the Rust Legacy server's root folder** - the same folder that
contains `rust_server.exe` - and run from there (double-click, or `powershell -File .\Script.ps1` from a
terminal opened in that folder). Both scripts check for `rust_server.exe` on startup and refuse to run
(with a clear error message) if they're not in the right folder.

### 1. `AutoUpdate-Fougerite.ps1`

Automates installing/updating Fougerite to the latest GitHub release, on top of either:
- a plain/original Rust Legacy dedicated server (no Fougerite installed yet), or
- an older existing Fougerite installation.

In both cases you just drop the script into the server root and run it - it figures out what needs to
change and guides you through it interactively.

#### What it does, step by step
1. **Sanity check** - confirms `rust_server.exe` exists in the current folder.
2. **Legacy 32-bit Steam DLL check** - Fougerite now runs the server under 64-bit; if it finds old 32-bit
   shim DLLs (`steam_api.dll`, `steamclient.dll`, `tier0_s.dll`, `vstdlib_s.dll`) it asks `[Y/N]` whether to
   delete them (required for the 64-bit server to start correctly). This step is what makes the script
   usable on **original/vanilla server files** too, not just existing Fougerite installs.
3. **Fetches the latest release** from the GitHub API
   (`https://api.github.com/repos/Notulp/Fougerite/releases/latest`) and locates its `.zip` asset.
4. **Downloads the zip** into the server root.
5. **Asks how to handle `Save\*.cfg` / `*.ini` files** (e.g. `Fougerite.cfg`) - since these carry your
   personal server settings, you get to choose:
   - `[A]` Overwrite ALL (recommended - resets to the latest defaults, re-apply your tweaks afterward),
   - `[S]` Skip ALL (keep every current file untouched),
   - `[P]` Prompt me file-by-file (only asks when the shipped file actually differs from yours; identical
     files are skipped automatically).
6. **Asks how to handle example/sample plugins** shipped with the release (`Save\JsPlugins\PlayerLog\`,
   `Save\JsPlugins\Drop++\`, `Save\PyPlugins\Advertise\`, `Save\LuaPlugins\Test\`):
   - `[S]` Skip ALL (recommended for production servers - keeps your own edits to these folders intact),
   - `[A]` Overwrite ALL (useful the first time, or to grab the latest example code - discards any local
     edits to those specific folders).
7. **Extracts the zip**, applying the two choices above; every other file is always overwritten.
8. **Copies the pre-patched uLink DLLs** from
   `rust_server_Data\Managed\Prepatched_AssemblyDLLwithULink\*.dll` into `rust_server_Data\Managed\`
   (these are required for the server to run correctly with Fougerite's networking patches).
9. **Deletes the downloaded zip** to clean up after itself.

#### Running it on a fresh/original server vs. an older Fougerite server
- **Original server files** (no Fougerite yet): the script will typically find the legacy 32-bit Steam
  DLLs and offer to delete them, then extract a brand-new `Modules\`/`Save\` layout with all default
  config files (since nothing exists yet, the cfg-handling prompt effectively just creates them).
- **Older Fougerite server**: the legacy DLL check is normally a no-op (already removed on a previous
  update), and the important choice becomes the `Save\*.cfg`/`*.ini` prompt - pick `[P]` (Prompt me) the
  first time you update across a large version jump so you can review exactly what changed, or `[A]` if you
  don't mind resetting to defaults and reapplying your own settings from a backup/notes.

In both scenarios the script leads you through the same set of prompts, so there's no need to manually
diff files or guess which DLLs are outdated - just run it and answer the prompts.

### 2. `Collect-Logs.ps1`

Bundles up everything useful for diagnosing a crash or bug report into one timestamped zip
(`ServerLogs_<yyyy-MM-dd_HH-mm-ss>.zip`), created next to the script.

#### What it collects
| Source (if present) | Destination inside the zip |
|---|---|
| Any subfolder of the server root containing **all four** of `crash.dmp`, `error.log`, `output_log.txt`, `report.ini` | `crashdumps\<FolderName>\` |
| `Save\Logs\` (recursively) | `logs\Save\Logs\` |
| `Save\RustBuster2016Server\Logs\` (recursively) | `logs\RustBuster\` |
| `rust_server_Data\output_log.txt` | `logs\output_log.txt` |

Missing sources are simply skipped (with a message telling you they weren't found) - nothing fails just
because, say, you don't run RustBuster. If literally nothing was found to collect, the script exits without
creating an (empty) zip.

#### Usage
1. Place `Collect-Logs.ps1` in the server root (next to `rust_server.exe`).
2. Run it. It stages the collected files in a temporary folder under `%TEMP%`, zips them, then deletes the
   temporary folder.
3. Send the resulting `ServerLogs_*.zip` when reporting an issue - it contains everything needed to
   diagnose most crashes without you having to hunt down individual log files yourself.

### See also
- [`Installation.md`](Installation.md) - where plugins and `Fougerite.cfg` live on disk.
- [`FougeriteCfg.md`](FougeriteCfg.md) - full reference for the config file these scripts touch.
