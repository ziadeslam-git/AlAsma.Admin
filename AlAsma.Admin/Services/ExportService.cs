using System;
using System.Collections.Generic;
using System.Linq;
using AlAsma.Admin.DTOs.Author;
using AlAsma.Admin.DTOs.Dashboard;
using AlAsma.Admin.DTOs.Sale;
using AlAsma.Admin.Interfaces;

namespace AlAsma.Admin.Services
{
    public class ExportService : IExportService
    {
        public string BuildAuthorSalesHtml(AuthorSalesExportDto d)
        {
            // Iterate d.Sales (full history), NOT RecentSales
            var rows = string.Join("", d.Sales.Select(s => $@"
    <tr>
      <td>{s.BookTitle}</td>
      <td style='text-align:center'>{s.SalePrice:N2}</td>
      <td style='text-align:center'>{s.BasicExpenses:N2}</td>
      <td style='text-align:center;font-weight:bold'>{s.TotalAmount:N2}</td>
      <td style='text-align:center'>{s.Quantity}</td>
      <td style='text-align:center'>{s.StoreLocation}</td>
      <td style='text-align:center;direction:ltr'>{s.SaleDate:yyyy/MM/dd HH:mm}</td>
    </tr>"));

            var netClass = d.NetProfit >= 0 ? "green" : "red";

            var opsRows = string.Join("", d.Operations.Select(o => $@"
    <tr>
      <td>{o.OperationName}</td>
      <td style='text-align:center'>{o.BookTitle}</td>
      <td style='text-align:center'>{o.ExpenseAmount:N2}</td>
      <td style='text-align:center'>{o.Quantity}</td>
      <td style='text-align:center;font-weight:bold'>{o.TotalAmount:N2}</td>
      <td style='text-align:center;direction:ltr'>{o.OperationDate:yyyy/MM/dd HH:mm}</td>
    </tr>"));

            return $@"<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='utf-8'>
<style>
  @page {{ margin: 20mm; }}
  body {{ font-family: Arial, sans-serif; direction: rtl; color: #1e293b; }}
  .header {{ text-align: center; border-bottom: 3px solid #064e3b; padding-bottom: 12px; margin-bottom: 20px; }}
  .header h1 {{ color: #064e3b; font-size: 20px; margin: 0 0 4px; }}
  .header p {{ color: #64748b; font-size: 13px; margin: 0; }}
  .info-grid {{ display: grid; grid-template-columns: repeat(3,1fr); gap: 12px; margin-bottom: 20px; }}
  .info-card {{ border: 1px solid #e2e8f0; border-radius: 8px; padding: 10px 14px; }}
  .info-card .label {{ font-size: 11px; color: #94a3b8; margin-bottom: 4px; }}
  .info-card .value {{ font-size: 16px; font-weight: bold; color: #0f172a; }}
  .info-card .value.green {{ color: #059669; }}
  .info-card .value.red {{ color: #dc2626; }}
  .info-card .value.orange {{ color: #ea580c; }}
  table {{ width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 20px; }}
  h2.table-title {{ font-size: 16px; color: #064e3b; margin-top: 30px; margin-bottom: 5px; }}
  th {{ background-color: #064e3b; color: white; padding: 9px 8px; text-align: center; font-weight: 600; }}
  th.orange {{ background-color: #ea580c; }}
  td {{ border: 1px solid #e2e8f0; padding: 8px; }}
  tr:nth-child(even) {{ background: #f8fafc; }}
  .total-row {{ background: #ecfdf5 !important; font-weight: bold; }}
  .total-row-ops {{ background: #fff7ed !important; font-weight: bold; }}
  .footer {{ text-align: center; margin-top: 30px; font-size: 11px; color: #94a3b8; border-top: 1px solid #e2e8f0; padding-top: 10px; }}
</style>
</head>
<body>
<div class='header'>
  <h1>الأسمى للنشر والتوزيع</h1>
  <p>تقرير المبيعات والعمليات — {d.AuthorName}</p>
  <p>تاريخ التقرير: {DateTime.Now:yyyy/MM/dd}</p>
</div>

<div class='info-grid'>
  <div class='info-card'>
    <div class='label'>إجمالي المبيعات</div>
    <div class='value'>{d.TotalSales:N2} <span style='font-size:11px;font-weight:normal'>ج.م</span></div>
  </div>
  <div class='info-card'>
    <div class='label'>مصاريف أساسية</div>
    <div class='value red'>{d.BasicFees:N2} <span style='font-size:11px;font-weight:normal'>ج.م</span></div>
  </div>
  <div class='info-card'>
    <div class='label'>عمليات إضافية</div>
    <div class='value orange'>{d.OperationsExpenses:N2} <span style='font-size:11px;font-weight:normal'>ج.م</span></div>
  </div>
  <div class='info-card'>
    <div class='label'>صافي الربح</div>
    <div class='value {netClass}'>{d.NetProfit:N2} <span style='font-size:11px;font-weight:normal'>ج.م</span></div>
  </div>
  <div class='info-card'>
    <div class='label'>عدد المبيعات</div>
    <div class='value'>{d.SalesCount}</div>
  </div>
  <div class='info-card'>
    <div class='label'>عدد العمليات</div>
    <div class='value'>{d.OperationsCount}</div>
  </div>
</div>

<h2 class='table-title'>المبيعات</h2>
<table>
  <thead>
    <tr>
      <th>الكتاب</th><th>السعر</th><th>المصروفات</th>
      <th>المجموع</th><th>الكمية</th><th>المنفذ</th><th>التاريخ</th>
    </tr>
  </thead>
  <tbody>
    {rows}
    <tr class='total-row'>
      <td colspan='3' style='text-align:center'>الإجمالي</td>
      <td style='text-align:center'>{d.TotalSales:N2} ج.م</td>
      <td style='text-align:center'>{d.Sales.Sum(s => s.Quantity)}</td>
      <td colspan='2'></td>
    </tr>
  </tbody>
</table>

<h2 class='table-title'>العمليات الإضافية</h2>
<table>
  <thead>
    <tr>
      <th class='orange'>العملية</th><th class='orange'>الكتاب</th>
      <th class='orange'>التكلفة</th><th class='orange'>الكمية</th>
      <th class='orange'>المجموع</th><th class='orange'>التاريخ</th>
    </tr>
  </thead>
  <tbody>
    {opsRows}
    <tr class='total-row-ops'>
      <td colspan='3' style='text-align:center'>الإجمالي</td>
      <td style='text-align:center'>{d.Operations.Sum(o => o.Quantity)}</td>
      <td style='text-align:center'>{d.OperationsExpenses:N2} ج.م</td>
      <td></td>
    </tr>
  </tbody>
</table>

<div class='footer'>© {DateTime.Now.Year} جميع الحقوق محفوظة لدار الأسمى للنشر والتوزيع</div>
</body>
</html>";
        }

        public string BuildAllSalesHtml(IEnumerable<SaleListDto> sales, DateTime reportDate)
        {
            var salesList = sales.ToList();

            var rows = string.Join("", salesList.Select(s => $@"
<tr>
<td>{s.BookTitle}</td>
<td>{s.AuthorName}</td>
<td>{s.SalePrice:N2}</td>
<td>{s.BasicExpenses:N2}</td>
<td><strong>{s.TotalAmount:N2}</strong></td>
<td>{s.Quantity}</td>
<td>{s.StoreLocation}</td>
<td dir='ltr'>{s.SaleDate:yyyy/MM/dd HH:mm}</td>
</tr>"));

            return $@"<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='utf-8'>
<style>
    body {{ font-family: 'Arial', sans-serif; direction: rtl; margin: 40px; }}
    h1 {{ text-align: center; color: #064e3b; font-size: 22px; border-bottom: 3px solid #064e3b; padding-bottom: 10px; }}
    .date {{ text-align: center; color: #666; margin-bottom: 20px; }}
    table {{ width: 100%; border-collapse: collapse; font-size: 13px; }}
    th {{ background-color: #064e3b; color: white; padding: 10px 8px; text-align: center; }}
    td {{ border: 1px solid #ddd; padding: 8px; text-align: center; }}
    tr:nth-child(even) {{ background-color: #f8f9fa; }}
    .total {{ font-weight: bold; background-color: #e8f5e9 !important; }}
</style>
</head>
<body>
<h1>الأسمى للنشر والتوزيع — تقرير المبيعات</h1>
<p class='date'>تاريخ التقرير: {reportDate:yyyy/MM/dd}</p>
<table>
<thead>
<tr><th>الكتاب</th><th>الكاتب</th><th>السعر</th><th>المصروفات</th><th>المجموع</th><th>الكمية</th><th>المنفذ</th><th>التاريخ</th></tr>
</thead>
<tbody>
{rows}
<tr class='total'>
<td colspan='4'>الإجمالي</td>
<td><strong>{salesList.Sum(s => s.TotalAmount):N2} ج.م</strong></td>
<td><strong>{salesList.Sum(s => s.Quantity)}</strong></td>
<td colspan='2'></td>
</tr>
</tbody>
</table>
</body>
</html>";
        }

        public string BuildAllOperationsHtml(IEnumerable<AlAsma.Admin.DTOs.Operation.OperationListDto> operations, DateTime reportDate)
        {
            var opsList = operations.ToList();

            var rows = string.Join("", opsList.Select(o => $@"
<tr>
<td>{o.OperationName}</td>
<td>{o.BookTitle}</td>
<td>{o.AuthorName}</td>
<td>{o.ExpenseAmount:N2}</td>
<td>{o.Quantity}</td>
<td><strong>{o.TotalAmount:N2}</strong></td>
<td dir='ltr'>{o.OperationDate:yyyy/MM/dd HH:mm}</td>
</tr>"));

            return $@"<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='utf-8'>
<style>
    body {{ font-family: 'Arial', sans-serif; direction: rtl; margin: 40px; }}
    h1 {{ text-align: center; color: #ea580c; font-size: 22px; border-bottom: 3px solid #ea580c; padding-bottom: 10px; }}
    .date {{ text-align: center; color: #666; margin-bottom: 20px; }}
    table {{ width: 100%; border-collapse: collapse; font-size: 13px; }}
    th {{ background-color: #ea580c; color: white; padding: 10px 8px; text-align: center; }}
    td {{ border: 1px solid #ddd; padding: 8px; text-align: center; }}
    tr:nth-child(even) {{ background-color: #f8f9fa; }}
    .total {{ font-weight: bold; background-color: #fff7ed !important; }}
</style>
</head>
<body>
<h1>الأسمى للنشر والتوزيع — تقرير العمليات التشغيلية</h1>
<p class='date'>تاريخ التقرير: {reportDate:yyyy/MM/dd}</p>
<table>
<thead>
<tr><th>بيان العملية</th><th>الكتاب</th><th>الكاتب</th><th>التكلفة</th><th>الكمية</th><th>المجموع</th><th>التاريخ</th></tr>
</thead>
<tbody>
{rows}
<tr class='total'>
<td colspan='4'>الإجمالي</td>
<td><strong>{opsList.Sum(o => o.Quantity)}</strong></td>
<td><strong>{opsList.Sum(o => o.TotalAmount):N2} ج.م</strong></td>
<td></td>
</tr>
</tbody>
</table>
</body>
</html>";
        }
    }
}
