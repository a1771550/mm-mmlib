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
using MMLib.Models.Purchase.Supplier;
using MMLib.Models.POS.Settings;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Text;
using ItemModel = MMLib.Models.Item.ItemModel;
using MMLib.Models.MYOB;
using MMLib.Models.User;


namespace MMLib.Models.Purchase
{
	public class PurchaseEditModel : PagingBaseModel
    {
        public PurchaseStatus ListMode { get; set; }
        public PurchaseModel Purchase { get; set; }

        public ReceiptViewModel Receipt;
        public List<string> DisclaimerList;
        public List<string> PaymentTermsList;
        public PurchaseEditModel(string receiptno, int? ireadonly)
        {
            Get(0, receiptno, ireadonly);
        }
        public PurchaseEditModel(long Id, string type, bool forprint = false)
        {
            Get(Id, null, 0, type, forprint);
        }

        public void Get(long Id = 0, string receiptno = null, int? ireadonly = 0, string type = "", bool forprint = false)
        {
            MMDbContext context;
            Device device=null;
            bool isapprover = (bool)HttpContext.Current.Session["IsApprover"];            
            get0(isapprover, out context, ref device);
            SqlConnection connection = get1(Id, receiptno);

            if (Id > 0 || !string.IsNullOrEmpty(receiptno))
            {
                GetPurchaseItemList(context, Purchase);

                if (forprint)
                {
                    Receipt = new ReceiptViewModel();
                    DisclaimerList = new List<string>();
                    PaymentTermsList = new List<string>();
                    Helpers.ModelHelper.GetReady4Print(context, ref Receipt, ref DisclaimerList, ref PaymentTermsList);
                }

                #region Handle View File
                Helpers.ModelHelper.HandleViewFile(Purchase.UploadFileName, Purchase.AccountProfileId, Purchase.pstCode, ref Purchase.ImgList, ref Purchase.FileList);
                #endregion
            }
            else
            {
                Purchase = new PurchaseModel
                {
                    AccountProfileId = ComInfo.AccountProfileId
                };
                if (device != null)
                {
					var purchaseinitcode = device.dvcPurchasePrefix ?? "IP";
					var purchaseno = $"{device.dvcNextPurchaseNo:000000}";
					Purchase.pstCode = string.Concat(purchaseinitcode, purchaseno);
					Purchase.pstStatus = type;
				}               
            }
            get2(context, device);
            Purchase.JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();
            Purchase.Mode = ireadonly == 1 ? "readonly" : "";
            
        }
        private void get2(MMDbContext context, Device device)
        {
            Purchase.Device = device!=null? device as DeviceModel:null;
            SupplierEditModel model = new(Purchase.supCode);
            Purchase.Supplier = model.Supplier;
            var supplierlist = context.Suppliers.Where(x => x.AccountProfileId == ComInfo.AccountProfileId && (bool)x.supCheckout).OrderBy(x => x.supName).ToList();
            Purchase.SupplierList = new List<SelectListItem>();
            foreach (var supplier in supplierlist)
            {
                Purchase.SupplierList.Add(
                    new SelectListItem
                    {
                        Value = supplier.supCode,
                        Text = supplier.supName,
                    }
                );
            }

            var stocklocationlist = NonABSS ? ComInfo.Shops.Split(',').ToList() : context.GetStockLocationList1(ComInfo.AccountProfileId).ToList();
            Purchase.LocationList = new List<SelectListItem>();
            foreach (var item in stocklocationlist)
            {
                Purchase.LocationList.Add(new SelectListItem
                {
                    Value = item,
                    Text = item
                });
            }

            if (!NonAbss)
            {
                var myobcurrencylist = context.MyobCurrencies.Where(x => x.AccountProfileId == ComInfo.AccountProfileId).ToList();
                if (myobcurrencylist != null && myobcurrencylist.Count > 0)
                {
                    Purchase.DicCurrencyExRate = new Dictionary<string, double>();
                    foreach (var currency in myobcurrencylist)
                    {
                        Purchase.DicCurrencyExRate[currency.CurrencyCode] = currency.ExchangeRate ?? 1;
                    }
                }
            }
            Purchase.TaxModel = Helpers.ModelHelper.GetTaxInfo(context);
            Purchase.UseForexAPI = ExchangeRateEditModel.GetForexInfo(context);
            Purchase.DicLocation = new Dictionary<string, string>();
            foreach (var shop in stocklocationlist)
            {
                Purchase.DicLocation[shop] = shop;
            }
        }

        private SqlConnection get1(long Id = 0, string receiptno = null)
        {            
            var connection = new SqlConnection(DefaultConnection);
            connection.Open();
            Purchase = connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@code=@code,@Id=@Id,@openonly=@openonly", new { apId, Id, code = receiptno, openonly = false });
            return connection;
        }

