"""PARADOX AMONG US — online Windows installer/launcher.

The PyInstaller --onefile build embeds Paradox.dll and an audited installer.
Verified, pinned BepInEx IL2CPP and Reactor are downloaded over HTTPS.
On-disk installation and actual plugin startup are verified separately.
All UI controls are thread-safe through a queue.
"""
from __future__ import annotations

import os
import queue
import random
import re
import subprocess
import sys
import threading
import tempfile

from launcher_python.online_dependencies import stage_live_dependencies
from launcher_python.lobby_config import write_lobby_config
from launcher_python.mod_health import inspect_install, diagnosis_text
from pathlib import Path
import tkinter as tk
from tkinter import filedialog, messagebox

import customtkinter as ctk

APP_TITLE = "PARADOX AMONG US"
MOD_VERSION = "0.7.0-dev.6"
BACKGROUND = "#080D1B"
CARD = "#141E35"
CARD_ALT = "#1C2941"
TEXT = "#EAF6FF"
MUTED = "#91A9C5"
CYAN = "#4CE6F5"
PURPLE = "#AD86FE"
RED = "#F55073"
GREEN = "#78F4C3"

ctk.set_appearance_mode("Dark")
ctk.set_default_color_theme("blue")


def bundle_root() -> Path:
    return Path(getattr(sys, "_MEIPASS", Path(__file__).resolve().parent))


def bundled_mod_dll() -> Path:
    return bundle_root() / "mod" / "Paradox.dll"


def install_backend() -> Path:
    return bundle_root() / "installer" / "Install-Paradox.ps1"


def uninstall_backend() -> Path:
    return bundle_root() / "installer" / "Uninstall-Paradox.ps1"


def valid_game(folder: Path) -> bool:
    return all((
        (folder / "Among Us.exe").is_file(),
        (folder / "GameAssembly.dll").is_file(),
        (folder / "Among Us_Data").is_dir(),
    ))


def pe_machine(executable: Path) -> int | None:
    try:
        with executable.open("rb") as handle:
            if handle.read(2) != b"MZ":
                return None
            handle.seek(0x3C)
            offset = int.from_bytes(handle.read(4), "little")
            if offset < 0x40 or offset > executable.stat().st_size - 6:
                return None
            handle.seek(offset)
            if handle.read(4) != b"PE\0\0":
                return None
            return int.from_bytes(handle.read(2), "little")
    except (OSError, ValueError):
        return None


def _steam_roots() -> list[Path]:
    roots: list[Path] = []
    for key in ("ProgramFiles(x86)", "ProgramFiles"):
        value = os.environ.get(key)
        if value:
            roots.append(Path(value) / "Steam")
    if os.name == "nt":
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam") as registry:
                root, _ = winreg.QueryValueEx(registry, "SteamPath")
                roots.insert(0, Path(root))
        except OSError:
            pass
    roots += [Path(f"{drive}:/SteamLibrary") for drive in "CDEFGHIJKLMNOPQRSTUVWXYZ"]
    roots += [Path(f"{drive}:/Steam") for drive in "CDEFGHIJKLMNOPQRSTUVWXYZ"]
    return roots


def is_steam_install(folder: Path) -> bool:
    parts = {part.casefold() for part in folder.parts}
    return "steamapps" in parts and "common" in parts


def detect_among_us() -> str:
    visited: set[str] = set()
    for steam in _steam_roots():
        libraries = [steam]
        vdf = steam / "steamapps" / "libraryfolders.vdf"
        try:
            raw = vdf.read_text("utf-8-sig", errors="replace")
            libraries += [Path(p.replace("\\\\", "\\"))
                          for p in re.findall(r'"path"\s+"([^"]+)"', raw)]
        except OSError:
            pass
        for library in libraries:
            if not str(library).strip() or str(library).lower() in visited:
                continue
            visited.add(str(library).lower())
            folder = library / "steamapps" / "common" / "Among Us"
            if valid_game(folder):
                return str(folder)
    return ""


