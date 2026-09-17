<#
.SYNOPSIS
    Copies the canonical templates and shared reference pages into the Claude skill.

.DESCRIPTION
    The repository keeps one canonical copy of each shared file:

        templates/                          ->  skills/pmlnet/assets/templates/
        docs/reference/api-cheatsheet.md    ->  skills/pmlnet/references/api-cheatsheet.md
        docs/reference/troubleshooting.md   ->  skills/pmlnet/references/troubleshooting.md

    The skill needs its own copies so it stays self-contained when installed
    on its own. Run this after editing any canonical file.

    Build output (bin/, obj/) is never copied.

.PARAMETER Check
    Report differences and exit non-zero instead of copying. For CI.

.EXAMPLE
    pwsh tools/sync-skill-assets.ps1
    pwsh tools/sync-skill-assets.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$templatesSource = Join-Path $repoRoot 'templates'
$templatesTarget = Join-Path $repoRoot 'skills\pmlnet\assets\templates'
$referencesTarget = Join-Path $repoRoot 'skills\pmlnet\references'

$sharedReferences = @(
    'api-cheatsheet.md',
    'troubleshooting.md'
)

function Get-TemplateFiles([string]$root) {
    if (-not (Test-Path $root)) { return @() }
    Get-ChildItem -Path $root -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        ForEach-Object {
            [PSCustomObject]@{
                Relative = $_.FullName.Substring($root.Length).TrimStart('\')
                FullName = $_.FullName
            }
        }
}

$differences = @()

# --- templates -----------------------------------------------------------
$sourceFiles = Get-TemplateFiles $templatesSource
$targetFiles = Get-TemplateFiles $templatesTarget

foreach ($file in $sourceFiles) {
    $destination = Join-Path $templatesTarget $file.Relative
    $same = (Test-Path $destination) -and
            ((Get-FileHash $file.FullName).Hash -eq (Get-FileHash $destination).Hash)

    if ($same) { continue }

    $differences += "templates/$($file.Relative)"

    if (-not $Check) {
        $parent = Split-Path -Parent $destination
        if (-not (Test-Path $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        Copy-Item $file.FullName $destination -Force
    }
}

# Remove skill copies whose source is gone
$sourceRelative = $sourceFiles | ForEach-Object { $_.Relative }
foreach ($file in $targetFiles) {
    if ($sourceRelative -contains $file.Relative) { continue }

    $differences += "templates/$($file.Relative) (removed from source)"
    if (-not $Check) {
        Remove-Item $file.FullName -Force
    }
}

# --- shared reference pages ---------------------------------------------
foreach ($name in $sharedReferences) {
    $source = Join-Path $repoRoot "docs\reference\$name"
    $destination = Join-Path $referencesTarget $name

    $same = (Test-Path $destination) -and
            ((Get-FileHash $source).Hash -eq (Get-FileHash $destination).Hash)

    if ($same) { continue }

    $differences += "docs/reference/$name"
    if (-not $Check) {
        Copy-Item $source $destination -Force
    }
}

# --- result --------------------------------------------------------------
if ($differences.Count -eq 0) {
    Write-Output 'Skill assets are in sync.'
    exit 0
}

if ($Check) {
    Write-Output 'Skill assets are OUT OF SYNC:'
    $differences | ForEach-Object { Write-Output "  $_" }
    Write-Output ''
    Write-Output 'Run: pwsh tools/sync-skill-assets.ps1'
    exit 1
}

Write-Output "Synced $($differences.Count) file(s):"
$differences | ForEach-Object { Write-Output "  $_" }
exit 0