        private void get0(bool isapprover, out MMDbContext context, ref Device device)
        {            
            Purchase = new PurchaseModel();
            context = new MMDbContext();
            if (!isapprover)
            {
				DeviceModel dev = HttpContext.Current.Session["Device"] as DeviceModel;
				device = context.Devices.AsNoTracking().FirstOrDefault(x => x.dvcUID == dev.dvcUID);
			}			
        }

        public PurchaseEditModel()
        {
        }

        private void GetPurchaseItemList(MMDbContext context, PurchaseModel ps)
        {
            int apId = AccountProfileId;            
            using var connection = new SqlConnection(DefaultConnection);
            connection.Open();

            Purchase.PurchaseItems = connection.Query<PurchaseItemModel>(@"EXEC dbo.GetPurchaseItemListByCode8 @apId=@apId,@pstCode=@pstCode,@piStatus=@piStatus", new { apId = ps.AccountProfileId, ps.pstCode, piStatus = ps.pstStatus }).ToList();

            var itemcodes = Purchase.PurchaseItems.Select(x => x.itmCode).Distinct().ToHashSet();
            var itemcodelist = string.Join(",", itemcodes);

            Helpers.ModelHelper.GetShops(connection, ref Shops, ref ShopNames, apId);
            Helpers.ModelHelper.GetDicItemGroupedVariations(context, connection, itemcodes, ref Purchase.DicItemAttrList, ref Purchase.DicItemVariations, ref Purchase.DicItemGroupedVariations, Shops);

            List<ItemModel> itemoptionlist = connection.Query<ItemModel>(@"EXEC dbo.GetItemOptionsByItemCodes6 @apId=@apId,@itemcodes=@itemcodes", new { apId = ps.AccountProfileId, itemcodes = itemcodelist }).ToList();

            var batvtInfo = context.GetBatchVtInfoByItemCodes12(apId, ps.pstSalesLoc, itemcodelist).ToList();
            var snInfo = context.GetSerialInfo5(apId, ps.pstSalesLoc, itemcodelist, ps.pstCode).ToList();
            var vtInfo = context.GetValidThruInfo12(apId, ps.pstSalesLoc, itemcodelist).ToList();
            var ivInfo = context.GetItemVariInfo4(apId, ps.pstSalesLoc, itemcodelist).ToList();

            Purchase.DicItemOptions = new Dictionary<string, ItemOptions>();
            foreach (var item in Purchase.PurchaseItems)
            {
                item.itmNameDesc = (bool)item.itmUseDesc && !string.IsNullOrEmpty(item.itmDesc) ? item.itmDesc : item.itmName;
                var itemoptions = itemoptionlist.FirstOrDefault(x => x.itmCode == item.itmCode);


                if (item.piStatus.ToLower() != "order" && item.piStatus.ToLower() != "created")
                {
                    if (itemoptions != null)
                    {
                        if (itemoptions.chkBat)
                        {
                            var batchList = batvtInfo.Where(x => x.itmCode == item.itmCode && x.seq == item.piSeq).ToList();
                            foreach (var batch in batchList)
                            {
                                item.batchList.Add(new BatchModel
                                {
                                    batCode = batch.batCode,
                                    batSeq = batch.batSeq,
                                    batValidThru = batch.batValidThru,
                                    batItemCode = batch.itmCode,
                                    batStockInCode = batch.batStockInCode,
                                    batStockInDate = batch.batStockInDate,
                                    batQty = batch.batQty,
                                    batStatus = batch.batStatus,
                                    seq = batch.seq,
                                });
                            }
                        }

                        if (itemoptions.chkSN)
                        {
                            item.SerialNoList = (from sn in context.SerialNoes
                                                 where sn.snoStockInCode == item.pstCode && sn.snoStockInSeq == item.piSeq && sn.snoStockInLoc == item.piStockLoc && sn.AccountProfileId == ComInfo.AccountProfileId
                                                 orderby sn.snoStockInSeq
                                                 select new SerialNoView
                                                 {
                                                     snoUID = sn.snoUID,
                                                     snoIsActive = sn.snoIsActive,
                                                     snoCode = sn.snoCode,
                                                     snSeq = (int)sn.snoSeq,
                                                     snoStatus = sn.snoStatus,
                                                     snoItemCode = sn.snoItemCode,
                                                     snoBatchCode = sn.snoBatchCode,
                                                     snoStockInCode = sn.snoStockInCode,
                                                     snoStockInSeq = sn.snoStockInSeq,
                                                     snoStockInLoc = sn.snoStockInLoc,
                                                     snoValidThru = sn.snoValidThru,
                                                     AccountProfileId = sn.AccountProfileId,
                                                     //seq = sn.seq
                                                 }
                                                        ).ToList();

                            item.snbatseqvtlist = new List<SnBatSeqVt>();
                            foreach (var sn in item.SerialNoList)
                            {
                                item.snbatseqvtlist.Add(new SnBatSeqVt
                                {
                                    sn = sn.snoCode,
                                    batcode = sn.snoBatchCode,
                                    snseq = sn.snSeq,
                                    vt = sn.ValidThruDisplay,
                                    seq = sn.snoStockInSeq ?? 1
                                });
                            }
                        }

                        if (itemoptions.chkVT)
                        {
                            var vtlist = vtInfo.Where(x => x.itmCode == item.itmCode && x.vtSeq == item.piSeq).ToList();
                            foreach (var vt in vtlist)
                            {
                                item.vtList.Add(new ValidThruModel
                                {
                                    vtSeq = vt.vtSeq,
                                    vtValidThru = vt.vtValidThru,
                                    vtItemCode = vt.vtItemCode,
                                    vtStockInCode = vt.vtStockInCode,
                                    vtQty = vt.vtQty,
                                    vtStatus = vt.vtStatus,
                                });
                            }
                        }
                    }

                    if (ivInfo != null && ivInfo.Count > 0)
                    {
                        var ivlist = ivInfo.Where(x => x.itmCode == item.itmCode && x.seq == item.piSeq).ToList();
                        foreach (var iv in ivlist)
                        {
                            item.poItemVariList.Add(new PoItemVariModel
                            {
                                Id = iv.Id,
                                ivComboId = iv.ivComboId,
                                ivStockInCode = iv.ivStockInCode,
                                ivQty = iv.ivQty,
                                ivStatus = iv.ivStatus,
                                seq = (int)iv.seq!,
                                ivIdList = iv.ivIdList,
                                batCode = iv.batCode
                            });
                        }
                    }

                    Purchase.DicItemOptions[item.itmCode] = new ItemOptions
                    {
                        ChkBatch = itemoptions.chkBat,
                        ChkSN = itemoptions.chkSN,
                        WillExpire = itemoptions.chkVT
                    };
                }

            }
        }

       

