using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NavalArchitectureSuite.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Calculation engine + live chart for the Hydrostatics module.
    ///
    /// Formulas follow standard naval-architecture references (Rawson &amp; Tupper,
    /// "Basic Ship Theory"; Derrett, "Ship Stability for Masters and Mates"):
    ///   - Mean draft from the block coefficient identity  Cb = ∇ / (L·B·T)
    ///   - Transverse waterplane inertia via Normand's approximation
    ///       I_T = L·B³·(0.1216·Cwp − 0.0410)
    ///   - BM = I_T / ∇ ,  GM = KB + BM − KG
    ///   - GZ(θ) via the wall-sided formula  GZ = (GM + ½·BM·tan²θ)·sinθ
    ///   - IMO 2008 IS Code, Part A, Reg 2.2 general intact-stability criteria
    ///
    /// The wall-sided GZ formula assumes vertical topsides and is only accurate
    /// for small-to-moderate heel angles; it is presented here (0°-50°) for
    /// teaching / preliminary-design purposes. A full-form GZ curve requires
    /// hull-specific cross curves of stability (KN data).
    /// </summary>
    public class HydrostaticsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const double SeawaterDensity = 1.025; // t/m^3
        private const double Gravity = 9.80665;        // m/s^2

        #region Inputs

        private double _lwl = 120.0;
        public double Lwl { get => _lwl; set { if (SetField(ref _lwl, value)) Recalculate(); } }

        private double _bwl = 20.0;
        public double Bwl { get => _bwl; set { if (SetField(ref _bwl, value)) Recalculate(); } }

        private double _depth = 11.0;
        public double Depth { get => _depth; set { if (SetField(ref _depth, value)) Recalculate(); } }

        private double _cb = 0.68;
        public double Cb { get => _cb; set { if (SetField(ref _cb, value)) Recalculate(); } }

        private double _cm = 0.98;
        public double Cm { get => _cm; set { if (SetField(ref _cm, value)) Recalculate(); } }

        private double _cwp = 0.82;
        public double Cwp { get => _cwp; set { if (SetField(ref _cwp, value)) Recalculate(); } }

        private double _displacement = 9500.0;
        public double Displacement { get => _displacement; set { if (SetField(ref _displacement, value)) Recalculate(); } }

        private double _kg = 8.5;
        public double Kg { get => _kg; set { if (SetField(ref _kg, value)) Recalculate(); } }

        private double _kb = 3.0;
        public double Kb { get => _kb; set { if (SetField(ref _kb, value)) Recalculate(); } }

        #endregion

        #region Outputs

        private double _draftT;
        public double DraftT { get => _draftT; private set => SetField(ref _draftT, value); }

        private double _volumeDisplacement;
        public double VolumeDisplacement { get => _volumeDisplacement; private set => SetField(ref _volumeDisplacement, value); }

        private double _prismaticCoefficient;
        public double PrismaticCoefficient { get => _prismaticCoefficient; private set => SetField(ref _prismaticCoefficient, value); }

        private double _waterplaneArea;
        public double WaterplaneArea { get => _waterplaneArea; private set => SetField(ref _waterplaneArea, value); }

        private double _transverseInertia;
        public double TransverseInertia { get => _transverseInertia; private set => SetField(ref _transverseInertia, value); }

        private double _bm;
        public double Bm { get => _bm; private set => SetField(ref _bm, value); }

        private double _km;
        public double Km { get => _km; private set => SetField(ref _km, value); }

        private double _gm;
        public double Gm { get => _gm; private set => SetField(ref _gm, value); }

        public bool GmIsPositive => Gm >= 0;

        private double _deckEdgeImmersionAngle;
        public double DeckEdgeImmersionAngle { get => _deckEdgeImmersionAngle; private set => SetField(ref _deckEdgeImmersionAngle, value); }

        private double _hullSpeedKnots;
        public double HullSpeedKnots { get => _hullSpeedKnots; private set => SetField(ref _hullSpeedKnots, value); }

        private double _froudeNumber;
        public double FroudeNumber { get => _froudeNumber; private set => SetField(ref _froudeNumber, value); }

        private double _maxGz;
        public double MaxGz { get => _maxGz; private set => SetField(ref _maxGz, value); }

        private double _maxGzAngle;
        public double MaxGzAngle { get => _maxGzAngle; private set => SetField(ref _maxGzAngle, value); }

        public ObservableCollection<GzPoint> GzTable { get; } = new();
        public ObservableCollection<HydrostaticsCriterion> Criteria { get; } = new();

        public PlotModel GzPlotModel { get; }

        #endregion

        public HydrostaticsViewModel()
        {
            GzPlotModel = BuildPlotModel();
            Recalculate();
        }

        private double GzAtAngle(double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double tan = Math.Tan(rad);
            return (Gm + 0.5 * Bm * tan * tan) * Math.Sin(rad);
        }

        private double AreaUnderGz(double fromDeg, double toDeg, double stepDeg = 0.5)
        {
            if (toDeg <= fromDeg) return 0.0;
            int steps = Math.Max(1, (int)Math.Round((toDeg - fromDeg) / stepDeg));
            double actualStep = (toDeg - fromDeg) / steps;
            double area = 0.0;
            double prevGz = GzAtAngle(fromDeg);
            for (int i = 1; i <= steps; i++)
            {
                double angle = fromDeg + i * actualStep;
                double gz = GzAtAngle(angle);
                area += 0.5 * (prevGz + gz) * (actualStep * Math.PI / 180.0);
                prevGz = gz;
            }
            return area;
        }

        private void Recalculate()
        {
            // Guard against non-physical / incomplete inputs while the user is typing.
            bool valid = Lwl > 0 && Bwl > 0 && Cb > 0 && Cm > 0 && Displacement > 0;

            VolumeDisplacement = Displacement / SeawaterDensity;
            DraftT = valid ? VolumeDisplacement / (Cb * Lwl * Bwl) : 0.0;
            PrismaticCoefficient = Cm > 0 ? Cb / Cm : 0.0;
            WaterplaneArea = Cwp * Lwl * Bwl;
            TransverseInertia = Lwl * Math.Pow(Bwl, 3) * (0.1216 * Cwp - 0.0410);
            Bm = VolumeDisplacement > 0 ? TransverseInertia / VolumeDisplacement : 0.0;
            Km = Kb + Bm;
            Gm = Km - Kg;
            OnPropertyChanged(nameof(GmIsPositive));

            double freeboard = Depth - DraftT;
            DeckEdgeImmersionAngle = (Bwl > 0 && freeboard > 0)
                ? Math.Atan2(freeboard, Bwl / 2.0) * 180.0 / Math.PI
                : 0.0;

            HullSpeedKnots = Lwl > 0 ? 2.43 * Math.Sqrt(Lwl) : 0.0;
            double hullSpeedMs = HullSpeedKnots * 0.514444;
            FroudeNumber = Lwl > 0 ? hullSpeedMs / Math.Sqrt(Gravity * Lwl) : 0.0;

            RebuildGzCurve();
            RebuildCriteria();
        }

        private void RebuildGzCurve()
        {
            // Key-angle table (matches the IMO criteria angles).
            GzTable.Clear();
            double[] keyAngles = { 0, 10, 20, 30, 40, 50 };
            foreach (var a in keyAngles)
            {
                GzTable.Add(new GzPoint { AngleDeg = a, GzMetres = GzAtAngle(a) });
            }

            // Fine-resolution curve for the chart and max-GZ search (0°-50°: the
            // practical validity range of the wall-sided approximation here).
            var series = (LineSeries)GzPlotModel.Series[0];
            series.Points.Clear();

            double bestGz = double.NegativeInfinity;
            double bestAngle = 0;
            for (double a = 0; a <= 50.0; a += 1.0)
            {
                double gz = GzAtAngle(a);
                series.Points.Add(new DataPoint(a, gz));
                if (gz > bestGz)
                {
                    bestGz = gz;
                    bestAngle = a;
                }
            }
            MaxGz = bestGz;
            MaxGzAngle = bestAngle;

            GzPlotModel.InvalidatePlot(true);
        }

        private void RebuildCriteria()
        {
            Criteria.Clear();

            double gz30 = GzAtAngle(30);
            double area030 = AreaUnderGz(0, 30);
            double area040 = AreaUnderGz(0, 40);
            double area3040 = AreaUnderGz(30, 40);

            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "Initial GM",
                Requirement = "≥ 0.15 m",
                ActualText = $"{Gm:F3} m",
                IsPass = Gm >= 0.15
            });
            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "GZ at 30°",
                Requirement = "≥ 0.20 m",
                ActualText = $"{gz30:F3} m",
                IsPass = gz30 >= 0.20
            });
            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "Angle of Max GZ",
                Requirement = "≥ 25°",
                ActualText = $"{MaxGzAngle:F1}°",
                IsPass = MaxGzAngle >= 25
            });
            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "Area 0°–30°",
                Requirement = "≥ 0.055 m·rad",
                ActualText = $"{area030:F4} m·rad",
                IsPass = area030 >= 0.055
            });
            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "Area 0°–40°",
                Requirement = "≥ 0.090 m·rad",
                ActualText = $"{area040:F4} m·rad",
                IsPass = area040 >= 0.090
            });
            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "Area 30°–40°",
                Requirement = "≥ 0.030 m·rad",
                ActualText = $"{area3040:F4} m·rad",
                IsPass = area3040 >= 0.030
            });
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
                Title = "Heel Angle (deg)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine,
                Minimum = 0,
                Maximum = 50
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "GZ (m)",
                TitleColor = lightText,
                TextColor = lightText,
                TicklineColor = axisLine,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridLine,
                MinorGridlineColor = gridLine,
                AxislineColor = axisLine
            });

            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = 0,
                Color = axisLine,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1
            });
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = 30,
                Color = OxyColor.FromArgb(140, 0xC8, 0x96, 0x0C),
                LineStyle = LineStyle.Dash,
                Text = "30°",
                TextColor = gold,
                FontSize = 11
            });
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = 40,
                Color = OxyColor.FromArgb(140, 0xC8, 0x96, 0x0C),
                LineStyle = LineStyle.Dash,
                Text = "40°",
                TextColor = gold,
                FontSize = 11
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
