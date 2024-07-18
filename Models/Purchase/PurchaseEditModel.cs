using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CommonLib.Helpers;
using MMDAL;
using Resources = CommonLib.App_GlobalResources;
using Device = MMDAL.Device;
using Purchase = MMDAL.Purchase;
using System.Configuration;
using MMLib.Models.POS.Settings;
using Microsoft.Data.SqlClient;
using Dapper;
using MMLib.Models.User;
using System.Text.Json;
using CommonLib.BaseModels;
using MMLib.Models.Supplier;
using ModelHelper = MMLib.Helpers.ModelHelper;
using PagedList;
using MMLib.Models.POS.MYOB;
using MMLib.Helpers;

namespace MMLib.Models.Purchase
{
    public class PurchaseEditModel : PagingBaseModel
    {
        public List<MyobSupplierModel> SupplierList { get; set; }
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
        public List<SelectListItem> FundingSources
        {
            get
            {
                var list = ConfigurationManager.AppSettings["FundingSources"].Split('|').ToList();
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

        public bool ThresholdRequestApproved { get; set; } = false;
        public bool IsMD { get { return IsUserRole.ismuseumdirector; } }
        public bool IsDB { get { return IsUserRole.isdirectorboard; } }
        public bool IsFin { get { return IsUserRole.isfinancedept; } }
        public string ProcurementPersonName { get; set; }

        public PurchaseOrderReviewModel PurchaseOrderReview { get; set; }

        public Dictionary<string, string> DicLocation { get; set; }
        public string JsonDicLocation { get { return DicLocation == null ? "" : JsonSerializer.Serialize(DicLocation); } }

        public List<SelectListItem> LocationList { get; set; }


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
        public List<MyobSupplierModel> Vendors { get; private set; }

        public PurchaseEditModel()
        {
            DicCurrencyExRate = new Dictionary<string, double>();
            DicLocation = new Dictionary<string, string>();
            ImgList = new List<string>();
            FileList = new List<string>();
            PoQtyAmtList = new();
            SupplierList = new List<MyobSupplierModel>();
        }

        public PurchaseEditModel(long? Id, int? idoapproval) : this()
        {
            Get(Id, null, idoapproval);
        }
        public PurchaseEditModel(long? Id, string status = "", int? idoapproval = 1, bool forprint = false) : this()
        {
            Get(Id, null, idoapproval, status, forprint);
        }


        public void Get(long? Id, string pstCode = null, int? idoapproval = 1, string status = "", bool forprint = false)
        {
            bool isMD = UserHelper.CheckIfMD(User);
            bool isDB = UserHelper.CheckIfDB(User);

            using var context = new MMDbContext();
            Purchase = new PurchaseModel();
            Device device = null;
            if (!isMD && !isDB) device = context.Devices.First();

            if (SqlConnection.State == System.Data.ConnectionState.Closed) SqlConnection.Open();
            using (SqlConnection)
            {
                DateTime dateTime = DateTime.Now;

                if (Id > 0 || !string.IsNullOrEmpty(pstCode))
                {
                    Purchase = SqlConnection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id,@pstCode=@pstCode", new { apId, Id, pstCode });

                    DoApproval = idoapproval == 1;

                    if (forprint)
                    {
                        Receipt = new ReceiptViewModel();
                        DisclaimerList = new List<string>();
                        PaymentTermsList = new List<string>();
                        ModelHelper.GetReady4Print(context, ref Receipt, ref DisclaimerList, ref PaymentTermsList);
                    }

                    string baseUrl = UriHelper.GetBaseUrl();
                    SupplierList = SqlConnection.Query<MyobSupplierModel>(@"EXEC dbo.GetPurchaseSuppliersInfoesByCode @apId=@apId,@pstCode=@pstCode,@baseUrl=@baseUrl", new { apId, Purchase.pstCode, baseUrl }).ToList();

                    Purchase.IsEditMode = true;

                    if (Purchase.pstStatus.ToLower() == PurchaseStatus.order.ToString()) SelectedSupplier = SupplierList.FirstOrDefault(x => x.Selected);
                }
                else
                {
                    var purchaseinitcode = device.dvcPurchaseRequestPrefix;
                    var purchaseno = $"{device.dvcNextPurchaseRequestNo:000000}";
                    device.dvcNextPurchaseRequestNo++;
                    device.dvcModifyTime = dateTime;

                    string pqstatus = RequestStatus.requestingByStaff.ToString();
                    //if (IsUserRole.isdepthead) pqstatus = RequestStatus.requestingByDeptHead.ToString();
                    //if (IsUserRole.isfinancedept) pqstatus = RequestStatus.requestingByFinanceDept.ToString();

                    string pstcode = string.Concat(purchaseinitcode, purchaseno);

                    status = PurchaseStatus.requesting.ToString();
                    var latestpurchase = context.Purchases.OrderByDescending(x => x.Id).FirstOrDefault();
                    if (latestpurchase != null)
                    {
                        if (string.IsNullOrEmpty(latestpurchase.pstType)) PopulatePurchaseModel(latestpurchase.pstCode, latestpurchase.pstStatus, RequestStatus.requestingByStaff.ToString(), latestpurchase.Id);
                        else addNewPurchase(context, apId, pstcode, status, (bool)latestpurchase.IsThreshold, pqstatus);
                    }
                    else addNewPurchase(context, apId, pstcode, status, false, pqstatus);

                    Purchase.CreateByDisplay = User.UserName;
                }

                Vendors = SqlConnection.Query<MyobSupplierModel>(@"EXEC dbo.GetSupplierList6 @apId=@apId", new { apId }).ToList();

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

                Purchase.Mode = idoapproval == 1 ? "readonly" : "";
                PoSettings = PoSettingsEditModel.GetPoSettings(SqlConnection);
            }
        }

        public static List<PurchaseReturnMsg> Edit(PurchaseModel model, List<SupplierModel> SupplierList = null)
        {
            List<PurchaseReturnMsg> msglist = [];
            string status = "";
            string msg = string.Format(Resources.Resource.SavedFormat, Resources.Resource.PurchaseOrder);
            List<string> reviewurls = [];
            List<GetSuperior4Notification2_Result> superiors = [];
            Dictionary<string, string> DicReviewUrl = [];
            Dictionary<string, Dictionary<string, int>> dicItemLocQty = [];

            using var context = new MMDbContext();
            if (sqlConnection.State == System.Data.ConnectionState.Closed) sqlConnection.Open();
            using (sqlConnection)
            {
                MMDAL.Purchase ps = context.Purchases.Find(model.Id);
                DateTime dateTime = DateTime.Now;
                DeviceModel dev = HttpContext.Current.Session["Device"] as DeviceModel;
                string purchasestatus = model.pstStatus.ToLower() == PurchaseStatus.draft.ToString()
                 ? PurchaseStatus.draft.ToString()
                 : isUserRole.isdirectorboard ? model.pstStatus : !model.IsEditMode ? PurchaseStatus.requesting.ToString() : model.pstStatus;

                string supnames = SupplierList != null ? string.Join(",", SupplierList.Select(x => x.supName).Distinct().ToList()) : "";

                ReactType reactType = ReactType.RequestingByStaff;
                getPurchaseRequestStatus(ref reactType, isUserRole, ref model, ref ps, null, context, model.IsThreshold == null ? 0 : (bool)model.IsThreshold ? 1 : 0);

                SuperiorList = sqlConnection.Query<Superior>(@"EXEC dbo.GetSuperior4Notification2 @apId=@apId,@userId=@userId", new { apId, userId = user.surUID }).ToList();

                if (isUserRole.isfinancedept)
                {
                    if (enableAssistant)
                    {
                        decimal Threshold4DA = Convert.ToDecimal(ConfigurationManager.AppSettings["Threshold4DA"]);
                        SuperiorList = (ps.pstAmount > Threshold4DA) ? SuperiorList.Where(x => x.UserRole.ToLower() == RoleType.MuseumDirector.ToString().ToLower()).ToList() : SuperiorList.Where(x => x.UserRole.ToLower() == RoleType.DirectorAssistant.ToString().ToLower()).ToList();
                    }
                    else
                    {
                        SuperiorList = SuperiorList.Where(x => x.UserRole.ToLower() == RoleType.MuseumDirector.ToString().ToLower()).ToList();
                    }
                }

                GetReviewUrls(model, reviewurls, DicReviewUrl, SuperiorList);

                if (!model.IsEditMode) updatePurchaseRequest(model, ps, context, purchasestatus, SupplierList);
                else processPurchase(model, ps, dev, context, purchasestatus, SupplierList);

                status = "purchaseordersaved";

                if (model.pstStatus.ToLower() != PurchaseStatus.draft.ToString().ToLower() && model.pstStatus.ToLower() != PurchaseStatus.order.ToString())
                {
                    HandlingPurchaseOrderReview(model.pstCode, model.pqStatus, context);

                    if (!isUserRole.isdirectorboard && !isUserRole.ismuseumdirector)
                    {
                        #region Send Notification Email   
                        if ((bool)comInfo.enableEmailNotification)
                        {
                            var mailsettings = sqlConnection.QueryFirstOrDefault<EmailModel>("EXEC dbo.GetEmailSettings @apId=@apId", new { apId });

                            if (model.pstStatus.ToLower() == PurchaseStatus.withdraw.ToString())
                            {
                                model.CreateTime = ps.CreateTime;
                                reactType = ReactType.WithDraw;
                            }
                            else
                            {
                                reactType = ReactType.RequestingByStaff;
                                if (isUserRole.isdepthead) reactType = ReactType.RequestingByDeptHead;
                                if (isUserRole.isfinancedept) reactType = ReactType.RequestingByFinanceDept;
                                if (enableAssistant && isUserRole.isdirectorassistant) reactType = ReactType.RequestingByDirectorAssistant;
                                if (isUserRole.ismuseumdirector || isUserRole.isdirectorboard) reactType = ReactType.Approved;
                            }

                            var mdInfo = sqlConnection.Query<UserModel>($"EXEC dbo.GetMDInfo @apId=@apId", new { apId }).ToList();
                            var dbInfo = sqlConnection.Query<UserModel>($"EXEC dbo.GetDBInfo @apId=@apId", new { apId }).ToList();

                            if (ModelHelper.SendNotificationEmail(user, SuperiorList, DicReviewUrl, reactType, mailsettings, model, null, null, null, 0, null, mdInfo, dbInfo))
                            {
                                var purchase = context.Purchases.FirstOrDefault(x => x.Id == model.Id);
                                purchase.pstSendNotification = true;
                                context.SaveChanges();
                            }
                        }
                        #endregion
                    }
                    GenReturnMsgList(model, supnames, ref msglist, SuperiorList, status, isUserRole.isdirectorboard, DicReviewUrl, isUserRole.ismuseumdirector);
                }
                return msglist;
            }
        }

        public static void GetReviewUrls(PurchaseModel model, List<string> reviewurls, Dictionary<string, string> DicReviewUrl, List<Superior> SuperiorList)
        {
            foreach (var superior in SuperiorList)
            {
                var reviewurl = UriHelper.GetReviewPurchaseOrderUrl(ConfigurationManager.AppSettings["ReviewPurchaseOrderBaseUrl"], model.pstCode, 0, superior.surUID);
                var key = string.Concat(superior.UserName, ":", superior.Email, ":", model.pstCode);
                if (!DicReviewUrl.ContainsKey(key))
                    DicReviewUrl[key] = reviewurl;
                reviewurls.Add(reviewurl);
            }
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



        private static void GenReturnMsgList(PurchaseModel model, string supnames, ref List<PurchaseReturnMsg> msglist, List<Superior> SuperiorList, string status, bool isdirectorboard, Dictionary<string, string> DicReviewUrl, bool ismuseumdirector, string msg = null)
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
                    supnames = supnames,
                    superioremail = superior.Email,
                    isdirectorboard = isdirectorboard,
                });
            }
        }

