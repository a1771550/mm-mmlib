using CommonLib.Helpers;
using CommonLib.Models.MYOB;
using MMCommonLib.CommonModels;
using MMDAL;
using MMLib.Models.User;
using MMLib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Helpers
{
	public class QuotationHelper
	{
		public static ComInfo comInfo { get { return HttpContext.Current.Session["ComInfo"] == null ? null : HttpContext.Current.Session["ComInfo"] as ComInfo; } }
	

		static SessUser user => HttpContext.Current.Session["User"] as SessUser;
		public static ObservableCollection<QuotationView> GetQuotationListFrmDB(int apId, int userId, int pageIndex, int pageSize, string keyword)
		{
			ObservableCollection<QuotationView> quotations = new ObservableCollection<QuotationView>();
			List<QuotationView> quotationlist = new List<QuotationView>();

			using var context = new MMDbContext();

			var userEmployees = context.UserEmployees.Where(x => x.AccountProfileId == apId && x.UserId == userId).ToList();
			var userSalesPeople = context.UserSalesPersons.Where(x => x.AccountProfileId == apId && x.UserId == userId).ToList();

			var startIndex = CommonHelper.GetStartIndex(pageIndex, pageSize);

			var _quotations = context.GetQuotationList(apId, startIndex, pageSize, keyword).ToList();
			if (_quotations.Count > 0)
			{
				int idx = startIndex;
				foreach (var _quotation in _quotations)
				{
					quotationlist.Add(new QuotationView
					{
						idx = idx,
						InvoiceNumber = _quotation.InvoiceNumber,
						InvoiceDate = (DateTime)_quotation.InvoiceDate,
						CustomerName = _quotation.CustomerName,
						ItemNumber = _quotation.ItemCode,
						ItemName = _quotation.ItemName,
						SalesPersonName = _quotation.SalesPersonName,
						Qty = (decimal)_quotation.Qty,
						TaxInclusiveUnitPrice = (decimal)_quotation.TaxInclusiveUnitPrice,
						CurrencySymbol = _quotation.CurrencySymbol,
						SalesPersonID = _quotation.SalesPersonID,
						CustomerID = (int)_quotation.CustomerID,
						CreateTime = _quotation.CreateTime,
						ModifyTime = _quotation.ModifyTime,
					});
					idx++;
				}
			}


			if (!UserHelper.GetUserRoles(user).Contains(RoleType.ARAdmin))
			{
				if (userEmployees.Count > 0)
				{
					quotationlist = (from bar in quotationlist
									 join ue in userEmployees
										 on bar.SalesPersonID equals ue.EmployeeID
									 select bar
										  ).ToList();

				}
			}

			if (userSalesPeople.Count > 0)
			{
				quotationlist = (from tp in quotationlist
								 join usp in userSalesPeople
								 on tp.SalesPersonID equals usp.SalesPersonId
								 select tp
							  ).ToList();
			}

			quotationlist = quotationlist.Where(x => x.InvoiceDate.Year == DateTime.Now.Year).ToList();

			foreach (var q in quotationlist)
			{
				quotations.Add(q);
			}

			return quotations;
		}
		public static List<QuotationView> GetQuotationListFrmDB(int apId, int userId)
		{
			List<QuotationView> quotations = new List<QuotationView>();

			using var context = new MMDbContext();

			var userEmployees = context.UserEmployees.Where(x => x.AccountProfileId == apId && x.UserId == userId).ToList();
			var userSalesPeople = context.UserSalesPersons.Where(x => x.AccountProfileId == apId && x.UserId == userId).ToList();

			var _quotations = context.Quotations.Where(x => x.AccountProfileId == apId).ToList();
			if (_quotations.Count > 0)
			{
				foreach (var quotation in _quotations)
				{
					var _quotation = new QuotationView();
					_quotation.InvoiceNumber = quotation.InvoiceNumber;
					_quotation.InvoiceDate = (DateTime)quotation.InvoiceDate;
					_quotation.CustomerName = quotation.CustomerName;
					_quotation.ItemNumber = quotation.ItemCode;
					_quotation.ItemName = quotation.ItemName;
					_quotation.SalesPersonName = quotation.SalesPersonName;
					_quotation.Qty = (decimal)quotation.Qty;
					_quotation.TaxInclusiveUnitPrice = (decimal)quotation.TaxInclusiveUnitPrice;
					_quotation.CurrencySymbol = quotation.CurrencySymbol;
					_quotation.SalesPersonID = quotation.SalesPersonID;
					_quotation.CustomerID = (int)quotation.CustomerID;
					_quotation.CreateTime = quotation.CreateTime;
					_quotation.ModifyTime = quotation.ModifyTime;
					quotations.Add(_quotation);
				}
			}

			if (!UserHelper.GetUserRoles(user).Contains(RoleType.ARAdmin))
			{
				if (userEmployees.Count > 0)
				{
					quotations = (from bar in quotations
								  join ue in userEmployees
									  on bar.SalesPersonID equals ue.EmployeeID
								  select bar
										  ).ToList();
				}
			}

			if (userSalesPeople.Count > 0)
			{
				quotations = (from tp in quotations
							  join usp in userSalesPeople
				on tp.SalesPersonID equals usp.SalesPersonId
							  select tp
							  ).ToList();
			}

			return quotations;
		}

		public static List<QuotationView> GetQuotationList()
		{
			AbssConn abssConn = ModelHelper.GetCompanyProfiles(comInfo.AccountProfileId);
			return MYOBHelper.GetQuotationList(abssConn);
		}

		public static void SaveQuotationsToDB()
		{
			int apId = comInfo.AccountProfileId;
			List<QuotationView> quotationlist = GetQuotationList();

			DateTime dateTime = DateTime.Now;
			List<MMDAL.Quotation> quotations = [];
			foreach (var quotation in quotationlist)
			{
				quotations.Add(new MMDAL.Quotation
				{
					InvoiceNumber = quotation.InvoiceNumber,
					InvoiceDate = quotation.InvoiceDate,
					CustomerName = quotation.CustomerName,
					ItemCode = quotation.ItemNumber,
					ItemName = quotation.ItemName,
					SalesPersonName = quotation.SalesPersonName,
					Qty = quotation.Qty,
					TaxInclusiveUnitPrice = quotation.TaxInclusiveUnitPrice,
					CurrencySymbol = quotation.CurrencySymbol,
					SalesPersonID = quotation.SalesPersonID,
					CustomerID = quotation.CustomerID,
					CreateTime = dateTime,
					ModifyTime = dateTime,
					AccountProfileId = apId
				});
			}

			using var context = new MMDbContext();
			#region remove current records first:
			var _quotations = context.Quotations.Where(x => x.AccountProfileId == apId).ToList();
			if (_quotations.Count > 0)
			{
				context.Quotations.RemoveRange(_quotations);
				context.SaveChanges();
			}
			#endregion
			#region add records:
			context.Quotations.AddRange(quotations);
			context.SaveChanges();
			#endregion
		}
	}
}
