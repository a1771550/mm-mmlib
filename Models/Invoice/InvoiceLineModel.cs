using CommonLib.Helpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Invoice
{
    public class InvoiceLineModel:InvoiceLine
    {
        public List<string> FileList { get; set; }
        public string AmountDisplay { get { return ilAmt == null ? "" : CommonHelper.FormatNumber((decimal)ilAmt, false); } }
        public string RemarkDisplay
        {
            get
            {
                int maxremarkdisplaylength = int.Parse(ConfigurationManager.AppSettings["MaxRemarkDisplayLength"]);
                return Remark != null && Remark.Length > maxremarkdisplaylength ? string.Concat(Remark.Substring(0, maxremarkdisplaylength), "...") : Remark ?? string.Empty;
            }
        }
    }

}
