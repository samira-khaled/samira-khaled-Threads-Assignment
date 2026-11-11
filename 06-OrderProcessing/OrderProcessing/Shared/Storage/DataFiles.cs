namespace OrderProcessing.Shared.Storage;

public static class DataFiles
{
    public static readonly string DataFolder = Path.Combine(AppContext.BaseDirectory, "Data");//مكان الملفات 
    public static readonly string InvoicesFolder = Path.Combine(DataFolder, "invoices");
    public static readonly string ReportsFolder = Path.Combine(DataFolder, "reports");

    public const string Customers = "customers.json";
    public const string Products = "products.json";
    public const string Coupons = "coupons.json";
    public const string IncomingOrders = "incoming_orders.json";
    public const string Orders = "orders.json";
    public const string Payments = "payments.json";
    public const string Emails = "emails.json";
    public const string EmailRetry = "email_retry.json";
}