namespace NavalArchitectureSuite.Models
{
    /// <summary>One row of the IMO 2008 IS Code Part A, Reg 2.2 intact stability check.</summary>
    public class HydrostaticsCriterion
    {
        public string Name { get; set; } = string.Empty;
        public string Requirement { get; set; } = string.Empty;
        public string ActualText { get; set; } = string.Empty;
        public bool IsPass { get; set; }
        public string StatusText => IsPass ? "PASS" : "FAIL";
    }
}
