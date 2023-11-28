using System;
using System.Collections.Generic;
using System.Linq;
using MMCommonLib.BaseModels;
using System.Configuration;
using Dapper;
using PagedList;
using CommonLib.Helpers;
using MMDAL;
using Microsoft.Data.SqlClient;
using System.Web;

namespace MMLib.Models.Item
{
    public class ItemPeriodPromotionEditModel : PagingBaseModel
    {
        public long proId { get; set; } = 0;       

        public Dictionary<string, List<ItemPeriodPromotionModel>> DicPromotionItems { get; set; } = new Dictionary<string, List<ItemPeriodPromotionModel>>();
        public string JsonDicPromotionItems { get { return DicPromotionItems.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(DicPromotionItems) : string.Empty; } }
        public HashSet<ItemCategoryModel> CategoryList { get; set; }
        public List<PromotionModel> PromotionList { get; set; }
        public List<ItemPeriodPromotionModel> ItemPeriodPromotionList { get; set; }
        public IPagedList<ItemPeriodPromotionModel> PagingItemPeriodPromotionList { get; set; }

        //public ItemPeriodPromotionModel ItemPeriodPromotion { getPG; set; }
        //public List<ItemPeriodPromotionModel> ItemList { getPG; set; }
        public string ipIds { get; set; }
        //public Dictionary<string,int> DicItemCategory { getPG; set; }
        //public string JsonDicItemCategory { getPG { return DicItemCategory.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(DicItemCategory) : ""; } }
        public List<int> SelectedCategories { get; set; }
        public List<string> SelectedItemCodes { get; set; }
        public ItemPeriodPromotionEditModel() { }

        public ItemPeriodPromotionEditModel(long proId) : this()
        {
            Get(proId);
        }

        private void Get(long proId)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            this.proId = proId;

            PromotionList = Helpers.ModelHelper.GetPromotionList4Period(connection);
            foreach (var pro in PromotionList)
            {
                switch (CultureHelper.CurrentCulture)
                {
                    case 2:
                        pro.NameDisplay = pro.proName;
                        break;
                    case 1:
                        pro.NameDisplay = pro.proNameSC;
                        break;
                    default:
                    case 0:
                        pro.NameDisplay = pro.proNameTC;
                        break;
                }
            }
            CategoryList = Helpers.ModelHelper.GetCategoryList(connection);
            ItemPeriodPromotionList = proId == 0 ? connection.Query<ItemPeriodPromotionModel>(@"EXEC dbo.GetUnCatItemList @apId=@apId", new { apId }).ToList() : connection.Query<ItemPeriodPromotionModel>(@"EXEC dbo.GetItemPeriodPromotionListByProId @proId=@proId,@apId=@apId", new { proId,apId }).ToList();
            var ipIdList = ItemPeriodPromotionList.Select(x => x.Id).ToList();
            ipIds = proId == 0 ? string.Empty : string.Join(",", ipIdList);
            SelectedItemCodes = new List<string>();
            if (proId > 0)
            {
                DicPromotionItems = new Dictionary<string, List<ItemPeriodPromotionModel>>();
                DicPromotionItems[proId.ToString()] = new List<ItemPeriodPromotionModel>();
                SelectedCategories = new List<int>();

                foreach (var item in ItemPeriodPromotionList)
                {
                    DicPromotionItems[proId.ToString()].Add(item);
                    SelectedCategories.Add(item.catId);
                    SelectedItemCodes.Add(item.itemCode);
                }
            }
        }

        public ItemPeriodPromotionEditModel(int sortCol, string sortOrder, string keyword, int? pageNo) : this()
        {
            SortCol = sortCol;
            CurrentSortOrder = sortOrder;
            SortOrder = sortOrder == "desc" ? "asc" : "desc";
            Keyword = keyword ?? null;
            PageNo = pageNo;
        }

        public void Delete(long Id)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            connection.Execute(@"EXEC dbo.DeleteItemPeriodPromotion @Id=@Id", new { Id });
        }

        public void Delete(string Ids, SqlConnection connection)
        {
            //using var connection = new SqlConnection(_connectionString);
            //connection.Open();
            connection.Execute(@"EXEC dbo.DeleteItemPeriodPromotions @Ids=@Ids", new { Ids });
        }

        public void Add(ItemPeriodPromotionModel model, MMDbContext context=null)
        {
            if (context == null)
            {
                using (context = new MMDbContext())
                {
                    add(apId,model, context);
                }              
            }
            else
            {
                add(apId,model, context);
            }
        }

        private static void add(int apId, ItemPeriodPromotionModel model, MMDbContext context)
        {
            if (model.itemCodes.Length > 1)
            {
                List<ItemPeriodPromotion> itemPromotions = new List<ItemPeriodPromotion>();
                foreach (var itemcode in model.itemCodes)
                {
                    itemPromotions.Add(new ItemPeriodPromotion
                    {
                        itemCode = itemcode,
                        proId = model.proId,
                        AccountProfileId = apId,
                        CreateTime = DateTime.Now,
                    });
                }
                context.ItemPeriodPromotions.AddRange(itemPromotions);
            }
            else
            {
                context.AddItemPeriodPromotion(model.proId, model.itemCode, apId);
            }
            context.SaveChanges();
        }

        public void Edit(ItemPeriodPromotionModel model)
        {
            using var context = new MMDbContext();
            #region Delete Current Records First:
            context.DeleteItemPeriodPromotions(model.ipIds);
            context.SaveChanges();
            //can be this also: connection.Execute(@"EXEC dbo.DeleteItemPeriodPromotions @Ids=@Ids", new { Ids = model.ipIds }, transaction:transaction);
            //transaction.Execute(@"EXEC dbo.DeleteItemPeriodPromotions @Ids=@Ids", new { Ids = model.ipIds });
            #endregion

            #region Add Records:                
            Add(model, context);
            #endregion

        }



        public void GetList()
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            var sortColOrder = string.Concat(SortCol, " ", CurrentSortOrder);
            ItemPeriodPromotionList = connection.Query<ItemPeriodPromotionModel>(@"EXEC dbo.GetItemPeriodPromotionList @sortColOrder=@sortColOrder,@keyword=@keyword,@apId=@apId", new { sortColOrder, keyword = Keyword,apId }).ToList();
            PagingItemPeriodPromotionList = ItemPeriodPromotionList.ToPagedList((int)PageNo, PageSize);
            foreach (var pro in PagingItemPeriodPromotionList)
            {
                switch (CultureHelper.CurrentCulture)
                {
                    case 2:
                        pro.proNameDisplay = pro.proName;
                        break;
                    case 1:
                        pro.proNameDisplay = pro.proNameSC;
                        break;
                    default:
                    case 0:
                        pro.proNameDisplay = pro.proNameTC;
                        break;
                }
            }
        }
    }
}
