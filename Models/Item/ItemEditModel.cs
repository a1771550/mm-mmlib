using System;
using System.Collections.Generic;
using System.Linq;
using MMDAL;
using MMLib.Models.MYOB;
using System.Configuration;
using Dapper;
using ModelHelper = MMLib.Helpers.ModelHelper;
using Microsoft.Data.SqlClient;
using CommonLib.Models;
using MMLib.Models.Purchase;
using System.Text.Json;
using MMLib.Helpers;

namespace MMLib.Models.Item
{
	public class ItemEditModel : PagingBaseModel
	{
		public bool EditItem { get; set; } = false;
		public bool EditItemVari { get; set; } = false;
		public HashSet<ItemCategoryModel> CategoryList;
		public int TotalBaseStockQty { get; set; } = 0;
		public Dictionary<string, int> DicIVLocQty { get; set; } = new Dictionary<string, int>();
		public string JsonDicIVLocQty { get { return DicIVLocQty != null ? System.Text.Json.JsonSerializer.Serialize(DicIVLocQty) : ""; } }
		public ItemVariModel ItemVari { get; set; }
		public string JsonItemVari { get { return ItemVari != null ? JsonSerializer.Serialize(ItemVari) : ""; } }
		public List<ItemAttribute> ItemAttrList { get; set; } = new List<ItemAttribute>();
		public string JsonItemAttrList { get { return ItemAttrList != null && ItemAttrList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(ItemAttrList) : string.Empty; } }
		public List<ItemVariModel> SelectedIVList { get; set; }
		public string JsonSelectedIVList { get { return SelectedIVList != null && SelectedIVList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(SelectedIVList) : string.Empty; } }
		public List<ItemVariModel> ItemVariations { get; set; }
		public string JsonItemVariations { get { return ItemVariations != null && ItemVariations.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(ItemVariations) : string.Empty; } }
		public bool EnableBuySellUnits { get; set; }
		public List<ItemModel> Stocks { get; set; }
		public List<ItemView> Items { get; set; }
		public List<string> Locations; public List<string> LocationNames;
		public Dictionary<string, int> DicLocQty { get; set; }
		public int MaxNameLength { get { return int.Parse(ConfigurationManager.AppSettings["MaxItemNameLength"]); } }
		public int MaxCodeLength { get { return int.Parse(ConfigurationManager.AppSettings["MaxItemCodeLength"]); } }
		//public int AccountProfileId { getPG; set; }
		public Dictionary<ItemView, Dictionary<string, int>> DicItemQty { get; set; }
		public PagedList.IPagedList<ItemView> ItemList { get; set; }

		public List<AccountModel> AccountList { get; set; }
		public List<AccountModel> ACList { get; set; }

		public Dictionary<string, List<AccountModel>> DicAcAccounts { get; set; }
		public string JsonDicAcAccounts { get { return JsonSerializer.Serialize(DicAcAccounts); } }
		public ItemModel Item;
		public List<BatchQty> BatchQtyList;

		public Dictionary<string, List<string>> DicBatVtList;
		public List<string> SnosList;
		public Dictionary<string, List<Purchase.BatSnVt>> DicBatSnVtList;
		public List<BatSnVt> BatSnVtList;
		public List<SnVt> SnVtList;
		public List<VtQty> VtQtyList;
		public List<VtDelQty> VtDelQtyList;
		public ItemOptions ItemOptions;
		public List<PoBatVQ> PoBatVQList;

		public string JsonItem { get { return JsonSerializer.Serialize(Item); } }
		public List<string> ItemCodeList { get; set; }
		public List<string> ItemSupCodeList { get; set; }
		public MMDbContext context { get; set; }
		public Session session { get; set; }
		public Dictionary<string, int> DicBatTotalQty;
		//private Dictionary<string, List<PoItemVariModel>> DicBatIvList;
		//private Dictionary<string, List<ItemVariDelModel>> DicBatIvDelList;
		public Dictionary<string, int> DicIvTotalQty;
		public List<IvQty> IvQtyList;
		public List<IvDelQty> IvDelQtyList;
		public Dictionary<string, List<PoItemVariModel>> DicIvInfo;

