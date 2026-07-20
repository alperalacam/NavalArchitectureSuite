import fs from "node:fs/promises";
import path from "node:path";
import { Workbook, SpreadsheetFile } from "@oai/artifact-tool";

const outDir = "C:\\Temp\\NavalArchitectureSuite\\outputs\\outstanding_topics";
const previewDir = path.join(outDir, "previews");
await fs.mkdir(previewDir, { recursive: true });

const C = { navy:"#0A1F3D", blue:"#1B3A6B", mid:"#2E6DA4", gold:"#C8960C", pale:"#D6E8F7", input:"#FFF3CD", calc:"#D9EAD3", gray:"#F2F2F2", dark:"#404040", white:"#FFFFFF", alt:"#E3F2FD", red:"#F4CCCC" };
const author = "Alper Alacam  |  Naval Architect and Marine Engineer  |  Turkiye  |  2026";
const asOf = "Technical status checked 19 July 2026. Educational screening tool only; flag Administration and classification society requirements govern.";

function mergeValue(s, addr, value) { s.getRange(addr).merge(); s.getRange(addr.split(":")[0]).values=[[value]]; }
function baseSheet(s) {
  s.showGridLines=false; s.getRange("A1:A35").format.columnWidth=3;
  s.getRange("B1:B35").format.columnWidth=38; s.getRange("C1:C35").format.columnWidth=16;
  s.getRange("D1:D35").format.columnWidth=15; s.getRange("E1:E35").format.columnWidth=48;
  s.getRange("A1:E35").format.font={typeface:"Arial",fontSize:9,color:"#000000"};
}
function title(s, text, intro) {
  mergeValue(s,"B2:E2",text); s.getRange("B2:E2").format={fill:C.blue,font:{bold:true,color:C.gold,fontSize:14},horizontalAlignment:"center"};
  mergeValue(s,"B3:E3",intro); s.getRange("B3:E3").format={fill:C.pale,font:{italic:true,color:C.dark,fontSize:9},wrapText:true,horizontalAlignment:"left"};
  s.getRange("B3:E3").format.rowHeight=52;
}
function section(s,row,text) {
  mergeValue(s,`B${row}:E${row}`,text); s.getRange(`B${row}:E${row}`).format={fill:C.mid,font:{bold:true,color:C.white,fontSize:11},horizontalAlignment:"center"};
}
function headers(s,row,labels=["Parameter","Value","Unit","Explanation / source"]) {
  s.getRange(`B${row}:E${row}`).values=[labels]; s.getRange(`B${row}:E${row}`).format={fill:C.dark,font:{bold:true,color:C.white,fontSize:10},horizontalAlignment:"center",wrapText:true};
}
function inputRows(s,start,rows) {
  s.getRange(`B${start}:E${start+rows.length-1}`).values=rows;
  s.getRange(`B${start}:B${start+rows.length-1}`).format={fill:C.gray,font:{bold:true}};
  s.getRange(`C${start}:C${start+rows.length-1}`).format={fill:C.input,font:{color:"#0000FF",fontSize:10},horizontalAlignment:"center",borders:{preset:"all",style:"thin",color:"#AAAAAA"}};
  s.getRange(`D${start}:D${start+rows.length-1}`).format={fill:C.gray,font:{italic:true,color:"#808080"},horizontalAlignment:"center"};
  s.getRange(`E${start}:E${start+rows.length-1}`).format={fill:C.pale,font:{italic:true,color:C.dark},wrapText:true};
  s.getRange(`B${start}:E${start+rows.length-1}`).format.rowHeight=34;
}
function dataTable(s,start,headersArr,rows,width=4) {
  const endCol=String.fromCharCode(65+1+width-1);
  s.getRange(`B${start}:${endCol}${start}`).values=[headersArr]; s.getRange(`B${start}:${endCol}${start}`).format={fill:C.dark,font:{bold:true,color:C.white},horizontalAlignment:"center",wrapText:true};
  if(rows.length){ s.getRange(`B${start+1}:${endCol}${start+rows.length}`).values=rows;
    for(let i=0;i<rows.length;i++) s.getRange(`B${start+1+i}:${endCol}${start+1+i}`).format={fill:i%2?C.white:C.alt,wrapText:true};
    s.getRange(`B${start+1}:B${start+rows.length}`).format.font={bold:true};
  }
}
function calcRows(s,start,rows) {
  s.getRange(`B${start}:E${start+rows.length-1}`).values=rows.map(r=>[r[0],null,r[2],r[3]]);
  rows.forEach((r,i)=>{ if(r[1]) s.getRange(`C${start+i}`).formulas=[[r[1]]]; });
  s.getRange(`B${start}:E${start+rows.length-1}`).format={fill:C.calc,wrapText:true};
  s.getRange(`B${start}:B${start+rows.length-1}`).format.font={bold:true};
  s.getRange(`C${start}:C${start+rows.length-1}`).format={font:{bold:true},horizontalAlignment:"center"};
  s.getRange(`C${start}:C${start+rows.length-1}`).format.numberFormat="0.000";
  s.getRange(`D${start}:D${start+rows.length-1}`).format={font:{italic:true,color:"#808080"},horizontalAlignment:"center"};
  s.getRange(`B${start}:E${start+rows.length-1}`).format.rowHeight=32;
}
function sourceFooter(s,row,text) {
  mergeValue(s,`B${row}:E${row}`,`Sources and limits: ${text}`); s.getRange(`B${row}:E${row}`).format={font:{italic:true,color:"#777777",fontSize:8},wrapText:true}; s.getRange(`B${row}:E${row}`).format.rowHeight=42;
}
function cover(wb,spec) {
  const s=wb.worksheets.add("COVER"); baseSheet(s); s.getRange("B2:E9").format.fill=C.navy;
  mergeValue(s,"B3:E3","NAVAL ARCHITECTURE"); s.getRange("B3:E3").format={fill:C.navy,font:{bold:true,color:C.gold,fontSize:26},horizontalAlignment:"center"};
  mergeValue(s,"B4:E4","TEACHING TOOLKIT"); s.getRange("B4:E4").format={fill:C.navy,font:{bold:true,color:C.white,fontSize:20},horizontalAlignment:"center"};
  mergeValue(s,"B5:E5",`VOLUME ${spec.vol} :  ${spec.topic}`); s.getRange("B5:E5").format={fill:C.navy,font:{bold:true,color:C.gold,fontSize:13},horizontalAlignment:"center"};
  mergeValue(s,"B6:E6",spec.subtitle); s.getRange("B6:E6").format={fill:C.navy,font:{italic:true,color:C.pale,fontSize:10},horizontalAlignment:"center"};
  mergeValue(s,"B8:E8",author); s.getRange("B8:E8").format={fill:C.navy,font:{color:C.white,fontSize:10},horizontalAlignment:"center"};
  section(s,10,"ABOUT THIS VOLUME");
  const about=[["Why this topic matters",spec.why],["What this volume covers",spec.covers],["Verification boundary",spec.boundary],["Primary references",spec.primary]];
  s.getRange("B11:C14").values=about; s.getRange("B11:B14").format={fill:C.gray,font:{bold:true,color:C.blue}}; s.getRange("C11:C14").format={fill:C.gray,font:{color:C.dark},wrapText:true}; s.getRange("B11:C14").format.rowHeight=58;
  section(s,16,"WORKSHEET INDEX"); s.getRange("B17:D17").values=[["No.","Sheet","Contents"]]; s.getRange("B17:D17").format={fill:C.dark,font:{bold:true,color:C.white},horizontalAlignment:"center"};
  const indexRows=[["1","COVER","Scope, references and verification boundary"],...spec.sheets.map((x,i)=>[String(i+2),x.name,x.desc])];
  s.getRange(`B18:D${17+indexRows.length}`).values=indexRows;
  for(let i=0;i<indexRows.length;i++) s.getRange(`B${18+i}:D${18+i}`).format={fill:i%2? "#CFE2F3":C.white,wrapText:true};
  s.getRange(`B18:B${17+indexRows.length}`).format={font:{bold:true,color:C.blue},horizontalAlignment:"center"};
  s.getRange(`C18:C${17+indexRows.length}`).format.font={bold:true,color:C.mid};
  mergeValue(s,"B31:E31","© 2026 Alper Alacam Naval Architecture . All rights reserved."); s.getRange("B31:E31").format={fill:C.navy,font:{italic:true,color:C.gold,fontSize:8},horizontalAlignment:"center"};
  s.freezePanes.freezeRows(2);
}
function theory(wb,sh) {
  const s=wb.worksheets.add(sh.name); baseSheet(s); title(s,sh.title,sh.intro+" "+asOf); section(s,5,sh.section||"KEY KNOWLEDGE");
  dataTable(s,6,sh.headers||["Item","Meaning","Design use","Authority / limitation"],sh.rows,4);
  sourceFooter(s,8+sh.rows.length,sh.sources); s.freezePanes.freezeRows(6);
}
function refs(wb,rows) {
  const s=wb.worksheets.add("REFERENCE TABLES"); baseSheet(s); title(s,"REFERENCE TABLES -- AUTHORITATIVE SOURCES",asOf);
  section(s,5,"SOURCE REGISTER"); dataTable(s,6,["Instrument / source","Issuer","Use in this volume","Official URL"],rows,4);
  s.getRange(`E7:E${6+rows.length}`).format={fill:C.alt,font:{color:"#0563C1"},wrapText:true};
  sourceFooter(s,8+rows.length,"URLs are recorded in plain text for audit. Always confirm amendments, flag circulars and the selected class rules at project start.");
}
const commonRefs={
 stability:[
 ["2008 IS Code, MSC.267(85)","IMO","Intact stability framework and general criteria","https://wwwcdn.imo.org/localresources/en/KnowledgeCentre/IndexofIMOResolutions/MSCResolutions/MSC.267(85).pdf"],
 ["SOLAS chapter II-1","IMO","Subdivision and damage stability framework","https://www.imo.org/en/ourwork/safety/pages/shipdesignandstability-default.aspx"],
 ["2008 SPS Code, MSC.266(84)","IMO","Special purpose ships","https://wwwcdn.imo.org/localresources/en/KnowledgeCentre/IndexofIMOResolutions/MSCResolutions/MSC.266(84).pdf"],
 ["OSV Guidelines, MSC.235(82)","IMO","Offshore supply vessel design and damage stability","https://wwwcdn.imo.org/localresources/en/KnowledgeCentre/IndexofIMOResolutions/MSCResolutions/MSC.235(82).pdf"],
 ],
 maneuver:[
 ["Standards for Ship Manoeuvrability, MSC.137(76)","IMO","Trial criteria and applicability","https://wwwcdn.imo.org/localresources/en/KnowledgeCentre/IndexofIMOResolutions/MSCResolutions/MSC.137(76).pdf"],
 ["Full Scale Manoeuvring Trials 7.5-04-02-01 Rev.04","ITTC","Trial conduct and environmental controls","https://www.ittc.info/media/11976/75-04-02-01.pdf"],
 ],
 production:[
 ["Recommendation 47 Rev.10 Corr.1 (Oct 2025)","IACS","Shipbuilding and repair quality standard","https://iacs.org.uk/resolutions/recommendations/41-60/rec-47-rev10-cln"],
 ["SOLAS 1974, as amended","IMO","Construction, equipment and safety framework","https://www.imo.org/en/about/conventions/pages/international-convention-for-the-safety-of-life-at-sea-(solas),-1974.aspx"],
 ],
 noise:[
 ["MEPC.1/Circ.906","IMO","Revised guidelines for reducing underwater radiated noise","https://glonoise.imo.org/documents/2436"],
 ["ISO 17208-1:2016 + Amd 1:2024","ISO","Precision measurement in deep water","https://www.iso.org/standard/62408.html"],
 ],
 polar:[
 ["International Code for Ships Operating in Polar Waters","IMO","Mandatory safety and environmental framework","https://www.imo.org/en/ourwork/safety/pages/polar-code.aspx"],
 ["Polar Code supplement, January 2026","IMO","Amendments entering into force 1 January 2026","https://wwwcdn.imo.org/localresources/en/publications/Documents/Supplements/English/2Q191E_Supplement_January2026_EBK.pdf"],
 ["Unified Requirements, Polar Class","IACS","Polar Class structural machinery requirements register","https://iacs.org.uk/resolutions/unified-requirements"],
 ]};

