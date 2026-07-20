using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Calm-water resistance and propulsive power prediction using the
    /// Holtrop-Mennen (1984) regression method ("An Approximate Power
    /// Prediction Method", Intl. Shipbuilding Progress).
    ///
    /// Only principal particulars are exposed as inputs, so the coefficients
    /// the full method normally takes as separate inputs are estimated from
    /// Cb with standard preliminary-design approximations (Schneekluth):
    ///   Cm  = 0.977 + 0.085*(Cb-0.60)
    ///   Cwp = (1 + 2*Cb) / 3
    /// and the hull is assumed to have no bulbous bow, no immersed transom
    /// and LCB at midships, which drops the c2/c3/c5 bow/transom terms from
    /// the wave-resistance term (c2=c5=1, c3=0) and the appendage/bulb terms
    /// from resistance and wetted-surface area entirely. The wave-resistance
    /// exponential is the low/moderate Froude-number form (valid to Fn~0.45,
    /// i.e. most displacement hulls). Propulsive efficiency uses a simplified
    /// single-screw wake/thrust-deduction estimate (w = 0.5*Cb - 0.05,
    /// t = 0.6*w) with a fixed assumed open-water propeller efficiency, since
    /// no full propeller-series (B-series/Ka) geometry is modeled.
    ///
    /// Propeller diameter uses the standard draft-limited preliminary sizing
    /// rule for a single screw, D = 0.7*T. Pitch ratio follows from the real
    /// slip relation P = Va / (n*(1-slip)), with Va = V*(1-w) the speed of
    /// advance, n the shaft speed and slip an assumed design slip ratio —
    /// both are preliminary-design estimates, not a Bp-delta / KT-KQ chart
    /// solution.
    /// </summary>
    public class ResistancePropulsionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const double SeawaterDensity = 1025.0;   // kg/m^3
        private const double DisplacementDensity = 1.025; // t/m^3
        private const double Gravity = 9.80665;           // m/s^2
        private const double KinematicViscosity = 1.19e-6; // m^2/s, seawater ~15 C
        private const double KnotsToMs = 0.514444;

        private const double OpenWaterEfficiency = 0.62; // assumed, no propeller design modeled
        private const double RelativeRotativeEfficiency = 1.01; // single screw
        private const double ShaftEfficiency = 0.98;
        private const double GearboxEfficiency = 0.97;

        #region Inputs

        private double _lwl = 120.0;
        public double Lwl { get => _lwl; set { if (SetField(ref _lwl, value)) Recalculate(); } }

        private double _lbp = 118.0;
        public double Lbp { get => _lbp; set { if (SetField(ref _lbp, value)) Recalculate(); } }

        private double _bwl = 20.0;
        public double Bwl { get => _bwl; set { if (SetField(ref _bwl, value)) Recalculate(); } }

        private double _draft = 7.0;
        public double Draft { get => _draft; set { if (SetField(ref _draft, value)) Recalculate(); } }

        private double _cb = 0.68;
        public double Cb { get => _cb; set { if (SetField(ref _cb, value)) Recalculate(); } }

        private double _displacement = 9500.0;
        public double Displacement { get => _displacement; set { if (SetField(ref _displacement, value)) Recalculate(); } }

        private double _speedMin = 8.0;
        public double SpeedMin { get => _speedMin; set { if (SetField(ref _speedMin, value)) Recalculate(); } }

        private double _speedMax = 18.0;
        public double SpeedMax { get => _speedMax; set { if (SetField(ref _speedMax, value)) Recalculate(); } }

        private double _shaftRpm = 110.0;
        public double ShaftRpm { get => _shaftRpm; set { if (SetField(ref _shaftRpm, value)) Recalculate(); } }

        private double _designSlip = 0.25;
        public double DesignSlip { get => _designSlip; set { if (SetField(ref _designSlip, value)) Recalculate(); } }

        #endregion

        #region Derived / outputs

        private double _cm;
        public double Cm { get => _cm; private set => SetField(ref _cm, value); }

        private double _cp;
        public double Cp { get => _cp; private set => SetField(ref _cp, value); }

        private double _cwp;
        public double Cwp { get => _cwp; private set => SetField(ref _cwp, value); }

        private double _volumeDisplacement;
        public double VolumeDisplacement { get => _volumeDisplacement; private set => SetField(ref _volumeDisplacement, value); }

        private double _wettedSurfaceArea;
        public double WettedSurfaceArea { get => _wettedSurfaceArea; private set => SetField(ref _wettedSurfaceArea, value); }

        private double _formFactor;
        public double FormFactor { get => _formFactor; private set => SetField(ref _formFactor, value); }

        private double _wakeFraction;
        public double WakeFraction { get => _wakeFraction; private set => SetField(ref _wakeFraction, value); }

        private double _thrustDeduction;
        public double ThrustDeduction { get => _thrustDeduction; private set => SetField(ref _thrustDeduction, value); }

        private double _propulsiveCoefficient;
        public double PropulsiveCoefficient { get => _propulsiveCoefficient; private set => SetField(ref _propulsiveCoefficient, value); }

        private double _propellerDiameter;
        public double PropellerDiameter { get => _propellerDiameter; private set => SetField(ref _propellerDiameter, value); }

        private double _pitchRatio;
        public double PitchRatio { get => _pitchRatio; private set => SetField(ref _pitchRatio, value); }

        private double _froudeNumberMax;
        public double FroudeNumberMax { get => _froudeNumberMax; private set => SetField(ref _froudeNumberMax, value); }

        // Design-point (V = SpeedMax) results
        private double _rtDesign;
        public double RtDesign { get => _rtDesign; private set => SetField(ref _rtDesign, value); }

        private double _rfDesign;
        public double RfDesign { get => _rfDesign; private set => SetField(ref _rfDesign, value); }

        private double _rrDesign;
        public double RrDesign { get => _rrDesign; private set => SetField(ref _rrDesign, value); }

        private double _ehp;
        public double Ehp { get => _ehp; private set => SetField(ref _ehp, value); }

        private double _dhp;
        public double Dhp { get => _dhp; private set => SetField(ref _dhp, value); }

        private double _shp;
        public double Shp { get => _shp; private set => SetField(ref _shp, value); }

        private double _bhp;
        public double Bhp { get => _bhp; private set => SetField(ref _bhp, value); }

        public PlotModel ResistancePlotModel { get; }

        #endregion

        public ResistancePropulsionViewModel()
        {
            ResistancePlotModel = BuildPlotModel();
            Recalculate();
        }

        private (double rt, double rf, double rr) ResistanceAt(double speedKn)
        {
            double v = speedKn * KnotsToMs;
            if (v <= 0 || Lwl <= 0 || Bwl <= 0 || Draft <= 0 || Cb <= 0 || VolumeDisplacement <= 0)
                return (0, 0, 0);

            double l = Lwl;
            double b = Bwl;
            double t = Draft;
            double cb = Cb;
            double cm = Cm;
            double cp = Cp;
            double cwp = Cwp;
            double nabla = VolumeDisplacement;
            const double lcb = 0.0; // assumed at midships, % L

            // --- Frictional resistance (ITTC-1957 line + form factor) ---
            double rn = v * l / KinematicViscosity;
            double cf = 0.075 / Math.Pow(Math.Log10(rn) - 2.0, 2);
            double rF = 0.5 * SeawaterDensity * v * v * WettedSurfaceArea * cf;
            double rFForm = rF * FormFactor;

            // --- Wave-making resistance ---
            double fn = v / Math.Sqrt(Gravity * l);
            double lr = l * (1 - cp + 0.06 * cp * lcb / (4 * cp - 1));

            double bl = b / l;
            double c7 = bl < 0.11 ? 0.229577 * Math.Pow(bl, 1.0 / 3.0)
                      : bl <= 0.25 ? bl
                      : 0.5 - 0.0625 * (l / b);

            double iE = 1 + 89 * Math.Exp(
                -Math.Pow(l / b, 0.80856) *
                Math.Pow(1 - cwp, 0.30484) *
                Math.Pow(1 - cp - 0.0225 * lcb, 0.6367) *
                Math.Pow(lr / b, 0.34574) *
                Math.Pow(100 * nabla / Math.Pow(l, 3), 0.16302));

            double c1 = 2223105.0 * Math.Pow(c7, 3.78613) * Math.Pow(t / b, 1.07961) * Math.Pow(90 - iE, -1.37565);

            double c16 = cp < 0.8
                ? 8.07981 * cp - 13.8673 * cp * cp + 6.984388 * cp * cp * cp
                : 1.73014 - 0.7067 * cp;

            double m1 = 0.0140407 * (l / t) - 1.75254 * (Math.Cbrt(nabla) / l) - 4.79323 * bl - c16;

            double l3nabla = Math.Pow(l, 3) / nabla;
            double c15 = l3nabla < 512 ? -1.69385
                       : l3nabla > 1727 ? 0.0
                       : (l / Math.Cbrt(nabla) - 8.0) / 2.36;

            double lb = l / b;
            double lambda = lb < 12 ? 1.446 * cp - 0.03 * lb : 1.446 * cp - 0.36;

            double m4 = c15 * 0.4 * Math.Exp(-0.034 * Math.Pow(fn, -3.29));

            double rW = c1 * nabla * SeawaterDensity * Gravity *
                        Math.Exp(m1 * Math.Pow(fn, -0.9) + m4 * Math.Cos(lambda * Math.Pow(fn, -2)));
            if (double.IsNaN(rW) || double.IsInfinity(rW)) rW = 0;

            // --- Model-ship correlation allowance ---
            double c4 = Math.Min(t / l, 0.04);
            double ca = 0.006 * Math.Pow(l + 100, -0.16) - 0.00205 +
                        0.003 * Math.Sqrt(l / 7.5) * Math.Pow(cb, 4) * (0.04 - c4);
            double rA = 0.5 * SeawaterDensity * v * v * WettedSurfaceArea * ca;

            double rf = rFForm / 1000.0;       // kN, frictional incl. form factor
            double rr = (rW + rA) / 1000.0;    // kN, wave + correlation allowance
            double rt = rf + rr;

            return (rt, rf, rr);
        }

        private void Recalculate()
        {
            bool valid = Lwl > 0 && Bwl > 0 && Draft > 0 && Cb > 0 && Cb < 1 && Displacement > 0 && SpeedMax > SpeedMin;

            VolumeDisplacement = Displacement / DisplacementDensity;

            Cm = Math.Clamp(0.977 + 0.085 * (Cb - 0.60), 0.85, 0.99);
            Cp = Cm > 0 ? Cb / Cm : 0.0;
            Cwp = (1 + 2 * Cb) / 3.0;

            if (!valid)
            {
                WettedSurfaceArea = 0;
                FormFactor = 0;
                RtDesign = RfDesign = RrDesign = 0;
                Ehp = Dhp = Shp = Bhp = 0;
                PropellerDiameter = PitchRatio = 0;
                ClearSeries();
                return;
            }

            WettedSurfaceArea = Lwl * (2 * Draft + Bwl) * Math.Sqrt(Cm) *
                (0.453 + 0.4425 * Cb - 0.2862 * Cm - 0.003467 * (Bwl / Draft) + 0.3696 * Cwp);

            const double lcb = 0.0;
            double lr = Lwl * (1 - Cp + 0.06 * Cp * lcb / (4 * Cp - 1));
            FormFactor = 0.93 + 0.487118 *
                Math.Pow(Bwl / Lwl, 1.06806) *
                Math.Pow(Draft / Lwl, 0.46106) *
                Math.Pow(Lwl / lr, 0.121563) *
                Math.Pow(Math.Pow(Lwl, 3) / VolumeDisplacement, 0.36486) *
                Math.Pow(1 - Cp, -0.604247);

            FroudeNumberMax = (SpeedMax * KnotsToMs) / Math.Sqrt(Gravity * Lwl);

            WakeFraction = Math.Clamp(0.5 * Cb - 0.05, 0.05, 0.45);
            ThrustDeduction = 0.6 * WakeFraction;
            double hullEfficiency = (1 - ThrustDeduction) / (1 - WakeFraction);
            PropulsiveCoefficient = hullEfficiency * OpenWaterEfficiency * RelativeRotativeEfficiency;

            PropellerDiameter = 0.7 * Draft;
            double vaDesign = (SpeedMax * KnotsToMs) * (1 - WakeFraction);
            double nRevPerSec = ShaftRpm / 60.0;
            double slip = Math.Clamp(DesignSlip, 0.0, 0.9);
            double pitch = (nRevPerSec > 0 && PropellerDiameter > 0) ? vaDesign / (nRevPerSec * (1 - slip)) : 0.0;
            PitchRatio = PropellerDiameter > 0 ? pitch / PropellerDiameter : 0.0;

            var (rt, rf, rr) = ResistanceAt(SpeedMax);
            RtDesign = rt;
            RfDesign = rf;
            RrDesign = rr;

            Ehp = rt * (SpeedMax * KnotsToMs); // kW
            Dhp = PropulsiveCoefficient > 0 ? Ehp / PropulsiveCoefficient : 0;
            Shp = Dhp / ShaftEfficiency;
            Bhp = Shp / GearboxEfficiency;

            RebuildCurve();
        }

        private void ClearSeries()
        {
            foreach (var series in ResistancePlotModel.Series)
                ((LineSeries)series).Points.Clear();
            ResistancePlotModel.InvalidatePlot(true);
        }

        private void RebuildCurve()
        {
            var rtSeries = (LineSeries)ResistancePlotModel.Series[0];
            var rfSeries = (LineSeries)ResistancePlotModel.Series[1];
            var rrSeries = (LineSeries)ResistancePlotModel.Series[2];

            rtSeries.Points.Clear();
            rfSeries.Points.Clear();
            rrSeries.Points.Clear();

            const int steps = 24;
            for (int i = 0; i <= steps; i++)
            {
                double v = SpeedMin + (SpeedMax - SpeedMin) * i / steps;
                var (rt, rf, rr) = ResistanceAt(v);
                rtSeries.Points.Add(new DataPoint(v, rt));
                rfSeries.Points.Add(new DataPoint(v, rf));
                rrSeries.Points.Add(new DataPoint(v, rr));
            }

            ResistancePlotModel.InvalidatePlot(true);
        }

        private static PlotModel BuildPlotModel()
        {
            var gold = OxyColor.FromRgb(0xC8, 0x96, 0x0C);
            var red = OxyColor.FromRgb(0xE0, 0x52, 0x63);
            var teal = OxyColor.FromRgb(0x3D, 0xDC, 0x84);
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
                Title = "Speed (kn)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Resistance (kN)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine,
                Minimum = 0
            });

            model.Series.Add(new LineSeries { Title = "Rt (total)", Color = gold, StrokeThickness = 2.75 });
            model.Series.Add(new LineSeries { Title = "Rf (frictional)", Color = teal, StrokeThickness = 2, LineStyle = LineStyle.Dash });
            model.Series.Add(new LineSeries { Title = "Rr (residuary)", Color = red, StrokeThickness = 2, LineStyle = LineStyle.Dash });

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
