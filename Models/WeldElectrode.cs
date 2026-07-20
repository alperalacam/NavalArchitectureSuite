using System;

namespace NavalArchitectureSuite.Models
{
    /// <summary>
    /// One structural steel grade's welding reference data: matching AWS filler-metal classification,
    /// minimum preheat temperature, and a short procedural note.
    /// </summary>
    public record WeldElectrodeOption(string Grade, string AwsClassification, double MinPreheatC, string Notes);

    /// <summary>
    /// Static reference data for common hull-structural steel grades (as designated by IACS UR W —
    /// Grade A ordinary-strength steel, and the AH/DH/EH higher-strength grades) and their typical
    /// matching weld consumable classification and minimum preheat temperature.
    ///
    /// This is teaching-level reference data only, condensed from commonly published guidance
    /// (AWS D1.1 preheat tables and shipyard practice for carbon-manganese hull steels). Actual
    /// consumable and preheat selection for a real vessel must follow the applicable classification
    /// society rules and the shipyard's approved Welding Procedure Specification / Procedure
    /// Qualification Record (WPS/PQR) — carbon equivalent, restraint, and ambient conditions all
    /// shift the required preheat beyond what a simple grade lookup can capture.
    /// </summary>
    public static class WeldReferenceData
    {
        public static readonly WeldElectrodeOption[] Grades =
        {
            new("Mild Steel (A)",  "E7018 / E71T-1",       0.0,  "Ordinary-strength hull steel (Re ~235 MPa). No preheat normally required up to ~25 mm in temperate conditions."),
            new("AH32 / DH32",     "E7018-1 / E80T1-Ni1",  10.0, "Higher-strength hull steel (Re ~315 MPa). Light preheat recommended for thicker sections, high restraint, or cold ambient conditions."),
            new("AH36 / DH36",     "E8018-C1 / E81T1-Ni1", 20.0, "Higher-strength hull steel (Re ~355 MPa) — the most widely used HT hull grade. Preheat per thickness and carbon equivalent (CE)."),
            new("EH36",            "E9018-M / E91T1-K2",   50.0, "Higher-strength hull steel with improved low-temperature notch toughness. Heavier sections need higher preheat and strict low-hydrogen practice."),
        };

        public static readonly string[] GradeNames = Array.ConvertAll(Grades, g => g.Grade);

        public static WeldElectrodeOption Lookup(int index) =>
            (index >= 0 && index < Grades.Length) ? Grades[index] : Grades[0];

        public static WeldElectrodeOption Lookup(string grade)
        {
            foreach (var option in Grades)
            {
                if (option.Grade == grade) return option;
            }
            return Grades[0];
        }
    }
}