		public string JsonDicBatTotalQty { get { return DicBatTotalQty != null ? JsonSerializer.Serialize(DicBatTotalQty) : ""; } }
		public string JsonDicIvTotalQty { get { return DicIvTotalQty != null ? JsonSerializer.Serialize(DicIvTotalQty) : ""; } }
		public string PrimaryLocation { get; set; }
		public List<PoItemVariModel> PoIvInfo { get; set; }

		public ItemEditModel()
		{
			Items = new List<ItemView>();
			Stocks = new List<ItemModel>();
			Locations = new List<string>();
			DicLocQty = new Dictionary<string, int>();
			Item = new ItemModel();
			ItemCodeList = new List<string>();
			ItemSupCodeList = new List<string>();
			AccountList = new List<AccountModel>();
			ACList = new List<AccountModel>();
			DicAcAccounts = new Dictionary<string, List<AccountModel>>();
			DicItemQty = new Dictionary<ItemView, Dictionary<string, int>>();

			using (context = new MMDbContext())
			{
				var acIdList = context.AccountClassifications.Where(x => x.AccountProfileId == AccountProfileId).Select(x => x.AccountClassificationID).Distinct().ToList();
				foreach (var ac in acIdList)
				{
					DicAcAccounts[ac] = new List<AccountModel>();
				}

				ACList = (from a in context.Accounts
						  join ac in context.AccountClassifications
						  on a.AccountClassificationID equals ac.AccountClassificationID
						  where a.AccountProfileId == AccountProfileId && ac.AccountProfileId == AccountProfileId
						  select new AccountModel
						  {
							  AccountClassificationID = ac.AccountClassificationID,
							  ACDescription = ac.Description
						  }
								).Distinct().ToList();
				AccountList = (from a in context.Accounts
							   join ac in context.AccountClassifications
							   on a.AccountClassificationID equals ac.AccountClassificationID
							   where a.AccountProfileId == AccountProfileId && !a.AccountNumber.EndsWith("0000") && ac.AccountProfileId == AccountProfileId
							   && a.AccountTypeID != "H"
							   select new AccountModel
							   {
								   AccountID = a.AccountID,
								   AccountName = a.AccountName,
								   AccountNumber = a.AccountNumber,
								   AccountClassificationID = a.AccountClassificationID,
								   AccountProfileId = a.AccountProfileId,
								   ACDescription = ac.Description
							   }
							   ).ToList();

				foreach (var key in DicAcAccounts.Keys)
				{
					foreach (var account in AccountList)
					{
						if (account.AccountClassificationID == key)
						{
							DicAcAccounts[key].Add(account);
						}
					}
				}

				var mitemcodes = context.MyobItems.Where(x => x.AccountProfileId == AccountProfileId).Select(x => x.itmCode).Distinct().ToList();
				var msupcodes = context.MyobItems.Where(x => x.AccountProfileId == AccountProfileId && !string.IsNullOrEmpty(x.itmSupCode)).Select(x => x.itmSupCode).Distinct().ToList();

				ItemCodeList = mitemcodes.Concat(mitemcodes).ToList();
				ItemSupCodeList = msupcodes.Concat(msupcodes).ToList();

				EnableBuySellUnits = context.AppParams.FirstOrDefault(x => x.appParam == "EnableBuySellUnits").appVal == "1";
				using var connection = new SqlConnection(DefaultConnection);
				connection.Open();
				ModelHelper.GetShops(connection, ref Locations, ref LocationNames, apId);
			}
		}

		public ItemEditModel(int itemId, bool editItem, bool hasItemOptions, bool hasIvOnly, string location, int qty) : this(itemId, editItem)
		{
			EditItem = editItem;
			Get(itemId, hasItemOptions, hasIvOnly, location, qty);
		}

