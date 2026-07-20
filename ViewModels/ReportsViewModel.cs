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

        /// <summary>Live reference to the Machinery module, set by MainWindow. May be null if not yet wired.</summary>
        public MachineryViewModel? MachinerySource { get; set; }

        /// <summary>Live reference to the Damage Stability module, set by MainWindow.</summary>
        public DamageStabilityViewModel? DamageStabilitySource { get; set; }

        /// <summary>Live reference to the Yacht Design module, set by MainWindow.</summary>
        public YachtDesignViewModel? YachtDesignSource { get; set; }

        /// <summary>Live reference to the Manoeuvring module, set by MainWindow.</summary>
        public ManoeuvringViewModel? ManoeuvringSource { get; set; }

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

        #region PDF Section toggles

        private bool _includeShipBuilder = true;
        public bool IncludeShipBuilder { get => _includeShipBuilder; set => SetField(ref _includeShipBuilder, value); }

        private bool _includeHydrostatics = true;
        public bool IncludeHydrostatics { get => _includeHydrostatics; set => SetField(ref _includeHydrostatics, value); }

        private bool _includeStability = true;
        public bool IncludeStability { get => _includeStability; set => SetField(ref _includeStability, value); }

        private bool _includeResistance = true;
        public bool IncludeResistance { get => _includeResistance; set => SetField(ref _includeResistance, value); }

        private bool _includeMachinery = true;
        public bool IncludeMachinery { get => _includeMachinery; set => SetField(ref _includeMachinery, value); }

        private bool _includeDamageStability = true;
        public bool IncludeDamageStability { get => _includeDamageStability; set => SetField(ref _includeDamageStability, value); }

        private bool _includeWelding = true;
        public bool IncludeWelding { get => _includeWelding; set => SetField(ref _includeWelding, value); }

        private bool _includeTonnage = true;
        public bool IncludeTonnage { get => _includeTonnage; set => SetField(ref _includeTonnage, value); }

        private bool _includeYachtDesign = true;
        public bool IncludeYachtDesign { get => _includeYachtDesign; set => SetField(ref _includeYachtDesign, value); }

        private bool _includeManoeuvring = true;
        public bool IncludeManoeuvring { get => _includeManoeuvring; set => SetField(ref _includeManoeuvring, value); }

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

            // Title block — always included.
            doc.AddLine(ProjectName, size: 16, bold: true);
            doc.AddSpacer(10);
            doc.AddLine($"Project Number: {ProjectNumber}");
            doc.AddLine($"Client: {Client}");
            doc.AddLine($"Classification Society: {ClassSociety}");
            doc.AddLine($"Prepared By: {PreparedByCompany}");
            doc.AddLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            doc.AddSpacer(14);

            var sb = ShipBuilderViewModel.Instance;
            if (IncludeShipBuilder)
            {
                doc.AddLine("SHIP BUILDER — PRINCIPAL PARTICULARS", size: 13, bold: true);
                doc.AddSpacer(6);
                doc.AddTwoColumn("Vessel Type",   sb.VesselType,                    totalChars: 40, size: 9);
                doc.AddTwoColumn("Lpp",           $"{sb.Lpp:F2} m",                 totalChars: 40, size: 9);
                doc.AddTwoColumn("Breadth (B)",   $"{sb.Breadth:F2} m",             totalChars: 40, size: 9);
                doc.AddTwoColumn("Depth (D)",     $"{sb.Depth:F2} m",               totalChars: 40, size: 9);
                doc.AddTwoColumn("Draft (T)",     $"{sb.Draft:F2} m",               totalChars: 40, size: 9);
                doc.AddTwoColumn("Cb",            $"{sb.Cb:F3}",                    totalChars: 40, size: 9);
                doc.AddTwoColumn("Displacement",  $"{sb.Displacement:N1} t",        totalChars: 40, size: 9);
                doc.AddTwoColumn("Design Speed",  $"{sb.DesignSpeed:F1} kn",        totalChars: 40, size: 9);
                doc.AddTwoColumn("Froude Number", $"{sb.FroudeNumber:F4}",           totalChars: 40, size: 9);
                doc.AddSpacer(10);
                doc.AddRule();
                doc.AddSpacer(10);
            }

            if (IncludeHydrostatics)   AddHydrostaticsSection(doc);
            if (IncludeStability)      AddStabilitySection(doc);
            if (IncludeResistance)     AddResistanceSection(doc);
            if (IncludeMachinery)      AddMachinerySection(doc);
            if (IncludeDamageStability) AddDamageStabilitySection(doc);
            if (IncludeWelding)        AddWeldingSection(doc);
            if (IncludeTonnage)        AddTonnageSection(doc);
            if (IncludeYachtDesign)    AddYachtDesignSection(doc);
            if (IncludeManoeuvring)    AddManoeuvringSection(doc);

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

            static string PassFail(bool pass) => pass ? "PASS" : "FAIL";

            doc.AddTwoColumn("Area 0-30 deg (>= 0.055 m.rad)", $"{stab.Area030:F4} m.rad  {PassFail(stab.Area030 >= 0.055)}", totalChars: 56, size: 9);
            doc.AddTwoColumn("Area 0-40 deg (>= 0.090 m.rad)", $"{stab.Area040:F4} m.rad  {PassFail(stab.Area040 >= 0.090)}", totalChars: 56, size: 9);
            doc.AddTwoColumn("Area 30-40 deg (>= 0.030 m.rad)", $"{stab.Area3040:F4} m.rad  {PassFail(stab.Area3040 >= 0.030)}", totalChars: 56, size: 9);
            doc.AddTwoColumn("GZ at 30 deg (>= 0.200 m)", $"{stab.Gz30:F3} m  {PassFail(stab.Gz30 >= 0.200)}", totalChars: 56, size: 9);
            doc.AddTwoColumn("GM (fluid)", $"{stab.GmFluid:F3} m", totalChars: 40, size: 9);
            doc.AddTwoColumn("Angle of Max GZ (>= 25 deg)", $"{stab.MaxGzAngle:F1} deg  {PassFail(stab.MaxGzAngle >= 25.0)}", totalChars: 56, size: 9);
            doc.AddTwoColumn("Range of Positive Stability (>= 15 deg)", $"{stab.RangeOfPositiveStability:F1} deg  {PassFail(stab.RangeOfPositiveStability >= 15.0)}", totalChars: 56, size: 9);

            doc.AddSpacer(8);
            doc.AddLine("GZ Curve (Stability)", size: 10, bold: true);
            doc.AddSpacer(4);
            var pngS = ChartImageRenderer.RenderToPng(stab.GzPlotModel, 800, 380);
            doc.AddImage(pngS);

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

            doc.AddTwoColumn("Displacement", $"{hydro.Displacement:F1} t",  totalChars: 40, size: 9);
            doc.AddTwoColumn("Draft",        $"{hydro.DraftT:F3} m",         totalChars: 40, size: 9);
            doc.AddTwoColumn("KB",           $"{hydro.Kb:F3} m",             totalChars: 40, size: 9);
            doc.AddTwoColumn("KM",           $"{hydro.Km:F3} m",             totalChars: 40, size: 9);
            doc.AddTwoColumn("BM",           $"{hydro.Bm:F3} m",             totalChars: 40, size: 9);
            doc.AddTwoColumn("GM",           $"{hydro.Gm:F3} m",             totalChars: 40, size: 9);
            doc.AddSpacer(8);

            // GZ Curve chart
            doc.AddLine("GZ Curve (Hydrostatics)", size: 10, bold: true);
            doc.AddSpacer(4);
            var pngH = ChartImageRenderer.RenderToPng(hydro.GzPlotModel, 800, 380);
            doc.AddImage(pngH);

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

            doc.AddTwoColumn("Design Speed",          $"{res.SpeedMax:F1} kn",    totalChars: 40, size: 9);
            doc.AddTwoColumn("Total Resistance (Rt)",  $"{res.RtDesign:F1} kN",   totalChars: 40, size: 9);
            doc.AddTwoColumn("Effective Power (EHP)",  $"{res.Ehp:F1} kW",        totalChars: 40, size: 9);
            doc.AddTwoColumn("Delivered Power (DHP)",  $"{res.Dhp:F1} kW",        totalChars: 40, size: 9);
            doc.AddTwoColumn("Brake Power (BHP)",      $"{res.Bhp:F1} kW",        totalChars: 40, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("Resistance Curve (Holtrop-Mennen)", size: 10, bold: true);
            doc.AddSpacer(4);
            var pngR = ChartImageRenderer.RenderToPng(res.ResistancePlotModel, 800, 380);
            doc.AddImage(pngR);

            doc.AddSpacer(10);
            doc.AddRule();
            doc.AddSpacer(10);
        }

        private void AddMachinerySection(SimplePdfDocument doc)
        {
            doc.AddLine("MACHINERY SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var mc = MachinerySource;
            if (mc is null)
            {
                doc.AddLine("Machinery module data not available.", size: 9);
                doc.AddSpacer(10);
                doc.AddRule();
                doc.AddSpacer(10);
                return;
            }

            var csrRow = mc.FuelTable.FirstOrDefault(r => r.Label == "CSR (service)");

            doc.AddTwoColumn("MCR Power", $"{mc.Mcr:F0} kW", totalChars: 40, size: 9);
            doc.AddTwoColumn("CSR Power", $"{mc.Csr:F0} kW", totalChars: 40, size: 9);
            doc.AddTwoColumn("Selected Engine Model", string.IsNullOrWhiteSpace(mc.EngineModel) ? "(not specified)" : mc.EngineModel, totalChars: 40, size: 9);
            doc.AddTwoColumn("SFOC at CSR", $"{csrRow?.SfocGPerKwh ?? 0.0:F1} g/kWh", totalChars: 56, size: 9);
            doc.AddSpacer(2);
            doc.AddTwoColumn("Daily Fuel Consumption at CSR", $"{csrRow?.DailyFuelTonnes ?? 0.0:F2} t/day", totalChars: 56, size: 9);
            doc.AddTwoColumn("Attained EEDI",         $"{mc.AttainedEedi:F3}",                                   totalChars: 40, size: 9);
            doc.AddTwoColumn("NOx Tier Compliance",    $"{mc.ApplicableTier} — {mc.NoxComplianceText}",          totalChars: 40, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("SFOC Curve vs Load", size: 10, bold: true);
            doc.AddSpacer(4);
            var pngM = ChartImageRenderer.RenderToPng(mc.SfocPlotModel, 800, 380);
            doc.AddImage(pngM);

            doc.AddSpacer(10);
            doc.AddRule();
            doc.AddSpacer(10);
        }

        private void AddDamageStabilitySection(SimplePdfDocument doc)
        {
            doc.AddLine("DAMAGE STABILITY SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var ds = DamageStabilitySource;
            if (ds is null)
            {
                doc.AddLine("Damage Stability module data not available.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                return;
            }

            doc.AddLine("Method: Lost buoyancy (SOLAS II-1, simplified teaching model)", size: 9);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Selected Case",       ds.SelectedCase?.Name ?? "(none)",          totalChars: 56, size: 9);
            doc.AddTwoColumn("Damaged Draft",       $"{ds.DamagedDraftTd:F3} m",                totalChars: 40, size: 9);
            doc.AddTwoColumn("Sinkage",             $"{ds.Sinkage:F3} m",                       totalChars: 40, size: 9);
            doc.AddTwoColumn("GM (damaged)",        $"{ds.GmDamaged:F3} m",                     totalChars: 40, size: 9);
            doc.AddTwoColumn("Max GZ (residual)",   $"{ds.MaxGz:F3} m",                         totalChars: 40, size: 9);
            doc.AddTwoColumn("Attained Index A",    $"{ds.AttainedIndex:F4}",                   totalChars: 40, size: 9);
            doc.AddTwoColumn("Required Index R",    $"{ds.RequiredIndex:F4}",                   totalChars: 40, size: 9);
            doc.AddTwoColumn("Subdivision",         ds.AttainedIndexPass ? "PASS" : "FAIL",     totalChars: 40, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("Residual GZ Curve (Selected Damage Case)", size: 10, bold: true);
            doc.AddSpacer(4);
            var pngDs = ChartImageRenderer.RenderToPng(ds.GzPlotModel, 800, 360);
            doc.AddImage(pngDs);

            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddYachtDesignSection(SimplePdfDocument doc)
        {
            doc.AddLine("YACHT DESIGN SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var yd = YachtDesignSource;
            if (yd is null)
            {
                doc.AddLine("Yacht Design module data not available.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                return;
            }

            doc.AddTwoColumn("Displacement/Length Ratio",  $"{yd.Dlr:F1}",               totalChars: 40, size: 9);
            doc.AddTwoColumn("Sail Area/Displacement Ratio",$"{yd.Sadr:F2}",              totalChars: 40, size: 9);
            doc.AddTwoColumn("Capsize Screening Formula",  $"{yd.Csf:F2}",               totalChars: 40, size: 9);
            doc.AddTwoColumn("Comfort Ratio",              $"{yd.ComfortRatio:F1} ({yd.ComfortBand})", totalChars: 56, size: 9);
            doc.AddTwoColumn("Hull Speed",                 $"{yd.HullSpeedKts:F2} kts",  totalChars: 40, size: 9);
            doc.AddTwoColumn("Dellenbaugh Angle",          $"{yd.DellenbaughAngleDeg:F1} deg", totalChars: 40, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("VPP Speed Polar", size: 10, bold: true);
            doc.AddSpacer(4);
            var pngYd = ChartImageRenderer.RenderToPng(yd.PolarPlotModel, 800, 360);
            doc.AddImage(pngYd);

            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddManoeuvringSection(SimplePdfDocument doc)
        {
            doc.AddLine("MANOEUVRING SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var mn = ManoeuvringSource;
            if (mn is null)
            {
                doc.AddLine("Manoeuvring module data not available.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                return;
            }

            doc.AddTwoColumn("Advance",                $"{mn.Advance:F1} m",                    totalChars: 40, size: 9);
            doc.AddTwoColumn("Tactical Diameter",      $"{mn.TacticalDiameter:F1} m",           totalChars: 40, size: 9);
            doc.AddTwoColumn("Steady Turning Diameter",$"{mn.SteadyTurningDiameter:F1} m",      totalChars: 40, size: 9);
            doc.AddTwoColumn("First Overshoot Angle",  $"{mn.FirstOvershootAngleDeg:F1} deg",   totalChars: 40, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("Turning Circle Trajectory", size: 10, bold: true);
            doc.AddSpacer(4);
            var pngTc = ChartImageRenderer.RenderToPng(mn.TurningCirclePlotModel, 800, 360);
            doc.AddImage(pngTc);

            doc.AddSpacer(8);
            doc.AddLine("Zig-Zag Manoeuvre", size: 10, bold: true);
            doc.AddSpacer(4);
            var pngZz = ChartImageRenderer.RenderToPng(mn.ZigZagPlotModel, 800, 360);
            doc.AddImage(pngZz);

            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }
        private void AddWeldingSection(SimplePdfDocument doc)
        {
            doc.AddLine("WELDING — JOINT DESIGN SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            doc.AddLine("Refer to Welding module for joint sizing, heat input and WPS reference.", size: 9);
            doc.AddSpacer(10);
            doc.AddRule();
            doc.AddSpacer(10);
        }

        private void AddTonnageSection(SimplePdfDocument doc)
        {
            doc.AddLine("TONNAGE AND FREEBOARD SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            doc.AddLine("Refer to Tonnage and Freeboard module for ITC 1969 GT/NT and ILLC 1966 freeboard results.", size: 9);
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
