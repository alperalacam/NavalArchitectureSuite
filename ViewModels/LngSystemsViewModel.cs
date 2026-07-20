using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NavalArchitectureSuite.Models;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// LNG Systems calculation engine — cargo containment thermal performance and
    /// boil-off gas (BOG) management for gas carriers / LNG-fuelled ships.
    ///
    /// Scope and simplifications:
    ///   - Heat ingress is modelled as a single lumped U·A·ΔT term (U = k / insulation
    ///     thickness) across one effective wetted surface area — real cargo tanks have
    ///     multiple insulation zones (bottom, side, dome, pump tower penetrations) and
    ///     this collapses them into one representative value, appropriate for
    ///     first-pass/teaching estimates, not a detailed heat-balance calculation.
    ///   - Wetted surface area, when not otherwise known, is auto-estimated from tank
    ///     volume using a sphere-equivalent area scaled by a 1.15 shape factor to
    ///     approximate the larger surface-to-volume ratio of membrane/prismatic tanks
    ///     versus a true sphere (Moss tanks are closer to the unscaled sphere case).
    ///   - Boil-off rate follows directly from heat ingress and methane's latent heat
    ///     of vaporization at atmospheric pressure; it ignores pressure-rise effects,
    ///     sloshing-induced boil-off, and nitrogen/heavier-hydrocarbon content of real
    ///     LNG (composition is treated as pure methane for LHV/latent-heat purposes).
    ///   - The FGSS (Fuel Gas Supply System) coverage ratio compares natural BOG energy
    ///     to propulsion + auxiliary demand; it does not model actual gas combustion
    ///     unit / compressor / reliquefaction plant sizing, only whether natural BOG
    ///     alone could in principle satisfy demand.
    ///   - Cooldown LNG consumption is a rule-of-thumb allowance (typical industry
    ///     figure of ~0.15%/hour of tank capacity over a ~10-hour cooldown), not a
    ///     computed thermal-mass integration of the tank structure.
    ///   - The IGC Code checklist reflects representative clause references for
    ///     awareness/teaching purposes; it is not a substitute for the full IGC Code
    ///     or a class society's approval process.
    /// </summary>
    public class LngSystemsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // ---- Standard LNG reference constants ----
        private const double LngDensity = 450.0;                 // kg/m^3, saturated liquid at -162 degC, typical
        private const double LngLhv = 49.0;                      // MJ/kg, lower heating value, methane-dominant
        private const double LatentHeatOfVaporization = 511.0;   // kJ/kg, methane at atmospheric boil point
        private const double CargoTempC = -162.0;                // degC
        private const double AmbientTempC = 20.0;                // degC, design ambient

        #region Containment / tank inputs

        public ObservableCollection<string> ContainmentTypes { get; } = new()
        {
            "Membrane (GTT Mark III / NO96)",
            "Moss Spherical (IHI-SPB/Kvaerner)",
            "Independent Type C (Prismatic)"
        };

        private string _containmentType = "Membrane (GTT Mark III / NO96)";
        public string ContainmentType
        {
            get => _containmentType;
            set { if (SetField(ref _containmentType, value)) Recalculate(); }
        }

        private double _cargoTankCapacity_m3 = 174000.0;
        public double CargoTankCapacity_m3
        {
            get => _cargoTankCapacity_m3;
            set { if (SetField(ref _cargoTankCapacity_m3, value)) Recalculate(); }
        }

        private double _insulationThickness_mm = 300.0;
        public double InsulationThickness_mm
        {
            get => _insulationThickness_mm;
            set { if (SetField(ref _insulationThickness_mm, value)) Recalculate(); }
        }

        private double _insulationConductivity_W_mK = 0.032;
        public double InsulationConductivity_W_mK
        {
            get => _insulationConductivity_W_mK;
            set { if (SetField(ref _insulationConductivity_W_mK, value)) Recalculate(); }
        }

        #endregion

        #region Fuel / voyage inputs

        private double _dailyFuelConsumption_ME_tpd = 120.0;
        public double DailyFuelConsumption_ME_tpd
        {
            get => _dailyFuelConsumption_ME_tpd;
            set { if (SetField(ref _dailyFuelConsumption_ME_tpd, value)) Recalculate(); }
        }

        private double _dailyFuelConsumption_AE_tpd = 8.0;
        public double DailyFuelConsumption_AE_tpd
        {
            get => _dailyFuelConsumption_AE_tpd;
            set { if (SetField(ref _dailyFuelConsumption_AE_tpd, value)) Recalculate(); }
        }

        private double _voyageDurationDays = 18.0;
        public double VoyageDurationDays
        {
            get => _voyageDurationDays;
            set { if (SetField(ref _voyageDurationDays, value)) Recalculate(); }
        }

        #endregion

        #region Computed outputs

        private double _wettedSurfaceArea_m2;
        public double WettedSurfaceArea_m2 { get => _wettedSurfaceArea_m2; private set => SetField(ref _wettedSurfaceArea_m2, value); }

        private double _heatIngressQ_kW;
        public double HeatIngressQ_kW { get => _heatIngressQ_kW; private set => SetField(ref _heatIngressQ_kW, value); }

        private double _bogRate_kg_per_day;
        public double BogRate_kg_per_day { get => _bogRate_kg_per_day; private set => SetField(ref _bogRate_kg_per_day, value); }

        private double _bogRate_m3_per_day;
        public double BogRate_m3_per_day { get => _bogRate_m3_per_day; private set => SetField(ref _bogRate_m3_per_day, value); }

        private double _bogRatePercentPerDay;
        public double BogRatePercentPerDay { get => _bogRatePercentPerDay; private set => SetField(ref _bogRatePercentPerDay, value); }

        private double _bogEnergy_MJ_per_day;
        public double BogEnergy_MJ_per_day { get => _bogEnergy_MJ_per_day; private set => SetField(ref _bogEnergy_MJ_per_day, value); }

        private double _fuelRequired_kg_per_day;
        public double FuelRequired_kg_per_day { get => _fuelRequired_kg_per_day; private set => SetField(ref _fuelRequired_kg_per_day, value); }

        private double _fuelRequired_MJ_per_day;
        public double FuelRequired_MJ_per_day { get => _fuelRequired_MJ_per_day; private set => SetField(ref _fuelRequired_MJ_per_day, value); }

        private double _bogCoverageRatio;
        public double BogCoverageRatio { get => _bogCoverageRatio; private set => SetField(ref _bogCoverageRatio, value); }

        private string _fgssMode = string.Empty;
        public string FgssMode { get => _fgssMode; private set => SetField(ref _fgssMode, value); }

        private double _bogTotal_m3;
        public double BogTotal_m3 { get => _bogTotal_m3; private set => SetField(ref _bogTotal_m3, value); }

        private double _bogLossPercentOfCargo;
        public double BogLossPercentOfCargo { get => _bogLossPercentOfCargo; private set => SetField(ref _bogLossPercentOfCargo, value); }

        private double _estimatedCooldownLng_m3;
        public double EstimatedCooldownLng_m3 { get => _estimatedCooldownLng_m3; private set => SetField(ref _estimatedCooldownLng_m3, value); }

        public ObservableCollection<HydrostaticsCriterion> BogCriteria { get; } = new();
        public ObservableCollection<HydrostaticsCriterion> IgcChecklist { get; } = new();

        #endregion

        public LngSystemsViewModel()
        {
            Recalculate();
        }

        private void Recalculate()
        {
            bool valid = CargoTankCapacity_m3 > 0 && InsulationThickness_mm > 0 && InsulationConductivity_W_mK > 0;

            // Sphere-equivalent wetted area, scaled up for non-spherical containment shapes.
            WettedSurfaceArea_m2 = CargoTankCapacity_m3 > 0
                ? 4.0 * Math.PI * Math.Pow(3.0 * CargoTankCapacity_m3 / (4.0 * Math.PI), 2.0 / 3.0) * 1.15
                : 0.0;

            // Q = U * A * dT, U = k / thickness(m)
            double uValue = valid ? InsulationConductivity_W_mK / (InsulationThickness_mm / 1000.0) : 0.0;
            HeatIngressQ_kW = valid
                ? uValue * WettedSurfaceArea_m2 * (AmbientTempC - CargoTempC) / 1000.0
                : 0.0;

            BogRate_kg_per_day = HeatIngressQ_kW * 86400.0 / LatentHeatOfVaporization;
            BogRate_m3_per_day = BogRate_kg_per_day / LngDensity;
            BogRatePercentPerDay = CargoTankCapacity_m3 > 0 ? (BogRate_m3_per_day / CargoTankCapacity_m3) * 100.0 : 0.0;

            BogEnergy_MJ_per_day = BogRate_kg_per_day * LngLhv;

            FuelRequired_kg_per_day = (DailyFuelConsumption_ME_tpd + DailyFuelConsumption_AE_tpd) * 1000.0;
            FuelRequired_MJ_per_day = FuelRequired_kg_per_day * LngLhv;

            BogCoverageRatio = FuelRequired_MJ_per_day > 0 ? BogEnergy_MJ_per_day / FuelRequired_MJ_per_day : 0.0;
            FgssMode = BogCoverageRatio >= 1.0
                ? "Boil-off sustains full load (natural BOG mode)"
                : $"Forced vaporization required — natural BOG covers {BogCoverageRatio:P0} of demand";

            BogTotal_m3 = BogRate_m3_per_day * VoyageDurationDays;
            BogLossPercentOfCargo = CargoTankCapacity_m3 > 0 ? (BogTotal_m3 / CargoTankCapacity_m3) * 100.0 : 0.0;

            // Rule-of-thumb: ~0.15% of cargo capacity per hour of cooldown, ~10h reference duration.
            EstimatedCooldownLng_m3 = CargoTankCapacity_m3 * 0.0015 * 10.0;

            RebuildBogCriteria();
            RebuildIgcChecklist();
        }

        private void RebuildBogCriteria()
        {
            BogCriteria.Clear();
            BogCriteria.Add(new HydrostaticsCriterion
            {
                Name = "Boil-off Rate",
                Requirement = "0.05% – 0.15% /day (typical modern containment)",
                ActualText = $"{BogRatePercentPerDay:F3} %/day",
                IsPass = BogRatePercentPerDay >= 0.05 && BogRatePercentPerDay <= 0.20
            });
        }

        private void RebuildIgcChecklist()
        {
            IgcChecklist.Clear();

            bool isTypeC = ContainmentType.Contains("Type C");
            IgcChecklist.Add(new HydrostaticsCriterion
            {
                Name = "Cargo containment secondary barrier",
                Requirement = "IGC Ch.4 — required unless Type C (independent) tanks",
                ActualText = isTypeC
                    ? "Not required (Type C / independent tank)"
                    : "Required — full or partial secondary barrier per containment type",
                IsPass = true
            });
            IgcChecklist.Add(new HydrostaticsCriterion
            {
                Name = "Hold space inerting/atmosphere control",
                Requirement = "IGC Ch.9",
                ActualText = "Inert gas (N2) or dry air per containment system",
                IsPass = true
            });
            IgcChecklist.Add(new HydrostaticsCriterion
            {
                Name = "Gas detection in hold & interbarrier spaces",
                Requirement = "IGC Ch.13",
                ActualText = "Continuous monitoring required",
                IsPass = true
            });
            IgcChecklist.Add(new HydrostaticsCriterion
            {
                Name = "Emergency shutdown (ESD) system",
                Requirement = "IGC Ch.13",
                ActualText = "Required for cargo & FGSS",
                IsPass = true
            });
            IgcChecklist.Add(new HydrostaticsCriterion
            {
                Name = "Cargo tank relief valve setting",
                Requirement = "IGC Ch.8",
                ActualText = "Per tank design vapour pressure (MARVS)",
                IsPass = true
            });
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