		public ItemEditModel(int itemId, bool editItem) : this()
		{
			EditItem = editItem;
			Get(itemId);
		}
		public static void Edit(ItemModel model)
		{
			using (var context = new MMDbContext())
			{
				using (var transaction = context.Database.BeginTransaction())
				{
					try
					{
						Session session = ModelHelper.GetCurrentSession(context);
						model.SelectedLocation = session.sesShop;
						model.AccountProfileId = session.AccountProfileId;

						MyobItem item = context.MyobItems.FirstOrDefault(x => x.itmItemID == model.itmItemID && x.AccountProfileId == model.AccountProfileId);
						DateTime dateTime = DateTime.Now;
						if (item != null)
						{
							item.itmIsActive = true;
							item.itmCode = model.itmCode;
							item.itmName = model.itmName;
							item.itmDesc = model.itmDesc;
							item.itmBaseSellingPrice = model.itmBaseSellingPrice;
							item.itmBuyStdCost = model.itmBuyStdCost;
							item.itmModifyTime = dateTime;
							item.itmIsNonStock = model.itmIsNonStock;
							item.itmIsBought = model.itmIsBought;
							item.itmIsSold = model.itmIsSold;
							item.itmSupCode = model.itmSupCode;
							item.IncomeAccountID = model.IncomeAccountID;
							item.ExpenseAccountID = model.ExpenseAccountID;
							item.InventoryAccountID = model.InventoryAccountID;
							item.itmBuyUnit = model.itmBuyUnit;
							item.itmSellUnit = model.itmSellUnit;
							item.itmSellUnitQuantity = model.itmSellUnitQuantity;
							item.itmLastUnitPrice = model.itmLastUnitPrice;
							item.itmUseDesc = model.itmUseDesc;
							item.itmCheckout = false;

							#region Handle ItemOptions
							var itemoption = context.ItemOptions.FirstOrDefault(x => x.itemId == model.itmItemID);
							if (itemoption != null)
							{
								itemoption.chkBat = model.chkBat;
								itemoption.chkSN = model.chkSN;
								itemoption.chkVT = model.chkVT;
								itemoption.ModifyTime = dateTime;
								context.SaveChanges();
							}
							#endregion

							if (model.catId != null && model.catId > 0)
							{
								ModelHelper.SaveItemCategory(context, item.itmCode, (int)model.catId);
							}

							List<MyobItemPrice> itemPrices = context.MyobItemPrices.Where(x => x.ItemID == item.itmItemID && x.AccountProfileId == comInfo.AccountProfileId).ToList();
							if (itemPrices != null && itemPrices.Count > 0)
							{
								foreach (var itemPrice in itemPrices)
								{
									if (itemPrice.PriceLevelNameID == "PLA")
									{
										itemPrice.SellingPrice = model.PLA;
									}
									if (itemPrice.PriceLevelNameID == "PLB")
									{
										itemPrice.SellingPrice = model.PLB;
									}
									if (itemPrice.PriceLevelNameID == "PLC")
									{
										itemPrice.SellingPrice = model.PLC;
									}
									if (itemPrice.PriceLevelNameID == "PLD")
									{
										itemPrice.SellingPrice = model.PLD;
									}
									if (itemPrice.PriceLevelNameID == "PLE")
									{
										itemPrice.SellingPrice = model.PLE;
									}
									if (itemPrice.PriceLevelNameID == "PLF")
									{
										itemPrice.SellingPrice = model.PLF;
									}
									itemPrice.ModifyTime = dateTime;
								}
							}

							context.SaveChanges();

							if (model.SelectedAttrList4V.Count > 0)
							{
								ModelHelper.SaveItemVariations(context, model);
							}
							else if (model.AttrList.Count > 0)
							{
								ModelHelper.SaveItemAttributes(context, model.AttrList, item);
							}
						}
						transaction.Commit();
					}
					catch (Exception ex)
					{
						transaction.Rollback();
						throw new Exception(ex.Message);
					}
				}
			}
		}

