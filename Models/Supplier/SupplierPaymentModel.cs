using CommonLib.Helpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Supplier
{
	public class SupplierPaymentModel:SupplierPayment
	{		
		public string CreateTimeDisplay { get { return CommonHelper.FormatDateTime(CreateTime, true); } }
		public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }

		public string JsCreateTime { get; set; }
		public string JsModifyTime { get; set; }
		public string Creator { get; set; }
		public string Modifier { get; set; }
	}

}
