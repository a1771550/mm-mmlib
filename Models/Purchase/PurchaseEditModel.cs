using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CommonLib.Helpers;
using CommonLib.Models;
using MMDAL;
using Resources = CommonLib.App_GlobalResources;
using Device = MMDAL.Device;
using Purchase = MMDAL.Purchase;
using System.Configuration;
using MMLib.Models.POS.Settings;
using Microsoft.Data.SqlClient;
using Dapper;
using MMLib.Models.MYOB;
using MMLib.Models.User;
using System.Text.Json;
using CommonLib.BaseModels;
using MMLib.Models.Supplier;
using ModelHelper = MMLib.Helpers.ModelHelper;
using PagedList;

namespace MMLib.Models.Purchase
{
    public class PurchaseEditModel : PagingBaseModel
    {
        //Reasons4ChoosingSupplier
        public List<SelectListItem> Reasons4ChoosingSupplier
        {
            get
            {
                var list = ConfigurationManager.AppSettings["Reasons4ChoosingSupplier"].Split('|').ToList();
                var selectlist = new List<SelectListItem>();
                int idx = 0;
                foreach (var item in list)
                {
                    idx++;
                    selectlist.Add(new SelectListItem
                    {
                        Value = idx.ToString(),
                        Text = item
                    });
                }
                return selectlist;
            }
        }
        public List<SelectListItem> NotEnoughQuoReasons
        {
            get
            {
                var list = ConfigurationManager.AppSettings["NotEnoughQuoReasons"].Split('|').ToList();
                var selectlist = new List<SelectListItem>();
                int idx = 0;
                foreach (var item in list)
                {
                    idx++;
                    selectlist.Add(new SelectListItem
                    {
                        Value = idx.ToString(),
                        Text = item
                    });
                }
                return selectlist;
            }
        }
        public List<SelectListItem> FundingSources { get {
                var list = ConfigurationManager.AppSettings["FundingSources"].Split('|').ToList();
                var selectlist = new List<SelectListItem>();
                int idx = 0;
                foreach(var item in list)
                {
                    idx++;
                    selectlist.Add(new SelectListItem
                    {
                        Value = idx.ToString(),
                        Text = item
                    });
                }
                return selectlist;
            } 
        }
      
        public bool ThresholdRequestApproved { get; set; } = false;
        public IsUserRole IsUserRole { get { return UserEditModel.GetIsUserRole(user); } }
        public bool IsMD { get { return IsUserRole.ismuseumdirector; } }
        public bool IsDB { get { return IsUserRole.isdirectorboard; } }
        public string ProcurementPersonName { get; set; }
        
        public PurchaseOrderReviewModel PurchaseOrderReview { get; set; }
        
        public Dictionary<string, string> DicLocation { get; set; }
        public Dictionary<string, double> DicCurrencyExRate { get; set; }
        public string JsonDicCurrencyExRate { get { return JsonSerializer.Serialize(DicCurrencyExRate); } }
        public string JsonDicLocation { get { return DicLocation == null ? "" : JsonSerializer.Serialize(DicLocation); } }

       
        public List<SelectListItem> LocationList { get; set; }
       

       

       
        public bool Readonly { get; set; }
        public bool DoApproval { get; set; }
        static string PurchaseType = "PS";
        public PurchaseStatus ListMode { get; set; }
        public PurchaseModel Purchase { get; set; }

        public ReceiptViewModel Receipt;
        public List<string> DisclaimerList;
        public List<string> PaymentTermsList;

        public List<PurchaseModel> PSList { get; set; }
        public IPagedList<PurchaseModel> PagingPSList { get; set; }
        public string PrintMode { get; set; }
        public PoSettings PoSettings { get; set; }
        public static List<Superior> SuperiorList { get; set; }

        //public List<SupplierPaymentModel> InvoicePayments { get; set; }

        public PurchaseEditModel()
        {
            DicCurrencyExRate = new Dictionary<string, double>();
            DicLocation = new Dictionary<string, string>();
        
          
        }

       

        public PurchaseEditModel(string pstCode, int? ireadonly, int? idoapproval) : this()
        {
            Get(0, pstCode, ireadonly, idoapproval);
        }
        public PurchaseEditModel(long Id, string status = "", int? ireadonly = 0, int? idoapproval = 1, bool forprint = false) : this()
        {
            Get(Id, null, ireadonly, idoapproval, status, forprint);
        }

