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

        /// <summary>Live reference to the Bow Design module, set by MainWindow.</summary>
        public BowDesignViewModel? BowDesignSource { get; set; }

        /// <summary>Live reference to the Welding module, set by MainWindow.</summary>
        public WeldingViewModel? WeldingSource { get; set; }

        /// <summary>Live reference to the Tonnage and Freeboard module, set by MainWindow.</summary>
        public TonnageFreeboardViewModel? TonnageSource { get; set; }

        /// <summary>Live reference to the BodyPlanCanvas for PDF rendering, set by MainWindow.</summary>
        public NavalArchitectureSuite.Views.BodyPlanCanvas? BodyPlanCanvasRef { get; set; }

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

        private bool _includeBowDesign = true;
        public bool IncludeBowDesign { get => _includeBowDesign; set => SetField(ref _includeBowDesign, value); }

        private bool _includeBodyPlan = true;
        public bool IncludeBodyPlan { get => _includeBodyPlan; set => SetField(ref _includeBodyPlan, value); }

        // ── Paper size ───────────────────────────────────────────────────────
        public static string[] PaperSizeNames { get; } = { "A4 (report)", "A3", "A1", "A0 (body plan)" };

        private int _paperSizeIndex = 0;
        public int PaperSizeIndex
        {
            get => _paperSizeIndex;
            set => SetField(ref _paperSizeIndex, value);
        }

        private PdfPaperSize SelectedPaperSize => _paperSizeIndex switch
        {
            1 => PdfPaperSize.A3,
            2 => PdfPaperSize.A1,
            3 => PdfPaperSize.A0,
            _ => PdfPaperSize.A4,
        };

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
            var doc = new SimplePdfDocument(SelectedPaperSize);

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
            if (IncludeBowDesign)      AddBowDesignSection(doc);
            if (IncludeBodyPlan)       AddBodyPlanSection(doc);

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
        private void AddBowDesignSection(SimplePdfDocument doc)
        {
            doc.AddLine("BOW DESIGN SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var bd = BowDesignSource;
            if (bd is null)
            {
                doc.AddLine("Bow Design module data not available.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                return;
            }

            doc.AddLine("Bow Geometry (Kracht 1978)", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Froude Number",           $"{bd.FroudeNumber:F4}",                  totalChars: 40, size: 9);
            doc.AddTwoColumn("Prismatic Coefficient Cp", $"{bd.Cp:F3}",                            totalChars: 40, size: 9);
            doc.AddTwoColumn("Half Entry Angle alpha_E",  $"{bd.HalfEntryAngleDeg:F1} deg",      totalChars: 40, size: 9);
            doc.AddTwoColumn("Full Entry Angle 2*alpha_E", $"{bd.FullEntryAngleDeg:F1} deg",       totalChars: 40, size: 9);
            doc.AddTwoColumn("Entry Angle Assessment",   bd.EntryAngleAssessment,                  totalChars: 56, size: 9);
            doc.AddTwoColumn("Recommended Stem Type",   bd.RecommendedStemType,                   totalChars: 56, size: 9);
            doc.AddTwoColumn("Recommended Bulb Type",   bd.RecommendedBulbType,                   totalChars: 56, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("Bulbous Bow (Kracht 1978)", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Bulb Transverse Area A_BT", $"{bd.ABT:F2} m²  (→ Holtrop-Mennen input)", totalChars: 56, size: 9);
            doc.AddTwoColumn("Bulb Centroid Height z_B",  $"{bd.ZB:F2} m  (→ Holtrop-Mennen h_B)",  totalChars: 56, size: 9);
            doc.AddTwoColumn("Bulb Breadth B_B",          $"{bd.BulbBreadth:F2} m",                 totalChars: 40, size: 9);
            doc.AddTwoColumn("Bulb Protrusion L_B",       $"{bd.BulbLength:F2} m",                  totalChars: 40, size: 9);
            doc.AddTwoColumn("Bulb Volume V_B",           $"{bd.BulbVolume:F1} m³",                 totalChars: 40, size: 9);
            doc.AddTwoColumn("Bulb Submergence",          bd.BulbSubmergenceStatus,                 totalChars: 56, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("SOLAS Collision Bulkhead (Ch.II-1 Reg.11)", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Minimum position from FP",  $"{bd.CollisionBulkheadMin:F2} m",        totalChars: 40, size: 9);
            doc.AddTwoColumn("Maximum position from FP",  $"{bd.CollisionBulkheadMax:F2} m",        totalChars: 40, size: 9);
            doc.AddTwoColumn("Recommended position",      $"{bd.CollisionBulkheadRecommended:F2} m", totalChars: 40, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("Anchor Equipment (IACS UR A1 2019)", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Equipment Number EN",       $"{bd.EquipmentNumber:F0}",               totalChars: 40, size: 9);
            doc.AddTwoColumn("Anchor Mass (per bower)",   $"{bd.AnchorMassTonnes:F2} t",            totalChars: 40, size: 9);
            doc.AddTwoColumn("Chain Diameter (stud link)", $"{bd.ChainDiameterMm:F1} mm",           totalChars: 40, size: 9);
            doc.AddTwoColumn("Chain Length per Cable",    $"{bd.ChainLengthM:F0} m ({bd.ChainShots} shots)", totalChars: 56, size: 9);
            doc.AddTwoColumn("Chain Breaking Load (K2)",  $"{bd.ChainBreakingLoadKN:F0} kN",        totalChars: 40, size: 9);
            doc.AddTwoColumn("Hawse Pipe Inner Diameter", $"{bd.HawsePipeDiameterMm:F0} mm",        totalChars: 40, size: 9);

            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddBodyPlanSection(SimplePdfDocument doc)
        {
            doc.AddLine("BODY PLAN — 11 STATION SECTIONS", size: 13, bold: true);
            doc.AddSpacer(4);
            doc.AddLine("Parametric hull body plan. Forward sections right, aft sections left.", size: 9);
            doc.AddLine("Colour zones match 3D hull: navy=underwater, red=boot topping, grey=topside, cream=deck.", size: 9);
            doc.AddSpacer(8);

            var canvas = BodyPlanCanvasRef;
            if (canvas is null)
            {
                doc.AddLine("Body plan canvas not available.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                return;
            }

            // Render the BodyPlanCanvas to PNG at high resolution
            // For A0/A1 we render much larger
            int w, h;
            switch (doc.PaperSize)
            {
                case PdfPaperSize.A0: w = 6000; h = 3000; break;
                case PdfPaperSize.A1: w = 4000; h = 2000; break;
                case PdfPaperSize.A3: w = 2400; h = 1200; break;
                default:              w = 1600; h =  800; break;
            }

            byte[]? png = null;
            canvas.Dispatcher.Invoke(() =>
            {
                // Temporarily resize canvas for high-res render
                double origW = canvas.ActualWidth;
                double origH = canvas.ActualHeight;

                canvas.Width  = w;
                canvas.Height = h;
                canvas.Measure(new System.Windows.Size(w, h));
                canvas.Arrange(new System.Windows.Rect(0, 0, w, h));
                canvas.UpdateLayout();

                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    w, h, 96, 96,
                    System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(canvas);

                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var ms = new System.IO.MemoryStream();
                encoder.Save(ms);
                png = ms.ToArray();

                // Restore original size
                canvas.Width  = double.NaN;
                canvas.Height = 300;
                canvas.InvalidateMeasure();
                canvas.InvalidateArrange();
            });

            if (png is not null)
            {
                // For A0 use maximum page width minus margins; for A4 standard width
                double maxW = doc.PaperSize switch
                {
                    PdfPaperSize.A0 => 2276,
                    PdfPaperSize.A1 => 1576,
                    PdfPaperSize.A3 =>  734,
                    _               =>  487,
                };
                double maxH = maxW / 2.0;   // 2:1 aspect ratio for body plan
                doc.AddImage(png, maxW, maxH);
            }

            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddWeldingSection(SimplePdfDocument doc)
        {
            doc.AddLine("WELDING — JOINT DESIGN SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var wl = WeldingSource;
            if (wl is null)
            {
                doc.AddLine("Welding module data not available.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                return;
            }

            doc.AddLine("Joint Design", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Joint Type",              wl.JointTypeIndex == 0 ? "Butt Joint — Full Penetration" : "Fillet Joint",  totalChars: 56, size: 9);
            doc.AddTwoColumn("Steel Grade",             WeldingViewModel.SteelGradeNames[Math.Clamp(wl.SteelGradeIndex, 0, WeldingViewModel.SteelGradeNames.Length-1)], totalChars: 56, size: 9);
            doc.AddTwoColumn("Plate Thickness t1",      $"{wl.PlateThickness:F1} mm",                             totalChars: 40, size: 9);
            doc.AddTwoColumn("Design Load, P",          $"{wl.DesignLoad:F1} kN/m",                              totalChars: 40, size: 9);

            if (wl.IsButt)
            {
                doc.AddTwoColumn("Governing Thickness",     $"{wl.GoverningThicknessMm:F1} mm",                     totalChars: 40, size: 9);
                doc.AddTwoColumn("Butt Tensile Stress, sigma",  $"{wl.ButtTensileStress:F1} MPa",     totalChars: 40, size: 9);
                doc.AddTwoColumn("Allowable Stress, sigma_allow", $"{wl.AllowableTensileStress:F0} MPa", totalChars: 40, size: 9);
                doc.AddTwoColumn("NDT Requirement",         "100% RT or UT (full penetration)",                    totalChars: 56, size: 9);
            }
            else
            {
                doc.AddTwoColumn("Required Throat Thickness", $"{wl.RequiredThroatThickness_mm:F2} mm",             totalChars: 40, size: 9);
                doc.AddTwoColumn("Required Leg Size",         $"{wl.RequiredLegSize_mm:F2} mm",                     totalChars: 40, size: 9);
                doc.AddTwoColumn("Min Leg by Thickness Table", $"{wl.MinLegByThickness_mm:F1} mm",                  totalChars: 40, size: 9);
                doc.AddTwoColumn("Governing Leg Size",        $"{wl.GoverningLegSize_mm:F2} mm",                    totalChars: 40, size: 9);
                doc.AddTwoColumn("Weld Utilisation",          $"{wl.WeldUtilisation:F2}",                           totalChars: 40, size: 9);
            }

            doc.AddSpacer(8);
            doc.AddLine("Heat Input & Preheat", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Arc Heat Input, Q",         $"{wl.HeatInput_kJmm:F2} kJ/mm",                        totalChars: 40, size: 9);
            doc.AddTwoColumn("Preheat from CE",           $"{wl.PreheatFromCE_C:F0} °C",                           totalChars: 40, size: 9);
            doc.AddTwoColumn("AWS Electrode Class",       wl.WpsElectrode,                                        totalChars: 56, size: 9);
            doc.AddTwoColumn("Min Preheat (Grade Table)", wl.WpsMinPreheat,                                       totalChars: 40, size: 9);
            doc.AddTwoColumn("Recommended Process",       wl.RecommendedProcess,                                  totalChars: 56, size: 9);

            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddTonnageSection(SimplePdfDocument doc)
        {
            doc.AddLine("TONNAGE AND FREEBOARD SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);

            var tf = TonnageSource;
            if (tf is null)
            {
                doc.AddLine("Tonnage and Freeboard module data not available.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                return;
            }

            doc.AddLine("ITC 1969 Tonnage", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Gross Tonnage, GT",     $"{tf.GrossTonnage:F1}",                   totalChars: 40, size: 9);
            doc.AddTwoColumn("Net Tonnage, NT",       $"{tf.NetTonnage:F1}",                     totalChars: 40, size: 9);
            doc.AddTwoColumn("K1",                    $"{tf.K1:F4}",                             totalChars: 40, size: 9);
            doc.AddTwoColumn("K2",                    $"{tf.K2:F4}",                             totalChars: 40, size: 9);
            doc.AddTwoColumn("K3",                    $"{tf.K3:F4}",                             totalChars: 40, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("ICLL 1966 Freeboard (Type B)", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Freeboard Length, Lfb",      $"{tf.Lfb:F2} m",                    totalChars: 40, size: 9);
            doc.AddTwoColumn("Table Freeboard (ICLL Annex I Table B)", $"{tf.TableFreeboard_mm:F0} mm",  totalChars: 56, size: 9);
            doc.AddTwoColumn("Cb Correction Factor",       $"×{tf.CbCorrectionFactor:F4}",        totalChars: 40, size: 9);
            doc.AddTwoColumn("Table Freeboard after Cb",   $"{tf.TableFreeboardAfterCb_mm:F0} mm", totalChars: 40, size: 9);
            doc.AddTwoColumn("Depth Correction",           $"+{tf.DepthCorrection_mm:F0} mm",     totalChars: 40, size: 9);
            doc.AddTwoColumn("Superstructure Deduction",   $"-{tf.SuperstructureDeduction_mm:F0} mm", totalChars: 40, size: 9);
            doc.AddTwoColumn("Sheer Correction",           $"{tf.SheerCorrection_mm:F0} mm",      totalChars: 40, size: 9);
            doc.AddTwoColumn("Assigned Summer Freeboard",  $"{tf.AssignedFreeboard_m:F3} m",      totalChars: 40, size: 9);
            doc.AddSpacer(8);

            doc.AddLine("Load Line Marks", size: 10, bold: true);
            doc.AddSpacer(4);
            doc.AddTwoColumn("S — Summer",              $"{tf.SummerMark_m:F3} m",             totalChars: 40, size: 9);
            doc.AddTwoColumn("W — Winter",              $"{tf.WinterMark_m:F3} m",             totalChars: 40, size: 9);
            doc.AddTwoColumn("WNA — Winter North Atlantic", $"{tf.WinterNAMark_m:F3} m",       totalChars: 40, size: 9);
            doc.AddTwoColumn("T — Tropical",            $"{tf.TropicalMark_m:F3} m",           totalChars: 40, size: 9);
            doc.AddTwoColumn("F — Fresh Water",         $"{tf.FreshWaterMark_m:F3} m",         totalChars: 40, size: 9);
            doc.AddTwoColumn("TF — Tropical Fresh Water", $"{tf.TropicalFreshWaterMark_m:F3} m", totalChars: 40, size: 9);
            doc.AddSpacer(4);

            bool bowPass = tf.FreeboardCriteria.Count > 0 && tf.FreeboardCriteria[0].IsPass;
            doc.AddTwoColumn("Min Bow Height (ICLL Reg 39)",
                $"Required {tf.RequiredBowHeight_mm:F0} mm / Actual {tf.BowHeight_actual_m * 1000:F0} mm — {(bowPass ? "PASS" : "FAIL")}",
                totalChars: 72, size: 9);

            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
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
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Hydrostatics",             FormulaCount = 360 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Stability",                 FormulaCount = 335 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Damage Stability",          FormulaCount = 240 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Resistance and Propulsion", FormulaCount = 460 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Machinery",                 FormulaCount = 310 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Welding",                   FormulaCount = 195 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Yacht Design",              FormulaCount = 220 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "LNG Systems",               FormulaCount = 205 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Manoeuvring",               FormulaCount = 280 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Tonnage and Freeboard",     FormulaCount = 310 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Ship Builder",              FormulaCount = 180 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Bow Design",               FormulaCount = 112 });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "Reports",                  FormulaCount = 95  });
            FormulaAudit.Add(new ModuleFormulaCount { Module = "General / Shared Utilities",FormulaCount = 56  });
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
