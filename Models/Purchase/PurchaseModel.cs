using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using CommonLib.Helpers;
using CommonLib.Models;
using System.Text.Json;
using MMDAL;
using MMLib.Models.Supplier;
using CommonLib.App_GlobalResources;

namespace MMLib.Models.Purchase
{
    public class PurchaseModel : MMDAL.Purchase
	{
		public ComInfo ComInfo { get { return HttpContext.Current.Session["ComInfo"] == null ? null : HttpContext.Current.Session["ComInfo"] as ComInfo; } }

		public DeviceModel Device { get; set; }
		public string Mode { get; set; }
		public string JsPurchaseDate { get; set; }
		public string JsPromisedDate { get; set; }
		public string SupplierNames { get; set; }
		public string SupNamesDisplay { get
			{
				//MaxSupNamesDisplayLength
                int maxlength4name = int.Parse(ConfigurationManager.AppSettings["MaxSupNamesDisplayLength"]);
				return (SupplierNames!=null && SupplierNames.Length > maxlength4name)? string.Concat(SupplierNames.Substring(0, maxlength4name), "..."):SupplierNames??string.Empty;
            }
        }
        public string RemarkDisplay
        {
            get
            {
				int maxremarkdisplaylength = int.Parse(ConfigurationManager.AppSettings["MaxRemarkDisplayLength"]);
                return pstRemark != null && pstRemark.Length > maxremarkdisplaylength ? string.Concat(pstRemark.Substring(0, maxremarkdisplaylength), "...") : pstRemark ?? string.Empty;
            }
		}
		public DateTime? ResponseTime { get; set; }
		public string ResponseTimeDisplay { get { return ResponseTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ResponseTime, true); } }
		public string PromisedDateDisplay { get { return pstPromisedDate == null ? "N/A" : CommonHelper.FormatDate((DateTime)pstPromisedDate, true); } }
		public string PurchaseDateDisplay { get { return pstPurchaseDate == null ? "N/A" : CommonHelper.FormatDate((DateTime)pstPurchaseDate, true); } }
		public string PurchaseTimeDisplay { get { return pstPurchaseTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)pstPurchaseTime, true); } }
		public string CreateTimeDisplay { get { return CommonHelper.FormatDateTime(CreateTime, true); } }
		public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }
		public string TrimmedRemark { get { return string.IsNullOrEmpty(pstRemark) ? "N/A" : CommonHelper.GetTrimmedCharacters(pstRemark, int.Parse(ConfigurationManager.AppSettings["MaxCharacterNumInList"])); } }
	
		public bool EnableTax { get { return TaxModel != null ? TaxModel.EnableTax : false; } }
		public bool InclusiveTax { get { return TaxModel != null ? TaxModel.TaxType == TaxType.Inclusive : false; } }
		public bool EnableSerialNo { get { return (bool)ComInfo.comEnableSN; } }
		public bool PriceEditable { get { return (bool)ComInfo.comEnablePriceEditable; } }
		public bool DiscEditable { get { return (bool)ComInfo.comEnableDiscEditable; } }

		public string Currency { get { return ComInfo.Currency; } }
		public string itmName { get; set; }
		public string itmDesc { get; set; }
		public string PSCodeDisplay { get { return string.Concat(pstLocStock, "-", pstCode); } }

		public int ireviewmode { get; set; }
		public SupplierModel Supplier { get; set; }
		public string ItemCodes { get; set; }
		public string PurchasePersonName { get; set; }
		public string ItemsNameDesc { get; set; }

		public TaxModel TaxModel { get; set; }
		public bool UseForexAPI { get; set; }


		public string StatusDisplay
		{
			get
			{
				string statustxt = "";
				if (pqStatus != null)
				{
					Enum.TryParse(pqStatus, out RequestStatus status);
					switch (status)
					{
						case RequestStatus.requestingByStaff:
							statustxt = string.Format(Resource.RequestingByFormat, Resource.Staff); break;
						case RequestStatus.requestingByDeptHead:
							statustxt = string.Format(Resource.RequestingByFormat, Resource.DeptHead); break;
						case RequestStatus.requestingByFinanceDept:
							statustxt = string.Format(Resource.RequestingByFormat, Resource.FinanceDept); break;

						case RequestStatus.rejectedByDeptHead:
							statustxt = string.Format(Resource.RejectedByFormat, Resource.DeptHead); break;
						case RequestStatus.rejectedByFinanceDept:
							statustxt = string.Format(Resource.RejectedByFormat, Resource.FinanceDept); break;

						case RequestStatus.onholdByFinanceDept:
							statustxt = string.Format(Resource.OnHoldByFormat, Resource.FinanceDept); break;

						case RequestStatus.onhold:
							statustxt = Resource.OnHold;
							break;

						case RequestStatus.approved:
							statustxt = Resource.Approved; break;
						case RequestStatus.rejected:
							statustxt = Resource.Rejected; break;

					}
				}

				return statustxt;
			}
		}

		public bool IsEditMode { get; set; }
		public string dateformat { get; set; }
		public string PurchaseDate4ABSS { get { return pstPurchaseDate != null ? CommonHelper.FormatDate4ABSS((DateTime)pstPurchaseDate, dateformat) : string.Empty; } }
		public string PromisedDate4ABSS { get { return pstPromisedDate != null ? CommonHelper.FormatDate4ABSS((DateTime)pstPromisedDate, dateformat) : string.Empty; } }

		public string SupplierName { get; set; }
		public double Amount4Abss { get { return pstAmount == null ? 0 : Convert.ToDouble((decimal)pstAmount); } }

		public bool FullPaidCheckedOut { get; set; }

		public bool IsSupplierABSS { get; set; }
		public PurchaseModel()
		{
		}

		public PurchaseModel(long Id, MMDbContext context)
		{
			//string value = string.Format("(" + strcolumn + ")", purchase.pstCode, purchase.PurchaseDate4ABSS, StringHandlingForSQL(purchase.pstSupplierInvoice), "A", comInfo.comAccountNo, StringHandlingForSQL(purchase.SupplierName), purchase.Amount, purchase.Amount, status);
			
		}
	}
}