        public List<PurchaseModel> QuoteList { get; set; }
        public PagedList.IPagedList<PurchaseModel> PagingQuoteList { get; set; }
        public List<PurchaseModel> PSList { get; set; }
        public PagedList.IPagedList<PurchaseModel> PagingPSList { get; set; }
        public string PrintMode { get; set; }

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

        public static PurchaseReturnMsg Edit(PurchaseModel model, List<PurchaseItemModel> PurchaseItems, RecurOrder recurOrder = null)
        {
            string adminemail = "";
            string adminphone = "";
            string status = "";
            string msg = string.Format(Resources.Resource.SavedFormat, Resources.Resource.PurchaseOrder);
            string purchasestatus = string.Empty;
            SessUser user = HttpContext.Current.Session["User"] as SessUser;
            bool ismanager = user.Roles.Contains(RoleType.SalesManager);

            DeviceModel dev = HttpContext.Current.Session["Device"] as DeviceModel;

            using var context = new MMDbContext();
            DateTime dateTime = DateTime.Now;

            var supplier = SupplierEditModel.GetSupplierByCode(comInfo.AccountProfileId, model.supCode);

            DateTime purchasedate = CommonHelper.GetDateFrmString4SQL(model.JsPurchaseDate, DateTimeFormat.YYYYMMDD);
            DateTime promiseddate = CommonHelper.GetDateFrmString4SQL(model.JsPromisedDate, DateTimeFormat.YYYYMMDD);
            //var psttime = purchasedate.Add(dateTime.TimeOfDay);

            if (recurOrder != null && recurOrder.IsRecurring == 1)
            {
                purchasestatus = "RECURRING";
            }
            else
            {
                if (approvalMode)
                    purchasestatus = ismanager ? model.pstStatus : (model.Id == 0 || model.pstStatus.ToLower() == "created") ? "REQUESTING" : model.pstStatus;
                else
                    purchasestatus = model.pstStatus;
            }

            List<string> reviewurls = new();
            List<GetPosAdmin4Notification4_Result> admins = new();
            Dictionary<string, string> DicInvoice = new Dictionary<string, string>();
            Dictionary<string, Dictionary<string, int>> dicItemLocQty = new Dictionary<string, Dictionary<string, int>>();

            string reviewurl = "";

            if (approvalMode)
            {
                if (ismanager)
                {
                    processPurchase(model, PurchaseItems, dev, context, dateTime, purchasedate, promiseddate, user, purchasestatus, ref dicItemLocQty);
                }
                else
                {
                    admins = context.GetPosAdmin4Notification4(comInfo.AccountProfileId, comInfo.Shop).ToList();
                    var admin = admins.FirstOrDefault();
                    reviewurl = UriHelper.GetReviewPurchaseOrderUrl(ConfigurationManager.AppSettings["ReviewPurchaseOrderBaseUrl"], model.pstCode, 0, admin.surUID);
                    var key = string.Concat(admin.UserName, ":", admin.Email, ":", model.pstCode);
                    DicInvoice[key] = reviewurl;

                    reviewurls.Add(reviewurl);

                    if (model.Id == 0)
                    {
                        model.Id = addPurchaseOrder(model, PurchaseItems, dev, context, dateTime, purchasedate, promiseddate, user, purchasestatus, ref dicItemLocQty);
                    }
                    else
                    {
                        processPurchase(model, PurchaseItems, dev, context, dateTime, purchasedate, promiseddate, user, purchasestatus, ref dicItemLocQty);
                    }

                    status = "purchaseordersaved";
                    if (model.pstStatus.ToLower() != "opened" && model.ireviewmode == 0)
                    {
                        HandlingPurchaseOrderReview(model.pstCode, context);
                        string adminname = "";

                        if (recurOrder == null || recurOrder.IsRecurring == 0)
                        {
                            msg = user.Roles.Contains(RoleType.SalesManager) ? Resources.Resource.OrderSavedSuccessfully : string.Format(Resources.Resource.NotificationEmailWillBeSentToFormat, adminname);
                            if (!user.Roles.Contains(RoleType.SalesManager))
                            {
                                adminname = admin.UserName;
                                adminphone = admin.Phone;
                                adminemail = admin.Email;

                                #region Send Notification Email   
                                if ((bool)comInfo.enableEmailNotification)
                                {
                                    if (Helpers.ModelHelper.SendNotificationEmail(DicInvoice, model.ireviewmode, false))
                                    {
                                        var purchase = context.Purchases.FirstOrDefault(x => x.Id == model.Id);
                                        purchase.pstSendNotification = true;
                                        context.SaveChanges();
                                    }
                                }
                                #endregion
                            }
                        }
                        else
                        {
                            status = "recurordersaved";
                            msg = string.Format(Resources.Resource.SavedFormat, Resources.Resource.RecurringOrder);
                        }
                    }
                }
            }
            else
            {
                processPurchase(model, PurchaseItems, dev, context, dateTime, purchasedate, promiseddate, user, purchasestatus, ref dicItemLocQty);
            }

            // update stock qty:
            if (purchasestatus == "opened")
                UpdateStockQty(model, PurchaseItems, context, user, dateTime, dicItemLocQty);

            var returnmsg = new PurchaseReturnMsg
            {
                msg = msg,
                status = status,
                adminphone = adminphone,
                purchasecode = model.pstCode,
                reviewurl = reviewurl,
                supname = supplier.supName,
                purchaselnlength = PurchaseItems.Count,
                adminemail = adminemail,
                ismanager = ismanager
            };
            return returnmsg;
        }

