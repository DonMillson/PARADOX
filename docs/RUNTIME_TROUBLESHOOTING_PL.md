# PARADOX — brak ustawień / brak działania ról (runtime diagnostics)

## Najważniejsze: zainstalowane pliki ≠ uruchomiony mod

Ikona BepInEx, logotyp PARADOX w instalatorze lub plik `Paradox.dll` nie
potwierdzają, że patchowanie Harmony przebiegło pomyślnie.

### Gdzie znajdę ustawienia?

W wersji dev.6 zakładka **PARADOX** powinna pojawić się w
**własnym pokoju utworzonym jako HOST → Opcje / Game Settings**,
a nie na ekranie głównym. Mod próbuje utworzyć zakładkę ponownie przy
przełączeniu zakładek, ponieważ gra może inicjalizować menu przed
wejściem do pokoju.

Na głównym ekranie powinien pojawić się niewielki wskaźnik
`PARADOX 0.7.0-dev.6 • Ustawienia: HOST > Opcje pokoju`. To dowód, że
kod głównego menu uruchomił się, **nie** że działają role, mapy i sieć.

### Gdzie szukać przyczyny?

1. Zamknij Among Us i inne launchery.
2. Pobierz i uruchom najnowszy instalator PARADOX dev.6, wskaż
   **prawdziwy katalog z Among Us.exe, GameAssembly.dll i Among Us_Data**.
3. W instalatorze wybierz **ZAINSTALUJ / NAPRAW**, uruchom grę.
4. Załóż **prywatny pokój jako host** i otwórz opcje. Jeśli nie ma zakładki,
   przełącz najpierw zakładkę Standard/Role i sprawdź ponownie.
5. Po zamknięciu gry użyj w launcherze **SPRAWDŹ MODY**. Wynik
   `PLIKI ZAINSTALOWANE` nie oznacza `MOD WYSTARTOWAŁ`.
6. Otwórz plik `<Among Us>/BepInEx/LogOutput.log` lub
   `LogOutput.txt`. Sprawdź:
   - `PARADOX v0.7.0-dev.6 ... loading`
   - `PARADOX PATCH REPORT: successful=N; failed=M`
   - `PARADOX MAIN MENU INDICATOR READY`
   - `PARADOX SETTINGS TAB READY` (dopiero po wejściu do ustawień jako host)
   - `PARADOX PATCH FAILED`, `PARADOX PARTIAL LOAD`, `Failed to build PARADOX lobby settings`

Wgraj do zgłoszenia diagnostycznego **BepInEx/LogOutput.log**
(po ukryciu nazw graczy, prywatnych kodów pokoju, tokenów lub IP
jeżeli się pojawią). Bez tego nie można rozpoznać błędu zgodności
konkretnej wersji Among Us.

### Zmiana w dev.6

Wcześniejszy `Harmony.PatchAll()` kończył całą rejestrację, gdy choć jedna
poprawka była niezgodna z metodami gry. Dev.6 rejestruje patche oddzielnie
i raportuje dokładne błędy. Jeżeli część działa a część nie, mod nadal
jest **NIEZWERYFIKOWANY / CZĘŚCIOWY**, a nie certyfikowany.

Wersja modów musi być jednakowa na każdym kliencie, a kompatybilność
z Among Us v19.0.0 i multiplayer nadal wymaga prawdziwych testów.
