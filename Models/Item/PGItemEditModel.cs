using System;
using System.Collections.Generic;
using System.Linq;
using MMDAL;
using MMCommonLib.BaseModels;
using System.Configuration;
using ModelHelper = MMLib.Helpers.ModelHelper;
using MMLib.Models.MYOB;
using Dapper;
using System.ComponentModel.Design;
using CommonLib.Helpers;
using System.Web;

namespace MMLib.Models.Item
{
    public class PGItemEditModel : PagingBaseModel
    {
        public bool EditItem { get; set; } = false;
        public HashSet<ItemCategoryModel> CategoryList;
        public int TotalBaseStockQty { get; set; } = 0;
        public Dictionary<string, int> DicIVLocQty { get; set; } = new Dictionary<string, int>();
        public string JsonDicIVLocQty { get { return DicIVLocQty != null ? System.Text.Json.JsonSerializer.Serialize(DicIVLocQty) : ""; } }
        public ItemVariModel ItemVari { get; set; }
        public string JsonItemVari { get { return ItemVari != null ? Newtonsoft.Json.JsonConvert.SerializeObject(ItemVari) : ""; } }
        public List<ItemAttribute> ItemAttrList { get; set; } = new List<ItemAttribute>();
        public string JsonItemAttrList { get { return ItemAttrList != null && ItemAttrList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(ItemAttrList) : string.Empty; } }
        public List<ItemVariModel> SelectedIVList { get; set; }
        public string JsonSelectedIVList { get { return SelectedIVList != null && SelectedIVList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(SelectedIVList) : string.Empty; } }
        public List<ItemVariModel> ItemVariations { get; set; }
        public string JsonItemVariations { get { return ItemVariations != null && ItemVariations.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(ItemVariations) : string.Empty; } }
        public bool EnableBuySellUnits { get; set; }
        public List<PGItemModel> Stocks { get; set; }
        public List<ItemView> Items { get; set; }
        public List<string> Locations; public List<string> LocationNames;

        public int MaxNameLength { get { return int.Parse(ConfigurationManager.AppSettings["MaxItemNameLength"]); } }
        public int MaxCodeLength { get { return int.Parse(ConfigurationManager.AppSettings["MaxItemCodeLength"]); } }
        //public int AccountProfileId { getPG; set; }
        public Dictionary<ItemView, Dictionary<string, int>> DicItemQty { get; set; }
        public PagedList.IPagedList<ItemView> ItemList { get; set; }

        public List<AccountModel> AccountList { get; set; }
        public List<AccountModel> ACList { get; set; }

        public Dictionary<string, List<AccountModel>> DicAcAccounts { get; set; }
        public string JsonDicAcAccounts { get { return Newtonsoft.Json.JsonConvert.SerializeObject(DicAcAccounts); } }
        public List<string> ItemCodeList { get; set; }
        public List<string> ItemSupCodeList { get; set; }
        public PGItemEditModel()
        {
            Items = new List<ItemView>();
            Stocks = new List<PGItemModel>();
            Locations = new List<string>();
            Item = new PGItemModel();
            ItemCodeList = new List<string>();
            ItemSupCodeList = new List<string>();
            AccountList = new List<AccountModel>();
            ACList = new List<AccountModel>();
            DicAcAccounts = new Dictionary<string, List<AccountModel>>();
            DicItemQty = new Dictionary<ItemView, Dictionary<string, int>>();

            using var context = new MMDbContext();

            if (!NonABSS)
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
            }


            var mitemcodes = context.MyobItems.Where(x => x.AccountProfileId == AccountProfileId).Select(x => x.itmCode).Distinct().ToList();
            var msupcodes = context.MyobItems.Where(x => x.AccountProfileId == AccountProfileId && !string.IsNullOrEmpty(x.itmSupCode)).Select(x => x.itmSupCode).Distinct().ToList();
            var gitemcodes = context.PGItems.Where(x => x.AccountProfileId == AccountProfileId).Select(x => x.itmCode).Distinct().ToList();
            var gsupcodes = context.PGItems.Where(x => x.AccountProfileId == AccountProfileId && !string.IsNullOrEmpty(x.itmSupCode)).Select(x => x.itmSupCode).Distinct().ToList();

            ItemCodeList = NonAbss ? gitemcodes : mitemcodes.Concat(gitemcodes).ToList();
            ItemSupCodeList = NonAbss ? gsupcodes : msupcodes.Concat(gsupcodes).ToList();

