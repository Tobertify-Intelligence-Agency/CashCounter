---
name: dotnet-design-system
description: Ensures every UI built looks modern, professional, and follows enterprise design standards. Use when building any Blazor, Razor Pages, or WinForms UI components.
user-invocable: false
---

# .NET Design System

You are a UI/UX expert ensuring all .NET user interfaces follow enterprise design standards. Apply these principles to every UI component, page, or form you create.

## Core Design Principles

### 1. Clean and Minimal Interface
- Ample whitespace between elements
- Clear visual hierarchy
- No visual clutter
- One primary action per screen/section
- Progressive disclosure for complex features

### 2. Color Palette

**Primary Colors (Neutral Blues/Grays):**
| Name | Hex | Usage |
|------|-----|-------|
| Polar Night 1 | `#2E3440` | Backgrounds, dark mode |
| Polar Night 2 | `#3B4252` | Primary text, headers |
| Polar Night 3 | `#434C5E` | Secondary elements |
| Polar Night 4 | `#4C566A` | Borders, dividers |

**Accent Color:**
| Name | Hex | Usage |
|------|-----|-------|
| Frost Blue | `#5E81AC` | Interactive elements, links, primary buttons |

**Semantic Colors:**
| Purpose | Hex | Usage |
|---------|-----|-------|
| Success | `#A3BE8C` | Success messages, confirmations |
| Warning | `#EBCB8B` | Warnings, caution states |
| Error | `#BF616A` | Errors, destructive actions |
| Info | `#81A1C1` | Informational messages |

**Background Colors:**
| Name | Hex | Usage |
|------|-----|-------|
| Snow Storm 1 | `#ECEFF4` | Page backgrounds |
| Snow Storm 2 | `#E5E9F0` | Card backgrounds |
| Snow Storm 3 | `#D8DEE9` | Hover states, borders |

### 3. Spacing System (8px Grid)

Always use these spacing values:
```
--spacing-xs: 8px;
--spacing-sm: 16px;
--spacing-md: 24px;
--spacing-lg: 32px;
--spacing-xl: 48px;
--spacing-2xl: 64px;
```

**Usage Guidelines:**
- `8px` - Tight spacing within components (icon to text)
- `16px` - Standard padding inside components
- `24px` - Space between related elements
- `32px` - Space between sections
- `48px` - Major section separators
- `64px` - Page-level margins

### 4. Typography Hierarchy

**Font Families (max 2):**
```css
--font-primary: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
--font-mono: 'Cascadia Code', 'Fira Code', Consolas, monospace;
```

**Font Sizes:**
| Element | Size | Weight | Line Height |
|---------|------|--------|-------------|
| H1 | 32px | 600 | 1.2 |
| H2 | 24px | 600 | 1.3 |
| H3 | 20px | 600 | 1.4 |
| H4 | 18px | 500 | 1.4 |
| Body | 16px | 400 | 1.5 |
| Small | 14px | 400 | 1.5 |
| Caption | 12px | 400 | 1.4 |

**Rules:**
- Minimum body text: **14px** on desktop
- Never use font sizes below 12px
- Maximum 2 font families per application

### 5. Interactive States

Every interactive element MUST have these states:

```css
/* Default */
.button {
  background: #5E81AC;
  color: white;
  cursor: pointer;
}

/* Hover */
.button:hover {
  background: #81A1C1;
  transform: translateY(-1px);
}

/* Active/Pressed */
.button:active {
  background: #4C566A;
  transform: translateY(0);
}

/* Focused (keyboard navigation) */
.button:focus-visible {
  outline: 2px solid #5E81AC;
  outline-offset: 2px;
}

/* Disabled */
.button:disabled {
  background: #D8DEE9;
  color: #4C566A;
  cursor: not-allowed;
  opacity: 0.7;
}

/* Loading */
.button.loading {
  pointer-events: none;
  position: relative;
}
.button.loading::after {
  content: '';
  /* spinner animation */
}
```

### 6. Component Patterns

**Buttons:**
```css
/* Primary - main actions */
.btn-primary { background: #5E81AC; color: white; }

/* Secondary - alternative actions */
.btn-secondary { background: transparent; border: 1px solid #5E81AC; color: #5E81AC; }

/* Danger - destructive actions */
.btn-danger { background: #BF616A; color: white; }

/* Ghost - subtle actions */
.btn-ghost { background: transparent; color: #5E81AC; }
```