        public void Get(long Id = 0, string pstCode = null, int? ireadonly = 0, int? idoapproval = 1, string status = "", bool forprint = false)
        {
            bool isapprover = (bool)HttpContext.Current.Session["IsApprover"];
            using var context = new MMDbContext();

            Purchase = new PurchaseModel();
            Device device = null;
            if (!isapprover) device = context.Devices.First();

            var connection = new SqlConnection(defaultConnection);
            connection.Open();

            DateTime dateTime = DateTime.Now;

            if (Id > 0 || !string.IsNullOrEmpty(pstCode))
            {
                Purchase = connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id,@code=@code", new { apId, Id, code = pstCode });

                //GetPurchaseServiceList
                //Purchase.ServiceList = connection.Query<PurchaseServiceModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id,@code=@code", new { apId, Id, code = pstCode }).ToList();

                Readonly = ireadonly == 1;
                DoApproval = idoapproval == 1;

                if (forprint)
                {
                    Receipt = new ReceiptViewModel();
                    DisclaimerList = new List<string>();
                    PaymentTermsList = new List<string>();
                    ModelHelper.GetReady4Print(context, ref Receipt, ref DisclaimerList, ref PaymentTermsList);
                }

                PoSettings = PoSettingsEditModel.GetPoSettings(connection);

                Purchase.IsEditMode = true;

               

                if (Purchase.pstStatus.ToLower() == PurchaseStatus.order.ToString())
                {
                    

                   

                  

                }

               
            }
            else
            {
                var purchaseinitcode = device.dvcPurchaseRequestPrefix;
                var purchaseno = $"{device.dvcNextPurchaseRequestNo:000000}";
                device.dvcNextPurchaseRequestNo++;
                device.dvcModifyTime = dateTime;
                string pqstatus = RequestStatus.requestingByStaff.ToString();
                string pstcode = string.Concat(purchaseinitcode, purchaseno);

                Purchase.pstStatus = PurchaseStatus.requesting.ToString();
                //var latestpurchase = context.Purchases.OrderByDescending(x => x.Id).FirstOrDefault();
                //if (latestpurchase != null)
                //{
                //    if (string.IsNullOrEmpty(latestpurchase.pstType)) PopulatePurchaseModel(latestpurchase.pstCode, latestpurchase.pstStatus, RequestStatus.requestingByStaff.ToString(), latestpurchase.Id);
                //    else addNewPurchase(context, apId, pstcode, status, latestpurchase.IsThreshold, pqstatus);
                //}
                //else addNewPurchase(context, apId, pstcode, status, false, pqstatus);
            }

           

            var stocklocationlist = context.GetStockLocationList1(ComInfo.AccountProfileId).ToList();
            LocationList = new List<SelectListItem>();
            foreach (var item in stocklocationlist)
            {
                LocationList.Add(new SelectListItem
                {
                    Value = item,
                    Text = item
                });
            }

            var myobcurrencylist = context.MyobCurrencies.Where(x => x.AccountProfileId == ComInfo.AccountProfileId).ToList();
            if (myobcurrencylist != null && myobcurrencylist.Count > 0)
            {
                DicCurrencyExRate = new Dictionary<string, double>();
                foreach (var currency in myobcurrencylist)
                {
                    DicCurrencyExRate[currency.CurrencyCode] = currency.ExchangeRate ?? 1;
                }
            }

            Purchase.pstPurchaseDate = DateTime.Now.Date;
            Purchase.TaxModel = ModelHelper.GetTaxInfo(context);
            Purchase.UseForexAPI = ExchangeRateEditModel.GetForexInfo(context);

            DicLocation = new Dictionary<string, string>();
            foreach (var shop in stocklocationlist)
            {
                DicLocation[shop] = shop;
            }

            PoSettings = PoSettingsEditModel.GetPoSettings(connection);

            Purchase.Mode = ireadonly == 1 ? "readonly" : "";

           
        }

        private string GenInvoiceId(string pstCode, int numId)
        {
            return CommonHelper.GenOrderNumber(pstCode, int.Parse(ConfigurationManager.AppSettings["SupInvoiceLength"]), numId);
        }

