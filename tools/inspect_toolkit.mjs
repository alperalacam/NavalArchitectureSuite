import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const sourceDir = "C:\\Temp\\Yacht Designs\\Naval architecture design toolkit";
const outDir = "C:\\Temp\\NavalArchitectureSuite\\outputs\\toolkit_inspection";
await fs.mkdir(outDir, { recursive: true });

const files = (await fs.readdir(sourceDir))
  .filter((name) => /^Naval_Architecture_Teaching_Toolkit_Vol\d+\.xlsx$/i.test(name))
  .sort((a, b) => Number(a.match(/\d+/)[0]) - Number(b.match(/\d+/)[0]));

const summaries = [];
for (const name of files) {
  const fullPath = path.join(sourceDir, name);
  const wb = await SpreadsheetFile.importXlsx(await FileBlob.load(fullPath));
  const sheetInfo = await wb.inspect({ kind: "sheet", include: "id,name", maxChars: 8000 });
  const compact = await wb.inspect({
    kind: "workbook,table",
    maxChars: 5000,
    tableMaxRows: 5,
    tableMaxCols: 8,
    tableMaxCellChars: 80,
  });
  summaries.push({ name, sheets: sheetInfo.ndjson, compact: compact.ndjson });

  if ([1, 12, 18, 24, 28].includes(Number(name.match(/\d+/)[0]))) {
    const sheets = sheetInfo.ndjson.split("\n").filter(Boolean).map((line) => JSON.parse(line));
    for (const entry of sheets.slice(0, 3)) {
      const sheetName = entry.name;
      if (!sheetName) continue;
      const preview = await wb.render({ sheetName, autoCrop: "all", scale: 1, format: "png" });
      const safe = sheetName.replace(/[<>:"/\\|?*]/g, "_");
      await fs.writeFile(path.join(outDir, `${path.parse(name).name}_${safe}.png`), new Uint8Array(await preview.arrayBuffer()));
    }
  }
}
await fs.writeFile(path.join(outDir, "summary.json"), JSON.stringify(summaries, null, 2));
console.log(JSON.stringify({ files: files.length, outDir }, null, 2));
