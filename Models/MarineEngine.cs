namespace NavalArchitectureSuite.Models
{
    /// <summary>
    /// Representative marine diesel engine entry for the Machinery module engine selector.
    /// Data is indicative / teaching-level — always verify against the manufacturer's
    /// current project guide before use in a real design submission.
    /// </summary>
    public class MarineEngine
    {
        public string Manufacturer    { get; init; } = string.Empty;
        public string Model           { get; init; } = string.Empty;
        public string Category        { get; init; } = string.Empty;  // display group
        public string FuelType        { get; init; } = "HFO";
        public double McrKw           { get; init; }   // Maximum Continuous Rating, kW
        public double RatedRpm        { get; init; }   // rated shaft speed, rpm
        public double SfocAt100       { get; init; }   // SFOC at 100% MCR, g/kWh
        public double SfocMin         { get; init; }   // minimum SFOC, g/kWh
        public double LoadAtMinSfoc   { get; init; }   // % MCR at minimum SFOC
        public int    Cylinders       { get; init; }
        public string Stroke          { get; init; } = string.Empty;  // "2-stroke" / "4-stroke"
        public string Notes           { get; init; } = string.Empty;

        /// <summary>Display string shown in the ComboBox.</summary>
        public string DisplayName =>
            $"{Manufacturer} {Model}  —  {McrKw:N0} kW @ {RatedRpm:F0} rpm  ({Stroke}, {FuelType})";
    }
}
