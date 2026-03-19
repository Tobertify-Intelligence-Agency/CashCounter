# Project Context - CashCount

> **WICHTIG FÜR CLAUDE:** Diese Datei enthält den Projektkontext für zukünftige Sessions.

## Overview
Cross-platform Cash Counter application that runs on Android, iOS, Windows, macOS, and Web browsers. Similar to https://kapitalmarkt.org/tools/online-geldzaehler-bargeldrechner/ but more user-friendly.

## Project Location
`C:\GitHub\ProjectClaude\CashCount\`

## Tech Stack
- .NET 10.0
- .NET MAUI Blazor Hybrid (Android, iOS, Windows, macOS)
- Blazor WebAssembly (Browser)
- Shared Razor Class Library

---

## Project Structure

```
CashCount/
├── CashCount.sln                         # Solution file (3 projects)
│
├── CashCount.Shared/                     # Shared Razor Class Library
│   ├── CashCount.Shared.csproj
│   ├── _Imports.razor
│   ├── Models/
│   │   └── Currency.cs                   # Currency & Denomination models
│   ├── Components/
│   │   ├── AppShell.razor                # App shell with left navigation menu
│   │   └── CashCounter.razor             # Cash counter tool component
│   └── wwwroot/
│       └── cashcounter.css               # Shared styles (includes navigation)
│
├── CashCount.Maui/                       # .NET MAUI Blazor Hybrid App
│   ├── CashCount.Maui.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── MainPage.xaml / MainPage.xaml.cs
│   ├── MauiProgram.cs
│   ├── Components/
│   │   ├── _Imports.razor
│   │   ├── Routes.razor
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   ├── MainLayout.razor.css
│   │   │   ├── NavMenu.razor
│   │   │   └── NavMenu.razor.css
│   │   └── Pages/
│   │       ├── Home.razor                # Uses <CashCounter /> from shared
│   │       ├── Counter.razor
│   │       ├── Weather.razor
│   │       └── NotFound.razor
│   ├── Platforms/                        # Platform-specific code
│   │   ├── Android/
│   │   ├── iOS/
│   │   ├── MacCatalyst/
│   │   └── Windows/
│   ├── Resources/
│   │   ├── AppIcon/
│   │   ├── Fonts/
│   │   ├── Images/
│   │   ├── Raw/
│   │   └── Splash/
│   └── wwwroot/
│       ├── index.html
│       └── app.css
│
└── CashCount.Web/                        # Blazor WebAssembly App
    ├── CashCount.Web.csproj
    ├── Program.cs
    ├── App.razor
    ├── _Imports.razor
    ├── Layout/
    │   ├── MainLayout.razor
    │   ├── MainLayout.razor.css
    │   ├── NavMenu.razor
    │   └── NavMenu.razor.css
    ├── Pages/
    │   ├── Home.razor                    # Uses <CashCounter /> from shared
    │   ├── Counter.razor
    │   ├── Weather.razor
    │   └── NotFound.razor
    └── wwwroot/
        ├── index.html
        ├── css/app.css
        └── lib/bootstrap/
```

---

## Supported Currencies

The app supports 6 currencies with all their denominations:

| Currency | Code | Symbol | Banknotes | Coins |
|----------|------|--------|-----------|-------|
| Euro | EUR | € | 5, 10, 20, 50, 100, 200, 500 | 1c, 2c, 5c, 10c, 20c, 50c, 1€, 2€ |
| US Dollar | USD | $ | 1, 2, 5, 10, 20, 50, 100 | 1c, 5c, 10c, 25c, 50c, $1 |
| British Pound | GBP | £ | 5, 10, 20, 50, 100 | 1p, 2p, 5p, 10p, 20p, 50p, £1, £2 |
| Swiss Franc | CHF | CHF | 10, 20, 50, 100, 200, 1000 | 5Rp, 10Rp, 20Rp, 50Rp, 1, 2, 5 |
| Canadian Dollar | CAD | C$ | 5, 10, 20, 50, 100 | 5c, 10c, 25c, $1, $2 |
| Australian Dollar | AUD | A$ | 5, 10, 20, 50, 100 | 5c, 10c, 20c, 50c, $1, $2 |

---

## Features

- **Left Navigation Menu**: Collapsible sidebar for tool selection (hamburger menu on mobile)
- **Currency Selection**: Dropdown to switch between 6 currencies
- **Denomination Cards**: Each denomination has +/- buttons and direct input
- **Live Calculation**: Totals update instantly as quantities change
- **Section Subtotals**: Separate totals for banknotes and coins
- **Sticky Total Display**: Grand total stays visible while scrolling
- **Clear All**: Reset all quantities to zero
- **Responsive Design**: Works on mobile and desktop
- **Modern UI**: Clean, user-friendly interface with color-coded sections

---

## Key Files

### Models (CashCount.Shared/Models/Currency.cs)
```csharp
public class Currency
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public List<Denomination> Banknotes { get; set; }
    public List<Denomination> Coins { get; set; }

    public static List<Currency> GetAllCurrencies() => new() { ... };
}

