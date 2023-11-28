using CommonLib.Models;
using MMDAL;
using MMLib.Helpers;
using MMLib.Models;
using MMLib.Models.Item;
using MMLib.Models.Purchase;
using System.Collections.Generic;
using System.Linq;

namespace MMCommonLib.Models
{
	public class CentralDataModel
    {
        public TaxModel taxModel { get; set; }
        public bool isEpay { get; set; }
        public bool hassales { get; set; }
        public bool hascustomer { get; set; }
        public int noreceiptFound { get; set; }
        public int alreadyRefunded { get; set; }
       
        public List<ItemModel> items { get; set; }
        public decimal toBeRefunded { get; set; }
        public decimal remainamt { get; set; }
      
        public List<SerialNoView> snlist { get; set; }
      
        public List<ItemModel> nonInventorizedItems { get; set; }
        public ComInfoView companyinfo { get; set; }
        public ReceiptViewModel receipt { get; set; }
       
        public string refundCode { get; set; }
        public string ePayType { get; set; }
        public Dictionary<string, ItemOptions> DicItemOptions { get; set; }
        public Dictionary<string, List<BatchQty>> DicItemBatchQty;
        public Dictionary<string, List<BatDelQty>> DicItemBatDelQty;
        public Dictionary<string, Dictionary<string, List<string>>> DicItemBVList { get; set; }
        public Dictionary<string, List<string>> DicItemSnos;
        public Dictionary<string, Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>> DicItemBatSnVtList;
        public Dictionary<string, List<SnVt>> DicItemSnVtList;
        public Dictionary<string, List<VtQty>> DicItemVtQtyList;
        public Dictionary<string, List<VtDelQty>> DicItemVtDelQtyList;
        public List<PoItemBatVQ> PoItemBatVQList;
        public Dictionary<string, List<ItemAttribute>> DicItemAttrList { get; set; }
        public Dictionary<string, List<ItemVariModel>> DicItemSelectedIVList { get; set; }
        public Dictionary<string, List<IGrouping<string, ItemVariModel>>> DicItemGroupedVariations { get; set; }
        public Dictionary<string, List<ItemVariModel>> DicItemVariations { get; set; }
        public Dictionary<string, int> DicBatTotalQty { get; set; }
        public string PrimaryLocation { get; set; }

        public Dictionary<string, Dictionary<string, List<string>>> DicItemBatVtList;
        public Dictionary<string, List<BatSnVt>> DicItemBatSnVt;
        public Dictionary<int, List<SeqSnVtList>> DicSeqSnVtList;
        
        public Dictionary<string, List<IvQty>> DicIvQtyList;
        public Dictionary<string, List<IvDelQty>> DicIvDelQtyList;
        public Dictionary<string, int> DicIvTotalQty;

        public CentralDataModel()
        {
           
            items = new List<ItemModel>();
            nonInventorizedItems = new List<ItemModel>();
          

            DicIvQtyList = new Dictionary<string, List<IvQty>>();
            DicIvDelQtyList = new Dictionary<string, List<IvDelQty>>();
        }
    }
}
