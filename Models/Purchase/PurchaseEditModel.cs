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
using System.Data.Entity.Migrations.Model;

namespace MMLib.Models.Purchase
{
    public class PurchaseEditModel : PagingBaseModel
    {
        /// <summary>
        /// for choose
        /// </summary>
        public List<SupplierModel> Suppliers { get; set; } = new List<SupplierModel>();
        public string ProcurementPersonName { get; set; }
        public string LastSupplierInvoiceId { get; set; }
        public Dictionary<string, List<SupplierPaymentModel>> DicSupInvoicePayments { get; set; } = new Dictionary<string, List<SupplierPaymentModel>>();
        public List<PoQtyAmtModel> PoQtyAmtList { get; set; }
        public SupplierModel SelectedSupplier { get; set; }
        public List<SupplierInvoiceModel> SupplierInvoiceList { get; set; }
        //public List<SupplierModel> SupplierList { get; set; } = new List<SupplierModel>();
        public string RemoveFileIcon { get { return $"<i class='mx-2 fa-solid fa-trash removefile' data-id='{{2}}'></i>"; } }
        public string PDFThumbnailPath { get { return $"<img src='{UriHelper.GetBaseUrl()}/Images/pdf.jpg' class='thumbnail'/>"; } }
        public Dictionary<string, List<SupplierModel>> DicSupInfoes { get; set; }

