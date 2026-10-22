# PARADOX AMONG US — instalator online (Windows x64)

Nowy, mniejszy launcher `PARADOX-AMONG-US-Online-Launcher-v0.7.0-dev.4-x64.exe` nadal pokazuje ekran PARADOX, grę Impostor Hunt i zadanie matematyczne. Po odblokowaniu przycisku instalacji pobiera wymagane składniki przez HTTPS.

## Co pobiera sam?

- **BepInEx Unity IL2CPP Windows x64**: aktualnie zatwierdzony dla PARADOX wariant `6.0.0-be.790+4e98359` z `builds.bepinex.dev`, bez Mono lub x86. SHA256 pobranego ZIP musi zgadzać się z manifestem. W przeciwnym razie instalacja się zatrzymuje.
- **Reactor 2.5.1-ci.400**: kompatybilny z binarką PARADOX build z `nuget.reactor.gg`, pobierany jako NuGet `.nupkg` i sprawdzany przez oficjalny SHA512. Nie zamieniamy na dowolny `latest`, który mógłby zepsuć RPC lub Harmony.
- **Paradox.dll**: dostarczony z launcherem, tak aby Reactor był zgodny z konkretną kompilacją.

## Automatyczna aktualizacja

Przy każdej instalacji launcher sprawdza manifest kompatybilności:
[launcher_python/dependencies.json na gałęzi main](https://github.com/DonMillson/PARADOX/blob/main/launcher_python/dependencies.json).

Gdy opublikujemy i zatwierdzimy nowy BepInEx, manifest może wskazać nową sprawdzoną paczkę bez ponownego wypuszczania całego launchera. Nowy Reactor, zmiana głównej wersji gry lub nowa wersja samego PARADOX wymagają przebudowy i testów — launcher celowo odmówi instalacji niezgodnego zestawu.

Wszystkie pliki są sprawdzane przed skopiowaniem do folderu gry; ZIP jest sprawdzany pod kątem ścieżek poza folderem docelowym. Program utrzymuje weryfikowany cache lokalny; brak dostępu do internetu uniemożliwi nową instalację, jeśli paczek nie ma w cache.

## Instalacja

1. Pobierz instalator online z zielonego workflow `Build PARADOX AMONG US Online Launcher EXE` w [GitHub Actions](https://github.com/DonMillson/PARADOX/actions).
2. Uruchom EXE, ukończ Impostor Hunt i kod matematyczny.
3. Sprawdź wykryty folder Among Us lub wybierz go ręcznie.
4. Kliknij **ZAINSTALUJ MODA**. Program pobierze i zweryfikuje paczki, wykona kopie zapasowe i instaluje kompletne BepInEx / Reactor / PARADOX. Pasek postępu obejmuje pobieranie i faktyczne kopiowanie.
5. Dopiero po sukcesie użyj przycisku uruchomienia gry.

**Zastrzeżenie:** kontrola sum, kompilacja DLL i testy instalatora nie zastępują testów uruchomienia prawdziwego Among Us 19.0.0 i multiplayer. Oficjalne `latest` może być nowsze, ale niekoniecznie zgodne z modem, dlatego używamy najnowszego *zaakceptowanego* wariantu.

## Dla twórcy

- `online_dependencies.py`: pobieranie, walidacja wersji i sum, cache, bezpieczna ekstrakcja.
- `dependencies.json`: zatwierdzony zestaw `Among Us 19 / PARADOX dev.4 / Reactor CI400 / BepInEx`.
- `test_online_dependencies.py`: testy walidacji złośliwych adresów, ścieżek ZIP, checksum, zgodności wersji.
- `.github/workflows/python-launcher.yml`: build EXE bez dołączania paczek BepInEx/Reactor, ale z ich rzeczywistym pobraniem i testem instalacji w CI.
