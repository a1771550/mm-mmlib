using CommonLib.Helpers;
using MMCommonLib.CommonModels;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using CommonLib.Models.MYOB;
using MMLib.Helpers;
using ModelHelper = MMLib.Helpers.ModelHelper;

namespace MMLib.Models.Item
{
	public class InventoryEditModel:PagingBaseModel
	{		
		public static List<Location> GetLocationFrmDB(int apId)
		{
			List<Location> locations = new List<Location>();
			using var context = new MMDbContext();
			var _locations = context.MyobLocations.Where(x => x.AccountProfileId == apId).ToList();
			if (_locations.Count > 0)
			{
				foreach (var location in _locations)
				{
					locations.Add(new Location
					{
						ID = location.LocationID,
						IDtxt = location.LocationIdentification,
						Name = location.LocationName
					});
				}
			}
			return locations;
		}

		public static List<ARItem> GetInventoryListFrmDB(int apId, string location)
		{
			List<ARItem> items = new List<ARItem>();
			using var context = new MMDbContext();
			var stocks = context.MyobLocStocks.AsNoTracking().Where(x => x.AccountProfileId == apId && x.lstStockLoc.ToLower() == location.ToLower()).ToList();
			var _items = context.MyobItems.AsNoTracking().Where(x => x.AccountProfileId == apId).ToList();

			if (_items.Count > 0)
			{
				foreach (var _item in _items)
				{
					var stock = stocks.FirstOrDefault(x => x.lstItemCode == _item.itmCode);
					decimal selloo = stock != null ? (decimal)stock.lstSellOnOrder : 0;
					decimal purchaseoo = stock != null ? (decimal)stock.lstPurchaseOnOrder : 0;
					decimal qtyav = stock != null ? (decimal)stock.lstQuantityAvailable : 0;				
					string locname = stock != null ? stock.lstStockLoc : "";
					int locId = stock != null ? (int)stock.lstLocationID : 0;
					items.Add(new ARItem
					{
						ItemName = _item.itmName,
						ItemNumber = _item.itmCode,
						SellOnOrder = selloo,
						PurchaseOnOrder = purchaseoo,
						QuantityAvailable = qtyav,
						ItemID = _item.itmItemID,
						LocationName = locname,
						LocationID = locId,
						ModifyTime = _item.itmModifyTime,
						Stock = stock
					});
				}
			}

			return items;
		}


		public static List<ARItem> GetInventoryListFrmDB(int apId, string location, ref string ItemsLastUpdateTime, ref string StocksLastUpdateTime)
		{
			List<ARItem> items = GetInventoryListFrmDB(apId, location);
			var item = items.FirstOrDefault();
			ItemsLastUpdateTime = item == null ? "N/A" : item.ModifyTimeDisplay;
			var stock = item == null ? null : item.Stock;
			StocksLastUpdateTime = stock != null && stock.lstModifyTime != null ? CommonHelper.FormatDateTime((DateTime)stock.lstModifyTime) : "N/A";
			return items;
		}

		public static ObservableCollection<ARItem> GetInventoryListFrmDB(int apId, string companyCode, string location, int pageIndex, int pageSize, string keyword)
		{
			ObservableCollection<ARItem> items = new ObservableCollection<ARItem>();
			using var context = new MMDbContext();
			var stocks = context.MyobLocStocks.AsNoTracking().Where(x => x.AccountProfileId == apId && x.lstStockLoc.ToLower() == location.ToLower()).ToList();
			var startIndex = CommonHelper.GetStartIndex(pageIndex, pageSize);

			var _items = context.GetItemList5(apId, location, startIndex, pageSize, keyword).ToList();
			if (_items.Count > 0)
			{
				int idx = startIndex;
				foreach (var _item in _items)
				{
					var stock = stocks.FirstOrDefault(x => x.lstItemCode == _item.itmCode);
					decimal selloo = stock != null ? (decimal)stock.lstSellOnOrder : 0;
					decimal purchaseoo = stock != null ? (decimal)stock.lstPurchaseOnOrder : 0;
					decimal qtyav = stock != null ? (decimal)stock.lstQuantityAvailable : 0;				
					string locname = stock != null ? stock.lstStockLoc : "";
					int locId = stock != null ? (int)stock.lstLocationID : 0;
					items.Add(new ARItem
					{
						idx = idx,
						ItemName = _item.itmName,
						ItemDesc = _item.itmDesc,
						itmUseDesc = _item.itmUseDesc,
						ItemNumber = _item.itmCode,
						SellOnOrder = selloo,
						PurchaseOnOrder = purchaseoo,
						QuantityAvailable = qtyav,						
						ItemID = _item.itmItemID,
						LocationName = locname,
						LocationID = locId
					});
					idx++;
				}
			}
			return items;
		}