def new_challenge() -> tuple[str, int]:
    op = random.choice(["+", "-", "×", "^"])
    a = random.randint(1, 50)
    b = random.randint(2, 3) if op == "^" else random.randint(1, 50)
    result = {"+": lambda: a + b, "-": lambda: a - b,
              "×": lambda: a * b, "^": lambda: a ** b}[op]()
    return f"{a} {op} {b} = ?", result


class ParadoxLauncher(ctk.CTk):
    def __init__(self) -> None:
        super().__init__()
        self.title("PARADOX AMONG US • Launcher / Installer")
        self.geometry("1000x840")
        self.minsize(880, 770)
        self.configure(fg_color=BACKGROUND)
        self.protocol("WM_DELETE_WINDOW", self.on_close)
        self.stage = 0
        self.hits = 0
        self.hunt_passed = False
        self.math_passed = False
        self.problem, self.answer = new_challenge()
        self.game_dir = tk.StringVar(value=detect_among_us())
        self.large_lobby_enabled = tk.BooleanVar(value=False)
        self.large_lobby_capacity = tk.StringVar(value="50")
        self.events: queue.Queue[tuple] = queue.Queue()
        self.worker_busy = False
        self.installed_after: float | None = None
        self.animation_id: str | None = None
        self.board: tk.Canvas | None = None
        self.px, self.py, self.dx, self.dy = 125.0, 118.0, 3.25, 2.3
        self._make_frame()
        self.show_stage(0)
        self.after(70, self.check_events)

    def _make_frame(self) -> None:
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=37, pady=(24, 18))
        ctk.CTkLabel(header, text="◈  PARADOX", font=ctk.CTkFont(size=32, weight="bold"),
                     text_color=CYAN).pack(side="left")
        ctk.CTkLabel(header, text="AMONG US  /  LAUNCHER", text_color=PURPLE,
                     font=ctk.CTkFont(size=16, weight="bold")).pack(side="right", pady=12)
        self.headerline = ctk.CTkFrame(self, fg_color=CYAN, height=2)
        self.headerline.pack(fill="x", padx=37)
        self.steps = ctk.CTkFrame(self, fg_color=CARD, corner_radius=13)
        self.steps.pack(fill="x", padx=37, pady=(16, 15))
        self.status_items: list[ctk.CTkLabel] = []
        for title in ("01  MISJA", "02  KOD", "03  INSTALACJA"):
            lbl = ctk.CTkLabel(self.steps, text=title, text_color=MUTED,
                               font=ctk.CTkFont(size=14, weight="bold"))
            lbl.pack(side="left", expand=True, pady=12)
            self.status_items.append(lbl)
        self.content = ctk.CTkFrame(self, fg_color=CARD, corner_radius=18)
        self.content.pack(fill="both", expand=True, padx=37, pady=(0, 13))
        self.content.pack_propagate(False)
        foot = ctk.CTkFrame(self, fg_color="transparent")
        foot.pack(fill="x", padx=39, pady=(0, 15))
        ctk.CTkLabel(foot, text="PARADOX by DonMillson  •  Installer online + weryfikacja",
                     text_color=MUTED, font=ctk.CTkFont(size=12)).pack(side="left")
        ctk.CTkLabel(foot, text="Among Us 19 / Windows x64 • test build",
                     text_color=PURPLE, font=ctk.CTkFont(size=12)).pack(side="right")

    def show_stage(self, stage: int) -> None:
        if self.animation_id:
            self.after_cancel(self.animation_id)
            self.animation_id = None
        self.board = None
        for child in self.content.winfo_children():
            child.destroy()
        self.stage = stage
        for idx, label in enumerate(self.status_items, start=1):
            label.configure(text_color=GREEN if stage > idx else
                            CYAN if stage == idx else MUTED)
        if stage == 0:
            self.welcome()
        elif stage == 1:
            self.hunt()
        elif stage == 2:
            self.math()
        else:
            self.install_panel()

    def title_block(self, name: str, detail: str) -> None:
        ctk.CTkLabel(self.content, text=name, text_color=TEXT,
                     font=ctk.CTkFont(size=27, weight="bold")).pack(
            anchor="w", padx=30, pady=(23, 5))
        ctk.CTkLabel(self.content, text=detail, text_color=MUTED, justify="left",
                     wraplength=818, font=ctk.CTkFont(size=14)).pack(
            anchor="w", padx=30, pady=(0, 13))

    def welcome(self) -> None:
        self.title_block("PARADOX  /  NOWA MISJA",
                         "Among Us spotyka PARADOX: załoga, sabotaż i niestabilna rzeczywistość. "
                         "Trzy etapy przed uruchomieniem modyfikacji.")
        panel = ctk.CTkFrame(self.content, fg_color="#0B142B", corner_radius=18)
        panel.pack(fill="both", expand=True, padx=31, pady=(8, 14))
        picture = tk.Canvas(panel, height=250, bg="#0B142B", highlightthickness=0)
        picture.pack(fill="x", padx=20, pady=(19, 0))
        picture.update_idletasks()
        w = max(650, picture.winfo_width())
        cx = w / 2
        for r, accent in ((104, "#284367"), (93, PURPLE), (76, "#275A77")):
            picture.create_oval(cx-r, 110-r, cx+r, 110+r,
                                outline=accent, width=2)
        # Original spacesuit silhouette: recognisably Among Us-inspired,
        # with PARADOX's luminous visor and unstable violet orbital field.
        picture.create_oval(cx-72, 155, cx+70, 181, fill="#0A1227", outline="")
        picture.create_polygon(cx-46, 76, cx-70, 79, cx-76, 135,
                               cx-45, 147, fill="#792C63", outline=PURPLE, width=3)
        picture.create_polygon(cx-47, 115, cx-36, 115, cx-37, 180,
                               cx-7, 180, cx-5, 134, cx+8, 134,
                               cx+13, 180, cx+43, 180, cx+47, 95,
                               cx+29, 51, cx-18, 49, cx-45, 77,
                               fill="#DC365E", outline="#F476AA",
                               width=3, smooth=True)
        picture.create_oval(cx-13, 70, cx+51, 111, fill="#23496A",
                            outline="#6BDEFF", width=4)
        picture.create_oval(cx-9, 74, cx+45, 104, fill="#8CE7F8", outline="")
        picture.create_oval(cx+2, 77, cx+24, 85, fill="#EEFFFF", outline="")
        picture.create_line(cx-91, 117, cx-68, 94, fill=CYAN, width=3)
        picture.create_line(cx+65, 45, cx+82, 63, fill=PURPLE, width=3)
        picture.create_text(cx, 222, text="PARADOX  //  AMONG US",
                            fill=TEXT, font=("Segoe UI", 21, "bold"))
        ctk.CTkLabel(panel, text="IMPOSTOR HUNT   /   AUTORYZACJA   /   INSTALACJA",
                     text_color=PURPLE, font=ctk.CTkFont(size=13, weight="bold")
                     ).pack(pady=(8, 10))
        ctk.CTkButton(panel, text="ROZPOCZNIJ MISJĘ  →", height=49, width=280,
                      fg_color=CYAN, hover_color="#2AB5C8", text_color="#08101F",
                      font=ctk.CTkFont(size=17, weight="bold"),
                      command=lambda: self.show_stage(1)).pack(pady=(7, 5))
        ctk.CTkButton(panel, text="SPRAWDŹ INSTALACJĘ  /  LOGI",
                      fg_color="#273854", hover_color="#3A527C",
                      text_color=TEXT, width=280, height=39,
                      command=self.quick_diagnostics).pack(pady=(3, 14))

    def hunt(self) -> None:
        self.title_block("MISJA 01  /  ZŁAP IMPOSTORA",
                         "Kliknij poruszającą się postać 5 razy, aby uzyskać kod autoryzacji.")
        frame = ctk.CTkFrame(self.content, fg_color="#0B1428", corner_radius=11)
        frame.pack(padx=27)
        self.board = tk.Canvas(frame, width=785, height=327, bg="#0B1428",
                               highlightthickness=0, cursor="crosshair")
        self.board.pack(padx=6, pady=6)
        rng = random.Random(2026)
        for _ in range(90):
            x, y = rng.randrange(785), rng.randrange(327)
            self.board.create_oval(x, y, x+2, y+2, fill="#3E577B", outline="")
        self.px, self.py, self.dx, self.dy = 130.0, 120.0, 3.2, 2.0
        self.draw_impostor()
        self.board.tag_bind("target", "<Button-1>", self.catch)
        buttons = ctk.CTkFrame(self.content, fg_color="transparent")
        buttons.pack(fill="x", padx=37, pady=20)
        self.hits_text = ctk.CTkLabel(buttons, text=f"Złapano: {self.hits}/5",
                                      text_color=CYAN,
                                      font=ctk.CTkFont(size=18, weight="bold"))
        self.hits_text.pack(side="left")
        self.hunt_next = ctk.CTkButton(buttons, text="KOD AUTORYZACJI  →", width=250,
                                       state="normal" if self.hunt_passed else "disabled",
                                       fg_color=PURPLE, text_color="#091225",
                                       command=lambda: self.show_stage(2))
        self.hunt_next.pack(side="right")
        if not self.hunt_passed:
            self.move_target()

    def draw_impostor(self) -> None:
        c = self.board
        if c is None:
            return
        x, y = self.px, self.py
        c.create_oval(x-43, y+37, x+40, y+66, fill="#060E20",
                      outline="", tags="target")
        c.create_rectangle(x-48, y-18, x-25, y+37, fill="#74275B",
                           outline=PURPLE, width=3, tags="target")
        c.create_rectangle(x-29, y+19, x-8, y+55, fill=RED,
                           outline="#7D2444", width=3, tags="target")
        c.create_rectangle(x+11, y+19, x+32, y+55, fill=RED,
                           outline="#7D2444", width=3, tags="target")
        c.create_oval(x-34, y-50, x+35, y+45, fill="#DA315A",
                      outline="#FF6790", width=3, tags="target")
        c.create_oval(x-12, y-30, x+38, y+3, fill="#245274",
                      outline=CYAN, width=3, tags="target")
        c.create_oval(x-8, y-25, x+32, y-2, fill="#8CDEF4",
                      outline="", tags="target")
        c.create_oval(x-2, y-23, x+16, y-17, fill="#F2FFFF",
                      outline="", tags="target")

    def move_target(self) -> None:
        if self.board is None or self.stage != 1 or self.hunt_passed:
            return
        x, y = self.px + self.dx, self.py + self.dy
        if x < 65 or x > 725:
            self.dx *= -1
            x = self.px + self.dx
        if y < 64 or y > 260:
            self.dy *= -1
            y = self.py + self.dy
        self.board.move("target", x-self.px, y-self.py)
        self.px, self.py = x, y
        self.animation_id = self.after(24, self.move_target)

    def catch(self, _event: tk.Event) -> None:
        if self.board is None or self.hunt_passed:
            return
        self.hits += 1
        self.hits_text.configure(text=f"Złapano: {self.hits}/5")
        if self.hits == 5:
            self.hunt_passed = True
            self.hits_text.configure(text="✓  IMPOSTOR ZŁAPANY", text_color=GREEN)
            self.hunt_next.configure(state="normal")
            if self.animation_id:
                self.after_cancel(self.animation_id)
                self.animation_id = None
        else:
            nx, ny = random.randint(85, 700), random.randint(75, 245)
            self.board.move("target", nx-self.px, ny-self.py)
            self.px, self.py = float(nx), float(ny)
            self.dx *= 1.07
            self.dy *= -1.07

    def math(self) -> None:
        if not self.hunt_passed:
            self.show_stage(1)
            return
        self.title_block("MISJA 02  /  KOD DOSTĘPU",
                         "Rozwiąż losowe równanie. Dopiero prawidłowa odpowiedź odblokuje instalację.")
        center = ctk.CTkFrame(self.content, fg_color=CARD_ALT, corner_radius=16)
        center.pack(fill="x", padx=62, pady=(35, 20))
        ctk.CTkLabel(center, text="ZASZYFROWANY KOD", text_color=PURPLE,
                     font=ctk.CTkFont(size=13, weight="bold")).pack(pady=(29, 6))
        ctk.CTkLabel(center, text=self.problem, text_color=TEXT,
                     font=ctk.CTkFont(family="Consolas", size=41, weight="bold")
                     ).pack(pady=(7, 18))
        self.answer_var = tk.StringVar()
        entry = ctk.CTkEntry(center, textvariable=self.answer_var,
                             placeholder_text="Wynik", width=295, height=49,
                             font=ctk.CTkFont(size=19), justify="center")
        entry.pack(pady=(0, 17))
        entry.bind("<Return>", lambda _e: self.verify_answer())
        self.answer_entry = entry
        ctk.CTkButton(center, text="POTWIERDŹ KOD", width=210, height=45,
                      fg_color=CYAN, text_color="#091225",
                      command=self.verify_answer).pack(pady=(0, 12))
        self.math_message = ctk.CTkLabel(center, text="Wpisz wynik liczbowy.",
                                         text_color=MUTED)
        self.math_message.pack(pady=(0, 24))
        self.math_next = ctk.CTkButton(self.content, text="PRZEJDŹ DO INSTALACJI  →",
                                       height=47, width=265, state="normal"
                                       if self.math_passed else "disabled",
                                       fg_color=PURPLE, text_color="#091225",
                                       command=lambda: self.show_stage(3))
        self.math_next.pack(pady=13)
        entry.focus_set()

    def verify_answer(self) -> None:
        if self.math_passed:
            return
        try:
            answer = int(self.answer_var.get().strip())
        except ValueError:
            self.math_message.configure(text="Wpisz liczbę całkowitą.", text_color=RED)
            return
        if answer == self.answer:
            self.math_passed = True
            self.answer_entry.configure(state="disabled")
            self.math_message.configure(text="✓  Autoryzacja zakończona", text_color=GREEN)
            self.math_next.configure(state="normal")
        else:
            self.math_message.configure(text="✕  Błędny kod, spróbuj ponownie.", text_color=RED)

    def install_panel(self) -> None:
        if not self.hunt_passed or not self.math_passed:
            self.show_stage(1 if not self.hunt_passed else 2)
            return
        self.title_block("MISJA 03  /  INSTALACJA",
                         "Wybierz folder gry. BepInEx i zgodny Reactor pobiorę teraz z oficjalnych serwerów.")
        form = ctk.CTkFrame(self.content, fg_color=CARD_ALT, corner_radius=13)
        form.pack(fill="x", padx=30, pady=(12, 19))
        ctk.CTkLabel(form, text="KATALOG Z GRĄ AMONG US", text_color=MUTED,
                     font=ctk.CTkFont(size=13, weight="bold")
                     ).pack(anchor="w", padx=21, pady=(20, 8))
        line = ctk.CTkFrame(form, fg_color="transparent")
        line.pack(fill="x", padx=21, pady=(0, 14))
        ctk.CTkEntry(line, textvariable=self.game_dir, height=42,
                     font=ctk.CTkFont(size=13)).pack(side="left", fill="x",
                                                     expand=True, padx=(0, 10))
        ctk.CTkButton(line, text="PRZEGLĄDAJ", command=self.browse_game,
                      width=132, height=42, fg_color="#395070").pack(side="right")
        ctk.CTkLabel(form, text="ONLINE: BepInEx IL2CPP x64  •  Reactor CI400  •  PARADOX",
                     text_color=GREEN, font=ctk.CTkFont(size=13)
                     ).pack(anchor="w", padx=21, pady=(0, 7))

        lobby = ctk.CTkFrame(form, fg_color="#101C36", corner_radius=10)
        lobby.pack(fill="x", padx=18, pady=(1, 15))
        self.large_lobby_checkbox = ctk.CTkCheckBox(
            lobby,
            text="Eksperymentalne duże lobby (tylko prywatny serwer modowany)",
            variable=self.large_lobby_enabled,
            text_color=TEXT, font=ctk.CTkFont(size=12),
            command=self.update_lobby_toggle)
        self.large_lobby_checkbox.pack(anchor="w", padx=12, pady=(12, 5))
        select_line = ctk.CTkFrame(lobby, fg_color="transparent")
        select_line.pack(fill="x", padx=12, pady=(0, 11))
        ctk.CTkLabel(select_line, text="Maksymalnie graczy:", text_color=MUTED
                     ).pack(side="left", padx=(0, 15))
        self.large_lobby_picker = ctk.CTkOptionMenu(
            select_line, values=["15", "20", "30", "40", "50"],
            variable=self.large_lobby_capacity, width=95,
            state="disabled", fg_color="#344A73")
        self.large_lobby_picker.pack(side="left")
        ctk.CTkLabel(select_line, text="Wymaga zgodnego serwera; nie działa na oficjalnych.",
                     text_color=MUTED, font=ctk.CTkFont(size=11)
                     ).pack(side="left", padx=12)

        self.progress = ctk.CTkProgressBar(self.content, height=17,
                                           progress_color=CYAN, fg_color="#263550")
        self.progress.pack(fill="x", padx=33, pady=(24, 3))
        self.progress.set(0)
        self.progress_status = ctk.CTkLabel(self.content, text="GOTOWY  /  0%",
                                            text_color=CYAN,
                                            font=ctk.CTkFont(size=16, weight="bold"))
        self.progress_status.pack(pady=(9, 13))
        self.install_button = ctk.CTkButton(self.content, text="ZAINSTALUJ MODA",
                                            width=285, height=53, fg_color=CYAN,
                                            text_color="#07111D",
                                            font=ctk.CTkFont(size=18, weight="bold"),
                                            command=self.start_install)
        self.install_button.pack(pady=(11, 9))
        actions = ctk.CTkFrame(self.content, fg_color="transparent")
        actions.pack(pady=(4, 4))
        self.launch_button = ctk.CTkButton(actions, text="▶  URUCHOM AMONG US",
                                           width=230, height=40, state="disabled",
                                           fg_color="#365276", command=self.launch_game)
        self.launch_button.pack(side="left", padx=7)
        ctk.CTkButton(actions, text="✓  SPRAWDŹ MODY", width=210, height=40,
                      fg_color="#60448D", hover_color="#805AA9",
                      command=self.check_mods).pack(side="left", padx=7)
        self.diagnostic_status = ctk.CTkLabel(
            self.content, text="STATUS MODA: NIEZWERYFIKOWANY",
            text_color=PURPLE, font=ctk.CTkFont(size=12, weight="bold"))
        self.diagnostic_status.pack(pady=(1, 3))
        folder = Path(self.game_dir.get().strip())
        if valid_game(folder):
            status = inspect_install(folder, expected_mod=bundled_mod_dll())
            if status.state != "broken":
                self.launch_button.configure(state="normal")
            self.show_diagnostic_state(status, popup=False)

    def update_lobby_toggle(self) -> None:
        self.large_lobby_picker.configure(
            state="normal" if self.large_lobby_enabled.get() else "disabled")

    def browse_game(self) -> None:
        found = filedialog.askdirectory(title="Wybierz katalog z Among Us.exe",
                                        parent=self)
        if found:
            self.game_dir.set(found)

    def set_progress(self, amount: float, message: str) -> None:
        if self.stage != 3:
            return
        self.progress.set(max(0.0, min(1.0, amount)))
        self.progress_status.configure(text=f"{message}   /   {int(amount * 100)}%")

    def start_install(self) -> None:
        if self.worker_busy or not (self.hunt_passed and self.math_passed):
            return
        game = Path(self.game_dir.get().strip()).expanduser()
        if not valid_game(game):
            messagebox.showerror("Nieprawidłowa ścieżka",
                                 "Wskaż folder zawierający Among Us.exe, GameAssembly.dll i Among Us_Data.",
                                 parent=self)
            return
        if pe_machine(game / "Among Us.exe") != 0x8664:
            messagebox.showerror("Nieobsługiwana gra",
                                 "Instalator obsługuje Among Us x64.", parent=self)
            return
        if not bundled_mod_dll().is_file():
            messagebox.showerror("Brak moda",
                                 "Nie znaleziono Paradox.dll w programie. "
                                 "Pobierz oficjalny launcher PARADOX z GitHub Actions.",
                                 parent=self)
            return
        if not install_backend().is_file():
            messagebox.showerror("Brak instalatora",
                                 "Brakuje skryptu backendu instalacji.", parent=self)
            return
        enabled = bool(self.large_lobby_enabled.get())
        maximum = int(self.large_lobby_capacity.get()) if enabled else 15
        message = ("Pobrać biblioteki i zainstalować PARADOX?\n" +
                   ("Włączysz eksperymentalny limit " + str(maximum) + " graczy. "
                    "Wymaga prywatnego, kompatybilnego serwera.\n"
                    if enabled else
                    "Wielkość lobby pozostanie standardowa (15 graczy).\n") +
                   "Istniejące pliki otrzymają kopie zapasowe.")
        if not messagebox.askyesno("Potwierdzenie", message, parent=self):
            return
        self.worker_busy = True
        self.installed_after = None
        self.install_button.configure(state="disabled")
        self.set_progress(0.02, "Łączenie z serwerami")
        support = game / ".paradox-installer"
        threading.Thread(target=self.install_worker,
                         args=(game, support, enabled, maximum),
                         daemon=True, name="ParadoxInstallWorker").start()

    def install_worker(self, game: Path, support: Path,
                       experimental_enabled: bool, capacity: int) -> None:
        try:
            import time
            started = time.time()
            with tempfile.TemporaryDirectory(prefix="paradox-verified-") as working:
                payload = Path(working) / "payload"
                payload.mkdir()
                stage_live_dependencies(
                    payload, bundled_mod_dll(),
                    lambda fraction, message:
                        self.events.put(("progress", fraction, message))
                )
                command = ["powershell.exe", "-NoProfile", "-NonInteractive",
                           "-ExecutionPolicy", "Bypass", "-File", str(install_backend()),
                           "-GamePath", str(game), "-PayloadRoot", str(payload),
                           "-SupportRoot", str(support)]
                startup = getattr(subprocess, "CREATE_NO_WINDOW", 0)
                process = subprocess.Popen(command, stdout=subprocess.PIPE,
                                           stderr=subprocess.STDOUT, text=True,
                                           encoding="utf-8", errors="replace",
                                           creationflags=startup)
                output: list[str] = []
                assert process.stdout is not None
                for line in process.stdout:
                    output.append(line)
                    marker = re.search(r"PARADOX_PROGRESS:(\d+)/(\d+)", line)
                    if marker:
                        amount = int(marker[1]) / max(int(marker[2]), 1)
                        self.events.put(("progress", min(.99, .60 + .39 * amount),
                                         "Instaluję pobrane komponenty"))
                if process.wait() != 0:
                    raise RuntimeError("\n".join(output[-14:]).strip() or
                                       "Instalacja przerwana (PowerShell).")
                health = inspect_install(game, expected_mod=bundled_mod_dll(),
                                         installed_after=started)
                if health.state == "broken":
                    raise RuntimeError("Weryfikacja plików nie powiodła się: " +
                                       diagnosis_text(health))
                try:
                    write_lobby_config(game, experimental_enabled, capacity)
                except Exception as config_error:
                    self.events.put(("done_config_warning", str(config_error), started))
                else:
                    self.events.put(("done", str(game), started))
        except Exception as exc:
            self.events.put(("error", str(exc)))

    def check_events(self) -> None:
        try:
            while True:
                event = self.events.get_nowait()
                if event[0] == "progress":
                    self.set_progress(event[1], event[2])
                elif event[0] == "done":
                    self.worker_busy = False
                    self.installed_after = event[2]
                    self.set_progress(1.0, "PLIKI GOTOWE / TEST STARTU PRZED NAMI")
                    self.install_button.configure(state="normal", text="NAPRAW / ZAINSTALUJ PONOWNIE")
                    self.launch_button.configure(state="normal")
                    self.check_mods(popup=False)
                    messagebox.showinfo("PARADOX — pliki skopiowane",
                                        "BepInEx, Reactor i PARADOX są w folderze gry.\n"
                                        "To NIE oznacza jeszcze, że mod się uruchomił.\n\n"
                                        "Kliknij URUCHOM AMONG US, a następnie SPRAWDŹ MODY, "
                                        "żeby potwierdzić start pluginu w logu BepInEx.", parent=self)
                elif event[0] == "done_config_warning":
                    self.worker_busy = False
                    self.installed_after = event[2]
                    self.set_progress(1.0, "PLIKI GOTOWE / KONFIGURACJA LOBBY NIE")
                    self.install_button.configure(state="normal", text="NAPRAW / ZAINSTALUJ PONOWNIE")
                    self.launch_button.configure(state="normal")
                    self.check_mods(popup=False)
                    messagebox.showwarning("PARADOX — ustawienia lobby",
                        "Mod został zainstalowany, ale nie zapisano wybranego "
                        "limitu graczy: " + event[1], parent=self)
                elif event[0] == "error":
                    self.worker_busy = False
                    self.install_button.configure(state="normal")
                    self.progress_status.configure(text="BŁĄD INSTALACJI", text_color=RED)
                    messagebox.showerror("PARADOX — błąd", event[1][-1800:],
                                         parent=self)
        except queue.Empty:
            pass
        if self.winfo_exists():
            self.after(80, self.check_events)

    def quick_diagnostics(self) -> None:
        game = Path(self.game_dir.get().strip())
        if not valid_game(game):
            selected = filedialog.askdirectory(
                title="Wskaż katalog gry Among Us do diagnostyki", parent=self)
            if not selected:
                return
            self.game_dir.set(selected)
        self.check_mods()

    def show_diagnostic_state(self, status, popup: bool = True) -> None:
        color = GREEN if status.state == "loaded" else (
            RED if status.state in ("broken", "failed") else PURPLE)
        if hasattr(self, "diagnostic_status") and self.diagnostic_status.winfo_exists():
            self.diagnostic_status.configure(text=status.summary, text_color=color)
        if popup:
            if status.state == "loaded":
                messagebox.showinfo("PARADOX — wykryto start pluginu",
                                    diagnosis_text(status), parent=self)
            elif status.state == "pending":
                messagebox.showinfo("PARADOX — oczekuje na uruchomienie",
                                    diagnosis_text(status), parent=self)
            else:
                messagebox.showwarning("PARADOX — wymagane naprawy",
                                       diagnosis_text(status), parent=self)

    def check_mods(self, popup: bool = True) -> None:
        game = Path(self.game_dir.get().strip())
        if not valid_game(game):
            messagebox.showerror("PARADOX",
                                 "Wskaż prawidłowy katalog Among Us.", parent=self)
            return
        status = inspect_install(game, expected_mod=bundled_mod_dll(),
                                 installed_after=self.installed_after)
        self.show_diagnostic_state(status, popup=popup)
        if status.state == "broken" and hasattr(self, "launch_button"):
            self.launch_button.configure(state="disabled")
        elif hasattr(self, "launch_button"):
            self.launch_button.configure(state="normal")

    def launch_game(self) -> None:
        game = Path(self.game_dir.get().strip())
        exe = game / "Among Us.exe"
        if not valid_game(game):
            messagebox.showerror("PARADOX", "Nie znaleziono gry.", parent=self)
            return
        health = inspect_install(game, expected_mod=bundled_mod_dll(),
                                 installed_after=self.installed_after)
        if health.state == "broken":
            self.show_diagnostic_state(health)
            return
        try:
            if os.name == "nt" and is_steam_install(game):
                # The EXE installer may run elevated; Steam itself should start
                # the game with its normal ownership, authentication and cwd.
                try:
                    os.startfile("steam://rungameid/945360")  # type: ignore[attr-defined]
                except OSError:
                    subprocess.Popen([str(exe)], cwd=str(game))
            else:
                subprocess.Popen([str(exe)], cwd=str(game))
            self.diagnostic_status.configure(
                text="ŻĄDANIE STARTU GRY — po załadowaniu sprawdź log BepInEx",
                text_color=CYAN)
        except OSError as exc:
            messagebox.showerror("PARADOX", str(exc), parent=self)

    def on_close(self) -> None:
        if self.worker_busy:
            messagebox.showwarning("PARADOX",
                                   "Poczekaj do zakończenia instalacji.", parent=self)
            return
        if self.animation_id:
            self.after_cancel(self.animation_id)
        self.destroy()


if __name__ == "__main__":
    ParadoxLauncher().mainloop()
