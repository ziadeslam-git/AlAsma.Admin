# AlAsma.Admin — Design Handoff (Phase 0: Foundations)

Brand: دار الأسمى للنشر والتوزيع. Design system: **Al-Asma Pro** (shared with the store project; this admin app reuses its tokens).
Stack: ASP.NET Core MVC, Razor, Areas (SuperAdmin / Admin / Author), Tailwind via CDN.

## Files in this folder
| File | Purpose |
|---|---|
| `design-tokens.css` | Single source of colors, type, spacing, radius, shadow (light + dark). Link in `_Layout.cshtml`. |
| `tailwind.config.snippet.js` | Maps Tailwind classes to the CSS variables. Paste into the CDN `tailwind.config`. |
| `assets/*.svg` | Official logos (traced from originals). Do NOT redraw or recolor. |
| `reference/*.html` | Visual reference of the approved foundations (open in browser for look only; not production markup). |

## Logos
- `asma-wordmark-cream.svg` → sidebar header (on green), 52px high.
- `asma-wordmark-green.svg` → login card / light surfaces.
- `asma-lockup-*.svg` → login hero, PDF report header.
- `asma-feather-*.svg` → favicon, collapsed rail, watermark (opacity .08–.10 on green only), empty states (opacity .5).

## Color rules
- Only green / cream / brass family. **No purple, blue, orange, rainbow icons.**
- `--danger-*` only for delete & errors. `--accent-*` (brass) only for warnings ("ينتهي قريباً") and small markers.
- Table headers: `--surface-sunken` bg + `--ink-muted` text (no solid green/orange headers).
- PDF/Word export buttons: secondary style inside one "تصدير" menu (no red/blue buttons).
- Dark mode = deep green (`#00201e` / `#002f2c`), never black/gray.

## Typography
- UI: IBM Plex Sans Arabic. Display (greeting only): Aref Ruqaa. Tagline: Amiri.
- Min text 14px, labels/captions 12px. All numbers `.num` (tabular-nums). Currency after number: `27,485 ج.م`.

## Breakpoints & navigation
| Width | Shell |
|---|---|
| `< 640px` | Top bar 56px (page title + avatar) + fixed bottom tab bar 64px (4 items + «المزيد»), FAB «+» on Sales/Operations, safe-area padding. |
| `640–1023px` | Green icon rail 72px (right side), expands as overlay. |
| `≥ 1024px` | Green sidebar 264px (right side, RTL), logo top, user card bottom (name, role, theme toggle, logout). Content max 1280px. |

Active nav item: cream pill (`--on-brand` bg, `--primary` text) on green; on mobile bottom bar: `--chip-bg` pill.

### Per role
- **SuperAdmin**: الرئيسية، المؤلفون، المبيعات، العمليات، إدارة Admins (mobile: in «المزيد»).
- **Admin**: الرئيسية، المؤلفون (عرض فقط — no add/edit/delete controls rendered)، المبيعات، العمليات.
- **Author**: مبيعاتي، تقريري، حسابي.
- «المزيد» sheet: إدارة Admins (SuperAdmin), الوضع الليلي, تواصل واتساب (01005340021), تسجيل الخروج.
- Remove the floating theme button and the WhatsApp footer from every page.

## Components (build as Razor partials under `Views/Shared/Components/`)
- **Button**: primary / secondary (1px `--line-strong`) / ghost / danger; height 44px, radius 10px; one primary per region.
- **Input / Select**: label above (never placeholder-only), 48px, `--surface-sunken`, error = 2px `--danger-ink` border + message under field.
- **KPI card**: normal (surface + hairline) and **hero** (`--surface-brand`, feather watermark, 40px number). Always label gross vs net; info tooltip «الإيراد الخام: قبل خصم المصروفات».
- **Badge** (pill): نشط (success) / ينتهي قريباً (accent) / منتهي (danger) / غير محدد (sunken) — always icon + text. Author code = monospace chip.
- **Search + filter chips**: pill search 48px; chips 36px, selected = primary fill.
- **Table**: sunken header, hairline rows, book name + author/code stacked in first cell, totals bold with ج.م. Below 640px → **mobile card**.
- **Pagination**: 40px squares, active = primary; text «عرض 1 إلى 10 من 51 سجل».
- **Drawer** (desktop, 480px, slides in from the left edge — inline-end in RTL — so the sidebar stays visible) / **Bottom sheet** (mobile) for create/edit forms with sticky footer showing live total.
- **Modal**: confirmations (delete = soft delete message, bulk edit shows affected count). Bulk edit button is secondary, not orange.
- **Toast**, **Empty state** (feather + title + one line), **Skeleton**.

## Implementation rules for Claude Code
1. Front end only: Views, partials, `wwwroot/css`, `wwwroot/js`. Do not touch Controllers, Services, DTOs, Models, routes, authorization.
2. Keep every `asp-for`, `asp-action`, `asp-controller`, `asp-area`, field `name`, anti-forgery token, validation span and TempData message.
3. Use only `design-tokens.css` variables (or the Tailwind mapping). No new hex values in views.
4. Use logical properties (`margin-inline-start`, `inset-inline-end`), never left/right.
5. Dark mode: toggle `data-theme` on `<html>`, persist in `localStorage`, apply before paint (inline script in `<head>`).
6. `<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">`, `<meta name="theme-color" content="#004743">`, `padding-bottom: env(safe-area-inset-bottom)` on the bottom bar.
7. Icons: inline Lucide SVG, stroke 1.75, `currentColor`, 22–24px.
8. Work page by page: Layout shell → Login → Dashboards → Authors/Admins → Sales/Operations → Errors. Run the app after each page and report.

> Next design phases (dashboards, authors, sales/operations, states) will extend `reference/`. Phase 0 is enough to build the shell and shared components now.
