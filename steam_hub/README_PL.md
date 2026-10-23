# PARADOX HUB — Steam companion app (Windows x64, proof-of-concept)

PARADOX HUB to **osobna aplikacja PARADOX**, którą można już dodać do biblioteki Steam jako *grę spoza Steam*. Nie jest to jeszcze aplikacja opublikowana w sklepie Steam; publikacja wymaga Steamworks, sprawdzenia zgodności z prawami własności intelektualnej i weryfikacji Valve.

## Co robi

- Wykrywa **lokalną instalację Among Us z oficjalnego manifestu Steam AppID 945360**. Dla innych lokalizacji pozwala wskazać folder z grą. Nie pobiera gry, nie obchodzi licencji Steam.
- Własny launcher PARADOX w stylu statku, załoganta, konsol Security/Engineering i ostrzegawczych pasów, a nie AI-dashboard.
- Ma osobne ekrany **GRA**, **MOD**, **DIAGNOSTYKA**, **O PROJEKCIE**.
- Uruchamia legalną instalację Among Us za pomocą `steam://rungameid/945360`.
- Pobiera zweryfikowany oficjalny **BepInEx IL2CPP** oraz zgodną wersję **Reactor CI400**, kopiuje wbudowany `Paradox.dll` z użyciem istniejącego transactional installer (backup + manifest + rollback).
- Pokazuje postęp pobierania i kopiowania, pozwala ponowić instalację i bezpiecznie odinstalować pliki kontrolowane manifestem.
- Rozróżnia komplet plików, log BepInEx, start pluginu, zakładkę ustawień hosta i jej otwarcie — **sam napis BepInEx to nie sukces moda**.
- Pozwala wybrać eksperymentalny limit lobby 15/20/30/40/50 na własnym serwerze.
- Udostępnia lokalny log i raport diagnostyczny, bez wysyłania danych konta i kodów pokojów na serwer.

## Używanie z biblioteką Steam już teraz

1. Pobierz i uruchom `PARADOX-Hub-v0.7.0-dev.7-x64.exe`, a w zakładce **GRA** upewnij się, że wykryto Among Us.
2. Wejdź w **MOD** i wybierz **ZAINSTALUJ / NAPRAW**. Zamknij Among Us przed instalacją.
3. Użyj **URUCHOM AMONG US PRZEZ STEAM**.
4. W grze załóż własny pokój, przejdź do opcji i znajdź zakładkę PARADOX; awaryjnie naciśnij F10.
5. Zamknij Among Us i w zakładce **DIAGNOSTYKA** sprawdź, czy rejestrowana jest gotowość interfejsu.
6. Aby mieć PARADOX obok gier Steam, w kliencie Steam wybierz **Gry → Dodaj grę spoza Steam → wybierz plik PARADOX Hub**. To tylko skrót w bibliotece; aktualizacje PARADOX w tym trybie nie są dostarczane automatycznie przez Steam.

Instrukcja Valve: https://help.steampowered.com/en/faqs/view/4B8B-9697-2338-40EC

## Przed publikacją na Steam Store

- [ ] Program powinien otrzymać własny Steam AppID za pośrednictwem **Steamworks** oraz przejść sprawdzenie Valve. Wymagane konto partnera, opłata Steam Direct i spełnienie regulaminu dystrybucji. Nie używać AppID Among Us jako własnego produktu.
- [ ] Dodać mechanizm autoaktualizacji przez SteamPipe i historię wydań — dostępne dopiero po utworzeniu aplikacji w Steamworks.
- [ ] Zweryfikować zgodność projektu z polityką Innersloth, **w tym widoczne oznaczenie moda podczas całej rozgrywki**, wymagane zastrzeżenie prawne oraz rejestrację zmieniającego rozgrywkę moda przy użyciu oficjalnych serwerów.
- [ ] Potwierdzić w grze działanie PARADOX na Among Us 19.0.0, nie tylko kompilację.
- [ ] Nie sprzedawać i nie dołączać kopii Among Us, nie przedstawiać aplikacji jako oficjalnie autoryzowanej.
- [ ] Rozważyć osobne panele administracyjne serwera do 50 osób dopiero po jego realnych testach.

Polityka Innersloth: https://www.innersloth.com/among-us-mod-policy/

**Zastrzeżenie wymagane przez politykę moda:**

This mod is not affiliated with Among Us or Innersloth LLC, and the content contained therein is not endorsed or otherwise sponsored by Innersloth LLC. Portions of the materials contained herein are property of Innersloth LLC. © Innersloth LLC.

## Budowanie

Nowy workflow `.github/workflows/steam-hub.yml` na GitHub Actions (Windows) kompiluje `Paradox.dll`, uruchamia testy Python, tworzy pojedynczy plik EXE z PyInstaller i dołącza biblioteki konieczne do instalacji. Sam Among Us **nie jest** częścią tego pliku.
