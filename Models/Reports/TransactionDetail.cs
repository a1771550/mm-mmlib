using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public class TransactionDetail:CountPaymentDetail
	{
		public List<MyobItem> Items { get; set; }
		public List<SalesLnView> SalesLnViews { get; set; }
		public List<SalesLnView> RefundLnViews { get; set; }
		public List<PayView> ChangeList { get; set; }
		
		public int TotalQtySold { get; set; }
     

        public TransactionDetail():base()
		{
			Items = new List<MyobItem>();
			SalesLnViews = new List<SalesLnView>();
			RefundLnViews = new List<SalesLnView>();
			ChangeList = new List<PayView>();
		}
	}
}