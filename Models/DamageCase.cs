using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NavalArchitectureSuite.Models
{
    /// <summary>
    /// One entry in the Damage Stability module's damage-case manager
    /// (e.g. Fwd Peak, Cargo Hold No.1, Engine Room, Aft Peak).
    /// Editable in place, so it implements INotifyPropertyChanged directly
    /// rather than being a plain record like GzPoint/HydrostaticsCriterion.
    /// </summary>
    public class DamageCase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _name = "New Damage Case";
        public string Name { get => _name; set => SetField(ref _name, value); }

        private double _x1 = 0.0;
        /// <summary>Forward-most boundary of the damaged compartment, measured from AP (m).</summary>
        public double X1 { get => _x1; set => SetField(ref _x1, value); }

        private double _x2 = 10.0;
        /// <summary>Aft boundary of the damaged compartment, measured from AP (m).</summary>
        public double X2 { get => _x2; set => SetField(ref _x2, value); }

        private double _permeability = 0.85;
        /// <summary>
        /// Compartment permeability µ — fraction of the compartment's volume that can flood.
        /// Typical teaching values: 0.95 dry cargo/void, 0.85 accommodation/stores, 0.60 machinery spaces.
        /// </summary>
        public double Permeability { get => _permeability; set => SetField(ref _permeability, value); }

        private double _probabilityWeight = 1.0;
        /// <summary>Relative likelihood weight of this damage case among the case list, used to build a weighted-average attained index.</summary>
        public double ProbabilityWeight { get => _probabilityWeight; set => SetField(ref _probabilityWeight, value); }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
