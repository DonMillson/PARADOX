# Removes only installer-owned files that still match their installed SHA256.
# Prior versions of loader/mod files are restored from install-time backups.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$SupportRoot)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$manifest=Join-Path $SupportRoot 'installed-files.json'
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    Write-Host 'PARADOX: brak manifestu; nie usuwam plikow gry.'
    exit 0
}
$data=Get-Content -LiteralPath $manifest -Raw -Encoding UTF8 | ConvertFrom-Json
$game=[IO.Path]::GetFullPath([string]$data.GamePath).TrimEnd('\')
if (-not (Test-Path -LiteralPath (Join-Path $game 'Among Us.exe') -PathType Leaf)) {
    throw 'Brak Among Us.exe; odinstalowanie nie modyfikuje nieznanej lokalizacji.'
}
$skipped=0
$removed=0
foreach($entry in @($data.Files)) {
    $rel=[string]$entry.RelativePath
    if ([IO.Path]::IsPathRooted($rel) -or $rel.Contains('..') -or
        -not ($rel.StartsWith('BepInEx\', [StringComparison]::OrdinalIgnoreCase) -or
              $rel -in @('winhttp.dll','doorstop_config.ini','.doorstop_version'))) {
        Write-Warning "Pominieto nieznana sciezke: $rel"; $skipped++; continue
    }
    $target=Join-Path $game $rel
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { continue }
    if ((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        Write-Warning "Pominieto link: $rel"; $skipped++; continue
    }
    $sha=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    if ($sha -ne $entry.InstalledSha256) {
        Write-Warning "Plik zmieniony przez inny program; zachowano: $rel"
        $skipped++
        continue
    }
    if ([bool]$entry.HadOriginal) {
        $backup=Join-Path $SupportRoot (Join-Path 'backup' $rel)
        if (-not (Test-Path -LiteralPath $backup -PathType Leaf)) {
            Write-Warning "Brak kopii; zachowano plik: $rel"; $skipped++; continue
        }
        Copy-Item -LiteralPath $backup -Destination $target -Force
    } else {
        Remove-Item -LiteralPath $target -Force
    }
    $removed++
}
if ($skipped -gt 0) {
    Write-Warning "Pominieto $skipped plikow. Kopie pozostawiono w: $SupportRoot"
} else {
    Remove-Item -LiteralPath $manifest -Force
    $backupRoot=Join-Path $SupportRoot 'backup'
    if (Test-Path -LiteralPath $backupRoot) {
        Remove-Item -LiteralPath $backupRoot -Recurse -Force
    }
}
Write-Host "PARADOX: odinstalowanie: $removed plikow usunieto/przywrocono; pominieto $skipped."
