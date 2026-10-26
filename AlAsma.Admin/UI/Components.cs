using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;

namespace AlAsma.Admin.UI
{
    /// <summary>Data for the shared KPI card partial (_Kpi).</summary>
    public sealed record KpiModel(
        string Label,
        string Value,
        string? Currency = null,
        string? Note = null,
        string Tone = "",          // "", "ok", "bad"
        bool Hero = false,
        string? Tip = null,
        string? Breakdown = null);

    /// <summary>Data for the shared pagination partial (_Pagination).</summary>
    public sealed record PagerModel(int Current, int TotalPages, int TotalCount, int PageSize, Func<int, string?> UrlFor);

    /// <summary>Data for the shared empty-state partial (_EmptyState).</summary>
    public sealed record EmptyModel(string Title, string? Text = null, bool Compact = false);

    /// <summary>Data for the shared delete-confirmation modal partial (_DeleteModal).</summary>
    public sealed record DeleteModalModel(
        string ModalId,
        string Title,
        string Message,
        string? Area,
        string Controller,
        IReadOnlyList<KeyValuePair<string, string>> Facts);

    /// <summary>Status badges: always colour + icon + text.</summary>
    public static class Badges
    {
        public static IHtmlContent Contract(string? status)
        {
            var (css, icon) = status switch
            {
                "نشط" => ("success", "check-circle"),
                "ينتهي قريباً" => ("warning", "clock"),
                "منتهي" => ("danger", "x-circle"),
                _ => ("neutral", "minus-circle"),
            };
            var label = string.IsNullOrWhiteSpace(status) ? "غير محدد" : status;
            return new HtmlString($"<span class=\"badge badge--{css}\">{Icon.Svg(icon, 16)}{System.Net.WebUtility.HtmlEncode(label)}</span>");
        }

        public static string RingColor(string? status) => status switch
        {
            "نشط" => "var(--success-ink)",
            "ينتهي قريباً" => "var(--accent)",
            "منتهي" => "var(--danger-ink)",
            _ => "var(--line-strong)",
        };
    }

    public static class Format
    {
        /// <summary>Money with 2 decimals, e.g. 4,535.00</summary>
        public static string Money(decimal value) => value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Whole-number money for big KPI cards, e.g. 27,485</summary>
        public static string Whole(decimal value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        public static string Date(DateTime value) => value.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

        public static string DateTimeStamp(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
    }
}
