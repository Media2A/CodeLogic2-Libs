using System;
using System.Linq;

namespace CL.Core.Utilities
{
    public partial class CLU_Parser
    {
        /// <summary>
        /// Calculates the next execution time based on a cron expression.
        /// </summary>
        /// <param name="cronExpression">The cron expression to parse.</param>
        /// <returns>The next DateTime when the cron expression will be true.</returns>
        /// <exception cref="ArgumentException">Thrown when the cron expression is invalid.</exception>
        public static DateTime CalculateCronNextExecutionTime(string cronExpression)
        {
            var now = DateTime.Now;
            var parts = cronExpression.Split(' ');

            if (parts.Length != 5)
            {
                throw new ArgumentException("Cron expression must consist of 5 fields.");
            }

            var minute = ParseCronField(parts[0], 0, 59);
            var hour = ParseCronField(parts[1], 0, 23);
            var dayOfMonth = ParseCronField(parts[2], 1, 31);
            var month = ParseCronField(parts[3], 1, 12);
            var dayOfWeek = ParseCronField(parts[4], 0, 6, true);

            var nextRun = now.AddMinutes(1);
            while (true)
            {
                if ((minute.Contains(nextRun.Minute) || minute.Contains(-1)) &&
                    (hour.Contains(nextRun.Hour) || hour.Contains(-1)) &&
                    (dayOfMonth.Contains(nextRun.Day) || dayOfMonth.Contains(-1)) &&
                    (month.Contains(nextRun.Month) || month.Contains(-1)) &&
                    (dayOfWeek.Contains((int)nextRun.DayOfWeek) || dayOfWeek.Contains(-1)))
                {
                    return nextRun;
                }

                nextRun = nextRun.AddMinutes(1);
            }
        }

        /// <summary>
        /// Parses a single field of a cron expression.
        /// </summary>
        /// <param name="field">The field of the cron expression to parse.</param>
        /// <param name="minValue">The minimum valid value for the field.</param>
        /// <param name="maxValue">The maximum valid value for the field.</param>
        /// <param name="isDayOfWeek">Indicates whether the field is for the day of the week.</param>
        /// <returns>An array of valid integers for the field.</returns>
        private static int[] ParseCronField(string field, int minValue, int maxValue, bool isDayOfWeek = false)
        {
            if (field == "*")
            {
                return new int[] { -1 };
            }

            return field.Split(',')
                        .SelectMany(part =>
                        {
                            if (part.Contains('/'))
                            {
                                var stepParts = part.Split('/');
                                var range = stepParts[0] == "*" ? Enumerable.Range(minValue, maxValue - minValue + 1) : ParseRange(stepParts[0], minValue, maxValue, isDayOfWeek);
                                var step = int.Parse(stepParts[1]);
                                return range.Where((_, index) => index % step == 0);
                            }
                            else if (part.Contains('-'))
                            {
                                return ParseRange(part, minValue, maxValue, isDayOfWeek);
                            }
                            else
                            {
                                return new int[] { ParseCronPart(part, minValue, maxValue, isDayOfWeek) };
                            }
                        })
                        .Where(value => value >= minValue && value <= maxValue)
                        .ToArray();
        }

        /// <summary>
        /// Parses a range of cron values.
        /// </summary>
        /// <param name="range">The range part of the cron field to parse.</param>
        /// <param name="minValue">The minimum valid value for the field.</param>
        /// <param name="maxValue">The maximum valid value for the field.</param>
        /// <param name="isDayOfWeek">Indicates whether the part is for the day of the week.</param>
        /// <returns>An array of integers representing the range.</returns>
        private static int[] ParseRange(string range, int minValue, int maxValue, bool isDayOfWeek)
        {
            var rangeParts = range.Split('-').Select(p => ParseCronPart(p, minValue, maxValue, isDayOfWeek)).ToArray();
            return Enumerable.Range(rangeParts[0], rangeParts[1] - rangeParts[0] + 1).ToArray();
        }

        /// <summary>
        /// Parses a part of a cron field, converting names to integers if necessary.
        /// </summary>
        /// <param name="part">The part of the cron field to parse.</param>
        /// <param name="minValue">The minimum valid value for the field.</param>
        /// <param name="maxValue">The maximum valid value for the field.</param>
        /// <param name="isDayOfWeek">Indicates whether the part is for the day of the week.</param>
        /// <returns>The parsed integer value of the part.</returns>
        private static int ParseCronPart(string part, int minValue, int maxValue, bool isDayOfWeek)
        {
            if (isDayOfWeek)
            {
                switch (part.ToLower())
                {
                    case "sun": return 0;
                    case "mon": return 1;
                    case "tue": return 2;
                    case "wed": return 3;
                    case "thu": return 4;
                    case "fri": return 5;
                    case "sat": return 6;
                }
            }
            return int.Parse(part);
        }
    }
}