        private long addNewPurchase(MMDbContext context, int apId, string pstcode, string status, bool isThreshold, string pqstatus = null, bool addNewModel = true, string oldpstCode = null)
        {
            DateTime dateTime = DateTime.Now;
            MMDAL.Purchase ps = new()
            {
                pstCode = pstcode,
                pstRefCode = oldpstCode,
                pstPurchaseDate = dateTime.Date,
                pstPurchaseTime = dateTime,
                pstPromisedDate = dateTime.AddDays(1),
                pstStatus = status,
                IsThreshold = isThreshold,
                AccountProfileId = apId,
                CreateTime = dateTime,
                CreateBy = user.UserCode,
                pstCheckout = false,
                pstIsPartial = false,
                pqStatus = pqstatus,
            };
            ps = context.Purchases.Add(ps);
            context.SaveChanges();

            if (addNewModel) PopulatePurchaseModel(pstcode, status, pqstatus, ps.Id);

            return ps.Id;
        }

        private void PopulatePurchaseModel(string pstcode, string status, string pqstatus, long Id)
        {
            Purchase = new PurchaseModel
            {
                Id = Id,
                IsEditMode = false,
                pstCode = pstcode,
                pstStatus = status,
                pqStatus = pqstatus,
                CreateBy = user.UserName,
            };
        }


        public List<PurchaseModel> GetList(SessUser user, string strfrmdate, string strtodate, string keyword, PurchaseStatus type = PurchaseStatus.all)
        {
            /* Dates Handling */
            SearchDates = new SearchDates();
            CommonHelper.GetSearchDates(strfrmdate, strtodate, ref SearchDates);
            DateFromTxt = SearchDates.DateFromTxt;
            DateToTxt = SearchDates.DateToTxt;
            /******************/
            //using var context = new MMDbContext();         
            using var connection = new SqlConnection(DefaultConnection);
            connection.Open();
            PSList = new List<PurchaseModel>();

            PSList = connection.Query<PurchaseModel>(@"EXEC dbo.GetPurchaseList5 @apId=@apId,@username=@username,@frmdate=@frmdate,@todate=@todate,@keyword=@keyword", new { apId = ComInfo.AccountProfileId, username = user.UserName, SearchDates.frmdate, SearchDates.todate, keyword }).ToList();

            ListMode = type;
            return PSList;
        }

        public static List<PurchaseReturnMsg> Edit(PurchaseModel model)
        {
            List<PurchaseReturnMsg> msglist = [];
            string status = "";
            string msg = string.Format(Resources.Resource.SavedFormat, Resources.Resource.PurchaseOrder);
            string purchasestatus = string.Empty;


            IsUserRole IsUserRole = UserEditModel.GetIsUserRole(user);

            DeviceModel dev = HttpContext.Current.Session["Device"] as DeviceModel;

            using var context = new MMDbContext();
            DateTime dateTime = DateTime.Now;

            purchasestatus = model.pstStatus.ToLower() == PurchaseStatus.draft.ToString()
                ? PurchaseStatus.draft.ToString()
                : IsUserRole.isdirectorboard ? model.pstStatus : !model.IsEditMode ? PurchaseStatus.requesting.ToString() : model.pstStatus;

            List<string> reviewurls = new();
            List<GetSuperior4Notification2_Result> superiors = new();
            Dictionary<string, string> DicReviewUrl = new Dictionary<string, string>();
            Dictionary<string, Dictionary<string, int>> dicItemLocQty = new Dictionary<string, Dictionary<string, int>>();

            sqlConnection.Open();
            SuperiorList = sqlConnection.Query<Superior>(@"EXEC dbo.GetSuperior4Notification2 @apId=@apId,@userId=@userId,@shopcode=@shopcode", new { apId, userId = user.surUID, shopcode = comInfo.Shop }).ToList();

            foreach (var superior in SuperiorList)
            {
                var reviewurl = UriHelper.GetReviewPurchaseOrderUrl(ConfigurationManager.AppSettings["ReviewPurchaseOrderBaseUrl"], model.pstCode, 0, superior.surUID);
                var key = string.Concat(superior.UserName, ":", superior.Email, ":", model.pstCode);
                if (!DicReviewUrl.ContainsKey(key))
                    DicReviewUrl[key] = reviewurl;
                reviewurls.Add(reviewurl);
            }

            //if (!model.IsEditMode)
            //{
            //    updatePurchaseRequest(model, context, purchasestatus);
            //}
            //else
            //{
                AddPurchase(model, dev, context, purchasestatus);
            //}

            status = "purchaseordersaved";
            if (model.pstStatus.ToLower() != PurchaseStatus.draft.ToString().ToLower() && model.pstStatus.ToLower() != PurchaseStatus.order.ToString())
            {
                HandlingPurchaseOrderReview(model.pstCode, model.pqStatus, context);

                if (!IsUserRole.isdirectorboard && !IsUserRole.ismuseumdirector)
                {
                    #region Send Notification Email   
                    if ((bool)comInfo.enableEmailNotification)
                    {
                        ReactType reactType = ReactType.RequestingByStaff;
                        if (IsUserRole.isfinancedept) reactType = ReactType.PassedByFinanceDept;
                        if (IsUserRole.ismuseumdirector || IsUserRole.isdirectorboard) reactType = ReactType.Approved;
                        if (ModelHelper.SendNotificationEmail(model.pstCode, DicReviewUrl, reactType, model.pstDesc))
                        {
                            var purchase = context.Purchases.FirstOrDefault(x => x.Id == model.Id);
                            purchase.pstSendNotification = true;
                            context.SaveChanges();
                        }
                    }
                    #endregion
                }
                GenReturnMsgList(model, ref msglist, SuperiorList, status, IsUserRole.isdirectorboard, DicReviewUrl, IsUserRole.ismuseumdirector);
            }
            return msglist;
        }

