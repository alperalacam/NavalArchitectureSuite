using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NavalArchitectureSuite.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Preliminary sailing-yacht design calculator for the Yacht Design module.
    ///
    /// Scope and simplifications:
    ///   - This module works from classic naval-architecture *empirical ratios*
    ///     (Brewer / USSA / Delft-style rules of thumb) rather than a full
    ///     velocity-prediction-program (VPP) force-balance solver. It is a
    ///     screening / teaching tool, not a design-office VPP.
    ///   - Ballast Ratio, Displacement/Length Ratio (DLR), Sail Area/Displacement
    ///     Ratio (SADR), the Capsize Screening Formula (CSF) and Ted Brewer's
    ///     Comfort Ratio all use their standard published imperial-unit formulas;
    ///     inputs are taken in SI and converted internally (ft, lb, long tons)
    ///     so the constants match the historical definitions exactly.
    ///   - GM is *not* derived from hull form here (that needs full hydrostatics,
    ///     see the Hydrostatics/Stability modules) — it is taken as a direct,
    ///     user-supplied estimate (e.g. from the Stability module or an
    ///     inclining test) and used only to illustrate a small-angle righting
    ///     vs. heeling moment balance at a reference wind speed.
    ///   - The heeling-moment estimate uses a simple flat-plate aerodynamic
    ///     model (q·A·Cd acting at the sail-plan centre of effort) — it ignores
    ///     the reduction in effective heeling arm with heel angle, twist,
    ///     reefing, and the aerodynamic lift/drag polar of real sails.
    ///   - Dellenbaugh Angle: the small-angle-extrapolated steady heel angle under
    ///     the reference wind, DA[deg] = HeelingMoment / RightingMomentPer1Degree
    ///     (RightingMomentKNm here is already the moment at exactly 1° heel, so
    ///     dividing the heeling moment by it yields degrees directly). A widely
    ///     used quick screening figure; commonly cited guidance targets DA ≲ 15°
    ///     for a comfortable cruising boat. Like the heeling-moment estimate
    ///     above, it is a linear (small-angle) approximation and does not account
    ///     for the reduction in GZ growth rate at larger heel angles.
    ///   - The "VPP" polar is a simplified empirical multiplier applied to hull
    ///     speed (Froude-style, V = 1.34·√LWL(ft)) across true wind angle (TWA),
    ///     scaled by a coarse wind-speed factor. It reproduces the qualitative
    ///     shape of a real polar (soft upwind, fast reaching, easing off dead
    ///     downwind) but is not a substitute for a real VPP or full-scale polar
    ///     trial data.
    ///   - MCA Code of Practice / EU RCD 2013/53/EU design categories are based
    ///     on the STIX (Stability Index) formula, which needs a full righting-arm
    ///     curve to large angles of heel. STIX is not computed here — CSF and
    ///     Comfort Ratio are provided only as commonly used preliminary
    ///     screening ratios.
    /// </summary>
    public class YachtDesignViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const double AirDensity = 1.225;   // kg/m^3
        private const double Gravity = 9.81;       // m/s^2
        private const double SailCd = 1.2;         // approximate flat-plate drag coefficient for heeling-force estimate
        private const double KtsToMs = 0.5144;

        // Imperial conversion constants used so the classic-formula constants match their historical definitions.
        private const double MetresToFeet = 3.2808;
        private const double KgToLongTons = 1016.05;
        private const double KgToLb = 2.20462;
        private const double SeawaterDensityKgM3 = 1025.0;
        private const double M3ToFt3 = 35.3147;
        private const double M2ToFt2 = 10.7639;

        #region Principal particulars

        private double _lwl = 11.5;
        public double Lwl { get => _lwl; set { if (SetField(ref _lwl, value)) Recalculate(); } }

        private double _loa = 13.8;
        public double Loa { get => _loa; set { if (SetField(ref _loa, value)) Recalculate(); } }

        private double _bwl = 3.9;
        public double Bwl { get => _bwl; set { if (SetField(ref _bwl, value)) Recalculate(); } }

        private double _displacement = 9500.0;
        public double Displacement { get => _displacement; set { if (SetField(ref _displacement, value)) Recalculate(); } }

        private double _ballastMass = 3800.0;
        public double BallastMass { get => _ballastMass; set { if (SetField(ref _ballastMass, value)) Recalculate(); } }

        private double _draft = 2.1;
        public double Draft { get => _draft; set { if (SetField(ref _draft, value)) Recalculate(); } }

        private double _vcg = 0.35;
        public double Vcg { get => _vcg; set { if (SetField(ref _vcg, value)) Recalculate(); } }

        #endregion

        #region Sail plan

        private double _sailAreaUpwind = 78.0;
        public double SailAreaUpwind { get => _sailAreaUpwind; set { if (SetField(ref _sailAreaUpwind, value)) Recalculate(); } }

        private double _sailAreaDownwind = 145.0;
        public double SailAreaDownwind { get => _sailAreaDownwind; set { if (SetField(ref _sailAreaDownwind, value)) Recalculate(); } }

        private double _coeHeight = 6.2;
        public double CoeHeight { get => _coeHeight; set { if (SetField(ref _coeHeight, value)) Recalculate(); } }

        #endregion

        #region GM / wind reference inputs

        private double _gmEstimate = 1.15;
        public double GmEstimate { get => _gmEstimate; set { if (SetField(ref _gmEstimate, value)) Recalculate(); } }

        private double _referenceWindSpeedKts = 20.0;
        public double ReferenceWindSpeedKts { get => _referenceWindSpeedKts; set { if (SetField(ref _referenceWindSpeedKts, value)) Recalculate(); } }

        private double _polarWindSpeedKts = 12.0;
        public double PolarWindSpeedKts { get => _polarWindSpeedKts; set { if (SetField(ref _polarWindSpeedKts, value)) Recalculate(); } }

        #endregion

        #region Derived ratios / outputs

        private double _ballastRatio;
        public double BallastRatio { get => _ballastRatio; private set => SetField(ref _ballastRatio, value); }

        private double _dlr;
        public double Dlr { get => _dlr; private set => SetField(ref _dlr, value); }

        private double _sadr;
        public double Sadr { get => _sadr; private set => SetField(ref _sadr, value); }

        private double _csf;
        public double Csf { get => _csf; private set => SetField(ref _csf, value); }

        private double _comfortRatio;
        public double ComfortRatio { get => _comfortRatio; private set => SetField(ref _comfortRatio, value); }

        public string ComfortBand
        {
            get
            {
                if (ComfortRatio < 20.0) return "Lively / racy motion";
                if (ComfortRatio < 30.0) return "Moderate cruiser";
                if (ComfortRatio <= 40.0) return "Offshore cruiser";
                return "Heavy bluewater";
            }
        }

        private double _hullSpeedKts;
        public double HullSpeedKts { get => _hullSpeedKts; private set => SetField(ref _hullSpeedKts, value); }

        private double _rightingMomentKNm;
        public double RightingMomentKNm { get => _rightingMomentKNm; private set => SetField(ref _rightingMomentKNm, value); }

        private double _heelingMomentKNm;
        public double HeelingMomentKNm { get => _heelingMomentKNm; private set => SetField(ref _heelingMomentKNm, value); }

        private double _stabilityMargin;
        public double StabilityMargin { get => _stabilityMargin; private set => SetField(ref _stabilityMargin, value); }

        private double _dellenbaughAngleDeg;
        /// <summary>Small-angle-extrapolated steady heel angle under the reference wind (deg) = HeelingMoment / RightingMomentPer1Degree.</summary>
        public double DellenbaughAngleDeg { get => _dellenbaughAngleDeg; private set => SetField(ref _dellenbaughAngleDeg, value); }

        public ObservableCollection<HydrostaticsCriterion> Criteria { get; } = new();

        #endregion

        #region VPP polar

        public ObservableCollection<PolarPoint> Polar { get; } = new();

        public PlotModel PolarPlotModel { get; }

        // Classic qualitative polar shape: efficiency of hull speed achieved at each true wind angle,
        // referenced to a 12 kt true wind speed. Endpoints at 0 deg (dead upwind, no forward VMG)
        // and 180 deg (dead downwind) are added purely to close the curve smoothly for the chart;
        // the 11 interior points (30 deg..170 deg) are the values specified for the module.
        private static readonly (double twa, double mult)[] PolarShape =
        {
            (0,   0.00),
            (30,  0.45),
            (40,  0.62),
            (50,  0.72),
            (60,  0.80),
            (70,  0.86),
            (80,  0.92),
            (90,  0.98),
            (110, 1.00),
            (130, 0.92),
            (150, 0.78),
            (170, 0.68),
            (180, 0.65)
        };

        private static readonly double[] PolarTableAngles = { 30, 40, 50, 60, 70, 80, 90, 110, 130, 150, 170 };

        #endregion

        public YachtDesignViewModel()
        {
            PolarPlotModel = BuildPlotModel();
            Recalculate();
        }

        private static double InterpolatePolar(double twa)
        {
            if (twa <= PolarShape[0].twa) return PolarShape[0].mult;
            if (twa >= PolarShape[^1].twa) return PolarShape[^1].mult;
            for (int i = 0; i < PolarShape.Length - 1; i++)
            {
                var (x0, y0) = PolarShape[i];
                var (x1, y1) = PolarShape[i + 1];
                if (twa >= x0 && twa <= x1)
                {
                    double t = (x1 > x0) ? (twa - x0) / (x1 - x0) : 0.0;
                    return y0 + t * (y1 - y0);
                }
            }
            return PolarShape[^1].mult;
        }

        private void Recalculate()
        {
            bool valid = Lwl > 0 && Loa > 0 && Bwl > 0 && Displacement > 0;

            BallastRatio = Displacement > 0 ? BallastMass / Displacement * 100.0 : 0.0;

            double lwlFt = Lwl * MetresToFeet;
            double loaFt = Loa * MetresToFeet;
            double beamFt = Bwl * MetresToFeet;
            double dispLongTons = Displacement / KgToLongTons;
            double dispLb = Displacement * KgToLb;
            double dispFt3 = (Displacement / SeawaterDensityKgM3) * M3ToFt3;
            double saFt2 = SailAreaUpwind * M2ToFt2;

            Dlr = valid ? dispLongTons / Math.Pow(0.01 * lwlFt, 3) : 0.0;
            Sadr = (valid && dispFt3 > 0) ? saFt2 / Math.Pow(dispFt3, 2.0 / 3.0) : 0.0;
            Csf = (valid && dispLb > 0) ? beamFt / Math.Pow(dispLb / 64.0, 1.0 / 3.0) : 0.0;
            ComfortRatio = (valid && beamFt > 0)
                ? dispLb / (0.65 * (0.7 * lwlFt + 0.3 * loaFt) * Math.Pow(beamFt, 1.33))
                : 0.0;
            OnPropertyChanged(nameof(ComfortBand));

            HullSpeedKts = valid ? 1.34 * Math.Sqrt(lwlFt) : 0.0;

            RightingMomentKNm = (Displacement / 1000.0) * Gravity * GmEstimate * Math.Sin(1.0 * Math.PI / 180.0);

            double windMs = ReferenceWindSpeedKts * KtsToMs;
            double q = 0.5 * AirDensity * windMs * windMs;
            double heelingForceN = q * SailAreaUpwind * SailCd;
            HeelingMomentKNm = heelingForceN * CoeHeight / 1000.0;

            StabilityMargin = HeelingMomentKNm > 0 ? RightingMomentKNm / HeelingMomentKNm : 0.0;
            DellenbaughAngleDeg = RightingMomentKNm > 0 ? HeelingMomentKNm / RightingMomentKNm : 0.0;

            RebuildCriteria();
            RebuildPolar();
        }

        private void RebuildCriteria()
        {
            Criteria.Clear();

            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "Capsize Screening Formula (CSF)",
                Requirement = "≤ 2.0 (offshore)",
                ActualText = $"{Csf:F2}",
                IsPass = Csf <= 2.0
            });

            Criteria.Add(new HydrostaticsCriterion
            {
                Name = $"Righting vs Heeling Moment @ {ReferenceWindSpeedKts:F0} kts",
                Requirement = "Margin ≥ 1.0",
                ActualText = $"{StabilityMargin:F2}",
                IsPass = StabilityMargin >= 1.0
            });

            Criteria.Add(new HydrostaticsCriterion
            {
                Name = $"Dellenbaugh Angle @ {ReferenceWindSpeedKts:F0} kts",
                Requirement = "≲ 15°",
                ActualText = $"{DellenbaughAngleDeg:F1}°",
                IsPass = DellenbaughAngleDeg <= 15.0
            });
        }

        private void RebuildPolar()
        {
            Polar.Clear();
            double windFactor = Math.Clamp(PolarWindSpeedKts / 12.0, 0.5, 1.15);
            double cap = HullSpeedKts * 1.05;

            foreach (var twa in PolarTableAngles)
            {
                double mult = InterpolatePolar(twa);
                double speed = Math.Min(HullSpeedKts * mult * windFactor, cap);
                Polar.Add(new PolarPoint { TrueWindAngleDeg = twa, BoatSpeedKts = speed });
            }

            var series = (LineSeries)PolarPlotModel.Series[0];
            series.Points.Clear();
            for (double twa = 0; twa <= 180; twa += 2.0)
            {
                double mult = InterpolatePolar(twa);
                double speed = Math.Min(HullSpeedKts * mult * windFactor, cap);
                series.Points.Add(new DataPoint(twa, speed));
            }

            PolarPlotModel.InvalidatePlot(true);
        }

        private static PlotModel BuildPlotModel()
        {
            var gold = OxyColor.FromRgb(0xC8, 0x96, 0x0C);
            var lightText = OxyColor.FromRgb(0xD7, 0xE1, 0xF0);
            var gridLine = OxyColor.FromArgb(60, 0x24, 0x46, 0x7F);
            var axisLine = OxyColor.FromRgb(0x24, 0x46, 0x7F);

            var model = new PlotModel
            {
                Background = OxyColor.FromRgb(0x12, 0x2A, 0x52),
                PlotAreaBorderColor = axisLine,
                TextColor = lightText,
                DefaultFontSize = 12
            };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "True Wind Angle (deg)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine,
                Minimum = 0,
                Maximum = 180
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Boat Speed (kts)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine,
                Minimum = 0
            });

            model.Series.Add(new LineSeries
            {
                Color = gold,
                StrokeThickness = 2.5,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = gold
            });

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