        private static void processPurchase(PurchaseModel model, List<PurchaseItemModel> PurchaseItems, DeviceModel dev, MMDbContext context, DateTime dateTime, DateTime purchasedate, DateTime promiseddate, SessUser user, string purchasestatus, ref Dictionary<string, Dictionary<string, int>> dicItemLocQty)
        {
            if (model.Id == 0)
            {
                model.Id = addPurchaseOrder(model, PurchaseItems, dev, context, dateTime, purchasedate, promiseddate, user, purchasestatus, ref dicItemLocQty);
            }
            else
            {
                MMDAL.Purchase ps = context.Purchases.FirstOrDefault(x => x.Id == model.Id);
                ps.supCode = model.supCode;
                ps.pstRemark = model.pstRemark;
                ps.pstSalesLoc = model.pstSalesLoc;
                ps.pstPurchaseDate = purchasedate;
                ps.pstPromisedDate = promiseddate;
                ps.pstStatus = purchasestatus;
                ps.pstCurrency = model.pstCurrency;
                ps.pstExRate = model.pstExRate;
                ps.ModifyTime = dateTime;
                ps.pstSupplierInvoice = model.pstSupplierInvoice;
                ps.pstAllLoc = model.pstAllLoc;
                context.SaveChanges();

                if (purchasestatus == "opened")
                {
                    #region add pi records:
                    bool ispartial = AddPurchaseItems(model, PurchaseItems, context, ps, ref dicItemLocQty);
                    #endregion

                    #region remove original pi(status==order) records:
                    var oripilns = context.PurchaseItems.Where(x => x.pstCode == model.pstCode && x.AccountProfileId == comInfo.AccountProfileId && (x.piStatus.ToLower() == "order" || x.piStatus.ToLower() == "created") && !(bool)x.IsPartial);
                    if (oripilns.Any())
                    {
                        context.PurchaseItems.RemoveRange(oripilns);
                        context.SaveChanges();
                    }
                    #endregion

                    #region Update Purchase Status
                    // update wholesales status:                
                    ps.pstStatus = ispartial ? PurchaseStatus.partialreceival.ToString() : PurchaseStatus.opened.ToString();
                    ps.pstIsPartial = ispartial;
                    ps.ModifyTime = dateTime;
                    context.SaveChanges();
                    #endregion
                }
                else
                {
                    UpdatePurchaseItems(model, PurchaseItems, context);
                }
            }
        }

