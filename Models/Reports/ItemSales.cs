using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public class ItemSales:ReportBase
	{
		public List<SalesLnView> SalesLnViews { get; set; }
		public List<SalesLnView> RefundLnViews { get; set; }
	
		public List<IGrouping<string, SalesLnView>> GroupedItemSales { get; set; }
		public List<IGrouping<string, SalesLnView>> GroupedItemRefunds { get; set; }

        public List<IGrouping<string, SalesLnView>> GroupedBySalesmenItemSales { get; set; }
        public List<IGrouping<string, SalesLnView>> GroupedBySalesmenItemRefunds{ get; set; }
        public List<IGrouping<string, SalesLnView>> GroupedByPOItemSales { get; set; }
        public List<IGrouping<string, SalesLnView>> GroupedByPOItemRefunds { get; set; }
        public string Mode { get; set; }

		public ItemSales()
		{
			SalesLnViews = new List<SalesLnView>();
			RefundLnViews = new List<SalesLnView>();
			GroupedItemSales = new List<IGrouping<string, SalesLnView>>();
			GroupedItemRefunds = new List<IGrouping<string, SalesLnView>>();
            GroupedBySalesmenItemSales = new List<IGrouping<string, SalesLnView>>();
			GroupedBySalesmenItemRefunds = new List<IGrouping<string, SalesLnView>>();
			GroupedByPOItemSales = new List<IGrouping<string, SalesLnView>>();
			GroupedByPOItemRefunds = new List<IGrouping<string, SalesLnView>>();
        }
	}
}