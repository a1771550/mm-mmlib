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
using MMLib.Models.MYOB;
using System.Text.Json;
using ModelHelper = MMLib.Helpers.ModelHelper;
using MMLib.Models.Purchase;
using System.Configuration;
using System.IO;

namespace MMLib.Models.Invoice
{
    public class InvoiceEditModel : PagingBaseModel
    {
        public Dictionary<string, List<InvoiceLineInfoModel>> DicInvLnFileList { get; set; } = new Dictionary<string, List<InvoiceLineInfoModel>>();
        public Dictionary<string, List<InvoicePaymentModel>> DicInvoicePayments { get; set; } = new Dictionary<string, List<InvoicePaymentModel>>();
        public IsUserRole IsUserRole { get { return UserEditModel.GetIsUserRole(user); } }
        public bool IsMD { get { return IsUserRole.ismuseumdirector; } }
        public bool IsDB { get { return IsUserRole.isdirectorboard; } }

        public string NextInvoiceId { get; set; }
        public string LastInvoiceId { get; set; }
        public PoSettings PoSettings { get; set; } = new PoSettings();

        public string RemoveFileIcon { get { return $"<i class='mx-2 fa-solid fa-trash removefile' data-id='{{2}}'></i>"; } }
        public string PDFThumbnailPath { get { return $"<img src='{UriHelper.GetBaseUrl()}/Images/pdf.jpg' class='thumbnail'/>"; } }


        public List<MyobJobModel> JobList { get; set; }
        public string JsonJobList { get { return JobList == null ? "" : JsonSerializer.Serialize(JobList); } }
        public Dictionary<string, List<AccountModel>> DicAcAccounts { get; set; }
        //public List<InvoicePaymentModel> InvoicePayments { get; set; }

        public bool Readonly { get; set; }
        public List<InvoiceModel> InvoiceList { get; set; }
        public IPagedList<InvoiceModel> PagingInvoiceList { get; set; }
        public InvoiceModel Invoice { get; set; }
        public PurchaseModel Purchase { get; set; }
        public InvoiceEditModel()
        {
            JobList = new List<MyobJobModel>();
            ImgList = new List<string>();
            FileList = new List<string>();
            PoQtyAmtList = [];
        }
        public static void SaveInvoice(InvoiceModel Invoice)
        {
            using var context = new MMDbContext();
            MMDAL.Invoice currentInvoice = context.Invoices.FirstOrDefault(x => x.AccountProfileId == apId && x.Id == Invoice.Id);

            if (currentInvoice != null)
            {
                currentInvoice.siDesc = Invoice.siDesc;
                currentInvoice.siAmt = Invoice.siAmt;
                currentInvoice.AccountID = Invoice.AccountID;
                currentInvoice.JobID = Invoice.JobID;
                currentInvoice.Remark = Invoice.Remark;
                currentInvoice.ModifyBy = user.UserCode;
                currentInvoice.ModifyTime = DateTime.Now;
            }
            else
            {
                context.Invoices.Add(new MMDAL.Invoice
                {
                    Id = Invoice.Id,
                    siDesc = Invoice.siDesc,
                    siAmt = Invoice.siAmt,
                    pstCode = Invoice.pstCode,
                    supCode = Invoice.supCode,
                    AccountID = Invoice.AccountID,
                    JobID = Invoice.JobID,
                    Remark = Invoice.Remark,
                    siCheckout = false,
                    AccountProfileId = apId,
                    CreateBy = user.UserCode,
                    CreateTime = DateTime.Now,
                });
            }

            context.SaveChanges();
        }

        public void GetInvoiceList(string strfrmdate, string strtodate, int PageNo, int SortCol, string SortOrder, string Keyword, int filter, string searchmode)
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

            InvoiceList = SqlConnection.Query<InvoiceModel>(@"EXEC dbo.GetInvoicesByCode @apId=@apId,@frmdate=@frmdate,@todate=@todate,@sortName=@sortName,@sortOrder=@sortOrder,@userCode=@userCode,@baseUrl=@baseUrl,@Keyword=@Keyword", objects).ToList();

            PagingInvoiceList = InvoiceList.ToPagedList(PageNo, PageSize);
        }

