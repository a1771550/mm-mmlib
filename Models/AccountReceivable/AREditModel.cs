using MMCommonLib.CommonModels;
using MMLib.Helpers;
using System.Collections.Generic;

namespace MMLib.Models.AccountReceivable
{
	public class AREditModel:PagingBaseModel
	{		
		public List<AccountReceivableView> AccountReceivables { get; set; }
		public void GetList()
		{
			AccountReceivables = ARHelper.GetARListFrmDB(apId);
		}
	}
}
