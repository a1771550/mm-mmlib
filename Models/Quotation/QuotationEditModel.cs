using MMCommonLib.CommonModels;
using MMLib.Helpers;
using System.Collections.Generic;

namespace MMLib.Models.Quotation
{
	public class QuotationEditModel : PagingBaseModel
	{		
		public List<QuotationView> Quotations { get; set; }
		public void GetList()
		{		
			Quotations = QuotationHelper.GetQuotationListFrmDB(apId, User.surUID);
		}
	}
}
