using APSIM.Shared.Utilities;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UnitTests.UtilityTests
{
    [TestFixture]
    public class DataUtilitiesTests
    {
        [Test]
        public void TestValidDateFormats()
        {
            //build list of supported month names
            string[] months = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12",
                                "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12"};
            for (int i = 0; i <= 12; i++)
                months.Append(DateUtilities.MONTHS_3_LETTERS[i]);
            for (int i = 0; i <= 12; i++)
                months.Append(DateUtilities.MONTHS_AU_LETTERS[i]);
            for (int i = 0; i <= 12; i++)
                months.Append(DateUtilities.MONTHS_FULL_NAME[i]);

            //selection of semi random years so we don't have to test every day of every year
            string[] years = { "1900", "1907", "1923", "1935", "1948", "1952", "1964", "1973", "1985", "1999", "2000", "2004", "2016", "2023", "05", "10", "21" };

            //ISO - test ISO format sperately
            string isoTest1 = $"2000-01-01T00:00:00";
            string isoTest2 = $"2000-01-01";
            string isoTest3 = $"2000-1-01";
            string isoTest4 = $"2000-01-1";
            string isoTest5 = $"2000-1-1";
            Assert.That(DateUtilities.GetDate(isoTest1), Is.EqualTo(DateTime.Parse(isoTest1, null, DateTimeStyles.RoundtripKind)));
            Assert.That(DateUtilities.GetDate(isoTest2), Is.EqualTo(DateTime.Parse(isoTest2, null, DateTimeStyles.RoundtripKind)));
            Assert.That(DateUtilities.GetDate(isoTest3), Is.EqualTo(DateTime.Parse(isoTest2, null, DateTimeStyles.RoundtripKind)));
            Assert.That(DateUtilities.GetDate(isoTest4), Is.EqualTo(DateTime.Parse(isoTest2, null, DateTimeStyles.RoundtripKind)));
            Assert.That(DateUtilities.GetDate(isoTest5), Is.EqualTo(DateTime.Parse(isoTest2, null, DateTimeStyles.RoundtripKind)));

            //check dates are trimmed
            string trimTest = $" 2000-01-01 ";
            Assert.That(DateUtilities.GetDate(trimTest), Is.EqualTo(DateTime.Parse(trimTest, null, DateTimeStyles.RoundtripKind)));

            foreach (char seperator in DateUtilities.VALID_SEPERATORS)
            {
                foreach (string year in years)
                {
                    string fullYear = year;
                    if (fullYear.Length == 2)
                        fullYear = "20" + fullYear;

                    for (int month = 0; month < months.Length; month++)
                    {
                        int monthNum = (month % 12) + 1;
                        int daysInMonth = DateTime.DaysInMonth(int.Parse(year), monthNum);
                        for (int day = 1; day <= daysInMonth; day++)
                        {
                            string ddmmyyyy = $"{day}{seperator}{months[month]}{seperator}{year}";
                            string ddmm = $"{day}{seperator}{months[month]}";
                            string mmdd = $"{months[month]}{seperator}{day}";

                            //our date to compare against
                            DateTime date = new DateTime(int.Parse(fullYear), monthNum, day);

                            //Day-Month-Year
                            DateTime parsed = DateUtilities.GetDate(ddmmyyyy);
                            Assert.That(parsed, Is.EqualTo(date));

                            //day of year
                            parsed = DateUtilities.GetDate(date.DayOfYear, int.Parse(fullYear));
                            Assert.That(parsed, Is.EqualTo(date));

                            //day/month with date for year
                            parsed = DateUtilities.GetDate(ddmm, date);
                            Assert.That(parsed, Is.EqualTo(date));
                            //with year in string
                            parsed = DateUtilities.GetDate(ddmmyyyy, date);
                            Assert.That(parsed, Is.EqualTo(date));

                            //3 Ints
                            parsed = DateUtilities.GetDate(day, monthNum, int.Parse(fullYear));
                            Assert.That(parsed, Is.EqualTo(date));

                            //now we do all the date checking that will use a default year
                            date = new DateTime(DateUtilities.DEFAULT_YEAR, monthNum, day);

                            //day/month with mismatched year in string
                            parsed = DateUtilities.GetDate(ddmm + seperator.ToString() + DateUtilities.DEFAULT_YEAR.ToString(), date);
                            Assert.That(parsed, Is.EqualTo(date));
                            //with int year instead of date
                            parsed = DateUtilities.GetDate(ddmm, DateUtilities.DEFAULT_YEAR);
                            Assert.That(parsed, Is.EqualTo(date));

                            if (months[month].Length > 2)
                            { //we don't want to do these when month is a number, that should fail
                                //Month-Day-Year
                                parsed = DateUtilities.GetDate(mmdd + seperator.ToString() + year.ToString());
                                Assert.That(parsed, Is.EqualTo(date));

                                //Day-Month
                                parsed = DateUtilities.GetDate(ddmm);
                                Assert.That(parsed, Is.EqualTo(date));

                                //Month-Day
                                parsed = DateUtilities.GetDate(mmdd);
                                Assert.That(parsed, Is.EqualTo(date));

                                //DayMonth
                                parsed = DateUtilities.GetDate(day.ToString() + months[month]);
                                Assert.That(parsed, Is.EqualTo(date));

                                //MonthDay
                                parsed = DateUtilities.GetDate(months[month] + day.ToString());
                                Assert.That(parsed, Is.EqualTo(date));

                                //Replace Year
                                parsed = DateUtilities.GetDateReplaceYear(ddmm, DateUtilities.DEFAULT_YEAR);
                                Assert.That(parsed, Is.EqualTo(date));

                                parsed = DateUtilities.GetDateReplaceYear(ddmmyyyy, DateUtilities.DEFAULT_YEAR);
                                Assert.That(parsed, Is.EqualTo(date));
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void TestInvalidDateFormats()
        {
            Assert.Throws<Exception>(() => DateUtilities.GetDate("")); //empty string
            Assert.Throws<Exception>(() => DateUtilities.GetDate("   ")); //whitespace
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01")); //one number
            Assert.Throws<Exception>(() => DateUtilities.GetDate("String")); //a word
            Assert.Throws<Exception>(() => DateUtilities.GetDate("String-String-String")); //a word with seperators
            Assert.Throws<Exception>(() => DateUtilities.GetDate(null)); //null
            Assert.Throws<Exception>(() => DateUtilities.GetDate("29-Feburary-1900")); //no leap year despite being a leap year in 1900
            Assert.Throws<Exception>(() => DateUtilities.GetDate("29-Feburary-2001"));
            Assert.Throws<Exception>(() => DateUtilities.GetDate("29-Feburary-2000")); //Leap Year
            Assert.Throws<Exception>(() => DateUtilities.GetDate("Jab-12")); //bad month
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01:Jan:2000")); //bad symbol
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-Jan:2000")); //different symbols
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-Jan-2000-")); //to many symbols
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-Jan-")); //empty end
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-23-2000")); //US Format with no month name
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-13-2000")); //Impossible month
            Assert.Throws<Exception>(() => DateUtilities.GetDate("40-01-2000")); //Impossible day
            Assert.Throws<Exception>(() => DateUtilities.GetDate("1-Aug-15-Aug")); //typo in date list
        }

        [Test]
        public void TestDateFunctions()
        {
            int day = 10;
            string month = "Jan";
            int year = 2000;

            string ddmmyyyy = $"{day}-{month}-{year}";
            string ddmm = $"{day}-{month}";
            string mmdd = $"{month}-{day}";

            string yyyymmdd = $"{year}-01-{day}";

            DateTime date = new DateTime(year, 1, day);

            //Compares
            Assert.That(DateUtilities.CompareDates(ddmmyyyy, date), Is.Zero);
            Assert.That(DateUtilities.CompareDates(ddmmyyyy, date.AddDays(1)), Is.Positive);
            Assert.That(DateUtilities.CompareDates(ddmmyyyy, date.AddDays(-1)), Is.Negative);

            //DayMonth comparisions
            Assert.That(DateUtilities.DayMonthIsEqual(ddmmyyyy, "2000-01-10"), Is.True);
            Assert.That(DateUtilities.DayMonthIsEqual(ddmmyyyy, "2000-01-11"), Is.False);

            Assert.That(DateUtilities.DayMonthIsEqual(ddmmyyyy, date), Is.True);
            Assert.That(DateUtilities.DayMonthIsEqual(ddmmyyyy, date.AddDays(1)), Is.False);
            Assert.That(DateUtilities.DayMonthIsEqual(date, date.AddYears(1)), Is.True);

            //WithinDates
            string ddmmyyyy_5 = $"{day + 5}-{month}-{year}";
            string ddmmyyyy_10 = $"{day + 10}-{month}-{year}";
            Assert.That(DateUtilities.WithinDates(ddmmyyyy, new DateTime(2000, 1, 12), ddmmyyyy_5), Is.True); //between
            Assert.That(DateUtilities.WithinDates(ddmmyyyy_5, new DateTime(2000, 1, 12), ddmmyyyy_10), Is.False); //not between
            Assert.That(DateUtilities.WithinDates(ddmmyyyy_10, date, ddmmyyyy_5), Is.True); //between, wrap around year
            Assert.That(DateUtilities.WithinDates(ddmmyyyy_10, new DateTime(2000, 1, 12), ddmmyyyy), Is.False); //not between, wrap around year
            Assert.That(DateUtilities.WithinDates(ddmmyyyy, date, ddmmyyyy), Is.True); //on same date

            //DatesAreEqual
            Assert.That(DateUtilities.DatesAreEqual(ddmmyyyy, date), Is.True);
            Assert.That(DateUtilities.DatesAreEqual(ddmmyyyy, date.AddDays(1)), Is.False);
            Assert.That(DateUtilities.DatesAreEqual(ddmmyyyy, date.AddMonths(1)), Is.False);
            Assert.That(DateUtilities.DatesAreEqual(ddmmyyyy, date.AddYears(1)), Is.False);

            //IsEndOfMonth
            string ddmmyyyy_endMonth = $"31-Jan-2000";
            Assert.That(DateUtilities.IsEndOfMonth(DateUtilities.GetDate(ddmmyyyy_endMonth)), Is.True);
            Assert.That(DateUtilities.IsEndOfMonth(DateUtilities.GetDate(ddmmyyyy)), Is.False);

            //IsEndOfYear
            string ddmmyyyy_endYear = $"31-Dec-2000";
            Assert.That(DateUtilities.IsEndOfYear(DateUtilities.GetDate(ddmmyyyy_endYear)), Is.True);
            Assert.That(DateUtilities.IsEndOfYear(DateUtilities.GetDate(ddmmyyyy)), Is.False);

            //ValidateDateString
            Assert.That(DateUtilities.ValidateDateString("10/January/2000"), Is.EqualTo(yyyymmdd));
            Assert.That(DateUtilities.ValidateDateString("10 January"), Is.EqualTo(ddmm));
            Assert.That(DateUtilities.ValidateDateString("January 10"), Is.EqualTo(ddmm));

            Assert.That(DateUtilities.ValidateDateString("FakeMonth 10"), Is.Null);

            //ValidateDateStringWithYear
            Assert.That(DateUtilities.ValidateDateStringWithYear("10/January/2000"), Is.EqualTo(yyyymmdd));
            Assert.That(DateUtilities.ValidateDateStringWithYear("10 January"), Is.Null);

            //GetNextDate
            Assert.That(DateUtilities.GetNextDate("2-Jan", date), Is.EqualTo(DateUtilities.GetDate("2-Jan-2001"))); //2-Jan is before date
            Assert.That(DateUtilities.GetNextDate("20-Jan", date), Is.EqualTo(DateUtilities.GetDate("20-Jan-2000"))); //20-Jan is after date
            Assert.That(DateUtilities.GetNextDate("2-Jan-2000", date), Is.EqualTo(DateUtilities.GetDate("2-Jan-2001"))); //With year
            Assert.That(DateUtilities.GetNextDate("20-Jan-2000", date), Is.EqualTo(DateUtilities.GetDate("20-Jan-2000"))); //With year
            Assert.That(DateUtilities.GetNextDate("2-Jan-1996", date), Is.EqualTo(DateUtilities.GetDate("2-Jan-2001"))); //With multiple years
        }
    }
}
