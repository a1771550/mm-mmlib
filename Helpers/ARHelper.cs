using CommonLib.Models.MYOB;
using MMCommonLib.BaseModels;
using MMCommonLib.CommonModels;
using MMDAL;
using MMLib.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Helpers
{
	public class ARHelper
	{
		public static ComInfo comInfo { get { return HttpContext.Current.Session["ComInfo"] == null ? null : HttpContext.Current.Session["ComInfo"] as ComInfo; } }
		

		static SessUser user => HttpContext.Current.Session["User"] as SessUser;
		public static List<AccountReceivableView> GetARListFrmDB(int apId)
		{
			List<AccountReceivableView> ars = new List<AccountReceivableView>();
			using var context = new MMDbContext();
			var _ars = context.AccountReceivables.Where(x => x.AccountProfileId == apId).ToList();
			foreach (var ar in _ars)
			{
				var _ar = new AccountReceivableView();
				_ar.CustomerName = ar.CustomerName;
				_ar.CurrencySymbol = ar.CurrencySymbol;
				_ar.InvoiceDate = (DateTime)ar.InvoiceDate;
				_ar.InvoiceNumber = ar.InvoiceNumber;
				_ar.Amount = (decimal)ar.Amount;
				_ar.Discount = (decimal)ar.Discount;
				_ar.TermsOfPayment = new MMCommonLib.CommonModels.TermsOfPayment
                {
					TermsID = (int)ar.TermsID,
					TermsOfPaymentID = ar.TermsOfPaymentID,
					Description = ar.TermsDescription
				};
				_ar.SalesPerson = new SalesPerson
				{
					ID = (int)ar.SalesPersonID
				};
				_ar.CreateTime = ar.CreateTime;
				_ar.ModifyTime = (DateTime)ar.ModifyTime;
				ars.Add(_ar);
			}
			return ars;
		}
		public static List<AccountReceivableView> GetAccountReceivableList()
		{
			AbssConn abssConn = ModelHelper.GetCompanyProfiles(comInfo.AccountProfileId);
			return MYOBHelper.GetARList(abssConn);
		}
		public static void SaveARsToDB()
		{
			int apId = comInfo.AccountProfileId;
			List<AccountReceivableView> arlist = GetAccountReceivableList();

			DateTime dateTime = DateTime.Now;
			List<AccountReceivable> ars = new List<AccountReceivable>();
			foreach (var ar in arlist)
			{
				var _ar = new AccountReceivable();
				_ar.CustomerName = ar.CustomerName;
				_ar.CurrencySymbol = ar.CurrencySymbol;
				_ar.InvoiceDate = ar.InvoiceDate;
				_ar.InvoiceNumber = ar.InvoiceNumber;
				_ar.Amount = ar.Amount;
				_ar.Discount = ar.Discount;
				_ar.TermsID = ar.TermsOfPayment.TermsID;
				_ar.TermsOfPaymentID = ar.TermsOfPayment.TermsOfPaymentID;
				_ar.TermsDescription = ar.TermsOfPayment.Description;
				_ar.SalesPersonID = ar.SalesPerson.ID;				
				_ar.AccountProfileId = apId;
				_ar.CreateTime = dateTime;
				_ar.ModifyTime = dateTime;
				ars.Add(_ar);
			}
			using var context = new MMDbContext();
			#region remove current records first:
			var _ars = context.AccountReceivables.Where(x => x.AccountProfileId == apId).ToList();
			if (_ars.Count > 0)
			{
				context.AccountReceivables.RemoveRange(_ars);
				context.SaveChanges();
			}
			#endregion
			#region add records:
			context.AccountReceivables.AddRange(ars);
			context.SaveChanges();
			#endregion
		}
	}
}