const specs=[
{vol:29,topic:"Stability of Special Vessels",subtitle:"SPS. OSV. Tugs. Crane Vessels. Dredgers. Fishing Vessels.",why:"Special vessels experience heeling moments and loading modes that ordinary cargo-ship checks may not represent: lifting, towing, deck cargo, liquids with free surface, personnel distribution and mission equipment.",covers:"Applicability screening; external heeling moments; crane and towing examples; free-surface control; operational loading-condition register; approval evidence.",boundary:"The workbook calculates transparent first-principles screening quantities. It does not replace an approved stability booklet, limiting KG curves, downflooding analysis or Administration/class review.",primary:"IMO 2008 IS Code; 2008 SPS Code; IMO OSV Guidelines; applicable flag and class rules.",
sheets:[
{name:"APPLICABILITY",desc:"Select governing vessel/instrument path",title:"APPLICABILITY -- SPECIAL VESSEL STABILITY",intro:"Vessel label alone does not establish the governing criteria. Confirm service, size, persons, cargo and flag.",rows:[
["Special purpose ship","Mechanically self-propelled; special personnel definition and scope are set by the SPS Code.","Screen persons, GT and construction date.","MSC.266(84); Administration decides application."],
["Offshore supply vessel","Decked vessel serving offshore installations with distinctive deck cargo and tank arrangements.","Check OSV guideline scope and operational role.","MSC.235(82); length and GT application clauses matter."],
["Tug / towing vessel","Towline force can create a large transverse heeling moment.","Model tow point, force direction and tripping release.","Use flag/class towing rules; no universal criterion inserted here."],
["Crane / lifting vessel","Suspended load shifts the combined centre of gravity and creates heel.","Analyse every lift radius, outreach, hook load and environmental limit.","Use approved lifting/stability manual."],
["Dredger / hopper dredger","Slurry free surface, density uncertainty and dredging operations affect stability.","Control density, hopper level, overflow and spoil shift.","Apply vessel-specific flag/class rules."],
["Fishing vessel","Gear, catch, icing and water on deck are key hazards.","Use fishing-vessel instrument applicable to length/area.","Do not apply cargo-ship criteria blindly."]],sources:"IMO MSC.266(84), MSC.235(82), MSC.267(85)."},
{name:"HEELING MOMENTS",desc:"Crane and towline screening calculator",custom:"heel"},
{name:"FREE SURFACE",desc:"Free-surface correction calculator",custom:"fs"},
{name:"LOAD CONDITIONS",desc:"Minimum operational condition register",title:"LOAD CONDITIONS -- EVIDENCE REGISTER",intro:"Each row is an analysis case to be completed in the approved stability model.",headers:["Condition","Main risk driver","Required evidence","Status / owner"],rows:[
["Departure full load","Maximum consumables and mission load","Approved intact and damage results","Project input required"],
["Arrival / minimum consumables","High KG and reduced displacement","Approved intact and damage results","Project input required"],
["Worst free-surface case","Slack tanks / hopper / deck liquids","FSM list and corrected GM/GZ","Project input required"],
["Maximum crane lift","Hook load and outreach envelope","Lift curve, heel, residual GZ, downflooding","Project input required"],
["Maximum towline case","Bollard pull and tow point geometry","Towline heeling and release demonstration","Project input required"],
["Damage cases","Governing compartment groups","Approved subdivision model and results","Project input required"]],sources:"Project stability basis plus applicable IMO, flag and class requirements."},
{name:"OPERATING LIMITS",desc:"Limit and evidence checklist",title:"OPERATING LIMITS -- CONTROL PHILOSOPHY",intro:"Limits must be observable, measurable and tied to an action.",headers:["Limit","Monitored variable","Typical control","Required project evidence"],rows:[
["Maximum KG / minimum GM","Loading computer or condition sheet","Block loading if limit exceeded","Approved limiting KG/GM curve"],
["Downflooding margin","Heel angle and closure status","Secure openings; suspend operation","Opening model and operating procedure"],
["Lift envelope","Load, radius, boom azimuth, wind","Rated-capacity limiter / stop-work","Approved crane-stability envelope"],
["Tow envelope","Towline force and direction","Quick release / gob wire procedure","Approved towing manual"],
["Tank state","Fill %, density, free surface","Tank sequence and prohibited slack states","Approved tank plan"],
["Weather limit","Wind, wave, icing forecast","Suspend mission / seek shelter","Defined basis and master’s procedure"]],sources:"IMO instrument plus Administration/class-approved operating manuals."},
{name:"ASSURANCE",desc:"Independent verification checklist",title:"ASSURANCE -- DO NOT CONFUSE SCREENING WITH APPROVAL",intro:"A green spreadsheet result is not statutory or class approval.",headers:["Check","Evidence expected","Independent check","Close-out"],rows:[
["Geometry and openings","Hydrostatic model and downflooding points","Model QA / inclining alignment","Open"],
["Lightship and KG","Inclining or lightweight survey","Weight reconciliation","Open"],
["External moments","Crane/tow/mission equipment load basis","Vendor and class review","Open"],
["Free surfaces","Tank geometry and densities","FSM reconciliation","Open"],
["Damage model","Compartments, permeability, cross-flooding","Independent calculation","Open"],
["Operations","Manuals, alarms, limits and training","Survey / drills","Open"]],sources:"Flag Administration and recognized organization approval process."},
]},
{vol:30,topic:"Intact and Damage Stability in Depth",subtitle:"GZ Integration. Free Surface. Limiting KG. Probabilistic Damage.",why:"Stability compliance depends on the complete loading condition, righting-arm curve, openings, environmental criteria and—where applicable—probabilistic or deterministic damage analysis.",covers:"GZ-curve integration; selected 2008 IS Code general criteria; free-surface correction; limiting-KG logic; A/R damage index screening; model and evidence QA.",boundary:"The GZ worksheet demonstrates numerical integration and selected general criteria only. Applicability, weather criterion, special criteria and all damage cases require the approved vessel model and current statutory rules.",primary:"IMO 2008 IS Code (MSC.267(85)); SOLAS chapter II-1; applicable Administration/class rules.",
sheets:[
{name:"GZ CURVE",desc:"Editable curve and trapezoidal area calculation",custom:"gz"},
{name:"IS CRITERIA",desc:"Selected general criteria check",custom:"is"},
{name:"FREE SURFACE",desc:"Virtual-rise correction and sensitivity",custom:"fs2"},
{name:"LIMITING KG",desc:"GM-based limiting KG screening",custom:"kg"},
{name:"DAMAGE INDEX",desc:"A/R probabilistic screening table",custom:"damage"},
{name:"MODEL QA",desc:"Hydrostatic and damage-model checks",title:"MODEL QA -- REQUIRED BEFORE RELYING ON RESULTS",intro:"Most serious stability errors originate in geometry, openings, permeability, loading data or inconsistent conventions.",headers:["QA item","Check method","Acceptance evidence","Status"],rows:[
["Coordinate system","Confirm axes, signs, baseline and reference points","Model basis note","Open"],
["Watertight boundaries","Compare compartment model to approved plans","Compartment audit","Open"],
["Openings","Verify downflooding type, height and closure","Opening schedule","Open"],
["Tank calibration","Reconcile volumes, LCG/TCG/VCG and FSM","Tank-table comparison","Open"],
["Lightship","Reconcile displacement and KG to inclining test","Approved report","Open"],
["Damage cases","Confirm extent, permeability and intermediate stages","Case register / independent check","Open"],
["Numerics","Refine heel/trim steps around limiting events","Convergence note","Open"]],sources:"IMO SOLAS II-1 and 2008 IS Code; selected class software approval procedures."},
]},
{vol:31,topic:"Ship Manoeuvring and Hydrodynamics",subtitle:"Turning. Zig-Zag. Stopping. Shallow Water. Trial Corrections.",why:"Manoeuvrability is a coupled hydrodynamic response involving hull, propulsor, rudder, controls and environment. Trial results must be measured and interpreted under controlled conditions.",covers:"IMO trial criteria calculator; turning and stopping normalization; zig-zag limits; non-dimensional hydrodynamic derivatives; shallow-water and environmental influence checklist; trial QA.",boundary:"The IMO criteria sheet applies only within MSC.137(76) scope and stated trial conditions. Non-conventional propulsion and restricted-water effects need dedicated analysis.",primary:"IMO MSC.137(76); ITTC Full Scale Manoeuvring Trials 7.5-04-02-01 Rev.04 (2024).",
sheets:[
{name:"IMO TRIAL CHECK",desc:"Turning, stopping and zig-zag criteria",custom:"imo"},
{name:"HYDRO DERIVATIVES",desc:"Derivative sign and model register",title:"HYDRODYNAMIC DERIVATIVES -- MODEL REGISTER",intro:"Record convention and source before comparing coefficients; signs vary across references and software.",headers:["Derivative / term","Physical meaning","Expected role","Verification"],rows:[
["Y_v","Sway force sensitivity to lateral velocity","Lateral damping / stability","Confirm sign convention"],
["N_v","Yaw moment sensitivity to lateral velocity","Directional stability coupling","Confirm sign convention"],
["Y_r","Sway force sensitivity to yaw rate","Rotational coupling","Confirm sign convention"],
["N_r","Yaw moment sensitivity to yaw rate","Yaw damping","Confirm sign convention"],
["Rudder force","Control force versus angle and inflow","Turning and course keeping","Validate behind-hull inflow"],
["Propeller-rudder interaction","Slipstream and thrust deduction effects","Low-speed and ahead response","Validate propulsion state"]],sources:"Use a documented MMG, Abkowitz or project model; do not mix coefficient normalizations."},
{name:"TURNING ANALYSIS",desc:"Advance and tactical diameter normalization",custom:"turn"},
{name:"STOPPING ANALYSIS",desc:"Track reach and time normalization",custom:"stop"},
{name:"SHALLOW WATER",desc:"Depth and blockage screening",title:"SHALLOW WATER -- EFFECT REGISTER",intro:"Restricted water can materially change resistance, sinkage, trim, turning and stopping; this sheet flags analysis needs.",headers:["Indicator","Calculation / observation","Hydrodynamic implication","Action"],rows:[
["Depth-to-draft h/T","Primary screening ratio","Smaller ratio increases bottom interaction","Run restricted-water model/trials"],
["Under-keel clearance","Depth minus dynamic draft","Squat may consume margin","Include speed-dependent squat"],
["Blockage","Ship section / channel section","Bank/channel effects increase with blockage","Use channel-specific analysis"],
["Bank proximity","Lateral clearance and geometry","Suction and bow cushion create yaw","Plan speed and tug support"],
["Passing vessel","Relative speed and separation","Transient sway/yaw forces","Traffic controls"],
["Wind/current","Measured vectors","Biases track and apparent response","Apply ITTC trial controls/corrections"]],sources:"ITTC 7.5-04-02-01 Rev.04; project port/channel studies."},
{name:"TRIAL QA",desc:"Trial condition and instrumentation checklist",title:"TRIAL QA -- FULL-SCALE MANOEUVRING",intro:"Document conditions so results are repeatable and interpretable.",headers:["Item","Record","Why it matters","Status"],rows:[
["Loading condition","Drafts, trim, displacement, GM","Defines hydrodynamic state","Open"],
["Water depth / bathymetry","Depth along track","Bottom interaction","Open"],
["Wind / waves / current","Time-synchronized measurements","Track and force bias","Open"],
["Propulsion state","RPM/power/pitch and engine orders","Response and repeatability","Open"],
["Rudder / azimuth","Command and feedback histories","Control input fidelity","Open"],
["Position and heading","High-rate GNSS/INS data","Advance, transfer, rates","Open"],
["Repeat runs","Port/starboard and repeated cases","Bias and uncertainty","Open"]],sources:"ITTC Full Scale Manoeuvring Trials, effective 2024."},
]},
{vol:32,topic:"Ship Production and Outfitting",subtitle:"Block Construction. Zone Outfitting. Productivity. Quality. Completion.",why:"Production performance is governed by design maturity, material availability, work sequencing, access, quality and completion—not welding rate alone.",covers:"Work-package planning; earned-hours progress; block and zone strategy; outfit maturity gates; dimensional and welding quality; testing and mechanical completion.",boundary:"Productivity benchmarks are project inputs, not universal constants. Contract, yard procedures, class rules, statutory requirements and OEM instructions control.",primary:"IACS Recommendation 47 Rev.10 Corr.1 (Oct 2025); SOLAS; yard quality system and class-approved plans.",
sheets:[
{name:"WORK PACKAGES",desc:"Formula-driven progress and variance tracker",custom:"prod"},
{name:"BLOCK STRATEGY",desc:"Block/zone planning principles",title:"BLOCK STRATEGY -- DESIGN FOR PRODUCTION",intro:"The preferred breakdown minimizes difficult joins, uncontrolled distortion, rework and late access conflicts.",headers:["Decision","Key inputs","Production objective","Evidence"],rows:[
["Block boundaries","Crane capacity, berth, geometry, systems","Large stable blocks with accessible joins","Block plan and lift study"],
["Build orientation","Welding position, outfitting access, coating","Maximize downhand work and safe access","Erection method"],
["Grand-blocking","Transport and crane envelope","Reduce berth duration","Lift/transport analysis"],
["Datum strategy","Baseline, control points, survey network","Control accumulated dimensional error","Dimensional plan"],
["Temporary structure","Lugs, strongbacks, bracing","Safe lifting and distortion control","Approved temporary works"],
["Erection sequence","Hull strength, access, systems, weather","Stable structure and logical completion","Integrated master schedule"]],sources:"IACS Rec.47 plus yard-specific engineering and lifting procedures."},
{name:"ZONE OUTFITTING",desc:"Outfitting maturity gates",title:"ZONE OUTFITTING -- MATURITY GATES",intro:"Move work earlier only when design, material, preservation and access are ready.",headers:["Gate","Required condition","Typical evidence","Stop condition"],rows:[
["Design released","Approved coordinated model/drawings","IFC package and change status","Open design clashes"],
["Material available","Correct items, certificates and preservation","Kitting list / traceability","Shortage or wrong revision"],
["Pre-outfit ready","Foundations, penetrations and supports defined","Zone work package","Hot work conflicts"],
["Block closure ready","Inspection and hidden work accepted","Inspection release","Unclosed NCR"],
["Testing ready","System boundaries and temporary services defined","Test pack","Incomplete isolation"],
["Handover ready","Punch list at agreed category","Completion certificate","Safety-critical open item"]],sources:"Project completion system, class survey plan and yard procedures."},
{name:"QUALITY CONTROL",desc:"Production quality checkpoints",title:"QUALITY CONTROL -- BUILD IT RIGHT FIRST TIME",intro:"Acceptance values must come from the applicable approved standard; this volume does not invent tolerances.",headers:["Control point","Method","Governing acceptance","Record"],rows:[
["Material identity","Certificate and marking traceability","Class-approved material specification","Material register"],
["Fit-up","Gap, alignment, edge preparation","Approved WPS / IACS Rec.47 where adopted","Fit-up inspection"],
["Welding","Parameters, consumables, environment","Approved WPS and welder qualification","Weld log"],
["NDT","VT/MT/PT/UT/RT as specified","Approved NDT plan and acceptance criteria","NDT report"],
["Dimensions","Survey datums and key geometry","Approved dimensional standard","Survey report"],
["Coating","Preparation, climate, DFT, holidays","Approved coating specification","Coating report"],
["Pressure / function test","Boundary, medium, pressure and safety","Approved test procedure","Signed test pack"]],sources:"IACS Rec.47 Rev.10 Corr.1; class rules; approved project specifications."},
{name:"COMPLETION",desc:"System completion and punch control",custom:"completion"},
{name:"PRODUCTION RISKS",desc:"Leading risk register",title:"PRODUCTION RISKS -- LEADING INDICATORS",intro:"Track causes before they become schedule variance.",headers:["Risk","Leading indicator","Mitigation","Owner input"],rows:[
["Late design","IFC release vs need date","Freeze sequence / early clash resolution","Assign"],
["Material shortage","Kit completeness","Shortage escalation / alternates approval","Assign"],
["Rework","NCR and weld repair trend","Root-cause and first-piece control","Assign"],
["Access congestion","Trade stacking / hot work conflicts","4D zone planning","Assign"],
["Testing backlog","Open test packs and temporary services","System-based completion plan","Assign"],
["Change growth","Late modifications and revision churn","Change control / impact review","Assign"]],sources:"Project controls and yard quality records; no universal benchmark assumed."},
]},
{vol:33,topic:"Underwater Noise and Signatures",subtitle:"URN Sources. Measurement. Normalization. Mitigation. Verification.",why:"Underwater radiated noise affects marine life, contractual signatures and vessel detectability. Meaningful comparison requires controlled operating conditions, calibrated measurement and explicit processing.",covers:"One-third-octave data handling; distance normalization screening; energetic summation; source-path-receiver model; cavitation and machinery controls; measurement and mitigation plans.",boundary:"The acoustic calculator is a free-field screening demonstration. ISO 17208 procedures, site corrections, Lloyd’s mirror, propagation, background noise and uncertainty require competent specialist treatment.",primary:"IMO MEPC.1/Circ.906; ISO 17208-1:2016 with Amendment 1:2024.",
sheets:[
{name:"URN SPECTRUM",desc:"Distance normalization and energetic sum",custom:"noise"},
{name:"SOURCE PATH",desc:"Source-path-receiver register",title:"SOURCE-PATH-RECEIVER -- DIAGNOSTIC FRAMEWORK",intro:"Mitigation is most effective when tied to a verified source and transmission path.",headers:["Source","Dominant mechanism","Transmission path","Diagnostic evidence"],rows:[
["Propeller","Cavitation and unsteady loading","Direct water radiation / hull pressure","Cavitation observation, pressure pulses, spectrum"],
["Main engine","Combustion and mechanical orders","Foundations to hull shell","Order tracking / transfer mobility"],
["Gearbox","Gear mesh and bearing tones","Structure-borne through seating","Narrowband tonal analysis"],
["Pumps / auxiliaries","Hydraulic and rotating forces","Pipework and foundations","Run-up / on-off correlation"],
["Flow","Appendages, openings, turbulence","Direct hydrodynamic pressure","Speed scaling / CFD / inspection"],
["Electrical drive","Electromagnetic and switching tones","Mounts, cables, structure","Order/switching-frequency correlation"]],sources:"IMO MEPC.1/Circ.906 diagnostic and mitigation framework; specialist acoustic analysis."},
{name:"CAVITATION",desc:"Cavitation risk and evidence checklist",title:"CAVITATION -- CONTROL REGISTER",intro:"Cavitation inception and extent depend on local inflow, loading, pressure and surface condition.",headers:["Driver","Evidence","Design/operational control","Verification"],rows:[
["Wake non-uniformity","Wake survey or validated CFD","Hull/appendage/wake optimization","Model test / full-scale correlation"],
["Propeller loading","Thrust, RPM, blade loading","Diameter, area ratio, skew, RPM strategy","Cavitation tunnel / validated method"],
["Tip clearance","Geometry and pressure pulses","Increase clearance / reduce local loading","Drawing and test evidence"],
["Surface condition","Roughness, fouling, damage","Finish and maintenance standard","Inspection"],
["Air ingestion","Sea state and stern emergence","Draft/trim/operating limit","Trials across conditions"],
["Off-design operation","Low pitch/high RPM, manoeuvring","Control logic and operating guidance","Sea trial matrix"]],sources:"IMO MEPC.1/Circ.906; project propeller design and test reports."},
{name:"MEASUREMENT PLAN",desc:"ISO-aligned test planning checklist",title:"MEASUREMENT PLAN -- TRACEABLE DATA",intro:"Do not compare spectra unless operating state, geometry, processing and uncertainty are documented.",headers:["Item","Record","Reason","Status"],rows:[
["Vessel condition","Draft, trim, displacement, machinery line-up","Defines source state","Open"],
["Operating point","Speed through water, RPM, power, pitch","Noise strongly depends on operating point","Open"],
["Site","Depth, bathymetry, sound-speed profile, sea state","Propagation and reflections","Open"],
["Geometry","CPA, hydrophone depth and track","Normalization and averaging","Open"],
["Instrumentation","Calibration, sensitivity, bandwidth, sampling","Traceability","Open"],
["Background","Pre/post ambient measurements","Signal-to-noise assessment","Open"],
["Processing","Windowing, bands, averaging, corrections","Reproducibility","Open"],
["Uncertainty","Contributors and combined result","Decision confidence","Open"]],sources:"ISO 17208-1 scope and method; use the full purchased standard for compliance."},
{name:"MITIGATION",desc:"Design and operational measure register",title:"MITIGATION -- OPTIONS AND TRADE-OFFS",intro:"Select measures through source diagnosis and verify that safety, efficiency and durability are not impaired.",headers:["Measure","Target","Trade-off / dependency","Verification"],rows:[
["Propeller/wake optimization","Cavitation and pressure pulses","Efficiency, clearance, structural loads","Model test / CFD / trials"],
["Resilient mounting","Machinery structure-borne noise","Alignment, motion, service life","Mobility / insertion loss"],
["Flexible connections","Pipe and duct paths","Pressure, fatigue, fire safety","Installation inspection / test"],
["Hull treatment","Radiating panels","Weight, space, fire and maintainability","Panel response / sea trial"],
["Maintenance","Fouling, damage, bearings","Docking and inspection access","Before/after measurement"],
["Speed management","Cavitation and flow noise","Schedule and fuel impacts","Verified speed-noise curve"]],sources:"IMO MEPC.1/Circ.906."},
{name:"SIGNATURE QA",desc:"Claims and uncertainty checklist",title:"SIGNATURE QA -- CLAIMS NEED CONTEXT",intro:"A single dB number without band, reference, operating point and uncertainty is not auditable.",headers:["Claim element","Required statement","Example field","Status"],rows:[
["Quantity","SPL/RNL/source level and band definition","1/3-octave RNL","Open"],
["Reference","Reference pressure and distance convention","1 μPa; normalized distance","Open"],
["Operating state","Speed, RPM, power, draft, machinery","Project input","Open"],
["Method","Standard/procedure and processing revision","ISO 17208-1 + Amd 1:2024","Open"],
["Corrections","Geometry, background, propagation treatment","Method-specific","Open"],
["Uncertainty","Expanded uncertainty and coverage","Project result","Open"]],sources:"ISO 17208-1 and project test specification."},
]},
{vol:34,topic:"Ice Class and Polar Operations",subtitle:"Polar Code. Polar Class. POLARIS Inputs. Winterization. PWOM.",why:"Polar operations combine structural ice loads, low-temperature performance, icing, navigation limits, remoteness, environmental protection and crew competence.",covers:"Polar category and certificate pathway; Polar Class register; POLARIS RIO arithmetic with user-supplied official RIVs; winterization; voyage planning; PWOM evidence and 2026 amendment awareness.",boundary:"This workbook does not assign an ice class or operational permission. RIVs and ice concentrations must come from the current official POLARIS material and observed ice regime; class and Administration approval govern.",primary:"IMO Polar Code; January 2026 Polar Code supplement; IACS Polar Class Unified Requirements.",
sheets:[
{name:"POLAR PATHWAY",desc:"Category, certificate and manual path",title:"POLAR PATHWAY -- CERTIFICATION AND LIMITS",intro:"The Polar Code certificate identifies Category A, B or C and records assessed operational limitations.",headers:["Element","Purpose","Project evidence","Authority"],rows:[
["Operational assessment","Identify anticipated conditions and hazards","Hazard assessment and design basis","Administration / recognized organization"],
["Polar Ship Certificate","Record category and limitations","Valid certificate","SOLAS / Polar Code"],
["PWOM","Support master decisions within capabilities","Approved/accepted manual","Polar Code"],
["Structure","Withstand applicable global/local ice loads","Assigned class notation and approved drawings","Class / Administration"],
["Machinery","Function under ice and low temperature","Design and test evidence","Class / Administration"],
["Operations","Navigation, communications, survival, training","Procedures, equipment and competent crew","Company / Administration"]],sources:"IMO Polar Code official overview."},
{name:"POLAR CLASS",desc:"PC1–PC7 capability descriptions",title:"POLAR CLASS -- IACS DESIGNATIONS",intro:"A Polar Class notation is assigned by class after rule compliance; these descriptions do not assign class.",headers:["Class","General IACS description","Use in project","Status"],rows:[
["PC 1","Year-round operation in all polar waters","Confirm current IACS/class wording and notation","Not assigned"],
["PC 2","Year-round operation in moderate multi-year ice conditions","Confirm current IACS/class wording and notation","Not assigned"],
["PC 3","Year-round operation in second-year ice, may include multi-year inclusions","Confirm current IACS/class wording and notation","Not assigned"],
["PC 4","Year-round operation in thick first-year ice, may include old ice inclusions","Confirm current IACS/class wording and notation","Not assigned"],
["PC 5","Year-round operation in medium first-year ice, may include old ice inclusions","Confirm current IACS/class wording and notation","Not assigned"],
["PC 6","Summer/autumn operation in medium first-year ice, may include old ice inclusions","Confirm current IACS/class wording and notation","Not assigned"],
["PC 7","Summer/autumn operation in thin first-year ice, may include old ice inclusions","Confirm current IACS/class wording and notation","Not assigned"]],sources:"IACS Polar Class Unified Requirements; verify the current revision and selected member society rules."},
{name:"POLARIS RIO",desc:"User-supplied RIV and ice concentration arithmetic",custom:"rio"},
{name:"WINTERIZATION",desc:"Low-temperature and icing register",title:"WINTERIZATION -- SYSTEM FUNCTION",intro:"Design environmental conditions and polar service temperature must be project-defined and evidenced.",headers:["System / hazard","Failure mode","Design evidence","Operational control"],rows:[
["Sea water systems","Ice blockage / freezing","Sea-inlet and heating analysis","Monitoring and changeover"],
["Deck machinery","Lubricant, seal and hydraulic degradation","Low-temperature qualification","Warm-up and inspection"],
["Fire safety","Frozen lines / inaccessible equipment","Heating and protection basis","Readiness checks"],
["Life-saving appliances","Deployment and survival limitations","Polar equipment / test evidence","Training and maintenance"],
["Navigation sensors","Icing and low-temperature failure","Environmental qualification","De-icing and redundancy"],
["Battery/electronics","Capacity loss / condensation","Temperature qualification","Heated storage / charging"],
["Topside icing","Stability and access hazard","Icing allowance and removal plan","Weather limits / de-icing"]],sources:"IMO Polar Code chapters on machinery, fire safety, life-saving, navigation and operations."},
{name:"VOYAGE PLANNING",desc:"Polar voyage risk register",title:"VOYAGE PLANNING -- CAPABILITY VERSUS CONDITIONS",intro:"A safe plan stays within the certificate, PWOM and real-time environmental information.",headers:["Planning input","Decision use","Evidence source","Status"],rows:[
["Ice charts and forecasts","Route and timing","Recognized ice service","Open"],
["Observed ice regime","POLARIS / operational decision","Bridge observations and reports","Open"],
["Ship limitations","No-go and conditional operations","Certificate and PWOM","Open"],
["Hydrography","Safe water and chart confidence","Official charts/notices","Open"],
["Weather and visibility","Speed, route, icing and shelter","Forecast provider","Open"],
["SAR and refuge","Contingency and survival time","Regional plans / contacts","Open"],
["Fuel and stores","Delay and diversion endurance","Voyage calculation","Open"],
["Environmental restrictions","Discharge and protected areas","MARPOL / Polar Code / coastal rules","Open"]],sources:"IMO Polar Code and official voyage-planning guidance."},
{name:"PWOM CHECK",desc:"Operational manual evidence checklist",title:"PWOM CHECK -- DECISION SUPPORT",intro:"The manual must make capabilities and limitations usable by the master and crew.",headers:["Content","Evidence","Verification question","Status"],rows:[
["Ship capabilities","Ice, temperature and system limits","Are limits measurable onboard?","Open"],
["Normal operations","Route, speed and machinery guidance","Are triggers and actions explicit?","Open"],
["Risk-based procedures","Ice, icing, low temperature, remoteness","Do procedures reflect assessment hazards?","Open"],
["Incident response","Beset, damage, pollution, loss of systems","Are roles, contacts and resources current?","Open"],
["Coordination","Icebreaker, convoy, reporting, SAR","Are communications practical?","Open"],
["Training and drills","Competence and familiarization","Are records and scenarios adequate?","Open"]],sources:"IMO Polar Code requirement for a Polar Water Operational Manual."},
]},
];

