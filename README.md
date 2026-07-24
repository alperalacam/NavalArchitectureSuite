<p align="center">
  <img src="naval_architecture_suite_256.png" width="140" alt="Naval Architecture Engineering Suite Logo">
</p>

<h1 align="center">Naval Architecture Engineering Suite v1.0</h1>

<p align="center">
  <strong>Free desktop application for naval architects and marine engineers.</strong><br>
  14 calculation modules · 3,358 live formulas · No registration · No internet required.
</p>

<p align="center">
  <a href="https://github.com/alperalacam/NavalArchitectureSuite/releases/download/v1.0/NavalArchitectureSuite_v1.0_Setup.exe">
    <img src="https://img.shields.io/badge/⬇_Download-v1.0_Setup.exe_(49.8_MB)-gold?style=for-the-badge" alt="Download v1.0">
  </a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows_10%2F11_(64--bit)-lightgrey" alt="Platform">
  <img src="https://img.shields.io/badge/Runtime-.NET_8_(bundled)-purple" alt="Runtime">
  <img src="https://img.shields.io/badge/License-Free-brightgreen" alt="License">
  <img src="https://img.shields.io/badge/Formulas-3%2C358_live-blue" alt="Formulas">
  <a href="https://www.linkedin.com/in/alper-alacam-b70b9566">
    <img src="https://img.shields.io/badge/LinkedIn-Alper_Alacam-blue" alt="LinkedIn">
  </a>
</p>

---

## What is it?

The **Naval Architecture Engineering Suite** is a free, standalone Windows desktop application covering the essential calculations of early-stage ship design in a single connected tool. It implements 3,358 live formulas across 14 modules, all within a unified dark navy and gold interface.

No internet connection required. No registration. No telemetry. No cost. Ever.

---

## Screenshot

> 3D parametric hull · Lines Plan (Body Plan, Sheer Plan, Half-Breadth) · IMO stability curves · Holtrop-Mennen resistance

---

## What's Included

### 14 Calculation Modules

| Module | Formulas | Methods & References |
|---|---|---|
| **Ship Builder** | 180 | Parametric hull geometry, power-law sections, Lackenby envelope, Lines Plan (21 stations), 3D HelixToolkit viewport with heel slider |
| **Hydrostatics** | 360 | First-principles parametric hydrostatics — KB, BM, KM, GM, GZ curve, TPC, MCT |
| **Stability** | 335 | IMO IS Code 2008 (MSC.267(85)) — all 7 statutory criteria, weather criterion, GZ curve |
| **Resistance & Propulsion** | 460 | Holtrop & Mennen (1982, 1984) — Rt, EHP, DHP, BHP vs speed curves |
| **Machinery** | 310 | 50-engine database (MAN B&W, Wärtsilä, Caterpillar, and others) · SFOC curves · IMO EEDI (MEPC.212(63)) · NOx Tier II/III |
| **Damage Stability** | 240 | SOLAS 2020 Ch.II-1 simplified lost-buoyancy method · subdivision index A/R |
| **Welding** | 195 | AWS D1.1 / DNV-ST-B101 — butt and fillet joint sizing, carbon equivalent, preheat, heat input, WPS electrode selection |
| **Yacht Design** | 220 | VPP (simplified) — DLR, SADR, CSF, Comfort Ratio, Dellenbaugh Angle, MCA/RCD comfort indices |
| **LNG Systems** | 205 | IGC Code (MSC.5(48)) — U×A×ΔT heat ingress model, BOG rate, FGSS coverage, cooldown estimate |
| **Manoeuvring** | 280 | IMO MSC.137(76) — turning circle (Kijima regression), zig-zag response, advance, tactical diameter, overshoot angle |
| **Tonnage & Freeboard** | 310 | ITC 1969 · ICLL 1966/1988 Protocol — all six load line marks, bow height (Reg. 39) |
| **Bow Design** | 112 | Kracht (1978) bulbous bow method · IACS UR A1 (2019) anchor equipment number · SOLAS collision bulkhead (Ch.II-1 Reg.11) |
| **Reports** | 95 | PDF export (SimplePdfDocument) — A4 to A0, all 13 module sections selectable, project title block |
| **Shared Utilities** | 56 | Unit conversions, OxyPlot chart renderer, shared geometry, window persistence |
| **Total** | **3,358** | |

---

## Key Features

