using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.Helpers;
using MMCommonLib.BaseModels;
using MMDAL;
using Dapper;
using System.Configuration;
using MMLib.Models.POS.MYOB;
using MyobSupplier = MMDAL.MyobSupplier;
using CommonLib.Models.MYOB;
using System.Data.Odbc;

namespace MMLib.Models.Supplier
{
    public class SupplierEditModel : PagingBaseModel
    {
        public HashSet<string> SupplierNameList { get; set; }
        public string IpCountry { get { return "Hong Kong SAR"; } }
        public List<Country> MyobCountries { get; set; }
        public List<string> Countries { get; set; }
        public MyobSupplierModel MyobSupplier { get; set; }
        public SupplierEditModel()
        {
            var helper = new CountryData.Standard.CountryHelper();
            Countries = helper.GetCountries().ToList();
            // var region = CultureHelper.GetCountryByIP();
            //IpCountry = region.EnglishName;
        }
        public SupplierEditModel(string supCode) : this()
        {
            Get(0, supCode);
        }
        public SupplierEditModel(int Id) : this()
        {
            Get(Id);
        }
        public void Get(string supCode)
        {
            MyobSupplier = GetSupplierByCode(apId, supCode);
        }

        public static MyobSupplierModel GetSupplierByCode(int apId, string supCode)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(defaultConnection);
            connection.Open();
            return connection.QueryFirstOrDefault<MyobSupplierModel>(@"EXEC dbo.GetSupplierInfo6 @apId=@apId,@supCode=@supCode", new { apId, supCode });
        }

        public void Get(int supId = 0, string supCode = null)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
            if (connection.State == System.Data.ConnectionState.Closed) connection.Open();

            if (supId == 0 && supCode == null) MyobSupplier = new MyobSupplierModel();
            else
            {
                MyobSupplier = connection.QueryFirstOrDefault<MyobSupplierModel>(@"EXEC dbo.GetSupplierByIdCode @apId=@apId,@supId=@supId,@supCode=@supCode", new { apId, supId, supCode });
                if (MyobSupplier != null)
                {
                    MyobSupplier.StreetLines[0] = MyobSupplier.supAddrStreetLine1;
                    MyobSupplier.StreetLines[1] = MyobSupplier.supAddrStreetLine2;
                    MyobSupplier.StreetLines[2] = MyobSupplier.supAddrStreetLine3;
                    MyobSupplier.StreetLines[3] = MyobSupplier.supAddrStreetLine4;
                    MyobSupplier.UploadFileList = connection.Query<string>(@"EXEC dbo.GetSupplierFileList @apId=@apId,@supCodes=@supCodes", new { apId, supCodes = MyobSupplier.supCode }).ToHashSet();
                }
            }
            //GetSupplierNameList
            SupplierNameList = connection.Query<string>(@"EXEC dbo.GetSupplierNameList @apId=@apId,@active=@active", new { apId, active = true }).ToHashSet();

