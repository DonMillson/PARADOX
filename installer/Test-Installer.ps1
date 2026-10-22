# CI smoke test for the *real* install/uninstall PowerShell scripts.
# Uses synthetic Among Us x64 PE header; no game distribution or gameplay.
$ErrorActionPreference='Stop'
$root=(Resolve-Path .).Path
$payload=Join-Path $root 'dist/installer/payload'
$game=Join-Path $env:RUNNER_TEMP 'paradox-installer-smoke-game'
$support=Join-Path $env:RUNNER_TEMP 'paradox-installer-smoke-support'
foreach($path in @($game,$support)) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}
# Synthetic x64 PE (not executable), enough for the installer architecture check.
$bytes=New-Object byte[] 512
$bytes[0]=0x4D; $bytes[1]=0x5A
[BitConverter]::GetBytes([int]0x80).CopyTo($bytes,0x3C)
[BitConverter]::GetBytes([uint32]0x4550).CopyTo($bytes,0x80)
[BitConverter]::GetBytes([uint16]0x8664).CopyTo($bytes,0x84)
[IO.File]::WriteAllBytes((Join-Path $game 'Among Us.exe'),$bytes)
New-Item -ItemType Directory -Force -Path (Join-Path $game 'Among Us_Data') | Out-Null
Set-Content (Join-Path $game 'GameAssembly.dll') -Value 'synthetic smoke test only'

# Simulate pre-existing BepInEx and third-party mod; neither may be lost.
New-Item -ItemType Directory -Force -Path (Join-Path $game 'BepInEx/plugins') | Out-Null
[IO.File]::WriteAllText((Join-Path $game 'winhttp.dll'),'old-winhttp')
[IO.File]::WriteAllText((Join-Path $game 'BepInEx/plugins/Reactor.dll'),'old-reactor')
[IO.File]::WriteAllText((Join-Path $game 'BepInEx/plugins/FriendsCustomMod.dll'),'unrelated')
New-Item -ItemType Directory -Force -Path $support | Out-Null
& "$root/installer/Install-Paradox.ps1" -GamePath $game -PayloadRoot $payload -SupportRoot $support
foreach($name in @('winhttp.dll','BepInEx/plugins/Reactor.dll',
    'BepInEx/plugins/Paradox.dll','BepInEx/core')) {
    if (-not (Test-Path (Join-Path $game $name))) { throw "Installation missing $name" }
}
if (-not (Test-Path (Join-Path $support 'installed-files.json'))) {
    throw 'Installation did not write the uninstall manifest'
}
if ([IO.File]::ReadAllText((Join-Path $game 'BepInEx/plugins/FriendsCustomMod.dll')) -ne 'unrelated') {
    throw 'The installer modified an unrelated mod'
}
& "$root/installer/Uninstall-Paradox.ps1" -SupportRoot $support
if (Test-Path (Join-Path $game 'BepInEx/plugins/Paradox.dll')) {
    throw 'Uninstaller left PARADOX.dll'
}
if ([IO.File]::ReadAllText((Join-Path $game 'winhttp.dll')) -ne 'old-winhttp' -or
    [IO.File]::ReadAllText((Join-Path $game 'BepInEx/plugins/Reactor.dll')) -ne 'old-reactor') {
    throw 'Previous loader/Reactors not restored'
}
if ([IO.File]::ReadAllText((Join-Path $game 'BepInEx/plugins/FriendsCustomMod.dll')) -ne 'unrelated') {
    throw 'Uninstaller changed unrelated mod'
}
Write-Host 'PASS: offline install, PE x64 guard, backup restoration, uninstaller, no impact on foreign plugins.'

# Test no install on synthetic x86 image.
$bytes[0x84]=0x4c; $bytes[0x85]=0x01
[IO.File]::WriteAllBytes((Join-Path $game 'Among Us.exe'),$bytes)
$failed=$false
try {
    & "$root/installer/Install-Paradox.ps1" -GamePath $game -PayloadRoot $payload -SupportRoot $support
} catch { $failed=$true; Write-Host "PASS: rejected x86: $($_.Exception.Message)" }
if (-not $failed) { throw 'Installer failed to reject x86 Among Us executable.' }