        private static void processPurchase(PurchaseModel model, MMDAL.Purchase ps, DeviceModel dev, MMDbContext context, string purchasestatus, List<SupplierModel> SupplierList = null)
        {
            ps = updatePurchaseRequest(model, ps, context, purchasestatus, SupplierList);

            if (purchasestatus.ToLower() == PurchaseStatus.order.ToString())
            {
                #region Update Purchase Status
                // update wholesales status:                
                ps.pstStatus = PurchaseStatus.order.ToString();
                ps.ModifyTime = DateTime.Now;
                context.SaveChanges();
                #endregion

                #region Add New Purchase Order				
                var device = context.Devices.AsNoTracking().FirstOrDefault(x => x.dvcUID == dev.dvcUID);
                var purchaseinitcode = device.dvcPurchaseOrderPrefix;
                var purchaseno = $"{device.dvcNextPurchaseOrderNo:000000}";
                string code = string.Concat(purchaseinitcode, purchaseno);
                MMDAL.Purchase purchase = new MMDAL.Purchase
                {
                    pstCode = code,
                    pstRefCode = ps.pstCode,
                    IsThreshold = false,
                    AccountProfileId = apId,
                    CreateBy = user.UserCode,
                    CreateTime = DateTime.Now,
                };
                context.Purchases.Add(purchase);
                device.dvcNextPurchaseOrderNo++;
                context.SaveChanges();
                #endregion
            }
        }