        public void GetInvoice(string pstCode, string InvoiceId = "")
        {
            bool isapprover = (bool)HttpContext.Current.Session["IsApprover"];
            using var context = new MMDbContext();
            var connection = new SqlConnection(defaultConnection);
            connection.Open();

            Purchase = connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id,@code=@code", new { apId, Id = 0, code = pstCode });

            Invoice = new InvoiceModel();
            Device device = null;
            if (!isapprover) device = context.Devices.First();

            DateTime dateTime = DateTime.Now;

            string baseUrl = UriHelper.GetBaseUrl();

            InvoiceList = connection.Query<InvoiceModel>(@"EXEC dbo.GetInvoicesByCode @apId=@apId,@pstCode=@pstCode,@baseUrl=@baseUrl", new { apId, pstCode, baseUrl }).ToList();

            if (InvoiceList.Count == 0)
            {
                LastInvoiceId = GenInvoiceId(pstCode, 0);
                Invoice.Id = LastInvoiceId;
                Invoice.siAmt = Purchase.pstAmount;

                var currentInvoice = context.Invoices.FirstOrDefault(x => x.Id == LastInvoiceId);
                if (currentInvoice == null)
                {
                    context.Invoices.Add(new MMDAL.Invoice
                    {
                        Id = LastInvoiceId,
                        pstCode = pstCode,
                        siAmt = Purchase.pstAmount,
                        siCheckout = false,
                        AccountProfileId = apId,
                        CreateBy = User.UserCode,
                        CreateTime = dateTime,
                    });
                    context.SaveChanges();
                }

                var currentLines = context.InvoiceLines.Where(x => x.InvoiceId == LastInvoiceId).ToList();
                long newLineId = 0;
                if (currentLines.Count == 0)
                {
                    var Line = context.InvoiceLines.Add(new InvoiceLine
                    {
                        InvoiceId = LastInvoiceId,
                        ilAmt = 0,
                        AccountProfileId = apId,
                        CreateBy = User.UserCode,
                        CreateTime = dateTime,
                    });
                    context.SaveChanges();
                    newLineId = Line.Id;
                }

                var currentPayments = context.InvoicePayments.Where(x => x.InvoiceId == LastInvoiceId).ToList();
                long newPayId = 0;
                if (currentPayments.Count == 0)
                {
                    var Pay = context.InvoicePayments.Add(new InvoicePayment
                    {
                        InvoiceId = LastInvoiceId,
                        sipAmt = 0,
                        sipCheckout = false,
                        AccountProfileId = apId,
                        CreateBy = User.UserCode,
                        CreateTime = dateTime,
                    });
                    context.SaveChanges();
                    newPayId = Pay.Id;
                }

                Invoice.Lines.Add(new InvoiceLineModel
                {
                    Id = newLineId,
                    InvoiceId = LastInvoiceId,
                    ilAmt = 0,
                });

                Invoice.Payments.Add(new InvoicePaymentModel
                {
                    Id = newPayId,
                    InvoiceId = LastInvoiceId,
                    sipAmt = 0,
                });

                InvoiceList.Add(Invoice);
            }
            else
            {
                Invoice = string.IsNullOrEmpty(InvoiceId) ? InvoiceList.OrderByDescending(x => x.CreateTime).FirstOrDefault() : InvoiceList.FirstOrDefault(x => x.Id == InvoiceId);

                Invoice.Lines = connection.Query<InvoiceLineModel>(@"EXEC dbo.GetInvoiceLinesById @apId=@apId,@InvoiceId=@InvoiceId", new { apId, InvoiceId = Invoice.Id }).ToList();
                Invoice.Payments = connection.Query<InvoicePaymentModel>(@"EXEC dbo.GetInvoicePaymentsById @apId=@apId,@InvoiceId=@InvoiceId", new { apId, InvoiceId = Invoice.Id }).ToList();

                LastInvoiceId = Invoice.Id;

                List<InvoiceLineInfoModel> lineinfoes = connection.Query<InvoiceLineInfoModel>("EXEC dbo.GetInvoiceLineFilesByInvId @apId=@apId,@invoiceId=@invoiceId", new { apId, invoiceId = Invoice.Id }).ToList();
                DicInvLnFileList = new Dictionary<string, List<InvoiceLineInfoModel>>();

                HashSet<long?> lineIds = new HashSet<long?>();
                //init DicInvLnFileList
                if (lineinfoes != null && lineinfoes.Count > 0)
                {
                    lineIds = lineinfoes.Select(x => x.lineId).ToHashSet();
                    foreach (var lineId in lineIds) DicInvLnFileList[lineId.ToString()] = new List<InvoiceLineInfoModel>();
                }

                if (lineIds.Count > 0)
                {
                    foreach (var lineId in lineIds)
                    {
                        var infoes = lineinfoes.Where(x=>x.lineId == lineId).ToList();
                        if (DicInvLnFileList.Keys.Contains(lineId.ToString())) DicInvLnFileList[lineId.ToString()] = infoes;

                        var line = Invoice.Lines.FirstOrDefault(x => x.Id == lineId);
                        line.FileList = new List<string>();
                        foreach(var info in infoes)
                        {
                            string file = $"<li><a class=\"filelnk\" href=\"#\" data-lnk=\"/Uploads/Inv/{apId}/{Invoice.Id}/{lineId}/{info.fileName}\" data-id=\"{info.Id}\">{info.fileName}</a><i class=\"mx-2 fa-solid fa-trash removefile pointer line\" data-id=\"{info.Id}\" data-lineid=\"{info.lineId}\" data-name=\"{info.fileName}\" aria-hidden=\"true\"></i></li>";
                            line.FileList.Add(file);                            
                        }
                    }                    
                }
            }

            JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();

            DicAcAccounts = ModelHelper.GetDicAcAccounts(connection);

            PoSettings = PoSettingsEditModel.GetPoSettings(connection);

            NextInvoiceId = GenInvoiceId(pstCode, CommonHelper.GetDigitsFrmString(LastInvoiceId.Split('-')[1]));
        }

