using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models.MYOB
{
	public class MyobItemPriceModel
	{
		public int ItemPriceID { get; set; }
		public int ItemID { get; set; }
		public short? QuantityBreak { get; set; }
		public double? QuantityBreakAmount { get; set; }
		public char? PriceLevel { get; set; }
		public string PriceLevelNameID { get; set; }
		public decimal? SellingPrice { get; set; }
		public string ChangeControl { get; set; }
		public decimal? LastUnitPrice { get; set; }
		public string ItemCode { get; set; }
	}
}