        private static void AddPurchase(PurchaseModel model, DeviceModel device, MMDbContext context, string purchasestatus)
        {
            var dateTime = DateTime.Now;
            var purchaseinitcode = device.dvcPurchaseRequestPrefix;
            var purchaseno = $"{device.dvcNextPurchaseRequestNo:000000}";
            device.dvcNextPurchaseRequestNo++;
            device.dvcModifyTime = dateTime;
            string pqstatus = RequestStatus.requestingByStaff.ToString();
            string pstcode = string.Concat(purchaseinitcode, purchaseno);
            var status = PurchaseStatus.requesting.ToString();
            context.Purchases.Add(new MMDAL.Purchase
            {
                pstCode = pstcode,
                pstType = PurchaseType,
                pstDesc = model.pstDesc,
                pstPurchaseDate = dateTime.Date,
                pstPurchaseTime = dateTime,
                pstStatus = status,
                pqStatus =pqstatus,
                pstExpectedCostDesc = model.pstExpectedCostDesc,
                //pstCurrency = comInfo.Currency,
                pstFundingSource = model.pstFundingSource,
                pstFSOthers = model.pstFSOthers,
                pstPreApprovedMsg = model.pstPreApprovedMsg,
                pstRationnale = model.pstRationnale,
                pstVendorSummary = model.pstVendorSummary,
                pstMDReview = model.pstMDReview,
                pstQuotationNum = model.pstQuotationNum,
                pstChooseVendorReason = model.pstChooseVendorReason,
                pstContractualProtection= model.pstContractualProtection,
                pstQuotationInvitationNum = model.pstQuotationInvitationNum,
                pstNoInvitationReason = model.pstNoInvitationReason,
                pstQuotationReceivedNum = model.pstQuotationReceivedNum,
                pstReasonsNotEnoughQuotation = model.pstReasonsNotEnoughQuotation,
                pstOtherReason4NotEnoughQuotation = model.pstOtherReason4NotEnoughQuotation,
                pstReasons4ChoosingFinalSupplier = model.pstReasons4ChoosingFinalSupplier,
                pstOtherReason4ChoosingFinalSupplier = model.pstOtherReason4ChoosingFinalSupplier,
                IsThreshold = model.IsThreshold,
                pstCheckout = false,
                AccountProfileId = apId,
                CreateBy = user.UserCode,
                CreateTime = dateTime
            });
            context.SaveChanges();
        }

