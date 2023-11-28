using System;
using System.Linq;
using System.Web;
using MMDAL;

namespace MMLib.Models.Purchase
{
    public class PurchaseReturnModel:BaseModel
    {
        public bool InclusiveTax { get; set; }
        public string PurchaseNoList { get; set; }
        public DateTime ValidThru { get; set; }
        public decimal InclusiveTaxRate { get; set; }
        public bool EnableSerialNo { get; set; }
        public bool EnableTax { get; set; }
        public bool PriceEditable { get; set; }
        public bool DiscEditable { get; set; }

        #region JS Properties:    
        public string itemcode { get; set; }
        public string batchcode{get;set;}
        public decimal price{get;set;}
        public int returnedQty{get;set;}
        public int returnableQty{get;set;}
        public int qtyToReturn{get;set;}
        public decimal discpc{get;set;}
        public decimal amount{get;set;}
        public decimal amountplustax{get;set;}
        public int seq{get;set;}
        public decimal taxrate{get;set;}
        public decimal taxamt{get;set;}
        public string itmNameDesc{get;set;}
        public string psRefCode{get;set;}
        public string PurchaseDateDisplay{get;set;}
        public string baseUnit{get;set;}
        public SerialNo serialNo{get;set;}
        public bool hasSN{get;set;}
        public string JsValidThru{get;set;}
        public string sellUnit { get; set; }
        #endregion

       
    }

}
