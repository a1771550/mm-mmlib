using CommonLib.Helpers;
using Dapper;
using PagedList;
using MMDAL;
using MMLib.Models.Supplier;
using MMLib.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MMLib.Helpers;
using Microsoft.Data.SqlClient;

namespace MMLib.Models.Invoice
{
    public class InvoiceEditModel:PagingBaseModel
    {
        public SupInvoiceModel Invoice { get; set; }

        public static void SaveInvoice(SupInvoiceModel invoice)
        {
            using var context = new MMDbContext();
            SupplierInvoice currentInvoice = context.SupplierInvoices.FirstOrDefault(x => x.AccountProfileId == apId && x.Id == invoice.Id);

            if (currentInvoice != null)
            {
                currentInvoice.supCode = invoice.supCode;
                currentInvoice.siDesc = invoice.siDesc;
                currentInvoice.siAmt = invoice.siAmt;
                currentInvoice.AccountID = invoice.AccountID;
                currentInvoice.JobID = invoice.JobID;
                currentInvoice.pstCode = invoice.pstCode;
                currentInvoice.supCode = invoice.supCode;
                currentInvoice.siChequeNo = invoice.siChequeNo;
                currentInvoice.Remark = invoice.Remark;
                currentInvoice.ModifyBy = user.UserCode;
                currentInvoice.ModifyTime = DateTime.Now;
            }
            else
            {
                context.SupplierInvoices.Add(new SupplierInvoice
                {
                    Id = invoice.Id,
                    siDesc = invoice.siDesc,
                    siAmt = invoice.siAmt,
                    pstCode = invoice.pstCode,
                    supCode = invoice.supCode,
                    AccountID = invoice.AccountID,
                    JobID = invoice.JobID,
                    siChequeNo = invoice.siChequeNo,
                    Remark = invoice.Remark,
                    AccountProfileId = apId,
                    CreateBy = user.UserCode,
                    CreateTime = DateTime.Now,
                });
            }

            context.SaveChanges();
        }

        public static void AddInvoice(string pstCode, string invoiceId, MMDbContext context)
        {
            context.SupplierInvoices.Add(new SupplierInvoice
            {
                Id = invoiceId,
                pstCode = pstCode,
                AccountProfileId = apId,
                CreateBy = user.UserCode,
                CreateTime = DateTime.Now,
            });
            context.SaveChanges();
        }

        public void GetSupplierInvoiceList(string strfrmdate, string strtodate, int PageNo, int SortCol, string SortOrder, string Keyword, int filter, string searchmode)
        {
            IsUserRole IsUserRole = UserEditModel.GetIsUserRole(user);

            CommonHelper.DateRangeHandling(strfrmdate, strtodate, out DateTime frmdate, out DateTime todate, DateFormat.YYYYMMDD, '-');
            using var context = new MMDbContext();
            if (Keyword == "") Keyword = null;
            DateFromTxt = strfrmdate;
            DateToTxt = strtodate;
            this.SortCol = SortCol;
            this.Keyword = Keyword;
            this.PageNo = PageNo;
            CurrentSortOrder = SortOrder;
            //if(filter==0)
            this.SortOrder = SortOrder == "desc" ? "asc" : "desc";

            if (filter == 1) SortOrder = SortOrder == "desc" ? "asc" : "desc";

            string userCode = UserHelper.CheckIfApprover(User) ? null : user.UserCode;

            if (SqlConnection.State == System.Data.ConnectionState.Closed) SqlConnection.Open();

            string baseUrl = UriHelper.GetBaseUrl();

            var objects = new { apId, frmdate, todate, sortName = SortName, sortOrder = SortOrder, userCode, baseUrl, Keyword };
            //var spname = IsUserRole.isapprover ? "GetProcurement4Approval" : "GetProcurements";

            InvoiceList = SqlConnection.Query<SupInvoiceModel>(@"EXEC dbo.GetSupplierInvoicesByCode @apId=@apId,@frmdate=@frmdate,@todate=@todate,@sortName=@sortName,@sortOrder=@sortOrder,@userCode=@userCode,@baseUrl=@baseUrl,@Keyword=@Keyword", objects).ToList();

            PagingInvoiceList = InvoiceList.ToPagedList(PageNo, PageSize);
        }

