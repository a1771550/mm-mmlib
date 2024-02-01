using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using CommonLib.Helpers;
using CommonLib.Models.MYOB;
using MMCommonLib.BaseModels;
using MMCommonLib.CommonModels;
using MMDAL;
using MMLib.Models.MYOB;
using MMLib.Models.POS.MYOB;
using MMMyobLib;

namespace MMLib.Helpers
{
    public static class MYOBHelper
	{
		private static string ConnectionString = ConfigurationManager.AppSettings["DefaultConnection"];
		//private static ComInfo ComInfo = HttpContext.Current.Session == null?null: HttpContext.Current.Session["ComInfo"] as ComInfo;

		public static DataTable GetQuotationList(AccountProfileDTO dto, int selectedLocationId = 0)
		{
			string sql = "SELECT main.InvoiceNumber as 'Order No',main.InvoiceDate as Date,c.Name as CustomerName,i.ItemNumber,i.ItemName,e.Name as 'SalesPersonName',l.Quantity as Qty,l.TaxInclusiveUnitPrice,main.SalesPersonID,c.CardRecordID as CustomerID FROM Sales main join ItemSaleLines l on main.SaleID = l.SaleID join Items i on l.ItemID = i.ItemID join Customers c on main.CardRecordID = c.CardRecordID left join Employees e on main.SalespersonID = e.EmployeeID where main.InvoiceStatusID = 'Q' ";
			Repository rs = new Repository();
			DataSet ds = rs.Query(sql, dto.ProfileName);
			DataTable dt = ds.Tables[0];
			return dt;
		}


		