        private static void GenReturnMsgList(PurchaseModel model, ref List<PurchaseReturnMsg> msglist, List<Superior> SuperiorList, string status, bool isdirectorboard, Dictionary<string, string> DicReviewUrl, bool ismuseumdirector, string msg = null)
        {
            foreach (var superior in SuperiorList)
            {
                if (string.IsNullOrEmpty(msg))
                    msg = (!isdirectorboard && !ismuseumdirector) ? string.Format(Resources.Resource.NotificationEmailWillBeSentToFormat, superior.UserName) : Resources.Resource.OrderSavedSuccessfully;

                var key = string.Concat(superior.UserName, ":", superior.Email, ":", model.pstCode);
                msglist.Add(new PurchaseReturnMsg
                {
                    msg = msg,
                    status = status,
                    superiorphone = superior.Phone,
                    purchasecode = model.pstCode,
                    reviewurl = DicReviewUrl[key],
                    superioremail = superior.Email,
                    isdirectorboard = isdirectorboard,
                    remark = model.pstRemark
                });
            }
        }

        //private static void processPurchase(PurchaseModel model, DeviceModel dev, MMDbContext context, string purchasestatus)
        //{
        //    MMDAL.Purchase ps = updatePurchaseRequest(model, context, purchasestatus);

        //    if (purchasestatus.ToLower() == PurchaseStatus.order.ToString())
        //    {
        //        #region Update Purchase Status
        //        // update wholesales status:                
        //        ps.pstStatus = PurchaseStatus.order.ToString();
        //        ps.ModifyTime = DateTime.Now;
        //        context.SaveChanges();
        //        #endregion

        //        #region Add New Purchase Order				
        //        var device = context.Devices.AsNoTracking().FirstOrDefault(x => x.dvcUID == dev.dvcUID);
        //        var purchaseinitcode = device.dvcPurchaseOrderPrefix;
        //        var purchaseno = $"{device.dvcNextPurchaseOrderNo:000000}";
        //        string code = string.Concat(purchaseinitcode, purchaseno);
        //        MMDAL.Purchase purchase = new MMDAL.Purchase
        //        {
        //            pstCode = code,
        //            pstRefCode = ps.pstCode,
        //            IsThreshold = false,
        //            AccountProfileId = apId,
        //            CreateBy = user.UserCode,
        //            CreateTime = DateTime.Now,
        //        };
        //        context.Purchases.Add(purchase);
        //        device.dvcNextPurchaseOrderNo++;
        //        context.SaveChanges();
        //        #endregion
        //    }
        //}


        //private static MMDAL.Purchase updatePurchaseRequest(PurchaseModel model, MMDbContext context, string purchasestatus)
        //{  
        //    MMDAL.Purchase ps = _updatePurchaseRequest(model, context, purchasestatus);

           
        //    //List<PurchaseService> currentpslist = [.. context.PurchaseServices.Where(x => x.pstCode == ps.pstCode)];
        //    //List<PurchaseService> newpslist = [];
        //    //foreach (var item in model.ServiceList)
        //    //{
        //    //    var currentps = currentpslist.FirstOrDefault(x => x.Id == item.Id);
        //    //    if (currentps != null)
        //    //    {
        //    //        currentps.psSeq = item.psSeq;
        //    //        currentps.psDesc = item.psDesc;
        //    //        currentps.AccountID = item.AccountID;
        //    //        currentps.JobID = item.JobID;
        //    //        currentps.psAmt = item.psAmt;
        //    //        currentps.psAmtPlusTax = item.psAmtPlusTax;
        //    //        currentps.ModifyTime = DateTime.Now;
        //    //    }
        //    //    else
        //    //    {
        //    //        newpslist.Add(new PurchaseService
        //    //        {
        //    //            pstCode = item.pstCode,
        //    //            psSeq = item.psSeq,
        //    //            psDesc = item.psDesc,
        //    //            AccountID = item.AccountID,
        //    //            JobID = item.JobID,
        //    //            psAmt = item.psAmt,
        //    //            psAmtPlusTax = item.psAmtPlusTax,
        //    //            AccountProfileId = apId,
        //    //            CreateTime = DateTime.Now
        //    //        });
        //    //    }
        //    //}

        //    //if (newpslist.Count > 0) context.PurchaseServices.AddRange(newpslist);

        //    //context.SaveChanges();
        //    return ps;
        //}

        //private static MMDAL.Purchase _updatePurchaseRequest(PurchaseModel model, MMDbContext context, string purchasestatus)
        //{
        //    MMDAL.Purchase ps = context.Purchases.Find(model.Id);

