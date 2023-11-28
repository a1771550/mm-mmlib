using CommonLib.Helpers;
using MMCommonLib.CommonModels;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Item
{
	public class ARItem
	{
		public string ItemNameDesc { get { return itmUseDesc == null ? ItemName : (bool)itmUseDesc ? ItemName : ItemDesc; } }
		public bool? itmIsBought { get; set; }

		public int ItemID { get; set; }
		public bool IsInactive { get; set; }
		public string ItemName { get; set; }

		public string ItemNumber { get; set; }
		public decimal? QuantityOnHand { get; set; }
		public decimal? ValueOnHand { get; set; }
		public decimal? SellOnOrder { get; set; }
		public decimal? PurchaseOnOrder { get; set; }
		public string ItemIsSold { get; set; }
		public string ItemIsBought { get; set; }
		public string ItemIsInventoried { get; set; }
		public string Picture { get; set; }
		public string ItemDesc { get; set; }
		public string UseDescription { get; set; }
		public int? CustomList1ID { get; set; }
		public int? CustomList2ID { get; set; }
		public int? CustomList3ID { get; set; }
		public string CustomField1 { get; set; }
		public string CustomField2 { get; set; }
		public string CustomField3 { get; set; }
		public decimal? BaseSellingPrice { get; set; }
		public string ItemIsTaxedWhenSold { get; set; }
		public string SellUnitMeasure { get; set; }
		public int? SellUnitQuantity { get; set; }
		public decimal? TaxExclusiveLastPurchasePrice { get; set; }
		public decimal? TaxInclusiveLastPurchasePrice { get; set; }
		public bool? ItemIsTaxedWhenBought { get; set; }
		public string BuyUnitMeasure { get; set; }
		public int? BuyUnitQuantity { get; set; }
		public int? PrimarySupplierID { get; set; }
		public string SupplierItemNumber { get; set; }
		public decimal? MinLevelBeforeReorder { get; set; }
		public decimal? DefaultReorderQuantity { get; set; }
		public int? DefaultSellLocationID { get; set; }
		public int? DefaultReceiveLocationID { get; set; }
		public decimal? TaxExclusiveStandardCost { get; set; }
		public decimal? TaxInclusiveStandardCost { get; set; }
		public decimal? ReceivedOnOrder { get; set; }
		public decimal? QuantityAvailable { get; set; }
		public decimal? LastUnitPrice { get; set; }
		public decimal? NegativeQuantityOnHand { get; set; }
		public string NegativeValueOnHand { get; set; }
		public decimal? NegativeAverageCost { get; set; }
		public decimal? PositiveAverageCost { get; set; }
		public bool? ShowOnWeb { get; set; }
		public string NameOnWeb { get; set; }
		public string DescriptionOnWeb { get; set; }
		public string WebText { get; set; }
		public string WebCategory1 { get; set; }
		public string WebCategory2 { get; set; }
		public string WebCategory3 { get; set; }
		public string WebField1 { get; set; }
		public string WebField2 { get; set; }
		public string WebField3 { get; set; }
		public string ChangeControl { get; set; }

		public string LocationName { get; set; }
		public string LocationIDTxt { get; set; }
		public int? LocationID { get; set; }

		public List<MyobLocStock> Stocks { get; set; }

		public AccountProfileDTO AccountProfileDTO { get; set; }
		public int ItemLocationID { get; set; }
		public bool? itmUseDesc { get; set; }
		public bool? itmIsNonStock { get; set; }
		public bool? itmIsSold { get; set; }
		public int? itmDefaultSellLocationID { get; set; }
		public int? idx { get; set; }
		public DateTime? ModifyTime { get; set; }
		public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime); } }

		public MyobLocStock Stock { get; set; }
	}
}
