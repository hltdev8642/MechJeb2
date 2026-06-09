<#
.SYNOPSIS
    Builds MechJeb2 and assembles the mod into _export/GameData/
    with the standard KSP directory structure.

.DESCRIPTION
    This script:
      1. Builds MechJeb2.sln (Release configuration)
      2. Creates the _export/GameData/ directory tree
      3. Copies all built DLLs, configs, asset bundles, icons,
         localization, parts, and root files into place

    The output in _export/GameData/ can be copied directly into
    a KSP install's GameData/ folder.

.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release)
.PARAMETER KspDir
    KSP install root directory. Defaults to $env:KSPDIR, or
    "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
.PARAMETER SkipBuild
    If set, skips the dotnet build step (exports whatever is in bin/).
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$KspDir = $env:KSPDIR,

    [switch]$SkipBuild
)

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ExportRoot = Join-Path $RepoRoot '_export'
$GameData = Join-Path $ExportRoot 'GameData'

# ---- resolve KSP directory ----
if (-not $KspDir) {
    $KspDir = 'C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program'
}
Write-Host "KSP root: $KspDir" -ForegroundColor Cyan

# ---- Step 1: Build ----
if (-not $SkipBuild) {
    Write-Host "`nBuilding MechJeb2.sln ($Configuration)..." -ForegroundColor Yellow
    Push-Location $RepoRoot
    try {
        $env:KSPDIR = $KspDir
        dotnet build MechJeb2.sln -c $Configuration /nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed (exit code $LASTEXITCODE)."
            exit 1
        }
    } finally {
        Pop-Location
    }
} else {
    Write-Host "`nSkipping build (using existing binaries)." -ForegroundColor Yellow
}

# ---- Step 2: Create directory tree ----
Write-Host "`nCreating export directories..." -ForegroundColor Yellow
$Dirs = @(
    'MechJeb2\Bundles',
    'MechJeb2\Icons',
    'MechJeb2\Localization',
    'MechJeb2\Parts\MechJeb2_AR202',
    'MechJeb2\Plugins',
    'JSI\RasterPropMonitor\Plugins'
)
foreach ($d in $Dirs) {
    $null = New-Item -ItemType Directory -Force -Path (Join-Path $GameData $d)
}

# ---- Step 3: Copy files ----
Write-Host "`nCopying files..." -ForegroundColor Yellow

# 3a. MechJeb2 plugin DLLs and PDBs
$pluginSource = Join-Path $RepoRoot "MechJeb2\bin\$Configuration"
Write-Host "  Plugin source: $pluginSource"
$pluginTarget = Join-Path $GameData 'MechJeb2\Plugins'
$dlls = @(
    'MechJeb2.dll', 'MechJeb2.pdb', 'MechJeb2.dll.config',
    'alglib.dll', 'alglib.pdb',
    'MechJebLib.dll', 'MechJebLib.pdb',
    'MechJebLibBindings.dll', 'MechJebLibBindings.pdb'
)
foreach ($dll in $dlls) {
    $src = Join-Path $pluginSource $dll
    if (Test-Path $src) {
        Copy-Item $src $pluginTarget -Force
        Write-Host "    + $dll"
    } else {
        Write-Warning "    ? $dll not found at $src"
    }
}

# 3b. RPM plugin
$rpmSource = Join-Path $RepoRoot "MechJebRPM\bin\$Configuration"
$rpmTarget = Join-Path $GameData 'JSI\RasterPropMonitor\Plugins'
foreach ($file in @('MechJebRPM.dll', 'MechJebRPM.pdb')) {
    $src = Join-Path $rpmSource $file
    if (Test-Path $src) {
        Copy-Item $src $rpmTarget -Force
        Write-Host "    + $file (RPM)"
    } else {
        Write-Warning "    ? $file not found at $src"
    }
}

# 3c. Bundles
$bundleSource = Join-Path $RepoRoot 'Bundles'
$bundleTarget = Join-Path $GameData 'MechJeb2\Bundles'
if (Test-Path (Join-Path $bundleSource 'shaders.bundle')) {
    Copy-Item (Join-Path $bundleSource 'shaders.bundle') $bundleTarget -Force
    Write-Host '    + shaders.bundle'
}

# 3d. Icons
$iconSource = Join-Path $RepoRoot 'Icons'
$iconTarget = Join-Path $GameData 'MechJeb2\Icons'
Copy-Item "$iconSource\*.png" $iconTarget -Force
Write-Host '    + Icons/*.png'

# 3e. Localization
$locSource = Join-Path $RepoRoot 'Localization'
$locTarget = Join-Path $GameData 'MechJeb2\Localization'
Copy-Item "$locSource\*.cfg" $locTarget -Force
Write-Host '    + Localization/*.cfg'

# 3f. Parts
$partSource = Join-Path $RepoRoot 'Parts'
$partTarget = Join-Path $GameData 'MechJeb2\Parts'
Copy-Item "$partSource\MechJeb2_AR202\*" (Join-Path $partTarget 'MechJeb2_AR202') -Force
if (Test-Path (Join-Path $partSource 'MechJebNoCommandPod.cfg')) {
    Copy-Item (Join-Path $partSource 'MechJebNoCommandPod.cfg') $partTarget -Force
}
Write-Host '    + Parts/*'

# 3g. Root files
Copy-Item (Join-Path $RepoRoot 'LandingSites.cfg') (Join-Path $GameData 'MechJeb2') -Force
Copy-Item (Join-Path $RepoRoot 'LICENSE.md') (Join-Path $GameData 'MechJeb2') -Force
Write-Host '    + LandingSites.cfg, LICENSE.md'

# ---- Summary ----
$totalFiles = (Get-ChildItem $GameData -Recurse -File).Count
$totalSize = "{0:N2} MB" -f ((Get-ChildItem $GameData -Recurse -File | Measure-Object Length -Sum).Sum / 1MB)

Write-Host "`n================================" -ForegroundColor Green
Write-Host "  Export complete!" -ForegroundColor Green
Write-Host "  Location: $GameData" -ForegroundColor Green
Write-Host "  Files:    $totalFiles" -ForegroundColor Green
Write-Host "  Size:     $totalSize" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host "`nCopy '_export\GameData' over your KSP install's GameData folder to deploy."
