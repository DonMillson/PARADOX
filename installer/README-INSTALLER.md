# PARADOX — automatyczny instalator Windows

Uruchom `PARADOX-Setup-v0.7.0-dev.4-AmongUs19-x64.exe` jako administrator.

Instalator automatycznie wyszukuje Among Us w folderach Steam i bibliotekach dysków. Jeśli gry nie znajdzie, wskaż folder zawierający `Among Us.exe`.

Wraz z PARADOX automatycznie instalowane są:
- BepInEx Unity IL2CPP dla Windows x64, build **6.0.0-be.790+4e98359** (oficjalne archiwum BepInEx, pobrane przy kompilowaniu instalatora).
- Reactor **2.5.1-ci.400** — wersja zależności, z którą kompilowany jest PARADOX.
- PARADOX **0.7.0-dev.4** (`Paradox.dll`).

Po instalacji uruchom Among Us. W razie błędu zajrzyj do `BepInEx/LogOutput.log` w folderze gry.

**Odinstalowanie:** Panel sterowania Windows → Aplikacje → PARADOX Mod – Among Us → Odinstaluj. Instalator trzyma listę plików i ich sumy SHA-256. Przywraca wcześniejsze wersje nadpisanych plików, usuwa wyłącznie niezmienione pliki własnej instalacji i pozostawia inne mody oraz osobne konfiguracje.

Instalator nie pobiera plików z internetu w trakcie instalowania, nie modyfikuje `Among Us.exe` i nie dostarcza samej gry.

**UWAGA:** Mod jest eksperymentalnym buildem dla Among Us 19.0.0 na Windows x64. Zaliczone testy kompilacji, instalacji i deinstalacji nie potwierdzają jeszcze działania w prawdziwym meczu i nie gwarantują 100% zgodności.

Kod oraz pakiety źródłowe:
- https://github.com/DonMillson/PARADOX
- https://builds.bepinex.dev/projects/bepinex_be
- https://nuget.reactor.gg
