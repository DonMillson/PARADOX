# PARADOX AMONG US — Launcher + Instalator w jednym EXE

Nowy, samodzielny instalator na Windows x64 oparty na Python 3.12 i CustomTkinter. Jeden plik `PARADOX-AMONG-US-Launcher-v0.7.0-dev.4-x64.exe` zawiera **cały oficjalny BepInEx IL2CPP x64 be.790, Reactor 2.5.1-ci.400, Paradox.dll** i bezpieczny backend instalacji. Instalator nie musi pobierać bibliotek ani wymagać Pythona od gracza.

## Ekrany

- **Start:** PARADOX AMONG US / inicjalizacja, efekt neonowego logotypu PARADOX, przycisk rozpoczęcia misji.
- **Misja 1:** złap poruszającego się Impostora 5 razy.
- **Misja 2:** rozwiąż losowe działanie matematyczne (+, −, ×, potęgowanie).
- **Misja 3:** wykryta ścieżka Steam lub ręczne wskazanie folderu gry, potwierdzenie, pasek procentowy odzwierciedlający faktyczne kopiowanie plików, a po zakończeniu uruchomienie oryginalnego Among Us.

Instalacja nie zaczyna się przed zaliczeniem dwóch zadań. Powierzchnia GUI nigdy nie symuluje sukcesu: wymaga prawdziwych pakietów DLL, oryginalnego `Among Us.exe`, `GameAssembly.dll` i `Among Us_Data` oraz rzeczywistej architektury PE x64. Gdy brakuje paczki, przycisk zwraca błąd.

## Bezpieczeństwo

- Kopiuje zawartość pakietu z pliku EXE, nie podmienia samego `Among Us.exe`.
- Wymaga zamknięcia uruchomionej gry, inaczej backend przerwie operację.
- Zachowuje poprzednie wersje nadpisywanych plików; podczas nieudanej instalacji wykonuje rollback.
- Nie usuwa innych pluginów z folderu gry.
- Backend udostępnia autentyczne postępy `PARADOX_PROGRESS:x/y`, które GUI wyświetla w procentach.
- Rejestr manifestu i kopie zapasowe: `<folder gry>/.paradox-installer/`.

## Pobieranie i budowa

Z menu **Actions** repozytorium wybierz zielony workflow **Build PARADOX AMONG US Launcher EXE** i pobierz artefakt `PARADOX-AMONG-US-LAUNCHER-ALL-IN-ONE-v0.7.0-dev.4-x64`, zawierający JEDEN `.exe`. Program wymaga Windows 10/11 x64; może wymagać uruchomienia jako administrator (zależnie od uprawnień instalacji gry).

Budowanie ze źródeł w GitHub Actions: workflow `.github/workflows/python-launcher.yml` kompiluje oryginalny mod z biblioteki AmongUs.GameLibs 2026.9.29, pobiera oficjalne BepInEx be.790, dołącza Reactor CI400, sprawdza instalację / odinstalowanie na syntetycznej grze oraz testy Pythona, i dopiero potem tworzy PyInstaller `--onefile`.

**Testy modułów, instalacji i kompilacji NIE potwierdzają 100% działania moda wśród prawdziwych graczy Among Us v19.** Do tego potrzebne są testy startu i synchronizacji multiplayer z rzeczywistym klientem.
