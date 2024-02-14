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

namespace MMLib.Models.Invoice
{
    public class InvoiceEditModel:PagingBaseModel
    {
        public Dictionary<string, List<InvoicePaymentModel>> DicSupInvoicePayments { get; set; } = new Dictionary<string, List<InvoicePaymentModel>>();
        public IsUserRole IsUserRole { get { return UserEditModel.GetIsUserRole(user); } }
        public bool IsMD { get { return IsUserRole.ismuseumdirector; } }
        public bool IsDB { get { return IsUserRole.isdirectorboard; } }

        public string LastSupplierInvoiceId { get; set; }
        public PoSettings PoSettings { get; set; } = new PoSettings();
        public List<PoQtyAmtModel> PoQtyAmtList { get; set; }

        public SupplierModel SelectedSupplier { get; set; }

        //public List<SupplierModel> SupplierList { get; set; } = new List<SupplierModel>();
        public string RemoveFileIcon { get { return $"<i class='mx-2 fa-solid fa-trash removefile' data-id='{{2}}'></i>"; } }
        public string PDFThumbnailPath { get { return $"<img src='{UriHelper.GetBaseUrl()}/Images/pdf.jpg' class='thumbnail'/>"; } }
        public Dictionary<string, List<SupplierModel>> DicSupInfoes { get; set; }

        public List<string> ImgList;
        public List<string> FileList;
        public List<SupplierModel> SupplierList { get; set; }
        public Dictionary<string, string> DicSupCodeName { get; set; } = new Dictionary<string, string>();
        public List<SupplierModel> Suppliers { get; set; } = new List<SupplierModel>();
        public Dictionary<string, double> DicCurrencyExRate { get; set; }
        public List<MyobJobModel> JobList { get; set; }
        public string JsonJobList { get { return JobList == null ? "" : JsonSerializer.Serialize(JobList); } }
        public Dictionary<string, List<AccountModel>> DicAcAccounts { get; set; }
        public List<InvoicePaymentModel> InvoicePayments { get; set; }
        public List<InvoiceModel> SupplierInvoiceList { get; set; }
        public bool Readonly { get; set; }
        public InvoiceModel SelectedSupInvoice { get; set; }
        public List<InvoiceModel> InvoiceList { get; set; }
        public IPagedList<InvoiceModel> PagingInvoiceList { get; set; }
        public InvoiceModel Invoice { get; set; }
        public InvoiceEditModel()
        {
            JobList = new List<MyobJobModel>();
            ImgList = new List<string>();
            FileList = new List<string>();
            DicSupInfoes = new Dictionary<string, List<SupplierModel>>();
            PoQtyAmtList = [];
        }
        public static void SaveInvoice(InvoiceModel invoice)
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

            InvoiceList = SqlConnection.Query<InvoiceModel>(@"EXEC dbo.GetSupplierInvoicesByCode @apId=@apId,@frmdate=@frmdate,@todate=@todate,@sortName=@sortName,@sortOrder=@sortOrder,@userCode=@userCode,@baseUrl=@baseUrl,@Keyword=@Keyword", objects).ToList();

            PagingInvoiceList = InvoiceList.ToPagedList(PageNo, PageSize);
        }

