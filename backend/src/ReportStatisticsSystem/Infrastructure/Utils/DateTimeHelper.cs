using System;

namespace Infrastructure.Utils
{
    public static class DateTimeHelper
    {
        public static DateTime StartOfDay(DateTime date)
        {
            return date.Date;
        }

        public static DateTime EndOfDay(DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        public static DateTime StartOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static DateTime EndOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)).AddDays(1).AddTicks(-1);
        }

        public static DateTime StartOfYear(DateTime date)
        {
            return new DateTime(date.Year, 1, 1);
        }

        public static DateTime EndOfYear(DateTime date)
        {
            return new DateTime(date.Year, 12, 31).AddDays(1).AddTicks(-1);
        }
    }
}