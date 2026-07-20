using System.Collections.Generic;
using NavalArchitectureSuite.Models;

namespace NavalArchitectureSuite.Services
{
    /// <summary>
    /// Teaching-level marine diesel engine reference database — 50 engines.
    ///
    /// Data is drawn from publicly available manufacturer project guides
    /// (MAN B&W, WinGD, Wärtsilä, MAN Energy Solutions, Rolls-Royce Bergen,
    /// Hyundai HiMSEN, Daihatsu, ABC, Caterpillar, MTU, Cummins).
    /// SFOC figures are indicative ISO reference values at the engine flange —
    /// actual shop-test results and guarantee values differ per hull, installation,
    /// ambient conditions and fuel specification. Always verify against the current
    /// manufacturer project guide before use in a real design submission.
    ///
    /// Organised into six teaching categories:
    ///   1. Low-speed 2-stroke HFO      — large merchant ships
    ///   2. Low-speed 2-stroke Dual-Fuel — gas carriers, LNG-fuelled vessels
    ///   3. Medium-speed 4-stroke HFO   — ferries, smaller cargo, offshore
    ///   4. Medium-speed 4-stroke DF    — LNG/dual-fuel ferries and tankers
    ///   5. High-speed diesel           — tugs, workboats, patrol craft
    ///   6. Genset / Auxiliary          — auxiliary power reference
    /// </summary>
    public static class MarineEngineDatabase
    {
        public static readonly IReadOnlyList<MarineEngine> Engines = new List<MarineEngine>
        {
            // ════════════════════════════════════════════════════════════════════
            // 1. LOW-SPEED 2-STROKE — HFO / DIESEL
            // ════════════════════════════════════════════════════════════════════

            new() {
                Manufacturer="MAN B&W", Model="5S40ME-B9.5",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=5750, RatedRpm=146,
                SfocAt100=172, SfocMin=163, LoadAtMinSfoc=75,
                Cylinders=5, Stroke="2-stroke",
                Notes="Compact 5-cyl S40; handy size for small bulk carriers, coasters and product tankers."
            },
            new() {
                Manufacturer="MAN B&W", Model="6S40ME-B9.5",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=6900, RatedRpm=146,
                SfocAt100=172, SfocMin=163, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="6-cyl S40; common in short-sea and coastal cargo vessels."
            },
            new() {
                Manufacturer="MAN B&W", Model="5S50ME-B9.3",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=7700, RatedRpm=112,
                SfocAt100=171, SfocMin=163, LoadAtMinSfoc=75,
                Cylinders=5, Stroke="2-stroke",
                Notes="Compact low-speed 5-cyl; smaller bulk carriers and tankers."
            },
            new() {
                Manufacturer="MAN B&W", Model="6S50ME-B9.3",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=9240, RatedRpm=112,
                SfocAt100=171, SfocMin=163, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="Workhorse 6-cyl S50; Handysize bulk carriers and chemical tankers."
            },
            new() {
                Manufacturer="MAN B&W", Model="6S60ME-C10.5",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=13560, RatedRpm=105,
                SfocAt100=169, SfocMin=160, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="High-efficiency Mk10.5; Handymax and Panamax bulk carriers."
            },
            new() {
                Manufacturer="MAN B&W", Model="7S60ME-C10.5",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=15820, RatedRpm=105,
                SfocAt100=169, SfocMin=160, LoadAtMinSfoc=75,
                Cylinders=7, Stroke="2-stroke",
                Notes="7-cyl S60; larger Panamax bulkers and medium tankers."
            },
            new() {
                Manufacturer="MAN B&W", Model="6S65ME-C8.6",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=15960, RatedRpm=97,
                SfocAt100=170, SfocMin=161, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="S65 series; Supramax / Ultramax bulk carriers and MR tankers."
            },
            new() {
                Manufacturer="MAN B&W", Model="7S65ME-C8.6",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=18620, RatedRpm=97,
                SfocAt100=170, SfocMin=161, LoadAtMinSfoc=75,
                Cylinders=7, Stroke="2-stroke",
                Notes="Popular in Aframax tankers and Kamsarmax bulk carriers."
            },
            new() {
                Manufacturer="MAN B&W", Model="6S70ME-C10.5",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=19620, RatedRpm=91,
                SfocAt100=168, SfocMin=159, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="S70 Mk10.5; large Panamax / post-Panamax containerships and Suezmax tankers."
            },
            new() {
                Manufacturer="MAN B&W", Model="7S80ME-C10.5",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=26600, RatedRpm=78,
                SfocAt100=167, SfocMin=158, LoadAtMinSfoc=75,
                Cylinders=7, Stroke="2-stroke",
                Notes="Large-bore Mk10.5; VLCC tankers and large post-Panamax containerships."
            },
            new() {
                Manufacturer="MAN B&W", Model="8G80ME-C10.5",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=37280, RatedRpm=72,
                SfocAt100=166, SfocMin=157, LoadAtMinSfoc=75,
                Cylinders=8, Stroke="2-stroke",
                Notes="G-type ultra-long stroke 80-bore; ULCVs and VLCC twin-engine installations."
            },
            new() {
                Manufacturer="MAN B&W", Model="11G95ME-C10.5",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=58520, RatedRpm=72,
                SfocAt100=164, SfocMin=155, LoadAtMinSfoc=75,
                Cylinders=11, Stroke="2-stroke",
                Notes="Largest bore MAN; 24,000+ TEU mega-containerships."
            },
            new() {
                Manufacturer="WinGD", Model="6X35DF",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=4860, RatedRpm=167,
                SfocAt100=173, SfocMin=165, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="Compact WinGD X35; small feeder containerships and coastal cargo."
            },
            new() {
                Manufacturer="WinGD", Model="6X52",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=9060, RatedRpm=112,
                SfocAt100=172, SfocMin=164, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="WinGD X52 diesel-only; Handymax and Supramax vessels."
            },
            new() {
                Manufacturer="WinGD", Model="7X72",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=21700, RatedRpm=84,
                SfocAt100=168, SfocMin=159, LoadAtMinSfoc=75,
                Cylinders=7, Stroke="2-stroke",
                Notes="WinGD X72; large bulk carriers and product tankers."
            },
            new() {
                Manufacturer="WinGD", Model="6X92",
                Category="Low-speed 2-stroke", FuelType="HFO",
                McrKw=33720, RatedRpm=68,
                SfocAt100=165, SfocMin=156, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="WinGD largest bore; VLCCs and very large containerships."
            },

            // ════════════════════════════════════════════════════════════════════
            // 2. LOW-SPEED 2-STROKE — DUAL FUEL / LNG
            // ════════════════════════════════════════════════════════════════════

            new() {
                Manufacturer="WinGD", Model="6X52DF",
                Category="Low-speed 2-stroke Dual-Fuel", FuelType="HFO/LNG",
                McrKw=9060, RatedRpm=112,
                SfocAt100=172, SfocMin=164, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="Otto-cycle dual-fuel; gas mode SFOC ≈138 g/kWh LNG equiv. Pilot fuel ignition."
            },
            new() {
                Manufacturer="WinGD", Model="7X62DF",
                Category="Low-speed 2-stroke Dual-Fuel", FuelType="HFO/LNG",
                McrKw=13020, RatedRpm=105,
                SfocAt100=171, SfocMin=163, LoadAtMinSfoc=75,
                Cylinders=7, Stroke="2-stroke",
                Notes="Widely fitted in LNG carriers and dual-fuel bulkers."
            },
            new() {
                Manufacturer="WinGD", Model="7X82DF",
                Category="Low-speed 2-stroke Dual-Fuel", FuelType="HFO/LNG",
                McrKw=20790, RatedRpm=84,
                SfocAt100=168, SfocMin=159, LoadAtMinSfoc=75,
                Cylinders=7, Stroke="2-stroke",
                Notes="High-power DF 2-stroke; large tankers and dual-fuel bulk carriers."
            },
            new() {
                Manufacturer="MAN B&W", Model="6S60ME-GI",
                Category="Low-speed 2-stroke Dual-Fuel", FuelType="HFO/LNG",
                McrKw=13560, RatedRpm=105,
                SfocAt100=169, SfocMin=160, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="2-stroke",
                Notes="Gas Injection (GI) Diesel-cycle DF; near-diesel efficiency on gas fuel."
            },
            new() {
                Manufacturer="MAN B&W", Model="7S80ME-GI",
                Category="Low-speed 2-stroke Dual-Fuel", FuelType="HFO/LNG",
                McrKw=26600, RatedRpm=78,
                SfocAt100=167, SfocMin=158, LoadAtMinSfoc=75,
                Cylinders=7, Stroke="2-stroke",
                Notes="Large-bore GI variant; VLCC and ULCV dual-fuel retrofit/newbuild."
            },

            // ════════════════════════════════════════════════════════════════════
            // 3. MEDIUM-SPEED 4-STROKE — HFO / MDO
            // ════════════════════════════════════════════════════════════════════

            new() {
                Manufacturer="Wärtsilä", Model="6L20",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=1080, RatedRpm=1000,
                SfocAt100=198, SfocMin=188, LoadAtMinSfoc=85,
                Cylinders=6, Stroke="4-stroke",
                Notes="Compact W20; fishing vessels, small ferries, workboats."
            },
            new() {
                Manufacturer="Wärtsilä", Model="8L26",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=1820, RatedRpm=900,
                SfocAt100=193, SfocMin=184, LoadAtMinSfoc=85,
                Cylinders=8, Stroke="4-stroke",
                Notes="W26 series; small RoPax, OSVs and general cargo ships."
            },
            new() {
                Manufacturer="Wärtsilä", Model="6L32",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=2880, RatedRpm=750,
                SfocAt100=188, SfocMin=179, LoadAtMinSfoc=85,
                Cylinders=6, Stroke="4-stroke",
                Notes="Reliable W32 workhorse; also used as genset prime mover."
            },
            new() {
                Manufacturer="Wärtsilä", Model="9L32",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=4320, RatedRpm=750,
                SfocAt100=187, SfocMin=178, LoadAtMinSfoc=85,
                Cylinders=9, Stroke="4-stroke",
                Notes="9-cyl W32; medium RoPax, offshore supply vessels."
            },
            new() {
                Manufacturer="Wärtsilä", Model="6L46F",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=5850, RatedRpm=600,
                SfocAt100=182, SfocMin=174, LoadAtMinSfoc=85,
                Cylinders=6, Stroke="4-stroke",
                Notes="High-efficiency W46F; ferries, RoPax and offshore support vessels."
            },
            new() {
                Manufacturer="Wärtsilä", Model="8L46F",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=7800, RatedRpm=600,
                SfocAt100=182, SfocMin=173, LoadAtMinSfoc=85,
                Cylinders=8, Stroke="4-stroke",
                Notes="8-cyl W46F; larger ferries and offshore vessels."
            },
            new() {
                Manufacturer="MAN", Model="6L21/31",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=900, RatedRpm=1000,
                SfocAt100=202, SfocMin=192, LoadAtMinSfoc=80,
                Cylinders=6, Stroke="4-stroke",
                Notes="Compact MAN 21/31; small coasters, tugs and river vessels."
            },
            new() {
                Manufacturer="MAN", Model="6L32/40",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=2880, RatedRpm=750,
                SfocAt100=190, SfocMin=181, LoadAtMinSfoc=85,
                Cylinders=6, Stroke="4-stroke",
                Notes="MAN 32/40 series; ferries, smaller cargo ships and OSVs."
            },
            new() {
                Manufacturer="MAN", Model="9L32/40",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=4320, RatedRpm=750,
                SfocAt100=189, SfocMin=180, LoadAtMinSfoc=85,
                Cylinders=9, Stroke="4-stroke",
                Notes="9-cyl 32/40; also widely used as genset prime mover."
            },
            new() {
                Manufacturer="MAN", Model="6L35/44DF",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=3900, RatedRpm=750,
                SfocAt100=189, SfocMin=180, LoadAtMinSfoc=85,
                Cylinders=6, Stroke="4-stroke",
                Notes="MAN 35/44 series; medium-sized cargo and passenger vessels."
            },
            new() {
                Manufacturer="Rolls-Royce Bergen", Model="B32:40V12AG",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=4320, RatedRpm=750,
                SfocAt100=191, SfocMin=182, LoadAtMinSfoc=85,
                Cylinders=12, Stroke="4-stroke",
                Notes="Bergen V12 gas/diesel; offshore platforms, PSVs and AHTSVs."
            },
            new() {
                Manufacturer="Rolls-Royce Bergen", Model="C26:33L9AG",
                Category="Medium-speed 4-stroke", FuelType="MDO",
                McrKw=2430, RatedRpm=900,
                SfocAt100=197, SfocMin=187, LoadAtMinSfoc=82,
                Cylinders=9, Stroke="4-stroke",
                Notes="Bergen C-series inline; OSVs, patrol vessels, small ferries."
            },
            new() {
                Manufacturer="Hyundai HiMSEN", Model="6H21/32",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=1260, RatedRpm=900,
                SfocAt100=199, SfocMin=189, LoadAtMinSfoc=83,
                Cylinders=6, Stroke="4-stroke",
                Notes="HiMSEN H21/32; popular in Asian-built bulk carriers as genset."
            },
            new() {
                Manufacturer="Hyundai HiMSEN", Model="9H25/33",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=2925, RatedRpm=800,
                SfocAt100=194, SfocMin=185, LoadAtMinSfoc=84,
                Cylinders=9, Stroke="4-stroke",
                Notes="HiMSEN H25/33; medium cargo ships and offshore support vessels."
            },
            new() {
                Manufacturer="Daihatsu", Model="6DKM-26",
                Category="Medium-speed 4-stroke", FuelType="HFO/MDO",
                McrKw=1620, RatedRpm=900,
                SfocAt100=196, SfocMin=186, LoadAtMinSfoc=83,
                Cylinders=6, Stroke="4-stroke",
                Notes="Daihatsu DKM series; very common genset on Japanese-built vessels."
            },
            new() {
                Manufacturer="ABC", Model="8DZC",
                Category="Medium-speed 4-stroke", FuelType="MDO",
                McrKw=2000, RatedRpm=750,
                SfocAt100=200, SfocMin=190, LoadAtMinSfoc=82,
                Cylinders=8, Stroke="4-stroke",
                Notes="Anglo Belgian Corporation DZC; European short-sea and inland vessels."
            },

            // ════════════════════════════════════════════════════════════════════
            // 4. MEDIUM-SPEED 4-STROKE — DUAL FUEL / LNG
            // ════════════════════════════════════════════════════════════════════

            new() {
                Manufacturer="Wärtsilä", Model="9L34DF",
                Category="Medium-speed 4-stroke Dual-Fuel", FuelType="HFO/MDO/LNG",
                McrKw=4230, RatedRpm=750,
                SfocAt100=185, SfocMin=176, LoadAtMinSfoc=85,
                Cylinders=9, Stroke="4-stroke",
                Notes="Otto-cycle DF; gas mode SFOC ≈138 g/kWh LNG equiv. Popular in LNG-fuelled ferries."
            },
            new() {
                Manufacturer="Wärtsilä", Model="8L50DF",
                Category="Medium-speed 4-stroke Dual-Fuel", FuelType="HFO/MDO/LNG",
                McrKw=7200, RatedRpm=500,
                SfocAt100=183, SfocMin=174, LoadAtMinSfoc=85,
                Cylinders=8, Stroke="4-stroke",
                Notes="Widely used in LNG carriers (DFDE propulsion concept) and cruise ships."
            },
            new() {
                Manufacturer="Wärtsilä", Model="12V50DF",
                Category="Medium-speed 4-stroke Dual-Fuel", FuelType="HFO/MDO/LNG",
                McrKw=10800, RatedRpm=500,
                SfocAt100=182, SfocMin=173, LoadAtMinSfoc=85,
                Cylinders=12, Stroke="4-stroke",
                Notes="V-type DF; cruise ships and large LNG-fuelled RoPax vessels."
            },
            new() {
                Manufacturer="Wärtsilä", Model="6L46DF",
                Category="Medium-speed 4-stroke Dual-Fuel", FuelType="HFO/MDO/LNG",
                McrKw=5700, RatedRpm=600,
                SfocAt100=183, SfocMin=174, LoadAtMinSfoc=85,
                Cylinders=6, Stroke="4-stroke",
                Notes="W46DF; LNG-fuelled ferries and offshore vessels."
            },
            new() {
                Manufacturer="MAN", Model="6L51/60DF",
                Category="Medium-speed 4-stroke Dual-Fuel", FuelType="HFO/MDO/LNG",
                McrKw=7500, RatedRpm=500,
                SfocAt100=181, SfocMin=172, LoadAtMinSfoc=85,
                Cylinders=6, Stroke="4-stroke",
                Notes="MAN 51/60DF; cruise ships, large ferries and combined cycle plants."
            },
            new() {
                Manufacturer="Rolls-Royce Bergen", Model="B35:40V20NG",
                Category="Medium-speed 4-stroke Dual-Fuel", FuelType="MDO/LNG",
                McrKw=8000, RatedRpm=750,
                SfocAt100=182, SfocMin=173, LoadAtMinSfoc=85,
                Cylinders=20, Stroke="4-stroke",
                Notes="Bergen V20 gas; offshore platforms and LNG-fuelled PSVs."
            },

            // ════════════════════════════════════════════════════════════════════
            // 5. HIGH-SPEED DIESEL
            // ════════════════════════════════════════════════════════════════════

            new() {
                Manufacturer="MTU", Model="12V2000 M72",
                Category="High-speed diesel", FuelType="MDO",
                McrKw=1050, RatedRpm=2100,
                SfocAt100=215, SfocMin=203, LoadAtMinSfoc=80,
                Cylinders=12, Stroke="4-stroke",
                Notes="MTU 2000 series; fast patrol boats, small ferries and pilot vessels."
            },
            new() {
                Manufacturer="MTU", Model="16V4000 M73L",
                Category="High-speed diesel", FuelType="MDO",
                McrKw=1940, RatedRpm=1800,
                SfocAt100=210, SfocMin=198, LoadAtMinSfoc=80,
                Cylinders=16, Stroke="4-stroke",
                Notes="IMO Tier III compliant (SCR); tugs, patrol craft, fast ferries."
            },
            new() {
                Manufacturer="MTU", Model="20V4000 M93L",
                Category="High-speed diesel", FuelType="MDO",
                McrKw=3300, RatedRpm=1800,
                SfocAt100=208, SfocMin=196, LoadAtMinSfoc=80,
                Cylinders=20, Stroke="4-stroke",
                Notes="High-output V20; fast ferries, megayachts and naval craft."
            },
            new() {
                Manufacturer="Caterpillar", Model="3512C",
                Category="High-speed diesel", FuelType="MDO",
                McrKw=1490, RatedRpm=1800,
                SfocAt100=217, SfocMin=205, LoadAtMinSfoc=80,
                Cylinders=12, Stroke="4-stroke",
                Notes="Cat V12; workboats, fishing vessels and smaller OSVs."
            },
            new() {
                Manufacturer="Caterpillar", Model="3516C",
                Category="High-speed diesel", FuelType="MDO",
                McrKw=2460, RatedRpm=1600,
                SfocAt100=214, SfocMin=202, LoadAtMinSfoc=80,
                Cylinders=16, Stroke="4-stroke",
                Notes="Cat V16; offshore supply vessels and large workboats."
            },
            new() {
                Manufacturer="Cummins", Model="QSK38-M",
                Category="High-speed diesel", FuelType="MDO",
                McrKw=895, RatedRpm=1800,
                SfocAt100=220, SfocMin=208, LoadAtMinSfoc=80,
                Cylinders=12, Stroke="4-stroke",
                Notes="Cummins QSK38; tugs, fishing vessels and small workboats."
            },
            new() {
                Manufacturer="Cummins", Model="QSK60-M",
                Category="High-speed diesel", FuelType="MDO",
                McrKw=1864, RatedRpm=1900,
                SfocAt100=218, SfocMin=205, LoadAtMinSfoc=80,
                Cylinders=16, Stroke="4-stroke",
                Notes="IMO Tier II; fishing vessels, tugs and fast workboats."
            },

            // ════════════════════════════════════════════════════════════════════
            // 6. GENSET / AUXILIARY REFERENCE
            // ════════════════════════════════════════════════════════════════════

            new() {
                Manufacturer="Wärtsilä", Model="6L20 (genset)",
                Category="Genset / Auxiliary", FuelType="HFO/MDO",
                McrKw=1080, RatedRpm=1000,
                SfocAt100=198, SfocMin=188, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="4-stroke",
                Notes="Very common auxiliary genset on medium and large ships."
            },
            new() {
                Manufacturer="MAN", Model="6L21/31 (genset)",
                Category="Genset / Auxiliary", FuelType="HFO/MDO",
                McrKw=900, RatedRpm=1000,
                SfocAt100=200, SfocMin=190, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="4-stroke",
                Notes="Compact MAN auxiliary; bulk carriers and tankers."
            },
            new() {
                Manufacturer="Hyundai HiMSEN", Model="6H17/28 (genset)",
                Category="Genset / Auxiliary", FuelType="HFO/MDO",
                McrKw=690, RatedRpm=900,
                SfocAt100=203, SfocMin=193, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="4-stroke",
                Notes="HiMSEN H17/28; very common auxiliary on Korean-built vessels."
            },
            new() {
                Manufacturer="Daihatsu", Model="6DKM-20 (genset)",
                Category="Genset / Auxiliary", FuelType="MDO",
                McrKw=720, RatedRpm=900,
                SfocAt100=204, SfocMin=194, LoadAtMinSfoc=75,
                Cylinders=6, Stroke="4-stroke",
                Notes="Daihatsu DKM-20; standard Japanese auxiliary genset."
            },
        };
    }
}
