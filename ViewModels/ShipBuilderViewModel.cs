using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;
using NavalArchitectureSuite.Models;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Implements the input and derived-value cells from:
    /// Naval_Architecture_Teaching_Toolkit_Vol1.xlsx,
    /// sheet "VESSEL PARAMETERS", cells C6, C11:C15, D19:D22 and C26:C34.
    /// Constants intentionally match the workbook formulas (1.025 t/m³,
    /// 0.5144 m/s per knot and g = 9.81 m/s²).
    /// </summary>
    public sealed class ShipBuilderViewModel : INotifyPropertyChanged
    {
        // Singleton – every module reads ship particulars from this one instance.
        public static readonly ShipBuilderViewModel Instance = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public static readonly string[] VesselTypeNames =
        {
            "Bulk Carrier", "Container Ship", "Tanker", "General Cargo Ship",
            "Gas Carrier (LNG)", "Gas Carrier (LPG)", "RoRo Vessel", "Passenger Ship",
            "Ferry", "Fishing Vessel", "Tug", "Offshore Support Vessel", "Naval Vessel"
        };

        private string _vesselType = "Bulk Carrier";
        private double _lpp = 180.0;
        private double _breadth = 30.0;
        private double _depth = 15.0;
        private double _draft = 11.5;
        private double _designSpeed = 14.5;
        private double _cb = 0.82;
        private double _cm = 0.99;
        private double _cwp = 0.88;
        private double _enteredCp = 0.83;

        public string VesselType
        {
            get => _vesselType;
            set => SetField(ref _vesselType, value);
        }

        public double Lpp
        {
            get => _lpp;
            set { if (SetField(ref _lpp, value)) Recalculate(); }
        }

        public double Breadth
        {
            get => _breadth;
            set { if (SetField(ref _breadth, value)) Recalculate(); }
        }

        public double Depth
        {
            get => _depth;
            set { if (SetField(ref _depth, value)) Recalculate(); }
        }

        public double Draft
        {
            get => _draft;
            set { if (SetField(ref _draft, value)) Recalculate(); }
        }

        public double DesignSpeed
        {
            get => _designSpeed;
            set { if (SetField(ref _designSpeed, value)) Recalculate(); }
        }

        public double Cb
        {
            get => _cb;
            set { if (SetField(ref _cb, value)) Recalculate(); }
        }

        public double Cm
        {
            get => _cm;
            set { if (SetField(ref _cm, value)) Recalculate(); }
        }

        public double Cwp
        {
            get => _cwp;
            set { if (SetField(ref _cwp, value)) Recalculate(); }
        }

        public double EnteredCp
        {
            get => _enteredCp;
            set => SetField(ref _enteredCp, value);
        }

        private double _volumeDisplacement;
        public double VolumeDisplacement
        {
            get => _volumeDisplacement;
            private set => SetField(ref _volumeDisplacement, value);
        }

        private double _displacement;
        public double Displacement
        {
            get => _displacement;
            private set => SetField(ref _displacement, value);
        }

        private double _lengthBreadthRatio;
        public double LengthBreadthRatio
        {
            get => _lengthBreadthRatio;
            private set => SetField(ref _lengthBreadthRatio, value);
        }

        private double _breadthDraftRatio;
        public double BreadthDraftRatio
        {
            get => _breadthDraftRatio;
            private set => SetField(ref _breadthDraftRatio, value);
        }

        private double _depthDraftRatio;
        public double DepthDraftRatio
        {
            get => _depthDraftRatio;
            private set => SetField(ref _depthDraftRatio, value);
        }

        private double _freeboard;
        public double Freeboard
        {
            get => _freeboard;
            private set => SetField(ref _freeboard, value);
        }

        private double _froudeNumber;
        public double FroudeNumber
        {
            get => _froudeNumber;
            private set => SetField(ref _froudeNumber, value);
        }

        private double _speedLengthRatio;
        public double SpeedLengthRatio
        {
            get => _speedLengthRatio;
            private set => SetField(ref _speedLengthRatio, value);
        }

        private double _calculatedCp;
        public double CalculatedCp
        {
            get => _calculatedCp;
            private set => SetField(ref _calculatedCp, value);
        }

        private Model3DGroup _hullModel = new();
        /// <summary>Parametric hull shell for the 3D preview viewport (HelixToolkit). See <see cref="HullMeshBuilder"/>.</summary>
        public Model3DGroup HullModel
        {
            get => _hullModel;
            private set => SetField(ref _hullModel, value);
        }

        // Orientation label positions for the 3D viewport's BillboardTextVisual3D markers.
        // Match HullMeshBuilder's convention: stern X=0, bow X=Lpp, centerline Y=0,
        // +Y=port, -Y=starboard (right-handed scene with +X=forward/bow, +Z=up, so the
        // true-right side facing the bow is -Y — see navigation light convention: PORT is
        // always LEFT (red) and STARBOARD is always RIGHT (green) facing forward), keel Z=0, deck Z=Depth.
        private Point3D _bowLabelPosition;
        public Point3D BowLabelPosition
        {
            get => _bowLabelPosition;
            private set => SetField(ref _bowLabelPosition, value);
        }

        private Point3D _sternLabelPosition;
        public Point3D SternLabelPosition
        {
            get => _sternLabelPosition;
            private set => SetField(ref _sternLabelPosition, value);
        }

        private Point3D _portLabelPosition;
        public Point3D PortLabelPosition
        {
            get => _portLabelPosition;
            private set => SetField(ref _portLabelPosition, value);
        }

        private Point3D _starboardLabelPosition;
        public Point3D StarboardLabelPosition
        {
            get => _starboardLabelPosition;
            private set => SetField(ref _starboardLabelPosition, value);
        }

        private double _heelAngle = 0.0;
        /// <summary>Applied heel angle in degrees (positive = port side down).</summary>
        public double HeelAngle
        {
            get => _heelAngle;
            set => SetField(ref _heelAngle, value);
        }

        public ShipBuilderViewModel() => Recalculate();

        private void Recalculate()
        {
            VolumeDisplacement = Lpp * Breadth * Draft * Cb;
            Displacement = VolumeDisplacement * 1.025;
            LengthBreadthRatio = Breadth != 0 ? Lpp / Breadth : 0;
            BreadthDraftRatio = Draft != 0 ? Breadth / Draft : 0;
            DepthDraftRatio = Draft != 0 ? Depth / Draft : 0;
            Freeboard = Depth - Draft;
            FroudeNumber = Lpp > 0
                ? DesignSpeed * 0.5144 / System.Math.Sqrt(9.81 * Lpp)
                : 0;
            SpeedLengthRatio = Lpp > 0 ? DesignSpeed / System.Math.Sqrt(Lpp) : 0;
            CalculatedCp = Cm != 0 ? Cb / Cm : 0;

            HullModel = HullMeshBuilder.BuildHull(Lpp, Breadth, Depth, Draft, Cb, Cm, Cwp);

            double halfBeam = Breadth / 2.0;
            BowLabelPosition = new Point3D(Lpp, 0, Depth * 1.15);
            SternLabelPosition = new Point3D(0, 0, Depth * 1.15);
            PortLabelPosition = new Point3D(Lpp / 2.0, halfBeam * 1.8, Depth);
            StarboardLabelPosition = new Point3D(Lpp / 2.0, -halfBeam * 1.8, Depth);
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
