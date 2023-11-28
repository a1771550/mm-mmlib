using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models
{
    public class RecurOrder
    {
        public long wsUID { get; set; }
        public int IsRecurring { get; set; }
        public string Name { get; set; }
        public decimal TotalSalesAmt { get; set; }
        public DateTime? LastPostedDate { get; set; }
        public string LastPostedDateDisplay { get { return LastPostedDate == null ? "N/A" : CommonLib.Helpers.CommonHelper.FormatDate((DateTime)LastPostedDate, true); } }

        public string ItemsNameDesc { get; set; }
        public string wsCode { get; set; }
        public string pstCode { get; set; }
        public int pstUID { get; set; }
        public DateTime wsDeliveryDate { get; set; }
    }
}
