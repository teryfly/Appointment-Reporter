using System;

namespace Repositories.QueryBuilders
{
    public static class DateRangeQueryBuilder
    {
        public static (DateTime, DateTime) GetRange(string groupBy, DateTime date)
        {
            switch (groupBy)
            {
                case "day":
                    return (date.Date, date.Date.AddDays(1).AddTicks(-1));
                case "month":
                    var start = new DateTime(date.Year, date.Month, 1);
                    var end = start.AddMonths(1).AddTicks(-1);
                    return (start, end);
                case "year":
                    var startYear = new DateTime(date.Year, 1, 1);
                    var endYear = startYear.AddYears(1).AddTicks(-1);
                    return (startYear, endYear);
                default:
                    throw new ArgumentException("Invalid groupBy value");
            }
        }
    }
}