            MyobCountries = Helpers.ModelHelper.PopulateDefaultCountries();
            #region Handle View File
            Helpers.ModelHelper.HandleViewFileList(MyobSupplier.UploadFileList, AccountProfileId, MyobSupplier.supId, ref MyobSupplier.ImgList, ref MyobSupplier.FileList);
            #endregion
        }

        public List<MyobSupplierModel> SupplierList { get; set; }
        public PagedList.IPagedList<MyobSupplierModel> PagingSupplierList { get; set; }
        public void GetList()
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(DefaultConnection);
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
            SupplierList = connection.Query<MyobSupplierModel>(@"EXEC dbo.GetSupplierPagingList @apId=@apId,@sortName=@sortName,@sortOrder=@sortOrder,@keyword=@keyword", new { apId, sortName = SortName, sortOrder = SortOrder, keyword = Keyword }).ToList();
        }

        public static MyobSupplierModel QuickAdd(string supName)
        {
            using var context = new MMDbContext();
            DateTime dateTime = DateTime.Now;

            var supId = context.MyobSuppliers.Count() == 0 ? 1 : context.MyobSuppliers.Max(x => x.supId) + 1;
            int codelength = int.Parse(ConfigurationManager.AppSettings["MaxSupplierCodeLength"]);
            MMDAL.MyobSupplier supplier = new()
            {
                supId = supId,
                supName = supName,
                supCode = CommonHelper.GenerateNonce(codelength, false),
                supAbss = false,
                AccountProfileId = comInfo.AccountProfileId,
                supIsActive = true,
                supCheckout = false,
                CreateTime = dateTime,
                CreateBy = user.UserCode
            };
            supplier = context.MyobSuppliers.Add(supplier);
            context.SaveChanges();

            return new MyobSupplierModel
            {
                supId = supplier.supId,
                supName = supplier.supName,
                supCode = supplier.supCode,
            };
        }

        public static void Edit(MyobSupplierModel model)
        {
            using var context = new MMDbContext();
            DateTime dateTime = DateTime.Now;
            if (model.supId == 0)
            {
                model.supId = context.MyobSuppliers.Count() == 0 ? 1 : context.MyobSuppliers.Max(x => x.supId) + 1;
                int codelength = int.Parse(ConfigurationManager.AppSettings["MaxSupplierCodeLength"]);
                MMDAL.MyobSupplier ps = new()
                {
                    supId = model.supId,
                    supName = model.supName,
                    supCode = CommonHelper.GenerateNonce(codelength, false),
                    supAbss = false,
                    supAccount = model.supAccount,
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
                };
                context.MyobSuppliers.Add(ps);
                context.SaveChanges();
            }
            else
            {
                MMDAL.MyobSupplier ps = context.MyobSuppliers.FirstOrDefault(x => x.supId == model.supId);
                ps.supName = model.supName;
                ps.supAccount = model.supAccount;
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
                context.SaveChanges();
            }
        }

        public static void Delete(int id)
        {
            using var context = new MMDbContext();
            var ps = context.MyobSuppliers.FirstOrDefault(x => x.supId == id);
            context.MyobSuppliers.Remove(ps);
            context.SaveChanges();
        }

        public static IEnumerable<MyobSupplier> ConvertModel(List<MyobSupplierModel> selectedSuppliers, int apId)
        {
            DateTime dateTime = DateTime.Now;

            Dictionary<string, int> DicTermsOfPayments = new Dictionary<string, int>();
            using var context = new ProxyDbContext();
            var termsofpayments = context.MyobTermsOfPayments.ToList();
            foreach (var term in termsofpayments)
            {
                DicTermsOfPayments[term.TermsOfPaymentID.Trim()] = term.MyobID;
            }

            List<MyobSupplier> suppliers = new List<MyobSupplier>();
            foreach (var supplier in selectedSuppliers)
            {
                string supcode;
                supcode = supplier.supCode;
                //var salecomment = supplier.SaleComment;
                MyobSupplier msupplier = new MyobSupplier();
                msupplier.supId = supplier.supId;
                msupplier.supFirstName = supplier.supFirstName;
                msupplier.supIsActive = supplier.supIsActive;
                msupplier.supCode = supcode;

                msupplier.supPhone = (supplier.AddressList != null && supplier.AddressList.Count > 0) ? supplier.AddressList[0].Phone1 : supcode;
                msupplier.supName = supplier.supName;

                msupplier.CreateTime = dateTime;
                msupplier.ModifyTime = dateTime;

                msupplier.LatePaymentChargePercent = supplier.Terms.LatePaymentChargePercent == null ? 0 : Convert.ToDecimal(supplier.Terms.LatePaymentChargePercent);
                msupplier.EarlyPaymentDiscountPercent = supplier.Terms.EarlyPaymentDiscountPercent == null ? 0 : Convert.ToDecimal(supplier.Terms.EarlyPaymentDiscountPercent);
                msupplier.TermsOfPaymentID = supplier.Terms.TermsOfPaymentID == null ? null : supplier.Terms.TermsOfPaymentID.Trim();

                if (DicTermsOfPayments.ContainsKey(msupplier.TermsOfPaymentID))
                    msupplier.PaymentIsDue = DicTermsOfPayments[msupplier.TermsOfPaymentID];

                msupplier.DiscountDays = supplier.Terms.DiscountDays == null ? 0 : supplier.Terms.DiscountDays;
                msupplier.BalanceDueDays = supplier.Terms.BalanceDueDays == null ? 0 : supplier.Terms.BalanceDueDays;
                msupplier.ImportPaymentIsDue = supplier.Terms.ImportPaymentIsDue == null ? 0 : supplier.Terms.ImportPaymentIsDue;
                msupplier.DiscountDate = supplier.Terms.DiscountDate == null ? null : supplier.Terms.DiscountDate;
                msupplier.BalanceDueDate = supplier.Terms.BalanceDueDate == null ? null : supplier.Terms.BalanceDueDate;
                msupplier.PaymentTermsDesc = supplier.PaymentTermsDesc == null ? null : supplier.PaymentTermsDesc;

                msupplier.AbssSalesID = supplier.AbssSalesID;

                msupplier.AccountProfileId = apId;
                msupplier.CurrencyID = supplier.CurrencyID;
                msupplier.TaxIDNumber = supplier.TaxIDNumber;
                msupplier.TaxCodeID = supplier.TaxCodeID;

                if (supplier.AddressList.Count > 0)
                {
                    msupplier.supAddrLocation = supplier.AddressList[0].Location;
                    msupplier.supContact = supplier.AddressList[0].ContactName;
                    msupplier.supAddrPhone1 = supplier.AddressList[0].Phone1;
                    msupplier.supAddrPhone2 = supplier.AddressList[0].Phone2;
                    msupplier.supAddrPhone3 = supplier.AddressList[0].Phone3;
                    msupplier.supEmail = supplier.AddressList[0].Email;
                    msupplier.supAddrWeb = supplier.AddressList[0].WWW;
                    msupplier.supAddrCity = supplier.AddressList[0].City;
                    msupplier.supAddrCountry = supplier.AddressList[0].Country;
                    msupplier.supAddrStreetLine1 = supplier.AddressList[0].Street;
                    msupplier.supAddrStreetLine2 = supplier.AddressList[0].StreetLine1;
                    msupplier.supAddrStreetLine3 = supplier.AddressList[0].StreetLine2;
                    msupplier.supAddrStreetLine4 = supplier.AddressList[0].StreetLine3;
                }
                msupplier.supCheckout = true;
                msupplier.CreateTime = DateTime.Now;
                suppliers.Add(msupplier);
            }
            return suppliers;
        }

        public static List<SupplierInfo4Abss> ConvertSupInfoModel(List<MyobSupplierModel> selectedSuppliers, int apId)
        {
            List<SupplierInfo4Abss> supplierInfo = new List<SupplierInfo4Abss>();
            foreach (var supplier in selectedSuppliers)
            {
                if (supplier.AddressList.Count > 0)
                {
                    foreach (var addr in supplier.AddressList)
                    {
                        supplierInfo.Add(new SupplierInfo4Abss
                        {
                            SupCode = supplier.supCode,
                            AccountProfileId = apId,
                            SupAddrLocation = addr.Location,
                            StreetLine1 = addr.StreetLine1,
                            StreetLine2 = addr.StreetLine2,
                            StreetLine3 = addr.StreetLine3,
                            StreetLine4 = addr.StreetLine4,
                            City = addr.City,
                            State = addr.State,
                            Postcode = addr.Postcode,
                            Country = addr.Country,
                            Phone1 = addr.Phone1,
                            Phone2 = addr.Phone2,
                            Phone3 = addr.Phone3,
                            Fax = addr.Fax,
                            Email = addr.Email,
                            Salutation = addr.Salutation,
                            ContactName = addr.ContactName,
                            WWW = addr.WWW,
                            CreateTime = DateTime.Now,
                        });
                    }
                }
            }
            return supplierInfo;
        }

        public static List<string> GetImportAbssValues(List<MYOBSupplierModel> AbssSupplierList)
        {
            List<string> columns = new List<string>(); 
            int colcount = MyobHelper.ImportSupplierColCount;
            for (int j = 0; j < colcount; j++)
            {
                columns.Add("'{" + j + "}'");
            }
            string strcolumn = string.Join(",", columns);

            List<string> values = new List<string>();

            foreach (var supplier in AbssSupplierList)
            {
                string value = "";
                string cardstatus = supplier.CardStatus;
                /*
				 * CoLastName,CardID,CardStatus,Address1Phone1,Address1Email,PaymentIsDue,DiscountDays,BalanceDueDays,Address1ContactName,Address1AddressLine1,Address1AddressLine2,Address1AddressLine3,Address1AddressLine4,Address1Phone2,Address1Phone3,Address1City,Address1Country,Address1Website,FirstName
				 */

                //string address = StringHandleAddress(string.Concat(supplier.supAddrStreetLine1, supplier.supAddrStreetLine2, supplier.supAddrStreetLine3, supplier.supAddrStreetLine4));
                supplier.Address1AddressLine1 = CommonHelper.StringHandleAddress(supplier.Address1AddressLine1);
                supplier.Address1AddressLine2 = CommonHelper.StringHandleAddress(supplier.Address1AddressLine2);
                supplier.Address1AddressLine3 = CommonHelper.StringHandleAddress(supplier.Address1AddressLine3);
                supplier.Address1AddressLine4 = CommonHelper.StringHandleAddress(supplier.Address1AddressLine4);

                value = string.Format("(" + strcolumn + ")", StringHandlingForSQL(supplier.CoLastName), StringHandlingForSQL(supplier.CardID), cardstatus, StringHandlingForSQL(supplier.Address1Phone1), "", StringHandlingForSQL(supplier.Address1Email), "", "", "", "", supplier.Address1AddressLine1, supplier.Address1AddressLine2, supplier.Address1AddressLine3, supplier.Address1AddressLine4, StringHandlingForSQL(supplier.Address1Phone2), StringHandlingForSQL(supplier.Address1Phone3), StringHandlingForSQL(supplier.Address1City), StringHandlingForSQL(supplier.Address1Country), StringHandlingForSQL(supplier.Address1Website), StringHandlingForSQL(supplier.FirstName));

                values.Add(value);
            }

            return values;
        }

        private static string StringHandlingForSQL(string str)
        {
            str ??= "";
            return CommonHelper.StringHandlingForSQL(str);
        }

        
    }

    public class SupCodeName
    {
        public string supCode { get; set; }
        public string supName { get; set; }
    }
}
