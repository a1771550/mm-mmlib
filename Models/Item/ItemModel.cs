using System.Collections.Generic;
using System.Configuration;
using MMDAL;
using System.Linq;
using MMLib.Models.MYOB;
using System.Web.Mvc;
using MMLib.Helpers;
using CommonLib.Models;
using System.Web;
using MMLib.Models.Promotion;

namespace MMLib.Models.Item
{
    public class ItemModel : ItemView
    {       
        //ItemPromotion Id
        public long ipId { get; set; } = 0;
        public SimpleCategory Category { get; set; }
        public List<LocQty> LocQtyList { get; set; }        

        public List<SerialNoView> snlist { get; set; }
        public bool lstCheckout { get; set; }
        public bool IsABSS { get; set; }
        public ItemOptions ItemOptions { get; set; }
		public int OnHandStock { get; set; }
		public int Qty { get; set; }
		public int AbssQty { get; set; }
		public bool hasItemVari { get; set; }
		public List<ItemPromotionModel> ItemPromotions { get; set; }
		public List<JsStock> JsStockList { get; set; }
		public int OutOfBalance { get; set; }

		public Dictionary<string, Dictionary<string, int>> DicItemLocQty;
        public Dictionary<string, Dictionary<string, int>> DicItemAbssQty;

		public ItemModel()
        {
            DicLocQty = new Dictionary<string, int>();
            LocQtyList = new List<LocQty>();
            
            snlist = new List<SerialNoView>();
            PLs = new Dictionary<string, decimal?>();
            
            snlist = new List<SerialNoView>();
            DicItemAccounts = new Dictionary<string, string>();
            AttrList = new List<ItemAttributeModel>();
            SelectedAttrList4V = new List<ItemAttribute>();
            DicItemLocQty = new Dictionary<string, Dictionary<string, int>>();
            DicItemAbssQty = new Dictionary<string, Dictionary<string, int>>();
		}

        public ItemModel(int itemId) : this()
        {
            List<ItemModel> stocklist = new List<ItemModel>();
            List<string> salesitemcodes = new List<string>();
            using (var context = new MMDbContext())
            {  
                stocklist = ModelHelper.GetItemStockList(context);                
              
            }
            var item = stocklist.FirstOrDefault(x => x.itmItemID == itemId);
            if (item != null)
            {
                HasSalesRecords = salesitemcodes.Any(x => x == item.lstItemCode);
            }
            AttrList = new List<ItemAttributeModel>();
            Category = new SimpleCategory();
        }
    }

    public class SimpleCategory
    {
        public int catId { get; set; }
        public string catName { get; set; }
    }

    
}