		public static List<AccountClassificationModel> GetAccountClassificationList(string connectionstring, string sql = "")
		{
			if (string.IsNullOrEmpty(sql))
			{
				sql = MyobHelper.AccountClassificationListSql;
			}
			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];
			List<AccountClassificationModel> accountlist = new List<AccountClassificationModel>();
			accountlist = (from DataRow dr in dt.Rows
						   select new AccountClassificationModel()
						   {
							   AccountClassificationID = Convert.ToString(dr[0]),
							   Description = Convert.ToString(dr[1]),
						   }
						).ToList();
			return accountlist;
		}
		public static List<AccountModel> GetAccountList(string connectionstring, string sql = "")
		{
			if (string.IsNullOrEmpty(sql))
			{
				//public static string AccountListSql { getPG { return "Select AccountID, AccountName,AccountNumber,AccountClassificationID,AccountTypeID,AccountLevel From Accounts"; } }
				sql = MyobHelper.AccountListSql;
			}
			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];
			List<AccountModel> accountlist = new List<AccountModel>();
			accountlist = (from DataRow dr in dt.Rows
						   select new AccountModel()
						   {
							   AccountID = Convert.ToInt32(dr[0]),
							   AccountName = Convert.ToString(dr[1]),
							   AccountNumber = Convert.ToString(dr[2]),
							   AccountClassificationID = Convert.ToString(dr[3]),
							   AccountTypeID = Convert.ToString(dr[4]),
							   AccountLevel = Convert.ToByte(dr[5]),
						   }
						).ToList();
			return accountlist;
		}
		public static int GetItemCount(string dsn)
		{
			string sql = "Select Count(ItemID) from Items;";
			Repository rs = new Repository();
			DataSet ds = rs.Query(dsn, sql);
			if (ds != null)
			{
				DataTable dt = ds.Tables[0];
				return dt.Rows.Count;
			}
			return -1;
		}
		public static List<MyobItemPriceModel> GetItemPriceList(string connectionstring, string sql = "")
		{
			if (string.IsNullOrEmpty(sql))
			{
				//Select ip.ItemPriceID,ip.ItemID,ip.QuantityBreak,ip.QuantityBreakAmount,ip.PriceLevel,ip.PriceLevelNameID,ip.SellingPrice,ip.ChangeControl,i.ItemNumber,i.LastUnitPrice From ItemPrices ip Join Items i on ip.ItemID = i.ItemID Where ip.QuantityBreak=1;
				sql = MyobHelper.ItemPriceListSql;
			}
			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];
			List<MyobItemPriceModel> itemplist = new List<MyobItemPriceModel>();
			itemplist = (from DataRow dr in dt.Rows
						 select new MyobItemPriceModel()
						 {
							 ItemPriceID = Convert.ToInt32(dr[0]),
							 ItemID = dr[1] == DBNull.Value ? 0 : Convert.ToInt32(dr[1]),
							 QuantityBreak = (short?)(dr[2] == DBNull.Value ? 0 : Convert.ToInt16(dr[2])),
							 QuantityBreakAmount = dr[3] == DBNull.Value ? 0 : Convert.ToDouble(dr[3]),
							 PriceLevel = dr[4] == DBNull.Value ? '-' : Convert.ToChar(dr[4]),
							 PriceLevelNameID = dr[5] == DBNull.Value ? null : dr[5].ToString(),
							 SellingPrice = dr[6] == DBNull.Value ? 0 : Convert.ToDecimal(dr[6]),
							 ChangeControl = dr[7] == DBNull.Value ? null : dr[7].ToString(),
							 ItemCode = dr[8].ToString(),
							 LastUnitPrice = dr[9] == DBNull.Value ? 0 : Convert.ToDecimal(dr[9]),
						 }
						).ToList();
			return itemplist;
		}
		public static List<MyobItemModel> GetItemList(string connectionstring, string sql = "")
		{
			/*
			 * SELECT ItemID, IsInactive, ItemName, ItemNumber, QuantityOnHand, ValueOnHand, SellOnOrder, PurchaseOnOrder, ItemIsSold, ItemIsBought, ItemIsInventoried, InventoryAccountID, IncomeAccountID, ExpenseAccountID, Picture, ItemDescription, UseDescription, CustomList1ID, CustomList2ID, CustomList3ID, CustomField1, CustomField2, CustomField3, BaseSellingPrice, ItemIsTaxedWhenSold, SellUnitMeasure, SellUnitQuantity, TaxExclusiveLastPurchasePrice, TaxInclusiveLastPurchasePrice, ItemIsTaxedWhenBought, BuyUnitMeasure, BuyUnitQuantity, PrimarySupplierID, SupplierItemNumber, MinLevelBeforeReorder, DefaultReorderQuantity, DefaultSellLocationID, DefaultReceiveLocationID, TaxExclusiveStandardCost, TaxInclusiveStandardCost, ReceivedOnOrder, QuantityAvailable, LastUnitPrice, NegativeQuantityOnHand, NegativeValueOnHand, NegativeAverageCost, PositiveAverageCost, ShowOnWeb, NameOnWeb, DescriptionOnWeb, WebText, WebCategory1, WebCategory2, WebCategory3, WebField1, WebField2, WebField3, ChangeControl From Items;
			 */
			if (string.IsNullOrEmpty(sql))
			{
				sql = MyobHelper.ItemListSql;
			}
			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];
			List<MyobItemModel> itemlist = new List<MyobItemModel>();
			itemlist = (from DataRow dr in dt.Rows
						select new MyobItemModel()
						{
							ItemID = Convert.ToInt32(dr[0]),
							IsInactive = Convert.ToChar(dr[1]),
							ItemName = dr[2].ToString(),
							ItemNumber = dr[3].ToString(),
							QuantityOnHand = decimal.Parse(dr[4].ToString()),
							ValueOnHand = dr[5] == DBNull.Value ? 0 : decimal.Parse(dr[5].ToString()),
							SellOnOrder = decimal.Parse(dr[6].ToString()),
							PurchaseOnOrder = decimal.Parse(dr[7].ToString()),
							ItemIsSold = Convert.ToChar(dr[8]),
							ItemIsBought = Convert.ToChar(dr[9]),
							ItemIsInventoried = Convert.ToChar(dr[10]),
							InventoryAccountID = Convert.ToInt32(dr[11]),
							IncomeAccountID = Convert.ToInt32(dr[12]),
							ExpenseAccountID = Convert.ToInt32(dr[13]),
							Picture = dr[14].ToString(),
							ItemDescription = dr[15].ToString(),
							UseDescription = Convert.ToChar(dr[16]),
							CustomList1ID = Convert.ToInt32(dr[17]),
							CustomList2ID = Convert.ToInt32(dr[18]),
							CustomList3ID = Convert.ToInt32(dr[19]),
							CustomField1 = dr[20].ToString(),
							CustomField2 = dr[21].ToString(),
							CustomField3 = dr[22].ToString(),
							BaseSellingPrice = decimal.Parse(dr[23].ToString()),
							ItemIsTaxedWhenSold = Convert.ToChar(dr[24]),
							SellUnitMeasure = dr[25].ToString(),
							SellUnitQuantity = Convert.ToInt32(dr[26]),
							TaxExclusiveLastPurchasePrice = Convert.ToDecimal(dr[27]),
							TaxInclusiveLastPurchasePrice = Convert.ToDecimal(dr[28]),
							ItemIsTaxedWhenBought = Convert.ToChar(dr[29]),
							BuyUnitMeasure = dr[30].ToString(),
							BuyUnitQuantity = Convert.ToInt32(dr[31]),
							PrimarySupplierID = Convert.ToInt32(dr[32]),
							SupplierItemNumber = dr[33].ToString(),
							MinLevelBeforeReorder = decimal.Parse(dr[34].ToString()),
							DefaultReorderQuantity = Convert.ToInt32(dr[35]),
							DefaultReceiveLocationID = Convert.ToInt32(dr[36]),
							DefaultSellLocationID = Convert.ToInt32(dr[37]),
							TaxExclusiveStandardCost = decimal.Parse(dr[38].ToString()),
							TaxInclusiveStandardCost = decimal.Parse(dr[39].ToString()),
							ReceivedOnOrder = decimal.Parse(dr[40].ToString()),
							QuantityAvailable = Convert.ToInt32(dr[41]),
							LastUnitPrice = Convert.ToDecimal(dr[42]),
							NegativeQuantityOnHand = Convert.ToInt32(dr[43]),
							NegativeValueOnHand = dr[44] == DBNull.Value ? null : dr[44].ToString(),
							NegativeAverageCost = Convert.ToDecimal(dr[45]),
							PositiveAverageCost = Convert.ToDecimal(dr[46]),
							ShowOnWeb = Convert.ToChar(dr[47]),
							NameOnWeb = dr[48].ToString(),
							DescriptionOnWeb = dr[49].ToString(),
							WebText = dr[50].ToString(),
							WebCategory1 = dr[51].ToString(),
							WebCategory2 = dr[52].ToString(),
							WebCategory3 = dr[53].ToString(),
							WebField1 = dr[54].ToString(),
							WebField2 = dr[55].ToString(),
							WebField3 = dr[56].ToString(),
							ChangeControl = dr[57].ToString()
						}
					 ).ToList();
			return itemlist;
		}

		public static List<MyobCommentModel> GetCommentList(string connectionstring)
		{
			var sql = "Select CommentID, Comment From Comments;";
			List<MyobCommentModel> commentlist = new List<MyobCommentModel>();
			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];
			commentlist = (from DataRow dr in dt.Rows
						   select new MyobCommentModel()
						   {
							   CommentID = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
							   Comment = dr[1] == DBNull.Value ? null : Convert.ToString(dr[1]),
						   }).ToList();
			return commentlist;
		}

		public static List<MYOBCustomerModel> GetCustomerList(string connectionstring, string sql = "")
		{
			/* "c.CustomerID", "c.CardRecordID", "c.CardIdentification", "c.Name", "c.LastName", "c.FirstName", "c.IsIndividual", "c.IsInactive", "c.CurrencyID", "c.Picture", "c.Notes", "c.IdentifierID", "c.CustomList1ID", "c.CustomList2ID", "c.CustomList3ID", "c.CustomField1", "c.CustomField2", "c.CustomField3", "c.TermsID", "c.PriceLevelID", "c.TaxIDNumber", "c.TaxCodeID", "c.FreightIsTaxed", "c.CreditLimit", "c.VolumeDiscount", "c.CurrentBalance", "c.TotalDeposits", "c.CustomerSince", "c.LastSaleDate", "c.LastPaymentDate", "c.TotalReceivableDays", "c.TotalPaidInvoices", "c.HighestInvoiceAmount", "c.HighestReceivableAmount", "c.MethodOfPaymentID", "c.PaymentCardNumber", "c.PaymentNameOnCard", "c.PaymentExpiryDate", "c.PaymentNotes", "c.HourlyBillingRate", "c.SaleLayoutID", "c.PrintedForm", "c.IncomeAccountID", "c.SalespersonID", "c.SaleCommentID", "c.DeliveryMethodID", "c.ReceiptMemo", "c.ChangeControl", "c.OnHold", "c.InvoiceDeliveryID", "c.PaymentDeliveryID", "t.LatePaymentChargePercent", "t.EarlyPaymentDiscountPercent", "t.TermsOfPaymentID", "t.DiscountDays", "t.BalanceDueDays", "t.ImportPaymentIsDue", "t.DiscountDate", "t.BalanceDueDate", "p.Description", "cm.Comment", "cu.CurrencyCode"
			 */
			if (string.IsNullOrEmpty(sql))
			{
				//sql = MyobHelper.CustomerListSql_NoJoin;//Don't join!!! Chances are that customer will be empty if he has no phone record!!!!
				sql = MyobHelper.CustomerListSql;
			}
			List<MYOBCustomerModel> customerlist = new List<MYOBCustomerModel>();
			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];
			customerlist = (from DataRow dr in dt.Rows
							select new MYOBCustomerModel()
							{
								CustomerID = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
								CardRecordID = dr[1] == DBNull.Value ? 0 : Convert.ToInt32(dr[1]),
								CardIdentification = dr[2].ToString(),
								Name = dr[3].ToString(),
								LastName = dr[4].ToString(),
								FirstName = dr[5].ToString(),
								IsIndividual = dr[6] == DBNull.Value ? '-' : Convert.ToChar(dr[6]),
								IsInactive = dr[7] == DBNull.Value ? '-' : Convert.ToChar(dr[7]),
								CurrencyID = dr[8] == DBNull.Value ? 0 : Convert.ToInt32(dr[8]),
								Picture = dr[9].ToString(),
								Notes = dr[10].ToString(),
								IdentifierID = dr[11].ToString(),
								CustomList1ID = dr[12] == DBNull.Value ? 0 : Convert.ToInt32(dr[12]),
								CustomList2ID = dr[13] == DBNull.Value ? 0 : Convert.ToInt32(dr[13]),
								CustomList3ID = dr[14] == DBNull.Value ? 0 : Convert.ToInt32(dr[14]),
								CustomField1 = dr[15] == DBNull.Value ? null : Convert.ToString(dr[15]),
								CustomField2 = dr[16] == DBNull.Value ? null : Convert.ToString(dr[16]),
								//BRNo = dr[16] == DBNull.Value ? null : Convert.ToString(dr[16]),
								CustomField3 = dr[17] == DBNull.Value ? null : Convert.ToString(dr[17]),
								TermsID = dr[18] == DBNull.Value ? 0 : Convert.ToInt32(dr[18]),
								PriceLevelID = dr[19].ToString(),
								TaxIDNumber = dr[20].ToString(),
								TaxCodeID = dr[21] == DBNull.Value ? 0 : Convert.ToInt32(dr[21]),
								FreightIsTaxed = dr[22] == DBNull.Value ? '-' : Convert.ToChar(dr[22]),
								CreditLimit = dr[23] == DBNull.Value ? 0 : Convert.ToDecimal(dr[23]),
								VolumeDiscount = dr[24] == DBNull.Value ? 0 : Convert.ToDecimal(dr[24]),
								CurrentBalance = dr[25] == DBNull.Value ? 0 : Convert.ToDecimal(dr[25]),
								TotalDeposits = dr[26] == DBNull.Value ? 0 : Convert.ToDecimal(dr[26]),
								CustomerSince = dr[27] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr[27]),
								LastSaleDate = dr[28] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr[28]),
								LastPaymentDate = dr[29] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr[29]),
								TotalReceivableDays = dr[30] == DBNull.Value ? 0 : Convert.ToInt32(dr[30]),
								TotalPaidInvoices = dr[31] == DBNull.Value ? 0 : Convert.ToInt32(dr[31]),
								HighestInvoiceAmount = dr[32] == DBNull.Value ? 0 : Convert.ToDecimal(dr[32]),
								HighestReceivableAmount = dr[33] == DBNull.Value ? 0 : Convert.ToDecimal(dr[33]),
								MethodOfPaymentID = dr[34] == DBNull.Value ? 0 : Convert.ToInt32(dr[34]),
								PaymentCardNumber = dr[35].ToString(),
								PaymentNameOnCard = dr[36].ToString(),
								PaymentExpiryDate = dr[37].ToString(),
								PaymentNotes = dr[38].ToString(),
								HourlyBillingRate = dr[39] == DBNull.Value ? 0 : Convert.ToDecimal(dr[39]),
								SaleLayoutID = dr[40] == DBNull.Value ? '-' : Convert.ToChar(dr[40]),
								PrintedForm = dr[41].ToString(),
								IncomeAccountID = dr[42] == DBNull.Value ? 0 : Convert.ToInt32(dr[42]),
								SalespersonID = dr[43] == DBNull.Value ? 0 : Convert.ToInt32(dr[43]),
								SaleCommentID = dr[44] == DBNull.Value ? 0 : Convert.ToInt32(dr[44]),
								DeliveryMethodID = dr[45] == DBNull.Value ? 0 : Convert.ToInt32(dr[45]),
								ReceiptMemo = dr[46].ToString(),
								ChangeControl = dr[47].ToString(),
								OnHold = dr[48] == DBNull.Value ? '-' : Convert.ToChar(dr[48]),
								InvoiceDeliveryID = dr[49] == DBNull.Value ? '-' : Convert.ToChar(dr[49]),
								PaymentDeliveryID = dr[50] == DBNull.Value ? '-' : Convert.ToChar(dr[50]),

								Terms = new MyobTerms
								{
									LatePaymentChargePercent = dr[51] == DBNull.Value ? 0 : Convert.ToDouble(dr[51]),
									EarlyPaymentDiscountPercent = dr[52] == DBNull.Value ? 0 : Convert.ToDouble(dr[52]),
									TermsOfPaymentID = dr[53] == DBNull.Value ? null : Convert.ToString(dr[53]),
									DiscountDays = dr[54] == DBNull.Value ? 0 : Convert.ToInt32(dr[54]),
									BalanceDueDays = dr[55] == DBNull.Value ? 0 : Convert.ToInt32(dr[55]),
									ImportPaymentIsDue = dr[56] == DBNull.Value ? 0 : Convert.ToInt32(dr[56]),
									DiscountDate = dr[57] == DBNull.Value ? null : Convert.ToString(dr[57]),
									BalanceDueDate = dr[58] == DBNull.Value ? null : Convert.ToString(dr[58]),
								},
								PaymentTermsDesc = dr[59] == DBNull.Value ? null : Convert.ToString(dr[59]),
								SaleComment = dr[60] == DBNull.Value ? (dr[17] == DBNull.Value ? null : Convert.ToString(dr[17])) : Convert.ToString(dr[60]),
								//,"GSTIDNumber","FreightTaxCodeID","UseCustomerTaxCode"
								//GSTIDNumber = dr[61] == DBNull.Value ? 0 : Convert.ToInt32(dr[61]),
								//FreightTaxCodeID = dr[61] == DBNull.Value ? 0 : Convert.ToInt32(dr[62]),
								//UseCustomerTaxCode = dr[62] == DBNull.Value ? '-' : Convert.ToChar(dr[63]),
							}).ToList();

			List<AddressModel> addresslist;
			GetAbssAddressList(connectionstring, out addresslist);

			List<MyobCommentModel> commentlist = new List<MyobCommentModel>();
			commentlist = GetCommentList(connectionstring);

			foreach (var customer in customerlist)
			{
				customer.AddressList = new List<AddressModel>();
				foreach (var address in addresslist)
				{
					AddressModel addressModel = new AddressModel();
					if (customer.CardRecordID == address.CardRecordID)
					{
						addressModel.CardRecordID = address.CardRecordID;
						addressModel.Location = address.Location;
						addressModel.StreetLine1 = address.StreetLine1;
						addressModel.StreetLine2 = address.StreetLine2;
						addressModel.StreetLine3 = address.StreetLine3;
						addressModel.StreetLine4 = address.StreetLine4;
						addressModel.City = address.City;
						addressModel.State = address.State;
						addressModel.Postcode = address.Postcode;
						addressModel.Country = address.Country;
						addressModel.Phone1 = address.Phone1;
						addressModel.Phone2 = address.Phone2;
						addressModel.Phone3 = address.Phone3;
						addressModel.Fax = address.Fax;
						addressModel.Email = address.Email;
						addressModel.Salutation = address.Salutation;
						addressModel.ContactName = address.ContactName;
						addressModel.WWW = address.WWW;
						customer.AddressList.Add(addressModel);
					}
				}
				foreach (var comment in commentlist)
				{
					if (customer.SaleCommentID == comment.CommentID)
					{
						customer.SaleComment = comment.Comment;
					}
				}
			}

			return customerlist;
		}

		private static void GetAbssAddressList(string connectionstring, out List<AddressModel> addresslist)
		{
			var sql = MyobHelper.AddressListSql;
			Repository rs = new Repository();
			DataSet ds_a = rs.Query(connectionstring, sql);
			DataTable dt_a = ds_a.Tables[0];
			//Select CardRecordID,"Location", "Street", "StreetLine1", "StreetLine2", "StreetLine3", "StreetLine4", "City", "State", "Postcode", "Country", " Phone1", "Phone2", "Phone3", "Fax", "Email", "Salutation", "ContactName", "WWW" From Address
			addresslist = new List<AddressModel>();
			addresslist = (from DataRow dr in dt_a.Rows
						   select new AddressModel
						   {
							   CardRecordID = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
							   Location = dr[1] == DBNull.Value ? 0 : Convert.ToInt32(dr[1]),
							   Street = dr[2] == DBNull.Value ? "" : Convert.ToString(dr[2]),
							   StreetLine1 = dr[3] == DBNull.Value ? "" : Convert.ToString(dr[3]),
							   StreetLine2 = dr[4] == DBNull.Value ? "" : Convert.ToString(dr[4]),
							   StreetLine3 = dr[5] == DBNull.Value ? "" : Convert.ToString(dr[5]),
							   StreetLine4 = dr[6] == DBNull.Value ? "" : Convert.ToString(dr[6]),
							   City = dr[7] == DBNull.Value ? "" : Convert.ToString(dr[7]),
							   State = dr[8] == DBNull.Value ? "" : Convert.ToString(dr[8]),
							   Postcode = dr[9] == DBNull.Value ? "" : Convert.ToString(dr[9]),
							   Country = dr[10] == DBNull.Value ? "" : Convert.ToString(dr[10]),
							   Phone1 = dr[11] == DBNull.Value ? "" : Convert.ToString(dr[11]),
							   Phone2 = dr[12] == DBNull.Value ? "" : Convert.ToString(dr[12]),
							   Phone3 = dr[13] == DBNull.Value ? "" : Convert.ToString(dr[13]),
							   Fax = dr[14] == DBNull.Value ? "" : Convert.ToString(dr[14]),
							   Email = dr[15] == DBNull.Value ? "" : Convert.ToString(dr[15]),
							   Salutation = dr[16] == DBNull.Value ? "" : Convert.ToString(dr[16]),
							   ContactName = dr[17] == DBNull.Value ? "" : Convert.ToString(dr[17]),
							   WWW = dr[18] == DBNull.Value ? "" : Convert.ToString(dr[18]),
						   }
						   ).ToList();
		}

		public static List<MyobItemLocModel> GetItemLocList(string connectionstring, int apId, string sql = "")
		{
			string itemoptionsItemCodes = ModelHelper.GetItemOptionsItemCodes(ConnectionString, apId);

			if (string.IsNullOrEmpty(sql))
			{
				sql = MyobHelper.getItemLocListSql(itemoptionsItemCodes);
			}

			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];
			var rows = dt.Rows;

			/*
             * Select il.ItemID, il.QuantityOnHand, il.SellOnOrder, il.PurchaseOnOrder, l.LocationIdentification, i.ItemNumber, il.ItemLocationID From Items i Join ItemLocations il on i.ItemID = il.ItemID  Join Locations l on il.LocationID=l.LocationID Where l.IsInactive='N' And l.CanBeSold='Y'
             */
			List<MyobItemLocModel> itemloclist = new List<MyobItemLocModel>();
			itemloclist = (from DataRow dr in rows
							   //where !itemoptionsItemCodes.Contains(dr[5].ToString())
						   select new MyobItemLocModel()
						   {
							   ItemID = Convert.ToInt32(dr[0]),
							   //Convert.ToInt64(Math.Round(Convert.ToDouble(value)));
							   QuantityOnHand = Convert.ToInt32(Math.Round(Convert.ToDouble(dr[1]))),
							   SellOnOrder = int.Parse(dr[2].ToString()),
							   PurchaseOnOrder = int.Parse(dr[3].ToString()),
							   LocationCode = dr[4].ToString(),
							   ItemCode = dr[5].ToString(),
							   ItemLocationID = Convert.ToInt32(dr[6])
						   }
					 ).ToList();
			//itemloclist = itemloclist.Where(x => !itemoptionsItemCodes.Contains(x.ItemCode)).ToList();
			return itemloclist;
		}

		public static List<MyobCurrencyModel> GetCurrencyList(string connectionstring)
		{
			var sql = MyobHelper.CurrencyListSql;

			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];

			/*            CurrencyID,CurrencyCode,CurrencyName,ExchangeRate,CurrencySymbol,DigitGroupingSymbol,SymbolPosition,DecimalPlaces,NumberDigitsInGroup,DecimalPlaceSymbol,NegativeFormat,UseLeadingZero
             */
			List<MyobCurrencyModel> currencylist = new List<MyobCurrencyModel>();
			currencylist = (from DataRow dr in dt.Rows
							select new MyobCurrencyModel()
							{
								CurrencyID = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
								CurrencyCode = dr[1] == DBNull.Value ? "" : Convert.ToString(dr[1]),
								CurrencyName = dr[2].ToString(),
								ExchangeRate = dr[3] == DBNull.Value ? 0 : Convert.ToDouble(dr[3]),
								CurrencySymbol = dr[4].ToString(),
								DigitGroupingSymbol = dr[5].ToString(),
								SymbolPosition = dr[6].ToString(),
								DecimalPlaces = (short?)(dr[7] == DBNull.Value ? 0 : Convert.ToInt16(dr[7])),
								NumberDigitsInGroup = (short?)(dr[8] == DBNull.Value ? 0 : Convert.ToInt16(dr[8])),
								DecimalPlaceSymbol = dr[9].ToString(),
								NegativeFormat = dr[10].ToString(),
								UseLeadingZero = dr[11].ToString(),
							}).ToList();
			return currencylist;
		}

		public static List<MyobEmployeeModel> GetEmployeeList(string connectionstring)
		{
			var sql = MyobHelper.EmployeeListSql;

			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];

			/*
             *  public static string ExportEmployeeColList { getPG { StringBuilder sb = new StringBuilder(); sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}","EmployeeID","CardRecordID","CardIdentification","Name","LastName","FirstName","IsIndividual","IsInactive","Gender","Notes","IdentifierID","CustomField1", "CustomField2", "CustomField3"); return sb.ToString(); } }
             */
			List<MyobEmployeeModel> employeelist = new List<MyobEmployeeModel>();
			employeelist = (from DataRow dr in dt.Rows
							select new MyobEmployeeModel()
							{
								empId = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
								empCardRecordID = dr[1] == DBNull.Value ? 0 : Convert.ToInt32(dr[1]),
								empCode = dr[2].ToString(),
								empName = dr[3].ToString(),
								empLastName = dr[4].ToString(),
								empFirstName = dr[5].ToString(),
								empIsIndividual = dr[6] == DBNull.Value ? false : Convert.ToChar(dr[6]) == 'Y',
								empIsActive = dr[7] == DBNull.Value ? false : Convert.ToChar(dr[7]) == 'N',
								empGender = dr[8] == DBNull.Value ? "" : Convert.ToString(dr[8]),
								empNotes = dr[9].ToString(),
								empIdentifierID = dr[10].ToString(),
								empCustomField1 = dr[11].ToString(),
								empCustomField2 = dr[12].ToString(),
								empCustomField3 = dr[13].ToString(),
							}).ToList();

			List<AddressModel> addresslist;
			GetAbssAddressList(connectionstring, out addresslist);

			foreach (var employee in employeelist)
			{
				employee.AddressList = new List<AddressModel>();
				foreach (var address in addresslist)
				{
					AddressModel addressModel = new AddressModel();
					if (employee.empCardRecordID == address.CardRecordID)
					{
						addressModel.CardRecordID = address.CardRecordID;
						addressModel.Location = address.Location;
						addressModel.StreetLine1 = address.StreetLine1;
						addressModel.StreetLine2 = address.StreetLine2;
						addressModel.StreetLine3 = address.StreetLine3;
						addressModel.StreetLine4 = address.StreetLine4;
						addressModel.City = address.City;
						addressModel.State = address.State;
						addressModel.Postcode = address.Postcode;
						addressModel.Country = address.Country;
						addressModel.Phone1 = address.Phone1;
						addressModel.Phone2 = address.Phone2;
						addressModel.Phone3 = address.Phone3;
						addressModel.Fax = address.Fax;
						addressModel.Email = address.Email;
						addressModel.Salutation = address.Salutation;
						addressModel.ContactName = address.ContactName;
						addressModel.WWW = address.WWW;
						employee.AddressList.Add(addressModel);
					}
				}
			}

			return employeelist;
		}

		public static List<MyobSupplierModel> GetSupplierList(string connectionString)
		{
			var sql = MyobHelper.SupplierListSql;

			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionString, sql);
			DataTable dt = ds.Tables[0];
			List<MyobSupplierModel> supplierlist = new List<MyobSupplierModel>();
			supplierlist = (from DataRow dr in dt.Rows
							select new MyobSupplierModel()
							{
								supId = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
								supCardRecordID = dr[1] == DBNull.Value ? 0 : Convert.ToInt32(dr[1]),
								supCode = dr[2].ToString(),
								supName = dr[3].ToString(),
								supLastName = dr[4].ToString(),
								supFirstName = dr[5].ToString(),
								supIsIndividual = dr[6] == DBNull.Value ? false : Convert.ToChar(dr[6]) == 'Y',
								supIsActive = dr[7] == DBNull.Value ? false : Convert.ToChar(dr[7]) == 'N',
								supNotes = dr[8].ToString(),
								supIdentifierID = dr[9].ToString(),
								supCustomField1 = dr[10].ToString(),
								supCustomField2 = dr[11].ToString(),
								supCustomField3 = dr[12].ToString(),
								TaxIDNumber = dr[13] == DBNull.Value ? string.Empty : Convert.ToString(dr[13]),
								TaxCodeID = dr[14] == DBNull.Value ? 0 : Convert.ToInt32(dr[14]),
								CurrencyID = dr[15] == DBNull.Value ? 0 : Convert.ToInt32(dr[15]),
								Terms = new MyobTerms
								{
									LatePaymentChargePercent = dr[16] == DBNull.Value ? 0 : Convert.ToDouble(dr[16]),
									EarlyPaymentDiscountPercent = dr[17] == DBNull.Value ? 0 : Convert.ToDouble(dr[17]),
									TermsOfPaymentID = dr[18] == DBNull.Value ? null : Convert.ToString(dr[18]),
									DiscountDays = dr[19] == DBNull.Value ? 0 : Convert.ToInt32(dr[19]),
									BalanceDueDays = dr[20] == DBNull.Value ? 0 : Convert.ToInt32(dr[20]),
									ImportPaymentIsDue = dr[21] == DBNull.Value ? 0 : Convert.ToInt32(dr[21]),
									DiscountDate = dr[22] == DBNull.Value ? null : Convert.ToString(dr[22]),
									BalanceDueDate = dr[23] == DBNull.Value ? null : Convert.ToString(dr[23]),
								},
								PaymentTermsDesc = dr[24] == DBNull.Value ? null : Convert.ToString(dr[24]),
							}).ToList();

			List<AddressModel> addresslist;
			GetAbssAddressList(connectionString, out addresslist);

			foreach (var supplier in supplierlist)
			{
				supplier.AddressList = new List<AddressModel>();
				foreach (var address in addresslist)
				{
					AddressModel addressModel = new AddressModel();
					if (supplier.supCardRecordID == address.CardRecordID)
					{
						addressModel.CardRecordID = address.CardRecordID;
						addressModel.Location = address.Location;
						addressModel.StreetLine1 = address.StreetLine1;
						addressModel.StreetLine2 = address.StreetLine2;
						addressModel.StreetLine3 = address.StreetLine3;
						addressModel.StreetLine4 = address.StreetLine4;
						addressModel.City = address.City;
						addressModel.State = address.State;
						addressModel.Postcode = address.Postcode;
						addressModel.Country = address.Country;
						addressModel.Phone1 = address.Phone1;
						addressModel.Phone2 = address.Phone2;
						addressModel.Phone3 = address.Phone3;
						addressModel.Fax = address.Fax;
						addressModel.Email = address.Email;
						addressModel.Salutation = address.Salutation;
						addressModel.ContactName = address.ContactName;
						addressModel.WWW = address.WWW;
						supplier.AddressList.Add(addressModel);
					}
				}
			}

			return supplierlist;
		}

		public static List<TaxModel> GetTaxList()
		{
			//Select TaxCodeID, TaxCode, TaxCodeDescription, TaxPercentageRate, TaxCodeTypeID, TaxCollectedAccountID, TaxPaidAccountID, LinkedCardID From TaxCodes
			var sql = MyobHelper.TaxCodeListSql;

			Repository rs = new Repository();
			AbssConn abssConn = CommonHelper.GetAbssConn();
			DataSet ds = rs.Query(sql, abssConn);
			DataTable dt = ds.Tables[0];
			List<TaxModel> taxlist = new List<TaxModel>();
			taxlist = (from DataRow dr in dt.Rows
					   select new TaxModel()
					   {
						   TaxCodeID = Convert.ToInt32(dr[0]),
						   TaxCode = Convert.ToString(dr[1]),
						   TaxCodeDescription = Convert.ToString(dr[2]),
						   TaxPercentageRate = Convert.ToDouble(dr[3]),
						   TaxCodeTypeID = Convert.ToString(dr[4]),
						   TaxCollectedAccountID = Convert.ToInt32(dr[5]),
						   AccruedDutyAccountID = Convert.ToInt32(dr[6]),
						   LinkedCardID = Convert.ToInt32(dr[7]),
					   }
					 ).ToList();
			return taxlist;
		}

		public static string GetConnectionString(MMDbContext context, string accessType, int apId)
		{
			var comInfo = context.ComInfoes.FirstOrDefault(x => x.AccountProfileId == apId);
			string ConnectionString = string.Format(@"Driver={0};TYPE=MYOB;UID={1};PWD={2};DATABASE={3};HOST_EXE_PATH={4};NETWORK_PROTOCOL=NONET;DRIVER_COMPLETION=DRIVER_NOPROMPT;KEY={5};ACCESS_TYPE={6};", comInfo.MYOBDriver, comInfo.MYOBUID, comInfo.MYOBPASS, comInfo.MYOBDb, comInfo.MYOBExe, comInfo.MYOBKey, accessType.ToUpper());
			return ConnectionString;
		}

		public static string GetConnectionString(ComInfo comInfo, string accessType)
		{
			string ConnectionString = string.Format(@"Driver={0};TYPE=MYOB;UID={1};PWD={2};DATABASE={3};HOST_EXE_PATH={4};NETWORK_PROTOCOL=NONET;DRIVER_COMPLETION=DRIVER_NOPROMPT;KEY={5};ACCESS_TYPE={6};", comInfo.MYOBDriver, comInfo.MYOBUID, comInfo.MYOBPASS, comInfo.MYOBDb, comInfo.MYOBExe, comInfo.MYOBKey, accessType.ToUpper());
			return ConnectionString;
		}

		public static List<MyobCustomListModel> GetCustomList4Customer()
		{
			var sql = MyobHelper.CustomList4CustomerSql;
			Repository rs = new Repository();
			AbssConn abssConn = CommonHelper.GetAbssConn();
			DataSet ds_a = rs.Query(sql, abssConn);
			DataTable dt_a = ds_a.Tables[0];
			var customlist = new List<MyobCustomListModel>();
			customlist = (from DataRow dr in dt_a.Rows
						  select new MyobCustomListModel
						  {
							  CustomListID = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
							  CustomListText = dr[1] == DBNull.Value ? "" : Convert.ToString(dr[1]),
							  CustomListNumber = dr[2] == DBNull.Value ? 0 : Convert.ToInt32(dr[2]),
							  CustomListName = dr[3] == DBNull.Value ? "" : Convert.ToString(dr[3]),
							  ChangeControl = dr[4] == DBNull.Value ? "" : Convert.ToString(dr[4]),
							  CustomListType = dr[5] == DBNull.Value ? "" : Convert.ToString(dr[5])
						  }
						   ).ToList();
			return customlist;
		}

		public static List<MyobJobModel> GetJobList(string connectionstring)
		{
			var sql = MyobHelper.JobListSql;

			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];

			List<MyobJobModel> joblist = new List<MyobJobModel>();
			joblist = (from DataRow dr in dt.Rows
					   select new MyobJobModel()
					   {
						   JobID = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
						   ParentJobID = dr[1] == DBNull.Value ? 0 : Convert.ToInt32(dr[1]),
						   IsInactive = dr[2] == DBNull.Value ? false : Convert.ToChar(dr[2]) == 'Y',
						   JobName = dr[3] == DBNull.Value ? "" : Convert.ToString(dr[3]),
						   JobNumber = dr[4] == DBNull.Value ? "" : Convert.ToString(dr[4]),
						   IsHeader = dr[5] == DBNull.Value ? false : Convert.ToChar(dr[5]) == 'Y',
						   JobLevel = dr[6] == DBNull.Value ? (short)0 : Convert.ToInt16(dr[6]),
						   IsTrackingReimburseable = dr[7] == DBNull.Value ? false : Convert.ToChar(dr[7]) == 'Y',
						   JobDescription = dr[8] == DBNull.Value ? "" : Convert.ToString(dr[8]),
						   ContactName = dr[9] == DBNull.Value ? "" : Convert.ToString(dr[9]),
						   Manager = dr[10] == DBNull.Value ? "" : Convert.ToString(dr[10]),
						   PercentCompleted = dr[11] == DBNull.Value ? 0 : Convert.ToDecimal(dr[11]),
						   StartDate = dr[12] == DBNull.Value ? null : CommonHelper.GetDateFrmString4SQL(Convert.ToString(dr[12])),
						   FinishDate = dr[13] == DBNull.Value ? null : CommonHelper.GetDateFrmString4SQL(Convert.ToString(dr[13])),
						   CustomerID = dr[14] == DBNull.Value ? 0 : Convert.ToInt32(dr[14]),
					   }).ToList();
			return joblist;
		}

		public static List<MyobLocationModel> GetLocationList(string connectionstring)
		{
			var sql = MyobHelper.LocationListSql;

			Repository rs = new Repository();
			DataSet ds = rs.Query(connectionstring, sql);
			DataTable dt = ds.Tables[0];

			// LocationID, LocationName,LocationIdentification,IsInactive
			List<MyobLocationModel> locationlist = new List<MyobLocationModel>();
			locationlist = (from DataRow dr in dt.Rows
							select new MyobLocationModel()
							{
								LocationID = dr[0] == DBNull.Value ? 0 : Convert.ToInt32(dr[0]),
								LocationIdentification = dr[2] == DBNull.Value ? "" : Convert.ToString(dr[2]),
								IsInactive = dr[3] == DBNull.Value ? false : Convert.ToChar(dr[3]) == 'Y',
								LocationName = dr[1] == DBNull.Value ? "" : Convert.ToString(dr[1]),

							}).ToList();
			return locationlist;
		}

		public static List<QuotationView> GetQuotationList(AbssConn abssConn)
		{
			string sql = @"SELECT main.InvoiceNumber as 'Order No',main.InvoiceDate as Date,c.Name as CustomerName,i.ItemNumber,i.ItemName,e.Name as 'SalesPersonName',l.Quantity as Qty,l.TaxInclusiveUnitPrice,main.SalesPersonID,c.CardRecordID as CustomerID FROM Sales main join ItemSaleLines l on main.SaleID = l.SaleID join Items i on l.ItemID = i.ItemID join Customers c on main.CardRecordID = c.CardRecordID left join Employees e on main.SalespersonID = e.EmployeeID where main.InvoiceStatusID = 'Q';";
			Repository repos = new Repository();
			DataSet ds = repos.Query(sql, abssConn);
			DataTable dt = ds.Tables[0];
			List<QuotationView> Quotations = new List<QuotationView>();
			Quotations = (from DataRow dr in dt.Rows
						  select new QuotationView()
						  {
							  InvoiceNumber = dr[0].ToString(),
							  InvoiceDate = DateTime.Parse(dr[1].ToString()),
							  CustomerName = dr[2].ToString(),
							  ItemNumber = dr[3].ToString(),
							  ItemName = dr[4].ToString(),
							  SalesPersonName = dr[5].ToString(),
							  Qty = decimal.Parse(dr[6].ToString()),
							  TaxInclusiveUnitPrice = decimal.Parse(dr[7].ToString()),
							  CurrencySymbol = "$",
							  SalesPersonID = int.Parse(dr[8].ToString()),
							  CustomerID = int.Parse(dr[9].ToString())
						  }
					 ).ToList();
			return Quotations;
		}

		public static List<AccountReceivableView> GetARList(AbssConn abssConn)
		{
			string sql = "SELECT c.Name as 'CustomerName', main.InvoiceDate,main.InvoiceNumber,main.OutstandingBalance,main.TotalDiscounts as Discount,main.TermsID,main.SalesPersonID FROM Sales as main join Customers c on main.CardRecordID = c.CardRecordID where main.InvoiceStatusID='O'";
			Repository rs = new Repository();
			DataSet ds = rs.Query(sql, abssConn);
			DataTable dt = ds.Tables[0];
			List<AccountReceivableView> AccountReceivables = new List<AccountReceivableView>();
			AccountReceivables = (from DataRow dr in dt.Rows
								  select new AccountReceivableView()
								  {
									  CustomerName = dr[0].ToString(),
									  CurrencySymbol = "$",
									  InvoiceDate = Convert.ToDateTime(dr[1]),
									  InvoiceNumber = dr[2].ToString(),
									  Amount = Convert.ToDecimal(dr[3]),
									  Discount = Convert.ToDecimal(dr[4]),
									  TermsOfPayment = new MMCommonLib.CommonModels.TermsOfPayment
                                      {
										  TermsID = Convert.ToInt32(dr[5]),
									  },
									  SalesPerson = new SalesPerson
									  {
										  ID = Convert.ToInt32(dr[6])
									  }
								  }
					 ).ToList();
			return AccountReceivables;
		}
	}
}