function custom(wb,name,type){
  const s=wb.worksheets.add(name); baseSheet(s);
  if(type==="heel"){
    title(s,"HEELING MOMENTS -- CRANE AND TOWLINE SCREENING","Equilibrium heel estimate uses small-angle GM relation and must not be used near downflooding or large heel.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["Displacement",5000,"t","Loading-condition displacement."],["GM corrected",1.20,"m","Use free-surface-corrected GM."],["Crane hook load",50,"t","Suspended load including rigging."],["Transverse outreach",12,"m","Horizontal transverse lever from centreline."],["Towline force",0,"kN","Use verified maximum force for towing case."],["Tow point height above CG",3,"m","Vertical lever for simplified transverse force case."]]);
    section(s,14,"SCREENING RESULTS"); headers(s,15,["Result","Value","Unit","Formula / limitation"]); calcRows(s,16,[["Crane heeling moment","=C9*C10*9.80665","kN m","Hook load × outreach × g."],["Towline heeling moment","=C11*C12","kN m","Simplified transverse force × vertical lever."],["Total heeling moment","=C16+C17","kN m","Simultaneous load only if physically applicable."],["Small-angle heel","=C18/(C7*9.80665*C8)*180/PI()","deg","Linear GM equilibrium; verify against full GZ and openings."],["Screening status",'=IF(C19<=5,"LOW-ANGLE SCREEN ONLY","FULL GZ / OPERATING LIMIT REVIEW REQUIRED")',"","5° is a workflow trigger, not a statutory limit."]]);
    sourceFooter(s,22,"First-principles moment equilibrium. Acceptance must come from applicable vessel rules and approved stability analysis.");
  } else if(type==="fs"||type==="fs2"){
    title(s,"FREE SURFACE -- VIRTUAL RISE OF G","Free-surface correction is the sum of liquid free-surface moments divided by displacement.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["Displacement",5000,"t","Current loading condition."],["Uncorrected GM",1.20,"m","Before free-surface correction."],["Tank 1 FSM",600,"t m","Use approved tank tables and actual density."],["Tank 2 FSM",350,"t m","Use approved tank tables and actual density."],["Tank 3 FSM",0,"t m","Add further tanks in the approved model."]]);
    section(s,13,"RESULTS"); headers(s,14,["Result","Value","Unit","Formula / limitation"]); calcRows(s,15,[["Total FSM","=SUM(C9:C11)","t m","Sum of active slack-tank moments."],["Virtual rise GGv","=C15/C7","m","FSM / displacement."],["Corrected GM","=C8-C16","m","Uncorrected GM minus virtual rise."],["Screening status",'=IF(C17>0,"POSITIVE GM -- CHECK FULL CRITERIA","NON-POSITIVE GM -- REVISE CONDITION")',"","Positive GM alone is not compliance."]]);
    sourceFooter(s,20,"2008 IS Code free-surface principles; approved tank data and loading computer govern.");
  } else if(type==="gz"){
    title(s,"GZ CURVE -- NUMERICAL INTEGRATION","Enter approved-model GZ ordinates at 5-degree intervals. Areas are trapezoidal and converted from degree to radian.");
    section(s,5,"GZ ORDINATES"); s.getRange("B6:E6").values=[["Heel (deg)","GZ (m)","Incremental area (m rad)","Cumulative area (m rad)"]]; s.getRange("B6:E6").format={fill:C.dark,font:{bold:true,color:C.white},horizontalAlignment:"center"};
    const angles=Array.from({length:15},(_,i)=>i*5); s.getRange("B7:B21").values=angles.map(x=>[x]); s.getRange("C7:C21").values=angles.map(x=>[Math.max(0,0.32*Math.sin(x*Math.PI/60))]);
    s.getRange("B7:B21").format={fill:C.gray,horizontalAlignment:"center"}; s.getRange("C7:C21").format={fill:C.input,font:{color:"#0000FF"},numberFormat:"0.000",horizontalAlignment:"center"};
    s.getRange("D7:E7").values=[[0,0]]; s.getRange("D8").formulas=[["=(B8-B7)*PI()/180*(C8+C7)/2"]]; s.getRange("D8:D21").fillDown(); s.getRange("E8").formulas=[["=E7+D8"]]; s.getRange("E8:E21").fillDown();
    s.getRange("D7:E21").format={fill:C.calc,numberFormat:"0.0000",horizontalAlignment:"center"};
    sourceFooter(s,23,"Trapezoidal integration demonstration. Use the approved curve, downflooding angle and applicable criteria.");
  } else if(type==="is"){
    title(s,"SELECTED 2008 IS CODE GENERAL CRITERIA","These are selected general criteria from Part A 2.2; confirm applicability and limiting angle provisions in the Code.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["Area 0–30°",0.060,"m rad","From approved GZ curve; minimum 0.055."],["Area 0–40° or flooding angle",0.095,"m rad","Minimum 0.090, with limiting angle provisions."],["Area 30–40° or flooding angle",0.035,"m rad","Minimum 0.030, with limiting angle provisions."],["Maximum GZ",0.25,"m","Minimum 0.20 at angle ≥30°."],["Angle of maximum GZ",32,"deg","Should occur at angle not less than 25°."],["Initial GM corrected",0.18,"m","Minimum 0.15 m."]]);
    section(s,14,"CHECKS"); headers(s,15,["Criterion","Result","Required","Note"]); calcRows(s,16,[["Area 0–30°",'=IF(C7>=0.055,"PASS","FAIL")',"≥0.055 m rad","MSC.267(85) Part A 2.2."],["Area 0–40°",'=IF(C8>=0.09,"PASS","FAIL")',"≥0.090 m rad","Apply limiting angle provisions."],["Area 30–40°",'=IF(C9>=0.03,"PASS","FAIL")',"≥0.030 m rad","Apply limiting angle provisions."],["Maximum GZ",'=IF(AND(C10>=0.2,C11>=30),"PASS","FAIL")',"≥0.20 m at ≥30°","Alternative allowance may be considered only as Code states."],["Angle max GZ",'=IF(C11>=25,"PASS","FAIL")',"≥25°","Selected general criterion."],["Initial GM",'=IF(C12>=0.15,"PASS","FAIL")',"≥0.15 m","After free-surface correction."]]);
    sourceFooter(s,23,"IMO MSC.267(85), Part A 2.2. This sheet excludes weather criterion and vessel-specific criteria.");
  } else if(type==="kg"){
    title(s,"LIMITING KG -- GM-BASED SCREENING","A GM-derived KG limit is only one constraint; the approved limiting KG is the minimum across all applicable criteria.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["KM at condition",7.20,"m","From approved hydrostatics at displacement/trim."],["Required corrected GM",0.15,"m","Applicable minimum; project input."],["Free-surface correction",0.18,"m","Total FSM / displacement."],["Actual KG",6.70,"m","Current loading condition."]]);
    section(s,12,"RESULTS"); headers(s,13,["Result","Value","Unit","Meaning"]); calcRows(s,14,[["GM-based limiting KG","=C7-C8-C9","m","KM − required GM − FSC."],["KG margin","=C14-C10","m","Positive is below this screening limit."],["Status",'=IF(C15>=0,"PASS THIS SCREEN","FAIL THIS SCREEN")',"","Full limiting-KG curve may be lower."]]); sourceFooter(s,18,"GM identity. Full limiting KG must include GZ area, weather and all applicable special criteria.");
  } else if(type==="damage"){
    title(s,"DAMAGE INDEX -- A/R SCREENING","Probabilistic compliance is A ≥ R. Enter approved subdivision-calculation contributions; this sheet only sums them.");
    section(s,5,"GLOBAL INPUT"); headers(s,6); inputRows(s,7,[["Required index R",0.650,"-","Obtain from applicable SOLAS calculation."],["Number of scenario rows",6,"-","Demonstration table below."]]);
    s.getRange("B11:E11").values=[["Damage scenario","Probability p","Survival s","Contribution p×s"]]; s.getRange("B11:E11").format={fill:C.dark,font:{bold:true,color:C.white},horizontalAlignment:"center"};
    const rows=[["1",0.12,0.9],["2",0.10,0.8],["3",0.15,0.7],["4",0.08,0.6],["5",0.18,0.9],["6",0.20,0.8]];
    s.getRange("B12:D17").values=rows; s.getRange("C12:D17").format={fill:C.input,font:{color:"#0000FF"},numberFormat:"0.000"}; s.getRange("E12").formulas=[["=C12*D12"]]; s.getRange("E12:E17").fillDown(); s.getRange("E12:E17").format={fill:C.calc,numberFormat:"0.000"};
    s.getRange("B19:E21").values=[["Attained index A",null,"-","Sum of p×s"],["A/R ratio",null,"-","Screening ratio"],["Status",null,"","A ≥ R only; subdivision model approval remains required."]]; s.getRange("C19").formulas=[["=SUM(E12:E17)"]]; s.getRange("C20").formulas=[["=C19/C7"]]; s.getRange("C21").formulas=[['=IF(C19>=C7,"A >= R","A < R")']]; s.getRange("B19:E21").format={fill:C.calc,wrapText:true}; sourceFooter(s,23,"SOLAS II-1 probabilistic framework. Scenario probabilities and survival factors must come from the approved calculation.");
  } else if(type==="imo"){
    title(s,"IMO TRIAL CHECK -- MSC.137(76)","Inputs are full-scale trial results normalized by ship length L. Zig-zag limits depend on L/V in seconds.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["Length L",150,"m","Use the length definition in MSC.137(76)."],["Test speed V",7.5,"m/s","Speed at test condition."],["Advance",620,"m","Turning-circle advance."],["Tactical diameter",700,"m","Turning-circle tactical diameter."],["Track reach",1800,"m","Full astern stopping track reach."],["10/10 first overshoot",12,"deg","Maximum first overshoot."],["10/10 second overshoot",28,"deg","Maximum second overshoot."]]);
    section(s,15,"CRITERIA"); headers(s,16,["Measure","Result","Limit","MSC.137(76) criterion"]); calcRows(s,17,[["L/V","=C7/C8","s","Determines zig-zag limits."],["Advance / L","=C9/C7","L","≤4.5 L."],["Tactical diameter / L","=C10/C7","L","≤5.0 L."],["Track reach / L","=C11/C7","L","≤15 L; Administration may modify for large displacement ships."],["First overshoot limit",'=IF(C17<10,10,IF(C17<=30,5+0.5*C17,20))',"deg","Piecewise criterion."],["Second overshoot limit",'=IF(C17<10,25,IF(C17<=30,17.5+0.75*C17,40))',"deg","Piecewise criterion."],["Overall status",'=IF(AND(C18<=4.5,C19<=5,C20<=15,C12<=C21,C13<=C22),"PASS INPUT CRITERIA","REVIEW / FAIL")',"","Also assess initial turning and applicability."]]);
    sourceFooter(s,25,"IMO MSC.137(76). Confirm full text, trial conditions, scope and any Administration decisions.");
  } else if(type==="turn"){
    title(s,"TURNING ANALYSIS -- NORMALIZED GEOMETRY","Port/starboard asymmetry can expose wind, current, loading or control bias.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["Length L",150,"m","MSC.137(76) length basis."],["Port advance",620,"m","Measured trial result."],["Starboard advance",600,"m","Measured trial result."],["Port tactical diameter",700,"m","Measured trial result."],["Starboard tactical diameter",680,"m","Measured trial result."]]);
    section(s,13,"RESULTS"); headers(s,14,["Result","Value","Unit","Use"]); calcRows(s,15,[["Mean advance / L","=AVERAGE(C8:C9)/C7","L","Compare to criterion where applicable."],["Mean tactical diameter / L","=AVERAGE(C10:C11)/C7","L","Compare to criterion where applicable."],["Advance asymmetry","=ABS(C8-C9)/AVERAGE(C8:C9)","fraction","Investigate environmental/control bias."],["Diameter asymmetry","=ABS(C10-C11)/AVERAGE(C10:C11)","fraction","Investigate environmental/control bias."]]); s.getRange("C17:C18").format.numberFormat="0.0%"; sourceFooter(s,20,"IMO MSC.137(76) and ITTC 7.5-04-02-01.");
  } else if(type==="stop"){
    title(s,"STOPPING ANALYSIS -- TRACK REACH AND TIME","Stopping performance depends on initial speed, propulsion response, loading and environment.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["Length L",150,"m","Applicable length basis."],["Initial speed",7.5,"m/s","At full astern order."],["Track reach",1800,"m","Distance along initial course direction."],["Head reach",1700,"m","Project-defined geometric result."],["Stopping time",240,"s","Until vessel stopped by defined criterion."]]);
    section(s,13,"RESULTS"); headers(s,14,["Result","Value","Unit","Use"]); calcRows(s,15,[["Track reach / L","=C9/C7","L","MSC.137(76): ≤15 L, subject to stated provision."],["Mean deceleration","=C8/C11","m/s²","Simple average, not peak load."],["Equivalent constant-decel distance","=C8*C11/2","m","Diagnostic comparison only."],["Track criterion",'=IF(C15<=15,"PASS","REVIEW / FAIL")',"","Confirm applicability and Administration provision."]]); sourceFooter(s,20,"IMO MSC.137(76); ITTC trial procedure.");
  } else if(type==="prod"){
    title(s,"WORK PACKAGES -- EARNED HOURS PROGRESS","Earned hours = budget hours × physical progress. Variance compares earned with actual hours.");
    section(s,5,"WORK PACKAGE TRACKER"); s.getRange("B6:H6").values=[["Package","Discipline","Budget h","Physical %","Earned h","Actual h","Efficiency (earned/actual)"]]; s.getRange("B6:H6").format={fill:C.dark,font:{bold:true,color:C.white},horizontalAlignment:"center",wrapText:true};
    const r=[["B101","Structure",1200,0.75,null,980],["B102","Outfit",900,0.50,null,520],["Z201","Piping",700,0.40,null,360],["Z202","Electrical",500,0.30,null,210],["SYS-A","Testing",300,0.20,null,85]];
    s.getRange("B7:G11").values=r; s.getRange("D7:D11").format={fill:C.input,font:{color:"#0000FF"},numberFormat:"0"}; s.getRange("E7:E11").format={fill:C.input,font:{color:"#0000FF"},numberFormat:"0%"}; s.getRange("G7:G11").format={fill:C.input,font:{color:"#0000FF"}};
    s.getRange("F7").formulas=[["=D7*E7"]]; s.getRange("F7:F11").fillDown(); s.getRange("H7").formulas=[["=IF(G7=0,0,F7/G7)"]]; s.getRange("H7:H11").fillDown(); s.getRange("F7:F11").format.fill=C.calc; s.getRange("H7:H11").format={fill:C.calc,numberFormat:"0.00"};
    section(s,14,"TOTALS"); s.getRange("B15:E18").values=[["Budget hours",null,"h",""],["Earned hours",null,"h",""],["Actual hours",null,"h",""],["Overall efficiency",null,"","Earned / actual"]]; s.getRange("C15").formulas=[["=SUM(D7:D11)"]]; s.getRange("C16").formulas=[["=SUM(F7:F11)"]]; s.getRange("C17").formulas=[["=SUM(G7:G11)"]]; s.getRange("C18").formulas=[["=IF(C17=0,0,C16/C17)"]]; s.getRange("B15:E18").format={fill:C.calc}; s.getRange("C15:C17").format.numberFormat="0"; s.getRange("C18").format.numberFormat="0.00"; sourceFooter(s,20,"Project-control arithmetic. Physical-progress rules and budgets must be approved project inputs.");
  } else if(type==="completion"){
    title(s,"COMPLETION -- PUNCH AND TEST STATUS","Completion should be system-based, with safety-critical items controlled separately.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["Total systems",40,"count","Defined handover systems."],["Mechanically complete",26,"count","Signed mechanical-completion certificates."],["Test packs total",120,"count","Approved test boundaries."],["Test packs accepted",72,"count","Accepted and closed."],["Category A punches",8,"count","Project-defined safety/function-critical category."],["Other open punches",140,"count","Remaining agreed categories."]]);
    section(s,14,"STATUS"); headers(s,15,["KPI","Value","Unit","Interpretation"]); calcRows(s,16,[["Mechanical completion","=C8/C7","fraction","Signed systems / total systems."],["Test completion","=C10/C9","fraction","Accepted test packs / total."],["A-punch status",'=IF(C11=0,"CLEAR","BLOCKED")',"","Definition must follow contract completion procedure."],["Readiness screen",'=IF(AND(C16=1,C17=1,C11=0),"READY FOR DEFINED GATE","NOT READY")',"","Does not override formal handover authority."]]); s.getRange("C16:C17").format.numberFormat="0%"; sourceFooter(s,21,"Project completion procedure and signed evidence govern.");
  } else if(type==="noise"){
    title(s,"URN SPECTRUM -- DISTANCE NORMALIZATION SCREEN","Free-field normalization shown: L(1 m) = L(r) + 20 log10(r/1 m). Use ISO procedure for compliant reporting.");
    section(s,5,"INPUTS"); headers(s,6); inputRows(s,7,[["Measurement range r",100,"m","Slant range used for this simple screen."],["Reference range",1,"m","Normalization reference."],["Background margin requirement",3,"dB","Project input; ISO method governs treatment."]]);
    s.getRange("B12:F12").values=[["Band centre (Hz)","Measured SPL (dB re 1 μPa)","Normalized level (dB)","Linear energy","Background SPL (dB)"]]; s.getRange("B12:F12").format={fill:C.dark,font:{bold:true,color:C.white},horizontalAlignment:"center",wrapText:true};
    const bands=[31.5,63,125,250,500,1000,2000,4000]; const spl=[128,132,136,140,139,135,130,126]; const bg=[105,108,110,112,113,112,110,108];
    s.getRange("B13:B20").values=bands.map(x=>[x]); s.getRange("C13:C20").values=spl.map(x=>[x]); s.getRange("F13:F20").values=bg.map(x=>[x]); s.getRange("C13:C20").format={fill:C.input,font:{color:"#0000FF"}}; s.getRange("F13:F20").format={fill:C.input,font:{color:"#0000FF"}};
    s.getRange("D13:D20").formulas=Array.from({length:8},(_,i)=>[`=C${13+i}+20*LOG10($C$7/$C$8)`]);
    s.getRange("E13:E20").formulas=Array.from({length:8},(_,i)=>[`=10^(D${13+i}/10)`]);
    s.getRange("D13:E20").format.fill=C.calc;
    s.getRange("B22:E24").values=[["Broadband normalized level",null,"dB","Energetic sum of displayed normalized bands."],["Minimum signal-background margin",null,"dB","Minimum displayed band margin."],["Screen status",null,"","Background criterion is a project screen only."]]; s.getRange("C22").formulas=[["=10*LOG10(SUM(E13:E20))"]]; s.getRange("C23").formulas=[["=MIN(C13-F13,C14-F14,C15-F15,C16-F16,C17-F17,C18-F18,C19-F19,C20-F20)"]]; s.getRange("C24").formulas=[['=IF(C23>=C9,"MARGIN SCREEN PASS","BACKGROUND REVIEW")']]; s.getRange("B22:E24").format={fill:C.calc,wrapText:true}; sourceFooter(s,26,"ISO 17208-1:2016 + Amd 1:2024 and IMO MEPC.1/Circ.906. This simple distance law is not the complete ISO processing chain.");
  } else if(type==="rio"){
    title(s,"POLARIS RIO -- ARITHMETIC WITH OFFICIAL INPUTS","RIO = sum(C_i × RIV_i). Enter ice concentrations in tenths and RIVs from the current official POLARIS table for the ship’s assigned ice class.");
    section(s,5,"ICE REGIME"); s.getRange("B6:E6").values=[["Ice type / stage","Concentration (tenths)","RIV (official input)","Contribution C×RIV"]]; s.getRange("B6:E6").format={fill:C.dark,font:{bold:true,color:C.white},horizontalAlignment:"center",wrapText:true};
    const types=["Open water / ice free","New ice","Thin first-year ice","Medium first-year ice","Thick first-year ice","Old ice"]; s.getRange("B7:B12").values=types.map(x=>[x]); s.getRange("C7:C12").values=[[2],[2],[3],[2],[1],[0]]; s.getRange("D7:D12").values=[[null],[null],[null],[null],[null],[null]];
    s.getRange("C7:D12").format={fill:C.input,font:{color:"#0000FF"},horizontalAlignment:"center"}; s.getRange("E7").formulas=[["=C7*D7"]]; s.getRange("E7:E12").fillDown(); s.getRange("E7:E12").format.fill=C.calc;
    s.getRange("B14:E17").values=[["Concentration sum",null,"tenths","Must equal 10 for a complete regime."],["RIO",null,"","Sum of contributions."],["Input completeness",null,"","Concentrations and RIVs must be verified."],["Operational decision",null,"","Use current POLARIS decision criteria and PWOM."]]; s.getRange("C14").formulas=[["=SUM(C7:C12)"]]; s.getRange("C15").formulas=[["=SUM(E7:E12)"]]; s.getRange("C16").formulas=[['=IF(AND(C14=10,COUNT(D7:D12)=6),"INPUTS COMPLETE","ENTER / CHECK INPUTS")']]; s.getRange("C17").values=[["REFER TO CURRENT POLARIS + PWOM"]]; s.getRange("B14:E17").format={fill:C.calc,wrapText:true};
    sourceFooter(s,19,"POLARIS arithmetic only. No RIV table or go/no-go threshold is invented; use the current official guidance for assigned ice class.");
  }
  s.freezePanes.freezeRows(6);
}

for (const spec of specs) {
  const wb=Workbook.create(); cover(wb,spec);
  for(const sh of spec.sheets) sh.custom?custom(wb,sh.name,sh.custom):theory(wb,sh);
  const refKey=spec.vol<=30?"stability":spec.vol===31?"maneuver":spec.vol===32?"production":spec.vol===33?"noise":"polar";
  refs(wb,commonRefs[refKey]);
  const errors=await wb.inspect({kind:"match",searchTerm:"#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",options:{useRegex:true,maxResults:200},summary:`Vol${spec.vol} formula error scan`});
  console.log(`VOL ${spec.vol} ERRORS\n${errors.ndjson}`);
  for(const sh of ["COVER",...spec.sheets.map(x=>x.name),"REFERENCE TABLES"]){
    const img=await wb.render({sheetName:sh,autoCrop:"all",scale:0.8,format:"png"});
    await fs.writeFile(path.join(previewDir,`Vol${spec.vol}_${sh.replace(/[<>:"/\\|?*]/g,"_")}.png`),new Uint8Array(await img.arrayBuffer()));
  }
  const file=path.join(outDir,`Naval_Architecture_Teaching_Toolkit_Vol${spec.vol}.xlsx`);
  await (await SpreadsheetFile.exportXlsx(wb)).save(file);
  console.log(`SAVED ${file}`);
}