		public static void SaveStocksToDB(int apId, List<ARItem> inventories)
		{
			DateTime dateTime = DateTime.Now;
			List<MyobLocStock> stocks = new List<MyobLocStock>();

			foreach (var item in inventories)
			{
				var qtyav = decimal.ToInt32((decimal)item.QuantityAvailable);
				var sellorder = decimal.ToInt32((decimal)item.SellOnOrder);
				var purchaseorder = decimal.ToInt32((decimal)item.PurchaseOnOrder);
				var qtyhand = decimal.ToInt32((decimal)item.QuantityOnHand);

				MyobLocStock locStock = new MyobLocStock();
				locStock.lstItemLocationID = item.ItemLocationID;
				locStock.lstLocationID = item.LocationID;
				locStock.lstItemID = item.ItemID;
				locStock.lstItemCode = item.ItemNumber;
				locStock.lstStockLoc = item.LocationName;
				locStock.lstQuantityAvailable = qtyav;
				locStock.lstSellOnOrder = sellorder;
				locStock.lstPurchaseOnOrder = purchaseorder;
				locStock.lstCreateTime = dateTime;
				locStock.lstModifyTime = dateTime;
				locStock.AccountProfileId = apId;
				stocks.Add(locStock);
			}

			using var context = new MMDbContext();
			#region remove current records first:            
			var _stocks = context.MyobLocStocks.Where(x => x.AccountProfileId == apId).ToList();
			if (_stocks.Count > 0)
			{
				context.MyobLocStocks.RemoveRange(_stocks);
				context.SaveChanges();
			}
			#endregion
			#region add records:          
			context.MyobLocStocks.AddRange(stocks);
			context.SaveChanges();
			#endregion
		}

		public static void SaveItemsToDB(int apId, List<ARItem> inventories)
		{
			DateTime dateTime = DateTime.Now;
			List<MyobItem> items = new List<MyobItem>();
			foreach (var item in inventories)
			{
				MyobItem _item = new MyobItem();
				_item.itmIsActive = !item.IsInactive;
				_item.itmName = item.ItemName;
				_item.itmCode = item.ItemNumber;
				_item.itmDesc = item.ItemDesc;
				_item.itmUseDesc = (bool)item.itmUseDesc;
				_item.itmBaseSellingPrice = (decimal)item.BaseSellingPrice;
				_item.itmSellUnit = item.SellUnitMeasure;
				_item.itmSellUnitQuantity = item.SellUnitQuantity;
				_item.itmBuyUnit = item.BuyUnitMeasure;
				_item.itmLastUnitPrice = (decimal)item.LastUnitPrice;
				_item.itmChgCtrl = item.ChangeControl;
				_item.itmCreateTime = dateTime;
				_item.itmModifyTime = dateTime;
				_item.itmItemID = item.ItemID;
				_item.AccountProfileId = apId;
				_item.itmIsNonStock = (bool)item.itmIsNonStock;
				_item.itmIsSold = (bool)item.itmIsSold;
				_item.itmIsBought = (bool)item.itmIsBought;
				_item.AccountProfileId = apId;
				_item.itmCreateTime = dateTime;
				_item.itmModifyTime = dateTime;
				items.Add(_item);
			}

			using var context = new MMDbContext();
			#region remove current records first:
			var _items = context.MyobItems.Where(x => x.AccountProfileId == apId).ToList();
			if (_items.Count > 0)
			{
				context.MyobItems.RemoveRange(_items);
				context.SaveChanges();
			}
			#endregion
			#region add records:
			context.MyobItems.AddRange(items);
			context.SaveChanges();
			#endregion
		}

		public static List<AddOnItem> GetStockList(int apId)
		{			
			AbssConn abssConn = ModelHelper.GetCompanyProfiles(apId);			
			return MYOBHelper.GetStockList(abssConn);
		}

	}
}
