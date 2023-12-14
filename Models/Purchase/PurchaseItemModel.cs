using System;
using System.Collections.Generic;
using System.Text.Json;
using CommonLib.Helpers;
using MMDAL;
using MMLib.Models.Item;
using RestSharp;

namespace MMLib.Models.Purchase
{
    public class PurchaseItemModel : PurchaseItem
    {
        #region For Exporting to ABSS
        public string dateformat { get; set; }
        public string PurchaseDate4ABSS { get { return pstPurchaseDate != null ? CommonHelper.FormatDate4ABSS((DateTime)pstPurchaseDate, dateformat) : string.Empty; } }
        public string PromisedDate4ABSS { get { return pstPromisedDate != null ? CommonHelper.FormatDate4ABSS((DateTime)pstPromisedDate, dateformat) : string.Empty; } }
        public string SupplierName { get; set; }
        public string pstSupplierInvoice { get; set; }
        public string pstCurrency { get; set; }
        public double pstExRate { get; set; }
        public string pstLocStock { get; set; }
        public string supCode { get; set; }
        #endregion

        public List<PoItemVariModel> poItemVariList { get; set; }
        public List<BatchModel> batchList { get; set; }
        public string JsonBatchList { get { return batchList == null ? "" : JsonSerializer.Serialize(batchList); } }
        public string ValidThruDisplay { get { return piValidThru == null ? "" : CommonHelper.FormatDate((DateTime)piValidThru); } }
        public string CreateTimeDisplay { get { return CommonHelper.FormatDate(CreateTime, true); } }
        public string PurchaseDateDisplay { get { return pstPurchaseDate==null?"N/A": CommonHelper.FormatDate((DateTime)pstPurchaseDate, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDate((DateTime)ModifyTime, true); } }

        public string itmName { get; set; }
        public bool? itmUseDesc { get; set; }
        public string itmDesc { get; set; }
        public string itmNameDesc { get { return (bool)itmUseDesc && !string.IsNullOrEmpty(itmDesc) ? itmDesc : itmName; } }

        public ItemModel Item { get; set; }
        public List<SerialNoView> SerialNoList { get; set; }
        public List<SnBatSeqVt> snbatseqvtlist { get; set; }
        //public string pstLocStock { get; set; }
        public string JsValidThru { get; set; }
        public DateTime? pstPurchaseDate { get; set; }
        //public long pstId { get; set; }
        public DateTime? pstPromisedDate { get; set; }
        public string pstRemark { get; set; }
        public string JobNumber { get; set; }
        public string TaxCode { get; set; }
        public int Myob_PaymentIsDue { get; set; }
        public int Myob_DiscountDays { get; set; }
        public int Myob_BalanceDueDays { get; set; }

        public List<ValidThruModel> vtList { get; set; }

        #region JS properties
        public int returnedQty;
        public int returnableQty;
        public int qtyToReturn;
        //public string sncode;
        //public SerialNo serialNo;
        #endregion

        public PurchaseItemModel()
        {
            qtyToReturn = 0;
            returnableQty = -1;
            returnedQty = 0;
            snbatseqvtlist = new List<SnBatSeqVt>();
            batchList = new List<BatchModel>();         
            SerialNoList = new List<SerialNoView>();
            vtList = new List<ValidThruModel>();
            poItemVariList = new List<PoItemVariModel>();
        }
    }

    public class PoItemVariModel:PoItemVariation
    {         
        public List<long> JsIvIdList { get; set; }
        public string iaName { get; set; }
        public string iaValue { get; set; }
    }
    public class ItemSnSeqVtList
    {
        public string itemcode { get; set; }
        public int seq { get; set; }
        public List<SnBatSeqVt> snseqvtlist { get; set; }
    }

    public class BatSnVt : SnVt
    {
        public string batcode { get; set; }
	}
    public class SnBatSeqVt : SnVt
    {
        public string batcode { get; set; }
        public int seq { get; set; }
        public int snseq { get; set; }
    }

    public class SnVt
    {
        public string sn { get; set; }
        public string vt { get; set; }
        public string pocode { get; set; }
        public string status { get; set; }
    }
}
