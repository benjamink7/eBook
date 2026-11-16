using eBook.Model.Enums;

namespace eBook.Model.Reports;

public class RevenueReportRow
{
    public int PaymentId { get; set; }
    public string Username { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public DateTime PaidAt { get; set; }
    public decimal RefundedAmount { get; set; }
}

public class RevenueReportTotal
{
    public string Currency { get; set; } = null!;
    public int Count { get; set; }
    public decimal Paid { get; set; }
    public decimal Refunded { get; set; }
    public decimal Net { get; set; }
}

public class RevenueReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<RevenueReportRow> Rows { get; set; } = new();
    public List<RevenueReportTotal> Totals { get; set; } = new();
}
