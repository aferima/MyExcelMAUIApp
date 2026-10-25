
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyExcelMAUIApp
{
    public partial class MainPage : ContentPage
    {
        private const int CountColumn = 20;
        private const int CountRow = 50;
        private Spreadsheet spreadsheet = new(CountRow, CountColumn);
        private readonly FileManager fileManager = new();
        private Entry? selectedEntry;
        private bool updatingFormulaBar;

        public MainPage()
        {
            InitializeComponent();
            spreadsheet.SetMode(ViewMode.Expressions);
            ModeButton.Text = "Режим: ВИРАЗИ";
            textInput.TextChanged += TextInput_TextChanged;
            textInput.Unfocused += TextInput_Unfocused;
            CreateGrid();
        }

        private void CreateGrid()
        {
            selectedEntry = null;
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();
            SetFormulaBarText("");

            for (int c = 0; c <= spreadsheet.Columns; c++)
            {
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(70) });

                if (c > 0)
                    AddColumnLabel(c);
            }

            for (int r = 0; r <= spreadsheet.Rows; r++)
            {
                grid.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(44) });

                if (r > 0)
                    AddRowLabelAndEntries(r, spreadsheet.Columns);
            }

            ShowErrors();
        }

        private void AddColumnLabel(int col)
        {
            Label label = new()
            {
                Text = Cell.ColumnName(col),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            grid.SetRow(label, 0);
            grid.SetColumn(label, col);
            grid.Children.Add(label);
        }

        private void AddRowLabelAndEntries(int row, int columnCount)
        {
            Label label = new()
            {
                Text = row.ToString(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            grid.SetRow(label, row);
            grid.SetColumn(label, 0);
            grid.Children.Add(label);

            for (int col = 1; col <= columnCount; col++)
                AddEntry(row, col);
        }

        private void AddEntry(int row, int col)
        {
            Entry entry = new()
            {
                Text = "",
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalOptions = LayoutOptions.Center
            };

            entry.Focused += Entry_Focused;
            entry.Unfocused += Entry_Unfocused;

            grid.SetRow(entry, row);
            grid.SetColumn(entry, col);
            grid.Children.Add(entry);

            RefreshEntry(entry);
        }

        private string GetAddress(Entry entry)
        {
            return Cell.ColumnName(grid.GetColumn(entry)) +
                   grid.GetRow(entry);
        }

        private void RefreshEntry(Entry entry)
        {
            Cell? cell = spreadsheet.GetCell(GetAddress(entry));

            if (cell == null)
                return;

            string text = spreadsheet.GetMode() == ViewMode.Expressions
                ? cell.GetFormula()
                : (cell.GetValue() != ""
                    ? cell.GetValue()
                    : cell.GetFormula());

            entry.Text = text;

            if (spreadsheet.GetMode() == ViewMode.Values &&
                text.StartsWith("Помилка", StringComparison.Ordinal))
            {
                entry.TextColor = Colors.Red;
            }
            else
            {
                entry.ClearValue(Entry.TextColorProperty);
            }
        }

        private void RefreshAll()
        {
            foreach (Entry entry in grid.Children.OfType<Entry>().ToList())
            {
                if (!entry.IsFocused)
                    RefreshEntry(entry);
            }
        }

        private bool CommitEntry(Entry entry)
        {
            Cell? cell = spreadsheet.GetCell(GetAddress(entry));

            if (cell == null)
                return false;

            string text = entry.Text ?? "";

            if (cell.GetFormula() == text)
                return false;

            spreadsheet.SetCell(cell.GetAddress(), text);
            return true;
        }

        private bool CommitFocusedEntry()
        {
            bool changed = false;

            foreach (Entry entry in grid.Children.OfType<Entry>().ToList())
            {
                if (entry.IsFocused)
                    changed |= CommitEntry(entry);
            }

            return changed;
        }

        private void SetFormulaBarText(string value)
        {
            updatingFormulaBar = true;

            try
            {
                textInput.Text = value;
            }
            finally
            {
                updatingFormulaBar = false;
            }
        }

        private void TextInput_TextChanged(
            object? sender,
            TextChangedEventArgs e)
        {
            if (updatingFormulaBar || selectedEntry == null)
                return;

            Cell? cell = spreadsheet.GetCell(GetAddress(selectedEntry));

            if (cell == null)
                return;

            string formula = e.NewTextValue ?? "";

            if (cell.GetFormula() != formula)
                spreadsheet.SetCell(cell.GetAddress(), formula);

            selectedEntry.Text = formula;
            selectedEntry.ClearValue(Entry.TextColorProperty);

            ShowErrors();
        }

        private void TextInput_Unfocused(
            object? sender,
            FocusEventArgs e)
        {
            Recalculate();
        }

        private void Entry_Focused(
            object? sender,
            FocusEventArgs e)
        {
            if (sender is not Entry entry)
                return;

            selectedEntry = entry;

            Cell? cell = spreadsheet.GetCell(GetAddress(entry));

            if (cell == null)
                return;

            entry.ClearValue(Entry.TextColorProperty);
            entry.Text = cell.GetFormula();
            SetFormulaBarText(cell.GetFormula());

            ShowErrors();
        }

        private void Entry_Unfocused(
            object? sender,
            FocusEventArgs e)
        {
            if (sender is not Entry entry)
                return;

            CommitEntry(entry);
            Recalculate();
            RefreshEntry(entry);
        }

        private void ShowErrors()
        {
            if (selectedEntry == null)
            {
                StatusLabel.Text = "";
                StatusLabel.IsVisible = false;
                return;
            }

            string address = GetAddress(selectedEntry);
            string value = spreadsheet.GetValue(address);

            if (value.StartsWith("Помилка", StringComparison.Ordinal))
            {
                StatusLabel.Text = $"{address}: {value}";
                StatusLabel.IsVisible = true;
            }
            else
            {
                StatusLabel.Text = "";
                StatusLabel.IsVisible = false;
            }
        }

        private void Recalculate()
        {
            spreadsheet.CalculateAll();
            RefreshAll();
            ShowErrors();
        }

        private void AutoRecalculate()
        {
            spreadsheet.CalculateAll();
            spreadsheet.SetMode(ViewMode.Values);
            ModeButton.Text = "Режим: ЗНАЧЕННЯ";
            RefreshAll();
            ShowErrors();
        }

        private void CalculateButton_Clicked(
            object? sender,
            EventArgs e)
        {
            CommitFocusedEntry();
            AutoRecalculate();
        }

        private void ToggleModeButton_Clicked(
            object? sender,
            EventArgs e)
        {
            if (CommitFocusedEntry())
                spreadsheet.CalculateAll();

            ViewMode next = spreadsheet.GetMode() == ViewMode.Expressions
                ? ViewMode.Values
                : ViewMode.Expressions;

            spreadsheet.SetMode(next);

            ModeButton.Text = next == ViewMode.Expressions
                ? "Режим: ВИРАЗИ"
                : "Режим: ЗНАЧЕННЯ";

            RefreshAll();
            ShowErrors();
        }

        private async void SaveButton_Clicked(
            object? sender,
            EventArgs e)
        {
            CommitFocusedEntry();

            try
            {
                await fileManager.SaveAsync(spreadsheet);

                await DisplayAlertAsync(
                    "Збереження",
                    "Таблицю збережено:\n" + fileManager.FilePath,
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Помилка збереження",
                    ex.Message,
                    "OK");
            }
        }

        private async void ReadButton_Clicked(
            object? sender,
            EventArgs e)
        {
            try
            {
                spreadsheet = await fileManager.LoadAsync();
                spreadsheet.SetMode(ViewMode.Expressions);

                ModeButton.Text = "Режим: ВИРАЗИ";

                CreateGrid();
                spreadsheet.CalculateAll();
                ShowErrors();

                await DisplayAlertAsync(
                    "Завантаження",
                    "Таблицю успішно завантажено!",
                    "OK");
            }
            catch (FileNotFoundException)
            {
                await DisplayAlertAsync(
                    "Файл не знайдено",
                    "Спочатку збережіть таблицю.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Помилка завантаження",
                    ex.Message,
                    "OK");
            }
        }

        private async void ExitButton_Clicked(
            object? sender,
            EventArgs e)
        {
            if (await DisplayAlertAsync(
                "Підтвердження",
                "Ви дійсно хочете вийти?",
                "Так",
                "Ні"))
            {
                Environment.Exit(0);
            }
        }

        private async void HelpButton_Clicked(
            object? sender,
            EventArgs e)
        {
            await DisplayAlertAsync(
                "Довідка",
                "Лабораторна робота №1, варіант 22.\n" +
                "Операції: +, -, *, /, mod, div, ^, =, <>, <, >, <=, >=.",
                "OK");
        }

        private void AddRowButton_Clicked(
            object? sender,
            EventArgs e)
        {
            CommitFocusedEntry();
            spreadsheet.AddRow();

            grid.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(44) });

            AddRowLabelAndEntries(
                spreadsheet.Rows,
                spreadsheet.Columns);

            spreadsheet.CalculateAll();
            RefreshAll();
            ShowErrors();
        }

        private void AddColumnButton_Clicked(
            object? sender,
            EventArgs e)
        {
            CommitFocusedEntry();
            spreadsheet.AddColumn();

            int col = spreadsheet.Columns;

            grid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(70) });

            AddColumnLabel(col);

            for (int row = 1; row <= spreadsheet.Rows; row++)
                AddEntry(row, col);

            spreadsheet.CalculateAll();
            RefreshAll();
            ShowErrors();
        }

        private async void DeleteRowButton_Clicked(
            object? sender,
            EventArgs e)
        {
            CommitFocusedEntry();

            if (selectedEntry == null)
            {
                await DisplayAlertAsync(
                    "Виберіть клітинку",
                    "Спочатку виберіть клітинку рядка.",
                    "OK");
                return;
            }

            if (spreadsheet.Rows <= 1)
                return;

            int row = grid.GetRow(selectedEntry);

            if (!await DisplayAlertAsync(
                "Видалення",
                $"Видалити рядок {row}?",
                "Так",
                "Ні"))
            {
                return;
            }

            ViewMode previousMode = spreadsheet.GetMode();

            spreadsheet.DeleteRow(row);
            spreadsheet.CalculateAll();
            spreadsheet.SetMode(previousMode);

            CreateGrid();
            ShowErrors();
        }

        private async void DeleteColumnButton_Clicked(
            object? sender,
            EventArgs e)
        {
            CommitFocusedEntry();

            if (selectedEntry == null)
            {
                await DisplayAlertAsync(
                    "Виберіть клітинку",
                    "Спочатку виберіть клітинку стовпця.",
                    "OK");
                return;
            }

            if (spreadsheet.Columns <= 1)
                return;

            int col = grid.GetColumn(selectedEntry);

            if (!await DisplayAlertAsync(
                "Видалення",
                $"Видалити стовпець {Cell.ColumnName(col)}?",
                "Так",
                "Ні"))
            {
                return;
            }

            ViewMode previousMode = spreadsheet.GetMode();

            spreadsheet.DeleteColumn(col);
            spreadsheet.CalculateAll();
            spreadsheet.SetMode(previousMode);

            CreateGrid();
            ShowErrors();
        }
    }
}
