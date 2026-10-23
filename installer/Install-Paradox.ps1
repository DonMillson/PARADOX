# This installer runs from the bundled Windows setup, with no downloads at install time.
# Copies only BepInEx loader files and the two PARADOX plugin DLLs.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GamePath,
    [Parameter(Mandatory=$true)][string]$PayloadRoot,
    [Parameter(Mandatory=$true)][string]$SupportRoot
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

function Assert-Game([string]$path) {
    $exe=Join-Path $path 'Among Us.exe'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $path 'GameAssembly.dll') -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $path 'Among Us_Data') -PathType Container)) {
        throw 'Wybierz katalog gry Among Us (Among Us.exe, GameAssembly.dll i Among Us_Data).'
    }
    $fs=[IO.File]::OpenRead($exe)
    try {
        $reader=[IO.BinaryReader]::new($fs)
        if ($reader.ReadUInt16() -ne 0x5A4D) { throw 'Nieprawidlowy plik PE gry.' }
        $fs.Position=0x3C
        $offset=$reader.ReadInt32()
        if ($offset -lt 0x40 -or $offset -gt $fs.Length-6) { throw 'Nieprawidlowy naglowek PE.' }
        $fs.Position=$offset
        if ($reader.ReadUInt32() -ne 0x4550) { throw 'Nieprawidlowy podpis PE.' }
        if ($reader.ReadUInt16() -ne 0x8664) { throw 'Wymagana jest gra Among Us x64.' }
    } finally { $fs.Dispose() }
    if (Get-Process -Name 'Among Us' -ErrorAction SilentlyContinue) {
        throw 'Zamknij Among Us przed instalacja PARADOX.'
    }
}
$game=[IO.Path]::GetFullPath($GamePath).TrimEnd('\')
$payload=[IO.Path]::GetFullPath($PayloadRoot).TrimEnd('\')
$support=[IO.Path]::GetFullPath($SupportRoot).TrimEnd('\')
Assert-Game $game

foreach ($needed in @('BepInEx\core','BepInEx\core\BepInEx.Unity.IL2CPP.dll',
                     'BepInEx\plugins\Reactor.dll','BepInEx\plugins\Paradox.dll',
                     'winhttp.dll','doorstop_config.ini','dotnet\coreclr.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $payload $needed))) {
        throw "Niekompletna paczka instalatora: $needed"
    }
}
# A complete ZIP can still contain a disabled/wrong Doorstop configuration.
# Reject it BEFORE any game files are touched.
$doorstop=Get-Content -LiteralPath (Join-Path $payload 'doorstop_config.ini') -Raw -Encoding UTF8
if ($doorstop -notmatch '(?mi)^\s*enabled\s*=\s*true\s*$' -or
    $doorstop -notmatch '(?mi)^\s*target_assembly\s*=\s*BepInEx[\\/]core[\\/]BepInEx\.Unity\.IL2CPP\.dll\s*$') {
    throw 'Nieprawidlowy loader: Doorstop musi byc aktywny i wskazywac BepInEx IL2CPP.'
}
$manifestPath=Join-Path $support 'installed-files.json'
$prior=@{}
if (Test-Path -LiteralPath $manifestPath) {
    $stored=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($stored.GamePath -ne $game) {
        throw 'Instalator PARADOX jest przypisany do innego katalogu gry. Najpierw odinstaluj poprzednia instalacje.'
    }
    foreach ($entry in @($stored.Files)) { $prior[$entry.RelativePath]=$entry }
}
$entries=New-Object 'System.Collections.Generic.List[object]'
# An IL2CPP loader may contain a top-level dotnet runtime and other native
# directories. Installing only BepInEx/* and winhttp.dll is incomplete.
# Ship the *whole official ZIP*; the audit manifest itself is not a game file.
$files=@(Get-ChildItem -LiteralPath $payload -File -Recurse | Where-Object {
    $rel=$_.FullName.Substring($payload.Length+1)
    $rel -ne 'PARADOX-PAYLOAD-MANIFEST.json'
})
$transaction=Join-Path $support '.transaction'
New-Item -ItemType Directory -Force -Path $transaction | Out-Null
$changed=New-Object 'System.Collections.Generic.List[object]'
$installedCount=0
try {
    foreach($item in $files) {
        $rel=$item.FullName.Substring($payload.Length+1)
        if ([IO.Path]::IsPathRooted($rel) -or $rel.Contains('..')) { throw 'Niebezpieczna sciezka paczki.' }
        $target=Join-Path $game $rel
        $backup=Join-Path $support (Join-Path 'backup' $rel)
        $originalExists=Test-Path -LiteralPath $target -PathType Leaf
        if ($originalExists -and ((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Symlink nie moze zostac nadpisany: $rel"
        }
        $before=Join-Path $transaction $rel
        if ($originalExists) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $before) | Out-Null
            Copy-Item -LiteralPath $target -Destination $before -Force
        }
        $changed.Add([pscustomobject]@{ Target=$target; WasPresent=$originalExists; Before=$before })
        $hadOriginal=$originalExists
        if ($prior.ContainsKey($rel)) {
            $hadOriginal=[bool]$prior[$rel].HadOriginal
        } elseif ($originalExists) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
            Copy-Item -LiteralPath $target -Destination $backup -Force
        }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -LiteralPath $item.FullName -Destination $target -Force
        $sha=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        $expected=(Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
        if ($sha -ne $expected) { throw "Plik nie przeszedl weryfikacji kopiowania: $rel" }
        $entries.Add([pscustomobject]@{ RelativePath=$rel; HadOriginal=$hadOriginal; InstalledSha256=$sha })
        $installedCount++
        if ($installedCount % 4 -eq 0 -or $installedCount -eq $files.Count) {
            Write-Output "PARADOX_PROGRESS:$installedCount/$($files.Count)"
        }
    }
    $data=[pscustomobject]@{
        GamePath=$game; Version='0.7.0-dev.6'; Files=@($entries.ToArray())
    }
    $tmp=Join-Path $support 'installed-files.json.tmp'
    $data | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $tmp -Encoding UTF8
    Move-Item -LiteralPath $tmp -Destination $manifestPath -Force
    Write-Host "PARADOX z BepInEx zainstalowany: $game ($($entries.Count) plikow)"
}
catch {
    for($i=$changed.Count-1; $i -ge 0; $i--) {
        $entry=$changed[$i]
        try {
            if ($entry.WasPresent) { Copy-Item -LiteralPath $entry.Before -Destination $entry.Target -Force }
            elseif (Test-Path -LiteralPath $entry.Target) { Remove-Item -LiteralPath $entry.Target -Force }
        } catch { Write-Warning "Nie udalo sie cofnac: $($entry.Target)" }
    }
    throw
}
finally {
    if (Test-Path -LiteralPath $transaction) { Remove-Item -LiteralPath $transaction -Recurse -Force }
}
