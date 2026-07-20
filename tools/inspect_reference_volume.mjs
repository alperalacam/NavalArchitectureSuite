import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const file = "C:\\Temp\\Yacht Designs\\Naval architecture design toolkit\\Naval_Architecture_Teaching_Toolkit_Vol28.xlsx";
const wb = await SpreadsheetFile.importXlsx(await FileBlob.load(file));
for (const [sheetId, range] of [
  ["COVER", "A1:E31"],
  ["BOIL-OFF GAS", "A1:E25"],
  ["LNG PROPERTIES", "A1:E28"],
  ["REFERENCE TABLES", "A1:G21"],
]) {
  const values = await wb.inspect({ kind: "table", sheetId, range, include: "values,formulas", tableMaxRows: 40, tableMaxCols: 8, maxChars: 18000 });
  const styles = await wb.inspect({ kind: "computedStyle", sheetId, range: range === "A1:E31" ? "B2:E18" : "B2:E18", maxChars: 9000 });
  console.log(`\n### ${sheetId}\n${values.ndjson}\n---STYLES---\n${styles.ndjson}`);
}
