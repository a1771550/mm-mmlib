using CommonLib.Helpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MMLib.Models.Item
{
    public class ItemView : MyobItem
    {
        public bool chkBat { get; set; }
        public bool chkSN { get; set; }
        public bool chkVT { get; set; }
        public int? catId { get; set; } = 0;
        public int TotalBaseStockQty { get; set; } = 0;
        public bool IsModifyAttr { get; set; } = false;
        public List<ItemAttribute> SelectedAttrList4V { get; set; }
        public List<ItemAttributeModel> AttrList { get; set; }
        public string JsonAttrList { get { return AttrList != null && AttrList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(AttrList):""; } }
        public string NameDesc { get { return itmUseDesc ? itmDesc : itmName; } }
        public int IncomeAccountNo4Import { get; set; }
        public int ExpenseAccountNo4Import { get; set; }
        public int InventoryAccountNo4Import { get; set; }
        public long lstItemLocationID { get; set; }
        public int IncomeAccountNo { get { return string.IsNullOrEmpty(IncomeAccountNumber) ? 0 : int.Parse(IncomeAccountNumber.Replace("-", "")); } }
        public int InventoryAccountNo { get { return string.IsNullOrEmpty(InventoryAccountNumber) ? 0 : int.Parse(InventoryAccountNumber.Replace("-", "")); } }
        public int ExpenseAccountNo { get { return string.IsNullOrEmpty(ExpenseAccountNumber) ? 0 : int.Parse(ExpenseAccountNumber.Replace("-", "")); } }
        public string IncomeAccountNumber { get; set; }
        public string ExpenseAccountNumber { get; set; }
        public string InventoryAccountNumber { get; set; }
        public string JsonDicItemAccounts { get { return JsonSerializer.Serialize(DicItemAccounts); } }
        public Dictionary<string, string> DicItemAccounts { get; set; }//key must be a string or object
        public int lstItemID { get; set; }
        public int lstQuantityAvailable { get; set; }
        public string lstItemCode { get; set; }

        public bool HasSalesRecords { get; set; }
        public int ReplacingItemNameOnReceipt { get { return itmUseDesc ? 1 : 0; } }
        public string BaseSellingPriceDisplay { get { return CommonHelper.FormatMoney("", (decimal)itmBaseSellingPrice, false); } }
        public string SelectedLocation { get; set; }
        public int IsActive { get { return itmIsActive ? 1 : 0; } }
        public int IsNonStock { get { return itmIsNonStock ? 1 : 0; } }//for legacy...
        public int IsStock { get { return itmIsNonStock ? 0 : 1; } }
        public int IsBought { get { return itmIsBought ? 1 : 0; } }
        public int IsSold { get { return itmIsSold ? 1 : 0; } }    
      

        public Dictionary<string, decimal?> PLs { get; set; }
        public decimal? PLA { get; set; }
        public decimal? PLB { get; set; }
        public decimal? PLC { get; set; }
        public decimal? PLD { get; set; }
        public decimal? PLE { get; set; }
        public decimal? PLF { get; set; }

        public string lstStockLoc { get; set; }
        //stock on hand
        public int QuantityAvailable { get; set; }
        public string LocStockId { get; set; }
        //public string SellingPriceDisplay { getPG { return CommonHelper.FormatMoney("", (decimal)itmBaseSellingPrice, false); } }
        public string LastUnitPriceDisplay { get { return itmLastUnitPrice == null ? "" : CommonHelper.FormatMoney("", (decimal)itmLastUnitPrice, false); } }
        public string CreateTimeDisplay { get { return itmCreateTime == null ? "" : CommonHelper.FormatDateTime(itmCreateTime); } }
        public string ModifyTimeDisplay { get { return itmModifyTime == null ? "" : CommonHelper.FormatDateTime((DateTime)itmModifyTime); } }
        public string AdjustmentDate { get { return itmModifyTime == null ? "" : CommonHelper.FormatDate(((DateTime)itmModifyTime).Date); } }
        public string AdjustmentDate4Import { get; set; }
        public string AccountProfileName { get; set; }
        public Dictionary<string, int> DicLocQty { get; set; }
        public string JsonDicLocQty { get { return DicLocQty == null ? "" : System.Text.Json.JsonSerializer.Serialize(DicLocQty); } }
        public long? TransactionCount { get; set; }
    }
}
