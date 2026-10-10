# smoke-test.ps1 — in-game repack smoke test for SporeMaster 3
#
# What it does:
#   1. Backs up an original game archive (Data\<Package>).
#   2. Repacks that archive losslessly with SporeMaster (spore roundtrip) and puts it back.
#   3. Launches the game so you can reach the main menu / play a little.
#   4. Checks for NEW crash artifacts (the game writes a "<...> exception.txt" report and a
#      "<...> minidump.mdmp" file when it fails - that is our "broken repack" criterion).
#   5. Restores the original archive and reports PASS/FAIL.
#
# The original archive is ALWAYS restored at the end (also on Ctrl+C / error), so the game stays intact.
#
# Usage (from the repository root):
#   powershell -ExecutionPolicy Bypass -File tools\smoke-test.ps1
#   powershell -ExecutionPolicy Bypass -File tools\smoke-test.ps1 -GameFolder refs\SporeGames\SporeRetail -Package Spore_Game.package
#   powershell -ExecutionPolicy Bypass -File tools\smoke-test.ps1 -NoLaunch   # repack + restore only
#   powershell -ExecutionPolicy Bypass -File tools\smoke-test.ps1 -Auto -WaitSeconds 60   # unattended
#
# Launch arguments: "-w" (windowed) and "-devDirs" (portable: create UserData next to the game and
# use the game's own Data folders). Override with -LaunchArgs if needed.
#
param(
    [string]$GameFolder = "refs\SporeGames\SporeRetail",
    [string]$Package = "Spore_Game.package",
    [string]$Exe = "SporeApp.exe",
    [string]$LaunchArgs = "-w -devDirs",
    [switch]$NoLaunch,
    [switch]$Big,
    [switch]$NoCompress,
    [switch]$Auto,
    [int]$WaitSeconds = 60
)

$ErrorActionPreference = "Stop"

function Write-Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }
function Write-Ok($text)   { Write-Host "OK  $text" -ForegroundColor Green }
function Write-Bad($text)  { Write-Host "!!  $text" -ForegroundColor Red }

# --- resolve paths (repo root = parent of this script's folder) ---
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$gameDir = if ([System.IO.Path]::IsPathRooted($GameFolder)) { $GameFolder } else { Join-Path $repoRoot $GameFolder }
$archive = Join-Path $gameDir (Join-Path "Data" $Package)

if (-not (Test-Path $gameDir))  { Write-Bad "Game folder not found: $gameDir"; exit 2 }
if (-not (Test-Path $archive))  { Write-Bad "Archive not found: $archive"; exit 2 }

$repackageOptions = @()
if ($Big) { $repackageOptions += "--big" }
if (-not $NoCompress) { $repackageOptions += "--compress" }

# --- build the CLI once and use the produced dll ---
Write-Step "Building SporeMaster CLI..."
& dotnet build (Join-Path $repoRoot "src\SporeMaster.Cli\SporeMaster.Cli.csproj") -c Release -v q -nologo | Out-Null
$spore = Join-Path $repoRoot "src\SporeMaster.Cli\bin\Release\net9.0\spore.dll"
if (-not (Test-Path $spore)) { Write-Bad "spore.dll was not built."; exit 2 }

$backup = "$archive.smokebackup"
$repacked = Join-Path ([System.IO.Path]::GetTempPath()) ("sporemaster-smoke-" + [Guid]::NewGuid().ToString("N") + ".package")

# --- helper: find crash artifacts the game may have produced ---
function Get-CrashArtifacts {
    $roots = @($gameDir, [System.IO.Path]::GetTempPath())
    $found = @()
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        $found += Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "*.mdmp" -or ($_.Name -like "*exception*.txt") }
    }
    return $found
}

# --- always restore the original archive when we are done ---
$restore = {
    if (Test-Path $backup) {
        Move-Item -Force $backup $archive
        Write-Host "Restored original archive." -ForegroundColor Yellow
    }
}

try {
    # 1. backup
    Write-Step "Backing up $Package -> $([System.IO.Path]::GetFileName($backup))"
    Copy-Item -Force $archive $backup

    # 2. lossless repack (refuses if not identical)
    Write-Step "Repacking losslessly (roundtrip $($repackageOptions -join ' '))..."
    & dotnet $spore roundtrip $archive @repackageOptions --keep $repacked
    if ($LASTEXITCODE -ne 0) {
        Write-Bad "Round-trip is NOT lossless; aborting (nothing was replaced)."
        exit 1
    }

    # 3. put the repacked archive in place
    Write-Step "Replacing the archive with the repacked one."
    Copy-Item -Force $repacked $archive

    # snapshot crash artifacts *before* launch
    $before = @(Get-CrashArtifacts | ForEach-Object { $_.FullName })

    if ($NoLaunch) {
        Write-Ok "Repack in place. -NoLaunch given, so not starting the game."
        Write-Host "Launch the game manually to test, then re-run without -NoLaunch." -ForegroundColor Yellow
    }
    else {
        # 4. launch the game (windowed, portable: -w -devDirs)
        Write-Step "Launching $Exe $LaunchArgs (reach the main menu / play a little, then come back here)."
        $gameExe = Join-Path $gameDir $Exe
        $argList = $LaunchArgs.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
        $proc = Start-Process -FilePath $gameExe -WorkingDirectory $gameDir -ArgumentList $argList -PassThru

        if ($Auto) {
            Write-Step "Auto mode: waiting $WaitSeconds s for the game to start and settle."
            Start-Sleep -Seconds $WaitSeconds
            if ($proc.HasExited) {
                Write-Host "Game process exited on its own (exit code $($proc.ExitCode))." -ForegroundColor Yellow
            }
            else {
                Write-Ok "Game is still running after $WaitSeconds s (no immediate crash)."
            }
        }
        else {
            Write-Host ""
            Write-Host "Press Enter here once you have seen the game start (or after it crashed)..." -ForegroundColor Yellow
            [void][System.Console]::ReadLine()

            if (-not $proc.HasExited) {
                Write-Step "Closing the game..."
                try { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 3 } catch { }
            }
        }

        if (-not $proc.HasExited) {
            try { $proc.Kill() } catch { }
        }
    }

    # 5. check for new crash artifacts
    Start-Sleep -Seconds 2
    $after = @(Get-CrashArtifacts | ForEach-Object { $_.FullName })
    $new = $after | Where-Object { $before -notcontains $_ }

    Write-Host ""
    if ($new.Count -eq 0) {
        Write-Ok "No new crash artifacts. Repack looks GOOD."
        $exitCode = 0
    }
    else {
        Write-Bad "The game produced crash artifact(s) with the repacked archive:"
        foreach ($f in $new) { Write-Host "    $f" -ForegroundColor Red }
        $exitCode = 1
    }
}
finally {
    & $restore
    if (Test-Path $repacked) { Remove-Item -Force $repacked -ErrorAction SilentlyContinue }
}

exit $exitCode