        private string GenInvoiceId(string pstCode, int numId)
        {
            return CommonHelper.GenOrderNumber(pstCode, int.Parse(ConfigurationManager.AppSettings["SupInvoiceLength"]), numId);
        }
        public static void EditPayment(InvoicePaymentModel model)
        {
            using var context = new MMDbContext();
            //todo: editpayment

            context.SaveChanges();
        }

        public static void AddInvoice(string pstCode, string invoiceId)
        {
            using var context = new MMDbContext();
            MMDAL.Invoice invoice = context.Invoices.FirstOrDefault(x => x.AccountProfileId == apId && x.Id == invoiceId);
            var purchase = context.Purchases.FirstOrDefault(x => x.pstCode == pstCode);

            if (invoice == null)
            {
                context.Invoices.Add(new MMDAL.Invoice
                {
                    Id = invoiceId,
                    pstCode = pstCode,
                    siAmt = purchase.pstAmount,
                    siCheckout = false,
                    AccountProfileId = apId,
                    CreateBy = user.UserCode,
                    CreateTime = DateTime.Now,
                });
                context.SaveChanges();
            }

        }

        public static List<InvoiceLineModel> SaveLine(InvoiceLineModel InvoiceLine)
        {
            using var context = new MMDbContext();
            InvoiceLine line = context.InvoiceLines.FirstOrDefault(x => x.Id == InvoiceLine.Id);
            if (line == null)
            {
                context.InvoiceLines.Add(new InvoiceLine
                {
                    InvoiceId = InvoiceLine.InvoiceId,
                    ilDesc = InvoiceLine.ilDesc,
                    Remark = InvoiceLine.Remark,
                    AccountID = InvoiceLine.AccountID,
                    JobID = InvoiceLine.JobID,
                    ilAmt = InvoiceLine.ilAmt,
                    AccountProfileId = apId,
                    CreateBy = user.UserCode,
                    CreateTime = DateTime.Now,
                });
            }
            else
            {
                line.InvoiceId = InvoiceLine.InvoiceId;
                line.ilDesc = InvoiceLine.ilDesc;
                line.Remark = InvoiceLine.Remark;
                line.AccountID = InvoiceLine.AccountID;
                line.JobID = InvoiceLine.JobID;
                line.ilAmt = InvoiceLine.ilAmt;
                line.ModifyBy = user.UserCode;
                line.ModifyTime = DateTime.Now;
            }
            context.SaveChanges();

            var connection = new SqlConnection(defaultConnection);
            connection.Open();
            return connection.Query<InvoiceLineModel>("EXEC dbo.GetInvoiceLinesById @apId=@apId,@InvoiceId=@InvoiceId", new { apId, InvoiceLine.InvoiceId }).ToList();
        }
    }

}
