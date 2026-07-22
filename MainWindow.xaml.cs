using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NavalArchitectureSuite.Models;
using NavalArchitectureSuite.Services;
using NavalArchitectureSuite.ViewModels;
using NavalArchitectureSuite.Views;

namespace NavalArchitectureSuite
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, NavItem> _navItems = BuildNavItems();

        private readonly ShipBuilderView         _shipBuilderView     = new();
        private readonly HydrostaticsView        _hydrostaticsView    = new();
        private readonly StabilityView           _stabilityView       = new();
        private readonly ResistancePropulsionView _resistanceView     = new();
        private readonly MachineryView           _machineryView       = new();
        private readonly DamageStabilityView     _damageStabilityView = new();
        private readonly YachtDesignView         _yachtDesignView     = new();
        private readonly ManoeuvringView         _manoeuvringView     = new();
        private readonly BowDesignView           _bowDesignView       = new();
        private readonly LngSystemsView          _lngView             = new();
        private readonly ReportsView             _reportsView;

        public MainWindow()
        {
            InitializeComponent();
            BtnShipBuilder.IsChecked = true;

            _reportsView = new ReportsView();
            var rvm = (ReportsViewModel)_reportsView.DataContext;
            rvm.HydrostaticsSource    = (HydrostaticsViewModel)_hydrostaticsView.DataContext;
            rvm.StabilitySource       = (StabilityViewModel)_stabilityView.DataContext;
            rvm.ResistanceSource      = (ResistancePropulsionViewModel)_resistanceView.DataContext;
            rvm.MachinerySource       = (MachineryViewModel)_machineryView.DataContext;
            rvm.DamageStabilitySource = (DamageStabilityViewModel)_damageStabilityView.DataContext;
            rvm.YachtDesignSource     = (YachtDesignViewModel)_yachtDesignView.DataContext;
            rvm.ManoeuvringSource     = (ManoeuvringViewModel)_manoeuvringView.DataContext;
            rvm.BowDesignSource       = (BowDesignViewModel)_bowDesignView.DataContext;
            rvm.LngSource             = (LngSystemsViewModel)_lngView.DataContext;
            rvm.WeldingSource         = new WeldingViewModel();
            rvm.TonnageSource         = new TonnageFreeboardViewModel();
            rvm.ShipBuilderViewRef    = _shipBuilderView;

            RestoreWindowBounds();
            ClampToWorkArea();
            Closing += MainWindow_Closing;
        }

        private void RestoreWindowBounds()
        {
            var saved = WindowSettingsService.Load();
            if (saved is null || !IsOnScreen(saved)) return;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left   = saved.Left;
            Top    = saved.Top;
            Width  = Math.Max(saved.Width,  MinWidth);
            Height = Math.Max(saved.Height, MinHeight);
            WindowState = saved.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        private void ClampToWorkArea()
        {
            if (WindowState != WindowState.Normal) return;
            var wa   = SystemParameters.WorkArea;
            double w = Math.Min(Width,  wa.Width);
            double h = Math.Min(Height, wa.Height);
            Width = w; Height = h;
            Left  = wa.Left + (wa.Width  - w) / 2.0;
            Top   = wa.Top  + (wa.Height - h) / 2.0;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            var bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            WindowSettingsService.Save(new WindowSettings
            {
                Left      = bounds.Left,  Top    = bounds.Top,
                Width     = bounds.Width, Height = bounds.Height,
                Maximized = WindowState == WindowState.Maximized
            });
        }

        private static bool IsOnScreen(WindowSettings s) =>
            s.Width > 0 && s.Height > 0
            && s.Left + s.Width  > SystemParameters.VirtualScreenLeft
            && s.Left            < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
            && s.Top  + s.Height > SystemParameters.VirtualScreenTop
            && s.Top             < SystemParameters.VirtualScreenTop  + SystemParameters.VirtualScreenHeight;

        private void NavButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton { Tag: string key }) ShowModule(key);
        }

        private void ShowModule(string key)
        {
            if (key == "ShipBuilder")     { ContentFrame.Content = _shipBuilderView;     return; }
            if (key == "Hydrostatics")    { ContentFrame.Content = _hydrostaticsView;    return; }
            if (key == "Resistance")      { ContentFrame.Content = _resistanceView;      return; }
            if (key == "Machinery")       { ContentFrame.Content = _machineryView;       return; }
            if (key == "Stability")       { ContentFrame.Content = _stabilityView;       return; }
            if (key == "DamageStability") { ContentFrame.Content = _damageStabilityView; return; }
            if (key == "YachtDesign")     { ContentFrame.Content = _yachtDesignView;     return; }
            if (key == "Manoeuvring")     { ContentFrame.Content = _manoeuvringView;     return; }
            if (key == "BowDesign")       { ContentFrame.Content = _bowDesignView;       return; }
            if (key == "LngSystems")      { ContentFrame.Content = _lngView;             return; }
            if (key == "Reports")         { ContentFrame.Content = _reportsView;         return; }
            if (key == "Welding")         { ContentFrame.Content = new WeldingView();          return; }
            if (key == "TonnageFreeboard"){ ContentFrame.Content = new TonnageFreeboardView(); return; }
            if (_navItems.TryGetValue(key, out var item))
                ContentFrame.Content = new ModulePlaceholderView(item);
        }

        private static Dictionary<string, NavItem> BuildNavItems()
        {
            var items = new List<NavItem>
            {
                new()
                {
                    Key          = "ShipBuilder",
                    Name         = "Ship Builder",
                    Abbreviation = "SB",
                    Subtitle     = "Parametric Hull & Principal Particulars",
                    Description  = "Enter vessel dimensions and hull form coefficients. All values feed every downstream module automatically. The 3D viewport and Lines Plan update live as you type.",
                    Features     = new List<string>
                    {
                        "Vessel type selector & principal dimensions (Lpp, B, D, T, V)",
                        "Hull form coefficients: Cb, Cm, Cwp, Cp",
                        "Live derived parameters: displacement, Froude number, L/B, B/T, D/T",
                        "3D parametric hull preview with heel slider and pop-out window",
                        "Lines Plan: Body Plan, Sheer Plan, Half-Breadth Plan (21 stations)",
                        "Master input — feeds all 13 modules automatically"
                    }
                },
                new()
                {
                    Key          = "Hydrostatics",
                    Name         = "Hydrostatics",
                    Abbreviation = "HY",
                    Subtitle     = "Displacement, KB, BM, GM & GZ Curve",
                    Description  = "Computes key hydrostatic properties from the parametric hull defined in Ship Builder. Results update instantly with any dimension change.",
                    Features     = new List<string>
                    {
                        "Displacement (tonnes) from Cb, Lpp, B, T",
                        "KB — height of centre of buoyancy above keel",
                        "BM — metacentric radius",
                        "KM — transverse metacentre above keel",
                        "GM — initial metacentric height",
                        "GZ curve chart (OxyPlot, gold line)"
                    }
                },
                new()
                {
                    Key          = "Resistance",
                    Name         = "Resistance and Propulsion",
                    Abbreviation = "RP",
                    Subtitle     = "Holtrop-Mennen Speed-Power Prediction",
                    Description  = "Predicts calm-water resistance and required propulsive power using the Holtrop-Mennen (1982/1984) regression method for conventional displacement vessels.",
                    Features     = new List<string>
                    {
                        "Total resistance Rt (kN) vs speed — Holtrop-Mennen method",
                        "Frictional resistance Rf and residuary resistance Rr",
                        "Effective power EHP (kW)",
                        "Delivered power DHP (kW)",
                        "Brake power BHP (kW)",
                        "Resistance curve chart with Rt / Rf / Rr (OxyPlot)"
                    }
                },
                new()
                {
                    Key          = "Machinery",
                    Name         = "Machinery",
                    Abbreviation = "MC",
                    Subtitle     = "Engine Selection, SFOC Curve & EEDI",
                    Description  = "Select a main engine from a 50-engine database, set MCR and CSR power, read the SFOC curve, and check EEDI and NOx tier compliance.",
                    Features     = new List<string>
                    {
                        "50-engine dropdown database (MAN B&W, Wartsila, Caterpillar and others)",
                        "MCR and CSR power settings",
                        "SFOC curve vs load (% MCR) — OxyPlot chart with CSR and MCR markers",
                        "Daily fuel consumption at CSR (t/day)",
                        "Attained EEDI calculation (MEPC.212(63))",
                        "NOx Tier II / Tier III compliance reference"
                    }
                },
                new()
                {
                    Key          = "Stability",
                    Name         = "Stability",
                    Abbreviation = "ST",
                    Subtitle     = "GZ Curve & IMO IS Code Criteria",
                    Description  = "Evaluates intact stability against the IMO 2008 IS Code criteria for a selected loading condition, with GZ curve and automated pass/fail checks.",
                    Features     = new List<string>
                    {
                        "GZ curve for selected loading condition (Light Ship, Full Load, Ballast)",
                        "Area under GZ 0-30 deg >= 0.055 m.rad",
                        "Area under GZ 0-40 deg >= 0.090 m.rad",
                        "Area under GZ 30-40 deg >= 0.030 m.rad",
                        "GZ at 30 deg >= 0.200 m and angle of max GZ >= 25 deg",
                        "All criteria with PASS / FAIL status and GZ chart"
                    }
                },
                new()
                {
                    Key          = "DamageStability",
                    Name         = "Damage Stability",
                    Abbreviation = "DS",
                    Subtitle     = "SOLAS II-1 Lost-Buoyancy Method",
                    Description  = "Simplified damage stability analysis using the SOLAS II-1 lost-buoyancy teaching model. Computes damaged draft, sinkage, GM and subdivision index.",
                    Features     = new List<string>
                    {
                        "Lost-buoyancy method (SOLAS Ch.II-1, simplified teaching model)",
                        "Damage case selector (Fwd Peak / Collision Bulkhead and others)",
                        "Damaged draft and sinkage calculation",
                        "GM (damaged) and maximum residual GZ",
                        "Attained subdivision index A vs required index R",
                        "Residual GZ curve for selected damage case (OxyPlot)"
                    }
                },
                new()
                {
                    Key          = "Welding",
                    Name         = "Welding",
                    Abbreviation = "WL",
                    Subtitle     = "Joint Design, Heat Input & Preheat",
                    Description  = "Designs butt and fillet weld joints for ship structural steelwork. Calculates governing thickness, tensile stress, heat input, carbon equivalent and preheat temperature.",
                    Features     = new List<string>
                    {
                        "Butt joint (full penetration) and fillet joint type selector",
                        "Steel grade library: AH36/DH36, EH36, A131 and others",
                        "Governing thickness, tensile stress and allowable stress check",
                        "Fillet: throat thickness, leg size and weld utilisation",
                        "Arc heat input Q (kJ/mm) and carbon equivalent preheat",
                        "AWS electrode class, minimum preheat and recommended process"
                    }
                },
                new()
                {
                    Key          = "YachtDesign",
                    Name         = "Yacht Design",
                    Abbreviation = "YD",
                    Subtitle     = "Sailing Yacht Performance Indices & VPP",
                    Description  = "Calculates key sailing yacht performance and comfort indices and generates a VPP speed polar from the vessel parameters in Ship Builder.",
                    Features     = new List<string>
                    {
                        "Displacement/Length Ratio (DLR)",
                        "Sail Area/Displacement Ratio (SADR)",
                        "Capsize Screening Formula (CSF)",
                        "Comfort Ratio with band classification (Coastal to Offshore)",
                        "Hull speed (kts) and Dellenbaugh Angle (deg)",
                        "VPP speed polar chart vs true wind angle (OxyPlot)"
                    }
                },
                new()
                {
                    Key          = "LngSystems",
                    Name         = "LNG Systems",
                    Abbreviation = "LNG",
                    Subtitle     = "BOG Rate, Heat Ingress & FGSS Coverage",
                    Description  = "Models LNG cargo containment heat ingress, boil-off gas generation, fuel gas supply system coverage, and voyage BOG loss. Includes an IGC Code compliance checklist.",
                    Features     = new List<string>
                    {
                        "Containment type selector: Membrane (GTT Mark III / NO96) or Moss",
                        "Heat ingress Q (kW) via lumped U*A*dT model (ambient 20C / LNG -162C)",
                        "BOG rate: kg/day, m3/day and % of capacity/day with PASS criterion",
                        "FGSS coverage ratio: natural BOG vs ME + AE fuel demand",
                        "Voyage BOG loss over user-defined voyage duration",
                        "IGC Code compliance checklist (awareness reference, Ch.4/8/9/13)"
                    }
                },
                new()
                {
                    Key          = "Manoeuvring",
                    Name         = "Manoeuvring",
                    Abbreviation = "MN",
                    Subtitle     = "Turning Circle, Zig-Zag & IMO Criteria",
                    Description  = "Predicts manoeuvring performance using regression-based methods. Generates turning circle trajectory and zig-zag response charts and checks IMO manoeuvrability standards.",
                    Features     = new List<string>
                    {
                        "Advance, tactical diameter and steady turning diameter (m)",
                        "First overshoot angle for zig-zag manoeuvre (deg)",
                        "Turning circle trajectory chart (X vs Y, OxyPlot)",
                        "Zig-zag manoeuvre chart: heading and rudder vs time",
                        "IMO MSC.137(76) manoeuvrability standard reference",
                        "All results fed to PDF report automatically"
                    }
                },
                new()
                {
                    Key          = "TonnageFreeboard",
                    Name         = "Tonnage and Freeboard",
                    Abbreviation = "TF",
                    Subtitle     = "ITC 1969 Tonnage & ICLL 1966 Freeboard",
                    Description  = "Calculates gross and net tonnage under the 1969 International Tonnage Convention and minimum freeboard under the 1966 Load Line Convention with all six load line marks.",
                    Features     = new List<string>
                    {
                        "Gross Tonnage (GT) and Net Tonnage (NT) — ITC 1969 method",
                        "K1, K2, K3 coefficients",
                        "Table freeboard with Cb correction, depth correction and sheer correction",
                        "Assigned Summer freeboard (m)",
                        "All six load line marks: S, W, WNA, T, F, TF",
                        "Minimum bow height check (ICLL Reg.39) with PASS / FAIL"
                    }
                },
                new()
                {
                    Key          = "Reports",
                    Name         = "Reports",
                    Abbreviation = "RPT",
                    Subtitle     = "PDF Export — All 13 Modules in One Document",
                    Description  = "Compiles live results from every module into a single PDF report. Select paper size, tick the sections you want, fill in the project title block and export.",
                    Features     = new List<string>
                    {
                        "PDF export covering all 13 modules — per-section checkboxes",
                        "Paper size selector: A4, A3, A1, A0 (full-size Lines Plan)",
                        "Project title block: name, number, client, class society",
                        "All charts embedded as images (GZ curves, resistance, SFOC, VPP, manoeuvring)",
                        "Lines Plan: Body Plan / Sheer Plan / Half-Breadth selectable per view",
                        "Formula library audit table — 3,358 live formulas across all modules"
                    }
                },
            };

            var dict = new Dictionary<string, NavItem>();
            foreach (var item in items) dict[item.Key] = item;
            return dict;
        }
    }
}
