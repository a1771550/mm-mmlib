using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CommonLib.Helpers;
using CommonLib.Models;
using System.Text.Json;
using MMDAL;
using MMLib.Models.Purchase.Supplier;
using MMLib.Models.MYOB;
using MMLib.Models.Item;
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
        public string SupplierName { get; set; }
      
        public string PromisedDateDisplay { get { return CommonHelper.FormatDate(pstPromisedDate??DateTime.Now, true); } }
        public string PurchaseDateDisplay { get { return CommonHelper.FormatDate(pstPurchaseDate??DateTime.Now.AddDays(1), true); } }
        public string PurchaseTimeDisplay { get { return pstPurchaseTime==null?"N/A": CommonHelper.FormatDateTime((DateTime)pstPurchaseTime, true); } }
        public string CreateTimeDisplay { get { return CommonHelper.FormatDateTime(CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime==null?"N/A": CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }
        public string TrimmedRemark { get { return string.IsNullOrEmpty(pstRemark) ? "N/A" : CommonHelper.GetTrimmedCharacters(pstRemark, int.Parse(ConfigurationManager.AppSettings["MaxCharacterNumInList"])); } }
     
        public List<PurchaseItemModel> PurchaseItems;
        public string JsonPurchaseItems { get { return JsonSerializer.Serialize(PurchaseItems); } }
        public bool EnableTax { get { return TaxModel != null ? TaxModel.EnableTax : false; } }
        public bool InclusiveTax { get { return TaxModel != null ? TaxModel.TaxType == TaxType.Inclusive : false; } }
        public bool EnableSerialNo { get { return (bool)ComInfo.comEnableSN; } }
        public bool PriceEditable { get { return (bool)ComInfo.comEnablePriceEditable; } }
        public bool DiscEditable { get { return (bool)ComInfo.comEnableDiscEditable; } }
        
        public string Currency { get { return ComInfo.Currency; } }
        public string itmName { get; set; }
        public string itmDesc { get; set; }
        public string PSCodeDisplay { get { return string.Concat(pstLocStock, "-", pstCode); } }

        public decimal SubTotal { get { return PurchaseItems == null ? 0 : PurchaseItems.Sum(x => x.piUnitPrice * x.piQty); } }
        public string FormatSubTotal { get { return CommonHelper.FormatMoney(Currency, SubTotal); } }
        public decimal DiscTotal { get { return PurchaseItems == null ? 0 : (decimal)PurchaseItems.Sum(x => x.piUnitPrice * x.piDiscPc); } }
        public string FormatDiscTotal { get { return CommonHelper.FormatMoney(Currency, DiscTotal); } }
        public decimal TaxTotal { get {return PurchaseItems == null ? 0 : (decimal)PurchaseItems.Sum(x => x.piTaxAmt); } }
        public string FormatTaxTotal { get { return CommonHelper.FormatMoney(Currency, TaxTotal); } }
        public decimal Total { get { return PurchaseItems == null ? 0 : (decimal)PurchaseItems.Sum(x => x.piAmtPlusTax); } }
        public string FormatTotal { get { return CommonHelper.FormatMoney(Currency, Total); } }

      

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
                if(pstStatus!=null) {
     //               switch(pstStatus)
     //               {
     //                   case "requestingByStaff":
     //                       statustxt = string.Format(Resource.RequestingByFormat, Resource.Staff); break;
					//	case "requestingByDeptHead":
					//		statustxt = string.Format(Resource.RequestingByFormat, Resource.DeptHead); break;
					//	case "requestingByFinanceDept":
					//		statustxt = string.Format(Resource.RequestingByFormat, Resource.FinanceDept); break;
					//}
                    statustxt = pstStatus.ToUpper();
                }

                return statustxt;
            }
        }

		public bool IsEditMode { get; set; }

		public PurchaseModel()
        {
        }
    }
}
