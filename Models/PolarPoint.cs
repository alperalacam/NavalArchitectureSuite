namespace NavalArchitectureSuite.Models
{
    /// <summary>One True-Wind-Angle / boat-speed sample of the simplified VPP polar table.</summary>
    public class PolarPoint
    {
        public double TrueWindAngleDeg { get; set; }
        public double BoatSpeedKts { get; set; }
    }
}
