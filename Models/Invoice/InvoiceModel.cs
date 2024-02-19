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
    public class InvoiceModel : MMDAL.Invoice
    {
        public string AmountDisplay { get { return siAmt==null?"": CommonHelper.FormatNumber((decimal)siAmt); } }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public decimal PayAmt { get; set; }
		public string PayRemark { get; set; }
        public string RemarkDisplay
        {
            get
            {
                int maxremarkdisplaylength = int.Parse(ConfigurationManager.AppSettings["MaxRemarkDisplayLength"]);
                return Remark != null && Remark.Length > maxremarkdisplaylength ? string.Concat(Remark.Substring(0, maxremarkdisplaylength), "...") : Remark ?? string.Empty;
            }
        }
        public string CreateTimeDisplay { get { return CommonHelper.FormatDateTime(CreateTime, true); } }
		public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }
		public string dateformat { get; set; }
		public string PayDate4ABSS { get { return CreateTime != null ? CommonHelper.FormatDate4ABSS(CreateTime, dateformat) : ModifyTime != null ? CommonHelper.FormatDate4ABSS((DateTime)ModifyTime, dateformat) : string.Empty; } }
		public string JsOperationTime { get; set; }
		public string JsModifyTime { get; set; }
		public string Creator { get; set; }
		public string Modifier { get; set; }
		public string SupplierName { get; set; }
		public decimal OwedAmt { get; set; } = 0;
		public string PayCreateBy { get; set; }
		public DateTime PayCreateTime { get; set; }

		
		public string spChequeNo { get; set; }
		public long payId { get; set; }
		public string filePath { get; set; }
		public string Files { get; set; }

		public List<InvoiceLineModel> Lines { get; set; } = [];
        public List<InvoicePaymentModel> Payments { get; set; } = [];

		public InvoiceModel()
		{
			Lines = new List<InvoiceLineModel>();
			Payments = new List<InvoicePaymentModel>();
		}
    }

}