        private static void UpdatePurchaseItems(PurchaseModel model, List<PurchaseItemModel> PurchaseItems, MMDbContext context)
        {
            DateTime dateTime = DateTime.Now;

            var psitems = context.PurchaseItems.Where(x => x.pstCode == model.pstCode && (x.piStatus.ToLower() == "order" || x.piStatus.ToLower() == "created"));

            var itemcodes = PurchaseItems.Select(x => x.itmCode).Distinct().ToList();
            var itemoptions = context.GetItemOptionsByItemCodes6(comInfo.AccountProfileId, string.Join(",", itemcodes)).ToList();

            List<PurchaseItem> psiList = new List<PurchaseItem>();

            foreach (var item in PurchaseItems)
            {
                var psitem = psitems.FirstOrDefault(x => x.piSeq == item.piSeq);
                if (psitem != null)
                {
                    psitem.itmCode = item.itmCode;
                    psitem.piBaseUnit = item.piBaseUnit;
                    psitem.piQty = item.piQty;
                    psitem.piUnitPrice = item.piUnitPrice;
                    psitem.piDiscPc = item.piDiscPc;
                    psitem.piTaxPc = item.piTaxPc;
                    psitem.piTaxAmt = item.piTaxAmt;
                    psitem.piAmt = item.piAmt;
                    psitem.piAmtPlusTax = item.piAmtPlusTax;
                    psitem.piStockLoc = item.piStockLoc;
                    psitem.JobID = item.JobID;
                    psitem.ModifyTime = dateTime;
                }
                else
                {
                    DateTime? validthru;
                    GetItemOptionsByItemCodes6_Result itemoption;
                    addPurchaseItem(model, dateTime, ref psiList, itemoptions, item, out validthru, out itemoption);
                }
            }

            if (psiList.Count > 0)
                context.PurchaseItems.AddRange(psiList);

            context.SaveChanges();
        }

        private static long addPurchaseOrder(PurchaseModel model, List<PurchaseItemModel> PurchaseItems, DeviceModel dev, MMDbContext context, DateTime dateTime, DateTime purchasedate, DateTime promiseddate, SessUser user, string purchasestatus, ref Dictionary<string, Dictionary<string, int>> dicItemLocQty)
        {
            var pstTime = purchasedate.Add(dateTime.TimeOfDay);
            MMDAL.Purchase ps = new()
            {
                pstAllLoc = model.pstAllLoc,
                pstSalesLoc = model.pstSalesLoc,
                pstCode = model.pstCode,
                pstType = "PS",
                supCode = model.supCode,
                pstPurchaseDate = purchasedate,
                pstPurchaseTime = pstTime,
                pstPromisedDate = promiseddate,
                pstStatus = purchasestatus,
                pstCurrency = model.pstCurrency,
                pstSupplierInvoice = model.pstSupplierInvoice,
                pstRemark = model.pstRemark,
                pstExRate = model.pstExRate,
                AccountProfileId = comInfo.AccountProfileId,                
                CreateTime = dateTime,
                ModifyTime = dateTime,
                CreateBy = user.UserName,
                pstCheckout = false,
                pstIsPartial = false,
            };

            ps = context.Purchases.Add(ps);
            context.SaveChanges();
            Device device = context.Devices.Find(dev.dvcUID);
            device.dvcNextPurchaseNo++;
            device.dvcModifyTime = dateTime;
            context.SaveChanges();
            AddPurchaseItems(model, PurchaseItems, context, ps, ref dicItemLocQty);

            return ps.Id;
        }

        private static void HandlingPurchaseOrderReview(string purchasecode, MMDbContext context)
        {
            DateTime dateTime = DateTime.Now;
            var review = context.PurchaseOrderReviews.FirstOrDefault(x => x.PurchaseOrder == purchasecode);
            if (review == null)
            {
                review = new PurchaseOrderReview
                {
                    PurchaseOrder = purchasecode,
                    CreateTime = dateTime,
                    ModifyTime = dateTime
                };
                context.PurchaseOrderReviews.Add(review);
                context.SaveChanges();
            }
        }

