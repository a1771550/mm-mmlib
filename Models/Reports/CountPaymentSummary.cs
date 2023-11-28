using MMCommonLib.CommonHelpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public class CountPaymentSummary:ReportBase
	{
		
		public List<SessionLnView> SessionLnViews { get; set; }		

		public decimal TotalExpectedAmt { get; set; }
		public decimal TotalActualAmt { get; set; }
		public decimal TotalDiffAmt { get; set; }
		public Dictionary<string,string> DicPayTypes { get; set; }
		public CountPaymentSummary()
		{			
			SessionLnViews = new List<SessionLnView>();
			int lang = Helpers.ModelHelper.GetCurrentCulture();
			DicPayTypes = ModelHelper.GetDicPayTypes(lang,true);
		}
	}
}