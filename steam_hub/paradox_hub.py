"""PARADOX HUB — standalone Steam-library companion app, Windows x64.

Free, unofficial manager for an independently owned Among Us installation.
The program does not contain the game, impersonate its Steam AppID,
circumvent Steam account ownership or claim Innersloth affiliation.
"""
from __future__ import annotations

import os
from pathlib import Path
import queue
import re
import subprocess
import sys
import tempfile
import threading
import time
import tkinter as tk
from tkinter import filedialog, messagebox

import customtkinter as ctk

from launcher_python import paradox_visuals as art
from launcher_python.lobby_config import write_lobby_config
from launcher_python.mod_health import (
    inspect_install, diagnosis_text, build_support_report,
)
from launcher_python.online_dependencies import stage_live_dependencies
from steam_hub.steam_bridge import (
    discover_owned_installation, launch_among_us, valid_among_us_game,
)

VERSION = "0.7.0-dev.7"
BG = "#07111C"
PANEL = "#14283B"
EDGE = "#54738B"
GOLD = art.YELLOW
RED = art.RED
GREEN = art.TURQUOISE
WHITE = art.WHITE
MUTED = art.MUTED
INK = art.INK
ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("blue")


def resource_root() -> Path:
    return Path(getattr(sys, "_MEIPASS", Path(__file__).resolve().parents[1]))


def embedded_mod() -> Path:
    return resource_root() / "mod" / "Paradox.dll"


def installer_ps1() -> Path:
    return resource_root() / "installer" / "Install-Paradox.ps1"


def uninstaller_ps1() -> Path:
    return resource_root() / "installer" / "Uninstall-Paradox.ps1"


def diagnostic_color(state: str, actually_opened: bool = False) -> str:
    if state in ("failed", "broken", "partial"):
        return RED
    return GREEN if state == "loaded" and actually_opened else GOLD


