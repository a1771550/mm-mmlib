using MMDAL;
using MMLib.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CommonLib.Models;
using CommonLib.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;
using System.Data.Entity.Validation;
using System.Text;
using MMLib.Models.POS.MYOB;
using MMLib.Models.Supplier;
using MMCommonLib.Models;
using MMCommonLib.CommonModels;
using CommonLib.Models.MYOB;
using TaxType = CommonLib.Models.TaxType;
using Dapper;
using System.Net.Mail;
using System.Net;
using MMCommonLib.BaseModels;
using MMLib.Models.MYOB;
using System.IO;
using MMLib.Models.User;
using CommonLib.BaseModels;
using CommonLib.App_GlobalResources;
using MMLib.Models.Account;
using System.Runtime.Versioning;

namespace MMLib.Helpers
{
    public static class ModelHelper
    {
        public const string sqlfields4Journal = "JournalNumber,JournalDate,Memo,Inclusive,AccountNumber,DebitExTaxAmount,DebitIncTaxAmount,CreditExTaxAmount,CreditIncTaxAmount,Job,AllocationMemo";

        public const string sqlfields4Sales = "InvoiceNumber,SaleDate,AmountPaid,ItemNumber,Quantity,Price,Discount,SaleStatus,Location,CardID,PaymentMethod,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount,PercentMonthlyCharge,DeliveryStatus,CustomersNumber,JobName,SalespersonFirstName,SalespersonLastName,Memo,TaxCode,CurrencyCode,ExchangeRate,AddressLine1,AddressLine2,AddressLine3,AddressLine4";

        /// <summary>
        /// for Insert Item Services
        /// Important: Amount is mandatory!!!!
        /// </summary>
        public const string sqlfields4Deposit = "CardID,InvoiceNumber,SaleDate,AccountNumber,Amount4Abss,SaleStatus,DeliveryStatus,Memo,SalesPersonLastName,Description,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount,PercentMonthlyCharge";


        private static string DefaultConnection { get { return ConfigurationManager.AppSettings["DefaultConnection"]; } }
        private static SqlConnection SqlConnection { get { return new SqlConnection(DefaultConnection); } }
        private static ComInfo ComInfo { get { return HttpContext.Current.Session["ComInfo"] as ComInfo; } }
        //private static List<string> Shops;
        private static int AccountProfileId { get { return ComInfo.AccountProfileId; } }
        private static int apId { get { return AccountProfileId; } }
        private static int CompanyId { get { return 1; } }
        private static bool NonABSS { get { return ComInfo.DefaultCheckoutPortal == "nonabss"; } }
        private static bool ApprovalMode { get { return (bool)ComInfo.ApprovalMode; } }

        static bool EnableReviewUrl { get { return int.Parse(ConfigurationManager.AppSettings["EnableReviewUrl"]) == 1; } }

        public static List<string> ShopNames;

        public static List<Country> PopulateDefaultCountries()
        {
            return new List<Country> {
                new Country
            {
                CountryID = 1,
                CountryName = Resource.HongKong
            },
                new Country
            {
                CountryID = 2,
                CountryName = Resource.Macao
            },
                new Country
            {
                CountryID = 3,
                CountryName = Resource.China
            },
            };
        }

        public static string GetPDFLnk(string pstCode, string supCode, string fileName)
        {
            var _file = Path.Combine("Purchase", apId.ToString(), pstCode, supCode, fileName);
            return $"<a class='' href='/{_file}' target='_blank'><img src='{UriHelper.GetBaseUrl()}/Images/pdf.jpg' class='thumbnail'/>{fileName}</a>";
        }
        public static void HandleViewFileList(HashSet<string> uploadfilelist, int apId, int Id, ref List<string> ImgList, ref List<string> FileList)
        {
            if (uploadfilelist != null && uploadfilelist.Count > 0)
            {
                foreach (var file in uploadfilelist)
                {
                    var _file = Path.Combine(apId.ToString(), Id.ToString(), file);
                    if (CommonHelper.ImageExtensions.Contains(Path.GetExtension(file).ToUpperInvariant()))
                    {
                        _file = $"<a href='{_file}' target='_blank'><img src='{_file}'/></a>";
                        ImgList.Add(_file);
                    }
                    else
                    {
                        _file = $"<a class='btn btn-success' href='{_file}' target='_blank'>{file}</a>";
                        FileList.Add(_file);
                    }
                }
            }
        }
       










