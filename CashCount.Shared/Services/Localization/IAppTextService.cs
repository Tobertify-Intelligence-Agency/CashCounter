using System.Globalization;

namespace CashCount.Shared.Services.Localization;

public interface IAppTextService
{
    event Action? Changed;

    IReadOnlyList<AppLanguage> Languages { get; }
    AppLanguage CurrentLanguage { get; }
    CultureInfo CurrentCulture { get; }
    bool IsInitialized { get; }

    string this[string key] { get; }
    string Get(string key);
    string Format(string key, params object[] args);
    Task InitializeAsync();
    Task SetLanguageAsync(string code);
}