            EnableBuySellUnits = context.AppParams.FirstOrDefault(x => x.appParam == "EnableBuySellUnits").appVal == "1";           
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
            connection.Open();
            ModelHelper.GetShops(connection, ref Locations, ref LocationNames, apId);
        }
        public PGItemEditModel(int itemId, bool editItem) : this()
        {
            EditItem = editItem;
            Get(itemId);
        }
        public PGItemModel Item;
        public string JsonItem { get { return Item != null ? Newtonsoft.Json.JsonConvert.SerializeObject(Item) : ""; } }
        public List<PGItemModel> PGItemList { get; set; }
        public PagedList.IPagedList<PGItemModel> PagingPGItemList { get; set; }


        public List<PGItemModel> GetList(string keyword = null)
        {
            List<PGItemModel> pgitemlist = new List<PGItemModel>();
            using var context = new MMDbContext();            
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
            connection.Open();
            var itemlist = connection.Query<PGItemModel>(@"EXEC dbo.GetPGItemList9 @apId=@apId,@keyword=@keyword", new { apId, keyword }).ToList();
            var shops = ComInfo.Shops.Split(',');

            if (itemlist.Count > 0)
            {
                foreach (var item in itemlist)
                {
                    var Item = new PGItemModel();

                    Item.itmItemID = item.itmItemID;
                    Item.itmIsActive = true;
                    Item.chkBat = item.chkBat;
                    Item.chkSN = item.chkSN;
                    Item.chkVT = item.chkVT;
                    Item.itmCode = item.itmCode;
                    Item.itmName = item.itmName;
                    Item.itmDesc = item.itmDesc;
                    Item.itmBaseSellingPrice = item.itmBaseSellingPrice;
                    Item.itmLastUnitPrice = item.itmLastUnitPrice;
                    Item.itmCreateTime = item.itmCreateTime;
                    Item.itmModifyTime = item.itmModifyTime;
                    Item.itmIsNonStock = item.itmIsNonStock;
                    Item.itmIsBought = item.itmIsBought;
                    Item.itmIsSold = item.itmIsSold;
                    Item.itmUseDesc = item.itmUseDesc;
                    Item.itmSupCode = item.itmSupCode;
                    Item.IncomeAccountID = item.IncomeAccountID;
                    Item.ExpenseAccountID = item.ExpenseAccountID;
                    Item.InventoryAccountID = item.InventoryAccountID;
                    Item.itmBuyUnit = item.itmBuyUnit;
                    Item.itmSellUnit = item.itmSellUnit;
                    Item.itmSellUnitQuantity = item.itmSellUnitQuantity;
                    Item.chkBat = item.chkBat;
                    Item.chkSN = item.chkSN;
                    Item.chkVT = item.chkVT;

                    if (Item.IncomeAccountID > 0)
                    {
                        Item.DicItemAccounts[Item.IncomeAccountID.ToString()] = ModelHelper.GetAccountNumber(context, (int)Item.IncomeAccountID, Item.AccountProfileId);
                    }
                    else
                    {
                        Item.DicItemAccounts["-1"] = "N/A";
                    }
                    Item.ExpenseAccountID = item.ExpenseAccountID;
                    if (Item.ExpenseAccountID > 0)
                    {
                        Item.DicItemAccounts[Item.ExpenseAccountID.ToString()] = ModelHelper.GetAccountNumber(context, (int)Item.ExpenseAccountID, Item.AccountProfileId);
                    }
                    else
                    {
                        Item.DicItemAccounts["-2"] = "N/A";
                    }
                    Item.InventoryAccountID = item.InventoryAccountID;
                    if (Item.InventoryAccountID > 0)
                    {
                        Item.DicItemAccounts[Item.InventoryAccountID.ToString()] = ModelHelper.GetAccountNumber(context, (int)Item.InventoryAccountID, Item.AccountProfileId);
                    }
                    else
                    {
                        Item.DicItemAccounts["-3"] = "N/A";
                    }

                    List<PGItemPrice> itemPrices = context.PGItemPrices.Where(x => x.ItemID == item.itmItemID).ToList();
                    if (itemPrices != null && itemPrices.Count > 0)
                    {
                        foreach (var itemprice in itemPrices)
                        {
                            if (Item.itmItemID == itemprice.ItemID)
                            {
                                if (itemprice.PriceLevel == "A" && itemprice.QuantityBreak == 1)
                                {
                                    Item.PLA = itemprice.SellingPrice;
                                    Item.PLs["PLA"] = itemprice.SellingPrice;
                                }
                                if (itemprice.PriceLevel == "B" && itemprice.QuantityBreak == 1)
                                {
                                    Item.PLB = itemprice.SellingPrice;
                                    Item.PLs["PLB"] = itemprice.SellingPrice;
                                }
                                if (itemprice.PriceLevel == "C" && itemprice.QuantityBreak == 1)
                                {
                                    Item.PLC = itemprice.SellingPrice;
                                    Item.PLs["PLC"] = itemprice.SellingPrice;
                                }
                                if (itemprice.PriceLevel == "D" && itemprice.QuantityBreak == 1)
                                {
                                    Item.PLD = itemprice.SellingPrice;
                                    Item.PLs["PLD"] = itemprice.SellingPrice;
                                }
                                if (itemprice.PriceLevel == "E" && itemprice.QuantityBreak == 1)
                                {
                                    Item.PLE = itemprice.SellingPrice;
                                    Item.PLs["PLE"] = itemprice.SellingPrice;
                                }
                                if (itemprice.PriceLevel == "F" && itemprice.QuantityBreak == 1)
                                {
                                    Item.PLF = itemprice.SellingPrice;
                                    Item.PLs["PLF"] = itemprice.SellingPrice;
                                }
                            }
                        }
                    }

                    pgitemlist.Add(Item);
                }
            }

            return pgitemlist;
        }

