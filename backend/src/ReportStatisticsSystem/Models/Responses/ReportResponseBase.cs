namespace Models.Responses
{
    public class ReportResponseBase
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public int Total { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}