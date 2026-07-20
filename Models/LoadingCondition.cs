using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NavalArchitectureSuite.Models
{
    /// <summary>
    /// One entry in the Stability module's loading condition manager
    /// (e.g. Light Ship, Full Load Departure, Ballast Arrival).
    /// Editable in place, so it implements INotifyPropertyChanged directly
    /// rather than being a plain record like GzPoint/HydrostaticsCriterion.
    /// </summary>
    public class LoadingCondition : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _name = "New Condition";
        public string Name { get => _name; set => SetField(ref _name, value); }

        private double _displacement = 10000.0;
        /// <summary>Displacement, Δ (tonnes, salt water) for this loading condition.</summary>
        public double Displacement { get => _displacement; set => SetField(ref _displacement, value); }

        private double _kg = 8.0;
        /// <summary>Solid KG — height of the condition's centre of gravity above keel (m), before free surface correction.</summary>
        public double Kg { get => _kg; set => SetField(ref _kg, value); }

        private double _freeSurfaceMoment = 0.0;
        /// <summary>Total free surface moment of all slack tanks for this condition (t·m).</summary>
        public double FreeSurfaceMoment { get => _freeSurfaceMoment; set => SetField(ref _freeSurfaceMoment, value); }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