public class Denomination
{
    public decimal Value { get; set; }
    public string DisplayName { get; set; }
    public int Quantity { get; set; }
    public decimal Total => Value * Quantity;
}
```

### Main Component (CashCount.Shared/Components/CashCounter.razor)
- Currency selector dropdown
- Banknotes section with quantity controls
- Coins section with quantity controls
- Total display (sticky)
- Clear all button

---

## Run Commands

### Web (Browser)
```bash
cd C:\GitHub\ProjectClaude\CashCount
dotnet run --project CashCount.Web
# Opens at http://localhost:5000 or similar
```

### Windows
```bash
dotnet run --project CashCount.Maui -f net10.0-windows10.0.19041.0
```

### macOS
```bash
dotnet run --project CashCount.Maui -f net10.0-maccatalyst
```

### Android (Build)
```bash
dotnet build CashCount.Maui -f net10.0-android
# Deploy APK via adb or Visual Studio
```

### iOS (Requires Mac)
```bash
dotnet build CashCount.Maui -f net10.0-ios
# Deploy via Xcode or Visual Studio for Mac
```

### Build All
```bash
cd C:\GitHub\ProjectClaude\CashCount
dotnet build CashCount.sln
```

---

## Design Decisions

1. **Shared Library**: All UI components and models in CashCount.Shared for code reuse
2. **Blazor Hybrid**: MAUI uses WebView for consistent UI across platforms
3. **No Navigation**: Single-page app, removed default sidebar/nav menu
4. **Sticky Total**: Always visible grand total while scrolling through denominations
5. **Color Coding**: Green for banknotes section, yellow/amber for coins
6. **Modern Styling**: CSS variables, rounded corners, shadows, responsive grid

---

## Styling (CashCount.Shared/wwwroot/cashcounter.css)

CSS Variables:
- `--primary-color: #2563eb` (Blue)
- `--success-color: #10b981` (Green - banknotes)
- `--warning-color: #f59e0b` (Amber - coins)
- `--danger-color: #ef4444` (Red - minus button)
- `--background: #f8fafc`
- `--card-bg: #ffffff`

---

## Potential Future Improvements

- [ ] Add more currencies (JPY, INR, etc.)
- [ ] Save/load counting sessions
- [ ] Export totals to PDF or share
- [ ] Print functionality
- [ ] Dark mode support
- [ ] History of counts
- [ ] Multiple cash register support
- [ ] Localization (German, French, etc.)

---

## Session History

### Session 1 (2026-01-09)
**Initial Creation**
- Created CashCount folder
- Created .NET MAUI Blazor Hybrid project
- Added Currency model with 6 currencies
- Created CashCounter.razor component
- Added modern CSS styling
- Successfully built for Android, iOS, Windows, macOS

**Added Web Support**
- Restructured to shared library architecture
- Created CashCount.Shared (Razor Class Library)
- Created CashCount.Web (Blazor WebAssembly)
- Moved CashCount.Maui to subfolder
- All 3 projects build successfully

**Added Left Navigation Menu**
- Created AppShell.razor component with collapsible sidebar
- Dark gradient sidebar (slate colors)
- Hamburger menu toggle on mobile
- Fixed sidebar on desktop (>768px)
- Tool list ready for future tools to be added
- Updated MainLayout in both MAUI and Web to use AppShell
- Added navigation CSS styles to cashcounter.css

### Session 2 (2026-01-10)
**Fixed Load Function**
- Issue: SavedCounts navigated to `/?load={id}` but CashCounter didn't read the query parameter
- Added `GetByIdAsync(string id)` method to IStorageService interface
- Implemented `GetByIdAsync()` in LocalStorageService
- Updated CashCounter.razor to read `load` query parameter and fetch saved count
- Added manual query string parsing (no additional dependencies)

