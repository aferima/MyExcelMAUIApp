using System;

namespace MyExcelMAUIApp
{
    public class Cell
    {
        private string formula = "";
        private string value = "";
        private int row;
        private int column;

        public Cell(int row, int column)
        {
            this.row = row;
            this.column = column;
        }

        public void SetFormula(string formula)
        {
            this.formula = formula ?? "";
            this.value = "";
        }

        public void SetValue(string value)
        {
            this.value = value ?? "";
        }

        public string GetValue()
        {
            return value;
        }

        public string GetFormula()
        {
            return formula;
        }

        public int GetRow() => row;
        public int GetColumn() => column;

        public string GetAddress()
        {
            return ColumnName(column) + row;
        }

        public static string ColumnName(int colIndex)
        {
            int dividend = colIndex;
            string name = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                name = Convert.ToChar(65 + modulo) + name;
                dividend = (dividend - modulo) / 26;
            }
            return name;
        }
    }
}