        private static void UpdateStockQty(PurchaseModel model, List<PurchaseItemModel> PurchaseItems, MMDbContext context, SessUser sessUser, DateTime dateTime, Dictionary<string, Dictionary<string, int>> dicItemLocQty)
        {
            if (model.pstStatus == PurchaseStatus.opened.ToString())
            {
                List<MyobLocStock> newstocks = new List<MyobLocStock>();
                var itemCodesIds = context.GetItemIdsByCodes1(comInfo.AccountProfileId, !NonAbss, string.Join(",", PurchaseItems.Select(x => x.itmCode).ToList())).ToList();

                foreach (var itemcode in dicItemLocQty.Keys)
                {
                    foreach (var location in dicItemLocQty[itemcode].Keys)
                    {
                        MyobLocStock stock = context.MyobLocStocks.FirstOrDefault(x => x.lstItemCode == itemcode && x.lstStockLoc == location && x.AccountProfileId == comInfo.AccountProfileId);
                        if (stock != null)
                        {
                            stock.lstQuantityAvailable += dicItemLocQty[itemcode][location];
                            stock.lstModifyBy = sessUser.UserName;
                            stock.lstModifyTime = dateTime;
                        }
                        else
                        {
                            var itemcodeid = itemCodesIds.FirstOrDefault(x => x.itmCode == itemcode);
                            var itemId = itemcodeid == null ? 0 : itemcodeid.itmItemID;
                            var Id = CommonHelper.GenerateNonce(50);
							stock = new MyobLocStock
							{
                                Id = Id,
								lstItemLocationID = stock.lstItemLocationID,
								lstItemCode = itemcode,
								lstItemID = itemId,
								lstStockLoc = model.pstSalesLoc,
								lstQuantityAvailable = dicItemLocQty[itemcode][location],
								AccountProfileId = comInfo.AccountProfileId,
								lstCreateBy = sessUser.UserName,
								lstCreateTime = dateTime,
								lstModifyTime = dateTime
							};
							newstocks.Add(stock);
                        }
                    }
                }

                if (newstocks.Count > 0)
                {
                    context.MyobLocStocks.AddRange(newstocks);
                    context.SaveChanges();
                }
                context.SaveChanges();
            }
        }