        public static void SaveSuppliersFrmCentral(MMDbContext context, int apId, ComInfo comInfo = null)
        {
            if (comInfo == null)
                comInfo = context.ComInfoes.FirstOrDefault(x => x.AccountProfileId == apId);
            string ConnectionString = string.Format(@"Driver={0};TYPE=MYOB;UID={1};PWD={2};DATABASE={3};HOST_EXE_PATH={4};NETWORK_PROTOCOL=NONET;DRIVER_COMPLETION=DRIVER_NOPROMPT;KEY={5};ACCESS_TYPE=READ;", comInfo.MYOBDriver, comInfo.MYOBUID, comInfo.MYOBPASS, comInfo.MYOBDb, comInfo.MYOBExe, comInfo.MYOBKey);
            try
            {
                DateTime dateTime = DateTime.Now;
                List<MyobSupplierModel> suplist = MYOBHelper.GetSupplierList(ConnectionString);
                /* remove current records first: */
                List<MyobSupplier> suppliers = context.MyobSuppliers.Where(x => x.AccountProfileId == apId).ToList();
                context.MyobSuppliers.RemoveRange(suppliers);
                context.SaveChanges();
                /*********************************/

                List<MyobSupplier> newsuppliers = new List<MyobSupplier>();
                Dictionary<string, int> DicTermsOfPayments = new Dictionary<string, int>();
                var termsofpayments = context.MyobTermsOfPayments.ToList();
                foreach (var term in termsofpayments)
                {
                    DicTermsOfPayments[term.TermsOfPaymentID.Trim()] = term.MyobID;
                }
                foreach (var supplier in suplist)
                {
                    MyobSupplier msupplier = new MyobSupplier();
                    msupplier.supFirstName = supplier.supFirstName;
                    msupplier.supId = supplier.supId;
                    msupplier.supIsIndividual = supplier.supIsIndividual;
                    msupplier.supIsActive = supplier.supIsActive;
                    msupplier.supCode = supplier.supCode.StartsWith("*") ? supplier.supCardRecordID.ToString() : supplier.supCode;
                    //msupplier.supName = GetForeignCurrencyCardName(supplier.supCode, supplier.supName);
                    msupplier.supName = supplier.supName;
                    msupplier.supFirstName = supplier.supFirstName;
                    msupplier.supLastName = supplier.supLastName;
                    msupplier.supIdentifierID = supplier.supIdentifierID;
                    msupplier.supCustomField1 = supplier.supCustomField1;
                    msupplier.supCustomField2 = supplier.supCustomField2;
                    msupplier.supCustomField3 = supplier.supCustomField3;
                    msupplier.CurrencyID = supplier.CurrencyID;
                    msupplier.TaxIDNumber = supplier.TaxIDNumber;
                    msupplier.TaxCodeID = supplier.TaxCodeID;
                    msupplier.CreateTime = dateTime;
                    msupplier.ModifyTime = dateTime;
                    msupplier.AccountProfileId = apId;
                    msupplier.supAbss = true;
                    msupplier.supCheckout = true;

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

                    var address = supplier.AddressList.FirstOrDefault();
                    if (address != null)
                    {
                        msupplier.supEmail = address.Email;
                        msupplier.supPhone = address.Phone1;
                        msupplier.supAddrCountry = address.Country;
                        msupplier.supAddrCity = address.City;
                        msupplier.supAddrStreetLine1 = address.StreetLine1;
                        msupplier.supAddrStreetLine2 = address.StreetLine2;
                        msupplier.supAddrStreetLine3 = address.StreetLine3;
                        msupplier.supAddrStreetLine4 = address.StreetLine4;
                    }

                    newsuppliers.Add(msupplier);
                }
                context.MyobSuppliers.AddRange(newsuppliers);
                WriteLog(context, "Import MyobSupplier data from Central done", "ImportFrmCentral");
                context.SaveChanges();

                HashSet<string> AbssSupCodes = suplist.Select(x => x.supCode).Distinct().ToHashSet();
                #region Update Supplier Checkout
                UpdateSupplierCheckouts(AbssSupCodes, context);
                #endregion
            }
            catch (DbEntityValidationException e)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var eve in e.EntityValidationErrors)
                {
                    sb.AppendFormat("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        sb.AppendFormat("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"",
        ve.PropertyName,
        eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName),
        ve.ErrorMessage);
                    }
                }
                WriteLog(context, string.Format("Import MyobSupplier data from Central failed:{0}", sb.ToString()), "ExportFrmCentral");
                context.SaveChanges();
            }
        }

        private static void UpdateSupplierCheckouts(HashSet<string> abssSupCodes, MMDbContext context)
        {
            var suppliers = context.Suppliers.Where(x => x.AccountProfileId == apId).ToList();
            if (suppliers != null && suppliers.Count > 0)
            {
                foreach (var supplier in suppliers)
                {
                    if (abssSupCodes.Contains(supplier.supCode)) { supplier.supCheckout = true; supplier.ModifyTime = DateTime.Now; }
                }
                context.SaveChanges();
            }
        }

        private static string StringHandlingForSQL(string strInput)
        {
            return CommonHelper.StringHandlingForSQL(strInput);
        }

        public static string genMemo(string currencycode, double exchangerate, double paytypeamts, string ordercode, string paytypes = "")
        {
            return StringHandlingForSQL(string.Concat(currencycode, " ", paytypes, " ", paytypeamts, " ", $"({exchangerate})", " ", ordercode));
        }

        private static string GetForeignCurrencyCardName(string cardCode, string cardName)
        {
            string currkeys = cardCode.Length >= 6 && cardCode.StartsWith("CAS") ? cardCode.Substring(3, 3).ToLower() : cardCode;
            return currkeys == "usd" || currkeys == "cny" || currkeys == "eur" || currkeys == "mop" ? string.Concat(cardName, " (", currkeys.ToUpper(), ")") : cardName;
        }

        public static void GetReady4Print(MMDbContext context, ref ReceiptViewModel Receipt, ref List<string> DisclaimerList, ref List<string> PaymentTermsList)
        {
            int reId = 0;
            Receipt = (from r in context.Receipts
                       where                        
                       r.AccountProfileId == ComInfo.AccountProfileId
                       select new ReceiptViewModel { Id = r.Id, CompanyName = r.CompanyName + ":" + r.CompanyNameCN + ":" + r.CompanyNameEng, CompanyAddress = r.CompanyAddress + ":" + r.CompanyAddressCN + ":" + r.CompanyAddressEng, CompanyAddress1 = r.CompanyAddress1 + ":" + r.CompanyAddress1CN + ":" + r.CompanyAddress1Eng, CompanyPhone = r.CompanyPhone, CompanyWebSite = r.CompanyWebSite }
                          ).FirstOrDefault();
            int lang = CultureHelper.CurrentCulture;
            Receipt.CompanyName = Receipt.CompanyName.Split(':')[lang];
            Receipt.CompanyAddress = Receipt.CompanyAddress.Split(':')[lang];
            Receipt.CompanyAddress1 = Receipt.CompanyAddress1.Split(':')[lang];

            reId = Receipt.Id;
            var disclaimers = (from ri in context.ReceiptInfoes
                               where ri.reId == reId
                               select ri.DisclaimerTxt + ":" + ri.DisclaimerTxtCN + ":" + ri.DisclaimerTxtEng).ToList();
            if (disclaimers.Count > 0)
            {
                DisclaimerList = new List<string>();
                foreach (var d in disclaimers)
                {
                    DisclaimerList.Add(d.Split(':')[CultureHelper.CurrentCulture]);
                }
            }
            var paymentterms = (from ri in context.ReceiptInfoes
                                where ri.reId == reId
                                select ri.PaymentTermsTxt + ":" + ri.PaymentTermsTxtCN + ":" + ri.PaymentTermsTxtEng).ToList();
            if (paymentterms.Count > 0)
            {
                PaymentTermsList = new List<string>();
                foreach (var d in paymentterms)
                {
                    PaymentTermsList.Add(d.Split(':')[CultureHelper.CurrentCulture]);
                }
            }
        }

        public static string HandleCheckoutPortal(string defaultCheckoutPortal = "")
        {
            string defaultcheckoutportal;
            if (string.IsNullOrEmpty(defaultCheckoutPortal))
            {
                defaultcheckoutportal = ComInfo.DefaultCheckoutPortal;
                defaultcheckoutportal = defaultcheckoutportal == "abss" ? defaultcheckoutportal.ToUpper() : CommonHelper.FirstCharToUpper(defaultcheckoutportal);
            }
            else
            {
                defaultcheckoutportal = defaultCheckoutPortal == "abss" ? defaultCheckoutPortal.ToUpper() : CommonHelper.FirstCharToUpper(defaultCheckoutPortal);
            }

            return defaultcheckoutportal;
        }

        public static string GetUserCode(MMDbContext context, string usertype = "sales")
        {
            var MaxSalesCode = context.GetMaxUserCode(usertype).FirstOrDefault();
            var resultString = Regex.Match(MaxSalesCode, @"\d+").Value;
            CommonLib.Helpers.ModelHelper.GetCodeDigit(ref resultString);
            return string.Concat(usertype, resultString);
        }
        public static bool CheckIfCrmSalesManager(SessUser User = null)
        {
            SessUser user = User == null ? HttpContext.Current.Session["User"] as SessUser : User;
            return user.Roles.Any(x => x == RoleType.CRMSalesManager);
        }
        public static bool CheckIfCrmAdmin(SessUser User = null)
        {
            SessUser user = User == null ? HttpContext.Current.Session["User"] as SessUser : User;
            return user.Roles.Any(x => x == RoleType.CRMAdmin);
        }
        public static void GetEmailPhoneList(int apId, ref List<string> CusEmailList, ref List<string> CusPhoneList)
        {
            using var connection = new SqlConnection(DefaultConnection);
            connection.Open();
            var emailphonelist = connection.Query<string>(@"EXEC dbo.GetEmailPhoneList @apId=@apId,@type=@type", new { apId, type = "contact" }).ToList();

            //var mailphonelist = context.GetEmailPhoneList("contact").ToList();
            if (emailphonelist != null)
            {
                foreach (var item in emailphonelist)
                {
                    var arr = item.Split(new string[] { "||" }, StringSplitOptions.None);
                    CusPhoneList.Add(arr[1]);
                    CusEmailList.Add(arr[0]);
                }
            }
        }

        public static string GetSalesmanName(UserModel item)
        {
            if (item != null)
            {
                return item.FirstName == "" && item.LastName == "" ? item.UserName : string.Concat(item.FirstName, " ", item.LastName);
            }
            return "N/A";

        }


        public static string GetAccountProfileName(MMDbContext context)
        {
            var accname = "";
            var accountprofile = context.AccountProfiles.FirstOrDefault(x => x.Id == AccountProfileId && x.CompanyId == CompanyId);
            if (accountprofile != null)
            {
                accname = accountprofile.ProfileName;
            }
            return accname;
        }

        public static int GetAccountProfileId(MMDbContext context)
        {
            return GetCurrentSession(context).AccountProfileId;
        }       

       
        public static List<UserModel> GetStaffList(MMDbContext context, SessUser sessUser)
        {
            List<UserModel> stafflist = new List<UserModel>();
            stafflist = (from u in context.SysUsers
                         where u.ManagerId == sessUser.surUID
                         select new UserModel
                         {
                             surUID = u.surUID,
                             UserCode = u.UserCode,
                             UserName = u.UserName,
                             surIsActive = u.surIsActive,
                             ManagerId = u.ManagerId,
                         }
                         ).ToList();
            return stafflist;
        }

        public static RoleType GetUserRole(SysUser user)
        {
            if (user.ManagerId > 0)
            {
                return RoleType.SalesPerson;
            }
            else
            {
                if (user.UserCode.ToLower().Contains("admin"))
                {
                    return user.UserCode.ToLower() == "superadmin" ? RoleType.SuperAdmin : RoleType.Admin;
                }
                else if (user.UserCode.ToLower().Contains("crmadmin"))
                {
                    return RoleType.CRMAdmin;
                }
                else
                {
                    return RoleType.CRMSalesManager;
                }
            }
        }




        public static void GetDataTransferData(MMDbContext context, int apId, CheckOutType checkOutType, ref DataTransferModel model, string connectionString = null)
        {
            //if (checkOutType == CheckOutType.Purchase)
            //{
            //	DateTime frmdate = model.FrmToDate;
            //	DateTime todate = model.ToDate;
            //	string location = model.SelectedLocation;

            //	if (!model.includeUploaded && model.Purchase!=null)
            //	{
            //		model.Purchase = model.Purchase.pstCheckout==false?model.Purchase:null;
            //	}

            //	model.CheckOutIds_Purchase = new HashSet<long>();

            //	if (model.Purchase!=null)
            //	{
            //		model.CheckOutIds_Purchase.Add(model.Purchase.Id);
            //	}

            //}

            if (checkOutType == CheckOutType.Suppliers)
            {
                model.Supplierlist = (from c in context.Suppliers
                                      where c.supCheckout == false && c.AccountProfileId == apId
                                      select new SupplierModel
                                      {
                                          supId = c.supId,
                                          supFirstName = c.supFirstName,
                                          supIsOrganization = !c.supIsIndividual,
                                          supAddrPhone1 = c.supAddrPhone1,
                                          supAddrPhone2 = c.supAddrPhone2,
                                          supAddrPhone3 = c.supAddrPhone3,
                                          supAddrStreetLine1 = c.supAddrStreetLine1,
                                          supAddrStreetLine2 = c.supAddrStreetLine2,
                                          supAddrStreetLine3 = c.supAddrStreetLine3,
                                          supAddrStreetLine4 = c.supAddrStreetLine4,
                                          supAddrCity = c.supAddrCity,
                                          supAddrCountry = c.supAddrCountry,
                                          supAddrWeb = c.supAddrWeb,
                                          supCode = c.supCode,
                                          supName = c.supName,
                                          supPhone = c.supPhone,
                                          supIsActive = c.supIsActive,
                                          CreateTime = c.CreateTime,
                                          ModifyTime = c.ModifyTime,
                                          supEmail = c.supEmail,
                                          supPhone1Whatsapp = c.supPhone1Whatsapp,
                                          supPhone2Whatsapp = c.supPhone2Whatsapp,
                                          supPhone3Whatsapp = c.supPhone3Whatsapp
                                      }
                                        ).ToList();
                foreach (var supplier in model.Supplierlist)
                {
                    model.CheckOutIds_Supplier.Add(supplier.supId);
                }
            }
        }


        private static List<DeviceModel> GetDeviceList4Export(MMDbContext context, int accountProfileId)
        {
            List<DeviceModel> DeviceList = new List<DeviceModel>();
            var devices = context.Devices.Where(x => x.dvcIsActive == true && x.AccountProfileId == accountProfileId).ToList();
            foreach (var d in devices)
            {
                var _device = new DeviceModel();
                _device.dvcIsActive = d.dvcIsActive;
                _device.dvcCode = d.dvcCode;
                //_device.dvcName = d.dvcName;
                _device.dvcNextRtlSalesNo = d.dvcNextRtlSalesNo;
                _device.dvcNextRefundNo = d.dvcNextRefundNo;
                _device.dvcNextDepositNo = d.dvcNextDepositNo;
                _device.dvcShop = d.dvcShop;
                DeviceList.Add(_device);
            }
            return DeviceList;
        }


        public static int GetLatestCustomerID(MMDbContext context)
        {
            var apId = GetAccountProfileId(context);
            int pgId = 0;
            int mcId = 0;
            string sql;

            if (!NonABSS)
            {
                sql = $"Select Max(cusCustomerID) From MyobCustomer Where AccountProfileId={apId}";
                mcId = context.Database.SqlQuery<int>(sql).FirstOrDefault();
            }

            return pgId > mcId ? pgId : mcId;
        }


        public static string GetAccountNumber(MMDbContext context, int accountId, int accountProfileId)
        {
            string accountNumber = string.Empty;
            Account account = null;
            account = context.Accounts.FirstOrDefault(x => x.AccountID == accountId && x.AccountProfileId == accountProfileId);
            if (account != null)
            {
                accountNumber = account.AccountNumber;
            }
            return accountNumber;

        }




        public static CommonLib.Models.TaxModel GetTaxInfo(SqlConnection connection)
        {
            CommonLib.Models.TaxModel taxModel = new CommonLib.Models.TaxModel();

            var enabletax = connection.QueryFirstOrDefault<OtherSettingsView>(@"EXEC dbo.GetTaxInfo @apId=@apId", new { apId });
            if (enabletax != null)
            {
                taxModel.EnableTax = enabletax.appVal == "1";
            }
            taxModel.TaxType = TaxType.Exclusive;
            taxModel.TIN = ComInfo.TIN;
            return taxModel;
        }
        public static CommonLib.Models.TaxModel GetTaxInfo(MMDbContext context)
        {
            CommonLib.Models.TaxModel taxModel = new CommonLib.Models.TaxModel();
            var enabletax = context.AppParams.FirstOrDefault(x => x.appParam == "EnableTax" && x.AccountProfileId == apId);
            if (enabletax != null)
            {
                taxModel.EnableTax = enabletax.appVal == "1";
            }

            taxModel.TaxType = TaxType.Exclusive;
            taxModel.TIN = ComInfo.TIN;
            return taxModel;
        }
        public static DateTime GetLastSessionTime(MMDbContext context)
        {
            var lastsess = context.Sessions.Where(x => x.sesDateFr != null && x.sesDateTo != null).OrderByDescending(x => x.sesUID).FirstOrDefault();
            if (lastsess != null)
            {
                return (DateTime)lastsess.sesTimeTo;
            }
            return DateTime.MinValue;
        }

    


        public static int GetCurrentCulture(MMDbContext context = null)
        {
            if (context == null)
            {
                using (context = new MMDbContext())
                {
                    return GetCurrentSession(context).sesLang;
                }
            }
            return GetCurrentSession(context).sesLang;

        }


        public static string GetDSNbyMyob(string myobfile)
        {
            using (var context = new MMDbContext())
            {
                var dsn = context.AccountProfiles.FirstOrDefault(x => x.ProfilePath.ToLower().Contains(myobfile.ToLower()));
                if (dsn != null)
                {
                    return dsn.DsnName.ToString();
                }
                return "";
            }
        }

        public static List<AccountProfileView> GetAccountProfiles(MMDbContext context)
        {
            return (from d in context.AccountProfiles
                    where d.IsActive == true
                    select new AccountProfileView
                    {
                        DsnName = d.DsnName,
                        //ProfilePath = d.ProfilePath,
                        ProfileName = d.ProfileName,
                        //Comment = d.Comment
                    }
                                     ).ToList();
        }

        public static AccountProfileView GetAccountProfile(int accountProfileId = 0, MMDbContext context = null)
        {
            if (context == null)
            {
                context = new MMDbContext();
            }

            return getAccountProfile(accountProfileId, context);
        }

        private static AccountProfileView getAccountProfile(int accountProfileId, MMDbContext context)
        {
            using (context)
            {
                if (accountProfileId == 0)
                {
                    Session currsess = GetCurrentSession(context);
                    accountProfileId = (int)currsess.AccountProfileId;
                }

                AccountProfileView av = (from d in context.AccountProfiles
                                         where d.IsActive == true && d.Id == accountProfileId
                                         select new AccountProfileView
                                         {
                                             DsnName = d.DsnName,
                                             //ProfilePath = d.ProfilePath,
                                             ProfileName = d.ProfileName,
                                             //Comment = d.Comment
                                         }
                                     ).FirstOrDefault();
                return av;
            }
        }

        public static string GetMyobFile()
        {
            using (var context = new MMDbContext())
            {
                Session currsess = GetCurrentSession(context);
                //return "";
                var dsnfile = context.AccountProfiles.FirstOrDefault(x => x.DsnName.ToLower().EndsWith(currsess.sesShop.ToLower()));
                return dsnfile.ProfilePath;
            }
        }

        public static ReceiptViewModel GetReceipt(MMDbContext context, CommonLib.Models.TaxModel taxModel = null)
        {
            Session currsess = GetCurrentSession(context);

            ReceiptViewModel receipt = (from r in context.Receipts
                                        where 
                        r.AccountProfileId == ComInfo.AccountProfileId
                                        select new ReceiptViewModel
                                        {
                                            Id = r.Id,
                                            HeaderTitle = r.HeaderTitle + ":" + r.HeaderTitleCN + ":" + r.HeaderTitleEng,
                                            HeaderMessage = r.HeaderMessage + ":" + r.HeaderMessageCN + ":" + r.HeaderMessageEng,
                                            FooterMessage = r.FooterMessage + ":" + r.FooterMessageCN + ":" + r.FooterMessageEng,
                                            FooterTitle1 = r.FooterTitle1 + ":" + r.FooterTitle1CN + ":" + r.FooterTitle1Eng,
                                            FooterTitle2 = r.FooterTitle2 + ":" + r.FooterTitle2CN + ":" + r.FooterTitle2Eng,
                                            FooterTitle3 = r.FooterTitle3 + ":" + r.FooterTitle3CN + ":" + r.FooterTitle3Eng,
                                            CompanyAddress = r.CompanyAddress + ":" + r.CompanyAddressCN + ":" + r.CompanyAddressEng,
                                            CompanyAddress1 = r.CompanyAddress1 + ":" + r.CompanyAddress1CN + ":" + r.CompanyAddress1Eng,
                                            CompanyName = r.CompanyName + ":" + r.CompanyNameCN + ":" + r.CompanyNameEng,
                                            CompanyPhone = r.CompanyPhone,
                                            CompanyWebSite = r.CompanyWebSite,
                                            //Disclaimer = r.Disclaimer + ":" + r.DisclaimerCN + ":" + r.DisclaimerEng
                                        }).FirstOrDefault();

            if (receipt != null)
            {
                receipt.Disclaimers = (from ri in context.ReceiptInfoes
                                       where ri.reId == receipt.Id
                                       select ri.DisclaimerTxt + ":" + ri.DisclaimerTxtCN + ":" + ri.DisclaimerTxtEng).ToList();

                receipt.PaymentTerms = (from ri in context.ReceiptInfoes
                                        where ri.reId == receipt.Id
                                        select ri.PaymentTermsTxt + ":" + ri.PaymentTermsTxtCN + ":" + ri.PaymentTermsTxtEng).ToList();

                int lang = CultureHelper.CurrentCulture;

                receipt.HeaderTitle = receipt.HeaderTitle.Split(':')[lang];
                receipt.HeaderMessage = receipt.HeaderMessage.Split(':')[lang];
                receipt.FooterMessage = receipt.FooterMessage.Split(':')[lang];
                receipt.FooterTitle1 = receipt.FooterTitle1.Split(':')[lang];
                receipt.FooterTitle2 = receipt.FooterTitle2.Split(':')[lang];
                receipt.FooterTitle3 = receipt.FooterTitle3.Split(':')[lang];
                receipt.CompanyAddress = receipt.CompanyAddress.Split(':')[lang];
                receipt.CompanyAddress1 = receipt.CompanyAddress1.Split(':')[lang];
                receipt.CompanyName = receipt.CompanyName.Split(':')[lang];

                receipt.DisclaimerTxtList = new List<string>();
                receipt.PaymentTermsTxtList = new List<string>();
                foreach (var disclaimer in receipt.Disclaimers)
                {
                    receipt.DisclaimerTxtList.Add(disclaimer.Split(':')[lang]);
                }
                foreach (var pt in receipt.PaymentTerms)
                {
                    receipt.PaymentTermsTxtList.Add(pt.Split(':')[lang]);
                }

                receipt.Disclaimer = string.Join("<br/>", receipt.DisclaimerTxtList);
            }
            return receipt;
        }

        public static Dictionary<string, string> GetDicAR(List<AccessRightModel> funcs)
        {
            int lang = CultureHelper.CurrentCulture;
            Dictionary<string, string> dicAR = new Dictionary<string, string>();

            switch (lang)
            {
                case 2:
                    foreach (var func in funcs)
                    {
                        if (!dicAR.ContainsKey(func.FuncCode))
                            dicAR.Add(func.FuncCode, func.sfnNameEng);
                    }
                    break;
                case 1:
                    foreach (var func in funcs)
                    {
                        if (!dicAR.ContainsKey(func.FuncCode))
                            dicAR.Add(func.FuncCode, func.sfnNameChs);
                    }
                    break;
                default:
                case 0:
                    foreach (var func in funcs)
                    {
                        if (!dicAR.ContainsKey(func.FuncCode))
                            dicAR.Add(func.FuncCode, func.sfnNameCht);
                    }
                    break;
            }

            return dicAR;
        }

        public static Dictionary<string, string> GetDicAR()
        {
            int lang = CultureHelper.CurrentCulture;
            if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();
            var funcs = SqlConnection.Query<AccessRightModel>(@"EXEC dbo.GetAccessRights @apId=@apId,@usercode=@usercode", new { apId, usercode = "staff01" }).ToList();
            Dictionary<string, string> dicAR = new Dictionary<string, string>();

            switch (lang)
            {
                case 2:
                    foreach (var func in funcs)
                    {
                        if (!dicAR.ContainsKey(func.FuncCode))
                            dicAR.Add(func.FuncCode, func.sfnNameEng);
                    }
                    break;
                case 1:
                    foreach (var func in funcs)
                    {
                        if (!dicAR.ContainsKey(func.FuncCode))
                            dicAR.Add(func.FuncCode, func.sfnNameChs);
                    }
                    break;
                default:
                case 0:
                    foreach (var func in funcs)
                    {
                        if (!dicAR.ContainsKey(func.FuncCode))
                            dicAR.Add(func.FuncCode, func.sfnNameCht);
                    }
                    break;
            }

            return dicAR;
        }

        public static List<DeviceModel> GetDeviceList(MMDbContext context)
        {
            return (from d in context.Devices
                    where d.dvcIsActive == true && d.AccountProfileId == ComInfo.AccountProfileId
                    select new DeviceModel
                    {
                        dvcUID = d.dvcUID,
                        dvcIsActive = d.dvcIsActive,
                        dvcCode = d.dvcCode,
                        dvcNextRtlSalesNo = d.dvcNextRtlSalesNo,
                        dvcNextRefundNo = d.dvcNextRefundNo,
                        dvcShop = d.dvcShop,
                        dvcStockLoc = d.dvcStockLoc,
                        dvcReceiptPrinter = d.dvcReceiptPrinter,
                        dvcDayEndPrinter = d.dvcDayEndPrinter,
                        dvcDefaultCusRefund = d.dvcDefaultCusRefund,
                        dvcDefaultCusSales = d.dvcDefaultCusSales,
                        dvcRmks = d.dvcRmks,
                        dvcShopName = d.dvcShopName,
                        dvcShopInfo = d.dvcShopInfo,
                        dvcName = d.dvcName,
                        dvcIP = d.dvcIP,
                        dvcInvoicePrefix = d.dvcInvoicePrefix,
                        dvcRefundPrefix = d.dvcRefundPrefix,
                        AccountProfileId = d.AccountProfileId,
                        AccountNo = (int)d.AccountNo,
                        dvcRtlSalesCode = d.dvcRtlSalesCode,
                        dvcRtlRefundCode = d.dvcRtlRefundCode,
                        dvcPurchaseRequestPrefix = d.dvcPurchaseRequestPrefix,
                        dvcPurchaseOrderPrefix = d.dvcPurchaseOrderPrefix,
                        dvcPsReturnCode = d.dvcPsReturnCode,
                        dvcRtlSalesInitNo = d.dvcRtlSalesInitNo,
                        dvcRtlRefundInitNo = d.dvcRtlRefundInitNo,
                        dvcPurchaseInitNo = d.dvcPurchaseInitNo,
                        dvcPsReturnInitNo = d.dvcPsReturnInitNo
                    }
                                                  ).ToList();
        }

        public static ComInfoView GetCompanyInfo(MMDbContext context)
        {
            return (from c in context.ComInfoes
                    select new ComInfoView
                    {
                        comName = c.comName,
                        comEmail = c.comEmail,
                        comAddress1 = c.comAddress1,
                        comAddress2 = c.comAddress2,
                        comAccountNo = c.comAccountNo
                    }
                                   ).FirstOrDefault();
        }
        public static Session GetCurrentSession(MMDbContext context = null, string usercode = "")
        {
            if (HttpContext.Current.Session["Session"] == null) //the worst case...the following code is just for Contingency measures
            {
                DateTime frmDate = DateTime.Now.Date;
                
                if (string.IsNullOrEmpty(usercode))
                {
                    Session session = (from s in context.Sessions
                                       where s.sesDateFr == frmDate && s.sesIsActive == true
                                       orderby s.sesUID descending
                                       select s
                                     ).FirstOrDefault();
                    return session;
                }
                else
                {
                    Session session = (from s in context.Sessions
                                       where s.UserCode == usercode && s.sesDateFr == frmDate && s.sesIsActive == true
                                       orderby s.sesUID descending
                                       select s
                                     ).FirstOrDefault();
                    return session;
                }
            }
            else
            {
                return HttpContext.Current.Session["Session"] as Session;
            }
        }


        public static DeviceModel GetDeviceInfo(LoginUserModel model)
        {
            DeviceModel device = new DeviceModel();
            string infotxt = MMCommonLib.CommonHelpers.FileHelper.Read(device.DeviceInfoFileName);
            if (!string.IsNullOrEmpty(infotxt))
            {
                string[] deviceinfo = infotxt.Split(new string[] { ";;" }, StringSplitOptions.None);
                string hashdevice = deviceinfo[0];
                string hashshop = deviceinfo[1];

                if (HashHelper.ComputeHash(model.SelectedDevice) == hashdevice && HashHelper.ComputeHash(model.SelectedShop) == hashshop)
                {
                    device.dvcShop = model.SelectedShop;
                    device.dvcCode = model.SelectedDevice;
                    return device;
                }
            }
            return null;
        }

        public static string GetDSNByAccountProfileId(MMDbContext context, int accountprofileId)
        {
            return context.AccountProfiles.FirstOrDefault(x => x.Id == accountprofileId).DsnName;
        }
        
        public static void WriteLog(MMDbContext context, string message, string type)
        {
            DebugLog debugLog = new DebugLog();
            writelog(message, type, ref debugLog);
            context.DebugLogs.Add(debugLog);
        }
        private static void writelog(string message, string type, ref DebugLog debugLog)
        {
            debugLog.Message = message;
            debugLog.LogType = type;
            debugLog.CreateTime = CommonHelper.GetLocalTime();
        }

        public static Dictionary<string, DeviceModel> GetDicDeviceInfo(MMDbContext context)
        {
            Dictionary<string, DeviceModel> DicDeviceInfo = new Dictionary<string, DeviceModel>();
            var devicelist = (from d in context.Devices
                              join ap in context.AccountProfiles
                              on d.AccountProfileId equals ap.Id
                              where d.dvcIsActive == true
                              select new DeviceModel
                              {
                                  AccountProfileName = ap.ProfileName,
                                  AccountNo = (int)d.AccountNo,
                                  dvcInvoicePrefix = d.dvcInvoicePrefix,
                                  dvcRefundPrefix = d.dvcRefundPrefix
                              }
                              ).ToList();

            foreach (var device in devicelist)
            {
                if (!DicDeviceInfo.ContainsKey(device.AccountProfileName))
                {
                    DicDeviceInfo.Add(device.AccountProfileName, device);
                }
            }
            return DicDeviceInfo;
        }

        public static Dictionary<int, int> GetDicAcNo(MMDbContext context)
        {
            Dictionary<int, int> DicAcNo = new Dictionary<int, int>();
            var devicelist = (from d in context.Devices
                              join ap in context.AccountProfiles
                              on d.AccountProfileId equals ap.Id
                              where d.dvcIsActive == true
                              select new DeviceModel
                              {
                                  AccountProfileId = (int)d.AccountProfileId,
                                  AccountNo = (int)d.AccountNo
                              }
                              ).ToList();

            foreach (var device in devicelist)
            {
                if (!DicAcNo.ContainsKey(device.AccountProfileId))
                {
                    DicAcNo.Add(device.AccountProfileId, (int)device.AccountNo);
                }
            }
            return DicAcNo;
        }


        public static int GetAccountProfileIdByDSN(MMDbContext context, string dsn)
        {
            return context.AccountProfiles.FirstOrDefault(x => x.DsnName.ToLower() == dsn.ToLower()).Id;
        }



        public static int GetPosAdminID(DeviceModel device)
        {
            return -1;
        }

        public static AbssConn GetCompanyProfiles(int selectedProfileId)
        {
            using var context = new MMDbContext();
            var dto = (from a in context.AccountProfiles
                       join c in context.ComInfoes
                       on a.Id equals c.AccountProfileId
                       where a.Id == selectedProfileId && a.IsActive == true
                       select new AccountProfileDTO
                       {
                           Id = a.Id,
                           ProfileName = a.ProfileName,
                           CompanyId = a.CompanyId,
                           MYOBDriver = c.MYOBDriver,
                           MYOBUID = c.MYOBUID,
                           MYOBPASS = c.MYOBPASS,
                           MYOBDB = c.MYOBDb,
                           MYOBKey = c.MYOBKey,
                           MYOBExe = c.MYOBExe
                       }
                          ).FirstOrDefault();
            AbssConn abssConn = new AbssConn
            {
                Database = dto.MYOBDB,
                Driver = dto.MYOBDriver,
                UserId = dto.MYOBUID,
                Password = dto.MYOBPASS,
                KeyLocation = dto.MYOBKey,
                ExeLocation = dto.MYOBExe
            };
            return abssConn;
        }
   

        public static bool SendNotificationEmail(string pstCode, Dictionary<string, string> DicReviewUrl, ReactType reactType, string desc = null, string rejectonholdreasonremark = null, string suppernames = null, SupplierModel selectedSupplier = null, int isThreshold = 0, List<Inferior> inferiors = null)
        {
            int okcount = 0;
            int ngcount = 0;

            EmailEditModel model = new EmailEditModel();
            var mailsettings = model.Get();

            MailAddress frm = new MailAddress(mailsettings.emEmail, mailsettings.emDisplayName);

            bool addbc = int.Parse(ConfigurationManager.AppSettings["AddBccToDeveloper"]) == 1;
            MailAddress addressBCC = new MailAddress(ConfigurationManager.AppSettings["DeveloperEmailAddress"], ConfigurationManager.AppSettings["DeveloperEmailName"]);
            MailMessage message = new()
            {
                From = frm
            };
            if (addbc)
            {
                message.Bcc.Add(addressBCC);
            }

            var ccname = ConfigurationManager.AppSettings["CCEmailAddress"].ToString().Split(',')[0];
            var ccmail = ConfigurationManager.AppSettings["CCEmailAddress"].ToString().Split(',')[1];
            message.CC.Add(new MailAddress(ccmail, ccname));

            message.Subject = string.Format(Resource.PendingApprovalFormat, Resource.PurchaseOrder);
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;

            string approvaltxt = reactType == ReactType.Rejected ? Resource.Rejected : Resource.Approval;

            while (okcount == 0)
            {
                if (ngcount >= mailsettings.emMaxEmailsFailed || okcount > 0) break;

                string mailbody = string.Empty;
                string orderlnk;
                string strorder = string.Concat("<strong>", string.Format(Resource.RequestFormat, Resource.Purchase), "</strong>");
                string orderdesc = getOrderDesc(desc, pstCode);

                if (isThreshold == 1)
                {
                    if (SqlConnection.State == ConnectionState.Closed) SqlConnection.Open();

                    if (reactType == ReactType.PassedToMuseumDirector)
                    {
                        var mdInfo = SqlConnection.Query<UserModel>($"EXEC dbo.GetMDInfo @apId=@apId", new { apId }).ToList();
                        message.Subject = string.Format(Resource.PendingReviewFormat, Resource.PurchaseOrder);

                        foreach (var md in mdInfo)
                        {
                            var reviewurl = UriHelper.GetReviewPurchaseOrderUrl(ConfigurationManager.AppSettings["ReviewPurchaseOrderBaseUrl"], pstCode, 0, md.surUID);
                            orderlnk = getOrderLnk(reviewurl, pstCode);
                            mailbody = EnableReviewUrl ? string.Format(Resource.RequestWLinkHtmlFormat, md.UserName, strorder, approvaltxt, orderlnk) : string.Format(Resource.RequestHtmlFormat, md.UserName, strorder, approvaltxt);
                            mailbody = string.Concat(mailbody, orderdesc);
                            sendMail(ref okcount, ref ngcount, mailsettings, message, ref mailbody);
                        }

                    }
                    if (reactType == ReactType.PassedToDirectorBoard)
                    {
                        var dbInfo = SqlConnection.Query<UserModel>($"EXEC dbo.GetDBInfo @apId=@apId", new { apId }).ToList();
                        var rejectonholdreasonremarktxt = string.Concat("<strong>", Resource.MsgToDB, ":</strong>");

                        message.Subject = string.Format(Resource.PendingReviewFormat, Resource.PurchaseOrder);

                        foreach (var db in dbInfo)
                        {
                            var reviewurl = UriHelper.GetReviewPurchaseOrderUrl(ConfigurationManager.AppSettings["ReviewPurchaseOrderBaseUrl"], pstCode, 0, db.surUID);
                            orderlnk = getOrderLnk(reviewurl, pstCode);
                            /*
                             * <h3>Hi {0}</h3><p>The following <strong>{1}</strong> is pending for your review:</p>{2}<h4>{3}</h4><p>{4}</p>
                             */
                            mailbody = string.Format(Resource.MsgToDBHtmlFormat, db.UserName, strorder, orderdesc, string.Concat(rejectonholdreasonremarktxt, " ", rejectonholdreasonremark));
                            mailbody = string.Concat(mailbody, $"<p><strong>{string.Format(Resource.SelectedFormat, Resource.Vendor)}:</strong>", " ", selectedSupplier.supName, "</p>");
                            sendMail(ref okcount, ref ngcount, mailsettings, message, ref mailbody);
                        }
                    }
                    if (reactType == ReactType.ApprovedByDirectorBoard)
                    {
                        var mdInfo = SqlConnection.Query<UserModel>($"EXEC dbo.GetMDInfo @apId=@apId", new { apId }).ToList();
                        message.Subject = string.Format(Resource.ApprovedFormat, Resource.PurchaseOrder);
                        foreach (var md in mdInfo)
                        {
                            mailbody = string.Concat("<h3>Hi ", md.UserName, "</h3>", string.Format(Resource.ApprovedByDBPoMsgFormat, strorder, string.Concat("<strong>", pstCode, "</strong>"), string.Concat("<strong>", selectedSupplier.supName, "</strong>")));
                            sendMail(ref okcount, ref ngcount, mailsettings, message, ref mailbody);
                        }
                    }
                    
                    if (reactType == ReactType.ApprovedByMuseumDirector)
                    {                        
                        message.Subject = string.Format(Resource.ApprovedFormat, Resource.PurchaseOrder);

                        foreach (var inferior in inferiors)
                        {
                            mailbody = string.Concat("<h3>Hi ", inferior.UserName, "</h3>", string.Format(Resource.ApprovedByDBPoMsgFormat, strorder, string.Concat("<strong>", pstCode, "</strong>"), string.Concat("<strong>", selectedSupplier.supName, "</strong>")));
                            sendMail(ref okcount, ref ngcount, mailsettings, message, ref mailbody);
                        }
                    }
                }
                else
                {
                    foreach (var item in DicReviewUrl)
                    {
                        var arr = item.Key.Split(':');
                        var name = arr[0];
                        var email = arr[1];
                        var ordercode = arr[2];
                        orderlnk = getOrderLnk(item.Value, ordercode);
                        orderdesc = getOrderDesc(desc, ordercode);
                        message.To.Add(new MailAddress(email, name));

                        if (reactType == ReactType.RequestingByStaff || reactType == ReactType.RequestingByDeptHead || reactType == ReactType.RequestingByFinanceDept)
                        {
                            mailbody = EnableReviewUrl ? string.Format(Resource.RequestWLinkHtmlFormat, name, strorder, approvaltxt, orderlnk) : string.Format(Resource.RequestHtmlFormat, name, strorder, approvaltxt);
                        }

                        if (reactType == ReactType.PassedByDeptHead || reactType == ReactType.PassedByFinanceDept)
                        {
                            mailbody = EnableReviewUrl ? string.Format(Resource.RespondWLinkHtmlFormat, name, strorder, approvaltxt, orderlnk) : string.Format(Resource.RespondHtmlFormat, name, strorder, approvaltxt);
                        }
                        if (reactType == ReactType.Approved || reactType == ReactType.ApprovedByMuseumDirector || reactType == ReactType.PassedToDirectorBoard)
                        {
                            message.Subject = string.Format(Resource.ApprovedFormat, Resource.PurchaseOrder);
                            mailbody = EnableReviewUrl ? string.Format(Resource.ApprovedPoMsgWLnkFormat, strorder, suppernames, orderlnk) : string.Format(Resource.ApprovedPoMsgFormat, strorder, suppernames);
                            mailbody = string.Concat(mailbody, $"<p><strong>{string.Format(Resource.SelectedFormat, Resource.Vendor)}:</strong>", " ", selectedSupplier.supName, "</p>");
                        }
                        if (reactType == ReactType.Rejected || reactType == ReactType.RejectedByDeptHead || reactType == ReactType.RejectedByFinanceDept)
                        {
                            message.Subject = string.Format(Resource.PendingReviewFormat, Resource.PurchaseOrder);
                            var rejectonholdreasonremarktxt = string.Format(Resource.ReasonForFormat, Resource.Reject) + ":";
                            mailbody = string.Format(Resource.RejectHtmlFormat, name, strorder, ordercode, rejectonholdreasonremarktxt, rejectonholdreasonremark);
                        }
                        if (reactType == ReactType.OnHoldByDirector || reactType == ReactType.OnHoldByFinanceDept)
                        {
                            message.Subject = string.Format(Resource.PendingReviewFormat, Resource.PurchaseOrder);
                            var rejectonholdreasonremarktxt = string.Format(Resource.ReasonForFormat, Resource.OnHold) + ":";
                            mailbody = string.Format(Resource.OnHoldHtmlFormat, name, strorder, ordercode, rejectonholdreasonremarktxt, rejectonholdreasonremark);
                        }

                        mailbody = string.Concat(mailbody, orderdesc);

                        sendMail(ref okcount, ref ngcount, mailsettings, message, ref mailbody);
                    }
                }


            }
            return okcount > 0;

            static void sendMail(ref int okcount, ref int ngcount, EmailModel mailsettings, MailMessage message, ref string mailbody)
            {

                message.Body = mailbody;
                using (SmtpClient smtp = new SmtpClient(mailsettings.emSMTP_Server, mailsettings.emSMTP_Port))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.EnableSsl = mailsettings.emSMTP_EnableSSL;
                    smtp.Credentials = new NetworkCredential(mailsettings.emSMTP_UserName, mailsettings.emSMTP_Pass);
                    try
                    {
                        smtp.Send(message);
                        okcount++;
                    }
                    catch (Exception)
                    {
                        ngcount++;
                    }
                }
            }

            static string getOrderLnk(string lnk, string ordercode)
            {
                return $"<a href='{lnk}' target='_blank'>{ordercode}</a>";
            }

            static string getOrderDesc(string desc, string ordercode)
            {
                return $"<p><strong>{string.Format(Resource.RequestFormat, Resource.Procurement)}</strong>: {ordercode}</p><p><strong>{Resource.Description}</strong>: {desc}</p>";
            }
        }
       
        public static Dictionary<string, double> GetDicCurrencyExRate(MMDbContext context)
        {
            var DicCurrencyExRate = new Dictionary<string, double>();
            var myobcurrencylist = context.GetMyobCurrencyList(apId).ToList();
            if (myobcurrencylist != null && myobcurrencylist.Count > 0)
            {

                foreach (var currency in myobcurrencylist)
                {
                    DicCurrencyExRate[currency.CurrencyCode] = currency.ExchangeRate ?? 1;
                }
            }
            return DicCurrencyExRate;
        }
        public static Dictionary<string, double> GetDicCurrencyExRate(SqlConnection connection)
        {
            var DicCurrencyExRate = new Dictionary<string, double>();
            var myobcurrencylist = connection.Query<MyobCurrencyModel>(@"EXEC dbo.GetMyobCurrencyList @apId=@apId", new { apId }).ToList();
            if (myobcurrencylist != null && myobcurrencylist.Count > 0)
            {

                foreach (var currency in myobcurrencylist)
                {
                    DicCurrencyExRate[currency.CurrencyCode] = currency.ExchangeRate ?? 1;
                }
            }
            return DicCurrencyExRate;
        }



        public static string GetAbssConnectionString(MMDbContext context, string accesstype, int apId)
        {
            return MYOBHelper.GetConnectionString(context, accesstype, apId);
        }

        public static void HandleViewFile(string filepath, int apId, string invoiceId, string type, long? lineId, long? payId, ref List<string> ImgList, ref InvoiceFileData filedata)
        {
            if (!string.IsNullOrEmpty(filepath))
            {
                var filelnk = type=="line"? Path.Combine(apId.ToString(), invoiceId, lineId.ToString(), filepath): Path.Combine(apId.ToString(), invoiceId, payId.ToString(), filepath);
                if (CommonHelper.ImageExtensions.Contains(Path.GetExtension(filepath).ToUpperInvariant()))
                {
                    filelnk = $"<a class='imglnk' href='#' data-lnk='{filelnk}'><img src='{UriHelper.GetBaseUrl()}/{filelnk}'/></a>";
                    ImgList.Add(filelnk);
                }
                else
                {
                    string filename = Path.GetFileName(filepath);
                    filedata.FilePath = $"<a class='filelnk' href='#' data-lnk='{filelnk}' data-name='{filename}' data-invoiceid='{filedata.InvoiceId}' data-fileid='{filedata.FileId}'>{{0}}{filename}</a><i class='mx-2 fa-solid fa-trash removefile pointer' data-name='{filename}' data-invoiceid='{filedata.InvoiceId}' data-fileid='{filedata.FileId}'></i>";
                }
            }
        }

        public static void HandleViewFile(string filepath, int apId, string filecode, ref List<string> ImgList, ref FileData filedata)
        {
            if (!string.IsNullOrEmpty(filepath))
            {
                var filelnk = Path.Combine(apId.ToString(), filecode, filepath);
                if (CommonHelper.ImageExtensions.Contains(Path.GetExtension(filepath).ToUpperInvariant()))
                {
                    filelnk = $"<a class='imglnk' href='#' data-lnk='{filelnk}'><img src='{UriHelper.GetBaseUrl()}/{filelnk}'/></a>";
                    ImgList.Add(filelnk);
                }
                else
                {
                    string filename = Path.GetFileName(filepath);
                    filedata.FilePath = $"<a class='filelnk' href='#' data-lnk='{filelnk}' data-name='{filename}' data-id='{filedata.Id}'>{{0}}{filename}</a><i class='mx-2 fa-solid fa-trash removefile pointer' data-id='{filedata.Id}' data-name='{filename}'></i>";
                }
            }
        }
        public static Dictionary<string, List<AccountModel>> GetDicAcAccounts(SqlConnection connection)
        {
            var DicAcAccounts = new Dictionary<string, List<AccountModel>>();
            var acIdList = connection.Query<string>(@"EXEC dbo.GetAccountClassificationIDList @apId=@apId", new { apId }).ToList();
            foreach (var ac in acIdList)
            {
                DicAcAccounts[ac] = new List<AccountModel>();
            }
            List<AccountModel> AccountList = connection.Query<AccountModel>(@"EXEC dbo.GetAccountList @apId=@apId", new { apId }).ToList();
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
            return DicAcAccounts;
        }
    }

    public class VtTotalQty
    {
        public string Id { get; set; }
        public int totalVtQty { get; set; }
    }
    public class BatTotalQty
    {
        public string batCode { get; set; }
        public string batItemCode { get; set; }
        public int totalBatQty { get; set; }
    }

    public class IvTotalQty
    {
        public string ivIdList { get; set; }
        public string itmCode { get; set; }
        public int totalIvQty { get; set; }
    }

    public class VtDelQty
    {
        public string vt { get; set; }
        public int delqty { get; set; }
        public string pocode { get; set; }
        public int? seq { get; set; }
        public string itemcode { get; set; }
        public int vtId { get; set; }
        public long vtdelId { get; set; }
        public string type { get; set; }
        public string salescode { get; set; }
    }
    public class VtQty
    {
        public string vt { get; set; }
        public int qty { get; set; }
        public string pocode { get; set; }
        public int? vtseq { get; set; }
        public string itemcode { get; set; }
        public int? delqty { get; set; }
        public int sellableqty { get; set; }
        public int vtId { get; set; }
    }
    public class IvQty
    {
        public string Id { get; set; }
        public string ivIdList { get; set; }
        public int qty { get; set; }
        public string pocode { get; set; }
        public int? ivseq { get; set; }
        public string itemcode { get; set; }
        public int? delqty { get; set; }
        public int sellableqty { get; set; }
    }
    public class IvDelQty : IvQty
    {

        public int? seq { get; set; }
        //public string Id { get; set; }
    }
    public class BatchVtQty
    {
        public string batchcode { get; set; }
        public string validthru { get; set; }
        public int batchqty { get; set; }
    }

    public class BatDelQty
    {
        public string batcode { get; set; }
        public int? batdelqty { get; set; }
        public int batqty { get; set; }
        public int? seq { get; set; }
        public int batId { get; set; }
        public long batdelId { get; set; }
        public string itemcode { get; set; }
        public string pocode { get; set; }
        public DateTime? batVt { get; set; }
        public string batSn { get; set; }
        public string VtDisplay { get { return batVt == null ? "" : CommonHelper.FormatDate((DateTime)batVt, true); } }

        public string ivIdList { get; set; }
        public string type { get; set; }
        public string salescode { get; set; }
    }
    public class BatchQty
    {
        public string batcode { get; set; }
        public int batqty { get; set; }
        public int sellableqty { get; set; }
        public string itemcode { get; set; }
        public int? delqty { get; set; }
        public int? seq { get; set; }
        public int batId { get; set; }
        public string pocode { get; set; }
    }

    public class PoBatVQ
    {
        public string pocode { get; set; }
        public string batchcode { get; set; }
        public int batchqty { get; set; }
        public string vt { get; set; }
        public string id { get; set; }
        public int sellableqty { get; set; }
        public int? seq { get; set; }
        public int batId { get; set; }
    }
    public class PoItemBatVQ : PoBatVQ
    {
        public string itemcode { get; set; }
    }

    public class SeqSnVtList
    {
        public int seq { get; set; }
        public string sn { get; set; }
        public List<string> vtlist { get; set; }
    }

    public class InvoiceFileData()
    {
        public long? lineId { get; set; }
        public long? payId { get; set; }
        public string InvoiceId { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string ImgPath { get; set; }
        public long FileId { get; set; }
    }
    
    public class FileData()
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string ImgPath { get; set; }
    }
    public enum CusFollowUpStatus
    {
        need = 1,
        noneed = 0,
        completed = 2
    }
}
