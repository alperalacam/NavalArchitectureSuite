using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Bow Design module — entry angle, Kracht (1978) bulbous bow design,
    /// IACS UR A1 anchor equipment, and SOLAS collision bulkhead position.
    /// All principal dimension inputs are read from ShipBuilderViewModel.Instance.
    /// </summary>
    public sealed class BowDesignViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // ── Constructor ──────────────────────────────────────────────────────
        public BowDesignViewModel()
        {
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
                    case nameof(ShipBuilderViewModel.Cm):
                    case nameof(ShipBuilderViewModel.Cwp):
                    case nameof(ShipBuilderViewModel.Displacement):
                    case nameof(ShipBuilderViewModel.DesignSpeed):
                        SyncFromShipBuilder();
                        break;
                }
            };
            Recalculate();
        }

        private void SyncFromShipBuilder()
        {
            var sb = ShipBuilderViewModel.Instance;
            _lpp         = sb.Lpp;
            _breadth     = sb.Breadth;
            _depth       = sb.Depth;
            _draft       = sb.Draft;
            _cb          = sb.Cb;
            _cm          = sb.Cm;
            _cwp         = sb.Cwp;
            _displacement = sb.Displacement;
            _designSpeed  = sb.DesignSpeed;

            OnPropertyChanged(nameof(Lpp));
            OnPropertyChanged(nameof(Breadth));
            OnPropertyChanged(nameof(Depth));
            OnPropertyChanged(nameof(Draft));
            OnPropertyChanged(nameof(Cb));
            OnPropertyChanged(nameof(Cm));
            OnPropertyChanged(nameof(Cwp));
            OnPropertyChanged(nameof(Displacement));
            OnPropertyChanged(nameof(DesignSpeed));

            Recalculate();
        }

        // ── Synced inputs from ShipBuilder ───────────────────────────────────
        private double _lpp;
        public double Lpp { get => _lpp; private set => SetField(ref _lpp, value); }

        private double _breadth;
        public double Breadth { get => _breadth; private set => SetField(ref _breadth, value); }

        private double _depth;
        public double Depth { get => _depth; private set => SetField(ref _depth, value); }

        private double _draft;
        public double Draft { get => _draft; private set => SetField(ref _draft, value); }

        private double _cb;
        public double Cb { get => _cb; private set => SetField(ref _cb, value); }

        private double _cm;
        public double Cm { get => _cm; private set => SetField(ref _cm, value); }

        private double _cwp;
        public double Cwp { get => _cwp; private set => SetField(ref _cwp, value); }

        private double _displacement;
        public double Displacement { get => _displacement; private set => SetField(ref _displacement, value); }

        private double _designSpeed;
        public double DesignSpeed { get => _designSpeed; private set => SetField(ref _designSpeed, value); }

        // ── Kracht (1978) bulb parameter inputs ──────────────────────────────
        private double _cBB = 0.18;
        /// <summary>Breadth parameter C_BB = B_B / B_WL. Typical: 0.10–0.25.</summary>
        public double CBB
        {
            get => _cBB;
            set { if (SetField(ref _cBB, Math.Clamp(value, 0.05, 0.35))) Recalculate(); }
        }

        private double _cBL = 0.035;
        /// <summary>Length parameter C_BL = L_B / Lpp. Typical: 0.02–0.06.</summary>
        public double CBL
        {
            get => _cBL;
            set { if (SetField(ref _cBL, Math.Clamp(value, 0.01, 0.10))) Recalculate(); }
        }

        private double _cBT = 0.075;
        /// <summary>Transverse area parameter C_BT = A_BT / (B_WL × T). Typical: 0.05–0.12.</summary>
        public double CBT
        {
            get => _cBT;
            set { if (SetField(ref _cBT, Math.Clamp(value, 0.02, 0.18))) Recalculate(); }
        }

        private double _cBV = 0.006;
        /// <summary>Volume parameter C_BV. Typical: 0.003–0.012.</summary>
        public double CBV
        {
            get => _cBV;
            set { if (SetField(ref _cBV, Math.Clamp(value, 0.001, 0.020))) Recalculate(); }
        }

        private double _cBPo = 0.45;
        /// <summary>Position parameter C_BPo = z_B / T. Typical: 0.30–0.60.</summary>
        public double CBPo
        {
            get => _cBPo;
            set { if (SetField(ref _cBPo, Math.Clamp(value, 0.20, 0.70))) Recalculate(); }
        }

        // ── Anchor equipment extra inputs ────────────────────────────────────
        private double _freeboard = 3.5;
        /// <summary>Height from SLWL to top of uppermost deckhouse, m.</summary>
        public double Freeboard
        {
            get => _freeboard;
            set { if (SetField(ref _freeboard, Math.Max(0.5, value))) Recalculate(); }
        }

        private double _lateralWindageArea = 1200.0;
        /// <summary>Lateral projected area above SLWL, m².</summary>
        public double LateralWindageArea
        {
            get => _lateralWindageArea;
            set { if (SetField(ref _lateralWindageArea, Math.Max(10.0, value))) Recalculate(); }
        }

        // ── Computed: Bow Geometry ────────────────────────────────────────────
        private double _froudeNumber;
        public double FroudeNumber { get => _froudeNumber; private set => SetField(ref _froudeNumber, value); }

        private double _cp;
        public double Cp { get => _cp; private set => SetField(ref _cp, value); }

        private double _halfEntryAngleDeg;
        /// <summary>Half entry angle at bow waterplane (degrees). Kracht 1978 approximation.</summary>
        public double HalfEntryAngleDeg { get => _halfEntryAngleDeg; private set => SetField(ref _halfEntryAngleDeg, value); }

        private double _fullEntryAngleDeg;
        public double FullEntryAngleDeg { get => _fullEntryAngleDeg; private set => SetField(ref _fullEntryAngleDeg, value); }

        private string _entryAngleAssessment = string.Empty;
        public string EntryAngleAssessment { get => _entryAngleAssessment; private set => SetField(ref _entryAngleAssessment, value); }

        private string _froudeRegime = string.Empty;
        public string FroudeRegime { get => _froudeRegime; private set => SetField(ref _froudeRegime, value); }

        private string _recommendedStemType = string.Empty;
        public string RecommendedStemType { get => _recommendedStemType; private set => SetField(ref _recommendedStemType, value); }

        private string _recommendedBulbType = string.Empty;
        public string RecommendedBulbType { get => _recommendedBulbType; private set => SetField(ref _recommendedBulbType, value); }

        // ── Computed: Bulbous Bow (Kracht 1978) ──────────────────────────────
        private double _bWL;
        /// <summary>Waterplane breadth at FP ≈ B × Cwp (m).</summary>
        public double BWL { get => _bWL; private set => SetField(ref _bWL, value); }

        private double _bulbBreadth;
        /// <summary>Maximum bulb breadth B_B = C_BB × B_WL (m).</summary>
        public double BulbBreadth { get => _bulbBreadth; private set => SetField(ref _bulbBreadth, value); }

        private double _bulbLength;
        /// <summary>Bulb protrusion forward of FP L_B = C_BL × Lpp (m).</summary>
        public double BulbLength { get => _bulbLength; private set => SetField(ref _bulbLength, value); }

        private double _aBT;
        /// <summary>Bulb transverse area at FP A_BT = C_BT × B_WL × T (m²). Direct input to Holtrop-Mennen.</summary>
        public double ABT { get => _aBT; private set => SetField(ref _aBT, value); }

        private double _bulbVolume;
        /// <summary>Bulb volume V_B = C_BV × 0.5 × Lpp × B_WL × T (m³).</summary>
        public double BulbVolume { get => _bulbVolume; private set => SetField(ref _bulbVolume, value); }

        private double _zB;
        /// <summary>Bulb centroid height above keel z_B = C_BPo × T (m). Direct input to Holtrop-Mennen as h_B.</summary>
        public double ZB { get => _zB; private set => SetField(ref _zB, value); }

        private string _bulbSubmergenceStatus = string.Empty;
        public string BulbSubmergenceStatus { get => _bulbSubmergenceStatus; private set => SetField(ref _bulbSubmergenceStatus, value); }

        private string _ballastDraughtStatus = string.Empty;
        public string BallastDraughtStatus { get => _ballastDraughtStatus; private set => SetField(ref _ballastDraughtStatus, value); }

        // Holtrop (1984) bulb resistance
        private double _fPB;
        /// <summary>Bulb immersion parameter f_pb (Holtrop 1984 Eq.11).</summary>
        public double FPB { get => _fPB; private set => SetField(ref _fPB, value); }

        private double _rB;
        /// <summary>Bulb wave-breaking resistance R_B in kN (Holtrop 1984 Eq.12).</summary>
        public double RB { get => _rB; private set => SetField(ref _rB, value); }

        // ── Computed: SOLAS Collision Bulkhead ───────────────────────────────
        private double _cbMinDist;
        public double CollisionBulkheadMin { get => _cbMinDist; private set => SetField(ref _cbMinDist, value); }

        private double _cbMaxDist;
        public double CollisionBulkheadMax { get => _cbMaxDist; private set => SetField(ref _cbMaxDist, value); }

        private double _cbRecommended;
        public double CollisionBulkheadRecommended { get => _cbRecommended; private set => SetField(ref _cbRecommended, value); }

        // ── Computed: IACS UR A1 Anchor Equipment ────────────────────────────
        private double _equipmentNumber;
        public double EquipmentNumber { get => _equipmentNumber; private set => SetField(ref _equipmentNumber, value); }

        private double _anchorMassKg;
        public double AnchorMassKg { get => _anchorMassKg; private set => SetField(ref _anchorMassKg, value); }

        private double _anchorMassTonnes;
        public double AnchorMassTonnes { get => _anchorMassTonnes; private set => SetField(ref _anchorMassTonnes, value); }

        private double _chainDiameterMm;
        public double ChainDiameterMm { get => _chainDiameterMm; private set => SetField(ref _chainDiameterMm, value); }

        private double _chainLengthM;
        public double ChainLengthM { get => _chainLengthM; private set => SetField(ref _chainLengthM, value); }

        private int _chainShots;
        public int ChainShots { get => _chainShots; private set => SetField(ref _chainShots, value); }

        private double _chainBreakingLoadKN;
        public double ChainBreakingLoadKN { get => _chainBreakingLoadKN; private set => SetField(ref _chainBreakingLoadKN, value); }

        private double _hawsePipeDiameterMm;
        public double HawsePipeDiameterMm { get => _hawsePipeDiameterMm; private set => SetField(ref _hawsePipeDiameterMm, value); }

        // ── Recalculate ───────────────────────────────────────────────────────
        private void Recalculate()
        {
            const double g    = 9.81;
            const double rhoSW = 1.025;

            if (_lpp <= 0 || _breadth <= 0 || _draft <= 0) return;

            // ── Bow Geometry ─────────────────────────────────────────────────
            double Vm = _designSpeed * 0.5144;
            FroudeNumber = Vm / Math.Sqrt(g * _lpp);

            // Cp from Cb / Cm
            Cp = _cm > 0 ? _cb / _cm : _cb;

            // Half entry angle — Kracht (1978) approximation: alpha_E ≈ 125 × (1 - Cwp)² × (Cwp / Cp)
            // More robust form: from waterplane fullness
            double alphERad = Math.Atan((_breadth * (1.0 - _cwp)) / (0.5 * _lpp));
            HalfEntryAngleDeg = Math.Round(alphERad * 180.0 / Math.PI * (0.9 + 0.6 * Cp), 2);
            FullEntryAngleDeg = Math.Round(HalfEntryAngleDeg * 2.0, 2);

            EntryAngleAssessment = HalfEntryAngleDeg < 10
                ? "FINE ENTRY — good resistance, verify seakeeping forward"
                : HalfEntryAngleDeg < 20
                    ? "MODERATE ENTRY — balanced resistance and seakeeping"
                    : "FULL ENTRY — check wave-making resistance at design speed";

            FroudeRegime = FroudeNumber < 0.15
                ? "Fn < 0.15 — very slow speed — bulb benefit marginal"
                : FroudeNumber < 0.18
                    ? "Fn 0.15–0.18 — low speed — small bulb (D-type) may help"
                    : FroudeNumber < 0.25
                        ? "Fn 0.18–0.25 — optimum bulb range — O or NABLA type recommended"
                        : "Fn > 0.25 — higher speed — NABLA or AX-type bulb, fine entry";

            RecommendedStemType = FroudeNumber > 0.25
                ? "Raked stem (10–20° forward) — container/ferry type"
                : Cp < 0.62
                    ? "X-bow or raked stem — OSV/offshore type"
                    : "Vertical or slightly raked stem — cargo/tanker type";

            RecommendedBulbType = FroudeNumber < 0.18
                ? "D-type (delta) — best at low Fn"
                : FroudeNumber < 0.25
                    ? "O-type (cylindrical) — best at moderate Fn"
                    : "NABLA-type (inverted pear) — best at higher Fn";

            // ── Bulbous Bow (Kracht 1978) ────────────────────────────────────
            BWL = _breadth * _cwp;

            BulbBreadth = _cBB * BWL;
            BulbLength  = _cBL * _lpp;
            ABT         = _cBT * BWL * _draft;
            BulbVolume  = _cBV * 0.5 * _lpp * BWL * _draft;
            ZB          = _cBPo * _draft;

            // Submergence checks
            double bulbTop    = ZB + BulbBreadth / 2.0;
            double bulbBottom = ZB - BulbBreadth / 2.0;
            double ballastDraft = _draft * 0.60;

            BulbSubmergenceStatus = bulbTop < _draft
                ? $"✓ FULLY SUBMERGED — top of bulb at {bulbTop:F2} m < T {_draft:F2} m"
                : $"⚠ BULB EMERGING — top of bulb at {bulbTop:F2} m exceeds T {_draft:F2} m — reduce C_BPo";

            BallastDraughtStatus = bulbBottom > 0 && ZB < ballastDraft
                ? $"✓ SUBMERGED IN BALLAST — centroid at {ZB:F2} m < ballast T {ballastDraft:F2} m"
                : $"⚠ BULB MAY EMERGE IN BALLAST — check ballast condition (60% T = {ballastDraft:F2} m)";

            // Holtrop (1984) f_pb and R_B
            double denomFPB = _draft - 1.5 * ZB;
            if (Math.Abs(denomFPB) > 0.01 && ABT > 0)
            {
                FPB = 0.56 * Math.Sqrt(ABT) / denomFPB;
                double fnI = Vm / Math.Sqrt(g * (_draft - ZB - 0.25 * Math.Sqrt(ABT)) + 0.15 * Vm * Vm);
                RB = 0.11 * Math.Exp(-3.0 / (FPB * FPB)) * Math.Pow(fnI, 3)
                     * Math.Pow(ABT, 1.5) * rhoSW * g / (1.0 + fnI * fnI) / 1000.0; // kN
            }
            else { FPB = 0; RB = 0; }

            // ── SOLAS Collision Bulkhead (Ch.II-1 Reg.11) ───────────────────
            CollisionBulkheadMin         = Math.Round(0.05 * _lpp, 2);
            CollisionBulkheadMax         = _lpp <= 200.0
                ? Math.Round(0.08 * _lpp, 2)
                : Math.Round(0.05 * _lpp + 3.0, 2);
            CollisionBulkheadRecommended = Math.Round((CollisionBulkheadMin + CollisionBulkheadMax) / 2.0, 2);

            // ── IACS UR A1 Anchor Equipment ──────────────────────────────────
            // EN = Δ^(2/3) + 2 × B × h + A_lat/10
            double h = _depth + _freeboard;   // approx height from SLWL to top of house
            EquipmentNumber = Math.Pow(_displacement, 2.0 / 3.0)
                            + 2.0 * _breadth * h
                            + _lateralWindageArea / 10.0;

            double en = EquipmentNumber;

            // Power-law regression fitted to IACS UR A1 (2019) Table 1 (28 data points):
            // Anchor: a=1.2527, b=1.0196  | Chain diam: a=1.4209, b=0.5003
            // Chain length: a=26.8585, b=0.2692
            AnchorMassKg       = Math.Round(1.2527 * Math.Pow(en, 1.0196), 0);
            AnchorMassTonnes   = Math.Round(AnchorMassKg / 1000.0, 2);
            ChainDiameterMm    = Math.Round(1.4209 * Math.Pow(en, 0.5003), 1);
            ChainLengthM       = Math.Round(26.859 * Math.Pow(en, 0.2692), 1);
            ChainShots         = (int)Math.Ceiling(ChainLengthM / 27.5);

            // IACS UR W22 (2019) Grade K2 stud link breaking load:
            // BL = 0.0223 x d^2 x (44 - 0.08 x d)  kN  (d in mm)
            // Constants already incorporate gravity — result is directly in kN.
            // Source: IACS UR W22 (2019) Table 2.
            double d = ChainDiameterMm;
            ChainBreakingLoadKN = Math.Round(0.0223 * d * d * (44.0 - 0.08 * d), 0);
            HawsePipeDiameterMm = Math.Round(1.8 * d, 1);
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void OnPropertyChanged(string? name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
