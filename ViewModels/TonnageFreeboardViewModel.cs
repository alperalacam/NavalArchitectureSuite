using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NavalArchitectureSuite.Models;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Tonnage and Freeboard calculation engine for the Tonnage and Freeboard module.
    ///
    /// Scope, in standard simplified/teaching form:
    ///   PART 1 — 1969 International Convention on Tonnage Measurement of Ships (ITC69):
    ///     Gross Tonnage GT = K1·V (V = total volume of all enclosed spaces, m^3)
    ///     Net Tonnage NT from cargo-space volume Vc, draft/depth ratio and passenger
    ///     numbers, floored at 0.30·GT per the Convention.
    ///   PART 2 — International Convention on Load Lines 1966 / 1988 Protocol (ICLL),
    ///     Type "B" ship (unmodified), standard (unrestricted) freeboard:
    ///     Table freeboard from ICLL Annex I Table B (linearly interpolated between
    ///     standard tabulated anchor points), corrected for block coefficient, depth,
    ///     superstructure/trunk effective length and sheer, then checked against the
    ///     minimum bow height requirement (Reg 39) and used to derive the six load
    ///     line mark positions (S, W, WNA, T, F, TF).
    ///
    /// This is a teaching-toolkit implementation, not a certified tonnage/freeboard
    /// calculation. Several regulatory tables and 2D curves have been reduced to
    /// single-variable linear approximations — each simplification is documented
    /// at the point it is applied below. Results should not be used for statutory
    /// certificates.
    /// </summary>
    public class TonnageFreeboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        #region Part 1 — ITC69 Tonnage inputs

        private double _totalEnclosedVolume = 42000.0;
        /// <summary>V — total volume of all enclosed spaces of the ship (m^3).</summary>
        public double TotalEnclosedVolume_m3 { get => _totalEnclosedVolume; set { if (SetField(ref _totalEnclosedVolume, value)) Recalculate(); } }

        private double _cargoSpacesVolume = 31000.0;
        /// <summary>Vc — total volume of cargo spaces (m^3).</summary>
        public double CargoSpacesVolume_m3 { get => _cargoSpacesVolume; set { if (SetField(ref _cargoSpacesVolume, value)) Recalculate(); } }

        private double _summerDraft = 9.2;
        /// <summary>d — moulded summer draft (m), used in the NT depth-ratio term.</summary>
        public double SummerDraft_d { get => _summerDraft; set { if (SetField(ref _summerDraft, value)) Recalculate(); } }

        private double _mouldedDepth = 14.5;
        /// <summary>D — moulded depth amidships (m), used in the NT depth-ratio term.</summary>
        public double MouldedDepth_D { get => _mouldedDepth; set { if (SetField(ref _mouldedDepth, value)) Recalculate(); } }

        private double _passengersN1;
        /// <summary>N1 — number of passengers in cabins with no more than 8 berths.</summary>
        public double Passengers_N1 { get => _passengersN1; set { if (SetField(ref _passengersN1, value)) Recalculate(); } }

        private double _passengersN2;
        /// <summary>N2 — number of other passengers.</summary>
        public double Passengers_N2 { get => _passengersN2; set { if (SetField(ref _passengersN2, value)) Recalculate(); } }

        #endregion

        #region Part 1 — ITC69 Tonnage outputs

        private double _k1;
        public double K1 { get => _k1; private set => SetField(ref _k1, value); }

        private double _grossTonnage;
        public double GrossTonnage { get => _grossTonnage; private set => SetField(ref _grossTonnage, value); }

        private double _k2;
        public double K2 { get => _k2; private set => SetField(ref _k2, value); }

        private double _k3;
        public double K3 { get => _k3; private set => SetField(ref _k3, value); }

        private double _depthFactorSq;
        public double DepthFactorSq { get => _depthFactorSq; private set => SetField(ref _depthFactorSq, value); }

        private double _netTonnage;
        public double NetTonnage { get => _netTonnage; private set => SetField(ref _netTonnage, value); }

        #endregion

        #region Part 2 — ICLL Freeboard inputs

        private double _lfb = 165.0;
        /// <summary>Lfb — freeboard length (m).</summary>
        public double Lfb { get => _lfb; set { if (SetField(ref _lfb, value)) Recalculate(); } }

        private double _bfb = 27.0;
        /// <summary>Bfb — freeboard beam (m).</summary>
        public double Bfb { get => _bfb; set { if (SetField(ref _bfb, value)) Recalculate(); } }

        private double _freeboardDepthD = 14.2;
        /// <summary>D — moulded depth for freeboard purposes (m).</summary>
        public double FreeboardDepth_D { get => _freeboardDepthD; set { if (SetField(ref _freeboardDepthD, value)) Recalculate(); } }

        private double _cbFb = 0.70;
        /// <summary>Cb at 0.85D, used for the block coefficient correction.</summary>
        public double Cb_fb { get => _cbFb; set { if (SetField(ref _cbFb, value)) Recalculate(); } }

        private double _superstructureLengthPct;
        /// <summary>Effective superstructure/trunk length credited, as % of Lfb.</summary>
        public double SuperstructureLength_pct { get => _superstructureLengthPct; set { if (SetField(ref _superstructureLengthPct, value)) Recalculate(); } }

        private double _sheerCorrectionMm;
        /// <summary>Net standard sheer deficiency (-) / excess (+) correction, mm.</summary>
        public double SheerCorrection_mm { get => _sheerCorrectionMm; set { if (SetField(ref _sheerCorrectionMm, value)) Recalculate(); } }

        private double _bowHeightActual = 8.5;
        /// <summary>Actual bow height above the summer load waterline (m).</summary>
        public double BowHeight_actual_m { get => _bowHeightActual; set { if (SetField(ref _bowHeightActual, value)) Recalculate(); } }

        private double _summerDisplacement = 45000.0;
        /// <summary>Displacement at the summer load waterline (t), for the fresh water allowance.</summary>
        public double SummerDisplacement_t { get => _summerDisplacement; set { if (SetField(ref _summerDisplacement, value)) Recalculate(); } }

        private double _tpc = 28.0;
        /// <summary>Tonnes per centimetre immersion at the summer draft.</summary>
        public double TPC { get => _tpc; set { if (SetField(ref _tpc, value)) Recalculate(); } }

        #endregion

        #region Part 2 — ICLL Freeboard outputs

        private double _tableFreeboardMm;
        public double TableFreeboard_mm { get => _tableFreeboardMm; private set => SetField(ref _tableFreeboardMm, value); }

        private double _cbCorrectionFactor = 1.0;
        public double CbCorrectionFactor { get => _cbCorrectionFactor; private set => SetField(ref _cbCorrectionFactor, value); }

        private double _tableFreeboardAfterCbMm;
        public double TableFreeboardAfterCb_mm { get => _tableFreeboardAfterCbMm; private set => SetField(ref _tableFreeboardAfterCbMm, value); }

        private double _depthCorrectionMm;
        public double DepthCorrection_mm { get => _depthCorrectionMm; private set => SetField(ref _depthCorrectionMm, value); }

        private double _superstructureDeductionMm;
        public double SuperstructureDeduction_mm { get => _superstructureDeductionMm; private set => SetField(ref _superstructureDeductionMm, value); }

        private double _freeboardBeforeMinCheckMm;
        public double FreeboardBeforeMinimumCheck_mm { get => _freeboardBeforeMinCheckMm; private set => SetField(ref _freeboardBeforeMinCheckMm, value); }

        private double _minimumFreeboardMm;
        public double MinimumFreeboard_mm { get => _minimumFreeboardMm; private set => SetField(ref _minimumFreeboardMm, value); }

        private double _assignedFreeboardM;
        public double AssignedFreeboard_m { get => _assignedFreeboardM; private set => SetField(ref _assignedFreeboardM, value); }

        private double _requiredBowHeightMm;
        public double RequiredBowHeight_mm { get => _requiredBowHeightMm; private set => SetField(ref _requiredBowHeightMm, value); }

        public ObservableCollection<HydrostaticsCriterion> FreeboardCriteria { get; } = new();

        // Load line mark positions (freeboard from the deck line, metres).
        private double _summerMarkM;
        public double SummerMark_m { get => _summerMarkM; private set => SetField(ref _summerMarkM, value); }

        private double _winterMarkM;
        public double WinterMark_m { get => _winterMarkM; private set => SetField(ref _winterMarkM, value); }

        private double _winterNaMarkM;
        public double WinterNAMark_m { get => _winterNaMarkM; private set => SetField(ref _winterNaMarkM, value); }

        private double _tropicalMarkM;
        public double TropicalMark_m { get => _tropicalMarkM; private set => SetField(ref _tropicalMarkM, value); }

        private double _freshWaterMarkM;
        public double FreshWaterMark_m { get => _freshWaterMarkM; private set => SetField(ref _freshWaterMarkM, value); }

        private double _tropicalFreshWaterMarkM;
        public double TropicalFreshWaterMark_m { get => _tropicalFreshWaterMarkM; private set => SetField(ref _tropicalFreshWaterMarkM, value); }

        #endregion

        // ICLL Annex I Table B — standard Type "B" tabular freeboard, sampled at every
        // 5th published row (L in metres, freeboard in mm). Linear interpolation between
        // these anchor points is an accepted engineering approximation since the real
        // table is itself close to piecewise-linear over short spans of L.
        private static readonly (double x, double y)[] TypeBFreeboardTable =
        {
            (24, 200), (50, 483), (75, 822), (100, 1206), (125, 1608), (150, 2032),
            (175, 2478), (200, 2882), (215, 3075), (250, 3496), (275, 3766),
            (300, 4000), (325, 4216), (350, 4392), (365, 4500)
        };

        public TonnageFreeboardViewModel()
        {
            Recalculate();
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

        private void Recalculate()
        {
            RecalculateTonnage();
            RecalculateFreeboard();
        }

        private void RecalculateTonnage()
        {
            double v = Math.Max(TotalEnclosedVolume_m3, 0.0);
            double vc = Math.Max(CargoSpacesVolume_m3, 0.0);

            K1 = 0.2 + 0.02 * Math.Log10(Math.Max(v, 1.0));
            GrossTonnage = K1 * v;

            K2 = 0.2 + 0.02 * Math.Log10(Math.Max(vc, 1.0));
            K3 = 1.25 * (GrossTonnage + 10000.0) / 10000.0;

            // Depth/draft ratio term is capped at 1.0 per the Convention (a ship cannot
            // be credited for a draft greater than 2/3 of its depth in this formula).
            double depthFactor = MouldedDepth_D > 0 ? Math.Min(1.0, 4.0 * SummerDraft_d / (3.0 * MouldedDepth_D)) : 0.0;
            DepthFactorSq = depthFactor * depthFactor;

            double ntRaw = K2 * vc * DepthFactorSq + K3 * (Passengers_N1 + Passengers_N2 / 10.0);

            // NT shall not be taken less than 0.30 GT (ITC69 Reg 4).
            NetTonnage = Math.Max(ntRaw, 0.30 * GrossTonnage);
        }

        private void RecalculateFreeboard()
        {
            double lfb = Math.Max(Lfb, 0.0);

            TableFreeboard_mm = Interpolate(TypeBFreeboardTable, lfb);

            // 1. Block coefficient correction (multiplicative), Cb > 0.68 only.
            CbCorrectionFactor = Cb_fb > 0.68 ? (Cb_fb + 0.68) / 1.36 : 1.0;
            TableFreeboardAfterCb_mm = TableFreeboard_mm * CbCorrectionFactor;

            // 2. Depth correction. R is capped at 250 for L > 120 m; the same R is used
            // for both freeboard-increasing and freeboard-reducing depth deviations, a
            // simplification for this teaching tool (the full Convention applies a
            // different reduced correction for superstructures below the reduction
            // threshold, which is not modelled here).
            double r = lfb <= 120.0 ? lfb / 0.48 : 250.0;
            DepthCorrection_mm = (FreeboardDepth_D - lfb / 15.0) * r;

            // 3. Superstructure/trunk deduction — simplified single-tier proportional
            // approximation of ICLL Annex I Table for superstructure/trunk deductions
            // (the real table is a 2D function of effective length ratio E/L and number
            // of tiers). At 100% effective length the deduction is taken as ~50% of the
            // (Cb-corrected) table freeboard, scaling linearly with the credited %.
            SuperstructureDeduction_mm = (SuperstructureLength_pct / 100.0) * 0.5 * TableFreeboardAfterCb_mm;

            // 4. Sheer correction (user-supplied net value; deriving the standard sheer
            // profile itself is out of scope for this tool).
            FreeboardBeforeMinimumCheck_mm = TableFreeboardAfterCb_mm + DepthCorrection_mm - SuperstructureDeduction_mm + SheerCorrection_mm;

            // Nominal floor to avoid nonsense negative results in this simplified tool.
            MinimumFreeboard_mm = Math.Max(FreeboardBeforeMinimumCheck_mm, 50.0);
            AssignedFreeboard_m = MinimumFreeboard_mm / 1000.0;

            // Minimum bow height, ICLL Reg 39, Type B.
            double requiredBowHeightMm;
            if (lfb <= 250.0)
            {
                requiredBowHeightMm = 56.0 * lfb * (1.36 - lfb / 500.0);
                if (Cb_fb > 0.68) requiredBowHeightMm *= CbCorrectionFactor;
            }
            else
            {
                requiredBowHeightMm = 7000.0;
            }
            RequiredBowHeight_mm = requiredBowHeightMm;

            double actualBowHeightMm = BowHeight_actual_m * 1000.0;

            FreeboardCriteria.Clear();
            FreeboardCriteria.Add(new HydrostaticsCriterion
            {
                Name = "Minimum Bow Height (Reg 39)",
                Requirement = $"≥ {RequiredBowHeight_mm:F0} mm",
                ActualText = $"{actualBowHeightMm:F0} mm",
                IsPass = actualBowHeightMm >= RequiredBowHeight_mm
            });

            RecalculateLoadLineMarks();
        }

        private void RecalculateLoadLineMarks()
        {
            double winterDelta_m = AssignedFreeboard_m / 48.0;
            double freshWaterDelta_m = TPC > 0 ? SummerDisplacement_t / (4.0 * TPC * 100.0) : 0.0;

            SummerMark_m = AssignedFreeboard_m;
            WinterMark_m = SummerMark_m + winterDelta_m;
            WinterNAMark_m = Lfb <= 100.0 ? WinterMark_m + 0.050 : WinterMark_m;
            TropicalMark_m = SummerMark_m - winterDelta_m;
            FreshWaterMark_m = SummerMark_m - freshWaterDelta_m;
            TropicalFreshWaterMark_m = TropicalMark_m - freshWaterDelta_m;
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
