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

        // All primary module views are created once and kept alive so that:
        // (a) the ShipBuilder singleton feeds every module without re-instantiation, and
        // (b) Reports can read live figures from Hydrostatics, Stability, Resistance, Machinery.
        private readonly ShipBuilderView _shipBuilderView = new();
        private readonly HydrostaticsView _hydrostaticsView = new();
        private readonly StabilityView _stabilityView = new();
        private readonly ResistancePropulsionView _resistanceView = new();
        private readonly MachineryView _machineryView = new();
        private readonly DamageStabilityView _damageStabilityView = new();
        private readonly YachtDesignView _yachtDesignView = new();
        private readonly ManoeuvringView _manoeuvringView = new();
        private readonly BowDesignView _bowDesignView = new();
        private readonly ReportsView _reportsView;

        public MainWindow()
        {
            InitializeComponent();
            BtnShipBuilder.IsChecked = true;

            _reportsView = new ReportsView();
            var reportsViewModel = (ReportsViewModel)_reportsView.DataContext;
            reportsViewModel.HydrostaticsSource    = (HydrostaticsViewModel)_hydrostaticsView.DataContext;
            reportsViewModel.StabilitySource       = (StabilityViewModel)_stabilityView.DataContext;
            reportsViewModel.ResistanceSource      = (ResistancePropulsionViewModel)_resistanceView.DataContext;
            reportsViewModel.MachinerySource       = (MachineryViewModel)_machineryView.DataContext;
            reportsViewModel.DamageStabilitySource = (DamageStabilityViewModel)_damageStabilityView.DataContext;
            reportsViewModel.YachtDesignSource     = (YachtDesignViewModel)_yachtDesignView.DataContext;
            reportsViewModel.ManoeuvringSource     = (ManoeuvringViewModel)_manoeuvringView.DataContext;
            reportsViewModel.BowDesignSource       = (BowDesignViewModel)_bowDesignView.DataContext;
            reportsViewModel.WeldingSource         = new WeldingViewModel();
            reportsViewModel.TonnageSource         = new TonnageFreeboardViewModel();
            reportsViewModel.BodyPlanCanvasRef     = _shipBuilderView.BodyPlan;

            RestoreWindowBounds();
            ClampToWorkArea();
            Closing += MainWindow_Closing;
        }

        private void RestoreWindowBounds()
        {
            var saved = WindowSettingsService.Load();
            if (saved is null || !IsOnScreen(saved))
            {
                // No usable saved position (first run, or the saved position no longer
                // fits any connected monitor) - fall back to the centered XAML defaults.
                return;
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = saved.Left;
            Top = saved.Top;
            Width = Math.Max(saved.Width, MinWidth);
            Height = Math.Max(saved.Height, MinHeight);
            WindowState = saved.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        /// <summary>
        /// The taskbar (and any other docked app-bars) shrink SystemParameters.WorkArea below
        /// the raw screen resolution - e.g. a 1280x720 display commonly has only a 1280x672
        /// work area once a ~48px taskbar is subtracted. Re-centering on the full screen
        /// bounds (as WindowStartupLocation=CenterScreen alone does) does not account for
        /// this, so a window sized to the raw resolution still overlaps the taskbar. Clamp
        /// to the work area and re-center within it so startup never overlaps the taskbar,
        /// regardless of taskbar height/position or monitor resolution.
        /// </summary>
        private void ClampToWorkArea()
        {
            if (WindowState != WindowState.Normal) return;

            var wa = SystemParameters.WorkArea;
            double width = Math.Min(Width, wa.Width);
            double height = Math.Min(Height, wa.Height);
            Width = width;
            Height = height;
            Left = wa.Left + (wa.Width - width) / 2.0;
            Top = wa.Top + (wa.Height - height) / 2.0;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            var bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, Width, Height)
                : RestoreBounds;

            WindowSettingsService.Save(new WindowSettings
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Width = bounds.Width,
                Height = bounds.Height,
                Maximized = WindowState == WindowState.Maximized
            });
        }

        private static bool IsOnScreen(WindowSettings s)
        {
            return s.Width > 0 && s.Height > 0
                && s.Left + s.Width > SystemParameters.VirtualScreenLeft
                && s.Left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
                && s.Top + s.Height > SystemParameters.VirtualScreenTop
                && s.Top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
        }

        private void NavButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton { Tag: string key })
            {
                ShowModule(key);
            }
        }

        private void ShowModule(string key)
        {
            if (key == "ShipBuilder")
            {
                ContentFrame.Content = _shipBuilderView;
                return;
            }

            if (key == "Hydrostatics")
            {
                ContentFrame.Content = _hydrostaticsView;
                return;
            }

            if (key == "Resistance")
            {
                ContentFrame.Content = _resistanceView;
                return;
            }

            if (key == "Machinery")
            {
                ContentFrame.Content = _machineryView;
                return;
            }

            if (key == "Stability")
            {
                ContentFrame.Content = _stabilityView;
                return;
            }

            if (key == "DamageStability")
            {
                ContentFrame.Content = new DamageStabilityView();
                return;
            }

            if (key == "Welding")
            {
                ContentFrame.Content = new WeldingView();
                return;
            }

            if (key == "YachtDesign")
            {
                ContentFrame.Content = new YachtDesignView();
                return;
            }

            if (key == "LngSystems")
            {
                ContentFrame.Content = new LngSystemsView();
                return;
            }

            if (key == "Manoeuvring")
            {
                ContentFrame.Content = new ManoeuvringView();
                return;
            }

            if (key == "TonnageFreeboard")
            {
                ContentFrame.Content = new TonnageFreeboardView();
                return;
            }

            if (key == "Reports")
            {
                ContentFrame.Content = _reportsView;
                return;
            }

            if (key == "BowDesign")
            {
                ContentFrame.Content = _bowDesignView;
                return;
            }

            if (_navItems.TryGetValue(key, out var item))
            {
                ContentFrame.Content = new ModulePlaceholderView(item);
            }
        }

        private static Dictionary<string, NavItem> BuildNavItems()
        {
            var items = new List<NavItem>
            {
                new()
                {
                    Key = "ShipBuilder",
                    Name = "Ship Builder",
                    Abbreviation = "SB",
                    Subtitle = "Parametric Hull & General Arrangement Modeling",
                    Description = "Define principal particulars and generate the hull form, frame tables and general arrangement for any vessel type, feeding every downstream calculation module in the suite.",
                    Features = new List<string>
                    {
                        "Parametric hull surface generation",
                        "Lines plan & offset table export",
                        "Compartment & tank layout editor",
                        "Midship section and scantling references",
                        "3D hull preview (HelixToolkit viewport)",
                        "Project data stored in local SQLite model"
                    }
                },
                new()
                {
                    Key = "Hydrostatics",
                    Name = "Hydrostatics",
                    Abbreviation = "HY",
                    Subtitle = "Displacement, Trim & Stability Curves",
                    Description = "Compute hydrostatic properties across the full draft and trim range, including displacement, KB, BM, waterplane properties and cross curves of stability.",
                    Features = new List<string>
                    {
                        "Displacement & deadweight scale",
                        "KB, BM, LCB, LCF vs. draft",
                        "Cross curves of stability (KN)",
                        "Bonjean curves and sectional areas",
                        "Trim & list solver",
                        "OxyPlot hydrostatic curve charts"
                    }
                },
                new()
                {
                    Key = "Resistance",
                    Name = "Resistance and Propulsion",
                    Abbreviation = "RP",
                    Subtitle = "Speed-Power Prediction & Propeller Design",
                    Description = "Predict calm-water resistance and required propulsive power using standard series methods, then size and evaluate the propeller for the design speed range.",
                    Features = new List<string>
                    {
                        "Holtrop-Mennen resistance prediction",
                        "Speed-power and speed-RPM curves",
                        "Propeller series charts (B-series, Ka)",
                        "Sea margin & engine matching",
                        "Fuel consumption estimation",
                        "Wake fraction & thrust deduction"
                    }
                },
                new()
                {
                    Key = "Machinery",
                    Name = "Machinery",
                    Abbreviation = "MC",
                    Subtitle = "Engine Room Layout & Powertrain Selection",
                    Description = "Select and arrange main engines, gearboxes, shafting and auxiliary machinery, and verify power balance against the propulsion and electrical load requirements.",
                    Features = new List<string>
                    {
                        "Main engine selection database",
                        "Shaft line & gearbox sizing",
                        "Electrical load balance",
                        "Engine room arrangement",
                        "Auxiliary system schematics",
                        "Noise & vibration references"
                    }
                },
                new()
                {
                    Key = "Stability",
                    Name = "Stability",
                    Abbreviation = "ST",
                    Subtitle = "Intact Stability Criteria (IMO IS Code)",
                    Description = "Evaluate intact stability against IMO IS Code and flag-state criteria across all loading conditions, with GZ curve analysis and weather criterion checks.",
                    Features = new List<string>
                    {
                        "GZ curve generation & compliance check",
                        "Loading condition manager",
                        "Weather criterion (severe wind & rolling)",
                        "Grain code stability check",
                        "Free surface correction",
                        "Automated criteria pass/fail report"
                    }
                },
                new()
                {
                    Key = "DamageStability",
                    Name = "Damage Stability",
                    Abbreviation = "DS",
                    Subtitle = "SOLAS Damage Stability & Probabilistic Method",
                    Description = "Run deterministic and probabilistic damage stability analyses per SOLAS Ch. II-1, computing attained and required subdivision index across the damage case matrix.",
                    Features = new List<string>
                    {
                        "Probabilistic method (Attained Index A)",
                        "Damage case generator & matrix editor",
                        "Flooding simulation & equilibrium solver",
                        "SOLAS 2020 subdivision requirements",
                        "Cross-flooding arrangement checks",
                        "Damage stability booklet export"
                    }
                },
                new()
                {
                    Key = "Welding",
                    Name = "Welding",
                    Abbreviation = "WL",
                    Subtitle = "Weld Joint Design & Procedure Specifications",
                    Description = "Design structural weld joints and generate welding procedure specifications consistent with classification society and structural design requirements.",
                    Features = new List<string>
                    {
                        "Weld joint type & size calculator",
                        "WPS/PQR document generator",
                        "Fillet & butt weld strength checks",
                        "Electrode & consumable selection",
                        "NDT inspection plan references",
                        "Class society weld standards library"
                    }
                },
                new()
                {
                    Key = "YachtDesign",
                    Name = "Yacht Design",
                    Abbreviation = "YD",
                    Subtitle = "Sailing & Motor Yacht Performance Design",
                    Description = "Purpose-built tools for sailing and motor yacht design, covering righting moment, sail plan balance, VPP performance prediction and comfort criteria.",
                    Features = new List<string>
                    {
                        "Velocity Prediction Program (VPP)",
                        "Sail plan & rig balance",
                        "Righting moment & ballast sizing",
                        "Motor yacht planing performance",
                        "Comfort & seakeeping indices",
                        "MCA / RCD compliance references"
                    }
                },
                new()
                {
                    Key = "LngSystems",
                    Name = "LNG Systems",
                    Abbreviation = "LNG",
                    Subtitle = "Cargo Containment & Boil-Off Gas Management",
                    Description = "Model LNG cargo containment systems, boil-off gas generation and management, and fuel gas supply for LNG carriers and gas-fuelled vessels.",
                    Features = new List<string>
                    {
                        "Membrane & Moss containment sizing",
                        "Boil-off gas rate estimation",
                        "Cargo tank thermal insulation design",
                        "Fuel gas supply system (FGSS) sizing",
                        "IGC Code compliance checks",
                        "Cargo handling & cooldown sequencing"
                    }
                },
                new()
                {
                    Key = "Manoeuvring",
                    Name = "Manoeuvring",
                    Abbreviation = "MN",
                    Subtitle = "Turning Circle, Zig-Zag & Course-Keeping Analysis",
                    Description = "Predict manoeuvring performance including turning circle, zig-zag and stopping trials, and verify compliance with IMO manoeuvrability standards.",
                    Features = new List<string>
                    {
                        "Turning circle simulation",
                        "Zig-zag (Z-test) response prediction",
                        "Course-keeping & yaw checking ability",
                        "Rudder & steering gear sizing",
                        "IMO manoeuvrability standard checks",
                        "Sea trial data comparison"
                    }
                },
                new()
                {
                    Key = "TonnageFreeboard",
                    Name = "Tonnage and Freeboard",
                    Abbreviation = "TF",
                    Subtitle = "ITC69 Tonnage & ICLL Freeboard Calculations",
                    Description = "Calculate gross and net tonnage under the 1969 Tonnage Convention and minimum freeboard under the International Convention on Load Lines, with certificate-ready output.",
                    Features = new List<string>
                    {
                        "Gross & net tonnage (ITC69)",
                        "Minimum freeboard (ICLL 1966/88 Prot.)",
                        "Load line mark position",
                        "Timber & tropical freeboard corrections",
                        "Tonnage mark calculation",
                        "Statutory certificate data sheets"
                    }
                },
                new()
                {
                    Key = "Reports",
                    Name = "Reports",
                    Abbreviation = "RPT",
                    Subtitle = "Automated Class & Statutory Report Generation",
                    Description = "Compile calculation results from every module into formatted class-ready and statutory reports, with revision tracking and export to standard document formats.",
                    Features = new List<string>
                    {
                        "Automated report compilation",
                        "Class society submission templates",
                        "Revision history & version control",
                        "PDF / Word / Excel export",
                        "Company & project title blocks",
                        "Formula library audit trail (3246 formulas)"
                    }
                }
            };

            var dict = new Dictionary<string, NavItem>();
            foreach (var item in items)
            {
                dict[item.Key] = item;
            }
            return dict;
        }
    }
}
