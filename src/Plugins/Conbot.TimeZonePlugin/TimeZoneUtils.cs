using System.Collections.Generic;

namespace Conbot.TimeZonePlugin
{
    public static class TimeZoneUtils
    {
        public static Dictionary<string, string> TzdbGmtMapping =>
            new Dictionary<string, string>
            {
                ["Etc/GMT+12"] = "GMT-12",
                ["Etc/GMT+11"] = "GMT-11",
                ["Etc/GMT+10"] = "GMT-10",
                ["Etc/GMT+9"] = "GMT-9",
                ["Etc/GMT+8"] = "GMT-8",
                ["Etc/GMT+7"] = "GMT-7",
                ["Etc/GMT+6"] = "GMT-6",
                ["Etc/GMT+5"] = "GMT-5",
                ["Etc/GMT+4"] = "GMT-4",
                ["Etc/GMT+3"] = "GMT-3",
                ["Etc/GMT+2"] = "GMT-2",
                ["Etc/GMT+1"] = "GMT-1",
                ["Etc/GMT"] = "GMT",
                ["Etc/GMT-1"] = "GMT+1",
                ["Etc/GMT-2"] = "GMT+2",
                ["Etc/GMT-3"] = "GMT+3",
                ["Etc/GMT-4"] = "GMT+4",
                ["Etc/GMT-5"] = "GMT+5",
                ["Etc/GMT-6"] = "GMT+6",
                ["Etc/GMT-7"] = "GMT+7",
                ["Etc/GMT-8"] = "GMT+8",
                ["Etc/GMT-9"] = "GMT+9",
                ["Etc/GMT-10"] = "GMT+10",
                ["Etc/GMT-11"] = "GMT+11",
                ["Etc/GMT-12"] = "GMT+12",
                ["Etc/GMT-13"] = "GMT+13",
                ["Etc/GMT-14"] = "GMT+14"
            };

        public static Dictionary<string, string> GmtTzdbMapping =>
            new Dictionary<string, string>
            {
                ["GMT-12"] = "Etc/GMT+12",
                ["GMT-11"] = "Etc/GMT+11",
                ["GMT-10"] = "Etc/GMT+10",
                ["GMT-9"] = "Etc/GMT+9",
                ["GMT-8"] = "Etc/GMT+8",
                ["GMT-7"] = "Etc/GMT+7",
                ["GMT-6"] = "Etc/GMT+6",
                ["GMT-5"] = "Etc/GMT+5",
                ["GMT-4"] = "Etc/GMT+4",
                ["GMT-3"] = "Etc/GMT+3",
                ["GMT-2"] = "Etc/GMT+2",
                ["GMT-1"] = "Etc/GMT+1",
                ["GMT"] = "Etc/GMT",
                ["GMT+1"] = "Etc/GMT-1",
                ["GMT+2"] = "Etc/GMT-2",
                ["GMT+3"] = "Etc/GMT-3",
                ["GMT+4"] = "Etc/GMT-4",
                ["GMT+5"] = "Etc/GMT-5",
                ["GMT+6"] = "Etc/GMT-6",
                ["GMT+7"] = "Etc/GMT-7",
                ["GMT+8"] = "Etc/GMT-8",
                ["GMT+9"] = "Etc/GMT-9",
                ["GMT+10"] = "Etc/GMT-10",
                ["GMT+11"] = "Etc/GMT-11",
                ["GMT+12"] = "Etc/GMT-12",
                ["GMT+13"] = "Etc/GMT-13",
                ["GMT+14"] = "Etc/GMT-14"
            };
    }
}