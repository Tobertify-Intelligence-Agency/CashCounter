# CashCounter

Cross-platform cash counting app built with .NET MAUI Blazor Hybrid and Blazor WebAssembly.

## Structure

- `CashCount.Maui/` — mobile/desktop app
- `CashCount.Web/` — web app
- `CashCount.Shared/` — shared UI, models, and services
- `CashCount.sln` — solution file

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

## Notes

- Keep IDE-specific files out of git.
- Keep local Claude settings machine-local.
- Use the repo for product code; keep TIA core docs in the separate Core repo.
