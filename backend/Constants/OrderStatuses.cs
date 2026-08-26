namespace VirtualTryOn.Api.Constants;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Processing = "Processing";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = [Pending, Confirmed, Processing, Shipped, Delivered, Cancelled];
}