        //    var pqstatus = model.pstStatus.ToLower() == PurchaseStatus.draft.ToString() ? RequestStatus.draftingByStaff.ToString() : model.pqStatus;

        //    if (ps != null)
        //    {
        //        ps.supCode = supcodes;
        //        ps.pstType = PurchaseType;
        //        ps.pstDesc = model.pstDesc;
        //        ps.pstAmount = model.pstAmount;
        //        ps.pstStatus = purchasestatus;
        //        ps.pqStatus = pqstatus;
        //        ps.pstSupplierInvoice = model.pstSupplierInvoice;
        //        ps.pstRemark = model.pstRemark;
        //        ps.pstCheckout = false;
        //        ps.pstIsPartial = false;
        //        ps.IsThreshold = model.IsThreshold;
        //        ps.ThresholdMsg = model.ThresholdMsg;
        //        ps.CreateBy = ps.ModifyBy = user.UserCode; //maybe created by staff01 first, but populated by staff02 later 
        //        ps.ModifyTime = DateTime.Now;
        //        context.SaveChanges();
        //    }

        //    return ps;
        //}

        private static void AddSelectedSuppliers(string pstCode, List<SupplierModel> SupplierList, MMDbContext context)
        {
            //var currentIds = context.PurchaseSuppliers.Where(x => x.AccountProfileId == apId && x.pstCode == pstCode).Select(x => x.Id).ToList();
            //List<PurchaseSupplier> pslist = new List<PurchaseSupplier>();
            //foreach (SupplierModel supplier in SupplierList)
            //{
            //    if (!currentIds.Contains(supplier.Id))
            //    {
            //        pslist.Add(new PurchaseSupplier
            //        {
            //            pstCode = pstCode,
            //            supCode = supplier.supCode,
            //            Amount = supplier.Amount,
            //            Remark = supplier.Remark,
            //            Selected = false,
            //            AccountProfileId = apId,
            //            CreateTime = DateTime.Now,
            //        });
            //    }
            //}
            //context.PurchaseSuppliers.AddRange(pslist);
            //context.SaveChanges();
        }


        private static void HandlingPurchaseOrderReview(string purchasecode, string pqstatus, MMDbContext context)
        {
            DateTime dateTime = DateTime.Now;

            var review = context.PurchaseOrderReviews.FirstOrDefault(x => x.PurchaseOrder == purchasecode && x.AccountProfileId == apId);
            if (review != null)
            {
                review.pqStatus = pqstatus;
                review.ModifyTime = dateTime;
            }
            else
            {
                review = new PurchaseOrderReview
                {
                    PurchaseOrder = purchasecode,
                    pqStatus = pqstatus,
                    AccountProfileId = apId,
                    CreateTime = dateTime,
                    ModifyTime = dateTime
                };
                context.PurchaseOrderReviews.Add(review);
            }

            context.SaveChanges();
        }

        public static void Delete(int id)
        {
            //using var context = new MMDbContext();
            //var ps = context.Purchases.FirstOrDefault(x => x.Id == id);
            //var psilist = context.PurchaseServices.Where(x => x.AccountProfileId == ps.AccountProfileId && x.pstCode == ps.pstCode);
            //context.PurchaseServices.RemoveRange(psilist);
            //context.Purchases.Remove(ps);
            //context.SaveChanges();
        }

        private static string genMemo(string currencycode, double exchangerate, double paytypeamts, string pstRemark)
        {
            //return StringHandlingForSQL(string.Concat(currencycode, " ", paytypeamts, $"({exchangerate})", " ", pstRemark));
            return Helpers.ModelHelper.genMemo(currencycode, exchangerate, paytypeamts, pstRemark);
        }
        private static string StringHandlingForSQL(string str)
        {
            return CommonHelper.StringHandlingForSQL(str);
        }

