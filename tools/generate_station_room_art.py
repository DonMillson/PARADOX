#!/usr/bin/env python3
"""Generate ELEVEN original layered PARADOX STATION concept rooms as SVG.

Arrival Bay has its own manually authored original layers. The eleven other
rooms are distinct illustrated level-art concepts for Unity prefab authoring.
This generator NEVER modifies the existing gameplay DLL, and does not create
an Among Us ShipStatus or claim a playable map.
"""
from __future__ import annotations

from html import escape
from math import cos, sin, pi
from pathlib import Path
import argparse
import hashlib
import random

SIZE = (1920, 1080)

ROOMS = [
    ("cargo", "CARGO HOLD", "#f5b75e", "LOGISTICS"),
    ("crew_quarters", "CREW QUARTERS", "#7ad5dd", "HABITAT"),
    ("security", "SECURITY DECK", "#e89b77", "SURVEILLANCE"),
    ("communications", "COMMUNICATIONS", "#79cfed", "UPLINK"),
    ("observation", "OBSERVATION DOME", "#a2a3ff", "TELESCOPE"),
    ("temporal_lab", "TEMPORAL LAB", "#bda7ff", "CHRONOLOGY"),
    ("medical", "MEDICAL BAY", "#8de6bb", "LIFE SUPPORT"),
    ("containment", "CONTAINMENT", "#f1a56c", "ISOLATION"),
    ("reactor_rift", "RIFT REACTOR", "#a8edfa", "POWER / RIFT"),
    ("power_core", "POWER CORE", "#efcb74", "ENERGY GRID"),
    ("void_chamber", "VOID CHAMBER", "#cf9ffb", "ANOMALY"),
]

def R(x,y,w,h,fill,stroke="#081a25",sw=4,rad=9):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rad}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>'

def C(x,y,r,fill,stroke="none",sw=0):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>'

def E(x,y,rx,ry,fill,stroke="none",sw=0):
    return f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>'

def P(d,fill="none",stroke="#314b5b",sw=5,extra=""):
    return f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" stroke-linejoin="round" {extra}/>'

def T(x,y,s,color="#c5e8e9",size=22,tracking=1):
    return f'<text x="{x}" y="{y}" fill="{color}" font-family="DejaVu Sans,sans-serif" font-weight="bold" font-size="{size}" letter-spacing="{tracking}">{escape(str(s))}</text>'

