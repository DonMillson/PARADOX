$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Read-ProjectFile([string]$relativePath) {
    return Get-Content -Raw -Encoding UTF8 (Join-Path $root $relativePath)
}

function Require-Match([string]$text, [string]$pattern, [string]$label) {
    $match = [regex]::Match($text, $pattern)
    if (-not $match.Success) {
        throw "Missing or changed source section: $label"
    }
    return $match.Groups[1].Value
}

function Parse-CommaNames([string]$value) {
    return @($value -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })
}

function Parse-RoleRefs([string]$value) {
    return @([regex]::Matches($value, 'RoleId\.(\w+)') |
        ForEach-Object { $_.Groups[1].Value })
}

function Assert-Roster([string]$label, [string[]]$actual, [string[]]$expected) {
    $unique = @($actual | Select-Object -Unique)
    $missing = @($expected | Where-Object { $actual -cnotcontains $_ })
    $extra = @($actual | Where-Object { $expected -cnotcontains $_ })
    if ($actual.Count -ne $expected.Count -or
        $unique.Count -ne $actual.Count -or
        $missing.Count -gt 0 -or
        $extra.Count -gt 0) {
        throw "$label invalid. Missing: $($missing -join ', '); Extra: $($extra -join ', '); Count: $($actual.Count)/$($expected.Count)"
    }
    Write-Host "OK $label ($($actual.Count) roles)"
}

$roles = Read-ProjectFile 'src/Paradox/Roles/RoleId.cs'
$pool = Read-ProjectFile 'src/Paradox/Roles/InitialRolePool.cs'
$settings = Read-ProjectFile 'src/Paradox/Settings/ParadoxRoleSettings.cs'
$translations = Read-ProjectFile 'src/Paradox/Localization/DefaultTranslations.cs'
$rpc = Read-ProjectFile 'src/Paradox/Networking/ParadoxRpcId.cs'

$impostor = Parse-CommaNames (Require-Match $roles '(?s)// Impostor\s*(.*?)\s*// Crewmate' 'RoleId Impostor')
$crewmate = Parse-CommaNames (Require-Match $roles '(?s)// Crewmate\s*(.*?)\s*// Neutral' 'RoleId Crewmate')
$neutral = Parse-CommaNames (Require-Match $roles '(?s)// Neutral\s*(.*?)\s*\}' 'RoleId Neutral')
if ($impostor.Count -ne 15 -or $crewmate.Count -ne 15 -or $neutral.Count -ne 10) {
    throw "Expected 15 Impostor, 15 Crewmate and 10 Neutral roles."
}
$all = @($impostor) + @($crewmate) + @($neutral)
Assert-Roster 'Unique RoleId values' $all $all

foreach ($faction in @('Impostor', 'Crewmate', 'Neutral')) {
    $expected = switch ($faction) {
        'Impostor' { $impostor }
        'Crewmate' { $crewmate }
        'Neutral' { $neutral }
    }
    $section = Require-Match $pool "(?s)IReadOnlyList<RoleId> $faction \{ get; \} = new\[\]\s*\{(.*?)\}" "InitialRolePool $faction"
    Assert-Roster "InitialRolePool $faction" (Parse-RoleRefs $section) $expected
}

$enabled = Require-Match $settings '(?s)Dictionary<RoleId, bool> Enabled = new\(\)\s*\{(.*?)\};' 'Enabled settings'
$chances = Require-Match $settings '(?s)Dictionary<RoleId, int> SpawnChance = new\(\)\s*\{(.*?)\};' 'Spawn chance settings'
$implemented = Require-Match $settings '(?s)bool IsImplemented\(RoleId role\) => role is(.*?);' 'Implemented roles'
Assert-Roster 'Enabled settings' (Parse-RoleRefs $enabled) $all
Assert-Roster 'Spawn chances' (Parse-RoleRefs $chances) $all
Assert-Roster 'Implemented roles' (Parse-RoleRefs $implemented) $all

foreach ($role in $all) {
    if (-not $translations.Contains('AddRole(l, "' + $role + '"')) {
        throw "Missing bilingual role description: $role"
    }
}
Write-Host "OK bilingual descriptions ($($all.Count) roles)"

$rpcEntries = @([regex]::Matches($rpc, '(?m)^\s*(\w+)\s*=\s*(\d+),?\s*$'))
$ids = @($rpcEntries | ForEach-Object { [int]$_.Groups[2].Value })
$uniqueIds = @($ids | Select-Object -Unique)
if ($ids.Count -eq 0 -or $ids.Count -ne $uniqueIds.Count) {
    throw 'RPC identifiers are missing or duplicated.'
}
for ($number = 0; $number -lt $ids.Count; $number++) {
    if ($ids -notcontains $number) {
        throw "RPC identifier $number is missing."
    }
}
Write-Host "OK unique contiguous RPC identifiers ($($ids.Count))"
Write-Host 'PARADOX static manifest smoke check passed.'
