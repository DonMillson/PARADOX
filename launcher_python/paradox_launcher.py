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
from launcher_python import paradox_visuals as art
from pathlib import Path
import tkinter as tk
from tkinter import filedialog, messagebox

import customtkinter as ctk

APP_TITLE = "PARADOX AMONG US"
MOD_VERSION = "0.7.0-dev.6"
# Among Us-inspired industrial spacecraft palette: ink, warning yellow,
# emergency red, teal visor reflections. No generic neon AI dashboard.
BACKGROUND = "#090E17"
CARD = "#132236"
CARD_ALT = "#1D3046"
TEXT = "#F4F1E8"
MUTED = "#B9C2CC"
CYAN = "#82E3E1"
PURPLE = "#AB91D8"
RED = "#ED3E4D"
GREEN = "#7EDFB7"
YELLOW = "#FFD35A"
FRAME = "#60758A"
INK = BACKGROUND
WHITE = TEXT

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
        # One complete fictional spacecraft console, not separate floating
        # neumorphic/AI cards. The warning stripe is shared across all tasks.
        top = ctk.CTkFrame(self, fg_color="#0D1727", corner_radius=0,
                           border_width=2, border_color="#31485F")
        top.pack(fill="x", padx=19, pady=(17, 8))
        banner = tk.Canvas(top, width=930, height=96, bg="#0B1422",
                           highlightthickness=0)
        banner.pack(fill="x", padx=2, pady=2)
        art.header(banner, 930, 96)

        mission = ctk.CTkFrame(self, fg_color="#182838", corner_radius=7,
                               border_width=1, border_color=FRAME)
        mission.pack(fill="x", padx=19, pady=(2, 9))
        self.steps = mission
        self.status_items: list[ctk.CTkLabel] = []
        for idx, label in enumerate(
            ("01  |  SECURITY", "02  |  TASK", "03  |  DOWNLOAD"), start=1
        ):
            lbl = ctk.CTkLabel(
                mission, text=label, text_color=MUTED,
                font=ctk.CTkFont(family="Consolas", size=13, weight="bold"),
                corner_radius=2, height=36)
            lbl.pack(side="left", expand=True, padx=4, pady=3)
            self.status_items.append(lbl)

        self.content = ctk.CTkFrame(
            self, fg_color=CARD, corner_radius=8,
            border_width=2, border_color="#47607A")
        self.content.pack(fill="both", expand=True, padx=19, pady=(0, 8))
        self.content.pack_propagate(False)

        foot = ctk.CTkFrame(self, fg_color="#132236", corner_radius=0)
        foot.pack(fill="x", padx=19, pady=(0, 13))
        ctk.CTkLabel(
            foot, text="PARADOX  /  SHIP SYSTEMS  /  DONMILLSON",
            text_color=YELLOW,
            font=ctk.CTkFont(family="Consolas", size=12, weight="bold")
        ).pack(side="left", padx=13, pady=5)
        ctk.CTkLabel(
            foot, text=f"AMONG US 19   •   {MOD_VERSION}   •   WINDOWS x64",
            text_color=MUTED, font=ctk.CTkFont(family="Consolas", size=11)
        ).pack(side="right", padx=13)

    def show_stage(self, stage: int) -> None:
        if self.animation_id:
            self.after_cancel(self.animation_id)
            self.animation_id = None
        self.board = None
        for child in self.content.winfo_children():
            child.destroy()
        self.stage = stage
        for idx, label in enumerate(self.status_items, start=1):
            label.configure(
                text_color=INK if stage == idx else (
                    GREEN if stage > idx else MUTED),
                fg_color=YELLOW if stage == idx else "#1B3045")
        if stage == 0:
            self.welcome()
        elif stage == 1:
            self.hunt()
        elif stage == 2:
            self.math()
        else:
            self.install_panel()

    def title_block(self, name: str, detail: str) -> None:
        ctk.CTkLabel(
            self.content, text=name, text_color=YELLOW,
            font=ctk.CTkFont(family="Segoe UI Black", size=25, weight="bold")
        ).pack(anchor="w", padx=27, pady=(17, 4))
        ctk.CTkLabel(
            self.content, text=detail, text_color=MUTED, justify="left",
            wraplength=860, font=ctk.CTkFont(family="Segoe UI", size=14)
        ).pack(anchor="w", padx=27, pady=(0, 10))

    def welcome(self) -> None:
        self.title_block(
            "EMERGENCY BROADCAST   /   PARADOX",
            "Zaginął sygnał załogi. Rozpoznaj impostora, napraw konsolę "
            "i przywróć systemy PARADOX."
        )
        panel = ctk.CTkFrame(
            self.content, fg_color="#0B1525", corner_radius=7,
            border_width=2, border_color="#476079")
        panel.pack(fill="both", expand=True, padx=27, pady=(4, 12))

        picture = tk.Canvas(panel, width=780, height=288,
                            bg=art.SPACE, highlightthickness=0)
        picture.pack(fill="x", padx=17, pady=(14, 9))
        art.poster(picture, 780, 288)

        ctk.CTkLabel(
            panel, text="SECURITY / FIX WIRING / INSTALL PARADOX",
            text_color=YELLOW,
            font=ctk.CTkFont(family="Consolas", size=14, weight="bold")
        ).pack(pady=(4, 8))
        ctk.CTkButton(
            panel, text="▶  ROZPOCZNIJ MISJĘ", width=305, height=53,
            fg_color=RED, hover_color="#B92D3C", text_color=TEXT,
            corner_radius=5, border_width=2, border_color="#F28387",
            font=ctk.CTkFont(family="Segoe UI", size=18, weight="bold"),
            command=lambda: self.show_stage(1)
        ).pack(pady=(2, 8))
        ctk.CTkButton(
            panel, text="▣  SPRAWDŹ MODY / LOGI", width=305, height=40,
            fg_color="#293F55", hover_color="#355873", text_color=WHITE,
            corner_radius=5, border_width=1, border_color="#7994A8",
            font=ctk.CTkFont(family="Consolas", size=13, weight="bold"),
            command=self.quick_diagnostics
        ).pack(pady=(0, 11))

    def hunt(self) -> None:
        self.title_block("SECURITY CAM 01  /  ZŁAP IMPOSTORA",
                         "Sabotaż wykryty! Kliknij czerwonego Impostora 5 razy, "
                         "aby otworzyć terminal instalacji.")
        frame = ctk.CTkFrame(self.content, fg_color="#080D16",
                             corner_radius=7, border_width=2,
                             border_color="#718296")
        frame.pack(padx=27)
        self.board = tk.Canvas(frame, width=785, height=327, bg=art.SPACE,
                               highlightthickness=0, cursor="crosshair")
        self.board.pack(padx=6, pady=6)
        art.hunt_backdrop(self.board, 785, 327)
        self.px, self.py, self.dx, self.dy = 130.0, 120.0, 3.2, 2.0
        self.draw_impostor()
        self.board.tag_bind("target", "<Button-1>", self.catch)
        buttons = ctk.CTkFrame(self.content, fg_color="transparent")
        buttons.pack(fill="x", padx=37, pady=20)
        self.hits_text = ctk.CTkLabel(buttons, text=f"PODEJRZANY: {self.hits}/5",
                                      text_color=YELLOW,
                                      font=ctk.CTkFont(size=18, weight="bold"))
        self.hits_text.pack(side="left")
        self.hunt_next = ctk.CTkButton(buttons, text="▶  NASTĘPNE ZADANIE", width=250,
                                       state="normal" if self.hunt_passed else "disabled",
                                       fg_color=RED, text_color=WHITE,
                                       hover_color="#B92D3C", corner_radius=5,
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
        self.hits_text.configure(text=f"PODEJRZANY: {self.hits}/5")
        if self.hits == 5:
            self.hunt_passed = True
            self.hits_text.configure(text="✓  IMPOSTOR WYKRYTY", text_color=GREEN)
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
        self.title_block("ELECTRICAL  /  NAPRAW TERMINAL",
                         "Awaria w Electrical. Rozwiąż równanie na panelu, "
                         "aby przywrócić zasilanie instalatora.")
        center = ctk.CTkFrame(self.content, fg_color="#182B38", corner_radius=6,
                              border_width=3, border_color="#6C8897")
        center.pack(fill="x", padx=62, pady=(35, 20))
        ctk.CTkLabel(center, text="REACTOR CONTROL  •  ELECTRICAL", text_color=YELLOW,
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
        ctk.CTkButton(center, text="⚡  PRZYWRÓĆ ZASILANIE", width=235, height=45,
                      fg_color=YELLOW, hover_color="#DAAC34", text_color=INK,
                      corner_radius=5,
                      command=self.verify_answer).pack(pady=(0, 12))
        self.math_message = ctk.CTkLabel(center, text="Wpisz wynik liczbowy.",
                                         text_color=MUTED)
        self.math_message.pack(pady=(0, 24))
        self.math_next = ctk.CTkButton(self.content, text="▶  OTWÓRZ ŁADOWNIĘ",
                                       height=47, width=265, state="normal"
                                       if self.math_passed else "disabled",
                                       fg_color=RED, text_color=WHITE, corner_radius=5,
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
        self.title_block("CARGO BAY  /  MONTAŻ SYSTEMÓW",
                         "Cel: Among Us. Wybierz folder gry, przygotuj BepInEx i Reactor, "
                         "a potem zainstaluj PARADOX.")
        form = ctk.CTkFrame(self.content, fg_color="#1A2D40", corner_radius=6,
                            border_width=2, border_color="#60758A")
        form.pack(fill="x", padx=30, pady=(12, 19))
        ctk.CTkLabel(form, text="DESTINATION / FOLDER AMONG US", text_color=YELLOW,
                     font=ctk.CTkFont(size=13, weight="bold")
                     ).pack(anchor="w", padx=21, pady=(20, 8))
        line = ctk.CTkFrame(form, fg_color="transparent")
        line.pack(fill="x", padx=21, pady=(0, 14))
        ctk.CTkEntry(line, textvariable=self.game_dir, height=42,
                     font=ctk.CTkFont(size=13)).pack(side="left", fill="x",
                                                     expand=True, padx=(0, 10))
        ctk.CTkButton(line, text="WYBIERZ", command=self.browse_game,
                      width=132, height=42, fg_color="#40556D",
                      corner_radius=4, border_width=1,
                      border_color="#8399A8").pack(side="right")
        ctk.CTkLabel(form, text="ONLINE: BepInEx IL2CPP x64  •  Reactor CI400  •  PARADOX",
                     text_color=GREEN, font=ctk.CTkFont(size=13)
                     ).pack(anchor="w", padx=21, pady=(0, 7))

        lobby = ctk.CTkFrame(form, fg_color="#0B1928", corner_radius=5,
                             border_width=1, border_color="#405770")
        lobby.pack(fill="x", padx=18, pady=(1, 15))
        self.large_lobby_checkbox = ctk.CTkCheckBox(
            lobby,
            text="EXPERIMENTAL / BIG LOBBY (serwer modowany)",
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

        self.progress = ctk.CTkProgressBar(self.content, height=15,
                                           progress_color=YELLOW, fg_color="#263550",
                                           corner_radius=3)
        self.progress.pack(fill="x", padx=33, pady=(24, 3))
        self.progress.set(0)
        self.progress_status = ctk.CTkLabel(self.content, text="GOTOWY  /  0%",
                                            text_color=CYAN,
                                            font=ctk.CTkFont(size=16, weight="bold"))
        self.progress_status.pack(pady=(9, 13))
        self.install_button = ctk.CTkButton(self.content, text="▶  ZAINSTALUJ PARADOX",
                                            width=312, height=52, fg_color=RED,
                                            hover_color="#BD253F", text_color=WHITE,
                                            corner_radius=5, border_width=2,
                                            border_color="#F47A88",
                                            font=ctk.CTkFont(size=18, weight="bold"),
                                            command=self.start_install)
        self.install_button.pack(pady=(11, 9))
        actions = ctk.CTkFrame(self.content, fg_color="transparent")
        actions.pack(pady=(4, 4))
        self.launch_button = ctk.CTkButton(actions, text="▶  URUCHOM GRĘ",
                                           width=230, height=40, state="disabled",
                                           fg_color="#354F60", corner_radius=5,
                                           command=self.launch_game)
        self.launch_button.pack(side="left", padx=7)
        ctk.CTkButton(actions, text="▣  DIAGNOSTYKA", width=210, height=40,
                      fg_color="#41596B", hover_color="#5D7489", corner_radius=5,
                      command=self.check_mods).pack(side="left", padx=7)
        self.diagnostic_status = ctk.CTkLabel(
            self.content, text="STATUS MODA: NIEZWERYFIKOWANY",
            text_color=YELLOW, font=ctk.CTkFont(size=12, weight="bold"))
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
