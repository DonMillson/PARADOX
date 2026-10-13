#!/usr/bin/env python3
"""Build checked production GEOMETRY data for the 12-room PARADOX STATION.

This creates a deterministic Unity layout and traversable corridor/wall graph.
It does not claim an Among Us ShipStatus, game map choice, or task integration.
"""
from __future__ import annotations

import argparse
import heapq
import json
import re
from collections import defaultdict, deque
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "src/Paradox/Maps/ParadoxStation.cs"
DEFAULT_OUTPUT = ROOT / "unity/ParadoxStation/Assets/Resources/StationProductionLayout.json"

SCALE = 2  # cells per Unity world unit
X_STEP, Y_STEP = 56, 38  # 28 × 19 world units between manifest grid nodes
HALF_WIDTH, HALF_HEIGHT = 19, 10  # rooms: 19.5 × 10.5 world units
CORRIDOR_RADIUS = 3  # seven-cell / 3.5 unit wide traversable corridors
LAYOUT_VERSION = 1


def read_manifest():
    text = MANIFEST.read_text(encoding="utf-8")
    room_pattern = r'new Room\("([^"]+)",\s*"([^"]+)",\s*(-?\d+),\s*(-?\d+),\s*(true|false),\s*"([^"]+)"\)'
    rooms = []
    for name, title, gx, gy, is_spawn, task in re.findall(room_pattern, text):
        rooms.append(dict(id=name, name=title, gridX=int(gx), gridY=int(gy),
                          x=int(gx)*X_STEP, y=int(gy)*Y_STEP,
                          isSpawn=is_spawn == "true", taskId=task))
    sections = re.search(
        r'IReadOnlyList<Passage> Corridors.*?new\[\]\s*\{(.*?)\};'
        r'.*?IReadOnlyList<Passage> Vents.*?new\[\]\s*\{(.*?)\};',
        text, re.DOTALL)
    if not sections:
        raise ValueError("Could not parse corridor and vent manifests")
    link_re = r'new Passage\("([^"]+)",\s*"([^"]+)"\)'
    corridors = [tuple(v) for v in re.findall(link_re, sections.group(1))]
    vents = [tuple(v) for v in re.findall(link_re, sections.group(2))]
    if len(rooms) != 12 or len(corridors) != 16 or len(vents) != 3:
        raise ValueError("Station manifest must contain 12 rooms, 16 corridors, 3 vent links")
    if len({r["id"] for r in rooms}) != 12 or sum(r["isSpawn"] for r in rooms) != 1:
        raise ValueError("Nonunique rooms or spawn")
    if len({r["taskId"] for r in rooms}) != 12:
        raise ValueError("Nonunique task identifiers")
    ids = {r["id"] for r in rooms}
    if any(a not in ids or b not in ids or a == b for a, b in corridors+vents):
        raise ValueError("Invalid corridor/vent endpoints")
    if len({tuple(sorted(c)) for c in corridors}) != 16:
        raise ValueError("Duplicate corridors")
    return rooms, corridors, vents


def rect_cells(room, pad=0):
    x, y = room["x"], room["y"]
    return {(a, b) for a in range(x-HALF_WIDTH-pad, x+HALF_WIDTH+pad+1)
                   for b in range(y-HALF_HEIGHT-pad, y+HALF_HEIGHT+pad+1)}


def route_between(start, end, rooms):
    # Route centers through their own room but prevent crossing any third room.
    excluded = {start["id"], end["id"]}
    blocked = set()
    for r in rooms:
        if r["id"] not in excluded:
            blocked.update(rect_cells(r, CORRIDOR_RADIUS+2))

    source = (start["x"], start["y"])
    goal = (end["x"], end["y"])
    minx = min(r["x"] for r in rooms)-100
    maxx = max(r["x"] for r in rooms)+100
    miny = min(r["y"] for r in rooms)-100
    maxy = max(r["y"] for r in rooms)+100
    def distance(p):
        return abs(p[0]-goal[0])+abs(p[1]-goal[1])

    # Favour few 90-degree turns, but allow detours around other rooms.
    heap = [(distance(source), 0, source[0], source[1], -1)]
    best = {(source[0],source[1],-1): 0}
    back = {}
    directions = ((1,0),(0,1),(-1,0),(0,-1))
    end_key = None
    while heap:
        _, cost, x, y, prev = heapq.heappop(heap)
        key = (x,y,prev)
        if cost != best.get(key):
            continue
        if (x,y) == goal:
            end_key = key
            break
        for di,(dx,dy) in enumerate(directions):
            nx,ny=x+dx,y+dy
            if not(minx <= nx <= maxx and miny <= ny <= maxy):
                continue
            if (nx,ny) in blocked:
                continue
            step = 10+(3 if prev >= 0 and di != prev else 0)
            nc=cost+step
            nk=(nx,ny,di)
            if nc < best.get(nk, 10**18):
                best[nk]=nc
                back[nk]=key
                heapq.heappush(heap,(nc+distance((nx,ny))*10,nc,nx,ny,di))
    if end_key is None:
        raise ValueError(f"No valid route: {start['id']} -> {end['id']}")
    points = []
    curr = end_key
    while True:
        points.append((curr[0],curr[1]))
        if curr not in back:
            break
        curr=back[curr]
    points.reverse()
    if points[0] != source or points[-1] != goal:
        raise AssertionError("Broken route reconstruct")
    # Simplify collinear path for efficient Unity corridor meshes.
    bends=[points[0]]
    for i in range(1,len(points)-1):
        a,b,c=points[i-1:i+2]
        if (b[0]-a[0],b[1]-a[1]) != (c[0]-b[0],c[1]-b[1]):
            bends.append(b)
    bends.append(points[-1])
    return points, bends