        public static void Add(PGItemModel model)
        {
            using (var context = new MMDbContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        int apId = comInfo.AccountProfileId;
                        model.SelectedLocation = comInfo.Shop;
                        model.AccountProfileId = apId;
                        DateTime dateTime = DateTime.Now;
                        int latestItemId = context.GetLatestPGItemId2(apId).FirstOrDefault() == null ? 0 : (int)context.GetLatestPGItemId2(apId).FirstOrDefault();
                        latestItemId++;

                        PGItem item = new PGItem
                        {
                            itmItemID = latestItemId,
                            AccountProfileId = apId,
                            itmCode = model.itmCode,
                            itmName = model.itmName,
                            itmDesc = model.itmDesc,
                            itmBaseSellingPrice = (decimal?)model.itmBaseSellingPrice,
                            itmBuyStdCost = model.itmBuyStdCost,
                            itmCreateTime = dateTime,
                            itmModifyTime = dateTime,
                            itmIsNonStock = model.itmIsNonStock,
                            itmIsBought = model.itmIsBought,
                            itmIsSold = model.itmIsSold,
                            itmUseDesc = model.itmUseDesc,
                            itmSupCode = model.itmSupCode,
                            IncomeAccountID = model.IncomeAccountID,
                            ExpenseAccountID = model.ExpenseAccountID,
                            InventoryAccountID = model.InventoryAccountID,

                            itmBuyUnit = model.itmBuyUnit,
                            itmSellUnit = model.itmSellUnit,
                            itmSellUnitQuantity = model.itmSellUnitQuantity == 0 ? 1 : model.itmSellUnitQuantity,
                            itmCheckout = NonAbss,
                            itmIsActive = true
                        };
                        context.PGItems.Add(item);
                        context.SaveChanges();

                        if (model.catId != null && model.catId > 0)
                        {
                            ModelHelper.SaveItemCategory(context, item.itmCode, (int)model.catId);
                        }

                        model.PLs = new Dictionary<string, decimal?>
                        {
                            { "PLA", model.PLA },
                            { "PLB", model.PLB },
                            { "PLC", model.PLC },
                            { "PLD", model.PLD },
                            { "PLE", model.PLE },
                            { "PLF", model.PLF }
                        };

                        List<PGItemPrice> itemPrices = new List<PGItemPrice>();

                        foreach (var pl in model.PLs)
                        {
                            string strpl = pl.Key.Last().ToString();
                            PGItemPrice itemPrice = new PGItemPrice
                            {
                                ItemID = latestItemId,
                                PriceLevelNameID = pl.Key,
                                PriceLevel = strpl,
                                SellingPrice = pl.Value,
                                ItemCode = model.itmCode,
                                CreateTime = dateTime,
                                ModifyTime = dateTime,
                                QuantityBreak = 1,
                                AccountProfileId = apId,
                            };
                            itemPrices.Add(itemPrice);
                        }
                        context.PGItemPrices.AddRange(itemPrices);
                        context.SaveChanges();

                        ItemCategory category = new ItemCategory
                        {
                            itmCode = model.itmCode,
                            AccountProfileId = apId,
                            catId = (int)model.catId,
                            CreateTime = DateTime.Now,
                        };
                        context.ItemCategories.Add(category);
                        context.SaveChanges();
                       
                        transaction.Commit();
                        //return latestItemId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception(ex.Message);
                    }
                }

            }
        }

        public static void Edit(PGItemModel model)
        {
            using (var context = new MMDbContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {

                    //editpgitem includes checking if model.AttrList.Count>0
                    ModelHelper.EditPGItem(model, context);


                    try
                    {
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


        public void Get(int itemId)
        {            
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
            connection.Open();
            ModelHelper.GetShops(connection, ref Shops, ref ShopNames, apId);

            using var context = new MMDbContext();

            if (itemId > 0)
            {
                Item = connection.QueryFirstOrDefault<PGItemModel>(@"EXEC dbo.GetItemById @apId=@apId,@itemId=@itemId,@isAbss=@isAbss", new { apId = AccountProfileId, itemId, isAbss = false });

                if (Item != null)
                {
                    var salesitemcodes = context.RtlSalesLns.Select(x => x.rtlItemCode).ToList();
                    PopulatePGItem(context, Item);
                }
            }
            else
            {
                PopulatePGItem(context, Item);
                Item.AttrList = ModelHelper.GetItemAttrList(context, Item.itmCode);
            }


            CategoryList = new HashSet<ItemCategoryModel>();
            ModelHelper.GetCategoryList(connection, ref CategoryList);

            //if (EditItem) ItemVari = null;
            //else Item = null;
            //if (ItemVari != null) Item = null;
        }

        private void PopulatePGItem(MMDbContext context, PGItemModel item)
        {
            //ModelHelper.GetLocStock4PGItem(context, item.itmCode, ref Item);
            ModelHelper.GetPGItemPrices(context, item.itmCode, ref Item);
            ModelHelper.GetPGItemCategory(context, item.itmCode, ref Item);
        }


        public static string Delete(int itemId)
        {
            string msg = CommonLib.App_GlobalResources.Resource.ItemSaved;
            using (var context = new MMDbContext())
            {
                if (context.CheckIfItemHasTransaction2(itemId, comInfo.AccountProfileId).FirstOrDefault().Value > 0)
                {
                    msg = CommonLib.App_GlobalResources.Resource.ItemTransactedNotDeletable;
                    return msg;
                }

                List<PGItemPrice> itemPrices = context.PGItemPrices.Where(x => x.ItemID == itemId).ToList();
                if (itemPrices != null)
                {
                    context.PGItemPrices.RemoveRange(itemPrices);
                }
           
                PGItem item = context.PGItems.FirstOrDefault(x => x.itmItemID == itemId);
                if (item != null)
                {
                    context.PGItems.Remove(item);
                }
                context.SaveChanges();
            }
            return msg;
        }

        public static void UpdateBuySellUnit(PGItemModel item, MMDbContext context = null)
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

        private static void updateBuySellUnit(PGItemModel item, MMDbContext context)
        {
            if (NonAbss)
            {
                var pgitem = context.PGItems.FirstOrDefault(x => x.itmCode == item.itmCode && x.AccountProfileId == comInfo.AccountProfileId);
                pgitem.itmBuyUnit = item.itmBuyUnit;
                pgitem.itmSellUnit = item.itmSellUnit;
                pgitem.itmSellUnitQuantity = item.itmSellUnitQuantity;
                pgitem.itmModifyTime = DateTime.Now;
            }
            else
            {
                var myobitem = context.MyobItems.FirstOrDefault(x => x.itmCode == item.itmCode && x.AccountProfileId == comInfo.AccountProfileId);
                myobitem.itmBuyUnit = item.itmBuyUnit;
                myobitem.itmSellUnit = item.itmSellUnit;
                myobitem.itmSellUnitQuantity = item.itmSellUnitQuantity;
                myobitem.itmModifyTime = DateTime.Now;
            }
            context.SaveChanges();
        }
    }
}