- **3D hull preview** — parametric hull with real-time heel slider and pop-out window
- **Lines Plan** — Body Plan, Sheer Plan, Half-Breadth, 21 stations, lines-only drafting style (gold waterlines / blue stations)
- **IMO stability curves** — GZ curve with all statutory criteria marked
- **Holtrop-Mennen resistance** — EHP, DHP, BHP curves vs speed
- **50-engine machinery database** — SFOC curves, EEDI, NOx compliance
- **PDF export** — A4 · A3 · A1 · A0 paper sizes with section selector
- **Dark navy / gold palette** throughout — professional engineering aesthetic
- **Runs fully offline** — no internet, no server, no cloud
- **No project save / load** in v1 — inputs are session-only (coming in v2)

---

## System Requirements

| Requirement | Specification |
|---|---|
| Operating System | Windows 10 / 11 (64-bit) |
| Runtime | .NET 8 — **bundled inside the installer, no separate download needed** |
| RAM | 4 GB minimum, 8 GB recommended |
| Display | 1280 × 720 minimum, 1920 × 1080 recommended |
| Internet | Not required |
| Disk space | ~200 MB after installation |

---

## How to Install

1. Download `NavalArchitectureSuite_v1.0_Setup.exe` below
2. Run the installer — no administrator rights required for personal install
3. Launch from the Start menu or desktop shortcut
4. No .NET download needed — runtime is bundled

**SHA-256:** `76e1e6e6460ea141d2d67d84c7013231297d9ee401589f8253f7e98d24795779`

---

## Scientific Foundation

Based on the **Naval Architecture Teaching Toolkit** — 42 volumes, 3,000+ live formulas, all free and open educational resources. Key references used in v1.0:

- Holtrop, J. & Mennen, G.G.J. (1982, 1984) — Resistance prediction, *International Shipbuilding Progress*
- Kracht, A.M. (1978) — Design of bulbous bows, *SNAME Transactions*
- IMO IS Code (MSC.267(85)), 2008 — Intact stability
- SOLAS 2020 Consolidated Edition — Damage stability
- ICLL 1966 / 1988 Protocol — Load lines and freeboard
- ITC 1969 — Tonnage measurement
- IACS Unified Requirement A1 (2019) — Anchor equipment
- IMO EEDI Guidelines (MEPC.212(63)) — Energy efficiency
- IMO Manoeuvrability Standards (MSC.137(76)) — Turning and zig-zag
- IGC Code (MSC.5(48)), 2016 Edition — LNG cargo systems
- AWS D1.1 — Structural Welding Code

---

## Known Limitations in v1.0

- No project save / load — inputs reset on each launch
- Hull geometry is parametric (power-law) — visual aid, not a production lofting system
- PDF charts are rasterised (PNG embedded) — vector PDF coming in v2
- Resistance uses Holtrop-Mennen regression — displacement monohulls only
- Damage stability uses simplified SOLAS II-1 teaching model — not full probabilistic
- LNG uses lumped U×A×ΔT heat model — single zone
- No external hull offset import or CAD geometry
- English interface only

---

## What is Coming in v2

Version 2 is under active development. It introduces an **industrial-grade 3D CAD geometry kernel** (OpenCASCADE OCCT) — transforming Suite from an analytical toolkit into a full parametric CAD/CAE modelling environment.

**[→ See the v2 repository and roadmap](https://github.com/alperalacam/NavalArchitectureSuite-v2)**

Key additions planned for v2:

- Real NURBS hull surface (OpenCASCADE via Macad.Kernel)
- Exact hydrostatics integrated from 3D surface
- Compartment modelling — tanks, holds, voids as 3D solids
- STEP / IGES / DXF import and export (Rhino, AutoCAD, SolidWorks)
- Zebra stripe and porcupine curvature analysis
- Savitsky planing hull resistance (Vol 39)
- 2D Beam FEM — structural frame analysis and plate buckling
- 2D Thermal FEM — heat conduction, HVAC, heat exchangers
- 16 vessel categories · 195 vessel types — vessel-intelligent formula selection
- Formula Trace Panel — click any result to see every equation and source
- Class rule engine — RINA · Lloyd's Register · ABS · Turkish Lloyd
- Engine & equipment database — 200+ real entries
- Subscription desktop licensing (Student tier: free · Pro: $49/month)
- Vector PDF export — A0 to A4, ISO 128 linework hierarchy

---

## Author

**Alper Alaçam** — Naval Architect & Marine Engineer, Türkiye

[![LinkedIn](https://img.shields.io/badge/LinkedIn-Alper_Alacam-blue)](https://www.linkedin.com/in/alper-alacam-b70b9566)
[![v2 Repo](https://img.shields.io/badge/v2.0-Under_Development-blue)](https://github.com/alperalacam/NavalArchitectureSuite-v2)

---

*Free to use. Free to share. Dedicated to engineers who believe knowledge should be accessible to everyone.*

*Dedicated to my wife and daughters.*

© 2026 Alper Alaçam — Türkiye
