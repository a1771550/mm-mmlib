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
        public ComInfo ComInfo { get{ return HttpContext.Current.Session["ComInfo"] as ComInfo; } }
        public static ComInfo comInfo { get { return HttpContext.Current.Session["ComInfo"] as ComInfo; } }
        public Dictionary<string, ItemOptions> DicItemOptions { get; set; }
        public string JsonDicItemOptions { get { return JsonSerializer.Serialize(DicItemOptions); } }
        public DeviceModel Device { get; set; }
        public string Mode { get; set; }
        public string JsPurchaseDate { get; set; }
        public string JsPromisedDate { get; set; }
        public string SupplierName { get; set; }
      
        public string PromisedDateDisplay { get { return CommonHelper.FormatDate(pstPromisedDate??DateTime.Now, true); } }
        public string PurchaseDateDisplay { get { return CommonHelper.FormatDate(pstPurchaseDate??DateTime.Now.AddDays(1), true); } }
        public string PurchaseTimeDisplay { get { return pstPurchaseTime==null?"N/A": CommonHelper.FormatDate((DateTime)pstPurchaseTime, true); } }
        public string CreateTimeDisplay { get { return CommonHelper.FormatDate(CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime==null?"N/A": CommonHelper.FormatDate((DateTime)ModifyTime, true); } }
        public string TrimmedRemark { get { return string.IsNullOrEmpty(pstRemark) ? "N/A" : CommonHelper.GetTrimmedCharacters(pstRemark, int.Parse(ConfigurationManager.AppSettings["MaxCharacterNumInList"])); } }
        public List<SelectListItem> SupplierList { get; set; }
        public List<SelectListItem> LocationList { get; set; }
        public List<PurchaseItemModel> PurchaseItems;
        public List<PurchaseReturnItemModel> ReturnItems;
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

        public Dictionary<string, double> DicCurrencyExRate { get; set; }
        public string JsonDicCurrencyExRate { get { return JsonSerializer.Serialize(DicCurrencyExRate); } }

        public int ireviewmode { get; set; }
        public SupplierModel Supplier { get; set; }
        public string ItemCodes { get; set; }
        public string PurchasePersonName { get; set; }
        public string ItemsNameDesc { get; set; }
        public PurchaseOrderReviewModel PurchaseOrderReview { get; set; }
        public TaxModel TaxModel { get; set; }
        public bool UseForexAPI { get; set; }
        public Dictionary<string, string> DicLocation { get; set; }
        public string JsonDicLocation { get { return DicLocation == null ? "" : JsonSerializer.Serialize(DicLocation); } }

        public List<MyobJobModel> JobList { get; set; }
        public string JsonJobList { get { return JobList == null ? "" : JsonSerializer.Serialize(JobList); } }

        public Dictionary<string, List<ItemAttribute>> DicItemAttrList;      
        public Dictionary<string, List<ItemVariModel>> DicItemVariations;
        public Dictionary<string, List<IGrouping<string, ItemVariModel>>> DicItemGroupedVariations;
        public string JsonDicItemGroupedVariations { get { return DicItemGroupedVariations == null ? "" : JsonSerializer.Serialize(DicItemGroupedVariations); } }
        public List<string> ImgList;
        public List<string> FileList;

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
            DicCurrencyExRate = new Dictionary<string, double>();
            DicLocation = new Dictionary<string, string>();
            JobList=new List<MyobJobModel>();
            ImgList = new List<string>();
            FileList= new List<string>();

            DicItemAttrList = new Dictionary<string, List<ItemAttribute>>();
            DicItemVariations = new Dictionary<string, List<ItemVariModel>>();
            DicItemGroupedVariations = new Dictionary<string, List<IGrouping<string, ItemVariModel>>>();

        }
    }
}
