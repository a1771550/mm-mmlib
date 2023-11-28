using Dapper;
using Microsoft.Data.SqlClient;
using MMCommonLib.BaseModels;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Models.Item
{
    public class ItemCategoryEditModel : PagingBaseModel
    {
        public ItemCategoryModel ItemCategory { get; set; }
        public HashSet<ItemCategoryModel> ItemCategories { get; set; }
        public PagedList.IPagedList<ItemCategoryModel> PagingCategoryList { get; set; }

        public ItemCategoryEditModel() { }
        public ItemCategoryEditModel(int Id):this()
        {
            Get(Id);
        }
        public void Get(int Id)
        { 
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            ItemCategory = connection.QueryFirstOrDefault<ItemCategoryModel>(@"EXEC dbo.GetCategoryById @Id=@Id,@apId=@apId", new {Id, apId = AccountProfileId })??new ItemCategoryModel();
        }
        public void GetList(int SortCol=3, string SortOrder="desc", string Keyword = null)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            ItemCategories = Helpers.ModelHelper.GetCategoryList(connection, SortCol, SortOrder, Keyword);        
        }

        public void Edit(ItemCategoryModel model)
        {
            using var context = new MMDbContext();
            Category category = model.Id>0? context.Categories.Find(model.Id):new Category();          
            category.catName = model.catName;
            category.catNameTC = model.catNameTC;
            category.catNameSC = model.catNameSC;
            category.catDesc = model.catDesc;
            category.catDescSC = model.catDescSC;
            category.catDescTC = model.catDescTC;
            if (model.Id == 0)
            {
                category.AccountProfileId = AccountProfileId;
                category.CreateTime = DateTime.Now;
                context.Categories.Add(category);
            }
                
            category.ModifyTime = DateTime.Now;
            context.SaveChanges();
        }

        public static void Delete(int Id)
        {
            using var context = new MMDbContext();
            context.Categories.Remove(context.Categories.Find(Id));
            context.SaveChanges();
        }
    }
}
