using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndetectableBrowser
{
    internal class Variables
    {
        public string debug_port { get; set; }
        public static string proxySet { get; set; }
        public static string googleTblPath { get; set; } = Directory.GetCurrentDirectory() + @"\ReportTbl.xlsx";
        public static string order_id { get; set; }
        public static string order_phone { get; set; }
        public static string profNameInUndetect { get; set; }
        public static string profRegPass { get; set; }
        public static string profRegStatus { get; set; }
        public static string profRegWork { get; set; }
    }
}