        public PurchaseOrderReviewModel PurchaseOrderReview { get; set; }
        public Dictionary<string, string> DicSupCodeName { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> DicLocation { get; set; }
        public Dictionary<string, double> DicCurrencyExRate { get; set; }
        public string JsonDicCurrencyExRate { get { return JsonSerializer.Serialize(DicCurrencyExRate); } }
        public string JsonDicLocation { get { return DicLocation == null ? "" : JsonSerializer.Serialize(DicLocation); } }

        public List<SupplierModel> SupplierList { get; set; }
        public List<SelectListItem> LocationList { get; set; }
        public List<MyobJobModel> JobList { get; set; }
        public string JsonJobList { get { return JobList == null ? "" : JsonSerializer.Serialize(JobList); } }

        public List<string> ImgList;
        public List<string> FileList;
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
        public List<SupplierPaymentModel> InvoicePayments { get; set; }

        public PurchaseEditModel()
        {
            DicCurrencyExRate = new Dictionary<string, double>();
            DicLocation = new Dictionary<string, string>();
            JobList = new List<MyobJobModel>();
            ImgList = new List<string>();
            FileList = new List<string>();
            DicSupInfoes = new Dictionary<string, List<SupplierModel>>();
            PoQtyAmtList = [];
        }

        public static SupplierInvoice addSupplierInvoice(string pstCode, string invoiceId, MMDbContext context)
        {
            SupplierInvoice supplierInvoice = new SupplierInvoice
            {
                Id = invoiceId,
                pstCode = pstCode,
                AccountProfileId = apId,
                CreateTime = DateTime.Now,
                CreateBy = user.UserCode,
            };
            supplierInvoice = context.SupplierInvoices.Add(supplierInvoice);
            context.SaveChanges();
            return supplierInvoice;
        }

        public static void EditPayments(List<SupplierInvoiceModel> SupplierInvoices)
        {
            using var context = new MMDbContext();
            List<SupplierInvoice> invoices = [];
            List<SupplierPayment> payments = [];
            string pstCode = SupplierInvoices.First().pstCode;

            var currentSIs = context.SupplierInvoices.Where(x => x.pstCode == pstCode && x.AccountProfileId == apId).ToList();

            foreach (var invoice in SupplierInvoices)
            {
                var si = currentSIs.FirstOrDefault(x => x.Id == invoice.Id);
                DateTime operationTime = CommonHelper.GetDateTimeFrmString(invoice.JsOperationTime);

                if (si == null)
                {
                    invoices.Add(new SupplierInvoice
                    {
                        Id = invoice.Id,
                        pstCode = pstCode,
                        supInvoice = invoice.supInvoice,
                        Amount = invoice.Amount,
                        supCode = invoice.supCode,
                        siChequeNo = invoice.siChequeNo,
                        Remark = invoice.Remark,
                        AccountProfileId = apId,
                        CreateBy = user.UserCode,
                        CreateTime = operationTime,
                    });
                }
                else
                {
                    si.supInvoice = invoice.supInvoice;
                    si.Amount = invoice.Amount;
                    si.supCode = invoice.supCode;
                    si.siChequeNo = invoice.siChequeNo;
                    si.Remark = invoice.Remark;
                    si.ModifyTime = operationTime;
                    si.ModifyBy = user.UserCode;
                }

                if (invoice.Payments.Count > 0)
                {
                    foreach (var payment in invoice.Payments)
                    {
                        payments.Add(new SupplierPayment
                        {
                            InvoiceId = payment.InvoiceId,
                            seq = payment.seq,
                            Amount = payment.Amount,
                            spChequeNo = payment.spChequeNo,
                            Remark = payment.Remark,
                            pstCode = pstCode,
                            spCheckout = false,
                            AccountProfileId = apId,
                            CreateBy = user.UserCode,
                            CreateTime = DateTime.Now,
                        });
                    }
                }
            }
            if (invoices.Count > 0) context.SupplierInvoices.AddRange(invoices);
            if (payments.Count > 0) context.SupplierPayments.AddRange(payments);

            context.SaveChanges();
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
                Purchase = connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id,@code=@code", new { apId, Id, code=pstCode });

                Readonly = ireadonly == 1;
                DoApproval = idoapproval == 1;

                if (forprint)
                {
                    Receipt = new ReceiptViewModel();
                    DisclaimerList = new List<string>();
                    PaymentTermsList = new List<string>();
                    ModelHelper.GetReady4Print(context, ref Receipt, ref DisclaimerList, ref PaymentTermsList);
                }

                var suppurchaseInfoes = connection.Query<SupplierModel>(@"EXEC dbo.GetSuppliersInfoesByCode @apId=@apId,@pstCode=@pstCode", new { apId, Purchase.pstCode }).ToList();

                List<IGrouping<string, SupplierModel>> groupedsupPurchaseInfoes = new List<IGrouping<string, SupplierModel>>();
                if (suppurchaseInfoes != null && suppurchaseInfoes.Count > 0)
                {
                    groupedsupPurchaseInfoes = suppurchaseInfoes.GroupBy(x => x.supCode).ToList();
                }

                Purchase.IsEditMode = true;

                SupplierList = connection.Query<SupplierModel>(@"EXEC dbo.GetPurchaseSuppliersByCode @apId=@apId,@pstCode=@pstCode", new { apId, Purchase.pstCode }).ToList();

                DicSupCodeName = new Dictionary<string, string>();
                foreach (var supplier in SupplierList) if (!DicSupCodeName.ContainsKey(supplier.supCode)) DicSupCodeName[supplier.supCode] = supplier.supName;              

                if (Purchase.pstStatus.ToLower() == PurchaseStatus.order.ToString())
                {
                    SelectedSupplier = SupplierList.Single(x => x.Selected);

                    string baseUrl = UriHelper.GetBaseUrl();
                    SupplierInvoiceList = connection.Query<SupplierInvoiceModel>(@"EXEC dbo.GetSupplierInvoicesByCode @apId=@apId,@pstCode=@pstCode,@baseUrl=@baseUrl", new { apId, Purchase.pstCode, baseUrl }).ToList();

                    if (SupplierInvoiceList.Count == 0)
                    {
                        LastSupplierInvoiceId = GenInvoiceId(Purchase.pstCode, 0);
                    }
                    else
                    {
                        int? numId = CommonHelper.GetNumberFrmString(SupplierInvoiceList.OrderByDescending(x => x.Id).FirstOrDefault().Id.Split('-')[1]);
                        LastSupplierInvoiceId = GenInvoiceId(Purchase.pstCode, (int)numId);
                    }

                    HashSet<string> SupInvoiceIds = new HashSet<string>();

                    if (SupplierInvoiceList.Count > 0)
                    {
                        SupInvoiceIds = SupplierInvoiceList.Select(x => x.Id).Distinct().ToHashSet();
                        foreach (var id in SupInvoiceIds) if (!DicSupInvoicePayments.ContainsKey(id)) DicSupInvoicePayments[id] = [];

                        InvoicePayments = connection.Query<SupplierPaymentModel>(@"EXEC dbo.GetSupplierPaymentsByIds @apId=@apId,@InvoiceIds=@InvoiceIds", new { apId, InvoiceIds = string.Join(",", SupInvoiceIds) }).ToList();

                        foreach (var invoice in SupplierInvoiceList) invoice.Payments = InvoicePayments.Where(x => x.InvoiceId == invoice.Id).ToList();

                        foreach (var payment in InvoicePayments)
                        {
                            if (DicSupInvoicePayments.ContainsKey(payment.InvoiceId))
                            {
                                DicSupInvoicePayments[payment.InvoiceId].Add(new SupplierPaymentModel
                                {
                                    payId = payment.Id,
                                    InvoiceId = payment.InvoiceId,
                                    seq = payment.seq,
                                    Amount = payment.Amount,
                                    spChequeNo = payment.spChequeNo,
                                    Remark = payment.Remark,
                                    CreateTime = payment.CreateTime,
                                    CreateBy = payment.CreateBy,
                                });

                            }
                        }
                    }

                    groupedsupPurchaseInfoes = suppurchaseInfoes.Where(x => x.supCode == SelectedSupplier.supCode).GroupBy(x => x.supCode).ToList();

                }

                getDicSupInfoes(groupedsupPurchaseInfoes);
            }
            else
            {
                var purchaseinitcode = device.dvcPurchaseRequestPrefix;
                var purchaseno = $"{device.dvcNextPurchaseRequestNo:000000}";
                device.dvcNextPurchaseRequestNo++;
                device.dvcModifyTime = dateTime;
                string pqstatus = RequestStatus.requestingByStaff.ToString();
                string pstcode = string.Concat(purchaseinitcode, purchaseno);

                status = PurchaseStatus.requesting.ToString();
                var latestpurchase = context.Purchases.OrderByDescending(x => x.Id).FirstOrDefault();
                if (latestpurchase != null)
                {
                    if (string.IsNullOrEmpty(latestpurchase.pstType)) PopulatePurchaseModel(latestpurchase.pstCode, latestpurchase.pstStatus, RequestStatus.requestingByStaff.ToString(), latestpurchase.Id);
                    else addNewPurchase(context, apId, pstcode, status, pqstatus);
                }
                else addNewPurchase(context, apId, pstcode, status, pqstatus);
            }

            Suppliers = connection.Query<SupplierModel>(@"EXEC dbo.GetSupplierList6 @apId=@apId", new { apId }).ToList();

            Purchase.Device = device != null ? device as DeviceModel : null;            

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

            Purchase.TaxModel = ModelHelper.GetTaxInfo(context);
            Purchase.UseForexAPI = ExchangeRateEditModel.GetForexInfo(context);
            DicLocation = new Dictionary<string, string>();
            foreach (var shop in stocklocationlist)
            {
                DicLocation[shop] = shop;
            }
            JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();
            Purchase.Mode = ireadonly == 1 ? "readonly" : "";

            PoQtyAmtList = PoSettingsEditModel.GetPoQtyAmtList();

            void getDicSupInfoes(List<IGrouping<string, SupplierModel>> groupedsupPurchaseInfoes)
            {
                foreach (var group in groupedsupPurchaseInfoes)
                {
                    var g = group.FirstOrDefault();
                    if (!DicSupInfoes.ContainsKey(g.supCode)) DicSupInfoes[g.supCode] = new List<SupplierModel>();
                }
                foreach (var group in groupedsupPurchaseInfoes)
                {
                    var g = group.FirstOrDefault();
                    if (DicSupInfoes.ContainsKey(g.supCode))
                    {
                        foreach (var spi in group)
                        {
                            spi.filePath = ModelHelper.GetPDFLnk(Purchase.pstCode, g.supCode, spi.fileName);
                            DicSupInfoes[g.supCode].Add(spi);
                        }
                    }
                }
            }
        }

