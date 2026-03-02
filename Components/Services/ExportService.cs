using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PDTActivityTracker.Components.Model;
using Microsoft.EntityFrameworkCore;
using PDTActivityTracker.Model;
using System.Drawing;

namespace PDTActivityTracker.Components.Services
{
    public class ExportService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ExportService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<byte[]> ExportReportsToExcelAsync(List<StoreDailyActivity> activities)
        {
            ExcelPackage.License.SetNonCommercialPersonal("TuitionValidation");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Monitoring Reports");

            // Headers
            worksheet.Cells[1, 1].Value = "Date";
            worksheet.Cells[1, 2].Value = "Store Code";
            worksheet.Cells[1, 3].Value = "Store Name";
            worksheet.Cells[1, 4].Value = "Started By";
            worksheet.Cells[1, 5].Value = "Status";
            worksheet.Cells[1, 6].Value = "Completed At";
            worksheet.Cells[1, 7].Value = "Progress (%)";

            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 114, 207));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            using var db = await _dbFactory.CreateDbContextAsync();
            var row = 2;
            foreach (var activity in activities)
            {
                var progress = await CalculateProgressAsync(activity.Id, db);

                worksheet.Cells[row, 1].Value = activity.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = activity.Store?.StoreCode ?? "N/A";
                worksheet.Cells[row, 3].Value = activity.Store?.StoreName ?? "N/A";
                worksheet.Cells[row, 4].Value = activity.StartedBy?.Fullname ?? "N/A";
                worksheet.Cells[row, 5].Value = activity.IsCompleted ? "Completed" : (activity.Date.Date < DateTime.Today ? "Incomplete" : "In Progress");
                worksheet.Cells[row, 6].Value = activity.CompletedAt?.ToString("hh:mm tt") ?? "-";
                worksheet.Cells[row, 7].Value = progress / 100;
                worksheet.Cells[row, 7].Style.Numberformat.Format = "0%";
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> ExportReportsToPdfAsync(List<StoreDailyActivity> activities)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var db = await _dbFactory.CreateDbContextAsync();

            // Pre-calculate progress for all activities to avoid DB calls in loop
            var activityIds = activities.Select(a => a.Id).ToList();
            var progressData = await db.TaskProgresses
                .Where(p => activityIds.Contains(p.StoreDailyActivityId))
                .GroupBy(p => p.StoreDailyActivityId)
                .Select(g => new { ActivityId = g.Key, Total = g.Count(), Completed = g.Count(p => p.IsCompleted) })
                .ToDictionaryAsync(x => x.ActivityId, x => x.Total == 0 ? 0 : Math.Round((double)x.Completed / x.Total * 100, 1));

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("PDT Activity Tracker").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Activity Monitoring Report").FontSize(14);
                        });

                        row.ConstantItem(150).Column(col =>
                        {
                            col.Item().AlignRight().Text($"Generated: {DateTime.Now:MMM dd, yyyy}");
                            col.Item().AlignRight().Text($"Time: {DateTime.Now:hh:mm tt}");
                        });
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80); // Date
                            columns.ConstantColumn(80); // Store Code
                            columns.RelativeColumn();   // Store Name
                            columns.RelativeColumn();   // Started By
                            columns.ConstantColumn(80); // Status
                            columns.ConstantColumn(80); // Completed At
                            columns.ConstantColumn(60); // Progress
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Date");
                            header.Cell().Element(CellStyle).Text("Code");
                            header.Cell().Element(CellStyle).Text("Store Name");
                            header.Cell().Element(CellStyle).Text("Started By");
                            header.Cell().Element(CellStyle).Text("Status");
                            header.Cell().Element(CellStyle).Text("Completed");
                            header.Cell().Element(CellStyle).Text("Progress");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White))
                                                .Background(Colors.Blue.Medium)
                                                .PaddingHorizontal(5)
                                                .PaddingVertical(5);
                            }
                        });

                        foreach (var activity in activities)
                        {
                            var progress = progressData.TryGetValue(activity.Id, out var p) ? p : 0;
                            var status = activity.IsCompleted ? "Completed" : (activity.Date.Date < DateTime.Today ? "Incomplete" : "In Progress");
                            var statusColor = activity.IsCompleted ? Colors.Green.Medium : (activity.Date.Date < DateTime.Today ? Colors.Red.Medium : Colors.Orange.Medium);

                            table.Cell().Element(ValueStyle).Text(activity.Date.ToString("yyyy-MM-dd"));
                            table.Cell().Element(ValueStyle).Text(activity.Store?.StoreCode ?? "N/A");
                            table.Cell().Element(ValueStyle).Text(activity.Store?.StoreName ?? "N/A");
                            table.Cell().Element(ValueStyle).Text(activity.StartedBy?.Fullname ?? "N/A");
                            table.Cell().Element(ValueStyle).Text(status).FontColor(statusColor).SemiBold();
                            table.Cell().Element(ValueStyle).Text(activity.CompletedAt?.ToString("hh:mm tt") ?? "-");
                            table.Cell().Element(ValueStyle).Text($"{progress}%");

                            static IContainer ValueStyle(IContainer container)
                            {
                                return container.PaddingHorizontal(5).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            }
                        }
                    });

                    page.Footer().Row(row => {
                        row.RelativeItem().Text(x => {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                        row.RelativeItem().AlignRight().Text("PDT Activity Tracker - Confidential");
                    });
                });
            });

            return document.GeneratePdf();
        }

        private async Task<double> CalculateProgressAsync(int activityId, AppDbContext db)
        {
            var total = await db.TaskProgresses.CountAsync(p => p.StoreDailyActivityId == activityId);
            if (total == 0) return 0;
            var completed = await db.TaskProgresses.CountAsync(p => p.StoreDailyActivityId == activityId && p.IsCompleted);
            return (double)completed / total * 100;
        }
    }
}
