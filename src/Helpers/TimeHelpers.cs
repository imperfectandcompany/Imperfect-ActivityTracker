using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperfectActivityTracker.Helpers
{
    public static class TimeHelpers
    {
        public static string FormatPlaytime(int totalSeconds)
        {
            string[] units = { "year", "month", "day", "hour", "minute", "second" };
            int[] timeDivisors = { 31536000, 2592000, 86400, 3600, 60, 1 };

            StringBuilder formattedTime = new StringBuilder();
            bool addedValue = false;

            for (int i = 0; i < units.Length; i++)
            {
                int timeValue = totalSeconds / timeDivisors[i];
                totalSeconds %= timeDivisors[i];
                if (timeValue > 0)
                {
                    if (formattedTime.Length > 0)
                    {
                        formattedTime.Append(", ");
                    }

                    string unit = "";

                    if (timeValue <= 1)
                    {
                        unit = units[i];
                    }
                    else if (timeValue > 1)
                    {
                        unit = units[i] + "s";
                    }

                    formattedTime.Append($"{timeValue} {unit}");
                    addedValue = true;
                }
            }

            if (!addedValue)
            {
                return "0" + "seconds";
            }

            return formattedTime.ToString();
        }
    }
}
