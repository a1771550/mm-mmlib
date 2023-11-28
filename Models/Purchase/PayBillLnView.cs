using System;

namespace MMLib.Models.Purchase
{
	public class PayBillLnView
	{
		public long Id { get; set; }
		public string pstCode { get; set; }
		public decimal Amt { get; set; }
		public System.DateTime PayDate { get; set; }
		public DateTime? BillDate { get; set; }
	}
}
