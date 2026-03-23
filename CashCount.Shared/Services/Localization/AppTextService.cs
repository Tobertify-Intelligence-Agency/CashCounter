using System.Globalization;
using Microsoft.JSInterop;

namespace CashCount.Shared.Services.Localization;

public sealed class AppTextService : IAppTextService
{
    private const string LanguageStorageKey = "cashcount_language";
    private readonly IJSRuntime _jsRuntime;

    private static readonly AppLanguage[] _languages =
    {
        new("en", "English", "English", "en-US"),
        new("de", "German", "Deutsch", "de-DE")
    };

    private readonly Dictionary<string, Dictionary<string, string>> _texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["app.tools"] = "Tools",
            ["app.version"] = "v1.0.8",
            ["app.language"] = "Language",
            ["app.languageShort"] = "Lang",
            ["app.currency"] = "Currency",
            ["app.loading"] = "Loading...",
            ["app.cancel"] = "Cancel",
            ["app.delete"] = "Delete",
            ["app.clearAll"] = "Clear all",
            ["app.details"] = "Details",
            ["app.total"] = "Total",
            ["app.saved"] = "saved",
            ["tool.cashCounter"] = "Cash Counter",
            ["tool.savedCounts"] = "Saved Counts",
            ["tool.travelCost"] = "Travel Cost",
            ["tool.savedTrips"] = "Saved Trips",
            ["tool.funds"] = "Funds",
            ["tool.account"] = "Account",
            ["cashCounter.hero.title"] = "Cash Counter",
            ["cashCounter.hero.subtitle"] = "Fast, touch-friendly cash counting with totals that stay visible.",
            ["cashCounter.premium.currencyLocked"] = "Currency change",
            ["cashCounter.premium.currencyLockedHint"] = "Upgrade to Premium to change currency",
            ["cashCounter.currencyFixed"] = "Current currency",
            ["cashCounter.totalSummary"] = "Live total",
            ["cashCounter.banknotes"] = "Banknotes",
            ["cashCounter.coins"] = "Coins",
            ["cashCounter.items"] = "Items",
            ["cashCounter.sectionTotalBanknotes"] = "Banknotes total",
            ["cashCounter.sectionTotalCoins"] = "Coins total",
            ["cashCounter.savePlaceholder"] = "Enter name to save (e.g. Monday count)",
            ["cashCounter.save"] = "Save count",
            ["cashCounter.savedSuccess"] = "Saved successfully!",
            ["cashCounter.saveLocked"] = "Save & load counts",
            ["cashCounter.saveLockedHint"] = "Upgrade to Premium to save counts",
            ["cashCounter.clearAll"] = "Reset count",
            ["travel.hero.eyebrow"] = "Travel",
            ["travel.hero.fallbackName"] = "Unnamed trip",
            ["travel.hero.placeholder"] = "Trip name...",
            ["travel.hero.editHint"] = "Tap to rename",
            ["travel.hero.entries"] = "entries",
            ["travel.summary.income"] = "Income",
            ["travel.summary.expenses"] = "Expenses",
            ["travel.summary.balance"] = "Balance",
            ["travel.composer.title"] = "Add movement",
            ["travel.composer.subtitle"] = "Keep travel income and expenses compact and easy to scan.",
            ["travel.type.expense"] = "Expense",
            ["travel.type.income"] = "Income",
            ["travel.field.description"] = "Description",
            ["travel.field.descriptionPlaceholder"] = "Fuel, hotel, donation...",
            ["travel.field.amount"] = "Amount",
            ["travel.field.category"] = "Category",
            ["travel.field.categoryPlaceholder"] = "Optional category",
            ["travel.field.date"] = "Date",
            ["travel.addButton"] = "+ Add {0}",
            ["travel.list.title"] = "Entries",
            ["travel.filter.all"] = "All",
            ["travel.filter.income"] = "Income",
            ["travel.filter.expenses"] = "Expenses",
            ["travel.empty.title"] = "No entries yet",
            ["travel.empty.body"] = "Add your first income or expense above.",
            ["travel.save.title"] = "Save this trip",
            ["travel.save.body"] = "Keep this collection available for later review and loading.",
            ["travel.save.placeholder"] = "Trip name to save (e.g. Italy 2026)",
            ["travel.save.button"] = "Save trip",
            ["travel.save.success"] = "Saved successfully!",
            ["travel.save.locked"] = "Save & load trips",
            ["travel.save.lockedHint"] = "Premium",
            ["travel.clearAll"] = "Clear trip",
            ["account.loading"] = "Loading...",
            ["account.signedOut.title"] = "Account",
            ["account.signedOut.body"] = "Sign in to sync data across devices and unlock premium features.",
            ["account.signIn"] = "Sign in",
            ["account.create"] = "Create account",
            ["account.profile"] = "Profile",
            ["account.premiumStatus"] = "Premium status",
            ["account.premiumActive"] = "Premium active",
            ["account.validUntil"] = "Valid until {0}",
            ["account.premium.feature.currency"] = "Currency selection (6 currencies)",
            ["account.premium.feature.save"] = "Save and load cash counts",
            ["account.premium.feature.sync"] = "Sync across devices",
            ["account.upgradeTitle"] = "Upgrade to Premium",
            ["account.processing"] = "Processing...",
            ["account.upgradeFor"] = "Upgrade for {0}",
            ["account.restore"] = "Restore purchases",
            ["account.purchaseUnavailable.title"] = "In-app purchases are available on iOS and Android.",
            ["account.purchaseUnavailable.body"] = "Download the app to upgrade to Premium:",
            ["account.noPurchases"] = "No purchases found to restore.",
            ["account.signOut"] = "Sign out",
            ["account.provider.google"] = "Google",
            ["account.provider.apple"] = "Apple",
            ["account.provider.microsoft"] = "Microsoft",
            ["account.provider.email"] = "Email",
            ["ledger.hero.eyebrow"] = "Account management",
            ["ledger.hero.title"] = "Accounts & funds setup",
            ["ledger.hero.body"] = "Set up cash boxes, bank accounts, and fund buckets here. The Funds page stays focused on quick daily flow.",
            ["ledger.create"] = "+ Account / fund",
            ["ledger.reset"] = "Reset tracker",
            ["ledger.stats.accounts"] = "Tracked accounts",
            ["ledger.stats.liveBalance"] = "{0} live balance",
            ["ledger.stats.opening"] = "Opening balance",
            ["ledger.stats.openingBody"] = "Across all accounts",
            ["ledger.stats.transactions"] = "Transactions logged",
            ["ledger.stats.transactionsBody"] = "Use Funds for daily entries",
            ["ledger.accounts.title"] = "Accounts & funds",
            ["ledger.accounts.body"] = "A cleaner setup list with quick balances and less clutter.",
            ["ledger.accounts.total"] = "{0} total",
            ["ledger.account.opening"] = "Opening {0}",
            ["ledger.account.transactions"] = "{0} tx",
            ["ledger.empty.body"] = "No accounts yet. Start with your cash box, main bank account, or a fund bucket.",
            ["ledger.empty.action"] = "Create first account",
            ["ledger.guide.title"] = "How this works",
            ["ledger.guide.body"] = "Keep setup here and daily movements on the Funds page.",
            ["ledger.step1.title"] = "1. Create accounts",
            ["ledger.step1.body"] = "Add the places you track: cash, bank, savings, investment, or custom funds.",
            ["ledger.step2.title"] = "2. Log movements in Funds",
            ["ledger.step2.body"] = "Use the floating action button on the Funds page for income and expenses.",
            ["ledger.step3.title"] = "3. Reset carefully",
            ["ledger.step3.body"] = "Reset clears local accounts and transactions for this tracker.",
            ["ledger.currentCurrency"] = "Current currency",
            ["ledger.latestTransaction"] = "Latest transaction",
            ["ledger.nextAction"] = "Where to work next",
            ["ledger.nextAction.funds"] = "Funds dashboard",
            ["ledger.nextAction.create"] = "Create accounts here",
            ["ledger.openFunds"] = "Open Funds dashboard",
            ["ledger.modal.eyebrow"] = "New account",
            ["ledger.modal.title"] = "Add account or fund",
            ["ledger.modal.body"] = "Create a pot once, then track all movement against it.",
            ["ledger.field.accountName"] = "Account name",
            ["ledger.field.openingBalance"] = "Opening balance",
            ["ledger.field.note"] = "Optional note",
            ["ledger.createButton"] = "Create account",
            ["funds.eyebrow"] = "Funds",
            ["funds.accountsCount"] = "{0} account(s)",
            ["funds.net"] = "Net {0}",
            ["funds.empty.title"] = "No accounts yet",
            ["funds.empty.body"] = "Create your first cash box, bank account, or fund bucket on the Account page. This screen becomes your fast snapshot once at least one account exists.",
            ["funds.empty.action"] = "Create first account",
            ["funds.accounts.title"] = "Accounts",
            ["funds.accounts.body"] = "Live balances first.",
            ["funds.defaultCurrency"] = "{0} default",
            ["funds.transactionsShort"] = "{0} tx",
            ["funds.flow.title"] = "Flow",
            ["funds.flow.body"] = "Income, expenses, and balance at a glance.",
            ["funds.flow.incomeEntries"] = "{0} entry(s)",
            ["funds.flow.expenseEntries"] = "{0} entry(s)",
            ["funds.balance"] = "Balance",
            ["funds.activity"] = "Activity",
            ["funds.categoryOverview.title"] = "Category overview",
            ["funds.categoryOverview.body"] = "Where money comes from and where it goes.",
            ["funds.categoryOverview.tracked"] = "Tracked",
            ["funds.categoryOverview.categories"] = "categories",
            ["funds.categoryOverview.share"] = "{0} of activity",
            ["funds.categoryOverview.in"] = "In {0}",
            ["funds.categoryOverview.out"] = "Out {0}",
            ["funds.categoryOverview.empty"] = "Categories appear once transactions start using them.",
            ["funds.recent.title"] = "Recent",
            ["funds.recent.body"] = "Latest movements across all tracked accounts.",
            ["funds.recent.total"] = "{0} total",
            ["funds.recent.unknownAccount"] = "Unknown account",
            ["funds.recent.photo"] = "Photo",
            ["funds.recent.empty"] = "No transactions yet. Use the add button to log the first movement.",
            ["funds.fab.addTransaction"] = "Add transaction",
            ["funds.modal.title"] = "Add transaction",
            ["funds.modal.type"] = "Transaction type",
            ["funds.modal.outgoing"] = "Outgoing",
            ["funds.modal.incoming"] = "Incoming",
            ["funds.modal.description"] = "Description",
            ["funds.modal.amount"] = "Amount",
            ["funds.modal.category"] = "Category",
            ["funds.modal.optionalNote"] = "Optional note",
            ["funds.modal.pictureTitle"] = "Picture",
            ["funds.modal.pictureBody"] = "One optional image, resized before saving.",
            ["funds.modal.attached"] = "Attached",
            ["funds.modal.picturePlaceholder"] = "Add receipt or photo",
            ["funds.modal.chooseFile"] = "Choose from files or gallery",
            ["funds.modal.takePhoto"] = "Take a photo",
            ["funds.modal.removePicture"] = "Remove picture",
            ["funds.modal.replacePicture"] = "Replace picture",
            ["funds.modal.takeNew"] = "Take new",
            ["funds.modal.remove"] = "Remove",
            ["funds.modal.helper"] = "Add a receipt, invoice, or reference photo.",
            ["funds.modal.imageOnly"] = "Please select an image file.",
            ["funds.modal.imageUnreadable"] = "The selected image could not be read.",
            ["funds.modal.imageTooLarge"] = "That image is still too large after resizing. Try a smaller photo.",
            ["funds.modal.imagePickerClosed"] = "The image picker closed before the photo finished loading. Please try again.",
            ["funds.modal.imageAttachFailed"] = "Could not attach that image on this device/browser.",
            ["funds.modal.addIncoming"] = "+ Add incoming",
            ["funds.modal.addOutgoing"] = "+ Add outgoing",
        },
        ["de"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["app.tools"] = "Tools",
            ["app.version"] = "v1.0.12",
            ["app.language"] = "Sprache",
            ["app.languageShort"] = "Sprache",
            ["app.currency"] = "Währung",
            ["app.loading"] = "Lädt...",
            ["app.cancel"] = "Abbrechen",
            ["app.delete"] = "Löschen",
            ["app.clearAll"] = "Alles löschen",
            ["app.details"] = "Details",
            ["app.total"] = "Gesamt",
            ["app.saved"] = "gespeichert",
            ["tool.cashCounter"] = "Cash Counter",
            ["tool.savedCounts"] = "Gespeicherte Zählungen",
            ["tool.travelCost"] = "Reisekosten",
            ["tool.savedTrips"] = "Gespeicherte Reisen",
            ["tool.funds"] = "Konten",
            ["tool.account"] = "Account",
            ["cashCounter.hero.title"] = "Cash Counter",
            ["cashCounter.hero.subtitle"] = "Schnelles, touchfreundliches Bargeldzählen mit immer sichtbarer Summe.",
            ["cashCounter.premium.currencyLocked"] = "Währungswechsel",
            ["cashCounter.premium.currencyLockedHint"] = "Upgrade auf Premium, um die Währung zu ändern",
            ["cashCounter.currencyFixed"] = "Aktuelle Währung",
            ["cashCounter.totalSummary"] = "Live-Gesamt",
            ["cashCounter.banknotes"] = "Scheine",
            ["cashCounter.coins"] = "Münzen",
            ["cashCounter.items"] = "Positionen",
            ["cashCounter.sectionTotalBanknotes"] = "Scheine gesamt",
            ["cashCounter.sectionTotalCoins"] = "Münzen gesamt",
            ["cashCounter.savePlaceholder"] = "Namen zum Speichern eingeben (z. B. Montagszählung)",
            ["cashCounter.save"] = "Zählung speichern",
            ["cashCounter.savedSuccess"] = "Erfolgreich gespeichert!",
            ["cashCounter.saveLocked"] = "Zählungen speichern & laden",
            ["cashCounter.saveLockedHint"] = "Upgrade auf Premium, um Zählungen zu speichern",
            ["cashCounter.clearAll"] = "Zählung zurücksetzen",
            ["travel.hero.eyebrow"] = "Reise",
            ["travel.hero.fallbackName"] = "Unbenannte Reise",
            ["travel.hero.placeholder"] = "Reisename...",
            ["travel.hero.editHint"] = "Zum Umbenennen tippen",
            ["travel.hero.entries"] = "Einträge",
            ["travel.summary.income"] = "Einnahmen",
            ["travel.summary.expenses"] = "Ausgaben",
            ["travel.summary.balance"] = "Saldo",
            ["travel.composer.title"] = "Bewegung hinzufügen",
            ["travel.composer.subtitle"] = "Reise-Einnahmen und -Ausgaben kompakt und gut lesbar erfassen.",
            ["travel.type.expense"] = "Ausgabe",
            ["travel.type.income"] = "Einnahme",
            ["travel.field.description"] = "Beschreibung",
            ["travel.field.descriptionPlaceholder"] = "Tanken, Hotel, Spende...",
            ["travel.field.amount"] = "Betrag",
            ["travel.field.category"] = "Kategorie",
            ["travel.field.categoryPlaceholder"] = "Optionale Kategorie",
            ["travel.field.date"] = "Datum",
            ["travel.addButton"] = "+ {0} hinzufügen",
            ["travel.list.title"] = "Einträge",
            ["travel.filter.all"] = "Alle",
            ["travel.filter.income"] = "Einnahmen",
            ["travel.filter.expenses"] = "Ausgaben",
            ["travel.empty.title"] = "Noch keine Einträge",
            ["travel.empty.body"] = "Füge oben deine erste Einnahme oder Ausgabe hinzu.",
            ["travel.save.title"] = "Diese Reise speichern",
            ["travel.save.body"] = "Halte diese Sammlung für spätere Prüfungen und erneutes Laden bereit.",
            ["travel.save.placeholder"] = "Reisename zum Speichern (z. B. Italien 2026)",
            ["travel.save.button"] = "Reise speichern",
            ["travel.save.success"] = "Erfolgreich gespeichert!",
            ["travel.save.locked"] = "Reisen speichern & laden",
            ["travel.save.lockedHint"] = "Premium",
            ["travel.clearAll"] = "Reise leeren",
            ["account.loading"] = "Lädt...",
            ["account.signedOut.title"] = "Account",
            ["account.signedOut.body"] = "Melde dich an, um Daten geräteübergreifend zu synchronisieren und Premium freizuschalten.",
            ["account.signIn"] = "Anmelden",
            ["account.create"] = "Account erstellen",
            ["account.profile"] = "Profil",
            ["account.premiumStatus"] = "Premium-Status",
            ["account.premiumActive"] = "Premium aktiv",
            ["account.validUntil"] = "Gültig bis {0}",
            ["account.premium.feature.currency"] = "Währungsauswahl (6 Währungen)",
            ["account.premium.feature.save"] = "Bargeldzählungen speichern und laden",
            ["account.premium.feature.sync"] = "Geräteübergreifende Synchronisierung",
            ["account.upgradeTitle"] = "Auf Premium upgraden",
            ["account.processing"] = "Wird verarbeitet...",
            ["account.upgradeFor"] = "Upgrade für {0}",
            ["account.restore"] = "Käufe wiederherstellen",
            ["account.purchaseUnavailable.title"] = "In-App-Käufe sind auf iOS und Android verfügbar.",
            ["account.purchaseUnavailable.body"] = "Lade die App herunter, um auf Premium upzugraden:",
            ["account.noPurchases"] = "Keine Käufe zum Wiederherstellen gefunden.",
            ["account.signOut"] = "Abmelden",
            ["account.provider.google"] = "Google",
            ["account.provider.apple"] = "Apple",
            ["account.provider.microsoft"] = "Microsoft",
            ["account.provider.email"] = "E-Mail",
            ["ledger.hero.eyebrow"] = "Kontoverwaltung",
            ["ledger.hero.title"] = "Konten- & Fonds-Setup",
            ["ledger.hero.body"] = "Richte hier Kassen, Bankkonten und Töpfe ein. Die Konten-Seite bleibt für schnelle tägliche Bewegungen fokussiert.",
            ["ledger.create"] = "+ Konto / Fonds",
            ["ledger.reset"] = "Tracker zurücksetzen",
            ["ledger.stats.accounts"] = "Erfasste Konten",
            ["ledger.stats.liveBalance"] = "{0} Live-Saldo",
            ["ledger.stats.opening"] = "Startsaldo",
            ["ledger.stats.openingBody"] = "Über alle Konten hinweg",
            ["ledger.stats.transactions"] = "Erfasste Buchungen",
            ["ledger.stats.transactionsBody"] = "Für tägliche Bewegungen: Konten",
            ["ledger.accounts.title"] = "Konten & Fonds",
            ["ledger.accounts.body"] = "Übersichtliches Setup mit schnellen Salden und weniger Ballast.",
            ["ledger.accounts.total"] = "{0} gesamt",
            ["ledger.account.opening"] = "Start {0}",
            ["ledger.account.transactions"] = "{0} Buchungen",
            ["ledger.empty.body"] = "Noch keine Konten. Starte mit Kasse, Hauptkonto oder einem Fonds-Topf.",
            ["ledger.empty.action"] = "Erstes Konto anlegen",
            ["ledger.guide.title"] = "So funktioniert es",
            ["ledger.guide.body"] = "Setup hier, tägliche Bewegungen auf der Konten-Seite.",
            ["ledger.step1.title"] = "1. Konten anlegen",
            ["ledger.step1.body"] = "Lege die Orte an, die du tracken willst: Kasse, Bank, Rücklagen, Investments oder eigene Fonds.",
            ["ledger.step2.title"] = "2. Bewegungen in Konten buchen",
            ["ledger.step2.body"] = "Nutze den Floating Action Button auf der Konten-Seite für Einnahmen und Ausgaben.",
            ["ledger.step3.title"] = "3. Mit Reset vorsichtig sein",
            ["ledger.step3.body"] = "Reset löscht lokale Konten und Buchungen dieses Trackers.",
            ["ledger.currentCurrency"] = "Aktuelle Währung",
            ["ledger.latestTransaction"] = "Letzte Buchung",
            ["ledger.nextAction"] = "Nächster Schritt",
            ["ledger.nextAction.funds"] = "Konten-Dashboard",
            ["ledger.nextAction.create"] = "Hier Konten anlegen",
            ["ledger.openFunds"] = "Konten-Dashboard öffnen",
            ["ledger.modal.eyebrow"] = "Neues Konto",
            ["ledger.modal.title"] = "Konto oder Fonds hinzufügen",
            ["ledger.modal.body"] = "Lege einen Topf einmal an und verfolge dann alle Bewegungen darüber.",
            ["ledger.field.accountName"] = "Kontoname",
            ["ledger.field.openingBalance"] = "Startsaldo",
            ["ledger.field.note"] = "Optionale Notiz",
            ["ledger.createButton"] = "Konto anlegen",
            ["funds.eyebrow"] = "Konten",
            ["funds.accountsCount"] = "{0} Konto/Konten",
            ["funds.net"] = "Netto {0}",
            ["funds.empty.title"] = "Noch keine Konten",
            ["funds.empty.body"] = "Lege dein erstes Kassenfach, Bankkonto oder Fonds-Topf auf der Account-Seite an. Danach wird diese Ansicht dein schneller Überblick.",
            ["funds.empty.action"] = "Erstes Konto anlegen",
            ["funds.accounts.title"] = "Konten",
            ["funds.accounts.body"] = "Zuerst die Live-Salden.",
            ["funds.defaultCurrency"] = "{0} Standard",
            ["funds.transactionsShort"] = "{0} Buchungen",
            ["funds.flow.title"] = "Flow",
            ["funds.flow.body"] = "Einnahmen, Ausgaben und Saldo auf einen Blick.",
            ["funds.flow.incomeEntries"] = "{0} Einträge",
            ["funds.flow.expenseEntries"] = "{0} Einträge",
            ["funds.balance"] = "Saldo",
            ["funds.activity"] = "Aktivität",
            ["funds.categoryOverview.title"] = "Kategorieüberblick",
            ["funds.categoryOverview.body"] = "Wo Geld herkommt und wohin es geht.",
            ["funds.categoryOverview.tracked"] = "Erfasst",
            ["funds.categoryOverview.categories"] = "Kategorien",
            ["funds.categoryOverview.share"] = "{0} der Aktivität",
            ["funds.categoryOverview.in"] = "Rein {0}",
            ["funds.categoryOverview.out"] = "Raus {0}",
            ["funds.categoryOverview.empty"] = "Kategorien erscheinen, sobald Buchungen sie verwenden.",
            ["funds.recent.title"] = "Zuletzt",
            ["funds.recent.body"] = "Neueste Bewegungen über alle erfassten Konten.",
            ["funds.recent.total"] = "{0} gesamt",
            ["funds.recent.unknownAccount"] = "Unbekanntes Konto",
            ["funds.recent.photo"] = "Foto",
            ["funds.recent.empty"] = "Noch keine Buchungen. Nutze den Plus-Button für die erste Bewegung.",
            ["funds.fab.addTransaction"] = "Buchung hinzufügen",
            ["funds.modal.title"] = "Buchung hinzufügen",
            ["funds.modal.type"] = "Buchungstyp",
            ["funds.modal.outgoing"] = "Ausgang",
            ["funds.modal.incoming"] = "Eingang",
            ["funds.modal.description"] = "Beschreibung",
            ["funds.modal.amount"] = "Betrag",
            ["funds.modal.category"] = "Kategorie",
            ["funds.modal.optionalNote"] = "Optionale Notiz",
            ["funds.modal.pictureTitle"] = "Bild",
            ["funds.modal.pictureBody"] = "Ein optionales Bild, vor dem Speichern verkleinert.",
            ["funds.modal.attached"] = "Angehängt",
            ["funds.modal.picturePlaceholder"] = "Beleg oder Foto hinzufügen",
            ["funds.modal.chooseFile"] = "Aus Dateien oder Galerie wählen",
            ["funds.modal.takePhoto"] = "Foto aufnehmen",
            ["funds.modal.removePicture"] = "Bild entfernen",
            ["funds.modal.replacePicture"] = "Bild ersetzen",
            ["funds.modal.takeNew"] = "Neu aufnehmen",
            ["funds.modal.remove"] = "Entfernen",
            ["funds.modal.helper"] = "Füge einen Beleg, eine Rechnung oder ein Referenzfoto hinzu.",
            ["funds.modal.imageOnly"] = "Bitte wähle eine Bilddatei aus.",
            ["funds.modal.imageUnreadable"] = "Das ausgewählte Bild konnte nicht gelesen werden.",
            ["funds.modal.imageTooLarge"] = "Das Bild ist selbst nach dem Verkleinern noch zu groß. Bitte nutze ein kleineres Foto.",
            ["funds.modal.imagePickerClosed"] = "Der Bilddialog wurde geschlossen, bevor das Foto fertig geladen war. Bitte versuche es erneut.",
            ["funds.modal.imageAttachFailed"] = "Das Bild konnte auf diesem Gerät/Browser nicht angehängt werden.",
            ["funds.modal.addIncoming"] = "+ Eingang hinzufügen",
            ["funds.modal.addOutgoing"] = "+ Ausgang hinzufügen",
        }
    };

    public AppTextService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        CurrentLanguage = _languages[0];
        CurrentCulture = new CultureInfo(CurrentLanguage.Culture);
        ApplyCulture(CurrentCulture);
    }

    public event Action? Changed;
    public IReadOnlyList<AppLanguage> Languages => _languages;
    public AppLanguage CurrentLanguage { get; private set; }
    public CultureInfo CurrentCulture { get; private set; }
    public bool IsInitialized { get; private set; }

    public string this[string key] => Get(key);

    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        var preferred = await ReadStoredLanguageAsync() ?? await DetectBrowserLanguageAsync() ?? CurrentLanguage.Code;
        await SetLanguageInternalAsync(preferred, persist: false, notify: false);
        IsInitialized = true;
        Changed?.Invoke();
    }

    public string Get(string key)
    {
        if (_texts.TryGetValue(CurrentLanguage.Code, out var selected) && selected.TryGetValue(key, out var translated))
            return translated;

        if (_texts.TryGetValue("en", out var fallback) && fallback.TryGetValue(key, out var fallbackText))
            return fallbackText;

        return key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(CurrentCulture, Get(key), args);
    }

    public Task SetLanguageAsync(string code)
    {
        return SetLanguageInternalAsync(code, persist: true, notify: true);
    }

    private async Task SetLanguageInternalAsync(string code, bool persist, bool notify)
    {
        var normalized = Normalize(code);
        var language = _languages.FirstOrDefault(x => x.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase)) ?? _languages[0];

        CurrentLanguage = language;
        CurrentCulture = new CultureInfo(language.Culture);
        ApplyCulture(CurrentCulture);

        if (persist)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LanguageStorageKey, language.Code);
            }
            catch
            {
                // ignored on platforms without localStorage access at this moment
            }
        }

        if (notify)
            Changed?.Invoke();
    }

    private async Task<string?> ReadStoredLanguageAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", LanguageStorageKey);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> DetectBrowserLanguageAsync()
    {
        try
        {
            var raw = await _jsRuntime.InvokeAsync<string>("eval", "navigator.language || navigator.userLanguage || 'en'");
            return Normalize(raw);
        }
        catch
        {
            return null;
        }
    }

    private static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "en";

        return code.Split('-', '_')[0].Trim().ToLowerInvariant();
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
