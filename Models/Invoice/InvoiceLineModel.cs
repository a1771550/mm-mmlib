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

        public string DescDisplay
        {
            get
            {
                int maxlength = int.Parse(ConfigurationManager.AppSettings["MaxDescRemarkDisplayLength"]);
                return ilDesc != null && ilDesc.Length > maxlength ? string.Concat(ilDesc.Substring(0, maxlength), "...") : ilDesc ?? string.Empty;
            }
        }
        public string RemarkDisplay
        {
            get
            {
                int maxlength = int.Parse(ConfigurationManager.AppSettings["MaxDescRemarkDisplayLength"]);
                return Remark != null && Remark.Length > maxlength ? string.Concat(Remark.Substring(0, maxlength), "...") : Remark ?? string.Empty;
            }
        }
    }

}
