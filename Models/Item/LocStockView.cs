using CommonLib.Helpers;
using MMCommonLib.CommonHelpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models.Item
{
	public class LocStockView:MyobLocStock
	{
		public string CreateTimeDisplay { get { return CommonHelper.FormatDateTime((DateTime)lstCreateTime, true); } }
		public string ModifyTimeDisplay { get { return CommonHelper.FormatDateTime((DateTime)lstModifyTime, true); } }
	}
}