		public static void EditIV(ItemModel Item = null, ItemVariModel ItemVari = null, List<ItemAttributeModel> AttrList = null)
		{
			ModelHelper.EditIV(ItemVari, AttrList, Item);
		}

		private void initDictionaries()
		{
			BatchQtyList = new List<BatchQty>();
			DicBatVtList = new Dictionary<string, List<string>>();

			SnosList = new List<string>();

			DicBatSnVtList = new Dictionary<string, List<Purchase.BatSnVt>>();
			BatSnVtList = new List<BatSnVt>();

			SnVtList = new List<SnVt>();

			VtQtyList = new List<VtQty>();
			VtDelQtyList = new List<VtDelQty>();

			ItemOptions = new ItemOptions();
			PoBatVQList = new List<PoBatVQ>();
		}
		public void Get(int itemId, bool hasItemOptions = false, bool hasIvOnly = false, string location = "", int qty = 0)
		{
			//Shops = comInfo.Shops.Split(',');           
			using var connection = new SqlConnection(DefaultConnection);
			connection.Open();
			PrimaryLocation = ModelHelper.GetShops(connection, ref Shops, ref ShopNames, apId);

			if (itemId > 0)
			{
				Item = connection.QueryFirstOrDefault<ItemModel>(@"EXEC dbo.GetItemById @apId=@apId,@itemId=@itemId,@isAbss=@isAbss", new { apId, itemId, isAbss = true });
				string ItemCode = "";

				if (Item != null)
				{
					ItemCode = Item.itmCode;

					var context = new MMDbContext();

				

					ItemAttrList = context.ItemAttributes.Where(x => x.itmCode == Item.itmCode && x.AccountProfileId == AccountProfileId).ToList();
					if (ItemAttrList.Count > 0)
					{
						var attrIds = string.Join(",", ItemAttrList.Select(x => x.Id).ToList());

						PopulateMyobItem(context, Item);

						
					}
					else
					{
						PopulateMyobItem(context, Item);

						Item.AttrList = ModelHelper.GetItemAttrList(context, Item.itmCode);
					}

					

					#region HasItemOption or HasIvOnly
					if (hasItemOptions || hasIvOnly)
					{
						DicIvInfo = new Dictionary<string, List<PoItemVariModel>>();

						IvQtyList = new List<IvQty>();
						IvDelQtyList = new List<IvDelQty>();

						initDictionaries();
						//List<string> batchcodelist, batpocodeList, vtpocodeList, ivIdlistList;
						//List<GetBatchVtInfoByItemCodes12_Result> itembtInfo;
						//List<GetValidThruInfo12_Result> itemvtInfo;
						//List<GetSerialInfo5_Result> serialInfo;
						
						HashSet<string> itemcodelist = new HashSet<string>
						{
							Item.itmCode
						};
						ItemOptions itemOptions = new ItemOptions
						{
							ItemCode = Item.itmCode,
						};
						//List<GetItemVariInfo4_Result> ivInfo;
					
						//ModelHelper.Prepare4ItemOptionsInfo(context, location, connection, itemcodelist, out batchcodelist, out itembtInfo, out itemvtInfo, out serialInfo, out batdelInfo, out vtdelInfo, out batpocodeList, out vtpocodeList, ref itemOptions, ref DicBatTotalQty, out ivInfo, out ivdelInfo, ref DicIvTotalQty, out ivIdlistList);
						//foreach (var batchcode in batchcodelist)
						//{
						//	DicBatVtList[batchcode] = new List<string>();
						//	DicBatSnVtList[batchcode] = new List<Purchase.BatSnVt>();
						//}

						//ModelHelper.SetDictionaries(batchcodelist, batpocodeList, vtpocodeList, itembtInfo, itemvtInfo, serialInfo, batdelInfo, vtdelInfo, ivInfo, ivdelInfo, ivIdlistList, ref DicBatVtList, ref PoBatVQList, ref BatDelQtyList, ref BatchQtyList, ref VtDelQtyList, ref VtQtyList, ref SnosList, ref DicBatSnVtList, ref BatSnVtList, ref SnVtList, ref IvQtyList, ref IvDelQtyList);

						Item.QuantityAvailable = qty;
						Item.ItemOptions = itemOptions;

						DicIvInfo[ItemCode] = new List<PoItemVariModel>();
						PoIvInfo = connection.Query<PoItemVariModel>(@"EXEC dbo.GetPoItemVariInfo @apId=@apId,@itemcodes=@itemcodes", new { apId, itemcodes = ItemCode }).ToList();
						ModelHelper.GetDicIvInfo(context, PoIvInfo, ref DicIvInfo);
					}
					#endregion


				}
			}
			CategoryList = new HashSet<ItemCategoryModel>();
			ModelHelper.GetCategoryList(connection, ref CategoryList);
		}

