using System;
using System.Collections.Generic;
using CommonLib.Helpers;
using MMDAL;
using MMLib.Models.Promotion;

namespace MMLib.Models.Item
{
    public class ItemVariModel : ItemVariation
    {
        public int poqty { get; set; }
        public int delqty { get; set; }
        public string itmBuyUnit { get; set; }
        public string itmSellUnit { get; set; } 
        public int? itmSellUnitQuantity { get; set; }
        public bool itmIsNonStock { get; set; } 
        public string itmSupCode { get; set; }
        public decimal? itmWeight { get; set; }
        public decimal? itmWidth { get; set; }
        public decimal? itmHeight { get; set; }
        public decimal? itmLength { get; set; }
        public decimal itmBaseSellingPrice { get; set; }
        public decimal? itmBuyStdCost { get; set; }
        public bool itmIsBought { get; set; }
        public int? ExpenseAccountID { get; set; }
        public bool itmIsSold { get; set; }
        public int? IncomeAccountID { get; set; }   
        public int? InventoryAccountID { get; set; }
        public List<ItemPromotionModel> ItemPromotions { get; set; }
        public int? catId { get; set; }
        public Dictionary<string, string> DicIvNameValList { get; set; }
        public string JsonDicIvNameValList { get { return DicIvNameValList != null ? System.Text.Json.JsonSerializer.Serialize(DicIvNameValList) : string.Empty; } }
        public string CreateTimeDisplay { get { return CreateTime == null ? "" : CommonHelper.FormatDateTime(CreateTime); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "" : CommonHelper.FormatDateTime((DateTime)ModifyTime); } }
        public Dictionary<string, string> DicItemAccounts { get; set; } = new Dictionary<string, string>();
        public List<ItemAttributeModel> AttrList { get; set; }
        public List<ItemAttributeModel> SelectedAttrList4V { get; set; }
        public Dictionary<string, decimal?> PLs { get; set; }
        public decimal? PLA { get; set; }
        public decimal? PLB { get; set; }
        public decimal? PLC { get; set; }
        public decimal? PLD { get; set; }
        public decimal? PLE { get; set; }
        public decimal? PLF { get; set; }

        public bool HasSalesRecords { get; set; }
        public Dictionary<string, int> DicLocQty { get; set; }
        public string JsonDicLocQty { get { return DicLocQty == null ? "" : System.Text.Json.JsonSerializer.Serialize(DicLocQty); } }
        public bool IsModifyAttr { get; set; }
        public string NameDesc { get { return itmUseDesc ? itmDesc : itmName; } }

        public bool itmUseDesc { get; set; }
        public string itmDesc { get; set; }
        public string itmName { get; set; }
        public string pocode { get; set; }

        public ItemVariModel()
        {
            DicLocQty = new Dictionary<string, int>();
            DicItemAccounts = new Dictionary<string, string>();
            AttrList = new List<ItemAttributeModel>();
            PLs = new Dictionary<string, decimal?>();
            DicIvNameValList = new Dictionary<string, string>();
        }

    }
}
