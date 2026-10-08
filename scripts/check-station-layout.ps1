$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$map = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src/Paradox/Maps/ParadoxStation.cs')
$viewer = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src/Paradox/Maps/ParadoxStationMapHudPatch.cs')

$roomPattern = 'new Room\("([^"]+)",\s*"([^"]+)",\s*(-?\d+),\s*(-?\d+),\s*(true|false),\s*"([^"]+)"\)'
$roomMatches = [regex]::Matches($map, $roomPattern)
if ($roomMatches.Count -ne 12) { throw "Expected 12 station rooms, found $($roomMatches.Count)." }

$rooms = @{}
$tasks = @{}
$coordinates = @{}
$spawnCount = 0
foreach ($match in $roomMatches) {
    $id = $match.Groups[1].Value
    $task = $match.Groups[6].Value
    $position = "$($match.Groups[3].Value),$($match.Groups[4].Value)"
    if ($rooms.ContainsKey($id) -or $tasks.ContainsKey($task) -or $coordinates.ContainsKey($position)) {
        throw "Duplicate station room, task or coordinate: $id"
    }
    $rooms[$id] = $true
    $tasks[$task] = $true
    $coordinates[$position] = $true
    if ($match.Groups[5].Value -eq 'true') { $spawnCount++ }
}
if ($spawnCount -ne 1) { throw "Expected exactly one station spawn, found $spawnCount." }

$parts = [regex]::Match($map, '(?s)IReadOnlyList<Passage> Corridors.*?new\[\]\s*\{(.*?)\};.*?IReadOnlyList<Passage> Vents.*?new\[\]\s*\{(.*?)\};')
if (-not $parts.Success) { throw 'Cannot find corridor/vent lists.' }
$passagePattern = 'new Passage\("([^"]+)",\s*"([^"]+)"\)'
$corridors = @([regex]::Matches($parts.Groups[1].Value, $passagePattern))
$vents = @([regex]::Matches($parts.Groups[2].Value, $passagePattern))
if ($corridors.Count -ne 16 -or $vents.Count -ne 3) {
    throw "Expected 16 corridors/3 vents, found $($corridors.Count)/$($vents.Count)."
}

$graph = @{}
foreach ($id in $rooms.Keys) { $graph[$id] = [System.Collections.Generic.List[string]]::new() }
foreach ($kind in @('corridor', 'vent')) {
    $seen = @{}
    $links = if ($kind -eq 'corridor') { $corridors } else { $vents }
    foreach ($match in $links) {
        $from = $match.Groups[1].Value
        $to = $match.Groups[2].Value
        if (-not $rooms.ContainsKey($from) -or -not $rooms.ContainsKey($to) -or $from -eq $to) {
            throw "Invalid $kind link: $from -> $to"
        }
        $edge = @($from, $to) | Sort-Object
        $key = $edge -join ':'
        if ($seen.ContainsKey($key)) { throw "Duplicate $kind link: $key" }
        $seen[$key] = $true
        if ($kind -eq 'corridor') {
            $graph[$from].Add($to)
            $graph[$to].Add($from)
        }
    }
}
$start = $roomMatches | Where-Object { $_.Groups[5].Value -eq 'true' } | Select-Object -First 1
$queue = [System.Collections.Generic.Queue[string]]::new()
$visited = @{}
$spawn = $start.Groups[1].Value
$queue.Enqueue($spawn)
$visited[$spawn] = $true
while ($queue.Count -gt 0) {
    foreach ($next in $graph[$queue.Dequeue()]) {
        if (-not $visited.ContainsKey($next)) {
            $visited[$next] = $true
            $queue.Enqueue($next)
        }
    }
}
if ($visited.Count -ne 12) { throw "Unreachable station rooms: $($visited.Count)/12" }
if (-not $viewer.Contains('KeyCode.F6') -or -not $viewer.Contains('ParadoxStation.Corridors') -or -not $viewer.Contains('ParadoxStation.Vents')) {
    throw 'Station in-game blueprint viewer missing layout bindings.'
}
$runtime = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src/Paradox/Maps/ParadoxStationRuntime.cs')
foreach ($required in @('IsSoloLobby()', 'enteringSoloLobby', '_soloLobbyPreview', 'RestoreHostPlayers()', 'MoveSoloLobbyCameraToStation()', 'RestoreSoloLobbyCamera()', 'ShipStatus.Instance == null')) {
    if (-not $runtime.Contains($required)) { throw "Solo preview integration missing: $required" }
}
$ui = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src/Paradox/UI/ParadoxLobbyRoleSummaryPatch.cs')
foreach ($token in @('CreateSoloStationButton', 'TESTUJ MAPĘ', 'ParadoxStationRuntime.TryToggleHost')) {
    if (-not $ui.Contains($token)) { throw "Missing clickable lobby station entry: $token" }
}
Write-Host 'PARADOX STATION: 12 reachable rooms, 16 corridors, 3 vents, 12 tasks, F6 blueprint and clickable solo-lobby station button.'

