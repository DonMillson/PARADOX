# PARADOX v19 test installer for user-owned game files.
# Never patches/redistributes the game executable; only copies mod DLLs.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $AmongUsPath
)

$ErrorActionPreference = 'Stop'
$game = [IO.Path]::GetFullPath($AmongUsPath)
$exe = Join-Path $game 'Among Us.exe'
if (-not (Test-Path $exe -PathType Leaf)) {
    throw "Nie znaleziono Among Us.exe w: $game"
}

# Read the PE Machine field instead of guessing from the Windows installation
# or the folder name (Steam also has an x86 Program Files parent).
$stream = [IO.File]::OpenRead($exe)
try {
    $reader = [IO.BinaryReader]::new($stream)
    if ($reader.ReadUInt16() -ne 0x5A4D) {
        throw 'Plik gry nie ma naglowka PE (MZ).'
    }
    $stream.Position = 0x3C
    $peOffset = $reader.ReadInt32()
    if ($peOffset -lt 0x40 -or $peOffset -gt $stream.Length - 6) {
        throw 'Bledny naglowek PE.'
    }
    $stream.Position = $peOffset
    if ($reader.ReadUInt32() -ne 0x00004550) {
        throw 'Plik gry nie ma poprawnej sygnatury PE.'
    }
    $machine = $reader.ReadUInt16()
}
finally {
    $stream.Dispose()
}
if ($machine -ne 0x8664) {
    throw ('PARADOX v19 jest przeznaczony dla Among Us x64. Wykryta architektura PE: 0x{0:X4}.' -f $machine)
}

$bepInExRoot = Join-Path $game 'BepInEx'
$core = Join-Path $bepInExRoot 'core'
$plugins = Join-Path $bepInExRoot 'plugins'
if (-not (Test-Path $core -PathType Container) -or
    -not (Test-Path (Join-Path $game 'winhttp.dll') -PathType Leaf)) {
    throw 'Brak zainstalowanego BepInEx Unity.IL2CPP x64. Zainstaluj oficjalny BepInEx be.785 i uruchom gre raz.'
}

$source = Join-Path $PSScriptRoot 'BepInEx/plugins'
foreach ($name in @('Reactor.dll', 'Paradox.dll')) {
    if (-not (Test-Path (Join-Path $source $name) -PathType Leaf)) {
        throw "W paczce nie ma pliku $name"
    }
}
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
foreach ($name in @('Reactor.dll', 'Paradox.dll')) {
    $dest = Join-Path $plugins $name
    if (Test-Path $dest) {
        # Preserve preexisting plugin versions before replacing them.
        $backup = Join-Path $plugins ($name + '.backup-' +
            (Get-Date -Format 'yyyyMMdd-HHmmss'))
        Copy-Item -LiteralPath $dest -Destination $backup -Force
    }
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination $dest -Force
    Write-Host "Zainstalowano: $dest"
}
Write-Host 'PARADOX test build zainstalowany. Uruchom Among Us 19.0.0 i sprawdz BepInEx/LogOutput.log.'
Write-Host 'WAZNE: ta instalacja nie jest dowodem zgodnosci w trakcie rozgrywki.'
