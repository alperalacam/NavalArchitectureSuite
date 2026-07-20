namespace NavalArchitectureSuite.Models
{
    /// <summary>One read-only row of the Damage Stability module's per-case attained-index table.</summary>
    public class DamageCaseResult
    {
        public string CaseName { get; set; } = string.Empty;
        /// <summary>Damaged mean draft, Td (m).</summary>
        public double Td { get; set; }
        /// <summary>Damaged metacentric height, GMd (m).</summary>
        public double Gmd { get; set; }
        /// <summary>Maximum residual righting arm on the damaged GZ curve (m).</summary>
        public double MaxGz { get; set; }
        /// <summary>Range of positive residual stability (deg).</summary>
        public double Range { get; set; }
        /// <summary>Simplified single-case SOLAS-style survivability factor, si (0-1).</summary>
        public double SFactor { get; set; }
        /// <summary>Normalized probability weight of this case among the case list, pi (sums to 1 across all cases).</summary>
        public double Pi { get; set; }
    }
}
