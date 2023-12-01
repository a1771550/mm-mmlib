using CommonLib.Models;
using MMDAL;
using MMLib.Helpers;
using MMLib.Models.Purchase;
using System.Collections.Generic;
using System.Linq;

namespace MMLib.Models.Item
{
	public class ItemViewModel:BaseViewModel
	{
		public List<ItemModel> Items { get; set; }		
		public Dictionary<string, ItemOptions> DicItemOptions { get; set; }

        public Dictionary<string, List<BatchQty>> DicItemBatchQty;
        public Dictionary<string, List<BatDelQty>> DicItemBatDelQty;
        public Dictionary<string, Dictionary<string, List<string>>> DicItemBVList { get; set; }
        public Dictionary<string, Dictionary<string, List<Purchase.BatSnVt>>> DicItemBatSnVtList;
        public Dictionary<string, List<SnVt>> DicItemSnVtList;
        public Dictionary<string, List<VtQty>> DicItemVtQtyList;
        public Dictionary<string, List<VtDelQty>> DicItemVtDelQtyList;
        public List<PoItemBatVQ> PoItemBatVQList;

        public Dictionary<string, List<ItemAttribute>> DicItemAttrList { get; set; } = new Dictionary<string, List<ItemAttribute>>();      
        public Dictionary<string,List<ItemVariModel>> DicItemSelectedIVList { get; set; } = new Dictionary<string, List<ItemVariModel>>();
        public Dictionary<string, List<ItemVariModel>> DicItemVariations { get; set; } = new Dictionary<string, List<ItemVariModel>>();      
        public Dictionary<string, List<IGrouping<string,ItemVariModel>>> DicItemGroupedVariations { get; set; } = new Dictionary<string, List<System.Linq.IGrouping<string, ItemVariModel>>>();
        public Dictionary<string, List<string>> DicItemSnos;
        public string LatestUpdateTime { get; set; }
        public Dictionary<string, int> DicLocQty { get; set; }
        public Dictionary<string, int> DicBatTotalQty { get; set; }
        public Dictionary<string, Dictionary<string, List<string>>> DicItemBatVtList;
        public Dictionary<string, List<BatSnVt>> DicItemBatSnVt;
        public Dictionary<int, List<SeqSnVtList>> DicSeqSnVtList { get; set; }
        public string PrimaryLocation { get; set; }
		public bool EnableItemVari { get; set; }
		public bool EnableItemOptions { get; set; }

		public Dictionary<string, List<DistinctItem>> DicLocItemList;

        //for nonitemoptions items
        public Dictionary<string, Dictionary<string, int>> DicLocItemQty;
        public List<PoItemVariModel> PoIvInfo;
        public Dictionary<string, List<PoItemVariModel>> DicIvInfo;
        public Dictionary<string, List<IvQty>> DicIvQtyList;
        public Dictionary<string, List<IvDelQty>> DicIvDelQtyList;
        public Dictionary<string, int> DicIvTotalQty;

        public ItemViewModel()
        {
            DicLocItemList = new Dictionary<string, List<DistinctItem>>();
            DicLocQty = new Dictionary<string, int>();
            DicLocItemQty = new Dictionary<string, Dictionary<string, int>>();           

            PoIvInfo = new List<PoItemVariModel>();
            DicIvInfo = new Dictionary<string, List<PoItemVariModel>>();

            DicIvQtyList = new Dictionary<string, List<IvQty>>();
            DicIvDelQtyList = new Dictionary<string, List<IvDelQty>>();
        }
    }
}