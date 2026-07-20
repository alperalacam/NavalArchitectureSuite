using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NavalArchitectureSuite.Models;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Preliminary weld joint sizing and WPS/NDT reference for the Welding module.
    ///
    /// Scope and simplifications (generic structural-steel weld design practice, AWS D1.1-style,
    /// not a class-approval tool):
    ///   - Fillet welds (double- or single-continuous): sized by the simple strength-of-weld
    ///     throat-area method. Required total throat thickness falls out cleanly in millimetres
    ///     when the applied load P is expressed in kN per metre run and the allowable stress in
    ///     MPa (N/mm²), because 1 kN/m of run numerically equals 1 N/mm of run:
    ///         RequiredThroatThickness [mm] = (P [kN/m] * SafetyFactor) / τallow [MPa]
    ///     Double-continuous fillets split this evenly between the two welds; single-continuous
    ///     carries it on one weld. Leg size follows from the equal-leg throat ratio (a = 0.707·leg).
    ///   - A stepped minimum-leg-vs-thickness table (AWS D1.1 Table 5.8-style breakpoints: t≤6mm→
    ///     3mm, 6–13mm→5mm, 13–19mm→6mm, >19mm→8mm) is applied to the attached-plate thickness t1
    ///     as a simplification — the full AWS table technically keys off the thicker part joined.
    ///   - Butt welds (full penetration): checked as a plain net-section tensile stress, governed
    ///     by the thinner of the two plates (same clean unit derivation as above):
    ///         σ [MPa] = (P [kN/m] * SafetyFactor) / t_min [mm]
    ///     No leg-size calculation applies; full-penetration butt joints in primary structure are
    ///     assumed to require 100% volumetric NDT (RT or UT) per class-society practice.
    ///   - WPS reference data (AWS electrode classification, minimum preheat, recommended process)
    ///     is condensed teaching-level data for four representative hull-structural steel grades —
    ///     see Models/WeldElectrode.cs. It is indicative only, not a qualified WPS/PQR.
    ///   - Arc heat input: Q [kJ/mm] = (V·I·60) / (v·1000), with V in volts, I in amps and travel
    ///     speed v in mm/min (the standard arc-energy formula). No thermal-efficiency factor is
    ///     applied, so this is the "as-measured" heat input, not corrected per welding process.
    ///   - Minimum preheat temperature from carbon equivalent: Tp [°C] = 350·√(CE − 0.25) for
    ///     CE &gt; 0.25 (else no preheat indicated), taking the IIW carbon equivalent CE as a
    ///     direct input. This is a widely used quick-estimate formula for hydrogen-cracking
    ///     preheat, not a substitute for the full BS EN 1011-2 / AWS D1.1 Annex I method (which
    ///     also factors in diffusible-hydrogen scale, joint restraint and combined thickness).
    ///   - The NDT inspection plan shown is a fixed reference table (typical extents/methods by
    ///     joint class), not calculated from the inputs.
    ///   - Not modelled: weld-group eccentricity / combined (multi-axis) stress interaction,
    ///     fatigue, or distortion.
    /// </summary>
    public class WeldingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private const double ThroatToLegRatio = 0.707; // equal-leg fillet weld

        public static readonly string[] JointTypeNames =
        {
            "Butt Joint — Full Penetration",
            "Fillet Joint — Lap/Corner",
            "T-Joint — Fillet Weld"
        };

        public static readonly string[] SteelGradeNames = WeldReferenceData.GradeNames;

        #region Inputs

        private int _jointTypeIndex = 0;
        public int JointTypeIndex { get => _jointTypeIndex; set { if (SetField(ref _jointTypeIndex, value)) Recalculate(); } }

        private int _steelGradeIndex = 2; // AH36 / DH36 — the most common HT hull grade
        public int SteelGradeIndex { get => _steelGradeIndex; set { if (SetField(ref _steelGradeIndex, value)) Recalculate(); } }

        private double _plateThickness = 16.0; // mm, thinner/attached plate, t1
        public double PlateThickness { get => _plateThickness; set { if (SetField(ref _plateThickness, value)) Recalculate(); } }

        private double _secondPlateThickness = 20.0; // mm, t2 (butt joints — min of t1,t2 governs)
        public double SecondPlateThickness { get => _secondPlateThickness; set { if (SetField(ref _secondPlateThickness, value)) Recalculate(); } }

        private double _designLoad = 250.0; // kN/m run
        public double DesignLoad { get => _designLoad; set { if (SetField(ref _designLoad, value)) Recalculate(); } }

        private double _allowableShearStress = 100.0; // MPa, weld metal
        public double AllowableShearStress { get => _allowableShearStress; set { if (SetField(ref _allowableShearStress, value)) Recalculate(); } }

        private double _allowableTensileStress = 160.0; // MPa
        public double AllowableTensileStress { get => _allowableTensileStress; set { if (SetField(ref _allowableTensileStress, value)) Recalculate(); } }

        private double _safetyFactor = 1.0;
        public double SafetyFactor { get => _safetyFactor; set { if (SetField(ref _safetyFactor, value)) Recalculate(); } }

        #endregion

        #region Heat input & preheat inputs

        private double _voltage = 24.0; // V
        public double Voltage { get => _voltage; set { if (SetField(ref _voltage, value)) Recalculate(); } }

        private double _current = 220.0; // A
        public double Current { get => _current; set { if (SetField(ref _current, value)) Recalculate(); } }

        private double _travelSpeed = 300.0; // mm/min
        public double TravelSpeed { get => _travelSpeed; set { if (SetField(ref _travelSpeed, value)) Recalculate(); } }

        private double _carbonEquivalentCE = 0.38; // IIW CE = C + Mn/6 + (Cr+Mo+V)/5 + (Ni+Cu)/15
        public double CarbonEquivalentCE { get => _carbonEquivalentCE; set { if (SetField(ref _carbonEquivalentCE, value)) Recalculate(); } }

        #endregion

        #region Derived joint-type helpers

        private string JointType => JointTypeNames[Math.Clamp(JointTypeIndex, 0, JointTypeNames.Length - 1)];

        private bool _isFillet;
        public bool IsFillet { get => _isFillet; private set => SetField(ref _isFillet, value); }

        private bool _isButt;
        public bool IsButt { get => _isButt; private set => SetField(ref _isButt, value); }

        #endregion

        #region Fillet weld outputs

        private double _requiredThroatThickness;
        public double RequiredThroatThickness_mm { get => _requiredThroatThickness; private set => SetField(ref _requiredThroatThickness, value); }

        private double _requiredLegSize;
        public double RequiredLegSize_mm { get => _requiredLegSize; private set => SetField(ref _requiredLegSize, value); }

        private double _minLegByThickness;
        public double MinLegByThickness_mm { get => _minLegByThickness; private set => SetField(ref _minLegByThickness, value); }

        private double _governingLegSize;
        public double GoverningLegSize_mm { get => _governingLegSize; private set => SetField(ref _governingLegSize, value); }

        private double _weldUtilisation;
        /// <summary>RequiredLegSize / GoverningLegSize, as a fraction (1.0 = fully utilised).</summary>
        public double WeldUtilisation { get => _weldUtilisation; private set => SetField(ref _weldUtilisation, value); }

        #endregion

        #region Butt weld outputs

        private double _governingThickness;
        public double GoverningThicknessMm { get => _governingThickness; private set => SetField(ref _governingThickness, value); }

        private double _buttTensileStress;
        public double ButtTensileStress { get => _buttTensileStress; private set => SetField(ref _buttTensileStress, value); }

        #endregion

        #region Heat input & preheat outputs

        private double _heatInput;
        /// <summary>Arc heat input, Q = V·I·60 / (v·1000) (kJ/mm).</summary>
        public double HeatInput_kJmm { get => _heatInput; private set => SetField(ref _heatInput, value); }

        private double _preheatFromCE;
        /// <summary>Minimum preheat temperature estimated from carbon equivalent, Tp = 350·√(CE−0.25) (°C).</summary>
        public double PreheatFromCE_C { get => _preheatFromCE; private set => SetField(ref _preheatFromCE, value); }

        #endregion

        #region WPS reference outputs

        private string _wpsElectrode = string.Empty;
        public string WpsElectrode { get => _wpsElectrode; private set => SetField(ref _wpsElectrode, value); }

        private string _wpsMinPreheat = string.Empty;
        public string WpsMinPreheat { get => _wpsMinPreheat; private set => SetField(ref _wpsMinPreheat, value); }

        private string _wpsNotes = string.Empty;
        public string WpsNotes { get => _wpsNotes; private set => SetField(ref _wpsNotes, value); }

        private string _recommendedProcess = string.Empty;
        public string RecommendedProcess { get => _recommendedProcess; private set => SetField(ref _recommendedProcess, value); }

        #endregion

        public ObservableCollection<HydrostaticsCriterion> Criteria { get; } = new();

        /// <summary>Fixed reference table of typical NDT extent/method by joint and structural class. Not calculated from inputs.</summary>
        public ObservableCollection<HydrostaticsCriterion> NdtPlan { get; } = new();

        public WeldingViewModel()
        {
            SeedNdtPlan();
            Recalculate();
        }

        private void SeedNdtPlan()
        {
            NdtPlan.Add(new HydrostaticsCriterion { Name = "Butt Weld — Primary / Strength Structure", Requirement = "100%", ActualText = "RT or UT", IsPass = true });
            NdtPlan.Add(new HydrostaticsCriterion { Name = "Fillet Weld — Primary Structure", Requirement = "10–20% spot", ActualText = "MT or PT", IsPass = true });
            NdtPlan.Add(new HydrostaticsCriterion { Name = "Fillet Weld — Secondary / Non-Structural", Requirement = "Visual only", ActualText = "VT", IsPass = true });
        }

        private void Recalculate()
        {
            string jointType = JointType;
            bool isButt = jointType.StartsWith("Butt", StringComparison.Ordinal);
            bool isFillet = !isButt;
            IsFillet = isFillet;
            IsButt = isButt;

            double t1 = PlateThickness;
            double t2 = SecondPlateThickness > 0 ? SecondPlateThickness : PlateThickness;
            double tMin = Math.Min(t1, t2);
            GoverningThicknessMm = tMin;

            if (isFillet)
            {
                // Clean unit derivation: P[kN/m] numerically equals N per mm of run;
                // dividing by τallow[MPa]=[N/mm²] gives required throat thickness directly in mm.
                // Both Fillet and T-Joint options are sized as double-continuous fillet welds
                // (standard practice for primary hull structural connections), so the load
                // splits evenly between the two weld runs.
                double totalThroat = AllowableShearStress > 0 ? (DesignLoad * SafetyFactor) / AllowableShearStress : 0.0;
                double aPerWeld = totalThroat / 2.0;

                RequiredThroatThickness_mm = aPerWeld;
                RequiredLegSize_mm = aPerWeld / ThroatToLegRatio;

                // AWS D1.1 Table 5.8-style stepped minimum fillet leg size vs. plate thickness.
                MinLegByThickness_mm = t1 <= 6.0 ? 3.0 : t1 <= 13.0 ? 5.0 : t1 <= 19.0 ? 6.0 : 8.0;

                GoverningLegSize_mm = Math.Max(RequiredLegSize_mm, MinLegByThickness_mm);
                WeldUtilisation = GoverningLegSize_mm > 0 ? RequiredLegSize_mm / GoverningLegSize_mm : 0.0;

                ButtTensileStress = 0.0;
            }
            else
            {
                RequiredThroatThickness_mm = 0.0;
                RequiredLegSize_mm = 0.0;
                MinLegByThickness_mm = 0.0;
                GoverningLegSize_mm = 0.0;
                WeldUtilisation = 0.0;

                // Full-penetration butt weld: net-section tensile stress, same clean unit trick.
                ButtTensileStress = tMin > 0 ? (DesignLoad * SafetyFactor) / tMin : 0.0;
            }

            RecommendedProcess = tMin < 12.0
                ? "GMAW (semi-automatic) or SMAW"
                : tMin <= 25.0
                    ? "SAW (submerged arc) or FCAW"
                    : "SAW multi-pass with preheat";

            // Arc heat input, Q = V*I*60 / (v*1000), kJ/mm (v in mm/min).
            HeatInput_kJmm = TravelSpeed > 0 ? (Voltage * Current * 60.0) / (TravelSpeed * 1000.0) : 0.0;

            // Minimum preheat from carbon equivalent: Tp = 350*sqrt(CE-0.25), CE>0.25.
            PreheatFromCE_C = CarbonEquivalentCE > 0.25 ? 350.0 * Math.Sqrt(CarbonEquivalentCE - 0.25) : 0.0;

            RebuildCriteria(isFillet);
            RebuildWpsReference();
        }

        private void RebuildCriteria(bool isFillet)
        {
            Criteria.Clear();

            if (isFillet)
            {
                Criteria.Add(new HydrostaticsCriterion
                {
                    Name = "Required vs Minimum Leg",
                    Requirement = "≥ table minimum",
                    ActualText = $"{RequiredLegSize_mm:F2} mm req / {MinLegByThickness_mm:F1} mm min",
                    IsPass = true
                });
                Criteria.Add(new HydrostaticsCriterion
                {
                    Name = "Weld Utilisation",
                    Requirement = "≤ 1.00",
                    ActualText = $"{WeldUtilisation:F2}",
                    IsPass = WeldUtilisation <= 1.0
                });
            }
            else
            {
                Criteria.Add(new HydrostaticsCriterion
                {
                    Name = "Butt Weld Tensile Stress",
                    Requirement = $"≤ {AllowableTensileStress:F0} MPa",
                    ActualText = $"{ButtTensileStress:F1} MPa",
                    IsPass = ButtTensileStress <= AllowableTensileStress
                });
                Criteria.Add(new HydrostaticsCriterion
                {
                    Name = "NDT Requirement — Full Penetration",
                    Requirement = "100% volumetric",
                    ActualText = "RT or UT",
                    IsPass = true
                });
            }

            // Typical acceptable arc heat-input band for shipbuilding C-Mn/HT steels — too low risks
            // lack of fusion/hydrogen cracking, too high risks HAZ grain coarsening and toughness loss.
            Criteria.Add(new HydrostaticsCriterion
            {
                Name = "Heat Input, Q",
                Requirement = "0.5 – 2.5 kJ/mm",
                ActualText = $"{HeatInput_kJmm:F2} kJ/mm",
                IsPass = HeatInput_kJmm >= 0.5 && HeatInput_kJmm <= 2.5
            });
        }

        private void RebuildWpsReference()
        {
            var option = WeldReferenceData.Lookup(SteelGradeIndex);
            WpsElectrode = option.AwsClassification;
            WpsMinPreheat = option.MinPreheatC > 0 ? $"{option.MinPreheatC:F0} °C" : "None required";
            WpsNotes = option.Notes;
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
