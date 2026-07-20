using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using NavalArchitectureSuite.Models;
using NavalArchitectureSuite.Services;

namespace NavalArchitectureSuite.ViewModels
{
    /// <summary>
    /// Reports module — a document/revision compiler plus a calculation-results exporter.
    ///
    /// Reports itself has no in-memory project state of its own; it manages a local index
    /// of deliverable report entries (title, source module, revision, status, etc.) plus a
    /// small static formula-library audit summary. For the PDF's results tables it instead
    /// reads live figures from the other modules' ViewModel instances, handed in via
    /// <see cref="HydrostaticsSource"/>, <see cref="StabilitySource"/> and
    /// <see cref="ResistanceSource"/> (wired up by MainWindow, which keeps one persistent
    /// instance of those module views for the session instead of recreating them per nav
    /// click). ExportPdf() writes a real PDF file to disk via a SaveFileDialog, built with
    /// the dependency-free SimplePdfDocument writer.
    /// </summary>
    public class ReportsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Live reference to the Hydrostatics module, set by MainWindow. May be null if not yet wired.</summary>
        public HydrostaticsViewModel? HydrostaticsSource { get; set; }

        /// <summary>Live reference to the Stability module, set by MainWindow. May be null if not yet wired.</summary>
        public StabilityViewModel? StabilitySource { get; set; }

        /// <summary>Live reference to the Resistance and Propulsion module, set by MainWindow. May be null if not yet wired.</summary>
        public ResistancePropulsionViewModel? ResistanceSource { get; set; }

        #region Project title block

        private string _projectName = "MV Example Bulk Carrier — Newbuild 4201";
        public string ProjectName { get => _projectName; set => SetField(ref _projectName, value); }

        private string _projectNumber = "NA-2026-0042";
        public string ProjectNumber { get => _projectNumber; set => SetField(ref _projectNumber, value); }

        private string _client = "Example Shipping Co.";
        public string Client { get => _client; set => SetField(ref _client, value); }

        private string _classSociety = "DNV";
        public string ClassSociety { get => _classSociety; set => SetField(ref _classSociety, value); }

        private string _preparedByCompany = "Naval Architecture Engineering Suite";
        public string PreparedByCompany { get => _preparedByCompany; set => SetField(ref _preparedByCompany, value); }

        #endregion

        #region Report entries

        public ObservableCollection<ReportEntry> Entries { get; } = new();

