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
        Add(l, "event.100", "PARADOX EVENT", "ZDARZENIE PARADOKSU");

        AddRole(l, "Doppelganger", "Doppelgänger", "Sobowtór",
            "Copy another player's identity temporarily.", "Tymczasowo skopiuj tożsamość innego gracza.");
        AddRole(l, "Parasite", "Parasite", "Pasożyt",
            "Infect players and let the infection develop.", "Zarażaj graczy i pozwól infekcji się rozwijać.");
        AddRole(l, "Anomaly", "Anomaly", "Anomalia",
            "Drive the Paradox Meter toward 100%.", "Doprowadź Miernik Paradoksu do 100%.");
        AddRole(l, "Observer", "Observer", "Obserwator",
            "Detect traces left by unusual abilities.", "Wykrywaj ślady pozostawione przez niezwykłe zdolności.");
        AddRole(l, "Witness", "Witness", "Świadek",
            "Discover an incomplete clue near a body.", "Odkryj niepełną wskazówkę przy znalezionym ciele.");

        AddRoleName(l, "Puppeteer", "Puppeteer", "Lalkarz");
        AddRole(l, "Cleaner", "Cleaner", "Czyściciel",
            "Remove nearby bodies before the crew can report them.", "Usuwaj pobliskie ciała, zanim załoga zdąży je zgłosić.");
        Add(l, "role.Cleaner.ability", "CLEAN", "USUN");
        AddRoleName(l, "Blackmailer", "Blackmailer", "Szantażysta");
        AddRoleName(l, "Illusionist", "Illusionist", "Iluzjonista");
        AddRoleName(l, "Corruptor", "Corruptor", "Deprawator");
        AddRoleName(l, "Devourer", "Devourer", "Pożeracz");
        AddRoleName(l, "Timebreaker", "Timebreaker", "Łamacz Czasu");
        AddRoleName(l, "Nightmare", "Nightmare", "Koszmar");
        AddRoleName(l, "ShapeshifterX", "Shapeshifter X", "Zmiennokształtny X");
        AddRoleName(l, "Saboteur", "Saboteur", "Sabotażysta");
        AddRoleName(l, "Undertaker", "Undertaker", "Grabarz");
        AddRoleName(l, "Silencer", "Silencer", "Wyciszacz");
        AddRoleName(l, "Riftmaker", "Riftmaker", "Twórca Szczelin");

        AddRoleName(l, "EngineerX", "Engineer X", "Inżynier X");
        AddRoleName(l, "Guardian", "Guardian", "Strażnik");
        AddRoleName(l, "Chronologist", "Chronologist", "Chronolog");
        AddRoleName(l, "Detective", "Detective", "Detektyw");
        AddRoleName(l, "Medic", "Medic", "Medyk");
        AddRoleName(l, "Tracker", "Tracker", "Tropiciel");
        AddRoleName(l, "Locksmith", "Locksmith", "Ślusarz");
        AddRoleName(l, "Analyst", "Analyst", "Analityk");
        AddRoleName(l, "Technician", "Technician", "Technik");
        AddRoleName(l, "Seer", "Seer", "Wieszcz");
        AddRoleName(l, "Dispatcher", "Dispatcher", "Dyspozytor");
        AddRoleName(l, "Forensic", "Forensic", "Kryminalistyk");
        AddRoleName(l, "Stabilizer", "Stabilizer", "Stabilizator");

        AddRoleName(l, "Forgotten", "Forgotten", "Zapomniany");
        AddRoleName(l, "Collector", "Collector", "Kolekcjoner");
        AddRoleName(l, "BountyHunter", "Bounty Hunter", "Łowca Nagród");
        AddRoleName(l, "Survivor", "Survivor", "Ocalały");
        AddRoleName(l, "Revenant", "Revenant", "Powracający");
        AddRoleName(l, "JesterX", "Jester X", "Błazen X");
        AddRoleName(l, "Phantom", "Phantom", "Fantom");
        AddRoleName(l, "Opportunist", "Opportunist", "Oportunista");
        AddRoleName(l, "Harbinger", "Harbinger", "Zwiastun");

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
