using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NavalArchitectureSuite.Models;
using NavalArchitectureSuite.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>Single row of the MCR/CSR/NCR fuel summary table.</summary>
    public class EnginePowerPoint
    {
        public string Label { get; set; } = "";
        public double PowerKw { get; set; }
        public double LoadPercent { get; set; }
        public double SfocGPerKwh { get; set; }
        public double DailyFuelTonnes { get; set; }
        public double RangeNm { get; set; }
    }

    /// <summary>
    /// Marine diesel engine selection and machinery-fitness calculator.
    ///
    /// Power staging (MCR/CSR/NCR) follows standard ship-design margin practice
    /// (e.g. MAN B&amp;W / WinGD project guides): the power required at the propeller
    /// for the design speed is uplifted by a sea/weather margin to give the
    /// Continuous Service Rating (the power actually needed at sea), then an
    /// engine margin is applied so the selected engine's Maximum Continuous
    /// Rating (MCR, 100%) sits above that service point:
    ///   CSR   = RequiredPower * (1 + SeaMargin)
    ///   MCR   = CSR / (1 - EngineMargin)
    ///   NCR   = MCR * NcrPercent   (engine builder's reference rating, typ. 90% MCR)
    ///
    /// The SFOC-vs-load curve is modelled as the quadratic every low/medium-speed
    /// diesel test-bed curve approximates: a minimum near 80% MCR rising toward
    /// both idle and 100% MCR (Woodyard, "Pounder's Marine Diesel Engines"):
    ///   SFOC(L) = SfocMin + k*(L - LoadAtMin)^2,  k = (Sfoc100 - SfocMin) / (1 - LoadAtMin)^2
    ///
    /// EEDI uses the simplified single-main-engine form of the IMO formula
    /// (MARPOL Annex VI reg. 20/21, MEPC.212(63)) with no innovative energy-saving
    /// technologies, shaft generators or ice class (fj = feff = fi = fc = fl = fw = 1):
    ///   Attained EEDI = (P_ME*C_F,ME*SFC_ME(75%) + P_AE*C_F,AE*SFC_AE) / (Capacity * Vref)
    ///   P_ME = 0.75 * MCR,  P_AE = 0.025*MCR+250 (MCR>10000kW) else 0.05*MCR
    ///   Required EEDI  = a * Capacity^(-c) * (1 - X/100)
    /// Reference parameters a, c are MEPC.212(63) Table 1 values by ship type.
    /// Reduction factor X is the largest-size-bracket headline value per phase
    /// (Table 2) — consult MARPOL Annex VI reg. 21 directly for size-adjusted
    /// values on borderline tonnages.
    ///
    /// NOx limits use the exact MARPOL Annex VI regulation 13 piecewise curves
    /// for Tier I/II/III versus rated engine speed n (rpm).
    /// </summary>
    public class MachineryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public static readonly string[] ShipTypeNames =
        {
            "Bulk Carrier", "Gas Carrier", "Tanker", "Container Ship",
            "General Cargo Ship", "Refrigerated Cargo Carrier", "Combination Carrier"
        };

        // (a, c) reference-line parameters, MEPC.212(63) Table 1
        private static readonly (double a, double c)[] ShipTypeParams =
        {
            (961.79, 0.477), (1120.00, 0.456), (1218.80, 0.488), (174.22, 0.201),
            (107.48, 0.216), (227.01, 0.244), (1219.00, 0.488)
        };

        public static readonly string[] PhaseNames =
        {
            "Phase 0 (pre-2015)", "Phase 1 (2015-2019)", "Phase 2 (2020-2024)", "Phase 3 (2025+)"
        };

        private static readonly double[] PhaseReductionPercent = { 0.0, 10.0, 20.0, 30.0 };

        public static readonly string[] FuelTypeNames =
        {
            "HFO / Residual (ISO 8217 RM)", "MDO / MGO (ISO 8217 DM)", "LNG"
        };

        // CF, t-CO2 / t-fuel, MEPC.212(63)/245(66)
        private static readonly double[] FuelCarbonFactor = { 3.114, 3.206, 2.750 };

        #region Engine selector

        /// <summary>Flat list of all engines for the ComboBox, with a blank "Manual entry" at top.</summary>
        public static IReadOnlyList<MarineEngine> EngineList { get; } = MarineEngineDatabase.Engines;

        /// <summary>Category groups for grouped display.</summary>
        public static IReadOnlyList<string> EngineCategories { get; } =
            MarineEngineDatabase.Engines.Select(e => e.Category).Distinct().ToList();

        private MarineEngine? _selectedEngine;
        public MarineEngine? SelectedEngine
        {
            get => _selectedEngine;
            set
            {
                if (SetField(ref _selectedEngine, value) && value is not null)
                    ApplyEnginePreset(value);
            }
        }

        private void ApplyEnginePreset(MarineEngine eng)
        {
            // Auto-fill all related input fields from the selected engine.
            _engineModel    = eng.DisplayName;
            _sfocAt100      = eng.SfocAt100;
            _sfocMin        = eng.SfocMin;
            _loadAtMinSfoc  = eng.LoadAtMinSfoc;
            _ratedEngineSpeed = eng.RatedRpm;

            // Set MCR from engine rating and back-calculate RequiredPower
            // so the user sees the engine's actual MCR immediately.
            _requiredPower = eng.McrKw * (1.0 - Math.Clamp(EngineMargin, 0, 90) / 100.0)
                                       / (1.0 + Math.Clamp(SeaMargin,   0, 50) / 100.0);

            OnPropertyChanged(nameof(EngineModel));
            OnPropertyChanged(nameof(SfocAt100));
            OnPropertyChanged(nameof(SfocMin));
            OnPropertyChanged(nameof(LoadAtMinSfoc));
            OnPropertyChanged(nameof(RatedEngineSpeed));
            OnPropertyChanged(nameof(RequiredPower));

            Recalculate();
        }

        #endregion

        #region Inputs — Power & Engine Selection

        private string _engineModel = "";
        public string EngineModel { get => _engineModel; set => SetField(ref _engineModel, value); }

        private double _requiredPower = 6000.0;
        public double RequiredPower { get => _requiredPower; set { if (SetField(ref _requiredPower, value)) Recalculate(); } }

        private double _shipSpeed = 16.0;
        public double ShipSpeed { get => _shipSpeed; set { if (SetField(ref _shipSpeed, value)) Recalculate(); } }

        private double _seaMargin = 15.0;
        public double SeaMargin { get => _seaMargin; set { if (SetField(ref _seaMargin, value)) Recalculate(); } }

        private double _engineMargin = 10.0;
        public double EngineMargin { get => _engineMargin; set { if (SetField(ref _engineMargin, value)) Recalculate(); } }

        private double _ncrPercent = 90.0;
        public double NcrPercent { get => _ncrPercent; set { if (SetField(ref _ncrPercent, value)) Recalculate(); } }

        #endregion

        #region Inputs — SFOC curve

        private double _sfocAt100 = 175.0;
        public double SfocAt100 { get => _sfocAt100; set { if (SetField(ref _sfocAt100, value)) Recalculate(); } }

        private double _sfocMin = 170.0;
        public double SfocMin { get => _sfocMin; set { if (SetField(ref _sfocMin, value)) Recalculate(); } }

        private double _loadAtMinSfoc = 80.0; // % MCR
        public double LoadAtMinSfoc { get => _loadAtMinSfoc; set { if (SetField(ref _loadAtMinSfoc, value)) Recalculate(); } }

        #endregion

        #region Inputs — Fuel & Range

        private double _fuelCapacity = 1200.0;
        public double FuelCapacity { get => _fuelCapacity; set { if (SetField(ref _fuelCapacity, value)) Recalculate(); } }

        #endregion

        #region Inputs — EEDI

        private double _capacityDwt = 50000.0;
        public double CapacityDwt { get => _capacityDwt; set { if (SetField(ref _capacityDwt, value)) Recalculate(); } }

        private int _shipTypeIndex = 0;
        public int ShipTypeIndex { get => _shipTypeIndex; set { if (SetField(ref _shipTypeIndex, value)) Recalculate(); } }

        private int _phaseIndex = 2;
        public int PhaseIndex { get => _phaseIndex; set { if (SetField(ref _phaseIndex, value)) Recalculate(); } }

        private int _meFuelIndex = 0;
        public int MeFuelIndex { get => _meFuelIndex; set { if (SetField(ref _meFuelIndex, value)) Recalculate(); } }

        private int _aeFuelIndex = 0;
        public int AeFuelIndex { get => _aeFuelIndex; set { if (SetField(ref _aeFuelIndex, value)) Recalculate(); } }

        private double _sfcAe = 215.0;
        public double SfcAe { get => _sfcAe; set { if (SetField(ref _sfcAe, value)) Recalculate(); } }

        #endregion

        #region Inputs — NOx Tier

        private int _constructionYear = 2024;
        public int ConstructionYear { get => _constructionYear; set { if (SetField(ref _constructionYear, value)) Recalculate(); } }

        private double _ratedEngineSpeed = 500.0;
        public double RatedEngineSpeed { get => _ratedEngineSpeed; set { if (SetField(ref _ratedEngineSpeed, value)) Recalculate(); } }

        private bool _operatesInEca = false;
        public bool OperatesInEca { get => _operatesInEca; set { if (SetField(ref _operatesInEca, value)) Recalculate(); } }

        private double _actualNOx = 0.0;
        public double ActualNOx { get => _actualNOx; set { if (SetField(ref _actualNOx, value)) Recalculate(); } }

        #endregion

        #region Outputs — Power & Engine Selection

        private double _csr;
        public double Csr { get => _csr; private set => SetField(ref _csr, value); }

        private double _mcr;
        public double Mcr { get => _mcr; private set => SetField(ref _mcr, value); }

        private double _ncr;
        public double Ncr { get => _ncr; private set => SetField(ref _ncr, value); }

        private double _csrPercentOfMcr;
        public double CsrPercentOfMcr { get => _csrPercentOfMcr; private set => SetField(ref _csrPercentOfMcr, value); }

        #endregion

        #region Outputs — Fuel & Range

        public List<EnginePowerPoint> FuelTable { get; private set; } = new();

        private double _enduranceDays;
        public double EnduranceDays { get => _enduranceDays; private set => SetField(ref _enduranceDays, value); }

        private double _rangeNm;
        public double RangeNm { get => _rangeNm; private set => SetField(ref _rangeNm, value); }

        #endregion

        #region Outputs — EEDI

        private double _pMe;
        public double PMe { get => _pMe; private set => SetField(ref _pMe, value); }

        private double _pAe;
        public double PAe { get => _pAe; private set => SetField(ref _pAe, value); }

        private double _sfcMeAt75;
        public double SfcMeAt75 { get => _sfcMeAt75; private set => SetField(ref _sfcMeAt75, value); }

        private double _attainedEedi;
        public double AttainedEedi { get => _attainedEedi; private set => SetField(ref _attainedEedi, value); }

        private double _requiredEedi;
        public double RequiredEedi { get => _requiredEedi; private set => SetField(ref _requiredEedi, value); }

        private bool _eediCompliant;
        public bool EediCompliant { get => _eediCompliant; private set => SetField(ref _eediCompliant, value); }

        public string EediStatusText => RequiredEedi > 0 ? (EediCompliant ? "COMPLIANT" : "NON-COMPLIANT") : "—";

        #endregion

        #region Outputs — NOx Tier

        private double _tierILimit;
        public double TierILimit { get => _tierILimit; private set => SetField(ref _tierILimit, value); }

        private double _tierIILimit;
        public double TierIILimit { get => _tierIILimit; private set => SetField(ref _tierIILimit, value); }

        private double _tierIIILimit;
        public double TierIIILimit { get => _tierIIILimit; private set => SetField(ref _tierIIILimit, value); }

        private string _applicableTier = "";
        public string ApplicableTier { get => _applicableTier; private set => SetField(ref _applicableTier, value); }

        private double _applicableLimit;
        public double ApplicableLimit { get => _applicableLimit; private set => SetField(ref _applicableLimit, value); }

        private string _noxComplianceText = "";
        public string NoxComplianceText { get => _noxComplianceText; private set => SetField(ref _noxComplianceText, value); }

        #endregion

        public PlotModel SfocPlotModel { get; }

        public MachineryViewModel()
        {
            SfocPlotModel = BuildPlotModel();

            // Subscribe to the shared ShipBuilder singleton so that changing the design
            // speed in Ship Builder immediately updates Machinery's speed input.
            SyncFromShipBuilder();
            ShipBuilderViewModel.Instance.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ShipBuilderViewModel.DesignSpeed):
                        SyncFromShipBuilder();
                        break;
                }
            };
        }

        private void SyncFromShipBuilder()
        {
            var sb = ShipBuilderViewModel.Instance;
            _shipSpeed = sb.DesignSpeed;

            OnPropertyChanged(nameof(ShipSpeed));

            Recalculate();
        }

        /// <summary>SFOC (g/kWh) at a given load fraction (0-1) of MCR.</summary>
        private double SfocAtLoad(double loadFraction)
        {
            double lMin = Math.Clamp(LoadAtMinSfoc, 1.0, 99.0) / 100.0;
            double k = (1 - lMin) > 0 ? (SfocAt100 - SfocMin) / ((1 - lMin) * (1 - lMin)) : 0.0;
            double sfoc = SfocMin + k * (loadFraction - lMin) * (loadFraction - lMin);
            return Math.Max(sfoc, 0.0);
        }

        private void Recalculate()
        {
            // --- Power & Engine Selection ---
            Csr = RequiredPower * (1 + SeaMargin / 100.0);
            double engineMarginFrac = Math.Clamp(EngineMargin, 0.0, 90.0) / 100.0;
            Mcr = (1 - engineMarginFrac) > 0 ? Csr / (1 - engineMarginFrac) : 0.0;
            Ncr = Mcr * (NcrPercent / 100.0);
            CsrPercentOfMcr = Mcr > 0 ? Csr / Mcr * 100.0 : 0.0;

            bool valid = Mcr > 0 && ShipSpeed > 0;

            // --- Fuel & Range table (MCR / CSR / NCR) ---
            FuelTable = new List<EnginePowerPoint>();
            if (valid)
            {
                AddFuelRow("MCR (100%)", Mcr);
                AddFuelRow("CSR (service)", Csr);
                AddFuelRow("NCR", Ncr);
            }
            OnPropertyChanged(nameof(FuelTable));

            var csrRow = FuelTable.Count > 1 ? FuelTable[1] : null;
            if (csrRow != null && csrRow.DailyFuelTonnes > 0)
            {
                EnduranceDays = FuelCapacity / csrRow.DailyFuelTonnes;
                RangeNm = EnduranceDays * 24.0 * ShipSpeed;
            }
            else
            {
                EnduranceDays = 0;
                RangeNm = 0;
            }

            // --- EEDI ---
            if (valid && CapacityDwt > 0)
            {
                PMe = 0.75 * Mcr;
                PAe = Mcr > 10000 ? 0.025 * Mcr + 250.0 : 0.05 * Mcr;
                SfcMeAt75 = SfocAtLoad(0.75);

                double cfMe = FuelCarbonFactor[MeFuelIndex];
                double cfAe = FuelCarbonFactor[AeFuelIndex];

                double numerator = PMe * cfMe * SfcMeAt75 + PAe * cfAe * SfcAe;
                AttainedEedi = numerator / (CapacityDwt * ShipSpeed);

                var (a, c) = ShipTypeParams[ShipTypeIndex];
                double reduction = PhaseReductionPercent[PhaseIndex];
                RequiredEedi = a * Math.Pow(CapacityDwt, -c) * (1 - reduction / 100.0);

                EediCompliant = AttainedEedi <= RequiredEedi;
            }
            else
            {
                PMe = PAe = SfcMeAt75 = AttainedEedi = RequiredEedi = 0;
                EediCompliant = false;
            }
            OnPropertyChanged(nameof(EediStatusText));

            // --- NOx Tier ---
            double n = Math.Max(RatedEngineSpeed, 1.0);
            TierILimit = ComputeTierILimit(n);
            TierIILimit = ComputeTierIILimit(n);
            TierIIILimit = ComputeTierIIILimit(n);

            if (ConstructionYear < 2000)
            {
                ApplicableTier = "Pre-Tier (no NOx limit)";
                ApplicableLimit = double.NaN;
            }
            else if (ConstructionYear < 2011)
            {
                ApplicableTier = "Tier I";
                ApplicableLimit = TierILimit;
            }
            else if (ConstructionYear >= 2016 && OperatesInEca)
            {
                ApplicableTier = "Tier III (NOx ECA)";
                ApplicableLimit = TierIIILimit;
            }
            else
            {
                ApplicableTier = "Tier II";
                ApplicableLimit = TierIILimit;
            }

            if (double.IsNaN(ApplicableLimit))
            {
                NoxComplianceText = "N/A";
            }
            else if (ActualNOx <= 0)
            {
                NoxComplianceText = "Not evaluated";
            }
            else
            {
                NoxComplianceText = ActualNOx <= ApplicableLimit ? "COMPLIANT" : "NON-COMPLIANT";
            }

            RebuildCurve();
        }

        private void AddFuelRow(string label, double powerKw)
        {
            double loadFraction = Mcr > 0 ? powerKw / Mcr : 0.0;
            double sfoc = SfocAtLoad(loadFraction);
            double dailyFuel = powerKw * sfoc * 24.0 / 1_000_000.0; // kW * g/kWh * h -> g, /1e6 -> t
            double range = dailyFuel > 0 ? (FuelCapacity / dailyFuel) * 24.0 * ShipSpeed : 0.0;

            FuelTable.Add(new EnginePowerPoint
            {
                Label = label,
                PowerKw = powerKw,
                LoadPercent = loadFraction * 100.0,
                SfocGPerKwh = sfoc,
                DailyFuelTonnes = dailyFuel,
                RangeNm = range
            });
        }

        private static double ComputeTierILimit(double n) =>
            n < 130 ? 17.0 : n < 2000 ? 45.0 * Math.Pow(n, -0.2) : 9.8;

        private static double ComputeTierIILimit(double n) =>
            n < 130 ? 14.4 : n < 2000 ? 44.0 * Math.Pow(n, -0.23) : 7.7;

        private static double ComputeTierIIILimit(double n) =>
            n < 130 ? 3.4 : n < 2000 ? 9.0 * Math.Pow(n, -0.2) : 2.0;

        private void RebuildCurve()
        {
            var curve = (LineSeries)SfocPlotModel.Series[0];
            curve.Points.Clear();

            if (Mcr > 0)
            {
                const int steps = 36;
                for (int i = 0; i <= steps; i++)
                {
                    double loadPct = 10.0 + (100.0 - 10.0) * i / steps;
                    curve.Points.Add(new DataPoint(loadPct, SfocAtLoad(loadPct / 100.0)));
                }
            }

            SetMarker(1, Csr, "CSR");
            SetMarker(2, Ncr, "NCR");
            SetMarker(3, Mcr, "MCR");

            SfocPlotModel.InvalidatePlot(true);
        }

        private void SetMarker(int seriesIndex, double powerKw, string _)
        {
            var series = (ScatterSeries)SfocPlotModel.Series[seriesIndex];
            series.Points.Clear();
            if (Mcr > 0 && powerKw > 0)
            {
                double loadPct = powerKw / Mcr * 100.0;
                series.Points.Add(new ScatterPoint(loadPct, SfocAtLoad(loadPct / 100.0)));
            }
        }

        private static PlotModel BuildPlotModel()
        {
            var gold = OxyColor.FromRgb(0xC8, 0x96, 0x0C);
            var teal = OxyColor.FromRgb(0x3D, 0xDC, 0x84);
            var red = OxyColor.FromRgb(0xE0, 0x52, 0x63);
            var sky = OxyColor.FromRgb(0x5B, 0x9B, 0xF2);
            var lightText = OxyColor.FromRgb(0xD7, 0xE1, 0xF0);
            var gridLine = OxyColor.FromArgb(60, 0x24, 0x46, 0x7F);
            var axisLine = OxyColor.FromRgb(0x24, 0x46, 0x7F);

            var model = new PlotModel
            {
                Background = OxyColor.FromRgb(0x12, 0x2A, 0x52),
                PlotAreaBorderColor = axisLine,
                TextColor = lightText,
                DefaultFontSize = 12,
                IsLegendVisible = true
            };

            model.Legends.Add(new OxyPlot.Legends.Legend
            {
                LegendPosition = OxyPlot.Legends.LegendPosition.TopLeft,
                LegendTextColor = lightText,
                LegendBackground = OxyColor.FromArgb(0, 0, 0, 0),
                LegendBorderThickness = 0
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Load (% MCR)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine,
                Minimum = 0,
                Maximum = 105
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "SFOC (g/kWh)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine
            });

            model.Series.Add(new LineSeries { Title = "SFOC curve", Color = gold, StrokeThickness = 2.75 });
            model.Series.Add(new ScatterSeries { Title = "CSR", MarkerType = MarkerType.Diamond, MarkerFill = teal, MarkerSize = 7 });
            model.Series.Add(new ScatterSeries { Title = "NCR", MarkerType = MarkerType.Square, MarkerFill = sky, MarkerSize = 7 });
            model.Series.Add(new ScatterSeries { Title = "MCR", MarkerType = MarkerType.Circle, MarkerFill = red, MarkerSize = 7 });

            return model;
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string? propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
