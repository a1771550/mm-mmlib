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
using System.Web.UI;

namespace MMLib.Models.Invoice
{
    public class InvoiceEditModel : PagingBaseModel
    {
        public Dictionary<string, List<InvoicePayInfoModel>> DicInvPayFileList { get; set; } = new Dictionary<string, List<InvoicePayInfoModel>>();
        public Dictionary<string, List<InvoiceLineInfoModel>> DicInvLineFileList { get; set; } = new Dictionary<string, List<InvoiceLineInfoModel>>();
        public Dictionary<string, List<InvoicePayModel>> DicInvoicePays { get; set; } = new Dictionary<string, List<InvoicePayModel>>();
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
        public Dictionary<string, List<AccountModel>> DicAcAccounts4Pay { get; set; }
        //public List<InvoicePayModel> InvoicePays { get; set; }

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
                        AccountID = 0,
                        JobID = 0,
                        AccountProfileId = apId,
                        CreateBy = User.UserCode,
                        CreateTime = dateTime,
                    });
                    context.SaveChanges();
                    newLineId = Line.Id;
                }

                var currentPayments = context.InvoicePays.Where(x => x.InvoiceId == LastInvoiceId).ToList();
                long newPayId = 0;
                if (currentPayments.Count == 0)
                {
                    var Pay = context.InvoicePays.Add(new InvoicePay
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
                    AccountID = 0,
                    JobID = 0,
                    ilAmt = 0,
                });

                Invoice.Pays.Add(new InvoicePayModel
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
                Invoice.Pays = connection.Query<InvoicePayModel>(@"EXEC dbo.GetInvoicePaysById @apId=@apId,@InvoiceId=@InvoiceId", new { apId, InvoiceId = Invoice.Id }).ToList();

                LastInvoiceId = Invoice.Id;

                #region Handle DicInvLineFileList
                List<InvoiceLineInfoModel> lineinfoes;
                HashSet<long?> lineIds;
                DicInvLineFileList = HandleDicInvLineFileList(connection, Invoice.Id, out lineinfoes, out lineIds);
                #endregion

                if (lineIds.Count > 0)
                {
                    foreach (var lineId in lineIds)
                    {
                        var infoes = lineinfoes.Where(x => x.lineId == lineId).ToList();
                        var line = Invoice.Lines.FirstOrDefault(x => x.Id == lineId);
                        line.FileList = new List<string>();
                        foreach (var info in infoes)
                        {
                            string file = $"<li><a class=\"filelnk\" href=\"#\" data-lnk=\"/Uploads/Inv/{apId}/{Invoice.Id}/{lineId}/{info.fileName}\" data-id=\"{info.Id}\">{info.fileName}</a><i class=\"mx-2 fa-solid fa-trash removefile pointer line\" data-id=\"{info.Id}\" data-lineid=\"{info.lineId}\" data-name=\"{info.fileName}\" aria-hidden=\"true\"></i></li>";
                            line.FileList.Add(file);
                        }
                    }
                }

                #region Handle DicInvPayFileList
                List<InvoicePayInfoModel> payinfoes;
                HashSet<long?> payIds;
                DicInvPayFileList = HandleDicInvPayFileList(connection, Invoice.Id, out payinfoes, out payIds);
                #endregion

                if (payIds.Count > 0)
                {
                    foreach (var payId in payIds)
                    {
                        var infoes = payinfoes.Where(x => x.payId == payId).ToList();
                        var pay = Invoice.Pays.FirstOrDefault(x => x.Id == payId);
                        pay.FileList = new List<string>();
                        foreach (var info in infoes)
                        {
                            string file = $"<li><a class=\"filelnk\" href=\"#\" data-lnk=\"/Uploads/Inv/{apId}/{Invoice.Id}/{payId}/{info.fileName}\" data-id=\"{info.Id}\">{info.fileName}</a><i class=\"mx-2 fa-solid fa-trash removefile pointer pay\" data-id=\"{info.Id}\" data-payid=\"{info.payId}\" data-name=\"{info.fileName}\" aria-hidden=\"true\"></i></li>";
                            pay.FileList.Add(file);
                        }
                    }
                }
            }

            JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();

            DicAcAccounts = ModelHelper.GetDicAcAccounts(connection);
            DicAcAccounts4Pay = ModelHelper.GetDicAcAccounts(connection, "GetAccountList4PayBills");

            PoSettings = PoSettingsEditModel.GetPoSettings(connection);

            NextInvoiceId = GenInvoiceId(pstCode, CommonHelper.GetDigitsFrmString(LastInvoiceId.Split('-')[1]));
        }

        public static Dictionary<string, List<InvoiceLineInfoModel>> HandleDicInvLineFileList(SqlConnection connection, string invoiceId, out List<InvoiceLineInfoModel> lineinfoes, out HashSet<long?> lineIds)
        {
            lineinfoes = connection.Query<InvoiceLineInfoModel>("EXEC dbo.GetInvoiceLineFilesByInvId @apId=@apId,@invoiceId=@invoiceId", new { apId, invoiceId }).ToList();
            var DicInvLineFileList = new Dictionary<string, List<InvoiceLineInfoModel>>();

            lineIds = new HashSet<long?>();
            lineIds = handleDicInvLineFileList(lineinfoes, DicInvLineFileList, lineIds);

            return DicInvLineFileList;
        }

        public static Dictionary<string, List<InvoiceLineInfoModel>> HanldeDicInvLineFileList(string invoiceId)
        {
            var connection = new SqlConnection(defaultConnection);
            connection.Open();
            var lineinfoes = connection.Query<InvoiceLineInfoModel>("EXEC dbo.GetInvoiceLineFilesByInvId @apId=@apId,@invoiceId=@invoiceId", new { apId, invoiceId }).ToList();
            var DicInvLineFileList = new Dictionary<string, List<InvoiceLineInfoModel>>();

            handleDicInvLineFileList(lineinfoes, DicInvLineFileList);
            return DicInvLineFileList;
        }

        private static HashSet<long?> handleDicInvLineFileList(List<InvoiceLineInfoModel> lineinfoes, Dictionary<string, List<InvoiceLineInfoModel>> DicInvLineFileList, HashSet<long?> lineIds = null)
        {
            if (lineIds == null) lineIds = new HashSet<long?>();
            // init DicInvLineFileList
            if (lineinfoes != null && lineinfoes.Count > 0)
            {
                lineIds = lineinfoes.Select(x => x.lineId).ToHashSet();
                if (lineIds.Count > 0) foreach (var lineId in lineIds) DicInvLineFileList[lineId.ToString()] = new List<InvoiceLineInfoModel>();
            }

            if (lineIds.Count > 0) foreach (var lineId in lineIds) if (DicInvLineFileList.Keys.Contains(lineId.ToString())) DicInvLineFileList[lineId.ToString()] = lineinfoes.Where(x => x.lineId == lineId).ToList();
            return lineIds;
        }


        public static Dictionary<string, List<InvoicePayInfoModel>> HandleDicInvPayFileList(SqlConnection connection, string invoiceId, out List<InvoicePayInfoModel> payinfoes, out HashSet<long?> payIds)
        {
            payinfoes = connection.Query<InvoicePayInfoModel>("EXEC dbo.GetInvoicePayFilesByInvId @apId=@apId,@invoiceId=@invoiceId", new { apId, invoiceId }).ToList();
            var DicInvPayFileList = new Dictionary<string, List<InvoicePayInfoModel>>();

            payIds = new HashSet<long?>();
            payIds = handleDicInvPayFileList(payinfoes, DicInvPayFileList, payIds);

            return DicInvPayFileList;
        }

        public static Dictionary<string, List<InvoicePayInfoModel>> HanldeDicInvPayFileList(string invoiceId)
        {
            var connection = new SqlConnection(defaultConnection);
            connection.Open();
            var payinfoes = connection.Query<InvoicePayInfoModel>("EXEC dbo.GetInvoicePayFilesByInvId @apId=@apId,@invoiceId=@invoiceId", new { apId, invoiceId }).ToList();
            var DicInvPayFileList = new Dictionary<string, List<InvoicePayInfoModel>>();

            handleDicInvPayFileList(payinfoes, DicInvPayFileList);

            return DicInvPayFileList;
        }

        private static HashSet<long?> handleDicInvPayFileList(List<InvoicePayInfoModel> payinfoes, Dictionary<string, List<InvoicePayInfoModel>> DicInvPayFileList, HashSet<long?> payIds=null)
        {
            if(payIds==null) payIds = new HashSet<long?>();
            // init DicInvPayFileList
            if (payinfoes != null && payinfoes.Count > 0)
            {
                payIds = payinfoes.Select(x => x.payId).ToHashSet();
                if (payIds.Count > 0) foreach (var payId in payIds) DicInvPayFileList[payId.ToString()] = new List<InvoicePayInfoModel>();
            }

            if (payIds.Count > 0) foreach (var payId in payIds) if (DicInvPayFileList.Keys.Contains(payId.ToString())) DicInvPayFileList[payId.ToString()] = payinfoes.Where(x => x.payId == payId).ToList();
            return payIds;
        }


        private string GenInvoiceId(string pstCode, int numId)
        {
            return CommonHelper.GenOrderNumber(pstCode, int.Parse(ConfigurationManager.AppSettings["SupInvoiceLength"]), numId);
        }
        public static void EditPayment(InvoicePayModel model)
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
                    ilCheckout = false,
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

        public static List<InvoicePayModel> SavePay(InvoicePayModel InvoicePay)
        {
            using var context = new MMDbContext();
            InvoicePay pay = context.InvoicePays.FirstOrDefault(x => x.Id == InvoicePay.Id);
            if (pay == null)
            {
                context.InvoicePays.Add(new InvoicePay
                {
                    InvoiceId = InvoicePay.InvoiceId,
                    sipChequeNo = InvoicePay.sipChequeNo,
                    Remark = InvoicePay.Remark,
                    sipAmt = InvoicePay.sipAmt,
                    sipCheckout = false,
                    AccountProfileId = apId,
                    CreateBy = user.UserCode,
                    CreateTime = DateTime.Now,
                });
            }
            else
            {
                pay.InvoiceId = InvoicePay.InvoiceId;
                pay.sipChequeNo = InvoicePay.sipChequeNo;
                pay.Remark = InvoicePay.Remark;
                pay.sipAmt = InvoicePay.sipAmt;
                pay.ModifyBy = user.UserCode;
                pay.ModifyTime = DateTime.Now;
            }
            context.SaveChanges();

            var connection = new SqlConnection(defaultConnection);
            connection.Open();
            return connection.Query<InvoicePayModel>("EXEC dbo.GetInvoicePaysById @apId=@apId,@InvoiceId=@InvoiceId", new { apId, InvoicePay.InvoiceId }).ToList();
        }

        private static string StringHandlingForSQL(string str)
        {
            return CommonHelper.StringHandlingForSQL(str);
        }
        public static List<string> GetUploadServiceSqlList(long Id, ref DataTransferModel dmodel, MMDbContext context)
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
                sqlConnection.Open();

                List<InvoiceLineModel> ilList = sqlConnection.Query<InvoiceLineModel>("EXEC dbo.GetInvoiceLinesByPstId @apId=@apId,@pstId=@pstId", new { apId, pstId = Id }).ToList();

                dmodel.CheckOutIds_InvoiceLine = ilList.Select(x => x.Id).ToHashSet();

                sql = MyobHelper.InsertImportServicePurchasesSql;
                sql = sql.Replace("0", "{0}");

                for (int j = 0; j < MyobHelper.ImportServicePurchasesColCount; j++)
                {
                    columns.Add("'{" + j + "}'");
                }
                strcolumn = string.Join(",", columns);
                value = "";

                if (ilList.Count > 0)
                {
                    foreach (var line in ilList)
                    {
                        //INSERT INTO Import_Service_Purchases (PurchaseNumber,PurchaseDate,SuppliersNumber,DeliveryStatus,AccountNumber,CoLastName,ExTaxAmount,IncTaxAmount,PurchaseStatus) VALUES ('00001797','04/12/2023','SP100022','A','{account}','A1 DIGITAL','4000','4000')
                        value = string.Format("(" + strcolumn + ")", purchase.pstCode, purchase.PurchaseDate4ABSS, line.InvoiceId, "A", comInfo.comAccountNo, StringHandlingForSQL(line.SupplierName), purchase.Amount4Abss, purchase.Amount4Abss, StringHandlingForSQL(purchase.pstDesc));
                        values.Add(value);
                    }
                }


                sql = string.Format(sql, string.Join(",", values));
                sqllist.Add(sql);
            }

            #region Instalment Payments:

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

        public static void SaveAccId(string InvoiceId, int accId)
        {
            using var context = new MMDbContext();
            MMDAL.Invoice invoice = context.Invoices.Find(InvoiceId);
            if (invoice != null) {
                invoice.AccountID = accId;
                invoice.ModifyBy = user.UserCode;
                invoice.ModifyTime = DateTime.Now;
                context.SaveChanges();
            }
        }
    }

}
