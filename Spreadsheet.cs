using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace MyExcelMAUIApp
{
    public enum ViewMode { Expressions, Values }

    public class Spreadsheet
    {
        private int rows;
        private int columns;
        private Dictionary<string, Cell> cells = new();
        private ViewMode mode = ViewMode.Values;
        private readonly ExpressionEvaluator evaluator = new();
        private readonly HashSet<string> activeReferences = new();
        private readonly Dictionary<string, object> resultCache = new();

        public int Rows => rows;
        public int Columns => columns;

        public Spreadsheet(int rows, int columns)
        {
            if (rows < 1 || columns < 1)
                throw new ArgumentException("Розміри таблиці мають бути додатними");
            this.rows = rows;
            this.columns = columns;
            for (int r = 1; r <= rows; r++)
                for (int c = 1; c <= columns; c++)
                {
                    Cell cell = new(r, c);
                    cells[cell.GetAddress()] = cell;
                }
        }

        public Cell? GetCell(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;
            cells.TryGetValue(address.Trim().ToUpperInvariant(), out Cell? cell);
            return cell;
        }
        public void SetCell(string address, string formula) => GetCell(address)?.SetFormula(formula);
        public void SetValue(string address, string value) => GetCell(address)?.SetValue(value);
        public string GetValue(string address) => GetCell(address)?.GetValue() ?? "";
        public string GetDisplayText(string address)
        {
            Cell? cell = GetCell(address);
            if (cell == null) return "";
            return mode == ViewMode.Expressions ? cell.GetFormula() : cell.GetValue();
        }
        public void SetMode(ViewMode mode) => this.mode = mode;
        public ViewMode GetMode() => mode;

        public void AddRow()
        {
            rows++;
            for (int c = 1; c <= columns; c++)
            {
                Cell cell = new(rows, c);
                cells[cell.GetAddress()] = cell;
            }
        }
        public void AddColumn()
        {
            columns++;
            for (int r = 1; r <= rows; r++)
            {
                Cell cell = new(r, columns);
                cells[cell.GetAddress()] = cell;
            }
        }

        public void DeleteRow(int index)
        {
            if (rows <= 1 || index < 1 || index > rows) return;
            Dictionary<string, Cell> moved = new();
            foreach (Cell old in cells.Values)
            {
                if (old.GetRow() == index) continue;
                int newRow = old.GetRow() > index ? old.GetRow() - 1 : old.GetRow();
                Cell cell = new(newRow, old.GetColumn());
                cell.SetFormula(UpdateReferences(old.GetFormula(), index, null));
                moved[cell.GetAddress()] = cell;
            }
            cells = moved;
            rows--;
            activeReferences.Clear();
        }
        public void DeleteColumn(int index)
        {
            if (columns <= 1 || index < 1 || index > columns) return;
            Dictionary<string, Cell> moved = new();
            foreach (Cell old in cells.Values)
            {
                if (old.GetColumn() == index) continue;
                int newColumn = old.GetColumn() > index ? old.GetColumn() - 1 : old.GetColumn();
                Cell cell = new(old.GetRow(), newColumn);
                cell.SetFormula(UpdateReferences(old.GetFormula(), null, index));
                moved[cell.GetAddress()] = cell;
            }
            cells = moved;
            columns--;
            activeReferences.Clear();
        }

        private string UpdateReferences(string formula, int? deletedRow, int? deletedColumn)
        {
            if (string.IsNullOrEmpty(formula)) return "";
            return Regex.Replace(formula,
                @"(?<![A-Za-z0-9_])([A-Za-z]+)([1-9][0-9]*)(?![A-Za-z0-9_])",
                match =>
                {
                    if (!int.TryParse(match.Groups[2].Value, out int r)) return match.Value;
                    int c = 0;
                    foreach (char letter in match.Groups[1].Value.ToUpperInvariant())
                    {
                        int digit = letter - 'A' + 1;
                        if (c > (int.MaxValue - digit) / 26) return match.Value;
                        c = c * 26 + digit;
                    }
                    if ((deletedRow.HasValue && r == deletedRow.Value) ||
                        (deletedColumn.HasValue && c == deletedColumn.Value)) return "#REF!";
                    if (deletedRow.HasValue && r > deletedRow.Value) r--;
                    if (deletedColumn.HasValue && c > deletedColumn.Value) c--;
                    return Cell.ColumnName(c) + r;
                });
        }

        public void CalculateAll()
        {
            resultCache.Clear();
            try
            {
                foreach (Cell cell in cells.Values)
                {
                    string formula = cell.GetFormula().Trim();
                    if (formula.Length == 0)
                    {
                        cell.SetValue("");
                        continue;
                    }

                    activeReferences.Clear();
                    try
                    {
                        activeReferences.Add(cell.GetAddress());
                        object value = EvaluateFormula(formula);
                        resultCache[cell.GetAddress()] = value;
                        cell.SetValue(FormatValue(value));
                    }
                    catch (ExpressionException ex) { cell.SetValue("Помилка: " + ex.Message); }
                    catch (DivideByZeroException) { cell.SetValue("Помилка: ділення на нуль"); }
                    catch (OverflowException) { cell.SetValue("Помилка: переповнення"); }
                    finally { activeReferences.Clear(); }
                }
            }
            finally
            {
                activeReferences.Clear();
                resultCache.Clear();
            }
        }

        private static string FormatValue(object value) => value is bool b ? (b ? "true" : "false") : value.ToString() ?? "";

        private object EvaluateFormula(string formula)
        {
            string expression = formula.Trim();
            if (expression.StartsWith("=", StringComparison.Ordinal))
                expression = expression.Substring(1).Trim();
            if (expression.Length == 0)
                throw new ExpressionException("Незавершена формула після '='");
            return evaluator.EvaluateValue(expression, this);
        }

        public object GetReferencedResult(string address)
        {
            Cell? cell = GetCell(address);
            if (cell == null) throw new ExpressionException($"Невідома клітинка: {address}");
            string key = cell.GetAddress();
            if (resultCache.TryGetValue(key, out object? cached))
                return cached;
            if (!activeReferences.Add(key))
                throw new ExpressionException($"Циклічне посилання на {key}");
            try
            {
                string formula = cell.GetFormula().Trim();
                object result = formula.Length == 0 ? BigInteger.Zero : EvaluateFormula(formula);
                resultCache[key] = result;
                return result;
            }
            finally { activeReferences.Remove(key); }
        }

        public BigInteger GetReferencedValue(string address)
        {
            object result = GetReferencedResult(address);
            if (result is BigInteger number) return number;
            throw new ExpressionException($"Клітинка {address} містить логічне значення, а потрібне число");
        }
    }
}
