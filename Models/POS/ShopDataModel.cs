using MMLib.Models;
using MMLib.Models.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMCommonLib.Models
{
	public class ShopDataModel
	{
		public List<SerialNoView> SerialNoList { get; set; }
		public List<SalesModel> SalesList { get; set; }
		public List<SalesLnView> SalesLnViews { get; set; }
		
		public List<PayView> PayList { get; set; }
		public List<PayLnView> PayLnViews { get; set; }
		public List<ItemModel> StockList { get; set; }
		public ShopDataModel()
		{
			SerialNoList = new List<SerialNoView>();
			SalesList = new List<SalesModel>();
			SalesLnViews = new List<SalesLnView>();
			PayList = new List<PayView>();
			PayLnViews = new List<PayLnView>();
			StockList = new List<ItemModel>();
		}
	}
}
