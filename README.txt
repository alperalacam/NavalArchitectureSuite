================================================================================
  NAVAL ARCHITECTURE ENGINEERING SUITE  -  Version 1.0
  Free Desktop Application for Naval Architects and Marine Engineers
  (c) 2026 Alper Alacam Naval Architecture - Concept Design Studio, Turkiye
================================================================================

WHAT THE APPLICATION DOES
--------------------------
Naval Architecture Engineering Suite is a free, standalone Windows desktop
application that brings together fourteen calculation modules covering the full
breadth of early-stage ship design. Every module is connected: dimensions entered
in Ship Builder flow automatically into Hydrostatics, Stability, Resistance &
Propulsion, Machinery, Damage Stability, Manoeuvring, and Bow Design. Results
from all modules can be compiled and exported as a single professional PDF report.

Modules included in Version 1.0:

  1.  Ship Builder         - Principal particulars, hull form coefficients,
                             3D hull preview with heel slider, Lines Plan
                             (Body Plan / Sheer Plan / Half-Breadth Plan),
                             live derived parameters, 21-station sections.
  2.  Hydrostatics         - Displacement, KB, KM, BM, GM, GZ curve.
  3.  Stability            - GZ curve, IMO IS Code criteria, loading conditions.
  4.  Resistance & Prop.   - Holtrop-Mennen resistance, EHP / DHP / BHP curves.
  5.  Machinery            - Engine selection (50-engine database), MCR/CSR,
                             SFOC curve, EEDI, NOx tier compliance.
  6.  Damage Stability     - SOLAS II-1 lost-buoyancy method, subdivision index.
  7.  Welding              - Butt and fillet joint design, heat input, preheat.
  8.  Yacht Design         - VPP, Dellenbaugh angle, comfort and capsize indices.
  9.  LNG Systems          - BOG rate, heat ingress, FGSS coverage, IGC checklist.
  10. Manoeuvring          - Turning circle, zig-zag, IMO manoeuvrability criteria.
  11. Tonnage & Freeboard  - ITC 1969 tonnage, ICLL 1966 freeboard, load line marks.
  12. Bow Design           - Kracht 1978 bulb sizing, entry angle assessment,
                             IACS UR A1 anchor equipment, SOLAS collision bulkhead.
  13. Reports              - PDF export covering all modules, paper size A4/A3/A1/A0,
                             per-section checkboxes, project title block.

Total: 3,358 live formulas across all modules.


SYSTEM REQUIREMENTS
-------------------
  Operating System : Windows 10 (64-bit) or Windows 11 (64-bit)
  Architecture     : x64 only
  Runtime          : .NET 8.0 Desktop Runtime (bundled in installer)
  RAM              : 4 GB minimum, 8 GB recommended
  Display          : 1280 x 720 minimum, 1920 x 1080 recommended
  Disk Space       : ~120 MB installed
  Graphics         : Any DirectX 9 compatible GPU (for 3D hull preview)
  Internet         : Not required - runs fully offline


HOW TO INSTALL
--------------
  1. Run  NavalArchitectureSuite_v1.0_Setup.exe
  2. Follow the on-screen prompts: Next -> Next -> Install -> Finish.
  3. A shortcut is placed on your Desktop and in the Start Menu under
     "Naval Architecture Engineering Suite".
  4. .NET 8.0 Desktop Runtime is bundled - no separate download required.

  If Windows Defender SmartScreen shows a warning on first run, click
  "More info" -> "Run anyway". The application is unsigned freeware.


HOW TO UNINSTALL
----------------
  Method A (recommended):
    Control Panel -> Programs -> Programs and Features ->
    "Naval Architecture Engineering Suite" -> Uninstall

  Method B:
    Settings -> Apps -> Installed apps ->
    Search "Naval Architecture" -> Uninstall

  Note: User settings stored in %LOCALAPPDATA%\NavalArchitectureSuite\
  are NOT removed by the uninstaller. Delete that folder manually if desired.


KNOWN LIMITATIONS - VERSION 1.0
---------------------------------
  - Hull geometry is parametric (power-law sections). It is a visual aid for
    proportion checking, NOT a faired lines plan or offset table suitable
    for production lofting or detail design.

  - Hydrostatics and stability use parametric first-principles geometry, not
    an imported hull model. Results are concept-stage estimates.

  - Damage stability uses a simplified lost-buoyancy teaching model (SOLAS II-1).
    It is not a full probabilistic flooding simulation per the latest SOLAS
    amendments.

  - LNG Systems uses a lumped single-zone heat ingress model (U*A*DT). It is
    a first-pass estimate, not a detailed multi-zone heat balance.

  - Resistance prediction (Holtrop-Mennen 1982/1984) is valid for conventional
    monohull displacement vessels within the method's applicability range.
    High-speed, planing, or unconventional hull forms may give unreliable results.

  - The Manoeuvring module uses regression-based predictors. Sea trial data
    should be used for final IMO compliance assessment.

  - The Lines Plan is generated parametrically from Ship Builder inputs.
    It is illustrative, not suitable for fairing, lofting, or production use.

  - PDF export embeds charts and drawings as rasterised images.
    Vector PDF output is not supported in Version 1.0.

  - There is no project save/load feature in Version 1.0. All values must be
    re-entered each session. Project persistence is planned for Version 2.


CONTACT
-------
  Alper Alacam Naval Architecture - Concept Design Studio
  Turkiye

  LinkedIn : linkedin.com/in/alperalacam
  GitHub   : github.com/alperalacam

  For bug reports, feature requests, or collaboration enquiries,
  please reach out via LinkedIn.

================================================================================
  Free to use. Free to share. Dedicated to engineers who believe that
  engineering knowledge should be accessible to everyone.
================================================================================