        private static MMDAL.Purchase updatePurchaseRequest(PurchaseModel model, MMDAL.Purchase ps, MMDbContext context, string purchasestatus, List<SupplierModel> SupplierList = null)
        {
            var supcodes = SupplierList != null ? string.Join(",", SupplierList.Select(x => x.supCode).Distinct().ToList()) : null;
            ps = _updatePurchaseRequest(model, ps, context, purchasestatus, supcodes);

            if (SupplierList != null)
                AddSelectedSuppliers(model.pstCode, SupplierList, context);
            return ps;
        }

        private static MMDAL.Purchase _updatePurchaseRequest(PurchaseModel model, MMDAL.Purchase ps, MMDbContext context, string purchasestatus, string supcodes = null)
        {
            var pstTime = DateTime.Now;

            var pqstatus = model.pstStatus.ToLower() == PurchaseStatus.draft.ToString() ? RequestStatus.draftingByStaff.ToString() : model.pqStatus;

            if (ps != null)
            {
                ps.supCode = supcodes;
                ps.pstType = PurchaseType;
                ps.pstDesc = model.pstDesc;
                ps.pstAmount = model.pstAmount;
                ps.pstPurchaseDate = pstTime.Date;
                ps.pstPurchaseTime = pstTime;
                ps.pstStatus = purchasestatus;
                ps.pqStatus = pqstatus;

                ps.pstExpectedCost = model.pstExpectedCost;
                ps.pstExpectedCostDesc = model.pstExpectedCostDesc;
                ps.pstFundingSource = model.pstFundingSource;
                ps.pstFSOthers = model.pstFSOthers;
                ps.pstPreApprovedMsg = model.pstPreApprovedMsg;
                ps.pstRationnale = model.pstRationnale;
                ps.pstVendorSummary = model.pstVendorSummary;
                ps.pstMDReview = model.pstMDReview;
                ps.pstQuotationNum = model.pstQuotationNum;
                ps.pstChooseVendorReason = model.pstChooseVendorReason;
                ps.pstContractualProtection = model.pstContractualProtection;
                ps.pstQuotationInvitationNum = model.pstQuotationInvitationNum;
                ps.pstNoInvitationReason = model.pstNoInvitationReason;
                ps.pstQuotationReceivedNum = model.pstQuotationReceivedNum;
                ps.pstReasonsNotEnoughQuotation = model.pstReasonsNotEnoughQuotation;
                ps.pstOtherReason4NotEnoughQuotation = model.pstOtherReason4NotEnoughQuotation;
                ps.pstReasons4ChoosingFinalSupplier = model.pstReasons4ChoosingFinalSupplier;
                ps.pstOtherReason4ChoosingFinalSupplier = model.pstOtherReason4ChoosingFinalSupplier;

                ps.pstCheckout = false;
                ps.pstIsPartial = false;

                ps.IsThreshold = model.IsThreshold;
                ps.ThresholdMsg = model.ThresholdMsg;

                ps.pstNotEnoughQuotation = model.pstNotEnoughQuotation;
                ps.pstRemark = model.pstRemark;
                ps.pstOnHoldReason = model.pstOnHoldReason;

                ps.ModifyBy = user.UserCode; //maybe created by staff01 first, but populated by staff02 later 
                ps.ModifyTime = DateTime.Now;
                context.SaveChanges();
            }

            return ps;
        }

