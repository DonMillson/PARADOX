# PARADOX full offline installer (BepInEx IL2CPP x64 + Reactor + mod).
# BepInEx ZIP is fetched only when building installer; setup requires no internet.
param([string]$PayloadRoot = "dist/installer/payload")
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force -Path $PayloadRoot | Out-Null

$build='790'
$version='6.0.0-be.790+4e98359'
$name="BepInEx-Unity.IL2CPP-win-x64-$version.zip"
$url="https://builds.bepinex.dev/projects/bepinex_be/$build/$([uri]::EscapeDataString($name))"
$archive=Join-Path $env:RUNNER_TEMP 'bepinex-unity-il2cpp-x64.zip'
Write-Host "Downloading official BepInEx x64 artifact $version"
Invoke-WebRequest -Uri $url -OutFile $archive -MaximumRedirection 5
if ((Get-Item $archive).Length -lt 100000) {
    throw "BepInEx archive too small. Rejecting potentially invalid response."
}
Write-Host "BepInEx archive SHA256: $((Get-FileHash $archive -Algorithm SHA256).Hash)"
Expand-Archive -LiteralPath $archive -DestinationPath $PayloadRoot -Force
foreach($path in @('winhttp.dll','BepInEx/core')) {
    if (-not (Test-Path (Join-Path $PayloadRoot $path))) {
        throw "BepInEx package is not the expected IL2CPP x64 release: missing $path"
    }
}
# Place the matching DLLs alongside the unpacked BepInEx payload.
$pluginDir=Join-Path $PayloadRoot 'BepInEx/plugins'
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item "src/Paradox/bin/Release/net6.0/Paradox.dll" $pluginDir -Force
$reactorPackage=Join-Path $env:USERPROFILE ".nuget/packages/reactor/2.5.1-ci.400"
$reactor=Get-ChildItem -LiteralPath $reactorPackage -Filter Reactor.dll -File -Recurse |
  Where-Object { $_.FullName -match '[\\/]lib[\\/]' } | Select-Object -First 1
if (-not $reactor) { throw 'Missing pinned Reactor 2.5.1-ci.400 DLL' }
Copy-Item $reactor.FullName (Join-Path $pluginDir 'Reactor.dll') -Force

# The manifest is retained inside the Inno payload as an audit record.
$manifest=[ordered]@{
  Game='Among Us 19.0.0 / Steam 2026.9.29 / Windows x64'
  Paradox='0.7.0-dev.5'
  Reactor='2.5.1-ci.400'
  BepInEx=$version
  BepInExSource=$url
  BepInExArchiveSHA256=(Get-FileHash $archive -Algorithm SHA256).Hash
  ParadoxSHA256=(Get-FileHash (Join-Path $pluginDir 'Paradox.dll') -Algorithm SHA256).Hash
  ReactorSHA256=(Get-FileHash (Join-Path $pluginDir 'Reactor.dll') -Algorithm SHA256).Hash
}
$manifest | ConvertTo-Json -Depth 3 | Set-Content -Path (Join-Path $PayloadRoot 'PARADOX-PAYLOAD-MANIFEST.json') -Encoding UTF8
Write-Host "Installer payload staged. Files: $(@(Get-ChildItem -Path $PayloadRoot -Recurse -File).Count)"
