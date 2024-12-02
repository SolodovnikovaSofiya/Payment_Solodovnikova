using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms.DataVisualization.Charting;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;


namespace Payment_Solodovnikova
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Entities _context = new Entities();
        private int count;

        public MainWindow()
        {
            InitializeComponent();
            ChartPayments.ChartAreas.Add(new ChartArea("Main"));
            this.Closing += MainWindow_Closing;
            var currentSeries = new Series("Платежи")
            {
                IsValueShownAsLabel = true
            };
            ChartPayments.Series.Add(currentSeries);
            CmbUser.ItemsSource = _context.Users.ToList();
            CmbDiagram.ItemsSource = Enum.GetValues(typeof(SeriesChartType));
        }
        private void UpdateChart(object sender, SelectionChangedEventArgs e)
        {
            if (CmbUser.SelectedItem is Users currentUser && CmbDiagram.SelectedItem is SeriesChartType currentType)
            {
                Series currentSeries = ChartPayments.Series.FirstOrDefault();
                currentSeries.ChartType = currentType;
                currentSeries.Points.Clear();

                var categoriesList = _context.Category.ToList();
                foreach (var category in categoriesList)
                {
                    currentSeries.Points.AddXY(category.Name,
                        _context.Payment.ToList().Where(u => u.Users == currentUser
                        && u.Category == category).Sum(u => u.Price * u.Num));
                }
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы действительно хотите закрыть приложение?"
                , "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void Btn_excel(object sender, RoutedEventArgs e)
        {
            var allUsers = _context.Users.ToList().OrderBy(u => u.FIO).ToList();

            var application = new Excel.Application();
            application.SheetsInNewWorkbook = allUsers.Count();
            Excel.Workbook workbook = application.Workbooks.Add(Type.Missing);

            for (int i = 0; i < allUsers.Count(); i++)
            {
                int startRowIndex = 1;
                Excel.Worksheet worksheet = application.Worksheets.Item[i + 1];
                worksheet.Name = allUsers[i].FIO;

                worksheet.Cells[1][startRowIndex] = "Дата платежа";
                worksheet.Cells[2][startRowIndex] = "Название";
                worksheet.Cells[3][startRowIndex] = "Стоимость";
                worksheet.Cells[4][startRowIndex] = "Количество";
                worksheet.Cells[5][startRowIndex] = "Сумма";
                Excel.Range columlHeaderRange = worksheet.Range[worksheet.Cells[1][1], worksheet.Cells[5][1]];
                columlHeaderRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                columlHeaderRange.Font.Bold = true;
                startRowIndex++;

                var userCategories = allUsers[i].Payment.OrderBy(u => u.Date).GroupBy(u => u.Category).OrderBy(u => u.Key.Name);

                foreach (var groupCategory in userCategories)
                {
                    Excel.Range headerRange = worksheet.Range[worksheet.Cells[1][startRowIndex], worksheet.Cells[5][startRowIndex]];
                    headerRange.Merge();
                    headerRange.Value = groupCategory.Key.Name;
                    headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    headerRange.Font.Italic = true;
                    startRowIndex++;

                    foreach (var payment in groupCategory)
                    {
                        worksheet.Cells[1][startRowIndex] = payment.Date.ToString("dd.MM.yyyy");
                        worksheet.Cells[2][startRowIndex] = payment.Name; 
                        worksheet.Cells[3][startRowIndex] = payment.Price;
                        (worksheet.Cells[3][startRowIndex] as Excel.Range).NumberFormat = "0.00";
                        worksheet.Cells[4][startRowIndex] = payment.Num;
                        worksheet.Cells[5][startRowIndex].Formula = $"=C{startRowIndex}*D{startRowIndex}";
                        (worksheet.Cells[5][startRowIndex] as Excel.Range).NumberFormat = "0.00";
                        startRowIndex++;
                        worksheet.Columns.AutoFit();
                    }

                    Excel.Range sumRange = worksheet.Range[worksheet.Cells[1][startRowIndex], worksheet.Cells[4][startRowIndex]];
                    sumRange.Merge();
                    sumRange.Value = "ИТОГО:";
                    sumRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;

                    worksheet.Cells[5][startRowIndex].Formula = $"=SUM(E{startRowIndex - groupCategory.Count()}:" +
                    $"E{startRowIndex - 1})";
                    sumRange.Font.Bold = worksheet.Cells[5][startRowIndex].Font.Bold = true;
                    startRowIndex++;
                }

                // Добавляем строку "Общий итог"
                Excel.Range totalRange = worksheet.Range[worksheet.Cells[1][startRowIndex], worksheet.Cells[4][startRowIndex]];
                totalRange.Merge();
                totalRange.Value = "Общий итог:";
                totalRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                totalRange.Font.Bold = true;
                totalRange.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);

                worksheet.Cells[5][startRowIndex].Formula = $"=SUM(E2:E{startRowIndex - 1})";
                worksheet.Cells[5][startRowIndex].Font.Bold = true;
                worksheet.Cells[5][startRowIndex].Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);

                Excel.Range rangeBorders = worksheet.Range[worksheet.Cells[1][1], worksheet.Cells[5][startRowIndex]];
                rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeLeft]
                    .LineStyle = rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeRight].LineStyle = rangeBorders.Borders[Excel.XlBordersIndex.xlEdgeTop]
                    .LineStyle = rangeBorders.Borders[Excel.XlBordersIndex.xlInsideHorizontal].LineStyle = rangeBorders.Borders[Excel.XlBordersIndex.xlInsideVertical]
                    .LineStyle = Excel.XlLineStyle.xlContinuous;

                application.Visible = true;
            }
        }

        private void Btn_word(object sender, RoutedEventArgs e)
        {
            var allUsers = _context.Users.ToList();
            var allCategories = _context.Category.ToList();

            var application = new Word.Application();
            Word.Document document = application.Documents.Add();

            foreach (var user in allUsers)
            {
                Word.Paragraph userParagraph = document.Paragraphs.Add();
                Word.Range userRange = userParagraph.Range;
                userRange.Text = user.FIO;
                userParagraph.set_Style("Заголовок 1");
                userRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                userRange.InsertParagraphAfter();
                document.Paragraphs.Add();

                Word.Paragraph tableParagraph = document.Paragraphs.Add();
                Word.Range tableRange = tableParagraph.Range;
                Word.Table paymentsTable = document.Tables.Add(tableRange, allCategories.Count() + 1, 2);
                paymentsTable.Borders.InsideLineStyle = paymentsTable.Borders.OutsideLineStyle =
                         Word.WdLineStyle.wdLineStyleSingle;
                paymentsTable.Range.Cells.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                Word.Range cellRange;

                cellRange = paymentsTable.Cell(1, 1).Range;
                cellRange.Text = "Категория";
                cellRange = paymentsTable.Cell(1, 2).Range;
                cellRange.Text = "Сумма расходов";

                paymentsTable.Rows[1].Range.Font.Name = "Times New Roman";
                paymentsTable.Rows[1].Range.Font.Size = 14;
                paymentsTable.Rows[1].Range.Bold = 1;
                paymentsTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                for (int i = 0; i < allCategories.Count(); i++)
                {
                    var currentCategory = allCategories[i];
                    cellRange = paymentsTable.Cell(i + 2, 1).Range;
                    cellRange.Text = currentCategory.Name;
                    cellRange.Font.Name = "Times New Roman";
                    cellRange.Font.Size = 12;

                    cellRange = paymentsTable.Cell(i + 2, 2).Range;
                    cellRange.Text = user.Payment.ToList().
                    Where(u => u.Category == currentCategory).Sum(u => u.Num * u.Price).ToString("N2") + " руб.";
                    cellRange.Font.Name = "Times New Roman";
                    cellRange.Font.Size = 12;
                }
                document.Paragraphs.Add();
                Payment maxPayment = user.Payment.OrderByDescending(u => u.Price * u.Num).FirstOrDefault();
                if (maxPayment != null)
                {
                    Word.Paragraph maxPaymentParagraph = document.Paragraphs.Add();
                    Word.Range maxPaymentRange = maxPaymentParagraph.Range;
                    maxPaymentRange.Text = $"Самый дорогостоящий платеж - {maxPayment.Name} за {(maxPayment.Price * maxPayment.Num).ToString("N2")} " + $"руб. от {maxPayment.Date.ToString("dd.MM.yyyy")}";
                    maxPaymentParagraph.set_Style("Подзаголовок");
                    maxPaymentRange.Font.Color = Word.WdColor.wdColorDarkRed;
                    maxPaymentRange.InsertParagraphAfter();
                }
                document.Paragraphs.Add();

                Payment minPayment = user.Payment.OrderBy(u => u.Price * u.Num).FirstOrDefault();
                if (maxPayment != null)
                {
                    Word.Paragraph minPaymentParagraph = document.Paragraphs.Add();
                    Word.Range minPaymentRange = minPaymentParagraph.Range;
                    minPaymentRange.Text = $"Самый дешевый платеж - {minPayment.Name} за {(minPayment.Price * minPayment.Num).ToString("N2")} " + $"руб. от {minPayment.Date.ToString("dd.MM.yyyy")}";
                    minPaymentParagraph.set_Style("Подзаголовок");
                    minPaymentRange.Font.Color = Word.WdColor.wdColorDarkGreen;
                    minPaymentRange.InsertParagraphAfter();
                }
                if (user != allUsers.LastOrDefault()) document.Words.Last.InsertBreak(Word.WdBreakType.wdPageBreak);

                foreach (Word.Section section in document.Sections)
                {
                    Word.HeaderFooter header = section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary];
                    Word.Range headerRange = header.Range;
                    headerRange.Text = DateTime.Now.ToShortDateString();
                    headerRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                }

                foreach (Word.Section section in document.Sections)
                {
                    if (count == 0)
                    {
                        count++;
                        continue;
                    }
                    // Получаем нижний колонтитул
                    Word.HeaderFooter footer = section.Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary];
                    // Добавляем номер страницы в центр нижнего колонтитула
                    footer.PageNumbers.Add(Word.WdPageNumberAlignment.wdAlignPageNumberCenter);
                }

                application.Visible = true;
                document.SaveAs2(@"D:\Payments.docx");
                document.SaveAs2(@"D:\Payments.pdf", Word.WdExportFormat.wdExportFormatPDF);
            }
        }
    }
}
