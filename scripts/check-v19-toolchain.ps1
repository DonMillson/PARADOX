# Automated checks for the Among Us 19.0.0 (2026.9.29) test toolchain.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Read-Text([string]$path) {
    return Get-Content -Raw -Encoding UTF8 (Join-Path $root $path)
}

$project = Read-Text 'src/Paradox/Paradox.csproj'
$workflow = Read-Text '.github/workflows/build.yml'
$feeds = Read-Text 'nuget.config'
$readme = Read-Text 'docs/V19_INSTALL_PL.md'

if ($project -notmatch '<PackageReference Include="Reactor" Version="2\.5\.1-ci\.400"') {
    throw 'PARADOX is not built against the 2026 Reactor CI package.'
}
if ($project -notmatch '<PackageReference Include="AmongUs\.GameLibs\.Steam" Version="2026\.9\.29"') {
    throw 'PARADOX is not compiled against Among Us 2026.9.29 game libraries.'
}
if ($project -match 'lib/Reactor\.dll' -or $workflow -match 'releases/download/2\.5\.1/Reactor\.dll') {
    throw 'Old Reactor 2.5.1 release has returned to the build.'
}
if ($feeds -notmatch 'https://nuget\.reactor\.gg/v3/index\.json') {
    throw 'Reactor CI package source is missing.'
}
if ($workflow -notmatch 'Stage v19 test installation' -or
    $workflow -notmatch 'if-no-files-found: error') {
    throw 'CI would not produce a checked installable test package.'
}
if ($readme -notmatch 'BepInEx' -or $readme -notmatch 'x64' -or
    $readme -notmatch '19\.0\.0') {
    throw 'Version-specific installation instructions are incomplete.'
}
Write-Host 'PASS: pinned Among Us v19 GameLibs / Reactor CI400 and packaged x64 install workflow.'