**Form Fields:**
- Labels above inputs (not inline)
- Placeholder text in lighter color (#4C566A)
- Clear focus states with accent color border
- Error states with red border and message below
- 8px border-radius for modern look

**Cards:**
- Subtle shadow: `0 2px 4px rgba(0,0,0,0.1)`
- 16px padding minimum
- 8px border-radius
- Clear visual separation from background

**Modals:**
- Centered with backdrop overlay
- Max-width: 500px for forms, 800px for content
- Clear close button (X) in top-right
- Primary action on right, cancel on left

### 7. Accessibility (WCAG 2.1 AA)

**Required:**
- Color contrast ratio: 4.5:1 minimum for text
- Focus indicators visible on all interactive elements
- Semantic HTML (`<button>`, `<nav>`, `<main>`, `<aside>`)
- ARIA labels for icons and non-text content
- Skip-to-content links
- Keyboard navigable (Tab, Enter, Escape)
- Screen reader announcements for dynamic content

**Testing:**
- Test with keyboard only (no mouse)
- Test with screen reader (NVDA, VoiceOver)
- Verify in Windows High Contrast mode

### 8. Responsive Breakpoints

**Mobile-first approach. Test at these widths:**
| Breakpoint | Width | Target |
|------------|-------|--------|
| Mobile | 375px | Phones |
| Tablet | 768px | Tablets, small laptops |
| Desktop | 1440px | Standard desktop |

```css
/* Mobile first */
.container { padding: 16px; }

/* Tablet */
@media (min-width: 768px) {
  .container { padding: 24px; }
}

/* Desktop */
@media (min-width: 1440px) {
  .container { padding: 32px; max-width: 1200px; }
}
```

### 9. Error Handling

**Do:**
- Display errors in designated areas (top of form, or summary)
- Use clear, actionable language: "Email is required" not "Error 422"
- Red color (#BF616A) with icon for visibility
- Persist until user corrects the issue

**Don't:**
- Inline errors that shift layout
- Technical jargon in error messages
- Multiple error popups

### 10. Loading States

**Options (choose consistently):**
1. **Skeleton screens** - Preferred for content areas
2. **Spinners** - For buttons and small components
3. **Progress bars** - For known-duration operations

```css
/* Skeleton loading animation */
.skeleton {
  background: linear-gradient(90deg, #E5E9F0 25%, #ECEFF4 50%, #E5E9F0 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
```

---

## Framework-Specific Guidelines

### Blazor Applications

**CSS Custom Properties (required):**
```css
:root {
  /* Colors */
  --color-primary: #5E81AC;
  --color-text: #3B4252;
  --color-background: #ECEFF4;
  --color-error: #BF616A;
  --color-success: #A3BE8C;

  /* Spacing */
  --spacing-sm: 16px;
  --spacing-md: 24px;
  --spacing-lg: 32px;

  /* Typography */
  --font-family: 'Segoe UI', sans-serif;
  --font-size-base: 16px;

  /* Borders */
  --border-radius: 8px;
  --border-color: #D8DEE9;
}
```

**EditContext Styling:**
```css
.valid.modified:not([type=checkbox]) {
  border-color: var(--color-success);
}

.invalid {
  border-color: var(--color-error);
}

.validation-message {
  color: var(--color-error);
  font-size: 14px;
  margin-top: 4px;
}
```

**Component Library Structure:**
```
/Components
  /Shared
    Button.razor
    Card.razor
    Modal.razor
    FormField.razor
    LoadingSpinner.razor
    Alert.razor
```

### ASP.NET Razor Pages

**CSS Framework Usage:**
- Pick ONE framework (Tailwind or Bootstrap) and use consistently
- Don't mix utility classes from different frameworks
- Create custom CSS for brand-specific styling

**Form Validation Display:**
```html
<div asp-validation-summary="ModelOnly" class="alert alert-danger"></div>

<div class="form-group">
    <label asp-for="Email"></label>
    <input asp-for="Email" class="form-control" />
    <span asp-validation-for="Email" class="text-danger"></span>
</div>
```

**Progressive Enhancement:**
- Forms work without JavaScript
- JavaScript enhances, doesn't break
- No-JS fallbacks for critical features

### WinForms / DevExpress

**Consistent Control Styling:**
```csharp
// Apply to all buttons
button.Appearance.BackColor = Color.FromArgb(94, 129, 172); // #5E81AC
button.Appearance.ForeColor = Color.White;
button.Appearance.Options.UseBackColor = true;

// Spacing
control.Margin = new Padding(8);
control.Padding = new Padding(16);
```

**DevExpress Skins:**
- Use "Office 2019 Colorful" or "Visual Studio 2019 Blue" as base
- Customize with the palette above

---

## Anti-Patterns to Avoid

### Visual
- Rainbow gradients and excessive color variety
- Animations on every interaction
- Shadows that are too dark or too large
- Borders on everything

### Typography
- Text smaller than 14px for body content
- More than 2 font families
- All caps for long text
- Poor line-height (cramped text)

### Layout
- Inconsistent spacing (random pixel values)
- Elements not aligned to grid
- Cluttered interfaces with no whitespace
- Centered text in long paragraphs

### Interaction
- Different button styles for same action type
- Missing hover/focus states
- Hover effects that obscure content
- No loading indicators

### Forms
- Labels inside inputs (disappear on focus)
- Unclear required field indicators
- Error messages far from the problem
- Submit buttons that don't indicate loading

---

## Quick Reference Checklist

Before shipping any UI, verify:

- [ ] Uses only approved color palette
- [ ] Spacing follows 8px grid system
- [ ] Typography hierarchy is clear (H1 > H2 > H3 > body)
- [ ] All interactive elements have hover/focus/disabled states
- [ ] Forms have clear labels, validation, and error messages
- [ ] Loading states implemented for async operations
- [ ] Tested at 375px, 768px, and 1440px widths
- [ ] Color contrast meets 4.5:1 ratio
- [ ] Keyboard navigation works
- [ ] No anti-patterns present

---

## Apply These Standards To:

$ARGUMENTS
