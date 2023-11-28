using CommonLib.Models;
using MMCommonLib.BaseModels;
using MMCommonLib.CommonModels;
using MMDAL;
using MMLib.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace MMLib.Models.Item
{
	public class StockModel:PagingBaseModel
	{
		public PagedList.IPagedList<DistinctItem> StockList { get; set; }		
		public Dictionary<DistinctItem, Dictionary<string,int>> DicItemLocQty { get; set; }
		public Dictionary<string, List<DistinctItem>> DicLocItemList;
		public bool EnableBuySellUnits { get; set; }
		public HashSet<int> ItemIdList { get; set; }
		public Dictionary<int, ItemOptions> DicIDItemOptions { get; set; }
		public string JsonDicIDItemOptions { get { return DicIDItemOptions == null ? string.Empty : System.Text.Json.JsonSerializer.Serialize(DicIDItemOptions); } }
		public StockModel()
		{
			DicItemLocQty = new Dictionary<DistinctItem, Dictionary<string, int>>();
			DicLocItemList = new Dictionary<string, List<DistinctItem>>();            
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
            connection.Open();
            ModelHelper.GetShops(connection, ref Shops, ref ShopNames, apId);
			using var context = new MMDbContext();
			ItemIdList = context.MyobItems.Where(x => x.AccountProfileId == apId).Select(x => x.itmItemID).Distinct().ToHashSet();
			var ItemOptionList = context.GetItemOptionsInfo1(apId).ToList();
			DicIDItemOptions = new Dictionary<int, ItemOptions>();
			if (ItemOptionList != null && ItemOptionList.Count > 0)
			{
				foreach(var item in ItemOptionList)
				{
					if (!DicIDItemOptions.ContainsKey(item.itemId))
					{
						DicIDItemOptions[item.itemId] = new ItemOptions
						{
							ChkBatch = item.chkBat,
							ChkSN = item.chkSN,
							WillExpire = item.chkVT,
							Disabled = item.TransactionCount > 0,
						};
					}
				}
			}
        }
	}

	public class LocQty
	{
		public int Qty { get; set; }
		public string LocCode { get; set; }
	}

	public class JsStock
	{
		public string Id { get; set; }
		public string itmCode { get; set; }
		public string LocCode { get; set; }
		public int Qty { get; set; }
	}

	public class LocItem:IEquatable<LocItem>
	{
		public int Id { get; set; }
		public string LocCode { get; set; }		

        public bool Equals(LocItem other)
        {
           return other!=null && other.LocCode==LocCode && other.Id == Id;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as LocItem);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}