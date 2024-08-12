using CommonLib.Helpers;
using Dapper;
using PagedList;
using MMDAL;
using MMLib.Models.Supplier;
using MMLib.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using MMLib.Models.Invoice;

namespace MMLib.Models.Purchase
{
    public class PurchaseOrderEditModel : PagingBaseModel
	{
		public PurchaseModel PurchaseOrder { get; set; }
		public List<PurchaseModel> PurchaseOrderList { get; set; }
		public bool ShowWithDrawn { get; set; }
		public SupplierModel Supplier;
		public PurchaseOrderEditModel() { }
		public PagedList.IPagedList<PurchaseModel> PagingProcurementList { get; set; }
		public string KPurchasemanCode { get; set; }
		public string SearchModeList { get; set; }
		public void GetProcurementList(string strfrmdate, string strtodate, int PageNo, string SortName, string SortOrder, string Keyword, int filter, string searchmode, int iShowWithDrawn)
		{
			IsUserRole IsUserRole = UserEditModel.GetIsUserRole(user);

			CommonHelper.DateRangeHandling(strfrmdate, strtodate, out DateTime frmdate, out DateTime todate, DateFormat.YYYYMMDD, '-');
			using var context = new MMDbContext();
			if (Keyword == "") Keyword = null;
			DateFromTxt = strfrmdate;
			DateToTxt = strtodate;
			this.SortName = SortName;
			this.Keyword = Keyword;
			this.PageNo = PageNo;
			CurrentSortOrder = SortOrder;
			//if(filter==0)
			this.SortOrder = SortOrder == "desc" ? "asc" : "desc";

			if (filter == 1)SortOrder = SortOrder == "desc" ? "asc" : "desc";
			
			PurchaseOrderList = new List<PurchaseModel>();
			
			bool isStaff = IsUserRole.isstaff;
		
            string userCode = isStaff? user.UserCode:null;
			string sql = (!isStaff) ? "EXEC dbo.GetProcurementList @apId=@apId,@frmdate=@frmdate,@todate=@todate,@sortName=@sortName,@sortOrder=@sortOrder,@showWithDrawn=@showWithDrawn": "EXEC dbo.GetProcurementList @apId=@apId,@frmdate=@frmdate,@todate=@todate,@sortName=@sortName,@sortOrder=@sortOrder,@showWithDrawn=@showWithDrawn,@userCode=@userCode";
            if (SqlConnection.State == System.Data.ConnectionState.Closed) SqlConnection.Open();
            var orderlist = SqlConnection.Query<PurchaseModel>(sql, (!isStaff) ? new { apId, frmdate, todate, sortName = SortName, sortOrder = SortOrder, showWithDrawn=iShowWithDrawn==1 } : new { apId, frmdate, todate, sortName = SortName, sortOrder = SortOrder, showWithDrawn = iShowWithDrawn == 1, userCode }).ToList();
			orderlist = FilterOrderList(Keyword, searchmode, orderlist);

			List<PurchaseModel> filteredOrderList = new List<PurchaseModel>();

			if (orderlist.Count > 0)
			{					
				filteredOrderList = orderlist;

				var pstCodes = string.Join(",", orderlist.Select(x => x.pstCode).Distinct().ToHashSet());
				var invoicePaymentList = SqlConnection.Query<InvoicePayModel>(@"EXEC dbo.GetInvoicePays @apId=@apId,@pstCodes=@pstCodes", new { apId, pstCodes }).ToList();

				var groupedorderlist = filteredOrderList.GroupBy(x => x.pstCode).ToList();
				foreach (var group in groupedorderlist)
				{
					var g = group.FirstOrDefault();

					decimal totalCheckedOutPayments = invoicePaymentList.Where(x => x.pstCode == g.pstCode).Sum(x => x.sipAmt);
					if (totalCheckedOutPayments >= g.pstAmount) g.FullPaidCheckedOut = true;

                    PurchaseOrderList.Add(g);				
				}
				PagingProcurementList = PurchaseOrderList.ToPagedList(PageNo, PageSize);
			}

			ShowWithDrawn = iShowWithDrawn == 1;
		}

		private List<PurchaseModel> FilterOrderList(string Keyword, string searchmode, List<PurchaseModel> orderlist)
		{
			if (!string.IsNullOrEmpty(Keyword))
			{
				SearchModeList = searchmode;
				var keyword = Keyword.ToLower();
				var SearchMode = searchmode.Split(',');

				if (SearchMode.Length == 1)
				{
					if (SearchMode[0] == "0")
					{
						orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword)).ToList();
					}
					if (SearchMode[0] == "1")
					{
						orderlist = orderlist.Where(x => x.pstCode != null && x.pstCode.ToLower().Contains(keyword)).ToList();
					}
					if (SearchMode[0] == "2")
					{
						orderlist = orderlist.Where(x => x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.SupplierNames != null && x.SupplierNames.ToLower().Contains(keyword)).ToList();
					}
				}
				if (SearchMode.Length == 2)
				{
					if (SearchMode[0] == "0" && SearchMode[1] == "1")
					{
						orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.pstCode != null && x.pstCode.ToLower().Contains(keyword)).ToList();
					}

					if (SearchMode[0] == "0" && SearchMode[1] == "2")
					{
						orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.SupplierNames != null && x.SupplierNames.ToLower().Contains(keyword) || x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword)).ToList();
					}
					if (SearchMode[0] == "1" && SearchMode[1] == "2")
					{
						orderlist = orderlist.Where(x => x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.SupplierNames != null && x.SupplierNames.ToLower().Contains(keyword) || x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.pstCode != null && x.pstCode.ToLower().Contains(keyword)).ToList();
					}
				}

				if (SearchMode.Length == 3)
				{
					orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.pstCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.SupplierNames != null && x.SupplierNames.ToLower().Contains(keyword)).ToList();
				}

			}

			return orderlist;
		}

        
    }
}