		private void PopulateMyobItem(MMDbContext context, ItemModel item)
		{
			ModelHelper.GetMyobItemPrices(context, item.itmCode, ref Item);
			ModelHelper.GetMyobItemCategory(context, item.itmCode, ref Item);
		}

		public static string Delete(int itemId)
		{
			string msg = CommonLib.App_GlobalResources.Resource.ItemSaved;
			using (var context = new MMDbContext())
			{
				List<MyobItemPrice> itemPrices = context.MyobItemPrices.Where(x => x.ItemID == itemId).ToList();
				if (itemPrices != null)
				{
					context.MyobItemPrices.RemoveRange(itemPrices);
				}
				MyobLocStock locStock = context.MyobLocStocks.FirstOrDefault(x => x.lstItemID == itemId);
				if (locStock != null)
				{
					context.MyobLocStocks.Remove(locStock);
				}
				MyobItem item = context.MyobItems.FirstOrDefault(x => x.itmItemID == itemId);
				if (item != null)
				{
					context.MyobItems.Remove(item);
				}
				context.SaveChanges();
			}
			return msg;
		}

		public static void UpdateBuySellUnit(ItemModel item, MMDbContext context = null)
		{
			if (context == null)
			{
				using (context = new MMDbContext())
				{
					updateBuySellUnit(item, context);
				}
			}
			else
			{
				updateBuySellUnit(item, context);
			}
		}
		private static void updateBuySellUnit(ItemModel item, MMDbContext context)
		{
			var myobitem = context.MyobItems.FirstOrDefault(x => x.itmCode == item.itmCode && x.AccountProfileId == comInfo.AccountProfileId);
			myobitem.itmBuyUnit = item.itmBuyUnit;
			myobitem.itmSellUnit = item.itmSellUnit;
			myobitem.itmSellUnitQuantity = item.itmSellUnitQuantity;
			myobitem.itmModifyTime = DateTime.Now;

			context.SaveChanges();
		}

		public static void SaveItemOptions(List<ItemOptionsModel> model)
		{
			using var context = new MMDbContext();

			#region remove current data first
			var itemIds = model.Select(x => x.itemId).Distinct().ToList();
			var currentIPs = context.ItemOptions.Where(x => itemIds.Contains(x.itemId));
			if (currentIPs != null)
			{
				context.ItemOptions.RemoveRange(currentIPs);
				context.SaveChanges();
			}
			#endregion

			#region add data
			var items = context.GetItemCodesByIds(comInfo.AccountProfileId, string.Join(",", itemIds)).ToList();
			List<ItemOption> itemOptions = new List<ItemOption>();
			DateTime dateTime = DateTime.Now;
			foreach (var ip in model)
			{
				var item = items.FirstOrDefault(x => x.itmItemID == ip.itemId);
				itemOptions.Add(new ItemOption
				{
					itemId = ip.itemId,
					chkBat = ip.ChkBatch,
					chkSN = ip.ChkSN,
					chkVT = ip.WillExpire,
					itmCode = item == null ? "" : item.itmCode,
					CreateTime = dateTime,
					AccountProfileId = comInfo.AccountProfileId
				});
			}
			context.ItemOptions.AddRange(itemOptions);
			context.SaveChanges();
			#endregion
		}
	}
}
