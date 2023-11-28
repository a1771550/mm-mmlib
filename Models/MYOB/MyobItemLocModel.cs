using System;
using System.Collections.Generic;
using System.Text;

namespace MMLib.Models.MYOB
{
	public class MyobItemLocModel
	{
		public int ItemLocationID { get; set; }
		public int ItemID { get; set; }
		public int LocationID { get; set; }
		public int QuantityOnHand { get; set; }
		public int SellOnOrder { get; set; }
		public int PurchaseOnOrder { get; set; }

		public string ItemCode { get; set; }
		public string LocationCode { get; set; }
	}
}
