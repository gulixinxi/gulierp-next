import ExcelJS from 'exceljs';

function parseCsv(text: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let field = '';
  let quoted = false;
  for (let i = 0; i < text.length; i++) {
    const char = text[i];
    if (quoted && char === '"' && text[i + 1] === '"') {
      field += '"';
      i++;
    } else if (char === '"') {
      quoted = !quoted;
    } else if (!quoted && (char === ',' || char === '\n')) {
      row.push(field);
      field = '';
      if (char === '\n') {
        rows.push(row);
        row = [];
      }
    } else if (char !== '\r' || quoted) {
      field += char;
    }
  }
  if (field || row.length) {
    row.push(field);
    rows.push(row);
  }
  return rows;
}

export async function readSpreadsheetRows(file: File): Promise<Record<string, string>[]> {
  if (file.size > 5 * 1024 * 1024) throw new Error('文件不能超过 5 MB');
  const name = file.name.toLowerCase();
  let rows: string[][];
  if (name.endsWith('.csv')) {
    rows = parseCsv((await file.text()).replace(/^\uFEFF/, ''));
  } else if (name.endsWith('.xlsx')) {
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.load(await file.arrayBuffer() as any);
    const sheet = workbook.worksheets[0];
    if (!sheet) throw new Error('表格没有工作表');
    rows = [];
    sheet.eachRow((row) => {
      rows.push(Array.from({ length: sheet.columnCount }, (_, index) => row.getCell(index + 1).text));
    });
  } else if (name.endsWith('.xls')) {
    const XLSX = await import('xlsx');
    const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array' });
    const sheet = workbook.Sheets[workbook.SheetNames[0]];
    if (!sheet) throw new Error('表格没有工作表');
    rows = XLSX.utils.sheet_to_json<unknown[]>(sheet, { header: 1, defval: '', raw: false })
      .map(row => row.map(cell => String(cell ?? '')));
  } else {
    throw new Error('仅支持 .xls、.xlsx 或 .csv 文件');
  }
  if (rows.length < 2) return [];
  const headers = rows[0].map(header => header.trim());
  return rows.slice(1).map(row => Object.fromEntries(headers.map((header, index) => [header, row[index] ?? ''])));
}

export async function downloadSpreadsheet(rows: string[][], widths: number[], filename: string): Promise<void> {
  const workbook = new ExcelJS.Workbook();
  const sheet = workbook.addWorksheet('数据');
  sheet.columns = widths.map(width => ({ width }));
  rows.forEach(row => sheet.addRow(row));
  const buffer = await workbook.xlsx.writeBuffer();
  const url = URL.createObjectURL(new Blob([buffer as BlobPart], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
  }));
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