def wrap(body, accent):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="1920" height="1080" viewBox="0 0 1920 1080">
<defs>
<linearGradient id="deck" x1="0" y1="0" x2=".9" y2="1"><stop stop-color="#274553"/><stop offset=".5" stop-color="#162f3f"/><stop offset="1" stop-color="#0c1d2a"/></linearGradient>
<linearGradient id="metal" x1=".1" y1="0" x2=".9" y2="1"><stop stop-color="#526b78"/><stop offset=".42" stop-color="#2d4755"/><stop offset="1" stop-color="#152b3b"/></linearGradient>
<linearGradient id="device" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#66858d"/><stop offset=".26" stop-color="#304f5f"/><stop offset="1" stop-color="#102432"/></linearGradient>
<radialGradient id="glow"><stop stop-color="{accent}" stop-opacity=".42"/><stop offset=".45" stop-color="{accent}" stop-opacity=".12"/><stop offset="1" stop-color="{accent}" stop-opacity="0"/></radialGradient>
<radialGradient id="well"><stop stop-color="#0c1a2b"/><stop offset=".65" stop-color="#182b42"/><stop offset="1" stop-color="#07101b"/></radialGradient>
<pattern id="grate" width="26" height="26" patternUnits="userSpaceOnUse"><path d="M4 7h17M4 17h17" stroke="#8da6aa" stroke-opacity=".16" stroke-width="2"/></pattern>
<pattern id="hatch" width="40" height="40" patternUnits="userSpaceOnUse"><path d="M0 40L40 0" stroke="{accent}" opacity=".085" stroke-width="3"/></pattern>
</defs>{body}</svg>'''

def floor(room,accent,idx):
    out=[E(962,552,770,410,"#01070e")]
    out.append(P("M255 215 H1667 L1765 320 V814 L1670 911 H253 L152 812 V317 Z","url(#deck)","#05121d",18))
    out.append(P("M279 244 H1652 L1731 333 V801 L1654 877 H279 L189 804 V332 Z","none","#416a78",8))
    out.append(P("M302 266 H1626 L1700 345 V786 L1626 852 H300 L220 787 V349 Z","url(#grate)","#173c4b",3))
    palette=["#183444","#1b3948","#203d4c","#152e3f","#214453"]
    for y in range(295,823,78):
        for x in range(288+(y//78%2)*18,1643,120):
            n=(x//120+y//78+idx)%len(palette)
            out.append(R(x,y,112,70,palette[n],"#0b2634",3,7))
            out.append(P(f"M{x+13} {y+16}h78",stroke="#617e85",sw=2))
            for xx in (x+10,x+101):
                for yy in (y+11,y+61):
                    out.append(C(xx,yy,2.5,"#7595a2"))
            if (n==2):
                out.append(P(f"M{x+18} {y+57}h23m7 0h9",stroke=accent,sw=2))
    out.append(P("M680 391 H1240 L1317 472 V656 L1238 736 H681 L604 655 V470 Z","none",accent,9, 'opacity=".55"'))
    out.append(P("M701 408 H1220 L1292 482 V642 L1217 711 H704 L629 643 V485 Z","none","#527582",3))
    for x in (320,1600):
        out.append(R(x-21,400,42,290,"url(#hatch)",accent,3,7))
    # Station icon in deck. Similar orientation across all rooms.
    out.append(C(960,552,91,"#183b4c","#577a85",8))
    out.append(C(960,552,72,"#0b2733",accent,3))
    out.append(P("M960 510l33 20v44l-33 20l-33-20v-44Z","#244d5c",accent,4))
    out.append(T(923,560,f"{idx+2:02}",34,accent))
    out.append(T(338,825,"PARADOX // STATION",16,"#718f98"))
    return "".join(out)

def walls(label,accent,idx):
    o=[P("M238 213H1680L1790 315V836L1685 921H236L125 830V311Z","none","#07131d",40)]
    o.append(P("M238 220H1680L1782 318V832L1680 913H239L137 829V318Z","none","#577a85",22))
    o.append(P("M260 238H1659L1750 334V817L1657 883H264L167 814V336Z","none","#92aab0",5))
    o.append(P("M235 225H1679L1781 324",stroke=accent,sw=5,extra='opacity=".55"'))
    for k,x in enumerate(range(300,1670,142)):
        if 815<=x<=1090: continue
        o.append(R(x,181,120,52,"url(#metal)","#162c3b",6,7))
        o.append(R(x+12,194,94,17,"#203e4b","#7c9699",2,4))
        for z in range(4):
            o.append(P(f"M{x+24+z*21} 221v7",stroke="#83b5ba",sw=4))
    for xx in (178,1741):
        for yy in (335,810):
            o.append(C(xx,yy,19,"#163643","#9cb2ad",6))
            o.append(C(xx,yy,8,accent))
    o.append(P("M803 152H1114L1161 205V340L1110 387H809L760 340V211Z","#112431","#091621",16))
    o.append(P("M824 178H1094L1134 226V331L1093 364H826L786 331V226Z","url(#metal)","#71939d",8))
    o.append(P("M854 211H1063L1107 250V326L1063 342H856L813 325V251Z","#173744","#132b39",5))
    o.append(R(947,212,26,126,"#14313c","#78959d",4,3))
    o.append(R(827,145,264,39,"#103340",accent,3,3))
    o.append(T(840,172,label[:21],18,"#c2eeee",1))
    o.append(P("M863 905H1051",stroke=accent,sw=9))
    for x in range(859,1058,37):
        o.append(P(f"M{x} 918l23-30h12l-23 30Z",accent if x//37%2 else "#243f49","none",0))
    return "".join(o)

def screen(x,y,w,h,accent,seed=0,caption="SYS"):
    o=[R(x,y,w,h,"url(#device)","#071825",9,14),
       R(x+13,y+13,w-26,h-45,"#0c2d3c",accent,3,8),
       T(x+20,y+h-13,caption[:16],14,accent)]
    r=random.Random(seed+int(x)+int(y))
    for j in range(4):
        yy=y+31+j*18
        xx=x+24
        length=int((w-55)*(0.35+.55*r.random()))
        o.append(P(f"M{xx} {yy}h{length}",stroke=accent,sw=3,extra='opacity=".75"'))
    o.append(C(x+w-22,y+h-16,7,"#e9bc68"))
    return "".join(o)

def crate(x,y,size,accent,seed=0):
    return "".join([
        R(x,y,size,size*.83,"url(#metal)","#0b1c27",9,9),
        P(f"M{x+9} {y+17}h{size-19}v{size*.83-28}h{-size+19}Z","none","#7b9797",4),
        P(f"M{x+23} {y+25}l{size-46} {size*.83-47}m0 {-size*.83+47}l{-size+46} {size*.83-47}",stroke="#46626b",sw=7),
        R(x+size*.18,y+size*.62,size*.62,12,accent,"#122934",2,1),
    ])

def tube(x,y,w,h,accent,seed=0):
    o=[R(x,y,w,h,"#152d3a","#071825",10,24),
       R(x+15,y+12,w-30,h-24,"#234854","#6b909a",4,20),
       R(x+26,y+25,w-52,h-50,"url(#well)",accent,3,20),
       E(x+w/2,y+h/2,w*.27,h*.37,"url(#glow)"),
       P(f"M{x+30} {y+h*.23}H{x+w-30}M{x+30} {y+h*.77}H{x+w-30}",stroke="#93a4a6",sw=5)]
    for u in range(3):o.append(C(x+21+u*18,y+h-15,3,accent))
    return "".join(o)

def ring(cx,cy,r,accent,seed=0):
    o=[C(cx,cy,r,"#101d2d","#5a7682",18),C(cx,cy,r-22,"#132f3d",accent,8),C(cx,cy,r-50,"url(#well)",accent,4)]
    for k in range(24):
        a=k*2*pi/24
        x=cx+(r-12)*cos(a);y=cy+(r-12)*sin(a)
        o.append(C(round(x,1),round(y,1),5 if k%3==0 else 3,accent if k%2 else "#eab564"))
    o.append(E(cx,cy,r*.66,r*.66,"url(#glow)"))
    return "".join(o)

def radar(cx,cy,r,accent):
    o=[ring(cx,cy,r,accent),P(f"M{cx-r*.63} {cy+r*.63}L{cx+r*.63} {cy-r*.63}",stroke=accent,sw=4),
       P(f"M{cx} {cy-r+25}V{cy+r-25}M{cx-r+25} {cy}H{cx+r-25}",stroke="#6c999f",sw=2)]
    for a in (.9,2.8,4.6):
        o.append(C(round(cx+cos(a)*(r*.55),1),round(cy+sin(a)*(r*.55),1),10,"#e9b964"))
    return "".join(o)

def bunk(x,y,accent):
    o=[R(x,y,310,142,"url(#device)","#071924",10,20),
       R(x+13,y+13,284,115,"#284758","#668895",3,16),
       R(x+26,y+22,70,99,"#708a8c","#0d2530",5,14),
       R(x+104,y+18,179,105,"#3d6b74","#193d47",5,13),
       P(f"M{x+119} {y+42}H{x+274}M{x+121} {y+69}H{x+272}",stroke="#a4bdba",sw=3),
       R(x+23,y+130,263,7,accent,"#233b45",2,4)]
    return "".join(o)

def surgical_bed(x,y,accent):
    o=[R(x,y,272,146,"#1a3944","#081a26",12,27),
       R(x+24,y+16,222,119,"#486f76","#7f9da5",4,25),
       R(x+38,y+19,76,95,"#a7bfbf","#224653",5,20),
       R(x+126,y+25,101,84,"#2c5d65","#8fdcca",3,20)]
    for dx in (18,250):o.append(C(x+dx,y+117,11,"#1a333f",accent,4))
    return "".join(o)

def telescope(cx,cy,accent):
    o=[ring(cx,cy,185,accent),P(f"M{cx-80} {cy+90}L{cx+75} {cy-91}",stroke="#769fba",sw=26)]
    o.append(R(cx-110,cy-35,230,71,"url(#metal)",accent,6,26))
    o.append(C(cx+126,cy-71,65,"#223e5a",accent,9))
    o.append(C(cx+126,cy-71,40,"#09243f","#9ecfdb",6))
    o.append(E(cx+120,cy-80,39,39,"url(#glow)"))
    return "".join(o)

def cargo(accent):
    o=[R(420,380,1035,367,"#132d3b","#637d86",14,24)]
    for i in range(8):
        x=440+i*127
        o.append(R(x,395,114,340,"#203e4a","#385663",4,7))
        for y in (420,505,589,668):
            o.append(C(x+12,y,5,accent))
    for (x,y,s) in [(492,412,125),(632,423,112),(789,408,120),(1050,427,122),(1185,472,116),(523,605,100),(819,594,120),(1012,605,111)]:
        o.append(crate(x,y,s,accent))
    o.append(screen(1295,570,200,185,accent,7,"CARGO SCAN"))
    o.append(P("M350 749H1535",stroke="#dbb15f",sw=15,extra='stroke-dasharray="47 20"'))
    o.append(T(610,354,"FREIGHT BAY  /  CONTAINER HOLD",23,accent))
    return "".join(o)

def crew(accent):
    o=[bunk(385,372,accent),bunk(1185,372,accent),bunk(380,666,accent),bunk(1185,666,accent)]
    o.append(R(785,432,350,206,"url(#metal)","#0a1e29",12,30))
    o.append(R(814,462,291,147,"#254b58","#96a8b0",4,22))
    o.append(E(964,535,119,55,"#3c6f7b"))
    for x in (825,1104):
        for y in (481,584):
            o.append(C(x,y,8,accent))
    for y in (379,677):
        for x in (325,1545):
            o.append(R(x,y,40,150,"#15313c","#4d7480",5,12))
    o.append(T(809,687,"HABITAT / CREW",21,accent))
    return "".join(o)

def security(accent):
    o=[R(356,338,1210,401,"#142f3c","#627985",10,26)]
    for row in range(2):
        for col in range(5):
            x=384+col*226;y=372+row*168
            o.append(screen(x,y,205,145,accent,row*11+col,"CAM "+str(row*5+col+1).zfill(2)))
    o.append(R(742,758,440,96,"url(#metal)","#132a38",9,35))
    for i in range(8):
        o.append(C(810+i*43,794,10,accent if i%3==0 else "#678894"))
    o.append(T(786,835,"SECURITY MAINFRAME",20,accent))
    return "".join(o)

def communications(accent):
    o=[radar(985,560,235,accent)]
    o.append(P("M985 270V859",stroke="#7b9ca7",sw=16))
    o.append(P("M668 560H1302",stroke="#6b9ba6",sw=7))
    for x in (375,1307):o.append(screen(x,410,230,275,accent,int(x),"UPLINK"))
    o.append(R(785,779,430,54,"url(#metal)",accent,5,12))
    for i in range(12):
        o.append(R(812+i*33,794,21,24,"#2f6979" if i%3==0 else "#203b4b",accent,2,3))
    o.append(T(770,297,"DEEP SPACE ARRAY  /  SIGNAL LOCK",22,accent))
    return "".join(o)

def observation(accent):
    o=[E(950,550,315,295,"#0c1831","#7587b9",18)]
    for i in range(45):
        rng=random.Random(i*191)
        x=660+int(rng.random()*575);y=308+int(rng.random()*510)
        if ((x-950)/306)**2+((y-550)/286)**2<1:
            o.append(C(x,y,1.7+rng.random()*2.9,"#e9f5ff"))
    for r in (105,173,240):o.append(E(950,550,r,r,"none","#5d7ea2",3))
    o.append(P("M950 273V826M650 551H1250",stroke=accent,sw=4,extra='opacity=".6"'))
    o.append(telescope(950,550,accent))
    for x in (385,1330):o.append(screen(x,458,220,189,accent,3,"OPTICAL FEED"))
    o.append(T(800,290,"COSMIC SURVEY",24,accent))
    return "".join(o)

def temporal(accent):
    o=[ring(948,535,218,accent)]
    for r in (118,156,194):
        o.append(C(948,535,r,"none","#7576a6",6))
    for i in range(14):
        a=2*pi*i/14
        x=round(948+180*cos(a));y=round(535+180*sin(a))
        o.append(C(x,y,18,"url(#metal)",accent,5))
    o.append(P("M740 535H1156M948 320V750",stroke=accent,sw=5))
    o.append(E(947,535,91,112,"url(#glow)"))
    o.append(screen(369,441,216,263,accent,25,"CLOCK SYNC"))
    o.append(screen(1333,444,210,256,accent,26,"TEMPORAL FLUX"))
    o.append(T(760,310,"CHRONAL OSCILLATOR",25,accent))
    return "".join(o)

def medical(accent):
    o=[surgical_bed(365,410,accent),surgical_bed(1290,410,accent),surgical_bed(370,651,accent),surgical_bed(1282,654,accent)]
    o.append(ring(959,554,158,accent))
    o.append(C(959,554,75,"#285c62","#9ce8d1",9))
    o.append(P("M943 509V599M914 553H1004",stroke="#cefff0",sw=15))
    o.append(screen(767,314,387,130,accent,12,"DIAGNOSTICS"))
    o.append(T(814,788,"TRIAGE CONTROL",22,accent))
    return "".join(o)

def containment(accent):
    o=[R(342,363,1254,431,"#101f2d","#795d57",18,33)]
    for x in (422,717,1015,1310):
        o.append(tube(x,395,199,327,accent))
        o.append(T(x+24,744,f"POD {1+(x-422)//295:02}",16,accent))
    o.append(P("M357 785H1582",stroke="#ecae57",sw=20,extra='stroke-dasharray="44 22"'))
    for y in (395,705):
        o.append(P(f"M357 {y}H1565",stroke=accent,sw=4))
    o.append(T(660,332,"QUARANTINE / LEVEL 07",26,accent))
    return "".join(o)

def reactor(accent):
    o=[ring(958,559,245,accent),ring(958,559,158,accent)]
    o.append(E(958,559,105,105,"url(#glow)"))
    for i in range(14):
        a=i*2*pi/14;x=958+187*cos(a);y=559+187*sin(a)
        o.append(P(f"M{round(x,1)} {round(y,1)}l{round(39*cos(a),1)} {round(39*sin(a),1)}",stroke=accent,sw=12))
    o.append(P("M348 430H679V471H734M1172 479H1230V430H1565",stroke="#78a6b0",sw=23))
    o.append(P("M346 691H666V653H732M1174 650H1237V691H1565",stroke="#78a6b0",sw=23))
    o.append(screen(385,482,225,160,accent,44,"RIFT CONTROL"))
    o.append(screen(1319,483,223,160,accent,51,"CONTAINMENT"))
    o.append(T(752,293,"RIFT FIELD / UNSTABLE",24,accent))
    return "".join(o)

def power(accent):
    o=[ring(958,555,181,accent)]
    o.append(P("M960 310V804",stroke="#b19a55",sw=20))
    o.append(P("M695 555H1217",stroke="#e0bc65",sw=17))
    for x in (350,612,1280,1500):
        o.append(tube(x,414,90,310,accent))
        for i in range(6):o.append(P(f"M{x+18} {440+i*41}h51",stroke=accent if i%2 else "#9f9a6b",sw=8))
    for y in (380,740):
        o.append(P(f"M420 {y}H1510",stroke=accent,sw=11,extra='stroke-dasharray="21 21"'))
    o.append(T(760,298,"PRIMARY FUSION GRID",24,accent))
    return "".join(o)

def void(accent):
    o=[ring(954,552,266,accent),E(955,553,198,200,"url(#well)")]
    for k in range(8):
        a=k*pi/4
        r=100+k*12
        o.append(P(f"M{round(954+r*cos(a))} {round(552+r*sin(a))} q{round(50*sin(a))} {round(-90*cos(a))} {round(110*cos(a))} {round(94*sin(a))}",stroke=accent,sw=4,extra='opacity=".74"'))
    o.append(E(956,552,105,150,"url(#glow)"))
    for i in range(24):
        r=random.Random(i*719)
        x=690+r.randint(0,550);y=300+r.randint(0,500)
        o.append(C(x,y,2+r.randint(0,6),"#9b88db"))
    o.append(screen(363,430,218,245,accent,91,"VOID SIGNAL"))
    o.append(screen(1339,431,218,246,accent,93,"GRAVITY"))
    o.append(T(763,285,"ISOLATE THE UNKNOWN",24,accent))
    return "".join(o)

FEATURES = {
    "cargo":cargo,"crew_quarters":crew,"security":security,
    "communications":communications,"observation":observation,
    "temporal_lab":temporal,"medical":medical,"containment":containment,
    "reactor_rift":reactor,"power_core":power,"void_chamber":void,
}

def lights(room,accent,idx):
    o=[]
    for x,y,rad in ((350,286,150),(1570,289,170),(337,825,180),(1589,805,155)):
        o.append(E(x,y,rad,rad*.82,"url(#glow)"))
        o.append(R(x-35,y-7,70,14,accent,accent,0,6))
    o.append(E(955,550,305,294,"url(#glow)"))
    for i in range(11):
        x=340+i*127
        o.append(C(x,263,4 if i%2 else 6,accent))
    return "".join(o)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",type=Path,default=Path(__file__).resolve().parent.parent/"art"/"station-generated")
    args=parser.parse_args()
    args.output.mkdir(parents=True,exist_ok=True)
    for idx,(room,name,accent,category) in enumerate(ROOMS):
        target=args.output/room
        target.mkdir(parents=True,exist_ok=True)
        data={
            "01_floor.svg":wrap(floor(room,accent,idx+1),accent),
            "02_walls.svg":wrap(walls(name,accent,idx+1),accent),
            "03_props.svg":wrap(FEATURES[room](accent),accent),
            "04_lights.svg":wrap(lights(room,accent,idx+1),accent)
        }
        for filename,content in data.items():
            if len(content)<500:
                raise AssertionError(f"{room} {filename}: layer insufficient")
            (target/filename).write_text(content,encoding="utf-8")
        print(f"{room:<16} {category:<18} 4 unique illustrated layers")
    print(f"Generated {len(ROOMS)} distinct room sources and {len(ROOMS)*4} SVG layers.")

if __name__=="__main__":
    main()
