using MMCommonLib.CommonHelpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public class PaymentMethodDetail:ReportBase
	{
		public List<IGrouping<string, PayLnView>> GroupedPayAmt { get; set; }
		public List<PayLnView> PayLnViews { get; set; }
		public List<SalesModel> SalesList { get; set; }
		public List<SalesModel> RefundList { get; set; }
		//public List<PayView> PayViews { getPG; set; }
		
		public List<PaymentType> PaymentTypes { get; set; }
		public Dictionary<string, string> DicPayTypes { get; set; }
		public List<string> ViewablePayTypeCodes { get; set; }
        

        public PaymentMethodDetail()
		{
			ViewablePayTypeCodes = new List<string>();
			PayLnViews = new List<PayLnView>();
			SalesList = new List<SalesModel>();
			RefundList = new List<SalesModel>();
			int lang = Helpers.ModelHelper.GetCurrentCulture();
			DicPayTypes = ModelHelper.GetDicPayTypes(lang);
		}
	}
}