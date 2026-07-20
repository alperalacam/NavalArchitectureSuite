using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NavalArchitectureSuite.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Simplified, teaching-level damage stability calculation engine for the Damage Stability module.
    ///
    /// Scope and simplifications (SOLAS II-1, lost-buoyancy method):
    ///   - Damage case manager: each case is a single flooded compartment defined by its
    ///     forward/aft boundaries (X1, X2 from AP) and permeability. Multi-compartment
    ///     flooding, longitudinal/transverse subdivision and cross-flooding are not modelled.
    ///   - Flooded volume and parallel sinkage use a prismatic ("lost buoyancy") approximation:
    ///       v = lx·Bwl·T·µ,  Awp_d = Awp·(1 − µ·lx/Lpp) (floored at 0.15·Awp),  ΔT = v/Awp_d
    ///     This ignores hull form variation along the length (a box-like approximation of the
    ///     breach) and any trim caused by an off-centre longitudinal centre of flotation shift.
    ///   - Loss of transverse waterplane inertia is approximated as a rectangular strip:
    ///       ΔIT = µ·Bwl³·lx/12, subtracted from the intact transverse inertia (Normand's
    ///     approximation, same basis as the Hydrostatics/Stability modules) before dividing by
    ///     the (unchanged) volume of displacement to obtain BM_damaged.
    ///   - KB_damaged uses Morrish's approximation evaluated at the damaged draft.
    ///   - Damage is assumed symmetric about the centreline: no heel/list angle from off-centre
    ///     flooding is modelled. In reality asymmetric flooding produces a list that must be
    ///     corrected/accounted for before applying the residual GZ curve — this module only
    ///     evaluates the upright residual condition, which materially overstates stability for
    ///     any real off-centre damage case.
    ///   - The residual GZ curve reuses the intact wall-sided formula
    ///       GZ(θ) = (GMd + 0.5·BMd·tan²θ)·sinθ
    ///     with the damaged GM/BM. Its tan²θ term diverges as θ→90°, so it is only physically
    ///     plausible for small/moderate angles; the curve and Max-GZ search are limited to
    ///     0-30° (GzChartMaxDeg) rather than the full residual range, to keep "Max GZ" within
    ///     the ~0.2-3 m order of magnitude real residual GZ curves show instead of the
    ///     unbounded blow-up the formula produces past ~40-50°. As with the Stability module,
    ///     the formula has no interior peak over this range for GM,BM > 0, so MaxGzAngle and
    ///     RangeOfPositiveStability will generally just read as the domain edge — a known
    ///     limitation of the wall-sided model, not a computed hump.
    ///   - The SOLAS-style "s-factor" is a deliberately simplified single-case form:
    ///       s = clamp( sqrt(max(0,GZmax)/0.12) · sqrt(max(0,Range)/16), 0, 1 ), forced to 0 if
    ///     GMd &lt; 0.05 m. The real Reg 7-2 formula also incorporates a separate GZmax/Range
    ///     weighting split, heel-angle and time-to-flood factors, and differs for passenger vs
    ///     cargo ships — none of that is modelled here.
    ///   - The attained index A = Σ(pi·si) is a weighted average over the case list only
    ///     (pi = each case's ProbabilityWeight normalized to sum to 1). It is NOT the full
    ///     SOLAS probabilistic method, which sums pi·si over exhaustive damage zones/combinations
    ///     derived from the actual subdivision and loading conditions.
    ///   - The required index R = 1 − 1/(1 + Lpp/100) is a simplified single-variable stand-in
    ///     for SOLAS II-1 Reg 6, which is actually a function of ship type, Lpp and the number
    ///     of persons on board (N1, N2) — provided for teaching/preliminary use only.
    /// </summary>
    public class DamageStabilityViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const double SeawaterDensity = 1.025; // t/m^3

        // The wall-sided GZ formula's tan²θ term diverges as θ→90°, so it is only
        // trustworthy for small/moderate angles (past ~40-50° it produces
        // multi-metre "residual GZ" figures no damaged ship actually has). Capping
        // the residual curve/search domain at 30° keeps MaxGz within the ~0.2-3 m
        // order of magnitude real GZ curves show. As with the Stability module,
        // the formula has no interior peak over this range for GM,BM > 0, so
        // MaxGzAngle/RangeOfPositiveStability will generally just read as this
        // domain edge — a limitation of the wall-sided model, not a computed hump.
        private const double GzChartMaxDeg = 30.0;

        #region Hull / loading inputs (shared across damage cases)

        private double _lpp = 160.0;
        /// <summary>Length between perpendiculars (m).</summary>
        public double Lpp { get => _lpp; set { if (SetField(ref _lpp, value)) Recalculate(); } }

        private double _bwl = 26.0;
        /// <summary>Waterline beam (m).</summary>
        public double Bwl { get => _bwl; set { if (SetField(ref _bwl, value)) Recalculate(); } }

        private double _depth = 14.0;
        /// <summary>Moulded depth (m) — used only for the deck-edge-immersion warning note.</summary>
        public double Depth { get => _depth; set { if (SetField(ref _depth, value)) Recalculate(); } }

        private double _intactDraftT = 9.5;
        /// <summary>Intact mean draft, T (m).</summary>
        public double IntactDraftT { get => _intactDraftT; set { if (SetField(ref _intactDraftT, value)) Recalculate(); } }

        private double _cb = 0.70;
        public double Cb { get => _cb; set { if (SetField(ref _cb, value)) Recalculate(); } }

        private double _cwp = 0.85;
        public double Cwp { get => _cwp; set { if (SetField(ref _cwp, value)) Recalculate(); } }

        private double _kg = 8.4;
        /// <summary>Height of centre of gravity above keel (m), assumed unchanged by the flooding.</summary>
        public double Kg { get => _kg; set { if (SetField(ref _kg, value)) Recalculate(); } }

        private double _displacement = 26000.0;
        /// <summary>Intact displacement, Δ (t, salt water).</summary>
        public double Displacement { get => _displacement; set { if (SetField(ref _displacement, value)) Recalculate(); } }

        #endregion

        #region Damage case manager

        public ObservableCollection<DamageCase> DamageCases { get; } = new();

        private DamageCase? _selectedCase;
        public DamageCase? SelectedCase
        {
            get => _selectedCase;
            set
            {
                if (ReferenceEquals(_selectedCase, value)) return;
                if (_selectedCase is not null) _selectedCase.PropertyChanged -= OnCaseChanged;
                _selectedCase = value;
                if (_selectedCase is not null) _selectedCase.PropertyChanged += OnCaseChanged;
                OnPropertyChanged(nameof(SelectedCase));
                Recalculate();
            }
        }

        private void OnCaseChanged(object? sender, PropertyChangedEventArgs e) => Recalculate();

        public void AddCase()
        {
            var basis = SelectedCase;
            var damageCase = new DamageCase
            {
                Name = $"Damage Case {DamageCases.Count + 1}",
                X1 = basis?.X1 ?? 0.0,
                X2 = basis?.X2 ?? Math.Min(Lpp, 20.0),
                Permeability = basis?.Permeability ?? 0.85,
                ProbabilityWeight = 1.0
            };
            DamageCases.Add(damageCase);
            SelectedCase = damageCase;
            Recalculate();
        }

        public void RemoveCase(DamageCase damageCase)
        {
            if (DamageCases.Count <= 1) return; // always keep at least one case
            int index = DamageCases.IndexOf(damageCase);
            if (index < 0) return;
            DamageCases.RemoveAt(index);
            if (ReferenceEquals(SelectedCase, damageCase))
            {
                int fallback = Math.Max(0, index - 1);
                SelectedCase = DamageCases.Count > 0 ? DamageCases[fallback] : null;
            }
            Recalculate();
        }

        #endregion

        #region Selected-case results

        private double _damagedDraftTd;
        public double DamagedDraftTd { get => _damagedDraftTd; private set => SetField(ref _damagedDraftTd, value); }

        private double _sinkage;
        /// <summary>Parallel sinkage, ΔT = Td − T (m).</summary>
        public double Sinkage { get => _sinkage; private set => SetField(ref _sinkage, value); }

        private double _kbDamaged;
        public double KbDamaged { get => _kbDamaged; private set => SetField(ref _kbDamaged, value); }

        private double _bmDamaged;
        public double BmDamaged { get => _bmDamaged; private set => SetField(ref _bmDamaged, value); }

        private double _kmDamaged;
        public double KmDamaged { get => _kmDamaged; private set => SetField(ref _kmDamaged, value); }

        private double _gmDamaged;
        public double GmDamaged { get => _gmDamaged; private set => SetField(ref _gmDamaged, value); }

        public bool GmDamagedIsPositive => GmDamaged >= 0;

        private double _maxGz;
        public double MaxGz { get => _maxGz; private set => SetField(ref _maxGz, value); }

        private double _maxGzAngle;
        public double MaxGzAngle { get => _maxGzAngle; private set => SetField(ref _maxGzAngle, value); }

        private double _rangeOfPositiveStability;
        public double RangeOfPositiveStability { get => _rangeOfPositiveStability; private set => SetField(ref _rangeOfPositiveStability, value); }

        /// <summary>True when the (upright, symmetric) damaged mean draft has reached the moulded depth — an approximate deck-edge-immersion warning.</summary>
        public bool DeckEdgeImmersed => Depth > 0 && DamagedDraftTd >= Depth;

        public PlotModel GzPlotModel { get; }

        #endregion

        #region Attained / required index

        public ObservableCollection<DamageCaseResult> CaseResults { get; } = new();

        private double _attainedIndex;
        public double AttainedIndex { get => _attainedIndex; private set => SetField(ref _attainedIndex, value); }

        private double _requiredIndex;
        public double RequiredIndex { get => _requiredIndex; private set => SetField(ref _requiredIndex, value); }

        private bool _attainedIndexPass;
        public bool AttainedIndexPass { get => _attainedIndexPass; private set => SetField(ref _attainedIndexPass, value); }

        public string IndexStatusText => AttainedIndexPass ? "PASS" : "FAIL";

        #endregion

        public DamageStabilityViewModel()
        {
            GzPlotModel = BuildPlotModel();

            // Sync hull particulars from Ship Builder first so the default damage
            // cases below are seeded against the correct Lpp.
            SyncFromShipBuilder();
            ShipBuilderViewModel.Instance.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ShipBuilderViewModel.Lpp):
                    case nameof(ShipBuilderViewModel.Breadth):
                    case nameof(ShipBuilderViewModel.Depth):
                    case nameof(ShipBuilderViewModel.Draft):
                    case nameof(ShipBuilderViewModel.Cb):
                    case nameof(ShipBuilderViewModel.Cwp):
                    case nameof(ShipBuilderViewModel.Displacement):
                        SyncFromShipBuilder();
                        break;
                }
            };

            SeedDefaultCases();
            SelectedCase = DamageCases.Count > 0 ? DamageCases[0] : null;
        }

        private void SyncFromShipBuilder()
        {
            var sb = ShipBuilderViewModel.Instance;
            _lpp = sb.Lpp;
            _bwl = sb.Breadth;
            _depth = sb.Depth;
            _intactDraftT = sb.Draft;
            _cb = sb.Cb;
            _cwp = sb.Cwp;
            _displacement = sb.Displacement;

            // Fire property-changed for all synced inputs so the UI reflects the new values.
            OnPropertyChanged(nameof(Lpp));
            OnPropertyChanged(nameof(Bwl));
            OnPropertyChanged(nameof(Depth));
            OnPropertyChanged(nameof(IntactDraftT));
            OnPropertyChanged(nameof(Cb));
            OnPropertyChanged(nameof(Cwp));
            OnPropertyChanged(nameof(Displacement));

            Recalculate();
        }

        private void SeedDefaultCases()
        {
            DamageCases.Add(new DamageCase { Name = "Fwd Peak / Collision Bulkhead", X1 = 0.0, X2 = 0.08 * Lpp, Permeability = 0.95, ProbabilityWeight = 0.8 });
            DamageCases.Add(new DamageCase { Name = "Cargo Hold No.1", X1 = 0.08 * Lpp, X2 = 0.30 * Lpp, Permeability = 0.95, ProbabilityWeight = 1.4 });
            DamageCases.Add(new DamageCase { Name = "Engine Room", X1 = 0.55 * Lpp, X2 = 0.75 * Lpp, Permeability = 0.85, ProbabilityWeight = 1.2 });
            DamageCases.Add(new DamageCase { Name = "Aft Peak", X1 = 0.92 * Lpp, X2 = Lpp, Permeability = 0.95, ProbabilityWeight = 0.6 });
        }

        /// <summary>
        /// Computes damaged hydrostatics/stability metrics for a single case from the current
        /// hull inputs, without mutating any of the "currently displayed" (selected-case) fields.
        /// Used both for the selected-case display and for the all-cases attained-index loop.
        /// </summary>
        private (double Td, double DeltaT, double Kbd, double Bmd, double Kmd, double Gmd, double MaxGz, double MaxGzAngle, double Range, double SFactor) ComputeCaseMetrics(DamageCase c)
        {
            double lx = Math.Max(0.0, c.X2 - c.X1);

            double awp = Cwp * Lpp * Bwl;
            double v = lx * Bwl * IntactDraftT * c.Permeability;
            double awpD = Math.Max(0.15 * awp, awp * (1.0 - c.Permeability * lx / Lpp));
            double deltaT = awpD > 0 ? v / awpD : 0.0;
            double td = IntactDraftT + deltaT;

            double deltaIT = c.Permeability * Math.Pow(Bwl, 3) * lx / 12.0;
            double volDisp = Displacement / SeawaterDensity;
            double transverseInertia = Lpp * Math.Pow(Bwl, 3) * (0.1216 * Cwp - 0.0410);
            double bmd = volDisp > 0 ? Math.Max(0.0, (transverseInertia - deltaIT) / volDisp) : 0.0;

            double kbd = td * (5.0 / 6.0 - Cb / (3.0 * Cwp));
            double kmd = kbd + bmd;
            double gmd = kmd - Kg;

            double GzLocal(double deg)
            {
                double rad = deg * Math.PI / 180.0;
                double tan = Math.Tan(rad);
                return (gmd + 0.5 * bmd * tan * tan) * Math.Sin(rad);
            }

            double maxGz = double.NegativeInfinity, maxAngle = 0.0;
            for (double a = 0; a <= GzChartMaxDeg; a += 0.5)
            {
                double gz = GzLocal(a);
                if (gz > maxGz) { maxGz = gz; maxAngle = a; }
            }

            double range = 0.0;
            if (maxGz > 0)
            {
                range = GzChartMaxDeg;
                double prevA = maxAngle, prevGz = GzLocal(maxAngle);
                for (double a = maxAngle + 0.25; a <= GzChartMaxDeg; a += 0.25)
                {
                    double gz = GzLocal(a);
                    if (prevGz > 0 && gz <= 0)
                    {
                        double t = (prevGz - gz) != 0 ? prevGz / (prevGz - gz) : 0.0;
                        range = prevA + t * (a - prevA);
                        break;
                    }
                    prevA = a; prevGz = gz;
                }
            }

            double sFactor = Math.Clamp(Math.Sqrt(Math.Max(0.0, maxGz) / 0.12) * Math.Sqrt(Math.Max(0.0, range) / 16.0), 0.0, 1.0);
            if (gmd < 0.05) sFactor = 0.0;

            return (td, deltaT, kbd, bmd, kmd, gmd, maxGz, maxAngle, range, sFactor);
        }

        private double GzAtAngle(double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double tan = Math.Tan(rad);
            return (GmDamaged + 0.5 * BmDamaged * tan * tan) * Math.Sin(rad);
        }

        private void Recalculate()
        {
            var selected = SelectedCase;
            bool validHull = Lpp > 0 && Bwl > 0 && IntactDraftT > 0 && Cb > 0 && Cwp > 0 && Displacement > 0;

            if (validHull && selected is not null)
            {
                var m = ComputeCaseMetrics(selected);
                DamagedDraftTd = m.Td;
                Sinkage = m.DeltaT;
                KbDamaged = m.Kbd;
                BmDamaged = m.Bmd;
                KmDamaged = m.Kmd;
                GmDamaged = m.Gmd;
            }
            else
            {
                DamagedDraftTd = 0.0;
                Sinkage = 0.0;
                KbDamaged = 0.0;
                BmDamaged = 0.0;
                KmDamaged = 0.0;
                GmDamaged = 0.0;
            }
            OnPropertyChanged(nameof(GmDamagedIsPositive));
            OnPropertyChanged(nameof(DeckEdgeImmersed));

            RebuildGzCurve();
            RebuildCaseResults();
        }

        private void RebuildGzCurve()
        {
            var series = (LineSeries)GzPlotModel.Series[0];
            series.Points.Clear();

            double bestGz = double.NegativeInfinity, bestAngle = 0.0;
            for (double a = 0; a <= GzChartMaxDeg; a += 0.5)
            {
                double gz = GzAtAngle(a);
                series.Points.Add(new DataPoint(a, gz));
                if (gz > bestGz) { bestGz = gz; bestAngle = a; }
            }
            MaxGz = bestGz;
            MaxGzAngle = bestAngle;

            double range = 0.0;
            if (bestGz > 0)
            {
                range = GzChartMaxDeg;
                double prevA = bestAngle, prevGz = GzAtAngle(bestAngle);
                for (double a = bestAngle + 0.25; a <= GzChartMaxDeg; a += 0.25)
                {
                    double gz = GzAtAngle(a);
                    if (prevGz > 0 && gz <= 0)
                    {
                        double t = (prevGz - gz) != 0 ? prevGz / (prevGz - gz) : 0.0;
                        range = prevA + t * (a - prevA);
                        break;
                    }
                    prevA = a; prevGz = gz;
                }
            }
            RangeOfPositiveStability = range;

            GzPlotModel.Title = $"Residual GZ (m) — {SelectedCase?.Name ?? "—"}";
            GzPlotModel.InvalidatePlot(true);
        }

        private void RebuildCaseResults()
        {
            CaseResults.Clear();

            double totalWeight = DamageCases.Sum(c => c.ProbabilityWeight);
            bool validHull = Lpp > 0 && Bwl > 0 && IntactDraftT > 0 && Cb > 0 && Cwp > 0 && Displacement > 0;

            double attained = 0.0;
            foreach (var c in DamageCases)
            {
                double pi = totalWeight > 0 ? c.ProbabilityWeight / totalWeight : 0.0;
                double td = 0.0, gmd = 0.0, maxGz = 0.0, range = 0.0, si = 0.0;

                if (validHull)
                {
                    var m = ComputeCaseMetrics(c);
                    td = m.Td;
                    gmd = m.Gmd;
                    maxGz = m.MaxGz;
                    range = m.Range;
                    si = m.SFactor;
                }

                attained += pi * si;

                CaseResults.Add(new DamageCaseResult
                {
                    CaseName = c.Name,
                    Td = td,
                    Gmd = gmd,
                    MaxGz = maxGz,
                    Range = range,
                    SFactor = si,
                    Pi = pi
                });
            }

            AttainedIndex = attained;
            RequiredIndex = Lpp > 0 ? 1.0 - 1.0 / (1.0 + Lpp / 100.0) : 0.0;
            AttainedIndexPass = AttainedIndex >= RequiredIndex;
            OnPropertyChanged(nameof(IndexStatusText));
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
                TitleColor = lightText,
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
