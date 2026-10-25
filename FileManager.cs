using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MyExcelMAUIApp
{
    public class SpreadsheetFileData
    {
        public int Rows { get; set; }
        public int Columns { get; set; }

        public Dictionary<string, string> Formulas { get; set; }
            = new Dictionary<string, string>();
    }

    public class FileManager
    {
        private readonly string filePath;

        public FileManager()
        {
            filePath = Path.Combine(
                FileSystem.AppDataDirectory,
                "spreadsheet.json");
        }

        public string FilePath => filePath;


        public async Task SaveAsync(Spreadsheet spreadsheet)
        {
            if (spreadsheet == null)
                throw new ArgumentNullException(
                    nameof(spreadsheet));

            var data = new SpreadsheetFileData
            {
                Rows = spreadsheet.Rows,
                Columns = spreadsheet.Columns
            };

            for (int row = 1; row <= spreadsheet.Rows; row++)
            {
                for (int col = 1; col <= spreadsheet.Columns; col++)
                {
                    string address =
                        Cell.ColumnName(col) + row;

                    Cell? cell = spreadsheet.GetCell(address);

                    if (cell == null)
                        continue;

                    string formula = cell.GetFormula();

                    if (!string.IsNullOrEmpty(formula))
                    {
                        data.Formulas[address] = formula;
                    }
                }
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json =
                JsonSerializer.Serialize(data, options);

            await File.WriteAllTextAsync(filePath, json);
        }


        public async Task<Spreadsheet> LoadAsync()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "Збережену таблицю ще не знайдено.",
                    filePath);
            }

            string json =
                await File.ReadAllTextAsync(filePath);

            SpreadsheetFileData? data;

            try
            {
                data =
                    JsonSerializer.Deserialize<SpreadsheetFileData>(
                        json);
            }
            catch (JsonException)
            {
                throw new InvalidDataException(
                    "Файл містить некоректні дані.");
            }

            if (data == null ||
                data.Rows < 1 ||
                data.Columns < 1 ||
                data.Rows > 200 ||
                data.Columns > 200 ||
                data.Formulas == null)
            {
                throw new InvalidDataException(
                    "Некоректні розміри або структура таблиці.");
            }

            var spreadsheet =
                new Spreadsheet(data.Rows, data.Columns);

            foreach (var item in data.Formulas)
            {
                if (spreadsheet.GetCell(item.Key) == null)
                {
                    throw new InvalidDataException(
                        $"Некоректна адреса: {item.Key}");
                }

                if (item.Value == null)
                {
                    throw new InvalidDataException(
                        $"Відсутній вміст клітинки: {item.Key}");
                }

                spreadsheet.SetCell(
                    item.Key,
                    item.Value);
            }

            return spreadsheet;
        }
    }
}
