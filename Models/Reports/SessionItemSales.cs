using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public class SessionItemSales:ReportBase
	{
		public List<MyobItem> Items { get; set; }
		public List<SalesLnView> SalesLnViews { get; set; }
		public List<SalesLnView> RefundLnViews { get; set; }
		public List<IGrouping<string, SalesLnView>> GroupedItemSales { get; set; }
		public List<IGrouping<string, SalesLnView>> GroupedItemRefunds { get; set; }
		public int TotalQtySold { get; set; }	
		public SessionItemSales()
		{			
			Items = new List<MyobItem>();
			SalesLnViews = new List<SalesLnView>();
			RefundLnViews = new List<SalesLnView>();
			GroupedItemSales = new List<IGrouping<string, SalesLnView>>();
		}
	}
}