        public void GetInvoice(string pstCode, long Id = 0)
        {
            bool isapprover = (bool)HttpContext.Current.Session["IsApprover"];
            using var context = new MMDbContext();

            Invoice = new SupInvoiceModel();
            Device device = null;
            if (!isapprover) device = context.Devices.First();

            var connection = new SqlConnection(defaultConnection);
            connection.Open();

            DateTime dateTime = DateTime.Now;

            if (Id > 0)
            {
                Invoice = connection.QueryFirstOrDefault<SupInvoiceModel>(@"EXEC dbo.GetInvoiceById @apId=@apId,@Id=@Id", new { apId, Id });
              
                var suppurchaseInfoes = connection.Query<SupInvoiceModel>(@"EXEC dbo.GetSuppliersInfoesByCode @apId=@apId,@pstCode=@pstCode", new { apId, Invoice.pstCode }).ToList();

                List<IGrouping<string, SupInvoiceModel>> groupedsupInvoiceInfoes = new List<IGrouping<string, SupInvoiceModel>>();
                if (suppurchaseInfoes != null && suppurchaseInfoes.Count > 0) groupedsupInvoiceInfoes = suppurchaseInfoes.GroupBy(x => x.supCode).ToList();           

                if (Invoice.pstStatus.ToLower() == InvoiceStatus.order.ToString())
                {
                    SelectedSupInvoice = InvoiceList.Single(x => x.Selected);

                    string baseUrl = UriHelper.GetBaseUrl();
                    InvoiceList = connection.Query<SupInvoiceModel>(@"EXEC dbo.GetSupplierInvoicesByCode @apId=@apId,@pstCode=@pstCode,@baseUrl=@baseUrl", new { apId, Invoice.pstCode, baseUrl }).ToList();

                    if (InvoiceList.Count == 0)
                    {
                        LastSupplierInvoiceId = GenInvoiceId(Invoice.pstCode, 0);
                    }
                    else
                    {
                        int? numId = CommonHelper.GetNumberFrmString(Invoice.InvoiceList.OrderByDescending(x => x.Id).FirstOrDefault().Id.Split('-')[1]);
                        LastSupplierInvoiceId = GenInvoiceId(Invoice.pstCode, (int)numId);
                    }

                    HashSet<string> SupInvoiceIds = new HashSet<string>();

                    if (Invoice.InvoiceList.Count > 0)
                    {
                        SupInvoiceIds = Invoice.InvoiceList.Select(x => x.Id).Distinct().ToHashSet();
                        foreach (var id in SupInvoiceIds) if (!DicSupInvoicePayments.ContainsKey(id)) DicSupInvoicePayments[id] = [];

                        InvoicePayments = connection.Query<SupInvoicePaymentModel>(@"EXEC dbo.GetSupplierPaymentsByIds @apId=@apId,@InvoiceIds=@InvoiceIds", new { apId, InvoiceIds = string.Join(",", SupInvoiceIds) }).ToList();
                        //foreach (var invoice in Invoice.InvoiceList) invoice.Payments = InvoicePayments.Where(x => x.InvoiceId == invoice.Id).ToList();

                        foreach (var payment in InvoicePayments)
                        {
                            if (DicSupInvoicePayments.ContainsKey(payment.InvoiceId))
                            {
                                DicSupInvoicePayments[payment.InvoiceId].Add(new SupInvoicePaymentModel
                                {
                                    payId = payment.Id,
                                    InvoiceId = payment.InvoiceId,
                                    seq = payment.seq,
                                    sipAmt = payment.sipAmt,
                                    sipChequeNo = payment.sipChequeNo,
                                    Remark = payment.Remark,
                                    CreateTime = payment.CreateTime,
                                    CreateBy = payment.CreateBy,
                                });
                            }
                        }
                    }

                    groupedsupInvoiceInfoes = suppurchaseInfoes.Where(x => x.supCode == SelectedSupInvoice.supCode).GroupBy(x => x.supCode).ToList();

                }

                getDicSupInfoes(groupedsupInvoiceInfoes);
            }
            else
            {
                var purchaseinitcode = device.dvcInvoiceRequestPrefix;
                var purchaseno = $"{device.dvcNextInvoiceRequestNo:000000}";
                device.dvcNextInvoiceRequestNo++;
                device.dvcModifyTime = dateTime;
                string pqstatus = RequestStatus.requestingByStaff.ToString();
                string pstcode = string.Concat(purchaseinitcode, purchaseno);

                status = InvoiceStatus.requesting.ToString();
                var latestpurchase = context.Invoices.OrderByDescending(x => x.Id).FirstOrDefault();
                if (latestpurchase != null)
                {
                    if (string.IsNullOrEmpty(latestpurchase.pstType)) PopulateInvoiceModel(latestpurchase.pstCode, latestpurchase.pstStatus, RequestStatus.requestingByStaff.ToString(), latestpurchase.Id);
                    else addNewInvoice(context, apId, pstcode, status, latestpurchase.IsThreshold, pqstatus);
                }
                else addNewInvoice(context, apId, pstcode, status, false, pqstatus);
            }

            Suppliers = connection.Query<SupInvoiceModel>(@"EXEC dbo.GetInvoiceList6 @apId=@apId", new { apId }).ToList();

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

            Invoice.pstInvoiceDate = DateTime.Now.Date;
            Invoice.TaxModel = ModelHelper.GetTaxInfo(context);
            Invoice.UseForexAPI = ExchangeRateEditModel.GetForexInfo(context);

            DicLocation = new Dictionary<string, string>();
            foreach (var shop in stocklocationlist)
            {
                DicLocation[shop] = shop;
            }

            JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();

            DicAcAccounts = ModelHelper.GetDicAcAccounts(connection);

            Invoice.Mode = ireadonly == 1 ? "readonly" : "";

            PoSettings = PoSettingsEditModel.GetPoSettings(connection);

            populateFundingSources();
            populateNotEnoughQuoReasons();
            populateReasons4ChoosingSupplier();

            void populateFundingSources()
            {
                FundingSources = [];
                var sources = ConfigurationManager.AppSettings["FundingSources"].Split('|').ToList();
                int idx = 0;
                foreach (var source in sources)
                {
                    idx++;
                    FundingSources.Add(new SelectListItem
                    {
                        Value = $"fs{idx}",
                        Text = source.ToString()
                    });
                }
            }

            void populateNotEnoughQuoReasons()
            {
                NotEnoughQuoReasons = [];
                var reasons = ConfigurationManager.AppSettings["NotEnoughQuoReasons"].Split('|').ToList();
                int idx = 0;
                foreach (var reason in reasons)
                {
                    idx++;
                    NotEnoughQuoReasons.Add(new SelectListItem
                    {
                        Value = $"nrq{idx}",
                        Text = reason.ToString()
                    });
                }
            }

            void populateReasons4ChoosingSupplier()
            {
                Reasons4ChoosingSupplier = [];
                var reasons = ConfigurationManager.AppSettings["Reasons4ChoosingSupplier"].Split('|').ToList();
                int idx = 0;
                foreach (var reason in reasons)
                {
                    idx++;
                    Reasons4ChoosingSupplier.Add(new SelectListItem
                    {
                        Value = $"rcs{idx}",
                        Text = reason.ToString()
                    });
                }
            }

            void getDicSupInfoes(List<IGrouping<string, SupInvoiceModel>> groupedsupInvoiceInfoes)
            {
                foreach (var group in groupedsupInvoiceInfoes)
                {
                    var g = group.FirstOrDefault();
                    if (!DicSupInfoes.ContainsKey(g.supCode)) DicSupInfoes[g.supCode] = new List<SupInvoiceModel>();
                }
                foreach (var group in groupedsupInvoiceInfoes)
                {
                    var g = group.FirstOrDefault();
                    if (DicSupInfoes.ContainsKey(g.supCode))
                    {
                        foreach (var spi in group)
                        {
                            spi.filePath = ModelHelper.GetPDFLnk(Invoice.pstCode, g.supCode, spi.fileName);
                            DicSupInfoes[g.supCode].Add(spi);
                        }
                    }
                }
            }
        }
    }

}