        public void GetInvoice(string pstCode, long Id = 0)
        {
            bool isapprover = (bool)HttpContext.Current.Session["IsApprover"];
            using var context = new MMDbContext();

            Invoice = new InvoiceModel();
            Device device = null;
            if (!isapprover) device = context.Devices.First();

            var connection = new SqlConnection(defaultConnection);
            connection.Open();

            DateTime dateTime = DateTime.Now;

            string baseUrl = UriHelper.GetBaseUrl();

            //SupplierInvoiceList = connection.Query<SupplierInvoiceModel>(@"EXEC dbo.GetSupplierInvoicesByCode @apId=@apId,@pstCode=@pstCode,@baseUrl=@baseUrl", new { apId, Purchase.pstCode, baseUrl }).ToList();

            //if (SupplierInvoiceList.Count == 0)
            //{
            //    LastSupplierInvoiceId = GenInvoiceId(Purchase.pstCode, 0);
            //}
            //else
            //{
            //    int? numId = CommonHelper.GetNumberFrmString(SupplierInvoiceList.OrderByDescending(x => x.Id).FirstOrDefault().Id.Split('-')[1]);
            //    LastSupplierInvoiceId = GenInvoiceId(Purchase.pstCode, (int)numId);
            //}

            HashSet<string> SupInvoiceIds = new HashSet<string>();

            if (SupplierInvoiceList.Count > 0)
            {
                //SupInvoiceIds = SupplierInvoiceList.Select(x => x.Id).Distinct().ToHashSet();
                //foreach (var id in SupInvoiceIds) if (!DicSupInvoicePayments.ContainsKey(id)) DicSupInvoicePayments[id] = [];

                //InvoicePayments = connection.Query<SupplierPaymentModel>(@"EXEC dbo.GetSupplierPaymentsByIds @apId=@apId,@InvoiceIds=@InvoiceIds", new { apId, InvoiceIds = string.Join(",", SupInvoiceIds) }).ToList();

                //foreach (var invoice in SupplierInvoiceList) invoice.Payments = InvoicePayments.Where(x => x.InvoiceId == invoice.Id).ToList();

                //foreach (var payment in InvoicePayments)
                //{
                //    if (DicSupInvoicePayments.ContainsKey(payment.InvoiceId))
                //    {
                //        DicSupInvoicePayments[payment.InvoiceId].Add(new SupplierPaymentModel
                //        {
                //            payId = payment.Id,
                //            InvoiceId = payment.InvoiceId,
                //            seq = payment.seq,
                //            Amount = payment.Amount,
                //            spChequeNo = payment.spChequeNo,
                //            Remark = payment.Remark,
                //            CreateTime = payment.CreateTime,
                //            CreateBy = payment.CreateBy,
                //        });

                //    }
                //}
            }


            JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();

            DicAcAccounts = ModelHelper.GetDicAcAccounts(connection);

            Suppliers = connection.Query<SupplierModel>(@"EXEC dbo.GetSupplierList6 @apId=@apId", new { apId }).ToList();

            SupplierList = connection.Query<SupplierModel>(@"EXEC dbo.GetPurchaseSuppliersByCode @apId=@apId,@pstCode=@pstCode", new { apId, Invoice.pstCode }).ToList();
            SelectedSupplier = SupplierList.Single(x => x.Selected);

            DicSupCodeName = new Dictionary<string, string>();
            foreach (var supplier in SupplierList) if (!DicSupCodeName.ContainsKey(supplier.supCode)) DicSupCodeName[supplier.supCode] = supplier.supName;

            PoSettings = PoSettingsEditModel.GetPoSettings(connection);
            var suppurchaseInfoes = connection.Query<SupplierModel>(@"EXEC dbo.GetSuppliersInfoesByCode @apId=@apId,@pstCode=@pstCode", new { apId, Invoice.pstCode }).ToList();

            List<IGrouping<string, SupplierModel>> groupedsupPurchaseInfoes = new List<IGrouping<string, SupplierModel>>();
            if (suppurchaseInfoes != null && suppurchaseInfoes.Count > 0)
            {
                groupedsupPurchaseInfoes = suppurchaseInfoes.GroupBy(x => x.supCode).ToList();
            }
            groupedsupPurchaseInfoes = suppurchaseInfoes.Where(x => x.supCode == SelectedSupplier.supCode).GroupBy(x => x.supCode).ToList();

            getDicSupInfoes(groupedsupPurchaseInfoes);


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
                            spi.filePath = ModelHelper.GetPDFLnk(Invoice.pstCode, g.supCode, spi.fileName);
                            DicSupInfoes[g.supCode].Add(spi);
                        }
                    }
                }
            }
        }

        public static void EditPayment(InvoicePaymentModel model)
        {
            using var context = new MMDbContext();
            

            context.SaveChanges();
        }
    }

}