        private ReportEntry? _selectedEntry;
        public ReportEntry? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (ReferenceEquals(_selectedEntry, value)) return;
                if (_selectedEntry is not null) _selectedEntry.PropertyChanged -= OnEntryPropertyChanged;
                _selectedEntry = value;
                if (_selectedEntry is not null) _selectedEntry.PropertyChanged += OnEntryPropertyChanged;
                OnPropertyChanged(nameof(SelectedEntry));
            }
        }

        public void AddEntry()
        {
            var basis = SelectedEntry;
            var entry = new ReportEntry
            {
                Title = $"New Report {Entries.Count + 1}",
                Module = basis?.Module ?? "Hydrostatics",
                Revision = "A",
                PreparedBy = basis?.PreparedBy ?? string.Empty,
                Status = "Draft"
            };
            Entries.Add(entry);
            SelectedEntry = entry;
        }

        public void RemoveEntry(ReportEntry entry)
        {
            if (Entries.Count <= 1) return; // always keep at least one entry
            int index = Entries.IndexOf(entry);
            if (index < 0) return;
            Entries.RemoveAt(index);
            if (ReferenceEquals(SelectedEntry, entry))
            {
                int fallback = Math.Max(0, index - 1);
                SelectedEntry = Entries.Count > 0 ? Entries[fallback] : null;
            }
        }

        private void Entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (ReportEntry item in e.OldItems)
                    item.PropertyChanged -= OnEntryPropertyChanged;
            }
            if (e.NewItems is not null)
            {
                foreach (ReportEntry item in e.NewItems)
                    item.PropertyChanged += OnEntryPropertyChanged;
            }
            UpdateExportSummary();
        }

        private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReportEntry.IncludeInExport))
                UpdateExportSummary();
        }

        #endregion

        #region Export summary stat tile

        private int _includedCount;
        public int IncludedCount { get => _includedCount; private set => SetField(ref _includedCount, value); }

        private int _totalCount;
        public int TotalCount { get => _totalCount; private set => SetField(ref _totalCount, value); }

        public string ExportSummaryText => $"{IncludedCount} of {TotalCount} entries included in export";

        private void UpdateExportSummary()
        {
            TotalCount = Entries.Count;
            IncludedCount = Entries.Count(e => e.IncludeInExport);
            OnPropertyChanged(nameof(ExportSummaryText));
        }

        #endregion

        #region Formula library audit

        public class ModuleFormulaCount
        {
            public string Module { get; set; } = string.Empty;
            public int FormulaCount { get; set; }
        }

        public ObservableCollection<ModuleFormulaCount> FormulaAudit { get; } = new();

        /// <summary>Sum of FormulaCount across FormulaAudit — the live-formula count across the suite.</summary>
        public int TotalFormulaCount => FormulaAudit.Sum(f => f.FormulaCount);

        #endregion

        #region Export

        private string _exportStatusMessage = string.Empty;
        public string ExportStatusMessage { get => _exportStatusMessage; set => SetField(ref _exportStatusMessage, value); }

        public void ExportPdf()
        {
            var dialog = new SaveFileDialog
            {
                FileName = $"{SanitizeFileName(ProjectNumber)}_Report_Index.pdf",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
                DefaultExt = ".pdf"
            };

            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                ExportStatusMessage = "Export cancelled";
                return;
            }

            try
            {
                byte[] pdf = BuildPdfReport();
                System.IO.File.WriteAllBytes(dialog.FileName, pdf);
                ExportStatusMessage = $"Exported to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                ExportStatusMessage = $"Export failed: {ex.Message}";
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        public byte[] BuildPdfReport()
        {
            var doc = new SimplePdfDocument();

            doc.AddLine(ProjectName, size: 16, bold: true);
            doc.AddSpacer(10);
            doc.AddLine($"Project Number: {ProjectNumber}");
            doc.AddLine($"Client: {Client}");
            doc.AddLine($"Classification Society: {ClassSociety}");
            doc.AddLine($"Prepared By: {PreparedByCompany}");
            doc.AddLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            doc.AddSpacer(14);

            AddStabilitySection(doc);
            AddHydrostaticsSection(doc);
            AddResistanceSection(doc);

            doc.AddLine("REPORT INDEX", size: 13, bold: true);
            doc.AddSpacer(6);
            foreach (var entry in Entries.Where(e => e.IncludeInExport))
            {
                doc.AddLine(entry.Title, size: 11, bold: true);
                doc.AddLine($"Module: {entry.Module}   Revision: {entry.Revision}   Status: {entry.Status}", size: 9);
                doc.AddLine($"Prepared By: {entry.PreparedBy}   Date: {entry.DateIso}", size: 9);
                if (!string.IsNullOrWhiteSpace(entry.Notes))
                    doc.AddWrapped($"Notes: {entry.Notes}", maxChars: 88, size: 9);
                doc.AddSpacer(10);
            }
            doc.AddRule();
            doc.AddSpacer(10);

            doc.AddLine("FORMULA LIBRARY AUDIT — LIVE FORMULAS BY MODULE", size: 13, bold: true);
            doc.AddSpacer(6);
            doc.AddTwoColumn("Module", "Formulas", totalChars: 60, size: 9, bold: true);
            doc.AddRule(60);
            foreach (var row in FormulaAudit)
                doc.AddTwoColumn(row.Module, row.FormulaCount.ToString(), totalChars: 60, size: 9);
            doc.AddRule(60);
            doc.AddTwoColumn("Total", TotalFormulaCount.ToString(), totalChars: 60, size: 9, bold: true);

            doc.AddSpacer(16);
            doc.AddLine("Generated by Naval Architecture Engineering Suite", size: 8);

            return doc.Build();
        }

        private void AddStabilitySection(SimplePdfDocument doc)
        {
            doc.AddLine("STABILITY — GZ CURVE & IMO CRITERIA", size: 13, bold: true);
            doc.AddSpacer(6);

            var stab = StabilitySource;
            if (stab is null)
            {
                doc.AddLine("Stability module data not available.", size: 9);
                doc.AddSpacer(10);
                doc.AddRule();
                doc.AddSpacer(10);
                return;
            }

            doc.AddLine($"Loading Condition: {stab.SelectedCondition?.Name ?? "(none selected)"}", size: 9);
            doc.AddSpacer(6);

            doc.AddTwoColumn("Heel Angle", "GZ (m)", totalChars: 40, size: 9, bold: true);
            doc.AddRule(40);
            foreach (var point in stab.GzTable)
                doc.AddTwoColumn($"{point.AngleDeg:F0} deg", $"{point.GzMetres:F3}", totalChars: 40, size: 9);
            doc.AddSpacer(10);

            doc.AddTwoColumn("Area 0-30 deg", $"{stab.Area030:F4} m.rad", totalChars: 40, size: 9);
            doc.AddTwoColumn("Area 0-40 deg", $"{stab.Area040:F4} m.rad", totalChars: 40, size: 9);
            doc.AddTwoColumn("Area 30-40 deg", $"{stab.Area3040:F4} m.rad", totalChars: 40, size: 9);
            doc.AddTwoColumn("GZ at 30 deg", $"{stab.Gz30:F3} m", totalChars: 40, size: 9);
            doc.AddTwoColumn("GM (fluid)", $"{stab.GmFluid:F3} m", totalChars: 40, size: 9);
            doc.AddTwoColumn("Angle of Max GZ", $"{stab.MaxGzAngle:F1} deg", totalChars: 40, size: 9);
            doc.AddTwoColumn("Range of Positive Stability", $"{stab.RangeOfPositiveStability:F1} deg", totalChars: 40, size: 9);

            doc.AddSpacer(10);
            doc.AddRule();
            doc.AddSpacer(10);
        }

        private void AddHydrostaticsSection(SimplePdfDocument doc)
        {
            doc.AddLine("HYDROSTATIC SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var hydro = HydrostaticsSource;
            if (hydro is null)
            {
                doc.AddLine("Hydrostatics module data not available.", size: 9);
                doc.AddSpacer(10);
                doc.AddRule();
                doc.AddSpacer(10);
                return;
            }

            doc.AddTwoColumn("Displacement", $"{hydro.Displacement:F1} t", totalChars: 40, size: 9);
            doc.AddTwoColumn("Draft", $"{hydro.DraftT:F3} m", totalChars: 40, size: 9);
            doc.AddTwoColumn("KB", $"{hydro.Kb:F3} m", totalChars: 40, size: 9);
            doc.AddTwoColumn("KM", $"{hydro.Km:F3} m", totalChars: 40, size: 9);
            doc.AddTwoColumn("BM", $"{hydro.Bm:F3} m", totalChars: 40, size: 9);
            doc.AddTwoColumn("GM", $"{hydro.Gm:F3} m", totalChars: 40, size: 9);

            doc.AddSpacer(10);
            doc.AddRule();
            doc.AddSpacer(10);
        }

        private void AddResistanceSection(SimplePdfDocument doc)
        {
            doc.AddLine("RESISTANCE SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var res = ResistanceSource;
            if (res is null)
            {
                doc.AddLine("Resistance module data not available.", size: 9);
                doc.AddSpacer(10);
                doc.AddRule();
                doc.AddSpacer(10);
                return;
            }

            doc.AddTwoColumn("Design Speed", $"{res.SpeedMax:F1} kn", totalChars: 40, size: 9);
            doc.AddTwoColumn("Total Resistance (Rt)", $"{res.RtDesign:F1} kN", totalChars: 40, size: 9);
            doc.AddTwoColumn("Effective Power (EHP)", $"{res.Ehp:F1} kW", totalChars: 40, size: 9);

            doc.AddSpacer(10);
            doc.AddRule();
            doc.AddSpacer(10);
        }

        #endregion

        public ReportsViewModel()
        {
            Entries.CollectionChanged += Entries_CollectionChanged;
            SeedDefaultEntries();
            SeedFormulaAudit();
            SelectedEntry = Entries.Count > 0 ? Entries[0] : null;
            UpdateExportSummary();
        }

        private void SeedDefaultEntries()
        {
            Entries.Add(new ReportEntry
            {
                Title = "Intact Stability Booklet",
                Module = "Stability",
                Revision = "B",
                PreparedBy = "A. Alacam",
                DateIso = DateTime.Today.ToString("yyyy-MM-dd"),
                Status = "Approved",
                Notes = "IMO 2008 IS Code, all loading conditions."
            });
            Entries.Add(new ReportEntry
            {
                Title = "Damage Stability Analysis",
                Module = "Damage Stability",
                Revision = "A",
                PreparedBy = "A. Alacam",
                DateIso = "2026-06-18",
                Status = "Issued for Review",
                Notes = "SOLIS 2020, probabilistic method."
            });
            Entries.Add(new ReportEntry
            {
                Title = "Hydrostatic Particulars",
                Module = "Hydrostatics",
                Revision = "C",
                PreparedBy = "M. Sorensen",
                DateIso = "2026-05-14",
                Status = "Approved",
                Notes = "Bonjean curves and hydrostatic tables, 0-16 m draft range."
            });
            Entries.Add(new ReportEntry
            {
                Title = "Resistance & Powering Report",
                Module = "Resistance and Propulsion",
                Revision = "A",
                PreparedBy = "K. Ito",
                DateIso = "2026-07-01",
                Status = "Draft",
                Notes = "Holtrop-Mennen prediction, sea trial correlation pending."
            });
            Entries.Add(new ReportEntry
            {
                Title = "General Arrangement",
                Module = "Ship Builder",
                Revision = "D",
                PreparedBy = "R. Blake",
                DateIso = "2026-04-22",
                Status = "Approved",
                Notes = "Incorporates owner's comments rev C -> D."
            });
        }

        private void SeedFormulaAudit()
        {
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Hydrostatics", FormulaCount = 360 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Stability", FormulaCount = 335 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Damage Stability", FormulaCount = 240 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Resistance and Propulsion", FormulaCount = 460 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Machinery", FormulaCount = 310 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Welding", FormulaCount = 195 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Yacht Design", FormulaCount = 220 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "LNG Systems", FormulaCount = 205 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Manoeuvring", FormulaCount = 280 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Tonnage and Freeboard", FormulaCount = 310 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Ship Builder", FormulaCount = 180 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Reports", FormulaCount = 95 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "General / Shared Utilities", FormulaCount = 56 });
            OnPropertyChanged(nameof(TotalFormulaCount));
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
