using CommonLib.Helpers;
using MMDAL;
using System;

namespace MMLib.Models.Supplier
{
    public class InvoicePaymentModel:SupInvoicePayment
	{	
		public long payId { get; set; }
		public string CreateTimeDisplay { get { return CommonHelper.FormatDateTime(CreateTime, true); } }
		public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }
		public string dateformat { get; set; }
		public string PayDate4ABSS { get { return  CreateTime!= null ? CommonHelper.FormatDate4ABSS(CreateTime, dateformat) : ModifyTime!=null? CommonHelper.FormatDate4ABSS((DateTime)ModifyTime, dateformat) : string.Empty; } }
		public string JsCreateTime { get; set; }
		public string JsModifyTime { get; set; }
		public string Creator { get; set; }
		public string Modifier { get; set; }
		//public string InvoiceCode { get; set; }
		public string SupplierName { get; set; }
        public string supAccount { get; set; }
    }

}
