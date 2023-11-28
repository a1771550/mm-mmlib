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
    public class ItemQtyPromotionEditModel : PagingBaseModel
    {
        public long proId { get; set; } = 0;     
        public Dictionary<string, List<ItemQtyPromotionModel>> DicPromotionItems { get; set; } = new Dictionary<string, List<ItemQtyPromotionModel>>();
        public string JsonDicPromotionItems { get { return DicPromotionItems.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(DicPromotionItems) : string.Empty; } }
        public HashSet<ItemCategoryModel> CategoryList { get; set; }
        public List<PromotionModel> PromotionList { get; set; }
        public List<ItemQtyPromotionModel> ItemQtyPromotionList { get; set; }
        public IPagedList<ItemQtyPromotionModel> PagingItemQtyPromotionList { get; set; }
        public string ipIds { get; set; }
        public List<int> SelectedCategories { get; set; }
        public List<string> SelectedItemCodes { get; set; }
        public ItemQtyPromotionEditModel() { }

        public ItemQtyPromotionEditModel(long proId) : this()
        {
            Get(proId);
        }

        private void Get(long proId)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            this.proId = proId;

            PromotionList = Helpers.ModelHelper.GetPromotionList4Qty(connection);
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
            ItemQtyPromotionList = proId == 0 ? connection.Query<ItemQtyPromotionModel>(@"EXEC dbo.GetUnCatItemList @apId=@apId", new { apId }).ToList() : connection.Query<ItemQtyPromotionModel>(@"EXEC dbo.GetItemQtyPromotionListByProId @proId=@proId,@apId=@apId", new { proId,apId }).ToList();
            var ipIdList = ItemQtyPromotionList.Select(x => x.Id).ToList();
            ipIds = proId == 0 ? string.Empty : string.Join(",", ipIdList);
            SelectedItemCodes = new List<string>();
            if (proId > 0)
            {
                DicPromotionItems = new Dictionary<string, List<ItemQtyPromotionModel>>();
                DicPromotionItems[proId.ToString()] = new List<ItemQtyPromotionModel>();
                SelectedCategories = new List<int>();

                foreach (var item in ItemQtyPromotionList)
                {
                    DicPromotionItems[proId.ToString()].Add(item);
                    SelectedCategories.Add(item.catId);
                    SelectedItemCodes.Add(item.itemCode);
                }
            }
        }

        public ItemQtyPromotionEditModel(int sortCol, string sortOrder, string keyword, int? pageNo) : this()
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
            connection.Execute(@"EXEC dbo.DeleteItemQtyPromotion @Id=@Id", new { Id });
        }

        public void Delete(string Ids, SqlConnection connection)
        {
            //using var connection = new SqlConnection(_connectionString);
            //connection.Open();
            connection.Execute(@"EXEC dbo.DeleteItemQtyPromotions @Ids=@Ids", new { Ids });
        }
        public void Add(ItemQtyPromotionModel model, MMDbContext context = null)
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

        private static void add(int apId, ItemQtyPromotionModel model, MMDbContext context)
        {
            if (model.itemCodes.Length > 1)
            {
                List<ItemQtyPromotion> itemPromotions = new List<ItemQtyPromotion>();
                foreach (var itemcode in model.itemCodes)
                {
                    itemPromotions.Add(new ItemQtyPromotion
                    {
                        itemCode = itemcode,
                        proId = model.proId,
                        AccountProfileId = apId,
                        CreateTime = DateTime.Now,
                    });
                }
                context.ItemQtyPromotions.AddRange(itemPromotions);
            }
            else
            {
                context.AddItemQtyPromotion(model.proId, model.itemCode, apId);
            }
            context.SaveChanges();
        }
    

        public void Edit(ItemQtyPromotionModel model)
        {
            using var context = new MMDbContext();
           
            #region Delete Current Records First:
            context.DeleteItemQtyPromotions(model.ipIds);
            context.SaveChanges();
            //can be this also: connection.Execute(@"EXEC dbo.DeleteItemQtyPromotions @Ids=@Ids", new { Ids = model.ipIds }, transaction:transaction);
            //transaction.Execute(@"EXEC dbo.DeleteItemQtyPromotions @Ids=@Ids", new { Ids = model.ipIds });
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
            ItemQtyPromotionList = connection.Query<ItemQtyPromotionModel>(@"EXEC dbo.GetItemQtyPromotionList @sortColOrder=@sortColOrder,@keyword=@keyword,@apId=@apId", new { sortColOrder, keyword = Keyword,apId }).ToList();
            PagingItemQtyPromotionList = ItemQtyPromotionList.ToPagedList((int)PageNo, PageSize);
            foreach (var pro in PagingItemQtyPromotionList)
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
