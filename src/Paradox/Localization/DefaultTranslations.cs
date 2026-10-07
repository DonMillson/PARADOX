namespace Paradox.Localization;

public static class DefaultTranslations
{
    public static Localizer Create()
    {
        var l = new Localizer();

        Add(l, "ui.paradoxMeter", "Paradox Meter", "Miernik Paradoksu");
        Add(l, "ui.menu.title", "PARADOX SETTINGS", "USTAWIENIA PARADOX");
        Add(l, "ui.menu.open", "PARADOX", "PARADOX");
        Add(l, "ui.menu.roles", "ROLES", "ROLE");
        Add(l, "ui.menu.mechanics", "MECHANICS", "MECHANIKI");
        Add(l, "ui.menu.meter", "PARADOX METER", "MIERNIK");
        Add(l, "ui.menu.language", "LANGUAGE", "JĘZYK");
        Add(l, "ui.menu.close", "CLOSE", "ZAMKNIJ");
        Add(l, "ui.menu.previous", "PREV", "WSTECZ");
        Add(l, "ui.menu.next", "NEXT", "DALEJ");
        Add(l, "ui.menu.enabled", "ON", "WŁ.");
        Add(l, "ui.menu.disabled", "OFF", "WYŁ.");
        Add(l, "ui.menu.planned", "PLANNED", "PLANOWANA");
        Add(l, "ui.menu.spawn", "Spawn", "Szansa");
        Add(l, "ui.menu.roleHint", "Click ON/OFF or chance to change host settings.", "Kliknij WŁ./WYŁ. lub szansę, aby zmienić ustawienia hosta.");
        Add(l, "ui.menu.notHost", "Role settings can be changed by the host.", "Ustawienia ról może zmieniać host.");
        Add(l, "ui.menu.languageHint", "Choose the PARADOX interface language.", "Wybierz język interfejsu PARADOX.");
        Add(l, "ui.menu.mechanicsTitle", "GAMEPLAY MECHANICS", "MECHANIKI ROZGRYWKI");
        Add(l, "ui.menu.mechanicsHost", "Host controls enabled roles and spawn chances.", "Host kontroluje aktywne role i szanse ich pojawienia.");
        Add(l, "ui.menu.mechanicsSaved", "Language and role settings are saved between launches.", "Język i ustawienia ról są zapisywane między uruchomieniami.");
        Add(l, "ui.menu.mechanicsMore", "More role mechanics will appear here as they are completed.", "Kolejne mechaniki ról pojawią się tutaj wraz z ich ukończeniem.");
        Add(l, "ui.menu.meterTitle", "PARADOX METER", "MIERNIK PARADOKSU");
        Add(l, "ui.menu.kill", "Kill +10", "Zabójstwo +10");
        Add(l, "ui.menu.sabotage", "Sabotage +4", "Sabotaż +4");
        Add(l, "ui.menu.ability", "Role ability +2", "Umiejętność roli +2");
        Add(l, "ui.menu.anomaly", "Anomaly +5", "Anomalia +5");
        Add(l, "ui.menu.thresholds", "Events at 25 / 50 / 75 / 100%", "Zdarzenia przy 25 / 50 / 75 / 100%");
        Add(l, "ui.language.english", "English", "Angielski");
        Add(l, "ui.language.polish", "Polish", "Polski");

        Add(l, "map.paradoxStation.name", "Paradox Station", "Stacja Paradoks");
        Add(l, "event.25", "Reality disturbance", "Zakłócenie rzeczywistości");
        Add(l, "event.25.detail", "Reality flickers for a few seconds.", "Rzeczywistość migocze przez kilka sekund.");
        Add(l, "event.50", "Reality distortion", "Zniekształcenie rzeczywistości");
        Add(l, "event.50.detail", "Special role abilities are jammed for 6 seconds.", "Specjalne umiejętności ról są zablokowane na 6 sekund.");
        Add(l, "event.75", "Critical instability", "Krytyczna niestabilność");
        Add(l, "event.75.detail", "Reality collapses into heavy flicker and role abilities are jammed for 8 seconds.", "Rzeczywistość wpada w silne zakłócenia, a umiejętności ról są zablokowane na 8 sekund.");
        Add(l, "event.100", "PARADOX EVENT", "ZDARZENIE PARADOKSU");
        Add(l, "event.100.realityStorm", "Reality Storm: violent reality pulses and camera instability for 10 seconds.", "Burza Rzeczywistości: gwałtowne pulsowanie obrazu i niestabilna kamera przez 10 sekund.");
        Add(l, "event.100.blackout", "Blackout: visibility collapses into a deep global blackout for 10 seconds.", "Blackout: widoczność zapada się w głęboką globalną ciemność na 10 sekund.");
        Add(l, "event.100.nullField", "Null Field: reality darkens and all special role abilities are jammed for 12 seconds.", "Pole Zerowe: rzeczywistość ciemnieje, a wszystkie specjalne umiejętności ról są zablokowane na 12 sekund.");

        AddRole(l, "Doppelganger", "Doppelgänger", "Sobowtór",
            "Copy another player's identity temporarily.", "Tymczasowo skopiuj tożsamość innego gracza.");
        AddRole(l, "Parasite", "Parasite", "Pasożyt",
            "Infect players and let the infection develop.", "Zarażaj graczy i pozwól infekcji się rozwijać.");
        Add(l, "role.Parasite.ability", "INFECT", "ZARAŹ");
        Add(l, "role.Parasite.feedback.source", "Target infected. Incubation: {seconds}s.", "Cel zarażony. Inkubacja: {seconds}s.");
        Add(l, "role.Parasite.feedback.target", "Something feels wrong...", "Coś jest nie tak...");
        AddRole(l, "Anomaly", "Anomaly", "Anomalia",
            "Drive the Paradox Meter toward 100%.", "Doprowadź Miernik Paradoksu do 100%.");
        Add(l, "role.Anomaly.ability", "RUPTURE", "ROZERWIJ");
        Add(l, "role.Anomaly.pulse", "Reality rupture: Paradox Meter +5.", "Rozdarcie rzeczywistości: Miernik Paradoksu +5.");
        Add(l, "role.Anomaly.win", "ANOMALY WINS", "ANOMALIA WYGRYWA");
        AddRole(l, "Observer", "Observer", "Obserwator",
            "Detect traces left by unusual abilities.", "Wykrywaj ślady pozostawione przez niezwykłe zdolności.");
        Add(l, "role.Observer.trace", "Observer: ability trace detected (player {player}).", "Obserwator: wykryto ślad użycia zdolności (gracz {player}).");
        AddRole(l, "Witness", "Witness", "Świadek",
            "Discover an incomplete clue near a body.", "Odkryj niepełną wskazówkę przy znalezionym ciele.");
        Add(l, "role.Witness.clue.title", "WITNESS CLUE", "WSKAZÓWKA ŚWIADKA");

        AddRoleName(l, "Puppeteer", "Puppeteer", "Lalkarz");
        AddRole(l, "Cleaner", "Cleaner", "Czyściciel",
            "Remove nearby bodies before the crew can report them.", "Usuwaj pobliskie ciała, zanim załoga zdąży je zgłosić.");
        Add(l, "role.Cleaner.ability", "CLEAN", "USUN");
        AddRoleName(l, "Blackmailer", "Blackmailer", "Szantażysta");
        AddRoleName(l, "Illusionist", "Illusionist", "Iluzjonista");
        AddRole(l, "Corruptor", "Corruptor", "Deprawator",
            "Corrupt a nearby player and block their special role ability temporarily.", "Deprawuj pobliskiego gracza i czasowo blokuj jego specjalną zdolność roli.");
        Add(l, "role.Corruptor.ability", "CORRUPT", "DEPRAWUJ");
        Add(l, "role.Corruptor.feedback.source", "Target corrupted for {seconds}s.", "Cel skażony na {seconds}s.");
        Add(l, "role.Corruptor.feedback.target", "Your special role ability is corrupted.", "Twoja specjalna zdolność roli została skażona.");
        AddRole(l, "Devourer", "Devourer", "Pożeracz",
            "Consume a nearby victim; the kill leaves no reportable body.", "Pożeraj pobliską ofiarę; po zabójstwie nie pozostaje ciało do zgłoszenia.");
        Add(l, "role.Devourer.ability", "DEVOUR", "POŻRYJ");
        Add(l, "role.Devourer.feedback", "Victim consumed. No body remains.", "Ofiara pożarta. Nie pozostało ciało.");
        AddRoleName(l, "Timebreaker", "Timebreaker", "Łamacz Czasu");
        AddRole(l, "Nightmare", "Nightmare", "Koszmar",
            "Haunt a nearby player with a short synchronized fear and vision-distortion effect.", "Nawiedzaj pobliskiego gracza krótkim, zsynchronizowanym efektem strachu i zaburzonej widoczności.");
        Add(l, "role.Nightmare.ability", "HAUNT", "NAWIEDŹ");
        Add(l, "role.Nightmare.feedback.source", "Nightmare effect applied for {seconds}s.", "Efekt Koszmaru nałożony na {seconds}s.");
        Add(l, "role.Nightmare.feedback.target", "A nightmare closes in...", "Koszmar zaciska się wokół ciebie...");
        AddRoleName(l, "ShapeshifterX", "Shapeshifter X", "Zmiennokształtny X");
        AddRoleName(l, "Saboteur", "Saboteur", "Sabotażysta");
        AddRoleName(l, "Undertaker", "Undertaker", "Grabarz");
        AddRoleName(l, "Silencer", "Silencer", "Wyciszacz");
        AddRole(l, "Riftmaker", "Riftmaker", "Twórca Szczelin",
            "Place a personal rift anchor, then warp back to it on the next use.", "Ustaw osobistą kotwicę szczeliny, a przy kolejnym użyciu wróć do niej.");
        Add(l, "role.Riftmaker.anchor", "ANCHOR", "KOTWICA");
        Add(l, "role.Riftmaker.rift", "RIFT", "SZCZELINA");
        Add(l, "role.Riftmaker.feedback.anchor", "Rift anchor placed.", "Kotwica szczeliny ustawiona.");
        Add(l, "role.Riftmaker.feedback.warp", "Rift traversal complete.", "Przejście przez szczelinę zakończone.");

        AddRole(l, "EngineerX", "Engineer X", "Inżynier X",
            "Use Emergency Stabilize to reduce dangerous Paradox instability.", "Użyj Awaryjnej Stabilizacji, aby zmniejszyć niebezpieczną niestabilność Paradoksu.");
        Add(l, "role.EngineerX.ability", "EMERGENCY FIX", "NAPRAWA AWARYJNA");
        Add(l, "role.EngineerX.feedback", "Emergency stabilization complete: Paradox Meter -{amount}.", "Awaryjna stabilizacja zakończona: Miernik Paradoksu -{amount}.");
        AddRole(l, "Guardian", "Guardian", "Strażnik",
            "Protect a nearby player from one murder for a short time.", "Chroń pobliskiego gracza przed jednym zabójstwem przez krótki czas.");
        Add(l, "role.Guardian.ability", "PROTECT", "CHROŃ");
        Add(l, "role.Guardian.feedback.source", "Protection deployed.", "Ochrona aktywowana.");
        Add(l, "role.Guardian.feedback.target", "A Guardian is protecting you.", "Strażnik cię chroni.");
        Add(l, "role.Guardian.blocked", "Guardian protection blocked the attack.", "Ochrona Strażnika zablokowała atak.");
        AddRole(l, "Chronologist", "Chronologist", "Chronolog",
            "Read the authoritative timeline to learn how long ago the latest death occurred.", "Odczytuj autorytatywną oś czasu, aby ustalić, ile czasu minęło od ostatniego zgonu.");
        Add(l, "role.Chronologist.ability", "TIMELINE", "OŚ CZASU");
        Add(l, "role.Chronologist.result", "Chronologist: latest death {seconds}s ago.", "Chronolog: ostatni zgon {seconds}s temu.");
        Add(l, "role.Chronologist.none", "Chronologist: no death recorded yet.", "Chronolog: nie zarejestrowano jeszcze żadnego zgonu.");
        AddRole(l, "Detective", "Detective", "Detektyw",
            "Scan a nearby player for recent violent activity linked to recorded deaths.", "Badaj pobliskiego gracza pod kątem niedawnej gwałtownej aktywności powiązanej z zapisanymi zgonami.");
        Add(l, "role.Detective.ability", "INVESTIGATE", "ZBADAJ");
        Add(l, "role.Detective.result.hot", "Detective: recent violent residue DETECTED.", "Detektyw: WYKRYTO niedawny ślad gwałtownej aktywności.");
        Add(l, "role.Detective.result.clear", "Detective: no recent violent residue detected.", "Detektyw: nie wykryto niedawnego śladu gwałtownej aktywności.");
        AddRole(l, "Medic", "Medic", "Medyk",
            "Scan nearby players and cure active Parasite infections.", "Badaj pobliskich graczy i usuwaj aktywne infekcje Pasożyta.");
        Add(l, "role.Medic.ability", "SCAN", "BADAJ");
        Add(l, "role.Medic.feedback.healthy", "Scan complete: no active Parasite infection detected.", "Badanie zakończone: nie wykryto aktywnej infekcji Pasożyta.");
        Add(l, "role.Medic.feedback.cured", "Scan complete: Parasite infection detected and removed.", "Badanie zakończone: wykryto i usunięto infekcję Pasożyta.");
        Add(l, "role.Medic.feedback.target", "A Medic removed your Parasite infection.", "Medyk usunął twoją infekcję Pasożyta.");
        AddRole(l, "Tracker", "Tracker", "Tropiciel",
            "Track a selected player's live position for a short time.", "Śledź przez krótki czas aktualną pozycję wybranego gracza.");
        Add(l, "role.Tracker.ability", "TRACK", "ŚLEDŹ");
        Add(l, "role.Tracker.active", "TRACK {distance}", "ŚLAD {distance}");
        Add(l, "role.Tracker.feedback", "Target acquired for {seconds}s.", "Cel namierzony na {seconds}s.");
        AddRoleName(l, "Locksmith", "Locksmith", "Ślusarz");
        AddRole(l, "Analyst", "Analyst", "Analityk",
            "Analyze limited live match telemetry without identifying players.", "Analizuj ograniczoną telemetrię meczu bez identyfikowania graczy.");
        Add(l, "role.Analyst.ability", "ANALYZE", "ANALIZUJ");
        Add(l, "role.Analyst.result", "Analyst: alive {alive} | deaths {deaths} | traces {traces} | meter {meter}%.", "Analityk: żywi {alive} | zgony {deaths} | ślady {traces} | miernik {meter}%.");
        AddRoleName(l, "Technician", "Technician", "Technik");
        AddRole(l, "Seer", "Seer", "Wieszcz",
            "Read whether a nearby player has left a fresh special-ability trace.", "Odczytuj, czy pobliski gracz pozostawił świeży ślad użycia specjalnej zdolności.");
        Add(l, "role.Seer.ability", "READ AURA", "CZYTAJ AURĘ");
        Add(l, "role.Seer.result.disturbed", "Seer: the aura is DISTURBED by recent ability use.", "Wieszcz: aura jest ZABURZONA przez niedawne użycie zdolności.");
        Add(l, "role.Seer.result.calm", "Seer: the aura is calm.", "Wieszcz: aura jest spokojna.");
        AddRoleName(l, "Dispatcher", "Dispatcher", "Dyspozytor");
        AddRole(l, "Forensic", "Forensic", "Kryminalistyk",
            "Examine a nearby body for real death-time evidence and recent ability residue.", "Badaj pobliskie ciało pod kątem rzeczywistego czasu śmierci i śladów niedawnych zdolności.");
        Add(l, "role.Forensic.ability", "EXAMINE", "ZBADAJ");
        Add(l, "role.Forensic.result", "Forensic: death {seconds}s ago. Ability residue: {residue}.", "Kryminalistyka: śmierć {seconds}s temu. Ślad zdolności: {residue}.");
        Add(l, "role.Forensic.residue.yes", "DETECTED", "WYKRYTY");
        Add(l, "role.Forensic.residue.no", "NONE", "BRAK");
        Add(l, "role.Forensic.result.unknown", "Forensic: evidence record unavailable.", "Kryminalistyka: brak zapisu dowodowego.");
        AddRole(l, "Stabilizer", "Stabilizer", "Stabilizator",
            "Reduce the Paradox Meter and slow reality collapse.", "Obniżaj Miernik Paradoksu i spowalniaj rozpad rzeczywistości.");
        Add(l, "role.Stabilizer.ability", "STABILIZE", "STABILIZUJ");
        Add(l, "role.Stabilizer.feedback", "Reality stabilized: Paradox Meter -{amount}.", "Rzeczywistość ustabilizowana: Miernik Paradoksu -{amount}.");

        AddRoleName(l, "Forgotten", "Forgotten", "Zapomniany");
        AddRole(l, "Collector", "Collector", "Kolekcjoner",
            "Collect unique samples from three living players to claim a solo victory.", "Zbierz unikalne próbki od trzech żywych graczy, aby odnieść samodzielne zwycięstwo.");
        Add(l, "role.Collector.ability", "SAMPLE {count}/{required}", "PRÓBKA {count}/{required}");
        Add(l, "role.Collector.feedback", "Sample secured: {count}/{required}.", "Próbka zdobyta: {count}/{required}.");
        Add(l, "role.Collector.win", "COLLECTOR WINS", "KOLEKCJONER WYGRYWA");
        AddRole(l, "BountyHunter", "Bounty Hunter", "Łowca Nagród",
            "Hunt the assigned bounty target. Eliminating the correct target grants a solo victory.", "Poluj na wyznaczony cel. Eliminacja właściwego celu daje samodzielne zwycięstwo.");
        Add(l, "role.BountyHunter.ability", "EXECUTE", "ELIMINUJ");
        Add(l, "role.BountyHunter.target", "BOUNTY: {player}", "CEL: {player}");
        Add(l, "role.BountyHunter.feedback.target", "New bounty: {player}.", "Nowy cel: {player}.");
        Add(l, "role.BountyHunter.win", "BOUNTY HUNTER WINS", "ŁOWCA NAGRÓD WYGRYWA");
        AddRole(l, "Survivor", "Survivor", "Ocalały",
            "Survive until the match ends to join the winners.", "Przetrwaj do końca meczu, aby dołączyć do zwycięzców.");
        Add(l, "role.Survivor.win", "SURVIVOR ESCAPED", "OCALAŁY PRZETRWAŁ");
        AddRoleName(l, "Revenant", "Revenant", "Powracający");
        AddRoleName(l, "JesterX", "Jester X", "Błazen X");
        AddRoleName(l, "Phantom", "Phantom", "Fantom");
        AddRole(l, "Opportunist", "Opportunist", "Oportunista",
            "If you survive until the match ends while the Paradox Meter is at least 50%, you join the winners.", "Jeśli przeżyjesz do końca meczu przy Mierniku Paradoksu wynoszącym co najmniej 50%, dołączasz do zwycięzców.");
        Add(l, "role.Opportunist.win", "OPPORTUNITY SECURED", "OKAZJA WYKORZYSTANA");
        AddRole(l, "Harbinger", "Harbinger", "Zwiastun",
            "Invoke three Omens and survive until the Paradox Meter reaches at least 75% to claim a special victory.", "Przywołaj trzy Znaki i przeżyj, aż Miernik Paradoksu osiągnie co najmniej 75%, aby odnieść specjalne zwycięstwo.");
        Add(l, "role.Harbinger.ability", "OMEN {count}/{required}", "ZNAK {count}/{required}");
        Add(l, "role.Harbinger.feedback", "Omen invoked: {count}/{required}. Paradox Meter +{amount}.", "Przywołano Znak: {count}/{required}. Miernik Paradoksu +{amount}.");
        Add(l, "role.Harbinger.ready", "The final Omen is complete. Reach 75% Paradox and survive.", "Ostatni Znak ukończony. Osiągnij 75% Paradoksu i przetrwaj.");
        Add(l, "role.Harbinger.win", "HARBINGER WINS", "ZWIASTUN WYGRYWA");

        Add(l, "role.Witness.clue.bodyAge", "The body appears to be recent.", "Ciało wygląda na znalezione niedługo po śmierci.");
        Add(l, "role.Witness.clue.activity", "There are signs of recent activity nearby.", "W pobliżu widać ślady niedawnej aktywności.");

        return l;
    }

    private static void Add(Localizer l, string key, string en, string pl)
    {
        l.Add(Language.English, key, en);
        l.Add(Language.Polish, key, pl);
    }

    private static void AddRole(Localizer l, string id, string enName, string plName, string enDescription, string plDescription)
    {
        Add(l, $"role.{id}.name", enName, plName);
        Add(l, $"role.{id}.description", enDescription, plDescription);
    }

    private static void AddRoleName(Localizer l, string id, string enName, string plName) =>
        Add(l, $"role.{id}.name", enName, plName);
}