**Added Premium Feature System**
- Created IPremiumService interface with:
  - `IsPremiumAsync()` - Check premium status
  - `IsFeatureEnabledAsync(PremiumFeature)` - Check specific feature
  - `SetPremiumStatusAsync(bool)` - Set premium status
- Created PremiumFeature enum: `CurrencySelection`, `SaveCounts`, `LoadCounts`
- Created PremiumService implementation using localStorage
- Updated CashCounter.razor:
  - Currency selector only shown for premium users
  - Non-premium users see currency display with "Premium" badge
  - Save section only shown for premium users
  - Non-premium users see locked feature message
- Updated SavedCounts.razor:
  - Shows "Premium Required" message for non-premium users
  - Only loads saved counts for premium users
- Added CSS styles for premium badges and locked features
- Registered PremiumService in both Web and MAUI projects

---

## Premium Features

### How Premium Works
Premium status is stored in localStorage (`cashcount_premium_status`). Features can be individually checked:

```csharp
// Check if user has premium
bool isPremium = await PremiumService.IsPremiumAsync();

// Check specific feature
bool canSave = await PremiumService.IsFeatureEnabledAsync(PremiumFeature.SaveCounts);

// Set premium status (after purchase verification)
await PremiumService.SetPremiumStatusAsync(true);
```

### Premium Features
| Feature | Enum Value | Description |
|---------|------------|-------------|
| Currency Selection | `CurrencySelection` | Switch between 6 currencies |
| Save Counts | `SaveCounts` | Save cash counts for later |
| Load Counts | `LoadCounts` | Load previously saved counts |

### Files Created/Modified
- `Services/IPremiumService.cs` - Interface
- `Services/PremiumService.cs` - Implementation
- `Components/CashCounter.razor` - Premium checks
- `Components/SavedCounts.razor` - Premium checks
- `wwwroot/cashcounter.css` - Premium styles
- `Program.cs` (Web) - Service registration
- `MauiProgram.cs` (MAUI) - Service registration

---

## Quick Reference

### Solution Structure
```
CashCount.sln
├── CashCount.Shared.csproj  (Shared components)
├── CashCount.Maui.csproj    (Mobile/Desktop app)
└── CashCount.Web.csproj     (Web app)
```

### Project References
- CashCount.Maui → CashCount.Shared
- CashCount.Web → CashCount.Shared

### CSS Location
- Shared: `CashCount.Shared/wwwroot/cashcounter.css`
- Linked in HTML: `_content/CashCount.Shared/cashcounter.css`

---

## How to Add New Tools

To add a new tool to the navigation menu:

1. **Create the tool component** in `CashCount.Shared/Components/`:
   ```razor
   @* NewTool.razor *@
   <div class="new-tool">
       ... tool UI here ...
   </div>
   ```

2. **Add a page** in both projects:
   - `CashCount.Maui/Components/Pages/NewTool.razor`
   - `CashCount.Web/Pages/NewTool.razor`
   ```razor
   @page "/new-tool"
   <CashCount.Shared.Components.NewTool />
   ```

3. **Update the Tools list** in `AppShell.razor`:
   ```csharp
   private List<ToolItem> Tools { get; set; } = new()
   {
       new ToolItem { Id = "cash-counter", Name = "Cash Counter", Icon = "💰", Href = "/" },
       new ToolItem { Id = "new-tool", Name = "New Tool", Icon = "🔧", Href = "/new-tool" },
   };
   ```

4. **Add styles** (if needed) to `cashcounter.css`

### Current Tools
| Tool ID | Name | Icon | Route |
|---------|------|------|-------|
| cash-counter | Cash Counter | 💰 | / |

### Planned Tools (Examples)
- Calculator (🧮)
- Currency Converter (💱)
- Tip Calculator (💵)
- Loan Calculator (🏦)

---

## Session 3 (2026-01-10) - User Management & In-App Purchases

### Overview
Implemented complete user authentication system with Firebase and in-app purchase infrastructure.

### Firebase Project Configuration
- **Project Name:** cashcounter-1f178
- **API Key:** AIzaSyDSHEpC5yxstLLAUxwSoC2Z-qdTqRdkCHo
- **Auth Domain:** cashcounter-1f178.firebaseapp.com
- **Project ID:** cashcounter-1f178
- **Configured in:** `CashCount.Web/wwwroot/index.html`

### Architecture Created