        public static List<string> GetUploadServiceSqlList(ref DataTransferModel dmodel, MMDbContext context)
        {
            var dateformatcode = context.AppParams.FirstOrDefault(x => x.appParam == "DateFormat" && x.AccountProfileId == apId).appVal;

            dmodel.Purchase.dateformat = dateformatcode == "E" ? @"dd/MM/yyyy" : @"MM/dd/yyyy";
            var purchase = dmodel.Purchase; //for later use

            string sql = "";
            List<string> values = [];
            List<string> columns = [];
            string strcolumn = "", value = "";
            List<string> sqllist = [];

            if (dmodel.Purchase != null)
            {
                dmodel.CheckOutIds_Purchase = [purchase.Id];
                var location = comInfo.Shop;

                sql = MyobHelper.InsertImportServicePurchasesSql;
                sql = sql.Replace("0", "{0}");

                for (int j = 0; j < MyobHelper.ImportServicePurchasesColCount; j++)
                {
                    columns.Add("'{" + j + "}'");
                }
                strcolumn = string.Join(",", columns);
                //INSERT INTO Import_Service_Purchases (PurchaseNumber,PurchaseDate,SuppliersNumber,DeliveryStatus,AccountNumber,CoLastName,ExTaxAmount,IncTaxAmount,PurchaseStatus) VALUES ('00001797','04/12/2023','SP100022','A','{account}','A1 DIGITAL','4000','4000')
                value = string.Format("(" + strcolumn + ")", purchase.pstCode, purchase.PurchaseDate4ABSS, StringHandlingForSQL(purchase.pstSupplierInvoice), "A", comInfo.comAccountNo, StringHandlingForSQL(purchase.SupplierName), purchase.Amount4Abss, purchase.Amount4Abss, StringHandlingForSQL(purchase.pstDesc));
                values.Add(value);

                sql = string.Format(sql, string.Join(",", values));
                sqllist.Add(sql);
            }

            #region Instalment Payments:
            sqlConnection.Open();
            //List<SupplierPaymentModel> supplierpayments = sqlConnection.Query<SupplierPaymentModel>(@"EXEC dbo.GetSupplierPaymentsByCode @apId=@apId,@pstCode=@pstCode,@supCode=@supCode,@checkout=@checkout", new { apId, purchase.pstCode, purchase.supCode, checkout = false }).ToList();

            //if (supplierpayments.Count > 0)
            //{
            //    sql = MyobHelper.InsertImportPayBillsSql;
            //    sql = sql.Replace("0", "{0}");
            //    values = [];
            //    columns = [];
            //    strcolumn = "";

            //    for (int j = 0; j < MyobHelper.ImportPayBillsColCount; j++)
            //    {
            //        columns.Add("'{" + j + "}'");
            //    }
            //    strcolumn = string.Join(",", columns);
            //    value = "";

            //    //string payac = comInfo.comPayAccountNo.Replace("-", "");

            //    dmodel.CheckOutIds_SupPayLn = new HashSet<long>();

            //    //todo:
            //    //foreach (var sp in supplierpayments)
            //    //{
            //    //    dmodel.CheckOutIds_SupPayLn.Add(sp.Id);
            //    //    sp.dateformat = purchase.dateformat;
            //    //    string payac = sp.supAccount.Replace("-", "");
            //    //    //INSERT INTO Import_Pay_Bills (CoLastName,PaymentAccount,PurchaseNumber,SuppliersNumber,AmountApplied,PaymentDate,ChequeNumber) VALUES ('{supname}','{payac}','{pono}','{supno}','500','{date}','897852')
            //    //    value = string.Format("(" + strcolumn + ")", StringHandlingForSQL(sp.SupplierName), payac, sp.pstCode, StringHandlingForSQL(purchase.pstSupplierInvoice), Convert.ToDouble(sp.Amount), sp.PayDate4ABSS, StringHandlingForSQL(sp.spChequeNo));
            //    //    values.Add(value);
            //    //}

            //    sql = string.Format(sql, string.Join(",", values));
            //    sqllist.Add(sql);
            //}
            #endregion

            return sqllist;
        }

        public static PurchaseModel getPurchaseModelById(long Id)
        {
            var connection = new SqlConnection(defaultConnection);
            connection.Open();
            return connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id", new { apId, Id });
        }

