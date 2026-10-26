using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AlAsma.Admin.DTOs.Dashboard;
using AlAsma.Admin.DTOs.Operation;
using AlAsma.Admin.DTOs.Sale;
using AlAsma.Admin.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlAsma.Admin.Services
{
    public class PdfExportService : IPdfExportService
    {
        private const string PrimaryColor = "#064e3b";
        private const string OperationsColor = "#ea580c";
        private const string CompanyName = "الأسمى للنشر والتوزيع";

        // Windows-hosted servers have Arial/Tahoma, both with Arabic glyphs.
        private static readonly string[] FontFamilies = { "Arial", "Tahoma", "Segoe UI" };

        static PdfExportService()
        {
            // QuestPDF Community license (free for organisations below the revenue threshold of the license).
            QuestPDF.Settings.License = LicenseType.Community;

            // QuestPDF 2026.9+ no longer uses system fonts by default. Arabic text needs a font with Arabic glyphs:
            // use the server's installed fonts (Arial / Tahoma on Windows), and any font files dropped into
            // the "Fonts" folder next to the app (e.g. Cairo.ttf) are discovered too.
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
            var fontsFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts");
            if (System.IO.Directory.Exists(fontsFolder))
                QuestPDF.Settings.FontDiscoveryPath = fontsFolder;
        }

        private static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
        private static string Stamp(DateTime value) => value.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);

        public byte[] BuildAuthorSalesPdf(AuthorSalesExportDto d)
        {
            return Document.Create(container => container.Page(page =>
            {
                ConfigurePage(page, $"تقرير المبيعات والعمليات — {d.AuthorName}");

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                        Kpi(table, "إجمالي المبيعات", Money(d.TotalSales) + " ج.م", "#0f172a");
                        Kpi(table, "مصاريف أساسية", Money(d.BasicFees) + " ج.م", "#dc2626");
                        Kpi(table, "عمليات إضافية", Money(d.OperationsExpenses) + " ج.م", OperationsColor);
                        Kpi(table, "صافي الربح", Money(d.NetProfit) + " ج.م", d.NetProfit >= 0 ? "#059669" : "#dc2626");
                        Kpi(table, "عدد المبيعات", d.SalesCount.ToString(CultureInfo.InvariantCulture), "#0f172a");
                        Kpi(table, "عدد العمليات", d.OperationsCount.ToString(CultureInfo.InvariantCulture), "#0f172a");
                    });

                    column.Item().Text("المبيعات").FontSize(13).Bold().FontColor(PrimaryColor);
                    column.Item().Element(c => DataTable(c, PrimaryColor,
                        new[] { "الكتاب", "السعر", "المصروفات", "المجموع", "الكمية", "المنفذ", "التاريخ" },
                        new float[] { 2.6f, 1.4f, 1.4f, 1.6f, 1, 2, 2.8f },
                        d.Sales.Select(s => new[] { s.BookTitle, Money(s.SalePrice), Money(s.BasicExpenses), Money(s.TotalAmount), s.Quantity.ToString(CultureInfo.InvariantCulture), s.StoreLocation, Stamp(s.SaleDate) }),
                        new[] { "الإجمالي", "", "", Money(d.TotalSales), d.Sales.Sum(s => s.Quantity).ToString(CultureInfo.InvariantCulture), "", "" },
                        "#ecfdf5"));

                    column.Item().Text("العمليات الإضافية").FontSize(13).Bold().FontColor(OperationsColor);
                    column.Item().Element(c => DataTable(c, OperationsColor,
                        new[] { "العملية", "الكتاب", "التكلفة", "الكمية", "المجموع", "التاريخ" },
                        new float[] { 2.4f, 2.4f, 1.4f, 1, 1.6f, 2.8f },
                        d.Operations.Select(o => new[] { o.OperationName, o.BookTitle ?? "", Money(o.ExpenseAmount), o.Quantity.ToString(CultureInfo.InvariantCulture), Money(o.TotalAmount), Stamp(o.OperationDate) }),
                        new[] { "الإجمالي", "", "", d.Operations.Sum(o => o.Quantity).ToString(CultureInfo.InvariantCulture), Money(d.OperationsExpenses), "" },
                        "#fff7ed"));
                });
            })).GeneratePdf();
        }

        public byte[] BuildAllSalesPdf(IEnumerable<SaleListDto> sales, DateTime reportDate)
        {
            var list = sales.ToList();
            return Document.Create(container => container.Page(page =>
            {
                ConfigurePage(page, $"تقرير المبيعات — {reportDate:yyyy/MM/dd}");

                page.Content().PaddingTop(10).Element(c => DataTable(c, PrimaryColor,
                    new[] { "الكتاب", "الكاتب", "السعر", "المصروفات", "المجموع", "الكمية", "المنفذ", "التاريخ" },
                    new float[] { 2.4f, 2f, 1.3f, 1.4f, 1.5f, 1, 1.8f, 2.6f },
                    list.Select(s => new[] { s.BookTitle, s.AuthorName, Money(s.SalePrice), Money(s.BasicExpenses), Money(s.TotalAmount), s.Quantity.ToString(CultureInfo.InvariantCulture), s.StoreLocation, Stamp(s.SaleDate) }),
                    new[] { "الإجمالي", "", "", "", Money(list.Sum(s => s.TotalAmount)), list.Sum(s => s.Quantity).ToString(CultureInfo.InvariantCulture), "", "" },
                    "#ecfdf5"));
            })).GeneratePdf();
        }

        public byte[] BuildAllOperationsPdf(IEnumerable<OperationListDto> operations, DateTime reportDate)
        {
            var list = operations.ToList();
            return Document.Create(container => container.Page(page =>
            {
                ConfigurePage(page, $"تقرير العمليات التشغيلية — {reportDate:yyyy/MM/dd}", OperationsColor);

                page.Content().PaddingTop(10).Element(c => DataTable(c, OperationsColor,
                    new[] { "بيان العملية", "الكتاب", "الكاتب", "التكلفة", "الكمية", "المجموع", "التاريخ" },
                    new float[] { 2.4f, 2.2f, 2f, 1.4f, 1, 1.6f, 2.8f },
                    list.Select(o => new[] { o.OperationName, o.BookTitle ?? "", o.AuthorName, Money(o.ExpenseAmount), o.Quantity.ToString(CultureInfo.InvariantCulture), Money(o.TotalAmount), Stamp(o.OperationDate) }),
                    new[] { "الإجمالي", "", "", "", list.Sum(o => o.Quantity).ToString(CultureInfo.InvariantCulture), Money(list.Sum(o => o.TotalAmount)), "" },
                    "#fff7ed"));
            })).GeneratePdf();
        }

        // ─── Shared layout ──────────────────────────────────────────────

        private static void ConfigurePage(PageDescriptor page, string subtitle, string accent = PrimaryColor)
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.ContentFromRightToLeft();
            page.DefaultTextStyle(x => x.FontFamily(FontFamilies).FontSize(9.5f).FontColor("#1e293b"));

            page.Header().Column(column =>
            {
                column.Item().AlignCenter().Text(CompanyName).FontSize(18).Bold().FontColor(accent);
                column.Item().AlignCenter().Text(subtitle).FontSize(11).FontColor("#64748b");
                column.Item().PaddingTop(6).LineHorizontal(2).LineColor(accent);
            });

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor("#94a3b8"));
                    text.Span($"© {DateTime.Now.Year} جميع الحقوق محفوظة لدار الأسمى للنشر والتوزيع");
                });
                row.ConstantItem(110).AlignLeft().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor("#94a3b8"));
                    text.Span("صفحة ");
                    text.CurrentPageNumber();
                    text.Span(" من ");
                    text.TotalPages();
                });
            });
        }

        private static void Kpi(TableDescriptor table, string label, string value, string valueColor)
        {
            table.Cell().Padding(3).Border(0.7f).BorderColor("#e2e8f0").Padding(8).Column(column =>
            {
                column.Item().Text(label).FontSize(8.5f).FontColor("#94a3b8");
                column.Item().Text(value).FontSize(13).Bold().FontColor(valueColor);
            });
        }

        private static void DataTable(IContainer container, string headerColor, string[] headers, float[] widths,
            IEnumerable<string[]> rows, string[] totalRow, string totalBackground)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var width in widths) columns.RelativeColumn(width);
                });

                table.Header(header =>
                {
                    foreach (var title in headers)
                        header.Cell().Background(headerColor).Padding(5).AlignCenter()
                            .Text(title).FontColor(Colors.White).Bold();
                });

                var index = 0;
                foreach (var row in rows)
                {
                    var background = index++ % 2 == 0 ? "#ffffff" : "#f8fafc";
                    foreach (var cell in row)
                        table.Cell().Background(background).BorderBottom(0.5f).BorderColor("#e2e8f0")
                            .Padding(4).AlignCenter().Text(cell);
                }

                foreach (var cell in totalRow)
                    table.Cell().Background(totalBackground).Padding(5).AlignCenter().Text(cell).Bold();
            });
        }
    }
}