```
CashCount.Shared/
├── Models/
│   ├── UserProfile.cs              # User data model
│   ├── AuthResult.cs               # Auth operation result
│   └── SavedCount.cs               # (existing)
├── Services/
│   ├── Auth/
│   │   ├── IAuthService.cs         # Auth interface
│   │   ├── IUserSyncService.cs     # Firestore sync interface
│   │   └── CashCountAuthStateProvider.cs  # Blazor auth state
│   ├── Billing/
│   │   └── IBillingService.cs      # IAP interface (incl. ProductInfo, PurchaseResult, PurchaseState)
│   ├── IPremiumService.cs          # (updated)
│   └── PremiumService.cs           # (updated - Firebase sync)
└── Components/
    └── Auth/
        ├── Login.razor             # Login page
        ├── Register.razor          # Registration page
        └── AccountSettings.razor   # Account + Premium upgrade UI

CashCount.Maui/
└── Services/
    ├── Auth/
    │   ├── MauiAuthService.cs      # Stub (Firebase not configured)
    │   └── MauiUserSyncService.cs  # Stub (Firestore not configured)
    └── Billing/
        └── MauiBillingService.cs   # Stub (IAP not configured)

CashCount.Web/
├── Services/
│   ├── Auth/
│   │   ├── WebAuthService.cs       # Firebase JS interop
│   │   └── WebUserSyncService.cs   # Firestore JS interop
│   └── Billing/
│       └── WebBillingService.cs    # Returns "not available on web"
└── wwwroot/
    └── js/
        └── firebase-auth.js        # Firebase JS wrapper functions
```

### New Pages Created

**Web (`CashCount.Web/Pages/`):**
- `Login.razor` - Route: `/login`
- `Register.razor` - Route: `/register`
- `Account.razor` - Route: `/account`
- `ForgotPassword.razor` - Route: `/forgot-password`

**MAUI (`CashCount.Maui/Components/Pages/`):**
- `Login.razor` - Route: `/login`
- `Register.razor` - Route: `/register`
- `Account.razor` - Route: `/account`
- `ForgotPassword.razor` - Route: `/forgot-password`

### Service Registration

**MauiProgram.cs:**
```csharp
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CashCountAuthStateProvider>();
builder.Services.AddScoped<IAuthService, MauiAuthService>();
builder.Services.AddScoped<IUserSyncService, MauiUserSyncService>();
builder.Services.AddScoped<IBillingService, MauiBillingService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IPremiumService, PremiumService>();
```

**Program.cs (Web):**
```csharp
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CashCountAuthStateProvider>();
builder.Services.AddScoped<IAuthService, WebAuthService>();
builder.Services.AddScoped<IUserSyncService, WebUserSyncService>();
builder.Services.AddScoped<IBillingService, WebBillingService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IPremiumService, PremiumService>();
```

### NuGet Packages Added

**CashCount.Shared.csproj:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.0" />
```

**CashCount.Maui.csproj:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.0" />
<PackageReference Include="Plugin.Firebase.Auth" Version="3.0.0" />
<PackageReference Include="Plugin.Firebase.Firestore" Version="3.0.0" />
<PackageReference Include="Plugin.InAppBilling" Version="8.0.4" />
```