        public void GetPurchaseRequestStatus(ref ReactType reactType, IsUserRole IsUserRole, ref MMDAL.Purchase purchase, out string pqStatus, string SelectedSupCode, MMDbContext context, int poThreshold)
        {
            pqStatus = "";
            if (purchase.pstStatus == RequestStatus.approved.ToString())
            {
                reactType = ReactType.PassedByDeptHead;

                if (poThreshold == 1)
                {
                    if (IsUserRole.isdirectorboard)
                    {
                        pqStatus = RequestStatus.approvedByDirectorBoard.ToString();
                        reactType = ReactType.ApprovedByDirectorBoard;
                    }

                    if (IsUserRole.ismuseumdirector)
                    {
                        pqStatus = RequestStatus.requestingByMuseumDirector.ToString();
                        reactType = ReactType.PassedToDirectorBoard;
                    }

                    if (IsUserRole.isfinancedept)
                    {
                        pqStatus = RequestStatus.requestingByFinanceDept.ToString();
                        reactType = ReactType.PassedToMuseumDirector;
                    }
                }
                else
                {
                    if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector)
                    {
                        //todo:
                        //purchase.pstStatus = PurchaseStatus.order.ToString();
                        //pqStatus = RequestStatus.approved.ToString();
                        //reactType = ReactType.Approved;
                        //PurchaseSupplier purchaseSupplier = context.PurchaseSuppliers.FirstOrDefault(x => x.supCode == SelectedSupCode);
                        //purchaseSupplier.Selected = true;
                        //purchaseSupplier.ModifyTime = DateTime.Now;
                        //purchase.supCode = SelectedSupCode;
                        //purchase.pstAmount = purchaseSupplier.Amount;
                        //context.SaveChanges();
                    }

                    if (IsUserRole.isfinancedept)
                    {
                        pqStatus = RequestStatus.requestingByFinanceDept.ToString();
                        reactType = ReactType.PassedByFinanceDept;
                    }
                }


                if (IsUserRole.isdepthead)
                {
                    pqStatus = RequestStatus.requestingByDeptHead.ToString();
                    reactType = ReactType.PassedByDeptHead;
                }

                if (IsUserRole.isstaff)
                {
                    pqStatus = RequestStatus.requestingByStaff.ToString();
                    reactType = ReactType.RequestingByStaff;
                }

            }

            if (purchase.pstStatus == RequestStatus.rejected.ToString())
            {
                reactType = ReactType.RejectedByDeptHead;
                if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector)
                {
                    pqStatus = RequestStatus.rejectedByMuseumDirector.ToString();
                    reactType = ReactType.Rejected;
                }

                if (IsUserRole.isdepthead)
                {
                    pqStatus = RequestStatus.rejectedByDeptHead.ToString();
                    reactType = ReactType.RejectedByDeptHead;
                }

                if (IsUserRole.isfinancedept)
                {
                    pqStatus = RequestStatus.rejectedByFinanceDept.ToString();
                    reactType = ReactType.RejectedByFinanceDept;
                }
            }

            if (purchase.pstStatus == RequestStatus.onhold.ToString())
            {
                reactType = ReactType.OnHoldByFinanceDept;
                if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector)
                {
                    pqStatus = RequestStatus.onholdByMuseumDirector.ToString();
                    reactType = ReactType.OnHoldByDirector;
                }

                if (IsUserRole.isfinancedept)
                {
                    pqStatus = RequestStatus.onholdByFinanceDept.ToString();
                    reactType = ReactType.OnHoldByFinanceDept;
                }
            }

            if (purchase.pstStatus == RequestStatus.voided.ToString())
            {
                if (IsUserRole.isdepthead)
                {
                    pqStatus = RequestStatus.voidedByDeptHead.ToString();
                    reactType = ReactType.VoidedByDeptHead;
                }

                if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector)
                {
                    pqStatus = RequestStatus.voidedByMuseumDirector.ToString();
                    reactType = ReactType.Voided;
                }

                if (IsUserRole.isfinancedept)
                {
                    pqStatus = RequestStatus.voidedByFinanceDept.ToString();
                    reactType = ReactType.VoidedByFinanceDept;
                }
            }
        }

    }

    public class PurchaseReturnMsg
    {
        public string msg { get; set; }
        public string purchasecode { get; set; }
        public string reviewurl { get; set; }
        public string superiorphone { get; set; }
        public string status { get; set; }
        public int purchaselnlength { get; set; }
        public string superioremail { get; set; }
        public bool isdirectorboard { get; set; }
        public string supnames { get; set; }
        public string remark { get; set; }
    }
}