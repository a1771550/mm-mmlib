using CommonLib.BaseModels.MYOB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models.MYOB
{
	public class MyobItemModel : MyobBaseModel
	{
		public int ItemID { get; set; }
		public char IsInactive { get; set; }
		public string ItemName { get; set; }
		public string ItemNumber { get; set; }
		public decimal QuantityOnHand { get; set; }
		public decimal ValueOnHand { get; set; }
		public decimal SellOnOrder { get; set; }
		public decimal PurchaseOnOrder { get; set; }
		public char ItemIsSold { get; set; }
		public char ItemIsBought { get; set; }
		public char ItemIsInventoried { get; set; }
		public int InventoryAccountID { get; set; }
		public int IncomeAccountID { get; set; }
		public int ExpenseAccountID { get; set; }
		public string Picture { get; set; }
		public string ItemDescription { get; set; }
		public char UseDescription { get; set; }
		public int CustomList1ID { get; set; }
		public int CustomList2ID { get; set; }
		public int CustomList3ID { get; set; }
		public string CustomField1 { get; set; }
		public string CustomField2 { get; set; }
		public string CustomField3 { get; set; }
		public decimal BaseSellingPrice { get; set; }
		public char ItemIsTaxedWhenSold { get; set; }
		public string SellUnitMeasure { get; set; }
		public int SellUnitQuantity { get; set; }
		public Nullable<decimal> TaxExclusiveLastPurchasePrice { get; set; }
		public Nullable<decimal> TaxInclusiveLastPurchasePrice { get; set; }
		public char ItemIsTaxedWhenBought { get; set; }
		public string BuyUnitMeasure { get; set; }
		public int BuyUnitQuantity { get; set; }
		public int PrimarySupplierID { get; set; }
		public string SupplierItemNumber { get; set; }
		public decimal MinLevelBeforeReorder { get; set; }
		public decimal DefaultReorderQuantity { get; set; }
		public int DefaultSellLocationID { get; set; }
		public int DefaultReceiveLocationID { get; set; }
		public Nullable<decimal> TaxExclusiveStandardCost { get; set; }
		public Nullable<decimal> TaxInclusiveStandardCost { get; set; }
		public decimal ReceivedOnOrder { get; set; }
		public decimal QuantityAvailable { get; set; }
		public decimal LastUnitPrice { get; set; }
		public decimal NegativeQuantityOnHand { get; set; }
		public string NegativeValueOnHand { get; set; }
		public decimal NegativeAverageCost { get; set; }
		public decimal PositiveAverageCost { get; set; }
		public char ShowOnWeb { get; set; }
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
		public int LocationID { get; set; }

		public override string InvoiceDateDisplay => string.Empty;

		public override string InvoiceNumber { get; set; }
	}
}