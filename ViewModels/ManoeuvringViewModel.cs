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
    /// Manoeuvring calculation engine for the Manoeuvring module.
    ///
    /// Scope and simplifications:
    ///   This module uses the LINEAR FIRST-ORDER NOMOTO K-T MODEL
    ///   (dr/dt = (K·δ − r) / T) together with a simple parametric regression
    ///   for the non-dimensional gain K' and time constant T' calibrated to
    ///   give plausible merchant-ship orders of magnitude. It is adequate for
    ///   teaching-level turning-circle and zig-zag prediction — showing how
    ///   hull proportions (B/L, Cb) and rudder size trade off against
    ///   manoeuvrability — but it is NOT a substitute for a full 4-DOF/6-DOF
    ///   MMG-type simulation, a full IMO Res. MSC.137(76) trial matrix, or
    ///   model-basin captive/free-running tests. Notable simplifications:
    ///     - Speed loss during the turn is not modelled (V held constant).
    ///     - Second-order (K, T1, T2) Nomoto and rudder-rate/rudder-engine
    ///       dynamics are not modelled — the rudder is assumed to move
    ///       instantaneously to its commanded angle.
    ///     - The IMO overshoot-angle acceptance table (which varies with
    ///       L/V) is replaced by a single simplified 20° threshold.
    ///   Turning-circle and zig-zag trajectories are produced by simple
    ///   forward-Euler integration of the Nomoto heading-rate ODE plus a
    ///   kinematic position integrator.
    /// </summary>
    public class ManoeuvringViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const double KtsToMs = 0.5144;
        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;

        #region Principal particulars & rudder inputs

        private double _lpp = 180.0;
        public double Lpp { get => _lpp; set { if (SetField(ref _lpp, value)) Recalculate(); } }

        private double _bwl = 30.0;
        public double Bwl { get => _bwl; set { if (SetField(ref _bwl, value)) Recalculate(); } }

        private double _draft = 11.0;
        public double Draft { get => _draft; set { if (SetField(ref _draft, value)) Recalculate(); } }

        private double _cb = 0.70;
        public double Cb { get => _cb; set { if (SetField(ref _cb, value)) Recalculate(); } }

        private double _serviceSpeedKts = 15.0;
        public double ServiceSpeedKts { get => _serviceSpeedKts; set { if (SetField(ref _serviceSpeedKts, value)) Recalculate(); } }

        private double _rudderAngleDeg = 35.0;
        public double RudderAngleDeg { get => _rudderAngleDeg; set { if (SetField(ref _rudderAngleDeg, value)) Recalculate(); } }

        private double _rudderAreaRatio = 0.018;
        public double RudderAreaRatio { get => _rudderAreaRatio; set { if (SetField(ref _rudderAreaRatio, value)) Recalculate(); } }

        #endregion

        #region Zig-zag test parameters

        private double _zigZagRudderAngleDeg = 10.0;
        public double ZigZagRudderAngleDeg { get => _zigZagRudderAngleDeg; set { if (SetField(ref _zigZagRudderAngleDeg, value)) Recalculate(); } }

        private double _zigZagHeadingAngleDeg = 10.0;
        public double ZigZagHeadingAngleDeg { get => _zigZagHeadingAngleDeg; set { if (SetField(ref _zigZagHeadingAngleDeg, value)) Recalculate(); } }

        #endregion

        #region Derived / regression outputs

        private double _arM2;
        public double ArM2 { get => _arM2; private set => SetField(ref _arM2, value); }

        private double _arMin;
        public double ArMin { get => _arMin; private set => SetField(ref _arMin, value); }

        private double _kDim;
        public double KDim { get => _kDim; private set => SetField(ref _kDim, value); }

        private double _tDim;
        public double TDim { get => _tDim; private set => SetField(ref _tDim, value); }

        #endregion

        #region Turning circle outputs

        private double _advance;
        public double Advance { get => _advance; private set => SetField(ref _advance, value); }

        private double _tacticalDiameter;
        public double TacticalDiameter { get => _tacticalDiameter; private set => SetField(ref _tacticalDiameter, value); }

        private double _steadyTurningDiameter;
        public double SteadyTurningDiameter { get => _steadyTurningDiameter; private set => SetField(ref _steadyTurningDiameter, value); }

        public PlotModel TurningCirclePlotModel { get; }

        #endregion

        #region Zig-zag outputs

        private double _firstOvershootAngleDeg;
        public double FirstOvershootAngleDeg { get => _firstOvershootAngleDeg; private set => SetField(ref _firstOvershootAngleDeg, value); }

        public PlotModel ZigZagPlotModel { get; }

        #endregion

        public ObservableCollection<HydrostaticsCriterion> RudderCriteria { get; } = new();
        public ObservableCollection<HydrostaticsCriterion> TurningCriteria { get; } = new();
        public ObservableCollection<HydrostaticsCriterion> ZigZagCriteria { get; } = new();

        public ManoeuvringViewModel()
        {
            TurningCirclePlotModel = BuildTurningCirclePlotModel();
            ZigZagPlotModel = BuildZigZagPlotModel();

            // Subscribe to the shared ShipBuilder singleton so that changing principal
            // particulars in Ship Builder immediately updates Manoeuvring inputs.
            SyncFromShipBuilder();
            ShipBuilderViewModel.Instance.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ShipBuilderViewModel.Lpp):
                    case nameof(ShipBuilderViewModel.Breadth):
                    case nameof(ShipBuilderViewModel.Draft):
                    case nameof(ShipBuilderViewModel.Cb):
                    case nameof(ShipBuilderViewModel.DesignSpeed):
                        SyncFromShipBuilder();
                        break;
                }
            };
        }

        private void SyncFromShipBuilder()
        {
            var sb = ShipBuilderViewModel.Instance;
            _lpp = sb.Lpp;
            _bwl = sb.Breadth;
            _draft = sb.Draft;
            _cb = sb.Cb;
            _serviceSpeedKts = sb.DesignSpeed;

            // Fire property-changed for all synced inputs so the UI reflects the new values.
            OnPropertyChanged(nameof(Lpp));
            OnPropertyChanged(nameof(Bwl));
            OnPropertyChanged(nameof(Draft));
            OnPropertyChanged(nameof(Cb));
            OnPropertyChanged(nameof(ServiceSpeedKts));

            Recalculate();
        }

        private void Recalculate()
        {
            bool valid = Lpp > 0 && Bwl > 0 && Draft > 0 && Cb > 0 && ServiceSpeedKts > 0;

            // ---- Rudder area (informational) ----
            ArM2 = valid ? RudderAreaRatio * Lpp * Draft : 0.0;

            // Simplified DNV-style minimum recommended rudder area.
            ArMin = valid ? (Lpp * Draft / 100.0) * (1.0 + 25.0 * Math.Pow(Bwl / Lpp, 2)) : 0.0;

            RudderCriteria.Clear();
            RudderCriteria.Add(new HydrostaticsCriterion
            {
                Name = "Rudder Area",
                Requirement = $"≥ A_R,min = {ArMin:F2} m²",
                ActualText = $"{ArM2:F2} m²",
                IsPass = valid && ArM2 >= ArMin
            });

            if (!valid)
            {
                KDim = TDim = 0.0;
                Advance = TacticalDiameter = SteadyTurningDiameter = 0.0;
                FirstOvershootAngleDeg = 0.0;
                ClearSeries(TurningCirclePlotModel);
                ClearSeries(ZigZagPlotModel);
                RebuildTurningCriteria();
                RebuildZigZagCriteria();
                return;
            }

            // ---- Nomoto K, T regression (simplified parametric stand-in; see class summary) ----
            double vMs = ServiceSpeedKts * KtsToMs;
            double bOverL = Bwl / Lpp;
            double kPrime = 1.0 / (1.0 + 2.0 * Cb * bOverL);
            double tPrime = 1.0 / (0.5 + 2.0 * bOverL);
            KDim = kPrime * vMs / Lpp;   // 1/s per rad rudder
            TDim = tPrime * Lpp / vMs;   // s

            SimulateTurningCircle(vMs);
            SimulateZigZag(vMs);

            RebuildTurningCriteria();
            RebuildZigZagCriteria();
        }

        /// <summary>
        /// Forward-Euler integration of the first-order Nomoto response to a step
        /// rudder input, plus a kinematic position integrator (speed held constant —
        /// speed loss in the turn is not modelled). Records the trajectory and
        /// extracts Advance (X at 90° heading change) and Tactical Diameter
        /// (Y transfer at 180° heading change) by linear interpolation between the
        /// two samples straddling the target heading change.
        /// </summary>
        private void SimulateTurningCircle(double vMs)
        {
            var series = (LineSeries)TurningCirclePlotModel.Series[0];
            series.Points.Clear();

            double rudderRad = RudderAngleDeg * DegToRad;
            double heading = 0.0;
            double r = 0.0;
            double x = 0.0, y = 0.0;
            const double dt = 0.6;
            const int maxSteps = 2000; // 1200 s at dt = 0.6 s
            const double maxHeadingChangeDeg = 540.0;

            double prevX = x, prevY = y, prevHeadingDeg = 0.0;
            series.Points.Add(new DataPoint(x, y));

            double advance = double.NaN;
            double tacticalDiameter = double.NaN;
            bool advanceFound = false, tacticalFound = false;

            for (int i = 0; i < maxSteps; i++)
            {
                double drdt = (KDim * rudderRad - r) / TDim;
                r += drdt * dt;
                heading += r * dt;
                x += vMs * Math.Cos(heading) * dt;
                y += vMs * Math.Sin(heading) * dt;

                double headingDeg = heading * RadToDeg;

                if (!advanceFound && prevHeadingDeg < 90.0 && headingDeg >= 90.0)
                {
                    double t = (90.0 - prevHeadingDeg) / (headingDeg - prevHeadingDeg);
                    advance = prevX + t * (x - prevX);
                    advanceFound = true;
                }
                if (!tacticalFound && prevHeadingDeg < 180.0 && headingDeg >= 180.0)
                {
                    double t = (180.0 - prevHeadingDeg) / (headingDeg - prevHeadingDeg);
                    tacticalDiameter = prevY + t * (y - prevY);
                    tacticalFound = true;
                }

                series.Points.Add(new DataPoint(x, y));

                prevX = x; prevY = y; prevHeadingDeg = headingDeg;

                if (headingDeg >= maxHeadingChangeDeg) break;
                if (advanceFound && tacticalFound && headingDeg >= 200.0) break; // a little past 180° is enough once both are captured
            }

            Advance = advanceFound ? advance : prevX;
            TacticalDiameter = tacticalFound ? tacticalDiameter : prevY;

            // Analytic steady-state cross-check: r_inf = K*delta, D_steady = 2V/r_inf.
            double rInf = KDim * rudderRad;
            SteadyTurningDiameter = rInf > 0 ? 2.0 * vMs / rInf : 0.0;

            TurningCirclePlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Simulates the standard 10°/10° zig-zag manoeuvre: rudder is stepped to
        /// +ZigZagRudderAngleDeg at t=0 and held until heading deviates by
        /// ZigZagHeadingAngleDeg from the initial heading, at which point the
        /// rudder is reversed to −ZigZagRudderAngleDeg (and vice-versa on the
        /// return swing). First overshoot angle is the peak heading deviation
        /// reached after the FIRST rudder reversal, minus the execute angle.
        /// </summary>
        private void SimulateZigZag(double vMs)
        {
            var headingSeries = (LineSeries)ZigZagPlotModel.Series[0];
            var rudderSeries = (LineSeries)ZigZagPlotModel.Series[1];
            headingSeries.Points.Clear();
            rudderSeries.Points.Clear();

            double executeRad = ZigZagRudderAngleDeg * DegToRad;
            double headingLimitRad = ZigZagHeadingAngleDeg * DegToRad;

            double heading = 0.0;
            double r = 0.0;
            double rudder = executeRad;
            int sign = 1;
            int executions = 0;

            const double dt = 0.5;
            const int maxSteps = 1800; // 900 s at dt = 0.5 s
            const int maxExecutions = 4;

            double t = 0.0;
            headingSeries.Points.Add(new DataPoint(0, 0));
            rudderSeries.Points.Add(new DataPoint(0, rudder * RadToDeg));

            bool afterFirstReversal = false;
            bool overshootCaptured = false;
            double peakSinceFirstReversal = double.NegativeInfinity;
            double overshoot = 0.0;

            for (int i = 0; i < maxSteps; i++)
            {
                double drdt = (KDim * rudder - r) / TDim;
                r += drdt * dt;
                heading += r * dt;
                t += dt;

                if (sign > 0 && heading >= headingLimitRad)
                {
                    rudder = -executeRad;
                    sign = -1;
                    executions++;
                }
                else if (sign < 0 && heading <= -headingLimitRad)
                {
                    rudder = executeRad;
                    sign = 1;
                    executions++;
                }

                double headingDeg = heading * RadToDeg;

                if (executions >= 1) afterFirstReversal = true;
                if (afterFirstReversal && !overshootCaptured)
                {
                    peakSinceFirstReversal = Math.Max(peakSinceFirstReversal, headingDeg);
                }
                if (executions >= 2 && !overshootCaptured)
                {
                    overshoot = peakSinceFirstReversal - ZigZagHeadingAngleDeg;
                    overshootCaptured = true;
                }

                headingSeries.Points.Add(new DataPoint(t, headingDeg));
                rudderSeries.Points.Add(new DataPoint(t, rudder * RadToDeg));

                if (executions >= maxExecutions) break;
            }

            FirstOvershootAngleDeg = overshootCaptured ? Math.Max(0.0, overshoot) : 0.0;

            ZigZagPlotModel.InvalidatePlot(true);
        }

        private void RebuildTurningCriteria()
        {
            TurningCriteria.Clear();
            double advanceLimit = 4.5 * Lpp;
            double tacticalLimit = 5.0 * Lpp;

            TurningCriteria.Add(new HydrostaticsCriterion
            {
                Name = "Advance",
                Requirement = $"≤ 4.5·Lpp = {advanceLimit:F1} m",
                ActualText = $"{Advance:F1} m",
                IsPass = Advance <= advanceLimit
            });
            TurningCriteria.Add(new HydrostaticsCriterion
            {
                Name = "Tactical Diameter",
                Requirement = $"≤ 5.0·Lpp = {tacticalLimit:F1} m",
                ActualText = $"{TacticalDiameter:F1} m",
                IsPass = TacticalDiameter <= tacticalLimit
            });
        }

        private void RebuildZigZagCriteria()
        {
            ZigZagCriteria.Clear();
            const double overshootLimit = 20.0;
            ZigZagCriteria.Add(new HydrostaticsCriterion
            {
                Name = "First Overshoot Angle (10°/10° zig-zag)",
                Requirement = "≤ 20° (simplified)",
                ActualText = $"{FirstOvershootAngleDeg:F1}°",
                IsPass = FirstOvershootAngleDeg <= overshootLimit
            });
        }

        private static void ClearSeries(PlotModel model)
        {
            foreach (var s in model.Series)
            {
                if (s is LineSeries ls) ls.Points.Clear();
            }
            model.InvalidatePlot(true);
        }

        private static PlotModel BuildTurningCirclePlotModel()
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
                Title = "X (m)",
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
                Title = "Y (m)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine
            });

            model.Series.Add(new LineSeries
            {
                Title = "Turning Circle",
                Color = gold,
                StrokeThickness = 2.5
            });

            return model;
        }

        private static PlotModel BuildZigZagPlotModel()
        {
            var gold = OxyColor.FromRgb(0xC8, 0x96, 0x0C);
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
                Title = "Time (s)",
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
                Title = "Angle (deg)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine
            });

            model.Series.Add(new LineSeries { Title = "Heading (deg)", Color = gold, StrokeThickness = 2.5 });
            model.Series.Add(new LineSeries { Title = "Rudder (deg)", Color = teal, StrokeThickness = 1.75, LineStyle = LineStyle.Dash });

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
