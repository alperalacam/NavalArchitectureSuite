using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NavalArchitectureSuite.Models
{
    /// <summary>
    /// One entry in the Reports module's document/revision index (e.g. "Intact Stability
    /// Booklet", "Hydrostatic Particulars"). Editable in place, so it implements
    /// INotifyPropertyChanged directly rather than being a plain record.
    /// </summary>
    public class ReportEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _title = "New Report";
        public string Title { get => _title; set => SetField(ref _title, value); }

        /// <summary>Source module the report relates to (freeform, e.g. "Hydrostatics", "Stability").</summary>
        private string _module = "Hydrostatics";
        public string Module { get => _module; set => SetField(ref _module, value); }

        private string _revision = "A";
        public string Revision { get => _revision; set => SetField(ref _revision, value); }

        private string _preparedBy = string.Empty;
        public string PreparedBy { get => _preparedBy; set => SetField(ref _preparedBy, value); }

        /// <summary>ISO 8601 date string (yyyy-MM-dd) — kept as text so it is directly editable/exportable.</summary>
        private string _dateIso = DateTime.Today.ToString("yyyy-MM-dd");
        public string DateIso { get => _dateIso; set => SetField(ref _dateIso, value); }

        /// <summary>Freeform status, typically one of "Draft", "Issued for Review", "Approved".</summary>
        private string _status = "Draft";
        public string Status { get => _status; set => SetField(ref _status, value); }

        private string _notes = string.Empty;
        public string Notes { get => _notes; set => SetField(ref _notes, value); }

        private bool _includeInExport = true;
        public bool IncludeInExport { get => _includeInExport; set => SetField(ref _includeInExport, value); }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
