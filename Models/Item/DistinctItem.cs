using MMDAL;
using MMLib.Helpers;
using MMLib.Models.Purchase;
using System.Collections.Generic;
using System.Linq;

namespace MMLib.Models.Item
{
	public class DistinctItem
	{
		public string ItemCode { get; set; }
		public string ItemName { get; set; }
		public string ItemDesc { get; set; }

		public bool IsNonStock { get; set; }
		public double ItemTaxRate { get; set; }
		public string ItemSupCode { get; set; }
        public List<BatchQty> BatchQtyList { get; set; }
        public List<BatDelQty> BatDelQtyList { get; set; }
        public Dictionary<string, List<string>> BVList { get; set; }
        public List<string> Snos { get; set; }
        public Dictionary<string, List<Purchase.BatSnVt>> SnBatVtList { get; set; }
        public List<SnVt> SnVtList { get; set; }
        public List<VtQty> VtQtyList { get; set; }
        public List<VtDelQty> VtDelQtyList { get; set; }
        public List<PoItemBatVQ> PoItemBatVQList { get; set; }
        public List<ItemAttribute> AttrList { get; set; }
        public List<ItemVariModel> SelectedIVList { get; set; }
        public List<IGrouping<string, ItemVariModel>> GroupedVariations { get; set; }
        public List<ItemVariModel> Variations { get; set; }
        public string NameDesc { get; set; }
        public bool? chkBat { get; set; }
        public bool? chkSN { get; set; }
        public bool? chkVT { get; set; }
        //public int QuantityAvailable { get; set; }
        //public string StockLoc { get; set; }
    }
}
