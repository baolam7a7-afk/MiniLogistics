---
name: MiniLogistics Storefront
description: Light e-commerce customer UI — product-first, top navigation, no sidebar
colors:
  background: "#F4F6F8"
  backgroundSecondary: "#EEF1F4"
  surface: "#FFFFFF"
  surfaceMuted: "#F8FAFB"
  border: "#E2E8F0"
  borderStrong: "#CBD5E1"
  text: "#0F172A"
  textSecondary: "#475569"
  textMuted: "#94A3B8"
  primary: "#0F766E"
  primaryHover: "#0D9488"
  primarySoft: "#CCFBF1"
  accent: "#F97316"
  accentSoft: "#FFEDD5"
  success: "#059669"
  danger: "#DC2626"
  warning: "#D97706"
typography:
  display:
    fontFamily: Plus Jakarta Sans
    fontSize: 40px
    fontWeight: 700
    lineHeight: 1.15
    letterSpacing: -0.03em
  heading:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: 700
    lineHeight: 1.25
    letterSpacing: -0.02em
  body:
    fontFamily: Plus Jakarta Sans
    fontSize: 15px
    fontWeight: 400
    lineHeight: 1.55
  label:
    fontFamily: Plus Jakarta Sans
    fontSize: 13px
    fontWeight: 600
    lineHeight: 1.4
rounded:
  sm: 8px
  md: 12px
  lg: 16px
  xl: 24px
  full: 9999px
spacing:
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 32px
  2xl: 48px
components:
  storeHeader:
    backgroundColor: "{colors.surface}"
    borderBottom: "{colors.border}"
    height: 72px
  productCard:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.lg}"
    border: "{colors.border}"
  primaryButton:
    backgroundColor: "{colors.primary}"
    rounded: "{rounded.md}"
    color: "#FFFFFF"
---

# MiniLogistics Storefront

## Overview

Customer-facing Blazor storefront for MiniLogistics. Visual language: modern Vietnamese e-commerce (Shopee / Lazada / Tiki feel) — light surfaces, teal brand, product-first layout, top navigation only.

**Design read:** redesign of customer portal for shoppers; clean commerce; no sidebar dashboard chrome.

## Colors

- **Teal primary (`#0F766E`)** — logistics/trust, distinct from purple AI defaults.
- **Soft orange accent** — CTAs, badges, promo highlights sparingly.
- **Cool gray neutrals** — consistent cool slate family on off-white canvas.
- Dark mode is out of scope for this redesign.

## Typography

Plus Jakarta Sans for display and UI. Tight tracking on large titles; medium/semibold for hierarchy. Avoid Inter/system-only stacks.

## Layout

- Full-width sticky top bar: logo · search · cart · avatar menu.
- No left sidebar. Main content max-width ~1280px, centered.
- Home: hero + product grid. Account pages (orders, addresses, reviews, profile) reached via avatar dropdown.
- Generous whitespace; clear product hierarchy (image → name → meta → CTA).

## Elevation & Depth

Soft tinted shadows (`slate` tint), not pure black. Cards elevate slightly on hover. Header uses light border + optional blur.

## Shapes

12–16px radius on cards; 8–12px on inputs/buttons. Avatars circular.

## Components

- **Header:** logo left, search center (desktop), cart + avatar right.
- **Avatar dropdown:** Hồ sơ, Đơn hàng, Địa chỉ, Đánh giá, Đăng xuất.
- **Product card:** image area, status chip, title (2-line clamp), shop meta, primary CTA.
- **Buttons:** primary teal filled; secondary outlined; danger for cancel/delete.

## Do's and Don'ts

**Do:** product-first home; consistent teal/neutral tokens; responsive collapse of search/nav; Vietnamese labels.

**Don't:** restore sidebar; purple gradients; dark dashboard chrome; emoji-heavy navigation; dense admin-style left nav for customers.