**CashCount.Web.csproj:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.0" />
```

### Navigation Updated

**AppShell.razor** - Added Account to Tools list:
```csharp
new ToolItem { Id = "account", Name = "Account", Icon = "👤", Href = "/account" }
```

### Auth Features Implemented

| Feature | Web | MAUI (Win/Mac) | MAUI (Android/iOS) |
|---------|-----|----------------|-------------------|
| Email/Password Sign In | ✅ Firebase JS | Stub | Needs Firebase config |
| Email/Password Sign Up | ✅ Firebase JS | Stub | Needs Firebase config |
| Google OAuth | ✅ Firebase JS | Stub | Needs Firebase config |
| Apple OAuth | ✅ Firebase JS | Stub | Needs Firebase config |
| Microsoft OAuth | ✅ Firebase JS | Stub | Needs Firebase config |
| Password Reset | ✅ Firebase JS | Stub | Needs Firebase config |
| Firestore User Sync | ✅ Firebase JS | Stub | Needs Firebase config |
| Premium Status Sync | ✅ Firebase JS | Stub | Needs Firebase config |

### Build Status

| Platform | Status | Command |
|----------|--------|---------|
| Web | ✅ Working | `dotnet build CashCount.Web` |
| Windows | ✅ Working | `dotnet build CashCount.Maui -f net10.0-windows10.0.19041.0` |
| macOS | ✅ Working | `dotnet build CashCount.Maui -f net10.0-maccatalyst` |
| Android | ⚠️ SDK cache issue | Clear obj folder, rebuild |
| iOS | ⚠️ Requires macOS | Build on Mac only |

### CSS Styles Added

Added to `cashcounter.css`:
- Auth container and card styles (`.auth-container`, `.auth-card`)
- Form group styles (`.form-group`, `.auth-form`)
- Social login buttons (`.btn-social`, `.google`, `.apple`, `.microsoft`)
- Account page styles (`.account-container`, `.account-card`, `.profile-photo`)
- Premium section styles (`.premium-section`, `.premium-active`, `.premium-upgrade`)
- Purchase options styles (`.purchase-options`, `.btn-upgrade`, `.btn-restore`)

### PremiumService Updates

Updated `PremiumService.cs` to:
1. Check Firebase Firestore first when user is logged in
2. Fall back to localStorage when offline or not logged in
3. Sync premium status to Firestore on change
4. Handle premium expiry dates
5. Listen for auth state changes to reset cache

### Pending Tasks for Future Sessions

1. **Firebase Console Setup:**
   - Enable Email/Password auth provider
   - Enable Google auth provider
   - Enable Apple auth provider (requires Apple Developer account)
   - Enable Microsoft auth provider
   - Set up Firestore security rules

2. **MAUI Firebase Integration:**
   - Add `google-services.json` for Android
   - Add `GoogleService-Info.plist` for iOS
   - Implement full `MauiAuthService` with Plugin.Firebase.Auth
   - Implement full `MauiUserSyncService` with Plugin.Firebase.Firestore
   - Implement full `MauiBillingService` with Plugin.InAppBilling

3. **App Store Setup:**
   - Create in-app product `com.cashcount.premium` in Google Play Console
   - Create in-app purchase in App Store Connect
   - Test purchase flows

4. **Testing:**
   - Test Web login flow with Firebase
   - Test premium status sync across devices
   - Test purchase restoration

### Firestore Security Rules (To Apply)

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId} {
      allow read, write: if request.auth != null && request.auth.uid == userId;

      match /savedCounts/{countId} {
        allow read, write: if request.auth != null && request.auth.uid == userId;
      }
    }
  }
}
```

### Key Interfaces

**IAuthService:**
- `SignInWithEmailAsync(email, password)`
- `SignUpWithEmailAsync(email, password, displayName)`
- `SignInWithGoogleAsync()`
- `SignInWithAppleAsync()`
- `SignInWithMicrosoftAsync()`
- `SignOutAsync()`
- `GetCurrentUserAsync()`
- `IsSignedInAsync()`
- `SendPasswordResetEmailAsync(email)`
- `UpdateDisplayNameAsync(displayName)`
- `event AuthStateChanged`

**IUserSyncService:**
- `GetUserProfileAsync(userId)`
- `SaveUserProfileAsync(profile)`
- `UpdatePremiumStatusAsync(userId, isPremium, expiryDate)`
- `SyncSavedCountsAsync(userId, counts)`
- `GetSyncedCountsAsync(userId)`
- `DeleteUserDataAsync(userId)`

**IBillingService:**
- `IsAvailableAsync()`
- `GetProductsAsync()`
- `PurchaseAsync(productId)`
- `RestorePurchasesAsync()`
- `event PurchaseCompleted`

### Quick Test Commands

```bash
# Build and run Web (full Firebase support)
cd C:\GitHub\ProjectClaude\CashCount
dotnet run --project CashCount.Web

# Build Windows (stub services)
dotnet build CashCount.Maui -f net10.0-windows10.0.19041.0

# Run Windows app
dotnet run --project CashCount.Maui -f net10.0-windows10.0.19041.0
```

### Files Modified in This Session

| File | Changes |
|------|---------|
| `CashCount.Shared/CashCount.Shared.csproj` | Added Authorization package |
| `CashCount.Maui/CashCount.Maui.csproj` | Added Firebase & IAP packages |
| `CashCount.Maui/MauiProgram.cs` | Added service registrations |
| `CashCount.Web/Program.cs` | Added service registrations |
| `CashCount.Web/wwwroot/index.html` | Added Firebase SDK & config |
| `CashCount.Shared/Components/AppShell.razor` | Added Account nav item |
| `CashCount.Shared/Services/PremiumService.cs` | Firebase sync integration |
| `CashCount.Shared/wwwroot/cashcounter.css` | Added auth & account styles |
