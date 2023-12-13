using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.Helpers;
using MMCommonLib.BaseModels;
using MMDAL;
using Dapper;
using System.Runtime.Remoting.Contexts;
using System.Web;

namespace MMLib.Models.Supplier
{
    public class SupplierEditModel : PagingBaseModel
    {
		public string IpCountry { get; set; }
		public List<Country> MyobCountries { get; set; }
		public List<string> Countries { get; set; }
		public SupplierModel Supplier { get; set; }
        public SupplierEditModel() {
			var helper = new CountryData.Standard.CountryHelper();
			Countries = helper.GetCountries().ToList();
			var region = CultureHelper.GetCountryByIP();
			IpCountry = region.EnglishName;
		}
        public SupplierEditModel(string supCode):this()
        {
            Get(0,supCode);
        }
        public SupplierEditModel(int Id):this()
        {
            Get(Id);
        }
        public void Get(string supCode)
        {           
            Supplier = GetSupplierByCode(apId, supCode);
        }

        public static SupplierModel GetSupplierByCode(int apId, string supCode)
        {          
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(defaultConnection);
            connection.Open();
            return connection.QueryFirstOrDefault<SupplierModel>(@"EXEC dbo.GetSupplierInfo6 @apId=@apId,@supCode=@supCode", new { apId, supCode });
        }

        public void Get(int supId=0, string supCode=null)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
            connection.Open();
            Supplier = connection.QueryFirstOrDefault<SupplierModel>(@"EXEC dbo.GetSupplierByIdCode @apId=@apId,@supId=@supId,@supCode=@supCode", new { apId,supId,supCode });
            if (Supplier != null)
            {
                Supplier.StreetLines[0] = Supplier.supAddrStreetLine1;
                Supplier.StreetLines[1] = Supplier.supAddrStreetLine2;
                Supplier.StreetLines[2] = Supplier.supAddrStreetLine3;
                Supplier.StreetLines[3] = Supplier.supAddrStreetLine4;
                Supplier.UploadFileList = connection.Query<string>(@"EXEC dbo.GetSupplierFileList @apId=@apId,@supId=@supId", new { apId,supId }).ToHashSet();
            }
            else
            {
                Supplier = new SupplierModel();
            }

            MyobCountries = Helpers.ModelHelper.PopulateDefaultCountries();
            #region Handle View File
            Helpers.ModelHelper.HandleViewFileList(Supplier.UploadFileList, AccountProfileId, Supplier.supId, ref Supplier.ImgList, ref Supplier.FileList);
            #endregion
        }

        public List<SupplierModel> SupplierList { get; set; }
        public PagedList.IPagedList<SupplierModel> PagingSupplierList { get; set; }
        public void GetList()
        {
            using var context = new MMDbContext();
            SupplierList = new List<SupplierModel>();            
            var pslist = context.GetSupplierList5(ComInfo.AccountProfileId, SortName, SortOrder, Keyword).ToList();
            foreach (var ps in pslist)
            {
                SupplierList.Add(new SupplierModel
                {
                    supId = ps.supId,
                    AccountProfileId = ps.AccountProfileId,                  
                    supIsActive = ps.supIsActive,
                    supAbss = ps.supAbss,
                    supName = ps.supName,
                    supCode = ps.supCode,
                    supPhone = ps.supPhone,
                    supEmail = ps.supEmail,
                    supContact = ps.supContact,
                    CreateTime = ps.CreateTime,
                    ModifyTime = ps.ModifyTime,
                    supCheckout = ps.supCheckout,
                    AccountProfileName = ps.AccountProfileName,
                });
            }
        }

        public static void Edit(SupplierModel model)
        {
            using var context = new MMDbContext();
            DateTime dateTime = DateTime.Now;
            if (model.supId == 0)
            {
                model.supId = context.Suppliers.Count() == 0 ? 1 : context.Suppliers.Max(x => x.supId) + 1;
                MMDAL.Supplier ps = new()
                {
                    supId = model.supId,
                    supName = model.supName,
                    supCode = CommonHelper.GenerateNonce(20),
                    supAbss = false,
                    AccountProfileId = comInfo.AccountProfileId,                    
                    supIsActive = true,
                    supPhone = model.supPhone,
                    supEmail = model.supEmail,
                    supContact = model.supContact,
                    supAddrCity = model.supAddrCity,
                    supAddrCountry = model.supAddrCountry,
                    supAddrState = model.supAddrState,
                    supAddrStreetLine1 = model.supAddrStreetLine1,
                    supAddrStreetLine2 = model.supAddrStreetLine2,
                    supAddrStreetLine3 = model.supAddrStreetLine3,
                    supAddrStreetLine4 = model.supAddrStreetLine4,
                    supAddrWeb = model.supAddrWeb,
                    supAddrPhone1 = model.supAddrPhone1,
                    supAddrPhone2 = model.supAddrPhone2,
                    supAddrPhone3 = model.supAddrPhone3,
                    supCheckout = false,
                    CreateTime = dateTime,
                    ModifyTime = dateTime,
                    IsABSS=false
                };
                context.Suppliers.Add(ps);
                context.SaveChanges();
            }
            else
            {
                MMDAL.Supplier ps = context.Suppliers.FirstOrDefault(x=>x.supId==model.supId);
                ps.supName = model.supName;
                ps.ModifyTime = dateTime;
                ps.supPhone = model.supPhone;
                ps.supEmail = model.supEmail;
                ps.supContact = model.supContact;
                ps.supAddrCity = model.supAddrCity;
                ps.supAddrCountry = model.supAddrCountry;
                ps.supAddrState = model.supAddrState;
                ps.supAddrStreetLine1 = model.supAddrStreetLine1;
                ps.supAddrStreetLine2 = model.supAddrStreetLine2;
                ps.supAddrStreetLine3 = model.supAddrStreetLine3;
                ps.supAddrStreetLine4 = model.supAddrStreetLine4;
                ps.supAddrWeb = model.supAddrWeb;
                ps.supAddrPhone1 = model.supAddrPhone1;
                ps.supAddrPhone2 = model.supAddrPhone2;
                ps.supAddrPhone3 = model.supAddrPhone3;
                ps.supCheckout = false;
                context.SaveChanges();
            }
        }

        public static void Delete(int id)
        {
            using var context = new MMDbContext();
            var ps = context.Suppliers.FirstOrDefault(x=>x.supId==id);
            context.Suppliers.Remove(ps);
            context.SaveChanges();
        }
    }

    public class SupCodeName
    {
        public string supCode { get; set; }
        public string supName { get; set; } 
    }
}
