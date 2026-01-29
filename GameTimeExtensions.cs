using System;
using MSCCoreLibrary;

namespace CallThePlumber
{
    internal class GameTimeExtensions
    {
        public class GameDateTime
        {
            public GameTime.Days Day { get; private set; }
            public int Hour { get; private set; }
            public int Minute { get; private set; }

            private const int minutesPerDay = 1440;

            public static GameDateTime Now()
            {
                return new GameDateTime()
                {
                    Day = GameTime.Day,
                    Hour = GameTime.Hour,
                    Minute = GameTime.Minute,
                };
            }

            public void AdvanceHours(float hours)
            {
                if (hours < 0f)
                    throw new ArgumentOutOfRangeException(nameof(hours), hours, "Adding negative hours is not supported.");

                int minutesToAdd = (int)Math.Round(hours * 60f);

                int totalMinutes = Hour * 60 + Minute + minutesToAdd;
                int dayOverFlow = totalMinutes / minutesPerDay;
                int minutesOfDay = totalMinutes % minutesPerDay;

                int newHour = minutesOfDay / 60;
                int newMinute = minutesOfDay % 60;

                Day = GameTimeExtensions.AdvanceDays(Day, dayOverFlow);
                Hour = newHour;
                Minute = newMinute;
            }

        }

        private static readonly GameTime.Days[] OrderedSingleDays = [
            GameTime.Days.Monday,
            GameTime.Days.Tuesday,
            GameTime.Days.Wednesday,
            GameTime.Days.Thursday,
            GameTime.Days.Friday,
            GameTime.Days.Saturday,
            GameTime.Days.Sunday,
        ];

        public static GameTime.Days AdvanceDays(GameTime.Days day, int daysToAdd)
        {
            if (daysToAdd < 0)
                throw new ArgumentOutOfRangeException(nameof(daysToAdd), daysToAdd, "Adding negative days is not supported.");

            int idx = Array.IndexOf(OrderedSingleDays, day);
            if (idx < 0)
                throw new ArgumentException($"Invalid day value: {day}. Use only specific days.", nameof(day));

            int newIdx = (idx + daysToAdd) % 7;
            return OrderedSingleDays[newIdx];
        }
    }
}
