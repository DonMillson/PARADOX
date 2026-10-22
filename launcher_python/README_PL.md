# PARADOX AMONG US — Launcher Online

**Jeden plik EXE** z ekranem powitalnym, mini-grą w łapanie Impostora, kodem matematycznym i instalacją. W nowej wersji launcher nie pakuje w sobie całego BepInEx/Reactora. Dzięki temu rozmiar pliku jest mniejszy, a instalator za każdym razem sprawdza oficjalne serwery.

## Przebieg

1. Uruchom `PARADOX-AMONG-US-Online-Launcher-v0.7.0-dev.5-x64.exe` jako administrator na Windows x64.
2. Przejdź mini-grę (5 kliknięć Impostora) i zadanie matematyczne. Instalacja jest wcześniej zablokowana.
3. Launcher wykrywa Among Us w bibliotece Steam lub pozwala wskazać katalog ręcznie.
   W tym samym oknie można zaznaczyć **Eksperymentalne duże lobby** i wybrać
   15, 20, 30, 40 lub 50 graczy. Opcja wymaga własnego zgodnego serwera i
   nie znosi limitu publicznych serwerów Among Us. Bez zaznaczenia
   pozostaje bezpieczny standard 15.
4. Po kliknięciu „Zainstaluj moda” odczytuje [listę zgodności PARADOX](compatibility.json) z GitHub (z dołączonym zapasowym manifestem), pobiera **zweryfikowany BepInEx Unity.IL2CPP win-x64 be.790** (bez automatycznej zamiany na nieprzetestowany nowszy build) na [oficjalnym serwerze](https://builds.bepinex.dev/projects/bepinex_be), pobiera je i sprawdza typ paczki oraz sumę SHA-256 dla przypiętej wersji zapasowej.
5. Reactor pobierany jest jako oficjalny pakiet NuGet z [nuget.reactor.gg](https://nuget.reactor.gg/). **Wersja Reactor 2.5.1-ci.400 pozostaje przypięta do skompilowanego PARADOX dev.5**, a archiwum weryfikowane jest sumą SHA-512 udostępnianą przez NuGet.
6. Launcher wypakowuje pliki do katalogu tymczasowego i uruchamia dotychczasowy instalator z manifestem plików, kopiami zapasowymi i rollbackiem. Dopiero po pobraniu i sprawdzeniu wszystkich komponentów modyfikuje folder gry.
7. Po sukcesie instalator automatycznie zapisuje wybór dużego lobby do
   `BepInEx/config/gg.paradox.mod.cfg`, zachowując wcześniejsze ustawienia i
   kopię bezpieczeństwa. Nie trzeba ręcznie edytować pliku. Po tym można
   uruchomić Among Us.
8. Po instalacji launcher informuje jedynie o skopiowaniu plików. Kliknij **URUCHOM AMONG US**, a potem **SPRAWDŹ MODY**. Status „załadowany” jest pokazywany dopiero po znalezieniu potwierdzenia startu PARADOX w świeżym logu BepInEx. Jeśli brakuje loadera, Reactora, DLL albo Doorstop jest wyłączony, launcher pokaże błąd.
9. W razie problemów sprawdź `BepInEx/LogOutput.log` **lub** `BepInEx/LogOutput.txt`. Brak logu po uruchomieniu gry oznacza, że wstrzyknięcie BepInEx prawdopodobnie nie nastąpiło. Nawet poprawny log startu nie dowodzi jeszcze działania ról i multiplayer.

**Dlaczego Reactor nie zawsze „latest”?** Nowy Reactor może wymagać rekompilacji PARADOX i zmienić API; automatycznie aktualizujemy źródło kompatybilności, ale nie podstawiamy niezweryfikowanego wydania. Jeśli aktualna lista wymaga innej wersji niż wbudowany mod, instalator zatrzyma się i poprosi o nowy build launchera.

**Bezpieczeństwo:** tylko HTTPS do zweryfikowanych domen, odrzucanie obcych przekierowań, limity wielkości pobrań, kontrola archiwum i ścieżek w ZIP, SHA-512 Reactor oraz backupy istniejących plików. Do instalacji wymagane jest połączenie z internetem. W razie niedostępności serwerów program nie symuluje sukcesu.

**Ważne:** aktualne zależności **nie gwarantują**, że wszystkie role PARADOX działają w Among Us 19.0.0. Potrzebne są testy rozruchu i multiplayer.

## Budowa i testy

Workflow: `.github/workflows/python-launcher.yml`. Testy resolvera korzystają z mocków oficjalnych API, bez pobierania z Internetu. Kompilator moda używa `AmongUs.GameLibs.Steam 2026.9.29`, a PyInstaller dołącza tylko `Paradox.dll`, dane zgodności i skrypt instalacji.
