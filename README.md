# CashCounter

Cross-platform cash counting app built with .NET MAUI Blazor Hybrid and Blazor WebAssembly.

## Structure

- `CashCount.Maui/` — mobile/desktop app
- `CashCount.Web/` — web app
- `CashCount.Shared/` — shared UI, models, and services
- `CashCount.sln` — solution file

## Prerequisites

- .NET 10 SDK
- MAUI workloads for mobile/desktop builds

## Run

### Web
```bash
dotnet run --project CashCount.Web
```

### Windows
```bash
dotnet run --project CashCount.Maui -f net10.0-windows10.0.19041.0
```

### Build solution
```bash
dotnet build CashCount.sln
```

### Android release signing

Android release builds read signing settings from environment variables instead of the project file:

```bash
export CASHCOUNT_ANDROID_KEYSTORE=/absolute/path/to/cashcount.keystore
export CASHCOUNT_ANDROID_KEY_ALIAS=your-key-alias
export CASHCOUNT_ANDROID_KEY_PASS=your-key-password
export CASHCOUNT_ANDROID_STORE_PASS=your-store-password
```

Release Android builds fail fast if any of these variables are missing.

## Notes

- Keep IDE-specific files out of git.
- Keep local Claude settings machine-local.
- Use the repo for product code; keep TIA core docs in the separate Core repo.
