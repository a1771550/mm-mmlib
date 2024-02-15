﻿using CommonLib.Helpers;
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

namespace MMLib.Models.Invoice
{
    public class InvoiceEditModel : PagingBaseModel
    {
        public Dictionary<string, List<InvoicePaymentModel>> DicInvoicePayments { get; set; } = new Dictionary<string, List<InvoicePaymentModel>>();
        public IsUserRole IsUserRole { get { return UserEditModel.GetIsUserRole(user); } }
        public bool IsMD { get { return IsUserRole.ismuseumdirector; } }
        public bool IsDB { get { return IsUserRole.isdirectorboard; } }

        public string LastInvoiceId { get; set; }
        public PoSettings PoSettings { get; set; } = new PoSettings();

        public string RemoveFileIcon { get { return $"<i class='mx-2 fa-solid fa-trash removefile' data-id='{{2}}'></i>"; } }
        public string PDFThumbnailPath { get { return $"<img src='{UriHelper.GetBaseUrl()}/Images/pdf.jpg' class='thumbnail'/>"; } }


        public List<MyobJobModel> JobList { get; set; }
        public string JsonJobList { get { return JobList == null ? "" : JsonSerializer.Serialize(JobList); } }
        public Dictionary<string, List<AccountModel>> DicAcAccounts { get; set; }
        public List<InvoicePaymentModel> InvoicePayments { get; set; }

        public bool Readonly { get; set; }
        public InvoiceModel SelectedSupInvoice { get; set; }
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

        public void GetInvoice(string pstCode)
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

            InvoiceList = connection.Query<InvoiceModel>(@"EXEC dbo.GetSupplierInvoicesByCode @apId=@apId,@pstCode=@pstCode,@baseUrl=@baseUrl", new { apId, pstCode, baseUrl }).ToList();

            if (InvoiceList.Count == 0)
            {
                LastInvoiceId = GenInvoiceId(pstCode, 0);
                HashSet<string> SupInvoiceIds = new HashSet<string>();
                SupInvoiceIds = InvoiceList.Select(x => x.Id).Distinct().ToHashSet();
                foreach (var id in SupInvoiceIds) if (!DicInvoicePayments.ContainsKey(id)) DicInvoicePayments[id] = [];

                InvoicePayments = connection.Query<InvoicePaymentModel>(@"EXEC dbo.GetInvoicePaymentsByIds @apId=@apId,@InvoiceIds=@InvoiceIds", new { apId, InvoiceIds = string.Join(",", SupInvoiceIds) }).ToList();

                foreach (var invoice in InvoiceList) invoice.Payments = InvoicePayments.Where(x => x.InvoiceId == invoice.Id).ToList();

                foreach (var payment in InvoicePayments)
                {
                    if (DicInvoicePayments.ContainsKey(payment.InvoiceId))
                    {
                        DicInvoicePayments[payment.InvoiceId].Add(new InvoicePaymentModel
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
            else
            {
                int? numId = CommonHelper.GetNumberFrmString(InvoiceList.OrderByDescending(x => x.Id).FirstOrDefault().Id.Split('-')[1]);
                LastInvoiceId = GenInvoiceId(pstCode, (int)numId);
                Invoice.Id = LastInvoiceId;
            }

            JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();

            DicAcAccounts = ModelHelper.GetDicAcAccounts(connection);

            PoSettings = PoSettingsEditModel.GetPoSettings(connection);

            Purchase = connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@Id=@Id,@code=@code", new { apId, Id = 0, code = pstCode });

        }

        private string GenInvoiceId(string pstCode, int numId)
        {
            return CommonHelper.GenOrderNumber(pstCode, int.Parse(ConfigurationManager.AppSettings["SupInvoiceLength"]), numId);
        }
        public static void EditPayment(InvoicePaymentModel model)
        {
            using var context = new MMDbContext();


            context.SaveChanges();
        }
    }

}