        private string GenInvoiceId(string pstCode, int numId)
        {
            return CommonHelper.GenOrderNumber(pstCode, int.Parse(ConfigurationManager.AppSettings["SupInvoiceLength"]), numId);
        }

        private long addNewPurchase(MMDbContext context, int apId, string pstcode, string status, string pqstatus = null, bool addNewModel = true, string oldpstCode = null)
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

        public static List<PurchaseReturnMsg> Edit(PurchaseModel model, List<SupplierModel> SupplierList)
        {
            List<PurchaseReturnMsg> msglist = new List<PurchaseReturnMsg>();
            List<Superior> SuperiorList = new List<Superior>();
            string supnames = string.Join(",", SupplierList.Select(x => x.supName).Distinct().ToList());

            string status = "";
            string msg = string.Format(Resources.Resource.SavedFormat, Resources.Resource.PurchaseOrder);
            string purchasestatus = string.Empty;


            IsUserRole IsUserRole = UserEditModel.GetIsUserRole(user);

            DeviceModel dev = HttpContext.Current.Session["Device"] as DeviceModel;

            using var context = new MMDbContext();
            DateTime dateTime = DateTime.Now;

            DateTime purchasedate = CommonHelper.GetDateFrmString4SQL(model.JsPurchaseDate, DateTimeFormat.YYYYMMDD);
            DateTime promiseddate = CommonHelper.GetDateFrmString4SQL(model.JsPromisedDate, DateTimeFormat.YYYYMMDD);

            purchasestatus = model.pstStatus.ToLower() == PurchaseStatus.draft.ToString()
                ? PurchaseStatus.draft.ToString()
                : IsUserRole.isdirectorboard ? model.pstStatus : !model.IsEditMode ? PurchaseStatus.requesting.ToString() : model.pstStatus;
            //pqstatus = model.pstStatus.ToLower() == PurchaseStatus.draft.ToString()
            //	? RequestStatus.draftingByStaff.ToString() : model.IsEditMode ? RequestStatus.requestingByStaff.ToString():;
            //getPurchaseRequestStatus(out reactType, IsUserRole, ref purchase, ref pstStatus, out string pqStatus);

            List<string> reviewurls = new();
            List<GetSuperior4Notification2_Result> superiors = new();
            Dictionary<string, string> DicReviewUrl = new Dictionary<string, string>();
            Dictionary<string, Dictionary<string, int>> dicItemLocQty = new Dictionary<string, Dictionary<string, int>>();

            if (IsUserRole.isdirectorboard)
            {
                if (!model.IsEditMode)
                    updatePurchaseRequest(model, SupplierList, context, dateTime, purchasedate, promiseddate, purchasestatus);
                else
                    processPurchase(model, dev, context, dateTime, purchasedate, promiseddate, purchasestatus, SupplierList);
            }
            else
            {
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

                if (!model.IsEditMode)
                {
                    updatePurchaseRequest(model, SupplierList, context, dateTime, purchasedate, promiseddate, purchasestatus);
                }
                else
                {
                    processPurchase(model, dev, context, dateTime, purchasedate, promiseddate, purchasestatus, SupplierList);
                }

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
                            if (Helpers.ModelHelper.SendNotificationEmail(DicReviewUrl, reactType, model.pstDesc))
                            {
                                var purchase = context.Purchases.FirstOrDefault(x => x.Id == model.Id);
                                purchase.pstSendNotification = true;
                                context.SaveChanges();
                            }
                        }
                        #endregion
                    }
                    GenReturnMsgList(model, supnames, ref msglist, SuperiorList, status, IsUserRole.isdirectorboard, DicReviewUrl, IsUserRole.ismuseumdirector);
                }
            }

            return msglist;
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
                    remark = model.pstRemark
                });
            }
        }

        private static void processPurchase(PurchaseModel model, DeviceModel dev, MMDbContext context, DateTime dateTime, DateTime purchasedate, DateTime promiseddate, string purchasestatus, List<SupplierModel> SupplierList)
        {
            MMDAL.Purchase ps = updatePurchaseRequest(model, SupplierList, context, dateTime, purchasedate, promiseddate, purchasestatus);

            if (purchasestatus.ToLower() == PurchaseStatus.order.ToString())
            {
                #region Update Purchase Status
                // update wholesales status:                
                ps.pstStatus = PurchaseStatus.order.ToString();
                ps.ModifyTime = dateTime;
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
                    AccountProfileId = apId,
                    CreateBy = user.UserCode,
                    CreateTime = dateTime,
                };
                context.Purchases.Add(purchase);
                device.dvcNextPurchaseOrderNo++;
                context.SaveChanges();
                #endregion
            }
        }


        private static MMDAL.Purchase updatePurchaseRequest(PurchaseModel model, List<SupplierModel> SupplierList, MMDbContext context, DateTime dateTime, DateTime purchasedate, DateTime promiseddate, string purchasestatus)
        {
            var supcodes = string.Join(",", SupplierList.Select(x => x.supCode).Distinct().ToList());
            MMDAL.Purchase ps = _updatePurchaseRequest(model, supcodes, context, dateTime, purchasedate, promiseddate, purchasestatus);

            AddSelectedSuppliers(model.pstCode, SupplierList, context);
            return ps;
        }

        private static MMDAL.Purchase _updatePurchaseRequest(PurchaseModel model, string supcodes, MMDbContext context, DateTime dateTime, DateTime purchasedate, DateTime promiseddate, string purchasestatus)
        {
            var pstTime = purchasedate.Add(dateTime.TimeOfDay);
            MMDAL.Purchase ps = context.Purchases.Find(model.Id);

            var pqstatus = model.pstStatus.ToLower() == PurchaseStatus.draft.ToString() ? RequestStatus.draftingByStaff.ToString() : model.pqStatus;

            if (ps != null)
            {
                ps.supCode = supcodes;
                ps.pstType = PurchaseType;
                ps.pstDesc = model.pstDesc;
                ps.pstAmount = model.pstAmount;
                ps.pstPurchaseDate = purchasedate;
                ps.pstPurchaseTime = pstTime;
                ps.pstPromisedDate = promiseddate;
                ps.pstStatus = purchasestatus;
                ps.pqStatus = pqstatus;
                ps.pstSupplierInvoice = model.pstSupplierInvoice;
                ps.pstRemark = model.pstRemark;

                ps.pstCheckout = false;
                ps.pstIsPartial = false;

                ps.CreateBy = ps.ModifyBy = user.UserCode; //maybe created by staff01 first, but populated by staff02 later 
                ps.ModifyTime = dateTime;
                context.SaveChanges();
            }

            return ps;
        }

        private static void AddSelectedSuppliers(string pstCode, List<SupplierModel> SupplierList, MMDbContext context)
        {
            var currentIds = context.PurchaseSuppliers.Where(x => x.AccountProfileId == apId && x.pstCode == pstCode).Select(x => x.Id).ToList();
            List<PurchaseSupplier> pslist = new List<PurchaseSupplier>();
            foreach (SupplierModel supplier in SupplierList)
            {
                if (!currentIds.Contains(supplier.Id))
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



        public static void Delete(int id)
        {
            using var context = new MMDbContext();
            var ps = context.Purchases.FirstOrDefault(x => x.Id == id);
            var psilist = context.PurchaseItems.Where(x => x.AccountProfileId == ps.AccountProfileId && x.pstCode == ps.pstCode);
            context.PurchaseItems.RemoveRange(psilist);
            context.Purchases.Remove(ps);
            context.SaveChanges();
        }

        public static List<string> GetUploadPurchaseSqlList(int accountprofileId, ref DataTransferModel dmodel, string strfrmdate, string strtodate, ComInfo comInfo, MMDbContext context, DateTime? dateFrm = null, DateTime? dateTo = null, string connectionString = null)
        {
            if (dateFrm == null && dateTo == null)
            {
                #region Date Ranges
                int year = DateTime.Now.Year;
                DateTime frmdate;
                DateTime todate;
                if (string.IsNullOrEmpty(strfrmdate))
                {
                    frmdate = DateTime.Today;
                }
                else
                {
                    int mth = int.Parse(strfrmdate.Split('/')[1]);
                    int day = int.Parse(strfrmdate.Split('/')[0]);
                    year = int.Parse(strfrmdate.Split('/')[2]);
                    frmdate = new DateTime(year, mth, day);
                }
                if (string.IsNullOrEmpty(strtodate))
                {
                    todate = DateTime.Today;
                }
                else
                {
                    int mth = int.Parse(strtodate.Split('/')[1]);
                    int day = int.Parse(strtodate.Split('/')[0]);
                    year = int.Parse(strtodate.Split('/')[2]);
                    todate = new DateTime(year, mth, day);
                }
                #endregion

                dmodel.FrmToDate = frmdate;
                dmodel.ToDate = todate;
            }
            else
            {
                dmodel.FrmToDate = (DateTime)dateFrm;
                dmodel.ToDate = (DateTime)dateTo;
            }

            var dateformatcode = context.AppParams.FirstOrDefault(x => x.appParam == "DateFormat").appVal;
            List<string> sqllist = new List<string>();

            Helpers.ModelHelper.GetDataTransferData(context, accountprofileId, CheckOutType.Purchase, ref dmodel, connectionString);

            #region Handle ItemOptions
            var itemcodes = dmodel.ItemCodes;
            var itemoptions = context.GetItemOptionsByItemCodes6(comInfo.AccountProfileId, string.Join(",", itemcodes)).ToList();
            var stritemcodes = string.Join(",", itemcodes);
            var location = dmodel.SelectedLocation;

            var itembtInfo = context.GetBatchVtInfoByItemCodes12(comInfo.AccountProfileId, location, stritemcodes).ToList();
            var itemvtInfo = context.GetValidThruInfo12(comInfo.AccountProfileId, location, stritemcodes).ToList();

            var ibqList = (from item in itembtInfo
                           where item.batCode != null
                           group item by new { item.itmCode, item.batCode } into itemgroup
                           select new
                           {
                               ItemCode = itemgroup.Key.itmCode,
                               BatchCode = itemgroup.Key.batCode,
                               TotalQty = itemgroup.Sum(x => x.batQty)
                           }).ToList();
            ibqList = ibqList.OrderBy(x => x.ItemCode).ThenBy(x => x.BatchCode).ToList();

            var ivqList = (from item in itemvtInfo
                           where item.piValidThru != null
                           group item by new { item.itmCode, item.piValidThru, item.pstCode } into itemgroup
                           select new
                           {
                               PoCode = itemgroup.Key.pstCode,
                               ItemCode = itemgroup.Key.itmCode,
                               ValidThru = CommonHelper.FormatDate((DateTime)itemgroup.Key.piValidThru),
                               TotalQty = itemgroup.Sum(x => x.vtQty)
                           }).ToList();
            ivqList = ivqList.OrderBy(x => x.ItemCode).ToList();

            var serialInfo = context.GetSerialInfo5(comInfo.AccountProfileId, location, stritemcodes, null).ToList();
            var ibvqList = (from item in itembtInfo
                            where item.batCode != null
                            //group item by new { item.itmCode, item.ivBatCode, item.piValidThru } into itemgroup
                            select new
                            {
                                PoCode = item.pstCode,
                                ItemCode = item.itmCode,
                                BatchCode = item.batCode,
                                ValidThru = item.batValidThru == null ? "" : CommonHelper.FormatDate((DateTime)item.batValidThru),
                                BatQty = (int)item.batQty
                            }).ToList();
            ibvqList = ibvqList.OrderBy(x => x.ItemCode).ThenBy(x => x.BatchCode).ToList();

            var batchcodelist = ibvqList.Select(x => x.BatchCode).Distinct().ToList();

            var DicItemBatVtList = new Dictionary<string, Dictionary<string, List<string>>>();
            var DicItemBatSnVtList = new Dictionary<string, Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>>();
            var DicItemSnVtList = new Dictionary<string, List<SnVt>>();
            var DicItemVtList = new Dictionary<string, List<string>>();
            foreach (var itemcode in itemcodes)
            {
                DicItemBatVtList[itemcode] = new Dictionary<string, List<string>>();
                DicItemBatSnVtList[itemcode] = new Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>();
                DicItemSnVtList[itemcode] = new List<SnVt>();
                DicItemVtList[itemcode] = new List<string>();

                foreach (var batchcode in batchcodelist)
                {
                    DicItemBatVtList[itemcode][batchcode] = new List<string>();
                    DicItemBatSnVtList[itemcode][batchcode] = new List<MMLib.Models.Purchase.BatSnVt>();
                }
            }
            #endregion

            foreach (var po in dmodel.DicPoPurchaseItemList.Keys)
            {
                string sql = MyobHelper.InsertImportItemPurchasesSql;
                sql = sql.Replace("0", "{0}");
                List<string> values = null;

                List<string> columns = new List<string>();
                for (int j = 0; j < MyobHelper.ImportItemPurchasesColCount; j++)
                {
                    columns.Add("'{" + j + "}'");
                }
                string strcolumn = string.Join(",", columns);
                var purchaseitems = dmodel.DicPoPurchaseItemList[po];
                //string status = purchaseitems.Any(x => x.IsPartial != null && (bool)x.IsPartial) ? "B" : "O";
                string status = "O";
                values = new List<string>();
                foreach (var purchaseitem in dmodel.DicPoPurchaseItemList[po])
                {
                    string description = string.Empty;
                    string value = "";
                    purchaseitem.dateformat = dateformatcode == "E" ? @"dd/MM/yyyy" : @"MM/dd/yyyy";
                    var itemoption = itemoptions.FirstOrDefault(x => x.itmCode == purchaseitem.itmCode);

                    string bat = string.Empty;
                    string sn = string.Empty;
                    string vt = string.Empty;
                    string batvt = string.Empty;
                    string snvt = string.Empty;
                    string batsn = string.Empty;

                    string _batvtInfo = string.Empty;
                    List<string> batvtInfo = new List<string>();
                    string _snvtInfo = string.Empty;
                    List<string> snvtInfo = new List<string>();
                    List<string> vtInfo = new List<string>();

                    #region Handle Item Options
                    if (itemoption != null)
                    {
                        if (itemoption.chkBat)
                        {
                            if (ibvqList.Count > 0)
                            {
                                foreach (var item in ibvqList)
                                {
                                    string strvt = "";
                                    if (purchaseitem.itmCode == item.ItemCode)
                                    {
                                        foreach (var kv in DicItemBatVtList)
                                        {
                                            if (!(bool)itemoption.chkSN && (bool)itemoption.chkVT)
                                            {
                                                if (kv.Key == item.ItemCode)
                                                {
                                                    var vtarr = new List<string>();
                                                    foreach (var k in kv.Value.Keys)
                                                    {
                                                        if (k == item.BatchCode && !string.IsNullOrEmpty(item.ValidThru))
                                                        {
                                                            DicItemBatVtList[kv.Key][k].Add(item.ValidThru);
                                                            vtarr.Add(item.ValidThru);
                                                        }
                                                    }
                                                    if (vtarr.Count > 0)
                                                        strvt = string.Concat(" (", string.Join(",", vtarr), ")");
                                                }
                                            }
                                        }

                                        bat = string.Concat("[", item.BatchCode, "]");
                                        if (!string.IsNullOrEmpty(strvt))
                                        {
                                            batvt = string.Concat(bat, strvt);
                                        }
                                        //Response.Write(item.PoCode + ":" + item.ItemCode + ":" + bat + ":" + batvt + "<br>");
                                        _batvtInfo = string.IsNullOrEmpty(batvt) ? bat : batvt;
                                        batvtInfo.Add(_batvtInfo);
                                    }
                                }
                            }
                        }

                        if (itemoption.chkSN)
                        {
                            if (serialInfo.Count > 0)
                            {
                                foreach (var serial in serialInfo)
                                {
                                    string strvt = "";
                                    if (purchaseitem.itmCode == serial.snoItemCode)
                                    {
                                        foreach (var kv in DicItemSnVtList)
                                        {
                                            if (!(bool)itemoption.chkBat && (bool)itemoption.chkVT)
                                            {
                                                if (kv.Key == serial.snoItemCode)
                                                {
                                                    var vtarr = new List<string>();

                                                    foreach (var v in kv.Value)
                                                    {
                                                        if (!string.IsNullOrEmpty(v.vt))
                                                        {
                                                            DicItemSnVtList[kv.Key].Add(v);
                                                            vtarr.Add(v.vt);
                                                        }
                                                    }
                                                    if (vtarr.Count > 0)
                                                        strvt = string.Concat(" (", string.Join(",", vtarr), ")");
                                                }
                                            }
                                        }

                                        sn = string.Concat("[", serial.snoCode, "]");
                                        if (!string.IsNullOrEmpty(strvt))
                                        {
                                            snvt = string.Concat(sn, strvt);
                                        }
                                        _snvtInfo = string.IsNullOrEmpty(snvt) ? sn : snvt;
                                        snvtInfo.Add(_snvtInfo);
                                    }
                                }
                            }
                        }

                        if (itemoption.chkBat && itemoption.chkSN && itemoption.chkVT)
                        {
                            if (ivqList != null && ivqList.Count() > 0)
                            {
                                foreach (var v in ivqList)
                                {
                                    vt = string.Concat("[", v.ValidThru, "]");
                                    vtInfo.Add(vt);
                                }
                            }
                        }
                    }

                    if (batvtInfo.Count > 0)
                    {
                        description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", batvtInfo));
                    }
                    else if (snvtInfo.Count > 0)
                    {
                        description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", snvtInfo));
                    }
                    else if (vtInfo.Count > 0)
                    {
                        description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", vtInfo));
                    }
                    else
                    {
                        description = purchaseitem.itmNameDesc;
                    }
                    #endregion

                    if (description.Length > 255)
                        description = description.Substring(0, 255);
                    string memo = genMemo(purchaseitem.pstCurrency, purchaseitem.pstExRate, Convert.ToDouble(purchaseitem.piAmt), purchaseitem.pstRemark);

                    /*PurchaseNumber,PurchaseDate,SuppliersNumber,DeliveryStatus,ItemNumber,Quantity,Price,Discount,Memo,DeliveryDate,PurchaseStatus,AmountPaid,Ordered,Location,CardID,CurrencyCode,ExchangeRate,TaxCode,Job,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount
					 */
                    value = string.Format("(" + strcolumn + ")", purchaseitem.pstCode, purchaseitem.PurchaseDate4ABSS, StringHandlingForSQL(purchaseitem.pstSupplierInvoice), "A", StringHandlingForSQL(purchaseitem.itmCode), purchaseitem.piQty, purchaseitem.piUnitPrice, purchaseitem.piDiscPc, memo, purchaseitem.PromisedDate4ABSS, status, "0", purchaseitem.piReceivedQty, purchaseitem.piStockLoc, StringHandlingForSQL(purchaseitem.supCode), StringHandlingForSQL(purchaseitem.pstCurrency), purchaseitem.pstExRate, purchaseitem.TaxCode, purchaseitem.JobNumber, purchaseitem.Myob_PaymentIsDue, purchaseitem.Myob_DiscountDays, purchaseitem.Myob_BalanceDueDays, "0");
                    values.Add(value);
                }
                sql = string.Format(sql, string.Join(",", values));
                sqllist.Add(sql);
            }

            return sqllist;
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

        public static List<string> GetUploadPayBillSqlList(int accountprofileId, ref DataTransferModel dmodel, string strfrmdate, string strtodate, ComInfo comInfo, MMDbContext context, DateTime? dateFrm = null, DateTime? dateTo = null, string connectionString = null)
        {
            if (dateFrm == null && dateTo == null)
            {
                #region Date Ranges
                int year = DateTime.Now.Year;
                DateTime frmdate;
                DateTime todate;
                if (string.IsNullOrEmpty(strfrmdate))
                {
                    frmdate = DateTime.Today;
                }
                else
                {
                    int mth = int.Parse(strfrmdate.Split('/')[1]);
                    int day = int.Parse(strfrmdate.Split('/')[0]);
                    year = int.Parse(strfrmdate.Split('/')[2]);
                    frmdate = new DateTime(year, mth, day);
                }
                if (string.IsNullOrEmpty(strtodate))
                {
                    todate = DateTime.Today;
                }
                else
                {
                    int mth = int.Parse(strtodate.Split('/')[1]);
                    int day = int.Parse(strtodate.Split('/')[0]);
                    year = int.Parse(strtodate.Split('/')[2]);
                    todate = new DateTime(year, mth, day);
                }
                #endregion

                dmodel.FrmToDate = frmdate;
                dmodel.ToDate = todate;
            }
            else
            {
                dmodel.FrmToDate = (DateTime)dateFrm;
                dmodel.ToDate = (DateTime)dateTo;
            }

            var dateformatcode = context.AppParams.FirstOrDefault(x => x.appParam == "DateFormat").appVal;
            List<string> sqllist = new List<string>();

            Helpers.ModelHelper.GetDataTransferData(context, accountprofileId, CheckOutType.PayBills, ref dmodel, connectionString);

            #region Handle ItemOptions
            var itemcodes = dmodel.ItemCodes;
            var itemoptions = context.GetItemOptionsByItemCodes6(comInfo.AccountProfileId, string.Join(",", itemcodes)).ToList();
            var stritemcodes = string.Join(",", itemcodes);
            var location = dmodel.SelectedLocation;

            var itembtInfo = context.GetBatchVtInfoByItemCodes12(comInfo.AccountProfileId, location, stritemcodes).ToList();
            var itemvtInfo = context.GetValidThruInfo12(comInfo.AccountProfileId, location, stritemcodes).ToList();

            var ibqList = (from item in itembtInfo
                           where item.batCode != null
                           group item by new { item.itmCode, item.batCode } into itemgroup
                           select new
                           {
                               ItemCode = itemgroup.Key.itmCode,
                               BatchCode = itemgroup.Key.batCode,
                               TotalQty = itemgroup.Sum(x => x.batQty)
                           }).ToList();
            ibqList = ibqList.OrderBy(x => x.ItemCode).ThenBy(x => x.BatchCode).ToList();

            var ivqList = (from item in itemvtInfo
                           where item.piValidThru != null
                           group item by new { item.itmCode, item.piValidThru, item.pstCode } into itemgroup
                           select new
                           {
                               PoCode = itemgroup.Key.pstCode,
                               ItemCode = itemgroup.Key.itmCode,
                               ValidThru = CommonHelper.FormatDate((DateTime)itemgroup.Key.piValidThru),
                               TotalQty = itemgroup.Sum(x => x.vtQty)
                           }).ToList();
            ivqList = ivqList.OrderBy(x => x.ItemCode).ToList();

            var serialInfo = context.GetSerialInfo5(comInfo.AccountProfileId, location, stritemcodes, null).ToList();
            var ibvqList = (from item in itembtInfo
                            where item.batCode != null
                            //group item by new { item.itmCode, item.ivBatCode, item.piValidThru } into itemgroup
                            select new
                            {
                                PoCode = item.pstCode,
                                ItemCode = item.itmCode,
                                BatchCode = item.batCode,
                                ValidThru = item.batValidThru == null ? "" : CommonHelper.FormatDate((DateTime)item.batValidThru),
                                BatQty = (int)item.batQty
                            }).ToList();
            ibvqList = ibvqList.OrderBy(x => x.ItemCode).ThenBy(x => x.BatchCode).ToList();

            var batchcodelist = ibvqList.Select(x => x.BatchCode).Distinct().ToList();

            var DicItemBatVtList = new Dictionary<string, Dictionary<string, List<string>>>();
            var DicItemBatSnVtList = new Dictionary<string, Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>>();
            var DicItemSnVtList = new Dictionary<string, List<SnVt>>();
            var DicItemVtList = new Dictionary<string, List<string>>();
            foreach (var itemcode in itemcodes)
            {
                DicItemBatVtList[itemcode] = new Dictionary<string, List<string>>();
                DicItemBatSnVtList[itemcode] = new Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>();
                DicItemSnVtList[itemcode] = new List<SnVt>();
                DicItemVtList[itemcode] = new List<string>();

                foreach (var batchcode in batchcodelist)
                {
                    DicItemBatVtList[itemcode][batchcode] = new List<string>();
                    DicItemBatSnVtList[itemcode][batchcode] = new List<MMLib.Models.Purchase.BatSnVt>();
                }
            }
            #endregion

            foreach (var po in dmodel.DicPoPurchaseItemList.Keys)
            {
                string sql = MyobHelper.InsertImportItemPurchasesSql;
                sql = sql.Replace("0", "{0}");
                List<string> values = null;

                List<string> columns = new List<string>();
                for (int j = 0; j < MyobHelper.ImportItemPurchasesColCount; j++)
                {
                    columns.Add("'{" + j + "}'");
                }
                string strcolumn = string.Join(",", columns);
                var purchaseitems = dmodel.DicPoPurchaseItemList[po];
                //string status = purchaseitems.Any(x => x.IsPartial != null && (bool)x.IsPartial) ? "B" : "O";
                string status = "O";
                values = new List<string>();
                foreach (var purchaseitem in dmodel.DicPoPurchaseItemList[po])
                {
                    string description = string.Empty;
                    string value = "";
                    purchaseitem.dateformat = dateformatcode == "E" ? @"dd/MM/yyyy" : @"MM/dd/yyyy";
                    var itemoption = itemoptions.FirstOrDefault(x => x.itmCode == purchaseitem.itmCode);

                    string bat = string.Empty;
                    string sn = string.Empty;
                    string vt = string.Empty;
                    string batvt = string.Empty;
                    string snvt = string.Empty;
                    string batsn = string.Empty;

                    string _batvtInfo = string.Empty;
                    List<string> batvtInfo = new List<string>();
                    string _snvtInfo = string.Empty;
                    List<string> snvtInfo = new List<string>();
                    List<string> vtInfo = new List<string>();

                    #region Handle Item Options
                    if (itemoption != null)
                    {
                        if (itemoption.chkBat)
                        {
                            if (ibvqList.Count > 0)
                            {
                                foreach (var item in ibvqList)
                                {
                                    string strvt = "";
                                    if (purchaseitem.itmCode == item.ItemCode)
                                    {
                                        foreach (var kv in DicItemBatVtList)
                                        {
                                            if (!(bool)itemoption.chkSN && (bool)itemoption.chkVT)
                                            {
                                                if (kv.Key == item.ItemCode)
                                                {
                                                    var vtarr = new List<string>();
                                                    foreach (var k in kv.Value.Keys)
                                                    {
                                                        if (k == item.BatchCode && !string.IsNullOrEmpty(item.ValidThru))
                                                        {
                                                            DicItemBatVtList[kv.Key][k].Add(item.ValidThru);
                                                            vtarr.Add(item.ValidThru);
                                                        }
                                                    }
                                                    if (vtarr.Count > 0)
                                                        strvt = string.Concat(" (", string.Join(",", vtarr), ")");
                                                }
                                            }
                                        }

                                        bat = string.Concat("[", item.BatchCode, "]");
                                        if (!string.IsNullOrEmpty(strvt))
                                        {
                                            batvt = string.Concat(bat, strvt);
                                        }
                                        //Response.Write(item.PoCode + ":" + item.ItemCode + ":" + bat + ":" + batvt + "<br>");
                                        _batvtInfo = string.IsNullOrEmpty(batvt) ? bat : batvt;
                                        batvtInfo.Add(_batvtInfo);
                                    }
                                }
                            }
                        }

                        if (itemoption.chkSN)
                        {
                            if (serialInfo.Count > 0)
                            {
                                foreach (var serial in serialInfo)
                                {
                                    string strvt = "";
                                    if (purchaseitem.itmCode == serial.snoItemCode)
                                    {
                                        foreach (var kv in DicItemSnVtList)
                                        {
                                            if (!(bool)itemoption.chkBat && (bool)itemoption.chkVT)
                                            {
                                                if (kv.Key == serial.snoItemCode)
                                                {
                                                    var vtarr = new List<string>();

                                                    foreach (var v in kv.Value)
                                                    {
                                                        if (!string.IsNullOrEmpty(v.vt))
                                                        {
                                                            DicItemSnVtList[kv.Key].Add(v);
                                                            vtarr.Add(v.vt);
                                                        }
                                                    }
                                                    if (vtarr.Count > 0)
                                                        strvt = string.Concat(" (", string.Join(",", vtarr), ")");
                                                }
                                            }
                                        }

                                        sn = string.Concat("[", serial.snoCode, "]");
                                        if (!string.IsNullOrEmpty(strvt))
                                        {
                                            snvt = string.Concat(sn, strvt);
                                        }
                                        _snvtInfo = string.IsNullOrEmpty(snvt) ? sn : snvt;
                                        snvtInfo.Add(_snvtInfo);
                                    }
                                }
                            }
                        }

                        if (itemoption.chkBat && itemoption.chkSN && itemoption.chkVT)
                        {
                            if (ivqList != null && ivqList.Count() > 0)
                            {
                                foreach (var v in ivqList)
                                {
                                    vt = string.Concat("[", v.ValidThru, "]");
                                    vtInfo.Add(vt);
                                }
                            }
                        }
                    }

                    if (batvtInfo.Count > 0)
                    {
                        description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", batvtInfo));
                    }
                    else if (snvtInfo.Count > 0)
                    {
                        description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", snvtInfo));
                    }
                    else if (vtInfo.Count > 0)
                    {
                        description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", vtInfo));
                    }
                    else
                    {
                        description = purchaseitem.itmNameDesc;
                    }
                    #endregion

                    if (description.Length > 255)
                        description = description.Substring(0, 255);
                    string memo = genMemo(purchaseitem.pstCurrency, purchaseitem.pstExRate, Convert.ToDouble(purchaseitem.piAmt), purchaseitem.pstRemark);

                    /*PurchaseNumber,PurchaseDate,SuppliersNumber,DeliveryStatus,ItemNumber,Quantity,Price,Discount,Memo,DeliveryDate,PurchaseStatus,AmountPaid,Ordered,Location,CardID,CurrencyCode,ExchangeRate,TaxCode,Job,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount
					 */
                    value = string.Format("(" + strcolumn + ")", purchaseitem.pstCode, purchaseitem.PurchaseDate4ABSS, StringHandlingForSQL(purchaseitem.pstSupplierInvoice), "A", StringHandlingForSQL(purchaseitem.itmCode), purchaseitem.piQty, purchaseitem.piUnitPrice, purchaseitem.piDiscPc, memo, purchaseitem.PromisedDate4ABSS, status, "0", purchaseitem.piReceivedQty, purchaseitem.piStockLoc, StringHandlingForSQL(purchaseitem.supCode), StringHandlingForSQL(purchaseitem.pstCurrency), purchaseitem.pstExRate, purchaseitem.TaxCode, purchaseitem.JobNumber, purchaseitem.Myob_PaymentIsDue, purchaseitem.Myob_DiscountDays, purchaseitem.Myob_BalanceDueDays, "0");
                    values.Add(value);
                }
                sql = string.Format(sql, string.Join(",", values));
                sqllist.Add(sql);
            }

            return sqllist;
        }

        public void GetPurchaseRequestStatus(ref ReactType reactType, IsUserRole IsUserRole, ref MMDAL.Purchase purchase, out string pqStatus, string SelectedSupCode, MMDbContext context)
        {
            pqStatus = "";
            if (purchase.pstStatus == RequestStatus.approved.ToString())
            {
                reactType = ReactType.PassedByDeptHead;
                if (IsUserRole.isdirectorboard || IsUserRole.ismuseumdirector)
                {
                    purchase.pstStatus = PurchaseStatus.order.ToString();
                    pqStatus = RequestStatus.approved.ToString();
                    reactType = ReactType.Approved;                    
                    PurchaseSupplier purchaseSupplier = context.PurchaseSuppliers.FirstOrDefault(x => x.supCode == SelectedSupCode);
                    purchaseSupplier.Selected = true;
                    purchaseSupplier.ModifyTime = DateTime.Now;
                    purchase.supCode = SelectedSupCode;
                    purchase.pstAmount = purchaseSupplier.Amount;
                    //purchase.ModifyTime = DateTime.Now;
                    context.SaveChanges();
                }

                if (IsUserRole.isdepthead)
                {
                    pqStatus = RequestStatus.requestingByDeptHead.ToString();
                    reactType = ReactType.PassedByDeptHead;
                }

                if (IsUserRole.isfinancedept)
                {
                    pqStatus = RequestStatus.requestingByFinanceDept.ToString();
                    reactType = ReactType.PassedByFinanceDept;
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
            List<SupplierPaymentModel> supplierpayments = sqlConnection.Query<SupplierPaymentModel>(@"EXEC dbo.GetSupplierPaymentsByCode @apId=@apId,@pstCode=@pstCode,@supCode=@supCode,@checkout=@checkout", new { apId, purchase.pstCode, purchase.supCode, checkout = false }).ToList();

            if (supplierpayments.Count > 0)
            {
                sql = MyobHelper.InsertImportPayBillsSql;
                sql = sql.Replace("0", "{0}");
                values = [];
                columns = [];
                strcolumn = "";

                for (int j = 0; j < MyobHelper.ImportPayBillsColCount; j++)
                {
                    columns.Add("'{" + j + "}'");
                }
                strcolumn = string.Join(",", columns);
                value = "";

                //string payac = comInfo.comPayAccountNo.Replace("-", "");

                dmodel.CheckOutIds_SupPayLn = new HashSet<long>();

                foreach (var sp in supplierpayments)
                {
                    dmodel.CheckOutIds_SupPayLn.Add(sp.Id);
                    sp.dateformat = purchase.dateformat;
                    string payac = sp.supAccount.Replace("-", "");
                    //INSERT INTO Import_Pay_Bills (CoLastName,PaymentAccount,PurchaseNumber,SuppliersNumber,AmountApplied,PaymentDate,ChequeNumber) VALUES ('{supname}','{payac}','{pono}','{supno}','500','{date}','897852')
                    value = string.Format("(" + strcolumn + ")", StringHandlingForSQL(sp.SupplierName), payac, sp.pstCode, StringHandlingForSQL(purchase.pstSupplierInvoice), Convert.ToDouble(sp.Amount), sp.PayDate4ABSS, StringHandlingForSQL(sp.spChequeNo));
                    values.Add(value);
                }

                sql = string.Format(sql, string.Join(",", values));
                sqllist.Add(sql);
            }
            sqlConnection.Close();
            sqlConnection.Dispose();
            #endregion




            return sqllist;
        }

        public static PurchaseModel getPurchaseModelById(long Id)
        {
            var connection = new SqlConnection(defaultConnection);
            connection.Open();
            return connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id", new { apId, Id });
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
