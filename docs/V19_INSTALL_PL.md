# PARADOX v0.7.0-dev.4 — instalacja testowa Among Us v19.0.0 (2026.9.29)

**Status: build kompatybilności do TESTÓW, NIE zweryfikowana wersja grywalna.**
Zielony build nie gwarantuje, że Reactor, BepInEx, Harmony, RPC i wszystkie role zadziałają na prawdziwym kliencie.

## Dlaczego osobny pakiet?

- Among Us 19.0.0 / Steam 2026.9.29 korzysta z 64-bitowego pliku wykonywalnego.
- Reactor **oficjalne wydanie** 2.5.1 wspiera tylko grę do 17.4. PARADOX dev.4 przechodzi na pakiet eksperymentalny `Reactor 2.5.1-ci.400`, którego używa zgodny z 19.0.0 mod PropHunt; to mocna wskazówka, lecz nie dowód kompatybilności PARADOX.
- Referencje `AmongUs.GameLibs.Steam 2026.9.29` odpowiadają aktualnej bazie gry.
- Kompilacja ma osobny instalacyjny artefakt zawierający **Paradox.dll** i odpowiadający mu **Reactor.dll** z pakietu NuGet; nie kopiuj starego Reactor 2.5.1 ze starszego wydania.

## Instrukcja Windows Steam / Epic / Microsoft Store (x64)

1. Zaktualizuj grę do wersji **19.0.0 (2026.9.29)** i uruchom ją czysto bez modów; sprawdź czy sama gra startuje.
2. Zachowaj kopię folderu gry i `BepInEx/config`; nie mieszaj plików modyfikacji z różnych wersji Among Us.
3. Z oficjalnej strony [BepInEx Bleeding Edge](https://builds.bepinex.dev/projects/bepinex_be) pobierz **BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785+6abdba4.zip** (nie Mono i nie x86). Rozpakuj do katalogu z `Among Us.exe`; uruchom grę raz, aby BepInEx utworzył swoje foldery, potem ją zamknij.
4. Wejdź w [GitHub Actions PARADOX](https://github.com/DonMillson/PARADOX/actions) i pobierz artefakt `PARADOX-v0.7.0-dev.4-v19-test-NOT-RUNTIME-VERIFIED` z zielonego buildu. Rozpakuj na dysk.
5. Uruchom PowerShell w wypakowanym folderze. Wywołaj:
   ```powershell
   .\install-v19.ps1 -AmongUsPath "C:\Program Files (x86)\Steam\steamapps\common\Among Us"
   ```
   Można też skopiować obydwa pliki z `BepInEx/plugins` pakietu do `BepInEx/plugins` gry. Skrypt odmówi działania przy x86, braku BepInEx i braku plików DLL.
6. Włącz grę. Sprawdź `BepInEx/LogOutput.log`, czy widać **PARADOX**, poprawną wersję, Reactor oraz komunikat o ładowaniu. Jeśli pojawi się `Harmony patch failed`, `TypeLoadException`, `MissingMethodException`, `Il2CppInterop` albo zawieszenie, zachowaj log do dalszego debugowania.
7. Jeśli ładowanie działa, utwórz prywatny pokój z inną osobą z dokładnie tymi samymi wersjami moda, Reactora i BepInEx. Przetestuj dołączenie, synchronizację ról, rozpoczęcie gry, naradę, eliminację, cooldowny, reset lobby i koniec gry.

**Ważne:** ten pakiet nie zawiera samej gry ani BepInEx; biblioteki BepInEx są pobierane z oficjalnego źródła. Nie używaj pliku BepInEx x86 dla 64-bitowej gry Steam.

## Kryteria potwierdzenia działania

- [ ] Start gry 19.0.0 bez błędów w `LogOutput.log` (x64, BepInEx, Reactor, Paradox)
- [ ] Role, menu PL/EN i lobby poprawnie się wyświetlają
- [ ] Minimum dwaj gracze z tą samą wersją PARADOX mogą wejść do jednej gry
- [ ] Przebranie Doppelgängera synchronizuje się u obu osób
- [ ] Swapper naprawdę zmienia wynik głosowania, a kara działa tylko po wyrzuceniu załoganta
- [ ] Sheriff trafia Impostora i ponosi karę po błędnym strzale
- [ ] Rift Warden odblokowuje efekty PARADOX u obu klientów
- [ ] Po naradzie i zmianie mapy / zakończeniu gry nie ma crashy, duplikatów UI ani rozjechania RPC
- [ ] Długi test wszystkich 43 ról, w tym kolizji różnych zdolności

## Rozwiązywanie problemów

- **Brak logów BepInEx:** najpierw napraw wersję/architekturę BepInEx. Sama DLL moda nie może naprawić loadera.
- **Log nie wykazuje Reactor:** sprawdź `BepInEx/plugins/Reactor.dll` w wersji zgodnej z buildem, nie stare wydanie z czerwca.
- **PARADOX nie ładuje się:** przejrzyj log i sprawdź zależność `[BepInDependency(ReactorPlugin.Id)]`.
- **Nie działa tylko część ról:** kompilacja nie testuje rzeczywistych hooków Harmony / transakcji RPC; zapisuj nazwę roli, moment użycia i log błędu.

Źródła: Reactor CI [nuget.reactor.gg](https://nuget.reactor.gg/), projekt [PropHunt](https://github.com/ugackMiner53/PropHunt), [BepInEx](https://builds.bepinex.dev/projects/bepinex_be), [Among Us Steam](https://store.steampowered.com/app/945360/Among_Us/).

**Nie publikować jako „100% działa na Among Us 19”, dopóki checklisty z prawdziwej gry nie przejdą.**