class ParadoxHub(ctk.CTk):
    def __init__(self) -> None:
        super().__init__()
        self.title("PARADOX HUB — Among Us Companion • by DonMillson")
        self.geometry("1150x820")
        self.minsize(1130, 735)
        self.configure(fg_color=BG)
        self.protocol("WM_DELETE_WINDOW", self.on_close)

        installed = discover_owned_installation()
        self.game = tk.StringVar(value=str(installed) if installed else "")
        self.large_lobby = tk.BooleanVar(value=False)
        self.capacity = tk.StringVar(value="50")
        self.current_page = "GRA"
        self.busy = False
        self.installed_since: float | None = None
        self.events: queue.Queue[tuple] = queue.Queue()
        self.nav_buttons: dict[str, ctk.CTkButton] = {}
        self.progress_label = None
        self.progress_bar = None
        self.state_label = None

        self._build_shell()
        self.show_page("GRA")
        self.after(75, self._poll_events)

    def _build_shell(self) -> None:
        frame = ctk.CTkFrame(self, fg_color="#0E1C2A", corner_radius=0,
                             border_width=2, border_color=EDGE)
        frame.pack(fill="x", padx=14, pady=(13, 7))
        banner = tk.Canvas(frame, height=96, bg=BG, highlightthickness=0)
        banner.pack(fill="x", padx=3, pady=3)
        art.header(banner, 1080, 96)

        bottom = ctk.CTkFrame(self, fg_color="transparent")
        bottom.pack(fill="both", expand=True, padx=14, pady=(0, 7))
        nav = ctk.CTkFrame(bottom, fg_color="#111F2C", width=215,
                            corner_radius=4, border_width=2, border_color="#4B647C")
        nav.pack(side="left", fill="y", padx=(0, 9))
        nav.pack_propagate(False)
        ctk.CTkLabel(nav, text="SHIP TERMINAL", text_color=GOLD,
                     font=ctk.CTkFont(family="Consolas", size=16, weight="bold")
                     ).pack(padx=12, pady=(25, 13))
        for title, icon in (("GRA", "▶"), ("MOD", "✦"),
                            ("DIAGNOSTYKA", "▣"), ("O PROJEKCIE", "ⓘ")):
            btn = ctk.CTkButton(
                nav, text=f"{icon}  {title}", width=175, height=48,
                anchor="w", corner_radius=4, border_width=1,
                border_color="#638098", fg_color="#243B4F",
                hover_color="#36566D", text_color=WHITE,
                font=ctk.CTkFont(family="Consolas", size=14, weight="bold"),
                command=lambda name=title: self.show_page(name))
            btn.pack(padx=13, pady=5)
            self.nav_buttons[title] = btn
        ctk.CTkLabel(nav, text="CREW // PARADOX", text_color=GOLD,
                     font=ctk.CTkFont(family="Consolas", size=12)
                     ).pack(side="bottom", pady=18)

        self.page_host = ctk.CTkFrame(bottom, fg_color=PANEL, corner_radius=5,
                                       border_width=2, border_color=EDGE)
        self.page_host.pack(side="right", fill="both", expand=True)
        self.page_host.pack_propagate(False)

        ctk.CTkLabel(
            self,
            text="NIEOFICJALNY MOD • WYMAGA WŁASNEJ KOPII AMONG US • "
                 "NIE JEST POWIĄZANY Z INNERSLOTH LLC",
            text_color=MUTED, font=ctk.CTkFont(size=11)
        ).pack(pady=(0, 9))

    def _clear(self) -> None:
        for child in self.page_host.winfo_children():
            child.destroy()
        self.state_label = None
        self.progress_label = None
        self.progress_bar = None

    def show_page(self, page: str) -> None:
        self.current_page = page
        for name, button in self.nav_buttons.items():
            button.configure(fg_color=RED if name == page else "#243B4F",
                             border_color=GOLD if name == page else EDGE)
        self._clear()
        if page == "GRA":
            self._game_page()
        elif page == "MOD":
            self._mod_page()
        elif page == "DIAGNOSTYKA":
            self._diagnostics_page()
        else:
            self._about_page()

    def _title(self, title: str, sub: str) -> None:
        ctk.CTkLabel(self.page_host, text=title, text_color=GOLD,
                     font=ctk.CTkFont(family="Segoe UI Black", size=27,
                                      weight="bold")).pack(
            anchor="w", padx=28, pady=(21, 4))
        ctk.CTkLabel(self.page_host, text=sub, text_color=MUTED,
                     font=ctk.CTkFont(size=13), wraplength=760,
                     justify="left").pack(anchor="w", padx=28, pady=(0, 13))

    def _game_path_picker(self, parent=None) -> None:
        target = parent or self.page_host
        box = ctk.CTkFrame(target, fg_color="#0B192A", corner_radius=3)
        box.pack(fill="x", padx=23, pady=(6, 12))
        ctk.CTkLabel(box, text="INSTALACJA AMONG US / STEAM",
                     text_color=GOLD, font=ctk.CTkFont(family="Consolas",
                                                       size=12, weight="bold")
                     ).pack(anchor="w", padx=15, pady=(11, 5))
        line = ctk.CTkFrame(box, fg_color="transparent")
        line.pack(fill="x", padx=15, pady=(0, 14))
        ctk.CTkEntry(line, textvariable=self.game, height=38,
                     font=ctk.CTkFont(size=12)).pack(side="left",
                                                     fill="x", expand=True)
        ctk.CTkButton(line, text="WYBIERZ", width=105, height=38,
                      fg_color="#345267", command=self.browse
                      ).pack(side="right", padx=(9, 0))

    def _game_page(self) -> None:
        self._title("CREW TERMINAL  //  PARADOX",
                    "Jedna aplikacja do gry, moda i diagnostyki. "
                    "Among Us pozostaje osobną grą kupioną na Steam.")
        scene = tk.Canvas(self.page_host, width=780, height=288,
                          bg=BG, highlightthickness=0)
        scene.pack(padx=21, pady=(3, 10))
        art.poster(scene, 780, 288)
        self._game_path_picker()
        group = ctk.CTkFrame(self.page_host, fg_color="transparent")
        group.pack(pady=(12, 8))
        self._btn(group, "▶  URUCHOM AMONG US PRZEZ STEAM",
                  self.start_game, prominent=True, width=340).pack(side="left", padx=8)
        self._btn(group, "▣  SPRAWDŹ PARADOX",
                  lambda: self.show_page("DIAGNOSTYKA"), width=245
                  ).pack(side="left", padx=8)
        self._summary_label()

    def _mod_page(self) -> None:
        self._title("ENGINEERING  //  PARADOX MOD",
                    "Instalacja, aktualizacja i naprawa moda w katalogu Among Us. "
                    "Pliki pobierane są z oficjalnych serwerów BepInEx i Reactor.")
        self._game_path_picker()
        ctk.CTkLabel(self.page_host, text="PARADOX DLL / BepInEx IL2CPP / Reactor CI400",
                     text_color=GREEN, font=ctk.CTkFont(size=14, weight="bold")
                     ).pack(anchor="w", padx=29, pady=(13, 13))
        setting = ctk.CTkFrame(self.page_host, fg_color="#0E2131",
                                border_width=1, border_color=EDGE, corner_radius=3)
        setting.pack(fill="x", padx=27, pady=8)
        ctk.CTkCheckBox(setting, text="EXPERIMENTAL LARGE LOBBY (tylko modowany serwer)",
                        variable=self.large_lobby, text_color=WHITE).pack(
            anchor="w", padx=14, pady=(14, 9))
        row = ctk.CTkFrame(setting, fg_color="transparent")
        row.pack(anchor="w", padx=14, pady=(0, 13))
        ctk.CTkLabel(row, text="Testowy limit:", text_color=MUTED).pack(side="left")
        ctk.CTkOptionMenu(row, variable=self.capacity,
                          values=["15", "20", "30", "40", "50"],
                          width=90, fg_color="#345267").pack(side="left", padx=12)
        ctk.CTkLabel(row, text="Oficjalne serwery nadal: 15 osób",
                     text_color=MUTED).pack(side="left")

        self.progress_bar = ctk.CTkProgressBar(self.page_host,
                                               progress_color=GOLD, height=18)
        self.progress_bar.pack(fill="x", padx=29, pady=(24, 4))
        self.progress_bar.set(0)
        self.progress_label = ctk.CTkLabel(self.page_host, text="GOTOWY / 0%",
                                            text_color=GOLD)
        self.progress_label.pack(pady=(6, 14))
        buttons = ctk.CTkFrame(self.page_host, fg_color="transparent")
        buttons.pack(pady=9)
        self._btn(buttons, "▶  ZAINSTALUJ / NAPRAW",
                  self.install_mod, prominent=True, width=270).pack(
            side="left", padx=8)
        self._btn(buttons, "ODINSTALUJ MOD",
                  self.uninstall_mod, width=220).pack(side="left", padx=8)
        self._summary_label()

    def _diagnostics_page(self) -> None:
        self._title("SECURITY // RAPORT SYSTEMÓW",
                    "Osobno sprawdzamy obecność bibliotek, start PARADOX "
                    "i rzeczywiste utworzenie zakładki hosta.")
        self._game_path_picker()
        state = self._health()
        self.state_label = ctk.CTkTextbox(
            self.page_host, width=720, height=305,
            border_width=1, border_color=EDGE,
            fg_color="#07121C", text_color=WHITE,
            font=ctk.CTkFont(family="Consolas", size=13))
        self.state_label.pack(fill="both", expand=True, padx=27, pady=(9, 16))
        self._display_health(state)
        actions = ctk.CTkFrame(self.page_host, fg_color="transparent")
        actions.pack(pady=(0, 19))
        self._btn(actions, "▣  ODŚWIEŻ", self.refresh_health, width=145
                  ).pack(side="left", padx=4)
        self._btn(actions, "OTWÓRZ LOG", self.open_log, width=145
                  ).pack(side="left", padx=4)
        self._btn(actions, "KOPIUJ BEZPIECZNY RAPORT",
                  self.copy_report, width=245).pack(side="left", padx=4)

    def _about_page(self) -> None:
        self._title("PARADOX HUB  //  BY DONMILLSON",
                    "Nieoficjalna aplikacja towarzysząca Among Us — Windows x64.")
        notes = (
            "PARADOX HUB nie zawiera gry Among Us ani nie udaje oficjalnego "
            "produktu Innersloth. Instaluje wyłącznie własny mod PARADOX "
            "oraz jawne zależności BepInEx i Reactor.\n\n"
            "ROZGRYWKA\n"
            "43 role i tryb testowy 50 graczy znajdują się w kodzie moda. "
            "Ich pełne działanie wymaga odrębnego sprawdzenia w aktualnej grze "
            "oraz kompatybilnego serwera dla dużych lobby.\n\n"
            "DODAJ DO BIBLIOTEKI STEAM\n"
            "Steam → Gry → Dodaj grę spoza Steam → wskaż PARADOX-Hub.exe. "
            "Nie tworzy to produktu w sklepie ani nie włącza automatycznych "
            "aktualizacji przez Steam. Na Steam Store potrzebne jest "
            "konto Steamworks i osobna akceptacja Valve.\n\n"
            "POLITYKA MODÓW\n"
            "Ten mod nie jest powiązany z Among Us lub Innersloth LLC i nie "
            "jest przez nich sponsorowany ani zatwierdzony. Część materiałów "
            "gry stanowi własność Innersloth LLC. © Innersloth LLC."
        )
        ctk.CTkTextbox(self.page_host, font=ctk.CTkFont(size=15),
                       wrap="word", fg_color="#091B2A", text_color=WHITE,
                       width=780, height=430).pack(padx=27, pady=(12, 0))
        info = self.page_host.winfo_children()[-1]
        info.insert("1.0", notes)
        info.configure(state="disabled")

    def _btn(self, parent, label, callback, prominent=False, width=230):
        return ctk.CTkButton(
            parent, text=label, command=callback, width=width,
            height=48, corner_radius=5, border_width=2,
            border_color="#F08389" if prominent else EDGE,
            fg_color=RED if prominent else "#314D61",
            hover_color="#BA2C40" if prominent else "#416B83",
            text_color=WHITE,
            font=ctk.CTkFont(family="Segoe UI", size=14, weight="bold"),
        )

    def browse(self):
        folder = filedialog.askdirectory(title="Folder z Among Us.exe", parent=self)
        if folder:
            self.game.set(folder)
            if self.current_page == "DIAGNOSTYKA":
                self.refresh_health()

    def _game_folder(self) -> Path | None:
        folder = Path(self.game.get().strip()).expanduser()
        if not valid_among_us_game(folder):
            messagebox.showwarning(
                "Among Us", "Wybierz katalog z Among Us.exe, GameAssembly.dll "
                            "i Among Us_Data.", parent=self)
            return None
        return folder

    def _health(self):
        game = Path(self.game.get().strip())
        if not valid_among_us_game(game):
            return None
        return inspect_install(
            game, expected_mod=embedded_mod(), installed_after=self.installed_since
        )

    def _summary_label(self):
        health = self._health()
        if health is None:
            text, color = "STEAM / BRAK POTWIERDZONEJ INSTALACJI", GOLD
        else:
            opened = "Otwarcie PARADOX: WYKRYTO" in health.details
            color = diagnostic_color(health.state, opened)
            text = ("PARADOX / " + health.summary)
        self._status_note = ctk.CTkLabel(
            self.page_host, text=text, text_color=color,
            font=ctk.CTkFont(family="Consolas", size=12, weight="bold"),
            wraplength=780)
        self._status_note.pack(pady=(12, 3))

    def _display_health(self, state):
        if self.state_label is None:
            return
        text = diagnosis_text(state) if state is not None else (
            "Nie znaleziono Among Us w bibliotece Steam.\n"
            "Wskaż ręcznie folder z Among Us.exe."
        )
        self.state_label.configure(state="normal")
        self.state_label.delete("1.0", "end")
        self.state_label.insert("1.0", text)
        self.state_label.configure(state="disabled")

    def refresh_health(self):
        self._display_health(self._health())

    def open_log(self):
        health = self._health()
        if health is None or health.log is None:
            messagebox.showwarning("PARADOX", "Brak logu. Uruchom grę, "
                                   "zamknij ją i spróbuj ponownie.", parent=self)
            return
        try:
            os.startfile(str(health.log))  # type: ignore[attr-defined]
        except (OSError, AttributeError) as exc:
            messagebox.showerror("PARADOX", str(exc), parent=self)

    def copy_report(self):
        game = self._game_folder()
        if game is None:
            return
        report = build_support_report(game, expected_mod=embedded_mod(),
                                      installed_after=self.installed_since)
        self.clipboard_clear()
        self.clipboard_append(report)
        self.update()
        messagebox.showinfo("PARADOX", "Raport bez prywatnych danych jest w schowku.",
                            parent=self)

    def start_game(self):
        game = self._game_folder()
        if game is None:
            return
        try:
            via = launch_among_us(game, use_steam=True)
            messagebox.showinfo("PARADOX", f"Wysłano żądanie uruchomienia przez {via}.\n"
                                "Po wejściu do lobby hosta sprawdź zakładkę PARADOX "
                                "(awaryjnie: F10).", parent=self)
        except OSError as exc:
            messagebox.showerror("Steam", f"Nie mogę uruchomić gry przez Steam: {exc}",
                                 parent=self)

    def _set_progress(self, amount: float, text: str) -> None:
        if self.progress_bar is not None and self.progress_label is not None:
            self.progress_bar.set(min(1, max(0, amount)))
            self.progress_label.configure(text=f"{text}  /  {round(amount * 100)}%")

    def install_mod(self):
        if self.busy:
            return
        game = self._game_folder()
        if game is None:
            return
        if not embedded_mod().is_file() or not installer_ps1().is_file():
            messagebox.showerror("PARADOX", "Niekompletny EXE: brak wbudowanego "
                                 "moda albo backendu instalacji.", parent=self)
            return
        enabled = bool(self.large_lobby.get())
        cap = int(self.capacity.get()) if enabled else 15
        if not messagebox.askyesno(
            "PARADOX / Naprawa",
            "Zamknij Among Us przed instalacją.\n"
            "Pobrać zweryfikowany BepInEx, Reactor i zainstalować PARADOX?\n"
            f"Duże lobby: {'testowo ' + str(cap) if enabled else 'wyłączone'}.",
            parent=self
        ):
            return
        self.busy = True
        self._set_progress(0.02, "KONTROLA PRZED INSTALACJĄ")
        threading.Thread(target=self._install_worker,
                         args=(game, enabled, cap),
                         daemon=True, name="ParadoxHubInstall").start()

    def _install_worker(self, game: Path, enabled: bool, cap: int):
        start_time = time.time()
        try:
            with tempfile.TemporaryDirectory(prefix="paradox-hub-") as folder:
                payload = Path(folder) / "payload"
                payload.mkdir()
                stage_live_dependencies(
                    payload, embedded_mod(),
                    lambda fraction, label: self.events.put(
                        ("progress", fraction * 0.62, label))
                )
                support = game / ".paradox-installer"
                cmd = [
                    "powershell.exe", "-NoProfile", "-NonInteractive",
                    "-ExecutionPolicy", "Bypass", "-File", str(installer_ps1()),
                    "-GamePath", str(game), "-PayloadRoot", str(payload),
                    "-SupportRoot", str(support),
                ]
                proc = subprocess.Popen(
                    cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                    creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
                    text=True, encoding="utf-8", errors="replace")
                assert proc.stdout is not None
                output = []
                for line in proc.stdout:
                    output.append(line)
                    match = re.search(r"PARADOX_PROGRESS:(\d+)/(\d+)", line)
                    if match:
                        fraction = int(match.group(1)) / max(int(match.group(2)), 1)
                        self.events.put(("progress", 0.62 + fraction * .35,
                                         "KOPIOWANIE SYSTEMÓW"))
                if proc.wait() != 0:
                    raise RuntimeError("\n".join(output[-10:])[-2000:])
                health = inspect_install(game, expected_mod=embedded_mod(),
                                         installed_after=start_time)
                if health.state == "broken":
                    raise RuntimeError(diagnosis_text(health))
                try:
                    write_lobby_config(game, enabled, cap)
                except Exception as exc:
                    self.events.put(("warning", str(exc), start_time))
                else:
                    self.events.put(("done", start_time))
        except Exception as exc:
            self.events.put(("error", str(exc)))

    def uninstall_mod(self):
        if self.busy:
            return
        game = self._game_folder()
        if game is None:
            return
        if not uninstaller_ps1().is_file():
            messagebox.showerror("PARADOX", "Nie znaleziono skryptu odinstalowania.",
                                 parent=self)
            return
        if not messagebox.askyesno("PARADOX", "Wycofać pliki PARADOX zgodnie z "
                                   "manifestem instalatora? Nie usuwa to "
                                   "samej gry Among Us.", parent=self):
            return
        self.busy = True
        threading.Thread(target=self._uninstall_worker, args=(game,),
                         daemon=True).start()

    def _uninstall_worker(self, game: Path):
        try:
            cmd = [
                "powershell.exe", "-NoProfile", "-NonInteractive",
                "-ExecutionPolicy", "Bypass", "-File", str(uninstaller_ps1()),
                "-SupportRoot", str(game / ".paradox-installer"),
            ]
            process = subprocess.run(cmd, stdout=subprocess.PIPE,
                                     stderr=subprocess.STDOUT, text=True,
                                     errors="replace", encoding="utf-8",
                                     creationflags=getattr(subprocess,
                                                           "CREATE_NO_WINDOW", 0))
            if process.returncode:
                raise RuntimeError(process.stdout[-2000:])
            self.events.put(("uninstalled",))
        except Exception as exc:
            self.events.put(("error", str(exc)))

    def _poll_events(self) -> None:
        try:
            while True:
                event = self.events.get_nowait()
                if event[0] == "progress":
                    self._set_progress(event[1], event[2])
                elif event[0] in ("done", "warning", "uninstalled"):
                    self.busy = False
                    self.installed_since = event[-1] if event[0] != "uninstalled" else None
                    self._set_progress(1, "PLIKI GOTOWE / SPRAWDŹ GRĘ")
                    if event[0] == "warning":
                        messagebox.showwarning("PARADOX",
                            "Pliki skopiowano, ale ustawienia lobby nie zostały "
                            "zapisane: " + event[1], parent=self)
                    elif event[0] == "done":
                        messagebox.showinfo("PARADOX",
                            "Instalacja plików ukończona. Aby zweryfikować "
                            "działanie moda, URUCHOM grę, zostań HOSTEM "
                            "i sprawdź zakładkę PARADOX (F10).", parent=self)
                    else:
                        messagebox.showinfo("PARADOX",
                                            "Zakończono odinstalowanie plików moda.",
                                            parent=self)
                    if self.current_page == "DIAGNOSTYKA":
                        self.refresh_health()
                elif event[0] == "error":
                    self.busy = False
                    messagebox.showerror("PARADOX / ERROR",
                                         event[1][-2000:], parent=self)
        except queue.Empty:
            pass
        if self.winfo_exists():
            self.after(90, self._poll_events)

    def on_close(self):
        if self.busy:
            messagebox.showwarning("PARADOX", "Zakończ instalację lub "
                                   "odinstalowanie przed zamknięciem.", parent=self)
            return
        self.destroy()


if __name__ == "__main__":
    ParadoxHub().mainloop()
