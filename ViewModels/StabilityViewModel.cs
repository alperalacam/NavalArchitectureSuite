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
    /// Intact stability calculation engine for the Stability module.
    ///
    /// Scope (IMO 2008 IS Code, Res. MSC.267(85), Part A):
    ///   - Loading condition manager (Δ, KG, free surface moment per condition)
    ///   - Hydrostatics at each condition's draft: KB (Morrish's approximation),
    ///     BM (Normand's approximation, same as the Hydrostatics module), KM
    ///   - Free surface correction FSC = ΣFSM / Δ, GM(fluid) = KM − KG − FSC
    ///   - GZ curve via the wall-sided formula (as in Hydrostatics). Its tan²θ
    ///     term grows without bound as θ→90°, so it is only physically plausible
    ///     for small/moderate angles; the curve, GZ table and Max-GZ search are
    ///     limited to 0°-30° (GzChartMaxDeg) to keep "Max GZ" within the ~0.2-3 m
    ///     order of magnitude real ships show, rather than the unbounded blow-up
    ///     the formula produces past ~40-50°. Because the formula has no interior
    ///     peak over that range (it increases monotonically for any GM,BM > 0),
    ///     "Angle of Max GZ" will generally just read as GzChartMaxDeg itself —
    ///     a known limitation of the wall-sided model, not a computed hump. A
    ///     true peak-and-decline curve needs actual hull-form (Bonjean/cross-curve)
    ///     data, which this coefficient-only module does not have.
    ///   - Reg 2.2 general intact stability criteria (same six checks as Hydrostatics,
    ///     evaluated per selected loading condition)
    ///   - Reg 2.3 Severe Wind and Rolling Criterion ("weather criterion"):
    ///       lw1 = P·A·Z / (1000·g·Δ),  lw2 = 1.5·lw1
    ///       θ0  = steady heel angle under lw1 (first GZ/lw1 intersection)
    ///       θ1  = 109·k·X1·X2·√(r·s)   (roll amplitude to windward, degrees)
    ///       θ2  = min(down-flooding angle, 50°, second GZ/lw2 intersection)
    ///       Pass if Area b (θ0→θ2, GZ above lw2) ≥ Area a (θ0−θ1→θ0, lw2 above GZ)
    ///     X1 (B/T), X2 (Cb) and s (roll period Tr) use the standard published
    ///     breakpoint tables; s is implemented as the single-variable (Tr-only)
    ///     approximation — the full regulatory table also varies with r and
    ///     should be used for class/flag submissions. k = 0.7 with bilge keels,
    ///     1.0 for a round-bilge hull without bilge keels (the bilge-keel-area
    ///     table itself is not modelled).
    ///   Grain Code stability (International Grain Code) is not covered by this
    ///   module and would be a natural follow-on.
    /// </summary>
    public class StabilityViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const double SeawaterDensity = 1.025; // t/m^3
        private const double Gravity = 9.81;           // m/s^2

        // The wall-sided GZ formula's tan²θ term diverges as θ→90°, so it is only
        // trustworthy for small/moderate angles. GzChartMaxDeg bounds the plotted
        // curve, the GZ table and the Max-GZ search to where that stays plausible
        // (0.2-3 m order of magnitude, matching IMO's own primary GZ@30° check).
        // WeatherSearchMaxDeg is a separate, wider ceiling used only to search for
        // the weather-criterion equilibrium/second-intercept angles; those results
        // are further capped by θ2 = min(down-flooding, 50°, ...) so this constant
        // just needs to exceed that while still avoiding the near-90° blow-up.
        private const double GzChartMaxDeg = 30.0;
        private const double WeatherSearchMaxDeg = 70.0;

        #region Hull form inputs (shared across loading conditions)

        private double _lwl = 120.0;
        public double Lwl { get => _lwl; set { if (SetField(ref _lwl, value)) Recalculate(); } }

        private double _bwl = 20.0;
        public double Bwl { get => _bwl; set { if (SetField(ref _bwl, value)) Recalculate(); } }

        private double _cb = 0.68;
        public double Cb { get => _cb; set { if (SetField(ref _cb, value)) Recalculate(); } }

        private double _cwp = 0.82;
        public double Cwp { get => _cwp; set { if (SetField(ref _cwp, value)) Recalculate(); } }

        #endregion

        #region Weather criterion inputs

        private double _windPressure = 504.0; // Pa, IMO default for unrestricted service
        public double WindPressure { get => _windPressure; set { if (SetField(ref _windPressure, value)) Recalculate(); } }

        private double _lateralArea = 1500.0; // m^2, projected windage area above waterline
        public double LateralArea { get => _lateralArea; set { if (SetField(ref _lateralArea, value)) Recalculate(); } }

        private double _heightZ = 7.0; // m, centroid of lateral area to ~T/2
        public double HeightZ { get => _heightZ; set { if (SetField(ref _heightZ, value)) Recalculate(); } }

        private double _downfloodingAngle = 60.0; // deg
        public double DownfloodingAngle { get => _downfloodingAngle; set { if (SetField(ref _downfloodingAngle, value)) Recalculate(); } }

        private bool _hasBilgeKeels = true;
        public bool HasBilgeKeels { get => _hasBilgeKeels; set { if (SetField(ref _hasBilgeKeels, value)) Recalculate(); } }

        #endregion

        #region Loading conditions

        public ObservableCollection<LoadingCondition> LoadingConditions { get; } = new();

        private LoadingCondition? _selectedCondition;
        public LoadingCondition? SelectedCondition
        {
            get => _selectedCondition;
            set
            {
                if (ReferenceEquals(_selectedCondition, value)) return;
                if (_selectedCondition is not null) _selectedCondition.PropertyChanged -= OnConditionChanged;
                _selectedCondition = value;
                if (_selectedCondition is not null) _selectedCondition.PropertyChanged += OnConditionChanged;
                OnPropertyChanged(nameof(SelectedCondition));
                Recalculate();
            }
        }

        private void OnConditionChanged(object? sender, PropertyChangedEventArgs e) => Recalculate();

        public void AddCondition()
        {
            var basis = SelectedCondition;
            var condition = new LoadingCondition
            {
                Name = $"Condition {LoadingConditions.Count + 1}",
                Displacement = basis?.Displacement ?? 10000.0,
                Kg = basis?.Kg ?? 8.0,
                FreeSurfaceMoment = 0.0
            };
            LoadingConditions.Add(condition);
            SelectedCondition = condition;
        }

        public void RemoveCondition(LoadingCondition condition)
        {
            if (LoadingConditions.Count <= 1) return; // always keep at least one condition
            int index = LoadingConditions.IndexOf(condition);
            if (index < 0) return;
            LoadingConditions.RemoveAt(index);
            if (ReferenceEquals(SelectedCondition, condition))
            {
                int fallback = Math.Max(0, index - 1);
                SelectedCondition = LoadingConditions.Count > 0 ? LoadingConditions[fallback] : null;
            }
        }

        #endregion

        #region Per-condition hydrostatic outputs

        private double _draftT;
        public double DraftT { get => _draftT; private set => SetField(ref _draftT, value); }

        private double _kb;
        public double Kb { get => _kb; private set => SetField(ref _kb, value); }

        private double _bm;
        public double Bm { get => _bm; private set => SetField(ref _bm, value); }

        private double _km;
        public double Km { get => _km; private set => SetField(ref _km, value); }

        private double _fsc;
        public double Fsc { get => _fsc; private set => SetField(ref _fsc, value); }

        private double _gmSolid;
        public double GmSolid { get => _gmSolid; private set => SetField(ref _gmSolid, value); }

        private double _gmFluid;
        public double GmFluid { get => _gmFluid; private set => SetField(ref _gmFluid, value); }

        public bool GmIsPositive => GmFluid >= 0;

        private double _maxGz;
        public double MaxGz { get => _maxGz; private set => SetField(ref _maxGz, value); }

        private double _maxGzAngle;
        public double MaxGzAngle { get => _maxGzAngle; private set => SetField(ref _maxGzAngle, value); }

        public ObservableCollection<GzPoint> GzTable { get; } = new();
        public ObservableCollection<HydrostaticsCriterion> Criteria { get; } = new();

        public PlotModel GzPlotModel { get; }

        #endregion

        #region Weather criterion outputs

        private double _lw1;
        public double Lw1 { get => _lw1; private set => SetField(ref _lw1, value); }

        private double _lw2;
        public double Lw2 { get => _lw2; private set => SetField(ref _lw2, value); }

        private double _theta0;
        public double Theta0 { get => _theta0; private set => SetField(ref _theta0, value); }

        private double _theta1;
        public double Theta1 { get => _theta1; private set => SetField(ref _theta1, value); }

        private double _theta2;
        public double Theta2 { get => _theta2; private set => SetField(ref _theta2, value); }

        private double _areaA;
        public double AreaA { get => _areaA; private set => SetField(ref _areaA, value); }

        private double _areaB;
        public double AreaB { get => _areaB; private set => SetField(ref _areaB, value); }

        private bool _weatherCriterionPass;
        public bool WeatherCriterionPass { get => _weatherCriterionPass; private set => SetField(ref _weatherCriterionPass, value); }

        public string WeatherStatusText => WeatherCriterionPass ? "PASS" : "FAIL";

        public ObservableCollection<HydrostaticsCriterion> WeatherCriteria { get; } = new();

        #endregion

        // Standard published breakpoint tables for the weather criterion (IMO 2008 IS Code,
        // Res. MSC.267(85) Part A Annex; values as commonly tabulated in naval-architecture
        // references such as Rawson & Tupper, "Basic Ship Theory").
        private static readonly (double x, double y)[] X1Table =
        {
            (2.4, 1.00), (2.5, 0.98), (2.6, 0.96), (2.7, 0.95), (2.8, 0.93), (2.9, 0.91),
            (3.0, 0.90), (3.1, 0.88), (3.2, 0.86), (3.3, 0.84), (3.4, 0.82), (3.5, 0.80),
            (3.6, 0.79), (3.7, 0.77), (3.8, 0.76), (3.9, 0.74), (4.0, 0.73)
        };

        private static readonly (double x, double y)[] X2Table =
        {
            (0.45, 0.75), (0.50, 0.82), (0.55, 0.89), (0.60, 0.95), (0.65, 0.97), (0.70, 1.00)
        };

        // Simplified single-variable (Tr only) approximation of the s-factor table.
        private static readonly (double x, double y)[] STable =
        {
            (6, 0.100), (8, 0.093), (10, 0.079), (12, 0.065), (14, 0.053), (16, 0.044), (18, 0.038), (20, 0.035)
        };

        public StabilityViewModel()
        {
            GzPlotModel = BuildPlotModel();
            SeedDefaultConditions();
            SelectedCondition = LoadingConditions.Count > 0 ? LoadingConditions[0] : null;
        }

        private void SeedDefaultConditions()
        {
            LoadingConditions.Add(new LoadingCondition { Name = "Light Ship", Displacement = 6200, Kg = 7.8, FreeSurfaceMoment = 0 });
            LoadingConditions.Add(new LoadingCondition { Name = "Full Load Departure", Displacement = 15800, Kg = 8.6, FreeSurfaceMoment = 350 });
            LoadingConditions.Add(new LoadingCondition { Name = "Full Load Arrival", Displacement = 14600, Kg = 8.7, FreeSurfaceMoment = 180 });
            LoadingConditions.Add(new LoadingCondition { Name = "Ballast Arrival", Displacement = 9200, Kg = 7.9, FreeSurfaceMoment = 420 });
        }

        private double GzAtAngle(double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double tan = Math.Tan(rad);
            return (GmFluid + 0.5 * Bm * tan * tan) * Math.Sin(rad);
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

        /// <summary>Trapezoidal area between two curves (f = upper − lower) over [fromDeg, toDeg]; result clamped to ≥ 0.</summary>
        private double AreaBetween(Func<double, double> f, double fromDeg, double toDeg, double stepDeg = 0.25)
        {
            if (toDeg <= fromDeg) return 0.0;
            int steps = Math.Max(1, (int)Math.Round((toDeg - fromDeg) / stepDeg));
            double actualStep = (toDeg - fromDeg) / steps;
            double area = 0.0;
            double prev = f(fromDeg);
            for (int i = 1; i <= steps; i++)
            {
                double angle = fromDeg + i * actualStep;
                double v = f(angle);
                area += 0.5 * (prev + v) * (actualStep * Math.PI / 180.0);
                prev = v;
            }
            return Math.Max(0.0, area);
        }

        private static double Interpolate((double x, double y)[] table, double x)
        {
            if (x <= table[0].x) return table[0].y;
            if (x >= table[^1].x) return table[^1].y;
            for (int i = 0; i < table.Length - 1; i++)
            {
                var (x0, y0) = table[i];
                var (x1, y1) = table[i + 1];
                if (x >= x0 && x <= x1)
                {
                    double t = (x1 > x0) ? (x - x0) / (x1 - x0) : 0.0;
                    return y0 + t * (y1 - y0);
                }
            }
            return table[^1].y;
        }

        /// <summary>First angle (deg) where GZ(θ) rises to meet <paramref name="target"/>, linearly interpolated.</summary>
        private double FindEquilibriumAngle(double target, double fromDeg, double toDeg, double stepDeg = 0.1)
        {
            double prevAngle = fromDeg;
            double prevDiff = GzAtAngle(fromDeg) - target;
            for (double a = fromDeg + stepDeg; a <= toDeg; a += stepDeg)
            {
                double diff = GzAtAngle(a) - target;
                if (prevDiff < 0 && diff >= 0)
                {
                    double t = (diff - prevDiff) != 0 ? -prevDiff / (diff - prevDiff) : 0.0;
                    return prevAngle + t * (a - prevAngle);
                }
                prevAngle = a;
                prevDiff = diff;
            }
            return toDeg; // no equilibrium found within range (ship rolls past the range under the wind lever)
        }

        /// <summary>Angle (deg) where GZ(θ), having risen above lw2, drops back below it — the "second intercept".</summary>
        private double FindSecondIntercept(double lw2, double fromDeg, double toDeg, double stepDeg = 0.1)
        {
            bool wasAbove = GzAtAngle(fromDeg) - lw2 >= 0;
            double prevAngle = fromDeg;
            double prevDiff = GzAtAngle(fromDeg) - lw2;
            for (double a = fromDeg + stepDeg; a <= toDeg; a += stepDeg)
            {
                double diff = GzAtAngle(a) - lw2;
                if (wasAbove && diff < 0)
                {
                    double t = (prevDiff - diff) != 0 ? prevDiff / (prevDiff - diff) : 0.0;
                    return prevAngle + t * (a - prevAngle);
                }
                if (diff >= 0) wasAbove = true;
                prevAngle = a;
                prevDiff = diff;
            }
            return toDeg; // GZ never re-crosses lw2 within range; cap is used as-is
        }

        private void Recalculate()
        {
            var condition = SelectedCondition;
            bool validHull = Lwl > 0 && Bwl > 0 && Cb > 0 && Cwp > 0;
            bool valid = validHull && condition is not null && condition.Displacement > 0;

            double displacement = condition?.Displacement ?? 0.0;
            double kg = condition?.Kg ?? 0.0;
            double fsm = condition?.FreeSurfaceMoment ?? 0.0;

            double volumeDisplacement = displacement / SeawaterDensity;
            DraftT = valid ? volumeDisplacement / (Cb * Lwl * Bwl) : 0.0;

            // Morrish's approximation for KB.
            Kb = (valid && DraftT > 0 && Cwp > 0) ? DraftT * (5.0 / 6.0 - Cb / (3.0 * Cwp)) : 0.0;

            // Normand's approximation for transverse waterplane inertia (same as Hydrostatics module).
            double transverseInertia = Lwl * Math.Pow(Bwl, 3) * (0.1216 * Cwp - 0.0410);
            Bm = volumeDisplacement > 0 ? transverseInertia / volumeDisplacement : 0.0;

            Km = Kb + Bm;
            Fsc = displacement > 0 ? fsm / displacement : 0.0;
            GmSolid = Km - kg;
            GmFluid = GmSolid - Fsc;
            OnPropertyChanged(nameof(GmIsPositive));

            RebuildGzCurve();
            RebuildGeneralCriteria();
            RebuildWeatherCriterion(displacement);
        }

        private void RebuildGzCurve()
        {
            GzTable.Clear();
            double[] keyAngles = { 0, 5, 10, 15, 20, 25, 30 };
            foreach (var a in keyAngles)
            {
                GzTable.Add(new GzPoint { AngleDeg = a, GzMetres = GzAtAngle(a) });
            }

            var series = (LineSeries)GzPlotModel.Series[0];
            series.Points.Clear();

            double bestGz = double.NegativeInfinity;
            double bestAngle = 0;
            for (double a = 0; a <= GzChartMaxDeg; a += 1.0)
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

        private void RebuildGeneralCriteria()
        {
            Criteria.Clear();

            double gz30 = GzAtAngle(30);
            double area030 = AreaUnderGz(0, 30);
            double area040 = AreaUnderGz(0, 40);
            double area3040 = AreaUnderGz(30, 40);

            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "Initial GM (fluid)",
                Requirement = "≥ 0.15 m",
                ActualText = $"{GmFluid:F3} m",
                IsPass = GmFluid >= 0.15
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

        private void RebuildWeatherCriterion(double displacement)
        {
            WeatherCriteria.Clear();

            bool valid = displacement > 0 && DraftT > 0 && GmFluid > 0 && Bwl > 0 && Lwl > 0;
            if (!valid)
            {
                Lw1 = Lw2 = Theta0 = Theta1 = Theta2 = AreaA = AreaB = 0.0;
                WeatherCriterionPass = false;
                OnPropertyChanged(nameof(WeatherStatusText));
                WeatherCriteria.Add(new HydrostaticsCriterion
                {
                    Name = "Weather Criterion (b ≥ a)",
                    Requirement = "Area b ≥ Area a",
                    ActualText = "not evaluated",
                    IsPass = false
                });
                return;
            }

            Lw1 = (WindPressure * LateralArea * HeightZ) / (1000.0 * Gravity * displacement);
            Lw2 = 1.5 * Lw1;

            Theta0 = FindEquilibriumAngle(Lw1, 0.0, WeatherSearchMaxDeg);

            double bt = Bwl / DraftT;
            double c = 0.373 + 0.023 * bt - 0.043 * (Lwl / 100.0);
            double rollPeriod = 2.0 * c * Bwl / Math.Sqrt(GmFluid);
            double kgEffective = (SelectedCondition?.Kg ?? 0.0) + Fsc;
            double og = kgEffective - DraftT; // +ve when G is above the waterline
            double r = Math.Clamp(0.73 + 0.6 * (og / DraftT), -1.0, 1.0);
            double s = Interpolate(STable, rollPeriod);
            double x1 = Interpolate(X1Table, bt);
            double x2 = Interpolate(X2Table, Cb);
            double k = HasBilgeKeels ? 0.7 : 1.0;

            Theta1 = 109.0 * k * x1 * x2 * Math.Sqrt(Math.Max(0.0, r * s));

            double windwardAngle = Math.Max(-WeatherSearchMaxDeg, Theta0 - Theta1);

            double theta2Cap = Math.Min(DownfloodingAngle, Math.Min(50.0, WeatherSearchMaxDeg));
            double secondIntercept = FindSecondIntercept(Lw2, Theta0, theta2Cap);
            Theta2 = Math.Min(secondIntercept, theta2Cap);

            AreaA = AreaBetween(a => Lw2 - GzAtAngle(a), windwardAngle, Theta0);
            AreaB = AreaBetween(a => GzAtAngle(a) - Lw2, Theta0, Theta2);

            WeatherCriterionPass = AreaB >= AreaA;
            OnPropertyChanged(nameof(WeatherStatusText));

            WeatherCriteria.Add(new HydrostaticsCriterion { Name = "Steady wind lever, lw1", Requirement = "—", ActualText = $"{Lw1:F4} m", IsPass = true });
            WeatherCriteria.Add(new HydrostaticsCriterion { Name = "Gust wind lever, lw2 = 1.5·lw1", Requirement = "—", ActualText = $"{Lw2:F4} m", IsPass = true });
            WeatherCriteria.Add(new HydrostaticsCriterion { Name = "Steady heel angle, θ0", Requirement = "—", ActualText = $"{Theta0:F1}°", IsPass = true });
            WeatherCriteria.Add(new HydrostaticsCriterion { Name = "Roll angle to windward, θ1", Requirement = "—", ActualText = $"{Theta1:F1}°", IsPass = true });
            WeatherCriteria.Add(new HydrostaticsCriterion { Name = "θ2 (flooding / 50° / 2nd intercept)", Requirement = "—", ActualText = $"{Theta2:F1}°", IsPass = true });
            WeatherCriteria.Add(new HydrostaticsCriterion { Name = "Area a (θ0−θ1 → θ0)", Requirement = "—", ActualText = $"{AreaA:F4} m·rad", IsPass = true });
            WeatherCriteria.Add(new HydrostaticsCriterion { Name = "Area b (θ0 → θ2)", Requirement = "≥ Area a", ActualText = $"{AreaB:F4} m·rad", IsPass = WeatherCriterionPass });
            WeatherCriteria.Add(new HydrostaticsCriterion { Name = "Weather Criterion (Reg 2.3)", Requirement = "Area b ≥ Area a", ActualText = WeatherCriterionPass ? "Satisfied" : "Not satisfied", IsPass = WeatherCriterionPass });
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
                Maximum = GzChartMaxDeg
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
