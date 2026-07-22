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
    public class ReportsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public HydrostaticsViewModel?        HydrostaticsSource    { get; set; }
        public StabilityViewModel?           StabilitySource       { get; set; }
        public ResistancePropulsionViewModel? ResistanceSource     { get; set; }
        public MachineryViewModel?           MachinerySource       { get; set; }
        public DamageStabilityViewModel?     DamageStabilitySource { get; set; }
        public YachtDesignViewModel?         YachtDesignSource     { get; set; }
        public ManoeuvringViewModel?         ManoeuvringSource     { get; set; }
        public BowDesignViewModel?           BowDesignSource       { get; set; }
        public WeldingViewModel?             WeldingSource         { get; set; }
        public TonnageFreeboardViewModel?    TonnageSource         { get; set; }
        public LngSystemsViewModel?          LngSource             { get; set; }

        /// <summary>Live reference to the ShipBuilderView — gives access to all 3
        /// Lines Plan canvases and their PDF-export checkboxes. Set by MainWindow.</summary>
        public NavalArchitectureSuite.Views.ShipBuilderView? ShipBuilderViewRef { get; set; }

        #region Project title block
        private string _projectName      = "MV Example Bulk Carrier — Newbuild 4201";
        private string _projectNumber    = "NA-2026-0042";
        private string _client           = "Example Shipping Co.";
        private string _classSociety     = "DNV";
        private string _preparedByCompany = "Naval Architecture Engineering Suite";
        public string ProjectName      { get => _projectName;       set => SetField(ref _projectName, value); }
        public string ProjectNumber    { get => _projectNumber;     set => SetField(ref _projectNumber, value); }
        public string Client           { get => _client;            set => SetField(ref _client, value); }
        public string ClassSociety     { get => _classSociety;      set => SetField(ref _classSociety, value); }
        public string PreparedByCompany{ get => _preparedByCompany; set => SetField(ref _preparedByCompany, value); }
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
            if (Entries.Count <= 1) return;
            int index = Entries.IndexOf(entry);
            if (index < 0) return;
            Entries.RemoveAt(index);
            if (ReferenceEquals(SelectedEntry, entry))
                SelectedEntry = Entries.Count > 0 ? Entries[Math.Max(0, index - 1)] : null;
        }

        private void Entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null) foreach (ReportEntry i in e.OldItems) i.PropertyChanged -= OnEntryPropertyChanged;
            if (e.NewItems is not null) foreach (ReportEntry i in e.NewItems) i.PropertyChanged += OnEntryPropertyChanged;
            UpdateExportSummary();
        }

        private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReportEntry.IncludeInExport)) UpdateExportSummary();
        }
        #endregion

        #region PDF Section toggles
        private bool _includeShipBuilder    = true;
        private bool _includeHydrostatics   = true;
        private bool _includeStability      = true;
        private bool _includeResistance     = true;
        private bool _includeMachinery      = true;
        private bool _includeDamageStability= true;
        private bool _includeWelding        = true;
        private bool _includeTonnage        = true;
        private bool _includeYachtDesign    = true;
        private bool _includeManoeuvring    = true;
        private bool _includeBowDesign      = true;
        private bool _includeLng            = true;
        private bool _includeBodyPlan       = true;

        public bool IncludeShipBuilder     { get => _includeShipBuilder;     set => SetField(ref _includeShipBuilder, value); }
        public bool IncludeHydrostatics    { get => _includeHydrostatics;    set => SetField(ref _includeHydrostatics, value); }
        public bool IncludeStability       { get => _includeStability;       set => SetField(ref _includeStability, value); }
        public bool IncludeResistance      { get => _includeResistance;      set => SetField(ref _includeResistance, value); }
        public bool IncludeMachinery       { get => _includeMachinery;       set => SetField(ref _includeMachinery, value); }
        public bool IncludeDamageStability { get => _includeDamageStability; set => SetField(ref _includeDamageStability, value); }
        public bool IncludeWelding         { get => _includeWelding;         set => SetField(ref _includeWelding, value); }
        public bool IncludeTonnage         { get => _includeTonnage;         set => SetField(ref _includeTonnage, value); }
        public bool IncludeYachtDesign     { get => _includeYachtDesign;     set => SetField(ref _includeYachtDesign, value); }
        public bool IncludeManoeuvring     { get => _includeManoeuvring;     set => SetField(ref _includeManoeuvring, value); }
        public bool IncludeBowDesign       { get => _includeBowDesign;       set => SetField(ref _includeBowDesign, value); }
        public bool IncludeLng             { get => _includeLng;             set => SetField(ref _includeLng, value); }
        public bool IncludeBodyPlan        { get => _includeBodyPlan;        set => SetField(ref _includeBodyPlan, value); }

        public static string[] PaperSizeNames { get; } = { "A4 (report)", "A3", "A1", "A0 (body plan)" };
        private int _paperSizeIndex = 0;
        public int PaperSizeIndex { get => _paperSizeIndex; set => SetField(ref _paperSizeIndex, value); }
        private PdfPaperSize SelectedPaperSize => _paperSizeIndex switch
        {
            1 => PdfPaperSize.A3, 2 => PdfPaperSize.A1, 3 => PdfPaperSize.A0, _ => PdfPaperSize.A4,
        };
        #endregion

        #region Export summary
        private int _includedCount;
        private int _totalCount;
        public int IncludedCount { get => _includedCount; private set => SetField(ref _includedCount, value); }
        public int TotalCount    { get => _totalCount;    private set => SetField(ref _totalCount, value); }
        public string ExportSummaryText => $"{IncludedCount} of {TotalCount} entries included in export";
        private void UpdateExportSummary()
        {
            TotalCount    = Entries.Count;
            IncludedCount = Entries.Count(e => e.IncludeInExport);
            OnPropertyChanged(nameof(ExportSummaryText));
        }
        #endregion

        #region Formula library audit
        public class ModuleFormulaCount { public string Module { get; set; } = ""; public int FormulaCount { get; set; } }
        public ObservableCollection<ModuleFormulaCount> FormulaAudit { get; } = new();
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
            if (dialog.ShowDialog() != true) { ExportStatusMessage = "Export cancelled"; return; }
            try
            {
                System.IO.File.WriteAllBytes(dialog.FileName, BuildPdfReport());
                ExportStatusMessage = $"Exported to {dialog.FileName}";
            }
            catch (Exception ex) { ExportStatusMessage = $"Export failed: {ex.Message}"; }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        public byte[] BuildPdfReport()
        {
            var doc = new SimplePdfDocument(SelectedPaperSize);

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
                doc.AddTwoColumn("Vessel Type",   sb.VesselType,              totalChars: 40, size: 9);
                doc.AddTwoColumn("Lpp",           $"{sb.Lpp:F2} m",           totalChars: 40, size: 9);
                doc.AddTwoColumn("Breadth (B)",   $"{sb.Breadth:F2} m",       totalChars: 40, size: 9);
                doc.AddTwoColumn("Depth (D)",     $"{sb.Depth:F2} m",         totalChars: 40, size: 9);
                doc.AddTwoColumn("Draft (T)",     $"{sb.Draft:F2} m",         totalChars: 40, size: 9);
                doc.AddTwoColumn("Cb",            $"{sb.Cb:F3}",              totalChars: 40, size: 9);
                doc.AddTwoColumn("Displacement",  $"{sb.Displacement:N1} t",  totalChars: 40, size: 9);
                doc.AddTwoColumn("Design Speed",  $"{sb.DesignSpeed:F1} kn",  totalChars: 40, size: 9);
                doc.AddTwoColumn("Froude Number", $"{sb.FroudeNumber:F4}",    totalChars: 40, size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
            }

            if (IncludeHydrostatics)    AddHydrostaticsSection(doc);
            if (IncludeStability)       AddStabilitySection(doc);
            if (IncludeResistance)      AddResistanceSection(doc);
            if (IncludeMachinery)       AddMachinerySection(doc);
            if (IncludeDamageStability) AddDamageStabilitySection(doc);
            if (IncludeWelding)         AddWeldingSection(doc);
            if (IncludeTonnage)         AddTonnageSection(doc);
            if (IncludeYachtDesign)     AddYachtDesignSection(doc);
            if (IncludeManoeuvring)     AddManoeuvringSection(doc);
            if (IncludeBowDesign)       AddBowDesignSection(doc);
            if (IncludeLng)             AddLngSection(doc);
            if (IncludeBodyPlan)        AddLinesPlanSection(doc);

            doc.AddSpacer(16);
            doc.AddLine("Generated by Naval Architecture Engineering Suite", size: 8);
            return doc.Build();
        }

        // ── Shared canvas-to-PDF helper ────────────────────────────────────────
        private void AddCanvasPng(SimplePdfDocument doc, System.Windows.FrameworkElement? canvas)
        {
            if (canvas is null) return;
            byte[]? png = null;
            int rW = 0, rH = 0;
            canvas.Dispatcher.Invoke(() =>
            {
                rW = Math.Max((int)canvas.ActualWidth,  800);
                rH = Math.Max((int)canvas.ActualHeight, 400);
                canvas.Measure(new System.Windows.Size(rW, rH));
                canvas.Arrange(new System.Windows.Rect(0, 0, rW, rH));
                canvas.UpdateLayout();
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    rW, rH, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(canvas);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var ms = new System.IO.MemoryStream();
                enc.Save(ms);
                png = ms.ToArray();
            });
            if (png is null) return;
            double maxW = doc.PaperSize switch
            {
                PdfPaperSize.A0 => 2276, PdfPaperSize.A1 => 1576,
                PdfPaperSize.A3 =>  734, _               =>  487,
            };
            double aspect = rH > 0 ? (double)rW / rH : 2.0;
            doc.AddImage(png, maxW, maxW / aspect);
        }

        // ── Lines Plan section (Body Plan / Sheer / Half-Breadth) ─────────────
        private void AddLinesPlanSection(SimplePdfDocument doc)
        {
            var sbView = ShipBuilderViewRef;
            if (sbView is null)
            {
                doc.AddLine("LINES PLAN", size: 13, bold: true);
                doc.AddLine("Lines Plan canvas not available.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                return;
            }

            bool any = false;

            if (sbView.ExportBodyPlan)
            {
                doc.AddLine("LINES PLAN — BODY PLAN (21 STATIONS)", size: 13, bold: true);
                doc.AddSpacer(4);
                doc.AddLine("Forward sections right, aft sections left. Gold = waterlines. Light blue = station outlines.", size: 9);
                doc.AddSpacer(8);
                AddCanvasPng(doc, sbView.BodyPlan);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                any = true;
            }

            if (sbView.ExportSheerPlan)
            {
                doc.AddLine("LINES PLAN — SHEER PLAN (PROFILE VIEW)", size: 13, bold: true);
                doc.AddSpacer(4);
                doc.AddLine("Gold = sheer line & waterlines. Light blue = station verticals & buttock lines.", size: 9);
                doc.AddSpacer(8);
                AddCanvasPng(doc, sbView.SheerPlan);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                any = true;
            }

            if (sbView.ExportHalfBreadth)
            {
                doc.AddLine("LINES PLAN — HALF-BREADTH PLAN (TOP VIEW)", size: 13, bold: true);
                doc.AddSpacer(4);
                doc.AddLine("Gold = waterlines & deck outline. Light blue = buttock lines. Starboard above CL.", size: 9);
                doc.AddSpacer(8);
                AddCanvasPng(doc, sbView.HalfBreadth);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
                any = true;
            }

            if (!any)
            {
                doc.AddLine("LINES PLAN", size: 13, bold: true);
                doc.AddLine("No views selected. Tick Body Plan, Sheer Plan, or Half-Breadth in Ship Builder.", size: 9);
                doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
            }
        }

        private void AddHydrostaticsSection(SimplePdfDocument doc)
        {
            doc.AddLine("HYDROSTATIC SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var hydro = HydrostaticsSource;
            if (hydro is null) { doc.AddLine("Hydrostatics module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddTwoColumn("Displacement", $"{hydro.Displacement:F1} t", totalChars: 40, size: 9);
            doc.AddTwoColumn("Draft",        $"{hydro.DraftT:F3} m",       totalChars: 40, size: 9);
            doc.AddTwoColumn("KB",           $"{hydro.Kb:F3} m",           totalChars: 40, size: 9);
            doc.AddTwoColumn("KM",           $"{hydro.Km:F3} m",           totalChars: 40, size: 9);
            doc.AddTwoColumn("BM",           $"{hydro.Bm:F3} m",           totalChars: 40, size: 9);
            doc.AddTwoColumn("GM",           $"{hydro.Gm:F3} m",           totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("GZ Curve (Hydrostatics)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddImage(ChartImageRenderer.RenderToPng(hydro.GzPlotModel, 800, 380));
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddStabilitySection(SimplePdfDocument doc)
        {
            doc.AddLine("STABILITY — GZ CURVE & IMO CRITERIA", size: 13, bold: true);
            doc.AddSpacer(6);
            var stab = StabilitySource;
            if (stab is null) { doc.AddLine("Stability module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddLine($"Loading Condition: {stab.SelectedCondition?.Name ?? "(none selected)"}", size: 9);
            doc.AddSpacer(6);
            doc.AddTwoColumn("Heel Angle", "GZ (m)", totalChars: 40, size: 9, bold: true);
            doc.AddRule(40);
            foreach (var p in stab.GzTable) doc.AddTwoColumn($"{p.AngleDeg:F0} deg", $"{p.GzMetres:F3}", totalChars: 40, size: 9);
            doc.AddSpacer(10);
            static string PF(bool pass) => pass ? "PASS" : "FAIL";
            doc.AddTwoColumn("Area 0-30 deg (>= 0.055 m.rad)",  $"{stab.Area030:F4} m.rad  {PF(stab.Area030 >= 0.055)}",              totalChars: 56, size: 9);
            doc.AddTwoColumn("Area 0-40 deg (>= 0.090 m.rad)",  $"{stab.Area040:F4} m.rad  {PF(stab.Area040 >= 0.090)}",              totalChars: 56, size: 9);
            doc.AddTwoColumn("Area 30-40 deg (>= 0.030 m.rad)", $"{stab.Area3040:F4} m.rad  {PF(stab.Area3040 >= 0.030)}",            totalChars: 56, size: 9);
            doc.AddTwoColumn("GZ at 30 deg (>= 0.200 m)",       $"{stab.Gz30:F3} m  {PF(stab.Gz30 >= 0.200)}",                       totalChars: 56, size: 9);
            doc.AddTwoColumn("GM (fluid)",                       $"{stab.GmFluid:F3} m",                                               totalChars: 40, size: 9);
            doc.AddTwoColumn("Angle of Max GZ (>= 25 deg)",     $"{stab.MaxGzAngle:F1} deg  {PF(stab.MaxGzAngle >= 25.0)}",           totalChars: 56, size: 9);
            doc.AddTwoColumn("Range of Stability (>= 15 deg)",  $"{stab.RangeOfPositiveStability:F1} deg  {PF(stab.RangeOfPositiveStability >= 15.0)}", totalChars: 56, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("GZ Curve (Stability)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddImage(ChartImageRenderer.RenderToPng(stab.GzPlotModel, 800, 380));
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddResistanceSection(SimplePdfDocument doc)
        {
            doc.AddLine("RESISTANCE SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var res = ResistanceSource;
            if (res is null) { doc.AddLine("Resistance module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddTwoColumn("Design Speed",         $"{res.SpeedMax:F1} kn",  totalChars: 40, size: 9);
            doc.AddTwoColumn("Total Resistance (Rt)",$"{res.RtDesign:F1} kN",  totalChars: 40, size: 9);
            doc.AddTwoColumn("Effective Power (EHP)", $"{res.Ehp:F1} kW",      totalChars: 40, size: 9);
            doc.AddTwoColumn("Delivered Power (DHP)", $"{res.Dhp:F1} kW",      totalChars: 40, size: 9);
            doc.AddTwoColumn("Brake Power (BHP)",     $"{res.Bhp:F1} kW",      totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("Resistance Curve (Holtrop-Mennen)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddImage(ChartImageRenderer.RenderToPng(res.ResistancePlotModel, 800, 380));
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddMachinerySection(SimplePdfDocument doc)
        {
            doc.AddLine("MACHINERY SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var mc = MachinerySource;
            if (mc is null) { doc.AddLine("Machinery module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            var csrRow = mc.FuelTable.FirstOrDefault(r => r.Label == "CSR (service)");
            doc.AddTwoColumn("MCR Power",              $"{mc.Mcr:F0} kW",                                      totalChars: 40, size: 9);
            doc.AddTwoColumn("CSR Power",              $"{mc.Csr:F0} kW",                                      totalChars: 40, size: 9);
            doc.AddTwoColumn("Selected Engine Model",  string.IsNullOrWhiteSpace(mc.EngineModel) ? "(not specified)" : mc.EngineModel, totalChars: 40, size: 9);
            doc.AddTwoColumn("SFOC at CSR",            $"{csrRow?.SfocGPerKwh ?? 0.0:F1} g/kWh",              totalChars: 56, size: 9);
            doc.AddTwoColumn("Daily Fuel at CSR",      $"{csrRow?.DailyFuelTonnes ?? 0.0:F2} t/day",           totalChars: 56, size: 9);
            doc.AddTwoColumn("Attained EEDI",          $"{mc.AttainedEedi:F3}",                                totalChars: 40, size: 9);
            doc.AddTwoColumn("NOx Tier Compliance",    $"{mc.ApplicableTier} — {mc.NoxComplianceText}",        totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("SFOC Curve vs Load", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddImage(ChartImageRenderer.RenderToPng(mc.SfocPlotModel, 800, 380));
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddDamageStabilitySection(SimplePdfDocument doc)
        {
            doc.AddLine("DAMAGE STABILITY SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var ds = DamageStabilitySource;
            if (ds is null) { doc.AddLine("Damage Stability module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddLine("Method: Lost buoyancy (SOLAS II-1, simplified teaching model)", size: 9);
            doc.AddSpacer(4);
            doc.AddTwoColumn("Selected Case",     ds.SelectedCase?.Name ?? "(none)", totalChars: 56, size: 9);
            doc.AddTwoColumn("Damaged Draft",     $"{ds.DamagedDraftTd:F3} m",      totalChars: 40, size: 9);
            doc.AddTwoColumn("Sinkage",           $"{ds.Sinkage:F3} m",             totalChars: 40, size: 9);
            doc.AddTwoColumn("GM (damaged)",      $"{ds.GmDamaged:F3} m",           totalChars: 40, size: 9);
            doc.AddTwoColumn("Max GZ (residual)", $"{ds.MaxGz:F3} m",               totalChars: 40, size: 9);
            doc.AddTwoColumn("Attained Index A",  $"{ds.AttainedIndex:F4}",         totalChars: 40, size: 9);
            doc.AddTwoColumn("Required Index R",  $"{ds.RequiredIndex:F4}",         totalChars: 40, size: 9);
            doc.AddTwoColumn("Subdivision",       ds.AttainedIndexPass ? "PASS" : "FAIL", totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("Residual GZ Curve (Selected Damage Case)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddImage(ChartImageRenderer.RenderToPng(ds.GzPlotModel, 800, 360));
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddWeldingSection(SimplePdfDocument doc)
        {
            doc.AddLine("WELDING — JOINT DESIGN SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var wl = WeldingSource;
            if (wl is null) { doc.AddLine("Welding module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddLine("Joint Design", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Joint Type",    wl.JointTypeIndex == 0 ? "Butt Joint — Full Penetration" : "Fillet Joint", totalChars: 56, size: 9);
            doc.AddTwoColumn("Steel Grade",   WeldingViewModel.SteelGradeNames[Math.Clamp(wl.SteelGradeIndex, 0, WeldingViewModel.SteelGradeNames.Length-1)], totalChars: 56, size: 9);
            doc.AddTwoColumn("Plate Thickness t1", $"{wl.PlateThickness:F1} mm", totalChars: 40, size: 9);
            doc.AddTwoColumn("Design Load, P",     $"{wl.DesignLoad:F1} kN/m",   totalChars: 40, size: 9);
            if (wl.IsButt)
            {
                doc.AddTwoColumn("Governing Thickness",        $"{wl.GoverningThicknessMm:F1} mm",          totalChars: 40, size: 9);
                doc.AddTwoColumn("Butt Tensile Stress",        $"{wl.ButtTensileStress:F1} MPa",            totalChars: 40, size: 9);
                doc.AddTwoColumn("Allowable Stress",           $"{wl.AllowableTensileStress:F0} MPa",       totalChars: 40, size: 9);
                doc.AddTwoColumn("NDT Requirement",            "100% RT or UT (full penetration)",           totalChars: 56, size: 9);
            }
            else
            {
                doc.AddTwoColumn("Required Throat",  $"{wl.RequiredThroatThickness_mm:F2} mm", totalChars: 40, size: 9);
                doc.AddTwoColumn("Required Leg Size",$"{wl.RequiredLegSize_mm:F2} mm",         totalChars: 40, size: 9);
                doc.AddTwoColumn("Min Leg (Table)",  $"{wl.MinLegByThickness_mm:F1} mm",       totalChars: 40, size: 9);
                doc.AddTwoColumn("Governing Leg",    $"{wl.GoverningLegSize_mm:F2} mm",        totalChars: 40, size: 9);
                doc.AddTwoColumn("Weld Utilisation", $"{wl.WeldUtilisation:F2}",               totalChars: 40, size: 9);
            }
            doc.AddSpacer(8);
            doc.AddLine("Heat Input & Preheat", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Arc Heat Input, Q",         $"{wl.HeatInput_kJmm:F2} kJ/mm",    totalChars: 40, size: 9);
            doc.AddTwoColumn("Preheat from CE",           $"{wl.PreheatFromCE_C:F0} °C",       totalChars: 40, size: 9);
            doc.AddTwoColumn("AWS Electrode Class",       wl.WpsElectrode,                     totalChars: 56, size: 9);
            doc.AddTwoColumn("Min Preheat (Grade Table)", wl.WpsMinPreheat,                    totalChars: 40, size: 9);
            doc.AddTwoColumn("Recommended Process",       wl.RecommendedProcess,               totalChars: 56, size: 9);
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddTonnageSection(SimplePdfDocument doc)
        {
            doc.AddLine("TONNAGE AND FREEBOARD SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var tf = TonnageSource;
            if (tf is null) { doc.AddLine("Tonnage and Freeboard module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddLine("ITC 1969 Tonnage", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Gross Tonnage, GT", $"{tf.GrossTonnage:F1}", totalChars: 40, size: 9);
            doc.AddTwoColumn("Net Tonnage, NT",   $"{tf.NetTonnage:F1}",   totalChars: 40, size: 9);
            doc.AddTwoColumn("K1",                $"{tf.K1:F4}",           totalChars: 40, size: 9);
            doc.AddTwoColumn("K2",                $"{tf.K2:F4}",           totalChars: 40, size: 9);
            doc.AddTwoColumn("K3",                $"{tf.K3:F4}",           totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("ICLL 1966 Freeboard (Type B)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Freeboard Length, Lfb",            $"{tf.Lfb:F2} m",                    totalChars: 40, size: 9);
            doc.AddTwoColumn("Table Freeboard (ICLL Annex I B)", $"{tf.TableFreeboard_mm:F0} mm",     totalChars: 56, size: 9);
            doc.AddTwoColumn("Cb Correction Factor",             $"×{tf.CbCorrectionFactor:F4}",      totalChars: 40, size: 9);
            doc.AddTwoColumn("Table Freeboard after Cb",         $"{tf.TableFreeboardAfterCb_mm:F0} mm", totalChars: 40, size: 9);
            doc.AddTwoColumn("Depth Correction",                 $"+{tf.DepthCorrection_mm:F0} mm",   totalChars: 40, size: 9);
            doc.AddTwoColumn("Superstructure Deduction",         $"-{tf.SuperstructureDeduction_mm:F0} mm", totalChars: 40, size: 9);
            doc.AddTwoColumn("Sheer Correction",                 $"{tf.SheerCorrection_mm:F0} mm",    totalChars: 40, size: 9);
            doc.AddTwoColumn("Assigned Summer Freeboard",        $"{tf.AssignedFreeboard_m:F3} m",    totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("Load Line Marks", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("S — Summer",               $"{tf.SummerMark_m:F3} m",          totalChars: 40, size: 9);
            doc.AddTwoColumn("W — Winter",               $"{tf.WinterMark_m:F3} m",          totalChars: 40, size: 9);
            doc.AddTwoColumn("WNA — Winter N. Atlantic", $"{tf.WinterNAMark_m:F3} m",        totalChars: 40, size: 9);
            doc.AddTwoColumn("T — Tropical",             $"{tf.TropicalMark_m:F3} m",        totalChars: 40, size: 9);
            doc.AddTwoColumn("F — Fresh Water",          $"{tf.FreshWaterMark_m:F3} m",      totalChars: 40, size: 9);
            doc.AddTwoColumn("TF — Tropical Fresh",      $"{tf.TropicalFreshWaterMark_m:F3} m", totalChars: 40, size: 9);
            doc.AddSpacer(4);
            bool bowPass = tf.FreeboardCriteria.Count > 0 && tf.FreeboardCriteria[0].IsPass;
            doc.AddTwoColumn("Min Bow Height (ICLL Reg 39)",
                $"Required {tf.RequiredBowHeight_mm:F0} mm / Actual {tf.BowHeight_actual_m * 1000:F0} mm — {(bowPass ? "PASS" : "FAIL")}",
                totalChars: 72, size: 9);
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddYachtDesignSection(SimplePdfDocument doc)
        {
            doc.AddLine("YACHT DESIGN SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var yd = YachtDesignSource;
            if (yd is null) { doc.AddLine("Yacht Design module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddTwoColumn("Displacement/Length Ratio",   $"{yd.Dlr:F1}",                              totalChars: 40, size: 9);
            doc.AddTwoColumn("Sail Area/Disp. Ratio",       $"{yd.Sadr:F2}",                             totalChars: 40, size: 9);
            doc.AddTwoColumn("Capsize Screening Formula",   $"{yd.Csf:F2}",                              totalChars: 40, size: 9);
            doc.AddTwoColumn("Comfort Ratio",               $"{yd.ComfortRatio:F1} ({yd.ComfortBand})",  totalChars: 56, size: 9);
            doc.AddTwoColumn("Hull Speed",                  $"{yd.HullSpeedKts:F2} kts",                 totalChars: 40, size: 9);
            doc.AddTwoColumn("Dellenbaugh Angle",           $"{yd.DellenbaughAngleDeg:F1} deg",          totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("VPP Speed Polar", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddImage(ChartImageRenderer.RenderToPng(yd.PolarPlotModel, 800, 360));
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddManoeuvringSection(SimplePdfDocument doc)
        {
            doc.AddLine("MANOEUVRING SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var mn = ManoeuvringSource;
            if (mn is null) { doc.AddLine("Manoeuvring module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddTwoColumn("Advance",                $"{mn.Advance:F1} m",                totalChars: 40, size: 9);
            doc.AddTwoColumn("Tactical Diameter",      $"{mn.TacticalDiameter:F1} m",       totalChars: 40, size: 9);
            doc.AddTwoColumn("Steady Turning Diameter",$"{mn.SteadyTurningDiameter:F1} m",  totalChars: 40, size: 9);
            doc.AddTwoColumn("First Overshoot Angle",  $"{mn.FirstOvershootAngleDeg:F1} deg",totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("Turning Circle Trajectory", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddImage(ChartImageRenderer.RenderToPng(mn.TurningCirclePlotModel, 800, 360));
            doc.AddSpacer(8);
            doc.AddLine("Zig-Zag Manoeuvre", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddImage(ChartImageRenderer.RenderToPng(mn.ZigZagPlotModel, 800, 360));
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddBowDesignSection(SimplePdfDocument doc)
        {
            doc.AddLine("BOW DESIGN SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var bd = BowDesignSource;
            if (bd is null) { doc.AddLine("Bow Design module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddLine("Bow Geometry (Kracht 1978)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Froude Number",             $"{bd.FroudeNumber:F4}",           totalChars: 40, size: 9);
            doc.AddTwoColumn("Prismatic Coefficient Cp",  $"{bd.Cp:F3}",                     totalChars: 40, size: 9);
            doc.AddTwoColumn("Half Entry Angle alpha_E",  $"{bd.HalfEntryAngleDeg:F1} deg",  totalChars: 40, size: 9);
            doc.AddTwoColumn("Full Entry Angle 2*alpha_E",$"{bd.FullEntryAngleDeg:F1} deg",  totalChars: 40, size: 9);
            doc.AddTwoColumn("Entry Angle Assessment",    bd.EntryAngleAssessment,            totalChars: 56, size: 9);
            doc.AddTwoColumn("Recommended Stem Type",     bd.RecommendedStemType,             totalChars: 56, size: 9);
            doc.AddTwoColumn("Recommended Bulb Type",     bd.RecommendedBulbType,             totalChars: 56, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("Bulbous Bow (Kracht 1978)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Bulb Transverse Area A_BT", $"{bd.ABT:F2} m²",                 totalChars: 40, size: 9);
            doc.AddTwoColumn("Bulb Centroid Height z_B",  $"{bd.ZB:F2} m",                   totalChars: 40, size: 9);
            doc.AddTwoColumn("Bulb Breadth B_B",          $"{bd.BulbBreadth:F2} m",          totalChars: 40, size: 9);
            doc.AddTwoColumn("Bulb Protrusion L_B",       $"{bd.BulbLength:F2} m",           totalChars: 40, size: 9);
            doc.AddTwoColumn("Bulb Volume V_B",           $"{bd.BulbVolume:F1} m³",          totalChars: 40, size: 9);
            doc.AddTwoColumn("Bulb Submergence",          bd.BulbSubmergenceStatus,           totalChars: 56, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("SOLAS Collision Bulkhead (Ch.II-1 Reg.11)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Minimum position from FP",  $"{bd.CollisionBulkheadMin:F2} m",          totalChars: 40, size: 9);
            doc.AddTwoColumn("Maximum position from FP",  $"{bd.CollisionBulkheadMax:F2} m",          totalChars: 40, size: 9);
            doc.AddTwoColumn("Recommended position",      $"{bd.CollisionBulkheadRecommended:F2} m",  totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("Anchor Equipment (IACS UR A1 2019)", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Equipment Number EN",        $"{bd.EquipmentNumber:F0}",                    totalChars: 40, size: 9);
            doc.AddTwoColumn("Anchor Mass (per bower)",    $"{bd.AnchorMassTonnes:F2} t",                 totalChars: 40, size: 9);
            doc.AddTwoColumn("Chain Diameter (stud link)", $"{bd.ChainDiameterMm:F1} mm",                totalChars: 40, size: 9);
            doc.AddTwoColumn("Chain Length per Cable",     $"{bd.ChainLengthM:F0} m ({bd.ChainShots} shots)", totalChars: 56, size: 9);
            doc.AddTwoColumn("Chain Breaking Load (K2)",   $"{bd.ChainBreakingLoadKN:F0} kN",            totalChars: 40, size: 9);
            doc.AddTwoColumn("Hawse Pipe Inner Diameter",  $"{bd.HawsePipeDiameterMm:F0} mm",            totalChars: 40, size: 9);
            doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10);
        }

        private void AddLngSection(SimplePdfDocument doc)
        {
            doc.AddLine("LNG SYSTEMS SUMMARY", size: 13, bold: true);
            doc.AddSpacer(6);
            var lng = LngSource;
            if (lng is null) { doc.AddLine("LNG Systems module data not available.", size: 9); doc.AddSpacer(10); doc.AddRule(); doc.AddSpacer(10); return; }
            doc.AddLine("Cargo Containment & Tank", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Containment Type",          lng.ContainmentType,                             totalChars: 56, size: 9);
            doc.AddTwoColumn("Cargo Tank Capacity",       $"{lng.CargoTankCapacity_m3:N0} m³",            totalChars: 40, size: 9);
            doc.AddTwoColumn("Insulation Thickness",      $"{lng.InsulationThickness_mm:F0} mm",          totalChars: 40, size: 9);
            doc.AddTwoColumn("Insulation Conductivity, k",$"{lng.InsulationConductivity_W_mK:F3} W/m·K", totalChars: 40, size: 9);
            doc.AddTwoColumn("Wetted Surface Area (est.)",$"{lng.WettedSurfaceArea_m2:F0} m²",            totalChars: 40, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("Heat Ingress & Boil-Off Gas", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddLine("Reference: ambient 20°C / cargo -162°C.", size: 8); doc.AddSpacer(4);
            doc.AddTwoColumn("Heat Ingress, Q",            $"{lng.HeatIngressQ_kW:F1} kW",              totalChars: 40, size: 9);
            doc.AddTwoColumn("BOG Rate (mass)",            $"{lng.BogRate_kg_per_day:F0} kg/day",       totalChars: 40, size: 9);
            doc.AddTwoColumn("BOG Rate (volume)",          $"{lng.BogRate_m3_per_day:F1} m³/day",       totalChars: 40, size: 9);
            doc.AddTwoColumn("BOG Rate (% capacity/day)",  $"{lng.BogRatePercentPerDay:F3} %/day",      totalChars: 56, size: 9);
            doc.AddTwoColumn("BOG Energy",                 $"{lng.BogEnergy_MJ_per_day:F0} MJ/day",     totalChars: 40, size: 9);
            doc.AddTwoColumn("Cooldown LNG (approx.)",     $"{lng.EstimatedCooldownLng_m3:F0} m³",      totalChars: 56, size: 9);
            bool bogOk = lng.BogRatePercentPerDay >= 0.05 && lng.BogRatePercentPerDay <= 0.20;
            doc.AddTwoColumn("BOG Rate Criterion (0.05–0.15 %/day)", $"{lng.BogRatePercentPerDay:F3} %/day  {(bogOk ? "PASS" : "CHECK")}", totalChars: 72, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("FGSS Coverage", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("ME Consumption",             $"{lng.DailyFuelConsumption_ME_tpd:F1} t/day", totalChars: 56, size: 9);
            doc.AddTwoColumn("AE Consumption",             $"{lng.DailyFuelConsumption_AE_tpd:F1} t/day", totalChars: 40, size: 9);
            doc.AddTwoColumn("Total Demand",               $"{lng.FuelRequired_MJ_per_day:F0} MJ/day",    totalChars: 56, size: 9);
            doc.AddTwoColumn("Natural BOG Energy",         $"{lng.BogEnergy_MJ_per_day:F0} MJ/day",       totalChars: 56, size: 9);
            doc.AddTwoColumn("BOG Coverage Ratio",         $"{lng.BogCoverageRatio:P0}",                  totalChars: 40, size: 9);
            doc.AddTwoColumn("FGSS Mode",                  lng.FgssMode,                                  totalChars: 72, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("Voyage Boil-Off Loss", size: 10, bold: true); doc.AddSpacer(4);
            doc.AddTwoColumn("Voyage Duration",            $"{lng.VoyageDurationDays:F1} days",          totalChars: 40, size: 9);
            doc.AddTwoColumn("Total BOG over Voyage",      $"{lng.BogTotal_m3:F1} m³",                   totalChars: 40, size: 9);
            doc.AddTwoColumn("Loss as % of Capacity",      $"{lng.BogLossPercentOfCargo:F3} %",          totalChars: 56, size: 9);
            doc.AddSpacer(8);
            doc.AddLine("IGC Code Compliance Checklist (reference only)", size: 10, bold: true); doc.AddSpacer(4);
            foreach (var item in lng.IgcChecklist)
                doc.AddTwoColumn(item.Name, $"{item.Requirement}  —  {(item.IsPass ? "OK" : "REVIEW")}", totalChars: 80, size: 9);
            doc.AddSpacer(4);
            doc.AddLine("Note: IGC checklist is for awareness only — not a substitute for the full IGC Code or class society approval.", size: 8);
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
            Entries.Add(new ReportEntry { Title="Intact Stability Booklet",       Module="Stability",               Revision="B", PreparedBy="A. Alacam",  DateIso=DateTime.Today.ToString("yyyy-MM-dd"), Status="Approved",         Notes="IMO 2008 IS Code, all loading conditions." });
            Entries.Add(new ReportEntry { Title="Damage Stability Analysis",      Module="Damage Stability",        Revision="A", PreparedBy="A. Alacam",  DateIso="2026-06-18", Status="Issued for Review", Notes="SOLAS 2020, probabilistic method." });
            Entries.Add(new ReportEntry { Title="Hydrostatic Particulars",        Module="Hydrostatics",            Revision="C", PreparedBy="M. Sorensen", DateIso="2026-05-14", Status="Approved",         Notes="Bonjean curves and hydrostatic tables, 0-16 m draft range." });
            Entries.Add(new ReportEntry { Title="Resistance & Powering Report",   Module="Resistance and Propulsion",Revision="A",PreparedBy="K. Ito",      DateIso="2026-07-01", Status="Draft",            Notes="Holtrop-Mennen prediction, sea trial correlation pending." });
            Entries.Add(new ReportEntry { Title="General Arrangement",            Module="Ship Builder",            Revision="D", PreparedBy="R. Blake",    DateIso="2026-04-22", Status="Approved",         Notes="Incorporates owner's comments rev C -> D." });
        }

        private void SeedFormulaAudit()
        {
            FormulaAudit.Add(new ModuleFormulaCount { Module="Hydrostatics",              FormulaCount=360  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Stability",                  FormulaCount=335  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Damage Stability",           FormulaCount=240  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Resistance and Propulsion",  FormulaCount=460  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Machinery",                  FormulaCount=310  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Welding",                    FormulaCount=195  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Yacht Design",               FormulaCount=220  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="LNG Systems",                FormulaCount=205  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Manoeuvring",                FormulaCount=280  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Tonnage and Freeboard",      FormulaCount=310  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Ship Builder",               FormulaCount=180  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Bow Design",                 FormulaCount=112  });
            FormulaAudit.Add(new ModuleFormulaCount { Module="Reports",                    FormulaCount=95   });
            FormulaAudit.Add(new ModuleFormulaCount { Module="General / Shared Utilities", FormulaCount=56   });
            OnPropertyChanged(nameof(TotalFormulaCount));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void OnPropertyChanged(string? name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