        private static bool AddPurchaseItems(PurchaseModel model, List<PurchaseItemModel> PurchaseItems, MMDbContext context, MMDAL.Purchase ps, ref Dictionary<string, Dictionary<string, int>> dicItemLocQty)
        {
            StringBuilder sb = new StringBuilder();
            DateTime dateTime = DateTime.Now;
            List<PurchaseItem> psilist = new();
            List<PurchaseItem> pendinglist = new();

            List<Batch> batchlist = new List<Batch>();
            List<SerialNo> serialNoList = new List<SerialNo>();
			List<MMDAL.ValidThru> vtlist = new List<MMDAL.ValidThru>();
			List<PoItemVariation> ivlist = new List<PoItemVariation>();

            var itemcodes = PurchaseItems.Select(x => x.itmCode).Distinct().ToList();
            var itemoptions = context.GetItemOptionsByItemCodes6(comInfo.AccountProfileId, string.Join(",", itemcodes)).ToList();
            foreach (var itemcode in itemcodes)
            {
                dicItemLocQty[itemcode] = new Dictionary<string, int>();
            }           

            foreach (var item in PurchaseItems)
            {
                DateTime? validthru;
                GetItemOptionsByItemCodes6_Result itemoption;
                addPurchaseItem(ps, dateTime, ref psilist, itemoptions, item, out validthru, out itemoption);

                if (ps.pstStatus == PurchaseStatus.opened.ToString())
                {
                    if (item.batchList != null && item.batchList.Count > 0)
                    {
                        foreach (var bi in item.batchList)
                        {
                            DateTime? bivalidthru = string.IsNullOrEmpty(bi.validthru) ? null : CommonHelper.GetDateFrmString4SQL(bi.validthru, DateTimeFormat.YYYYMMDD);
                            int batqty = (itemoption != null && itemoption.chkSN && itemoption.chkSN) ? item.snbatseqvtlist.Count : (int)bi.batQty;
                            batchlist.Add(
                                new Batch
                                {
                                    batCode = bi.batCode,
                                    batSeq = bi.batSeq,
                                    seq = bi.seq,
                                    batQty = batqty,
                                    batValidThru = bivalidthru,
                                    batItemCode = bi.batItemCode,
                                    batStockInCode = ps.pstCode,
                                    batStockInDate = ps.pstPurchaseDate,
                                    batStatus = "READY",
                                    AccountProfileId = ps.AccountProfileId,
                                    CreateTime = dateTime,
                                    ModifyTime = dateTime
                                }
                            );
                        }
                    }

                    if (item.snbatseqvtlist != null && item.snbatseqvtlist.Count > 0)
                    {
                        foreach (var snv in item.snbatseqvtlist)
                        {
                            DateTime? snvalidthru = string.IsNullOrEmpty(snv.vt) ? null : CommonHelper.GetDateFrmString4SQL(snv.vt, DateTimeFormat.YYYYMMDD);
                            serialNoList.Add(new SerialNo
                            {
                                snoIsActive = true,
                                snoCode = snv.sn,
                                snoSeq = snv.seq,
                                snoValidThru = snvalidthru,
                                snoStatus = "REDEEM READY",
                                snoItemCode = item.itmCode,
                                snoStockInCode = ps.pstCode,
                                snoStockInSeq = (byte?)item.piSeq,
                                snoStockInLoc = model.pstSalesLoc,
                                snoStockInDate = ps.pstPurchaseDate,
                                snoBatchCode = snv.batcode,
                                seq = snv.seq,
                                AccountProfileId = comInfo.AccountProfileId,
                                CreateTime = DateTime.Now,
                                ModifyTime = DateTime.Now
                            });
                        }
                    }

                    if (itemoption != null && !itemoption.chkBat && !itemoption.chkSN && itemoption.chkVT)
                    {
                        vtlist.Add(new MMDAL.ValidThru
                        {
                            vtSeq = item.piSeq,
                            seq = item.piSeq,
                            vtValidThru = validthru,
                            vtItemCode = item.itmCode,
                            vtStockInCode = ps.pstCode,
                            vtQty = item.piReceivedQty,
                            vtStatus = "READY",
                            AccountProfileId = ps.AccountProfileId,
                            CreateTime = dateTime,
                            ModifyTime = dateTime
                        });
                    }

                    if (dicItemLocQty.ContainsKey(item.itmCode) && dicItemLocQty[item.itmCode].ContainsKey(item.piStockLoc))
                    {
                        dicItemLocQty[item.itmCode][item.piStockLoc] += (int)item.piReceivedQty;
                    }
                    else
                    {
                        dicItemLocQty[item.itmCode][item.piStockLoc] = (int)item.piReceivedQty;
                    }

                    SetPendingList(ps, dateTime, ref pendinglist, item, itemoptions);

                    #region ItemVariations
                    if (item.poItemVariList != null && item.poItemVariList.Count > 0)
                    {
                        int ivIdLength = int.Parse(ConfigurationManager.AppSettings["IvIdLength"]);
                        foreach (var iv in item.poItemVariList)
                        {
                            var Id = CommonHelper.GenerateNonce(ivIdLength, false);
                            var ivIdList = string.Join(",", iv.JsIvIdList);
                            ivlist.Add(new PoItemVariation
                            {
                                Id = Id,
                                ivComboId = iv.ivComboId,
                                ivStockInCode = ps.pstCode,
                                ivQty = iv.ivQty,
                                seq = iv.seq,
                                itmCode = iv.itmCode,
                                batCode = iv.batCode,
                                ivIdList = ivIdList,
                                AccountProfileId = ps.AccountProfileId,
                                CreateTime = dateTime
                            });
                        }
                    }
                    #endregion
                }
            }

            if (batchlist.Count > 0)
            {
                context.Batches.AddRange(batchlist);
                context.SaveChanges();
            }
            if (serialNoList.Count > 0)
            {
                context.SerialNoes.AddRange(serialNoList);
                context.SaveChanges();
            }
            if (vtlist.Count > 0)
            {
                context.ValidThrus.AddRange(vtlist);
                context.SaveChanges();
            }
            if (psilist.Count > 0)
            {
                context.PurchaseItems.AddRange(psilist);
                context.SaveChanges();
            }
            if (ivlist.Count > 0)
            {
                context.PoItemVariations.AddRange(ivlist);
                context.SaveChanges();
            }

            HandlePendingList(model, context, ps, dateTime, pendinglist, psilist);

            context.SaveChanges();

            return pendinglist.Count > 0;
        }

        private static void addPurchaseItem(MMDAL.Purchase ps, DateTime dateTime, ref List<PurchaseItem> psilist, List<GetItemOptionsByItemCodes6_Result> itemoptions, PurchaseItemModel item, out DateTime? validthru, out GetItemOptionsByItemCodes6_Result itemoption)
        {
            validthru = string.IsNullOrEmpty(item.JsValidThru) ? null : CommonHelper.GetDateFrmString4SQL(item.JsValidThru, DateTimeFormat.YYYYMMDD);
            itemoption = itemoptions.FirstOrDefault(x => x.itmCode == item.itmCode);
            psilist.Add(new PurchaseItem
            {
                pstCode = ps.pstCode,
                pstId = ps.Id,
                itmCode = item.itmCode,
                piSeq = item.piSeq,
                piBaseUnit = item.piBaseUnit,
                piQty = item.piQty,
                piHasSN = itemoption == null ? false : itemoption.chkSN,
                piValidThru = validthru,
                piUnitPrice = item.piUnitPrice,
                piDiscPc = item.piDiscPc,
                piTaxPc = item.piTaxPc,
                piTaxAmt = item.piTaxAmt,
                piAmt = item.piAmt,
                piAmtPlusTax = item.piAmtPlusTax,
                piReceivedQty = item.piReceivedQty,
                piStatus = ps.pstStatus.ToLower(),
                ivBatCode = item.ivBatCode,
                AccountProfileId = comInfo.AccountProfileId,
                CreateTime = dateTime,
                ModifyTime = dateTime,
                IsPartial = false,
                piStockLoc = item.piStockLoc,
                JobID = item.JobID,
            });
        }