        private static void AddSelectedSuppliers(string pstCode, List<SupplierModel> SupplierList, MMDbContext context)
        {
            List<PurchaseSupplier> pslist = [];
            List<string> currentRecords = context.PurchaseSuppliers.Where(x => x.AccountProfileId == apId && x.pstCode == pstCode)?.Select(x => x.supCode.ToLower()).ToList();            
            if (currentRecords == null)
            {
                foreach(var supplier in SupplierList)
                {
                    pslist.Add(new PurchaseSupplier
                    {
                        pstCode = pstCode,
                        supCode = supplier.supCode,
                        Amount = supplier.Amount,
                        Remark = supplier.Remark,
                        Selected = false,
                        AccountProfileId = apId,
                        CreateTime = DateTime.Now,
                    });
                }
            }
            else
            {
                var groupedSupList = SupplierList.GroupBy(x => x.supCode).ToList();
                foreach (var group in groupedSupList)
                {
                    var supplier = group.FirstOrDefault();
                    if (!currentRecords.Contains(supplier.supCode.ToLower()))
                    {
                        pslist.Add(new PurchaseSupplier
                        {
                            pstCode = pstCode,
                            supCode = supplier.supCode,
                            Amount = supplier.Amount,
                            Remark = supplier.Remark,
                            Selected = false,
                            AccountProfileId = apId,
                            CreateTime = DateTime.Now,
                        });
                    }
                }
            }
            
            context.PurchaseSuppliers.AddRange(pslist);
            context.SaveChanges();
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

        private static string genMemo(string currencycode, double exchangerate, double paytypeamts, string pstRemark)
        {
            //return StringHandlingForSQL(string.Concat(currencycode, " ", paytypeamts, $"({exchangerate})", " ", pstRemark));
            return Helpers.ModelHelper.genMemo(currencycode, exchangerate, paytypeamts, pstRemark);
        }



        public static PurchaseModel getPurchaseModelById(long Id)
        {
            var connection = new SqlConnection(defaultConnection);
            connection.Open();
            return connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id", new { apId, Id });
        }

        public void GetPurchaseRequestStatus(ref ReactType reactType, IsUserRole IsUserRole, ref PurchaseModel purchaseModel, ref MMDAL.Purchase purchase, string SelectedSupCode, MMDbContext context, int IsThreshold)
        {
            getPurchaseRequestStatus(ref reactType, IsUserRole, ref purchaseModel, ref purchase, SelectedSupCode, context, IsThreshold);
        }

        private static void getPurchaseRequestStatus(ref ReactType reactType, IsUserRole IsUserRole, ref PurchaseModel purchaseModel, ref MMDAL.Purchase purchase, string SelectedSupCode, MMDbContext context, int IsThreshold)
        {
           
            if (purchase.pstStatus.ToLower() == PurchaseStatus.requesting.ToString())
            {
                if (enableAssistant && IsUserRole.isdirectorassistant)
                {
                   purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByDirectorAssistant.ToString();
                    reactType = ReactType.RequestingByDirectorAssistant;
                }

                if (IsUserRole.isfinancedept)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByFinanceDept.ToString();
                    reactType = ReactType.RequestingByFinanceDept;
                }

                if (IsUserRole.isdepthead)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByDeptHead.ToString();
                    reactType = ReactType.RequestingByDeptHead;
                }

                if (IsUserRole.isstaff)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByStaff.ToString();
                    reactType = ReactType.RequestingByStaff;
                }

            }

            if (purchase.pstStatus == RequestStatus.approved.ToString())
            {
                if (IsThreshold == 1)
                {
                    if (IsUserRole.isdirectorboard)
                    {
                        purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.approvedByDirectorBoard.ToString();
                        reactType = ReactType.ApprovedByDirectorBoard;
                    }

                    if (IsUserRole.ismuseumdirector)
                    {
                        purchase.pqStatus = purchaseModel.pqStatus = purchase.pqStatus.ToLower() == RequestStatus.approvedByDirectorBoard.ToString().ToLower() ? RequestStatus.approvedByMuseumDirector.ToString() : RequestStatus.requestingByMuseumDirector.ToString();

                        if (purchase.pqStatus.ToLower() == RequestStatus.approvedByDirectorBoard.ToString().ToLower())
                        {
                            purchase.pqStatus = purchaseModel.pqStatus = PurchaseStatus.order.ToString();
                            reactType = ReactType.ApprovedByMuseumDirector;
                        }
                        else reactType = ReactType.PassedToDirectorBoard;

                        if (!string.IsNullOrEmpty(SelectedSupCode))
                            updatePurchase4MDApproval(purchase, SelectedSupCode, context);
                    }

                    if (IsUserRole.isfinancedept)
                    {
                        purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByFinanceDept.ToString();
                        reactType = ReactType.PassedByFinanceDept;
                    }

                    if (IsUserRole.isdepthead)
                    {
                        purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByDeptHead.ToString();
                        reactType = ReactType.PassedByDeptHead;
                    }
                }
                else
                {
                    if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector || (enableAssistant && IsUserRole.isdirectorassistant) || IsUserRole.istransientmd)
                    {
                        purchase.pstStatus = PurchaseStatus.order.ToString();
                        purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.approvedByMuseumDirector.ToString();
                        reactType = ReactType.Approved;

                        if (!string.IsNullOrEmpty(SelectedSupCode))
                            updatePurchase4MDApproval(purchase, SelectedSupCode, context);
                    }

                    if (IsUserRole.isfinancedept)
                    {
                        purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByFinanceDept.ToString();
                        reactType = ReactType.RequestingByFinanceDept;
                    }

                    if (IsUserRole.isdepthead)
                    {
                        purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByDeptHead.ToString();
                        reactType = ReactType.RequestingByDeptHead;
                    }

                    if (IsUserRole.isstaff)
                    {
                        purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.requestingByStaff.ToString();
                        reactType = ReactType.RequestingByStaff;
                    }
                }
            }

            if (purchase.pstStatus == RequestStatus.rejected.ToString())
            {
                reactType = ReactType.RejectedByDeptHead;
                if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector || (enableAssistant && IsUserRole.isdirectorassistant) || IsUserRole.istransientmd)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.rejectedByMuseumDirector.ToString();
                    reactType = ReactType.Rejected;
                }

                if (IsUserRole.isfinancedept)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.rejectedByFinanceDept.ToString();
                    reactType = ReactType.RejectedByFinanceDept;
                }

                if (IsUserRole.isdepthead)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.rejectedByDeptHead.ToString();
                    reactType = ReactType.RejectedByDeptHead;
                }


            }

            if (purchase.pstStatus == RequestStatus.onhold.ToString())
            {
                reactType = ReactType.OnHoldByFinanceDept;
                if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector || (enableAssistant && IsUserRole.isdirectorassistant) || IsUserRole.istransientmd)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.onholdByMuseumDirector.ToString();
                    reactType = ReactType.OnHoldByMuseumDirector;
                }

                if (IsUserRole.isfinancedept)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.onholdByFinanceDept.ToString();
                    reactType = ReactType.OnHoldByFinanceDept;
                }
            }

            //todo: to be complemented...
            if (purchase.pstStatus == RequestStatus.voided.ToString())
            {
                if (IsUserRole.isdepthead)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.voidedByDeptHead.ToString();
                    reactType = ReactType.VoidedByDeptHead;
                }

                if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.voidedByMuseumDirector.ToString();
                    reactType = ReactType.Voided;
                }

                if (IsUserRole.isfinancedept)
                {
                    purchase.pqStatus = purchaseModel.pqStatus = RequestStatus.voidedByFinanceDept.ToString();
                    reactType = ReactType.VoidedByFinanceDept;
                }
            }

            static void updatePurchase4MDApproval(MMDAL.Purchase purchase, string SelectedSupCode, MMDbContext context)
            {
                PurchaseSupplier purchaseSupplier = context.PurchaseSuppliers.FirstOrDefault(x => x.pstCode == purchase.pstCode && x.supCode == SelectedSupCode && x.AccountProfileId == apId);
                if (purchaseSupplier != null)
                {
                    purchaseSupplier.Selected = true;
                    purchaseSupplier.ModifyTime = DateTime.Now;
                    purchase.supCode = SelectedSupCode;
                    purchase.pstAmount = purchaseSupplier.Amount;
                    context.SaveChanges();
                }
                else
                {
                    context.PurchaseSuppliers.Add(new PurchaseSupplier
                    {
                        pstCode = purchase.pstCode,
                        supCode = SelectedSupCode,
                        Amount = purchase.pstAmount ?? 0,
                        Selected = true,
                        AccountProfileId = apId,
                    });
                    context.SaveChanges();
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