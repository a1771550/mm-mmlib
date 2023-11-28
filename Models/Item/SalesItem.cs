using MMDAL;
using MMLib.Models.Promotion;
using System.Collections.Generic;
using System.Text.Json;

namespace MMLib.Models.Item
{
	public class SalesItem:MyobItem
    {        
        public List<ItemPromotionModel> ItemPromotions { get; set; }
        public Dictionary<string, Dictionary<string,int>> DicItemLocQty { get; set; }
        public int IsStock { get { return itmIsNonStock ? 0 : 1; } }
        public int QuantityAvailable { get; set; }
        public string lstStockLoc { get; set; }
        public decimal? PLA { get; set; }
        public decimal? PLB { get; set; }
        public decimal? PLC { get; set; }
        public decimal? PLD { get; set; }
        public decimal? PLE { get; set; }
        public decimal? PLF { get; set; }
        public int Qty { get; set; }
        public int? OnHandStock { get; set; }
        public int? OutOfBalance { get; set; }
        public List<string> LocStockIds { get; set; }
        public List<JsStock> JsStockList { get; set; }
        public string JsonJsStockList { get { return JsStockList != null && JsStockList.Count > 0 ? JsonSerializer.Serialize(JsStockList): string.Empty; } }

        public string NameDesc { get { return itmUseDesc ? itmDesc : itmName; } }

        public Dictionary<string, Dictionary<string, int>> DicItemAbssQty { get; set; }
        public int AbssQty { get; set; }
        public bool? chkBat { get; set; }
        public bool? chkSN { get; set; }
        public bool? chkVT { get; set; }
        public bool hasItemVari { get; set; }

        public SalesItem()
        {
            DicItemLocQty = new Dictionary<string, Dictionary<string, int>>();
            DicItemAbssQty = new Dictionary<string, Dictionary<string, int>>();
        }
    }
}