        private static void HandlePendingList(PurchaseModel model, MMDbContext context, MMDAL.Purchase ps, DateTime dateTime, List<PurchaseItem> pendinglist, List<PurchaseItem> psilist)
        {
            if (pendinglist.Count > 0)
            {
                DateTime purchasedate = ps.pstPurchaseDate;
                DateTime promiseddate = ps.pstPromisedDate;
                var pstTime = purchasedate.Add(dateTime.TimeOfDay);
                var _ps = new MMDAL.Purchase
                {
                    pstCode = model.pstCode,
                    supCode = model.supCode,
                    pstRemark = model.pstRemark,
                    pstSalesLoc = model.pstSalesLoc,
                    pstPurchaseDate = purchasedate,
                    pstPromisedDate = promiseddate,
                    pstPurchaseTime = pstTime,
                    pstStatus = PurchaseStatus.order.ToString(),
                    pstCurrency = model.pstCurrency,
                    pstExRate = model.pstExRate,
                    pstSupplierInvoice = ps.pstSupplierInvoice,
                    pstIsPartial = true,
                    pstCheckout = false,
                    CreateBy = ps.CreateBy,
                    AccountProfileId = comInfo.AccountProfileId,
                    CreateTime = dateTime,
                    ModifyTime = dateTime,
                    pstType = "PS",
                    pstAllLoc = ps.pstAllLoc,
                };
                context.Purchases.Add(_ps);
                context.PurchaseItems.AddRange(pendinglist);
                ps.pstIsPartial = true;

                foreach (var item in psilist)
                {
                    item.piStatus = PurchaseStatus.partialreceival.ToString();
                    item.IsPartial = true;
                    item.ModifyTime = DateTime.Now;
                }

                context.SaveChanges();
            }
        }

        private static void SetPendingList(MMDAL.Purchase ps, DateTime dateTime, ref List<PurchaseItem> pendinglist, PurchaseItemModel item, List<GetItemOptionsByItemCodes6_Result> itemoptions)
        {
            int pendingqty = item.piReceivedQty != -1 ? item.piQty - (int)item.piReceivedQty : 0;
            decimal? amt = item.piUnitPrice * pendingqty * (1 - (item.piDiscPc / 100));
            decimal? taxamt = amt * (item.piTaxPc / 100);
            decimal? amtplustax = amt + taxamt;
            if (pendingqty > 0)
            {
                var itemoption = itemoptions.FirstOrDefault(x => x.itmCode == item.itmCode);

                pendinglist.Add(new PurchaseItem
                {
                    AccountProfileId = ps.AccountProfileId,
                    pstCode = ps.pstCode,
                    pstId = ps.Id,
                    piSeq = item.piSeq,
                    piStatus = PurchaseStatus.order.ToString(),
                    itmCode = item.itmCode,
                    piBaseUnit = item.piBaseUnit,
                    piQty = pendingqty,
                    ivBatCode = null,
                    piHasSN = itemoption == null ? false : itemoption.chkSN,
                    piValidThru = null,
                    piUnitPrice = item.piUnitPrice,
                    piDiscPc = item.piDiscPc,
                    piTaxPc = item.piTaxPc,
                    piTaxAmt = taxamt,
                    piAmt = (decimal)amt,
                    piAmtPlusTax = amtplustax,
                    piReceivedQty = -1,
                    CreateTime = dateTime,
                    ModifyTime = dateTime,
                    IsPartial = true,
                    piStockLoc = item.piStockLoc,
                    JobID = item.JobID,
                });
            }
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

        public static List<string> GetUploadPurchaseSqlList(int accountprofileId, ref DataTransferModel dmodel, string strfrmdate, string strtodate, ComInfo comInfo, MMDbContext context, DateTime? dateFrm=null, DateTime? dateTo=null, string connectionString = null)
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
	}

    public enum PurchaseStatus
    {
        all = -1,
        order = 0,
        quote = 1,
        receiveitems = 2,
        opened = 3,
        closed = 4,
        returned = 5,
        REQUESTING = 8,
        PASSED = 9,
        APPROVED = 10,
        rejected = 14,
        created = 11,
        VOIDED = 12,
        partialreceival = 13,
    }

    public class PurchaseReturnMsg
    {
        public string msg { get; set; }
        public string purchasecode { get; set; }
        public string reviewurl { get; set; }
        public string adminphone { get; set; }
        public string status { get; set; }
        public string supname { get; set; }
        public int purchaselnlength { get; set; }
        public string adminemail { get; set; }
        public bool ismanager { get; set; }
    }
}