def wall_runs(walkable):
    """Merge all exposed tile edges into boundary wall segments."""
    horizontal=defaultdict(set)
    vertical=defaultdict(set)
    for x,y in walkable:
        if (x,y+1) not in walkable:
            horizontal[y+1].add(x)
        if (x,y-1) not in walkable:
            horizontal[y].add(x)
        if (x+1,y) not in walkable:
            vertical[x+1].add(y)
        if (x-1,y) not in walkable:
            vertical[x].add(y)

    result=[]
    for axis, groups in (("H",horizontal),("V",vertical)):
        for fixed,values in sorted(groups.items()):
            start=None
            last=None
            for n in sorted(values):
                if last is not None and n != last+1:
                    result.append(dict(axis=axis,fixed=fixed,start=start,end=last+1))
                    start=None
                if start is None:
                    start=n
                last=n
            if start is not None:
                result.append(dict(axis=axis,fixed=fixed,start=start,end=last+1))
    return result


def build():
    rooms, corridors, vents = read_manifest()
    by_id={r["id"]:r for r in rooms}
    occupied=set()
    room_tiles={}
    for room in rooms:
        cells=rect_cells(room)
        room_tiles[room["id"]]=cells
        occupied.update(cells)
    result_corridors=[]
    for a,b in corridors:
        steps,bends=route_between(by_id[a],by_id[b],rooms)
        for x,y in steps:
            for dx in range(-CORRIDOR_RADIUS,CORRIDOR_RADIUS+1):
                for dy in range(-CORRIDOR_RADIUS,CORRIDOR_RADIUS+1):
                    occupied.add((x+dx,y+dy))

        # Each route must cross both intended room interiors.
        if steps[0] not in room_tiles[a] or steps[-1] not in room_tiles[b]:
            raise AssertionError(f"Lost corridor portals: {a}/{b}")
        result_corridors.append(dict(id=f"{a}__{b}", fromRoom=a,toRoom=b,
           points=[dict(x=p[0]/SCALE,y=p[1]/SCALE) for p in bends]))

    # Test actual traversable tiles, not just abstract 16-edge graph.
    root=(next(r for r in rooms if r["isSpawn"])["x"],
          next(r for r in rooms if r["isSpawn"])["y"])
    queue=deque([root])
    visited={root}
    while queue:
        x,y=queue.popleft()
        for p in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
            if p in occupied and p not in visited:
                visited.add(p)
                queue.append(p)
    if visited != occupied:
        raise ValueError(f"Disconnected geometry: {len(occupied)-len(visited)} unreachable cells")
    if any((r["x"],r["y"]) not in visited for r in rooms):
        raise AssertionError("Room center unreachable in generated geometry")

    walls=wall_runs(occupied)
    # Convert integer tile-edge coordinates to world coordinates.
    walls_out=[]
    for w in walls:
        if w["axis"] == "H":
            walls_out.append(dict(x1=w["start"]/SCALE, y1=(w["fixed"]-.5)/SCALE,
                                  x2=w["end"]/SCALE, y2=(w["fixed"]-.5)/SCALE))
        else:
            walls_out.append(dict(x1=(w["fixed"]-.5)/SCALE,y1=w["start"]/SCALE,
                                  x2=(w["fixed"]-.5)/SCALE,y2=w["end"]/SCALE))
    data=dict(version=LAYOUT_VERSION,cellSize=1/SCALE,roomWidth=(2*HALF_WIDTH+1)/SCALE,
       roomHeight=(2*HALF_HEIGHT+1)/SCALE,corridorWidth=(2*CORRIDOR_RADIUS+1)/SCALE,
       rooms=[dict(id=r["id"],name=r["name"],x=r["x"]/SCALE,y=r["y"]/SCALE,
                   taskId=r["taskId"],isSpawn=r["isSpawn"]) for r in rooms],
       corridors=result_corridors,
       vents=[dict(fromRoom=a,toRoom=b) for a,b in vents],
       walls=walls_out,
       diagnostics=dict(walkableCells=len(occupied),reachableCells=len(visited),
                        boundarySegments=len(walls),corridors=len(corridors)))
    return data


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",type=Path,default=DEFAULT_OUTPUT)
    parser.add_argument("--check",action="store_true",help="Verify existing checked-in layout is current")
    args=parser.parse_args()
    data=build()
    content=json.dumps(data,indent=2,ensure_ascii=False)+"\n"
    if args.check:
        if not args.output.is_file() or args.output.read_text(encoding="utf-8") != content:
            raise SystemExit("STALE production layout: run python tools/build_station_production_layout.py")
        print("PASS: 12 rooms, 16 navigable corridors, 3 vent plans; JSON matches C# manifest.")
    else:
        args.output.parent.mkdir(parents=True,exist_ok=True)
        args.output.write_text(content,encoding="utf-8")
        print(f"PARADOX STATION production layout -> {args.output}")
        print(data["diagnostics"])


if __name__=="__main__":
    main()
