using MMDAL;
using MMLib.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CommonLib.Models;
using CommonLib.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;
using MMLib.Models.Item;
using System.Data.Entity.Validation;
using System.Text;
using MMLib.Models.POS.MYOB;
using MMLib.Models.Purchase;
using MMLib.Models.Supplier;
using MMCommonLib.Models;
using MMCommonLib.CommonModels;
using System.Data.Entity;
using CommonLib.Models.MYOB;
using TaxType = CommonLib.Models.TaxType;
using Dapper;
using Resources = CommonLib.App_GlobalResources;
using System.Net.Mail;
using System.Net;
using MMCommonLib.BaseModels;
using MMLib.Models.MYOB;
using System.IO;
using MMLib.Models.Journal;
using MMLib.Models.User;
using CommonLib.BaseModels;
using CommonLib.App_GlobalResources;
using System.Collections;

namespace MMLib.Helpers
{
	public static class ModelHelper
	{
		public const string sqlfields4Journal = "JournalNumber,JournalDate,Memo,Inclusive,AccountNumber,DebitExTaxAmount,DebitIncTaxAmount,CreditExTaxAmount,CreditIncTaxAmount,Job,AllocationMemo";

		public const string sqlfields4Sales = "InvoiceNumber,SaleDate,AmountPaid,ItemNumber,Quantity,Price,Discount,SaleStatus,Location,CardID,PaymentMethod,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount,PercentMonthlyCharge,DeliveryStatus,CustomersNumber,JobName,SalespersonFirstName,SalespersonLastName,Memo,TaxCode,CurrencyCode,ExchangeRate,AddressLine1,AddressLine2,AddressLine3,AddressLine4";

		/// <summary>
		/// for Insert Item Services
		/// Important: Amount is mandatory!!!!
		/// </summary>
		public const string sqlfields4Deposit = "CardID,InvoiceNumber,SaleDate,AccountNumber,Amount,SaleStatus,DeliveryStatus,Memo,SalesPersonLastName,Description,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount,PercentMonthlyCharge";


		private static string DefaultConnection { get { return ConfigurationManager.AppSettings["DefaultConnection"]; } }

		private static ComInfo ComInfo { get { return HttpContext.Current.Session["ComInfo"] as ComInfo; } }
		//private static List<string> Shops;
		private static int AccountProfileId { get { return ComInfo.AccountProfileId; } }
		private static int apId { get { return AccountProfileId; } }
		private static int CompanyId { get { return 1; } }
		private static bool NonABSS { get { return ComInfo.DefaultCheckoutPortal == "nonabss"; } }
		private static bool ApprovalMode { get { return (bool)ComInfo.ApprovalMode; } }

		static bool EnableReviewUrl { get { return int.Parse(ConfigurationManager.AppSettings["EnableReviewUrl"]) == 1; } }

		public static List<string> ShopNames;

		public static List<Country> PopulateDefaultCountries()
		{
			return new List<Country> {
				new Country
			{
				CountryID = 1,
				CountryName = Resource.HongKong
			},
				new Country
			{
				CountryID = 2,
				CountryName = Resource.Macao
			},
				new Country
			{
				CountryID = 3,
				CountryName = Resource.China
			},
			};
		}
		public static List<ItemModel> HandleStockQty(MMDbContext context, Dictionary<string, Dictionary<string, int>> dicItemLocQty, bool forsales)
		{
			SessUser user = HttpContext.Current.Session["User"] as SessUser;
			List<ItemModel> OutOfStockSalesLns = new List<ItemModel>();
			foreach (var itemcode in dicItemLocQty.Keys)
			{
				foreach (var location in dicItemLocQty[itemcode].Keys)
				{
					MyobLocStock stock = context.MyobLocStocks.FirstOrDefault(x => x.lstItemCode == itemcode && x.lstStockLoc == location && x.AccountProfileId == apId);
					if (stock != null)
					{
						if (forsales)
							stock.lstQuantityAvailable -= dicItemLocQty[itemcode][location];
						else
							stock.lstQuantityAvailable += dicItemLocQty[itemcode][location];

						stock.lstModifyBy = user.UserName;
						stock.lstModifyTime = DateTime.Now;
						if (stock.lstQuantityAvailable <= 0)
						{
							var outofstock = context.MyobItems.FirstOrDefault(x => x.itmCode == itemcode && x.AccountProfileId == apId);

							OutOfStockSalesLns.Add(new ItemModel
							{
								lstItemCode = itemcode,
								itmUseDesc = outofstock.itmUseDesc,
								itmName = outofstock.itmName,
								itmDesc = outofstock.itmDesc,
							});
						}
					}
				}
			}
			context.SaveChanges();
			return OutOfStockSalesLns;
		}


		public static void SaveAttributeVals(MMDbContext context, string attributeId, string av, long contactId, int apId, bool attrmode = false)
		{
			if (attrmode)
			{
				#region Step 1:remove current records first:
				var attributevalues = context.CustomAttributeValues.Where(x => x.attrId == attributeId);
				context.CustomAttributeValues.RemoveRange(attributevalues);
				context.SaveChanges();
				#endregion

				#region Step 2:add records:
				List<CustomAttributeValue> attributeValues = new List<CustomAttributeValue>();
				//foreach (var av in avs)
				//{
				CustomAttributeValue attributeValue = new CustomAttributeValue
				{
					attrId = attributeId,
					attrValue = av,
					ContactID = (int)contactId,
					AccountProfileId = apId
				};
				attributeValues.Add(attributeValue);
				//}
				context.CustomAttributeValues.AddRange(attributeValues);
				context.SaveChanges();
				#endregion
			}
			else
			{
				#region Step 1:remove current records first:
				var attributevalues = context.CustomAttributeValues.Where(x => x.attrId == attributeId);
				context.CustomAttributeValues.RemoveRange(attributevalues);
				context.SaveChanges();
				#endregion

				#region Step 2:add records:
				List<CustomAttributeValue> attributeValues = new List<CustomAttributeValue>();
				//foreach (var av in avs)
				//{
				CustomAttributeValue attributeValue = new CustomAttributeValue
				{
					attrId = attributeId,
					attrValue = av,
					ContactID = (int)contactId,
					AccountProfileId = apId
				};
				attributeValues.Add(attributeValue);
				//}
				context.CustomAttributeValues.AddRange(attributeValues);
				context.SaveChanges();
				#endregion
			}
		}

		public static void HandleViewFileList(HashSet<string> uploadfilelist, int apId, int Id, ref List<string> ImgList, ref List<string> FileList)
		{
			if (uploadfilelist != null && uploadfilelist.Count > 0)
			{
				foreach (var file in uploadfilelist)
				{
					var _file = Path.Combine(apId.ToString(), Id.ToString(), file);
					if (CommonHelper.ImageExtensions.Contains(Path.GetExtension(file).ToUpperInvariant()))
					{
						_file = $"<a href='{_file}' target='_blank'><img src='{_file}'/></a>";
						ImgList.Add(_file);
					}
					else
					{
						_file = $"<a class='btn btn-success' href='{_file}' target='_blank'>{file}</a>";
						FileList.Add(_file);
					}
				}
			}
		}
		public static void HandleViewFile(string uploadfilename, int apId, string filecode, ref List<string> ImgList, ref List<string> FileList)
		{
			if (!string.IsNullOrEmpty(uploadfilename))
			{
				var fileList = uploadfilename.Split(',');
				foreach (var file in fileList)
				{
					var _file = Path.Combine(apId.ToString(), filecode, file);
					if (CommonHelper.ImageExtensions.Contains(Path.GetExtension(file).ToUpperInvariant()))
					{
						_file = $"<a href='{_file}' target='_blank'><img src='{UriHelper.GetBaseUrl()}/{_file}'/></a>";
						ImgList.Add(_file);
					}
					else
					{
						_file = $"<a class='' href='{_file}' target='_blank'><img src='{UriHelper.GetBaseUrl()}/Images/pdf.jpg' class='thumbnail'/>{Path.GetFileName(file)}</a>";
						FileList.Add(_file);
					}
				}
			}
		}



		public static HashSet<ItemCategoryModel> GetCategoryList(SqlConnection connection, int SortCol = 3, string SortOrder = "desc", string Keyword = null)
		{
			var catlist = connection.Query<ItemCategoryModel>(@"EXEC dbo.GetCategoryList @apId=@apId,@sortcol=@sortcol,@sortorder=@sortorder,@keyword=@keyword", new { apId = AccountProfileId, sortcol = SortCol, sortorder = SortOrder, keyword = Keyword }).ToHashSet();
			GetCatDisplayName(ref catlist);
			return catlist;
		}
		public static List<PromotionModel> GetPromotionList(SqlConnection connection, int SortCol = 3, string CurrentSortOrder = "desc", string Keyword = null)
		{
			var sortColOrder = string.Concat(SortCol, " ", CurrentSortOrder);
			return connection.Query<PromotionModel>(@"EXEC dbo.GetPromotionList @apId=@apId, @sortColOrder=@sortColOrder, @keyword=@keyword", new { apId, sortColOrder, keyword = Keyword }).ToList();
		}
		public static List<PromotionModel> GetPromotionList4Period(SqlConnection connection, int SortCol = 3, string CurrentSortOrder = "desc", string Keyword = null)
		{
			var sortColOrder = string.Concat(SortCol, " ", CurrentSortOrder);
			var promotionList = connection.Query<PromotionModel>(@"EXEC dbo.GetPromotionList4Period @sortColOrder=@sortColOrder, @keyword=@keyword", new { sortColOrder, keyword = Keyword }).ToList();
			return promotionList.Where(x => !x.IsObsolete).ToList();
		}
		public static List<PromotionModel> GetPromotionList4Qty(SqlConnection connection, int SortCol = 3, string CurrentSortOrder = "desc", string Keyword = null)
		{
			var sortColOrder = string.Concat(SortCol, " ", CurrentSortOrder);
			return connection.Query<PromotionModel>(@"EXEC dbo.GetPromotionList4Qty @sortColOrder=@sortColOrder, @keyword=@keyword", new { sortColOrder, keyword = Keyword }).ToList();
		}
		public static void GetCategoryList(SqlConnection connection, ref HashSet<ItemCategoryModel> CategoryList)
		{
			CategoryList = connection.Query<ItemCategoryModel>(@"EXEC dbo.GetCategoryList @apId=@apId", new { apId }).ToHashSet();
			GetCatDisplayName(ref CategoryList);
		}

		private static void GetCatDisplayName(ref HashSet<ItemCategoryModel> CategoryList)
		{
			foreach (var cat in CategoryList)
			{
				switch (CultureHelper.CurrentCulture)
				{
					case 2:
						cat.NameDisplay = cat.catName;
						cat.DescriptionDisplay = cat.catName;
						break;
					case 1:
						cat.NameDisplay = cat.catNameSC;
						cat.DescriptionDisplay = cat.catNameSC;
						break;
					default:
					case 0:
						cat.NameDisplay = cat.catNameTC;
						cat.DescriptionDisplay = cat.catNameTC;
						break;
				}
			}
		}

		public static void GetMyobItemPrices(MMDbContext context, string itemcode, ref ItemModel Item)
		{
			List<MyobItemPrice> itemPrices = context.MyobItemPrices.Where(x => x.ItemCode == itemcode && x.AccountProfileId == ComInfo.AccountProfileId).ToList();
			if (itemPrices != null && itemPrices.Count > 0)
			{
				foreach (var itemprice in itemPrices)
				{
					if (Item.itmItemID == itemprice.ItemID)
					{
						if (itemprice.PriceLevel == "A" && itemprice.QuantityBreak == 1)
						{
							Item.PLA = itemprice.SellingPrice;
							Item.PLs["PLA"] = itemprice.SellingPrice;
						}
						if (itemprice.PriceLevel == "B" && itemprice.QuantityBreak == 1)
						{
							Item.PLB = itemprice.SellingPrice;
							Item.PLs["PLB"] = itemprice.SellingPrice;
						}
						if (itemprice.PriceLevel == "C" && itemprice.QuantityBreak == 1)
						{
							Item.PLC = itemprice.SellingPrice;
							Item.PLs["PLC"] = itemprice.SellingPrice;
						}
						if (itemprice.PriceLevel == "D" && itemprice.QuantityBreak == 1)
						{
							Item.PLD = itemprice.SellingPrice;
							Item.PLs["PLD"] = itemprice.SellingPrice;
						}
						if (itemprice.PriceLevel == "E" && itemprice.QuantityBreak == 1)
						{
							Item.PLE = itemprice.SellingPrice;
							Item.PLs["PLE"] = itemprice.SellingPrice;
						}
						if (itemprice.PriceLevel == "F" && itemprice.QuantityBreak == 1)
						{
							Item.PLF = itemprice.SellingPrice;
							Item.PLs["PLF"] = itemprice.SellingPrice;
						}
					}
				}
			}
		}
		public static void GetLocStock4MyobItem(MMDbContext context, string itemcode, List<string> shops, ref ItemModel Item)
		{
			int totalqty = 0;
			foreach (var shop in shops)
			{
				MyobLocStock locStock = context.MyobLocStocks.FirstOrDefault(x => x.lstItemCode == itemcode && x.AccountProfileId == ComInfo.AccountProfileId && x.lstStockLoc == shop.Trim());
				if (locStock != null)
				{
					Item.DicLocQty[shop.Trim()] = locStock.lstQuantityAvailable == null ? 0 : (int)locStock.lstQuantityAvailable;
				}
				else
				{
					Item.DicLocQty[shop.Trim()] = 0;
				}
				totalqty += Item.DicLocQty[shop.Trim()];
			}
			Item.TotalBaseStockQty = totalqty;
		}

		public static List<ItemAttributeModel> GetItemAttrList(MMDbContext context, string itemCode)
		{
			List<ItemAttributeModel> attrs = new List<ItemAttributeModel>();
			var attrlist = context.ItemAttributes.Where(x => x.itmCode == itemCode).ToList();
			if (attrlist.Count > 0)
			{
				foreach (var attr in attrlist)
				{
					attrs.Add(new ItemAttributeModel
					{
						Id = attr.Id,
						itmCode = attr.itmCode,
						iaName = attr.iaName,
						iaValue = attr.iaValue,
						iaShowOnSalesPage = attr.iaShowOnSalesPage,
						iaUsed4Variation = attr.iaUsed4Variation,
					});
				}
			}
			return attrs;
		}
		public static void SaveItemAttributes(MMDbContext context, List<ItemAttributeModel> attrlist, MyobItem myobItem)
		{
			var itemcode = myobItem.itmCode;
			List<ItemAttribute> attrs = context.ItemAttributes.Where(x => x.itmCode == itemcode && x.AccountProfileId == AccountProfileId).ToList();
			var _attr = attrlist.First();
			/* MUST NOT REMOVE ATTRIBUTES (as item variations depend on iaId)!!! ONLY ADD/UPDATE IS ALLOWED!!! */
			if (attrs != null && attrs.Count > 0)
			{
				foreach (var attr in attrlist)
				{
					var currattr = context.ItemAttributes.Find(attr.Id);
					currattr.iaName = attr.iaName;
					currattr.iaValue = attr.iaValue;
					currattr.ModifyTime = DateTime.Now;
				}
			}
			else
			{
				attrs = new List<ItemAttribute>();
				foreach (var attr in attrlist)
				{
					attrs.Add(new ItemAttribute
					{
						itmCode = attr.itmCode.Trim(),
						iaName = attr.iaName.Trim(),
						iaValue = attr.iaValue,
						iaShowOnSalesPage = attr.iaShowOnSalesPage,
						iaUsed4Variation = attr.iaUsed4Variation,
						AccountProfileId = AccountProfileId,
						CreateTime = DateTime.Now,
						ModifyTime = DateTime.Now
					});
				}
				context.ItemAttributes.AddRange(attrs);
			}
			context.SaveChanges();

			#region remove current IVs first, if any...
			#region get current IVs, if any...
			List<string> comboIvIds = new List<string>();
			string comboIvId = string.Empty;
			foreach (var attr in attrs)
			{
				var valarr = attr.iaValue.Split('|');
				foreach (var val in valarr)
				{
					var _iv = context.ItemVariations.FirstOrDefault(x => x.itmCode == itemcode && x.comboIvId == null && x.iaName == attr.iaName && x.iaValue == val && x.AccountProfileId == AccountProfileId);
					if (_iv != null)
					{
						comboIvIds.Add(_iv.Id.ToString());
					}
				}
			}
			comboIvId = string.Join("|", comboIvIds);
			var currentIV = context.ItemVariations.FirstOrDefault(x => x.itmCode == itemcode && x.comboIvId == comboIvId && x.AccountProfileId == AccountProfileId);
			#endregion
			if (currentIV != null)
			{
				context.ItemVariations.Remove(currentIV);
				context.SaveChanges();
			}
			#endregion

			#region add IVs:
			List<ItemVariation> variations = new List<ItemVariation>();
			foreach (var attr in attrs)
			{
				var valarr = attr.iaValue.Split('|');
				foreach (var val in valarr)
				{
					variations.Add(new ItemVariation
					{
						iaId = attr.Id,
						iaName = attr.iaName.Trim(),
						iaValue = val,
						itemID = myobItem.itmItemID,
						AccountProfileId = AccountProfileId,
						itmCode = myobItem.itmCode,
						CreateTime = DateTime.Now,
						ModifyTime = DateTime.Now,
					});
				}
			}
			context.ItemVariations.AddRange(variations);
			context.SaveChanges();
			#endregion

		}


		public static void getReceiptData2(MMDbContext context, int lang, string shop, string device, string devicecode, CentralDataModel model, string receiptno)
		{
			model.snlist = (from s in context.SerialNoes
							where s.snoIsActive == true && s.snoRtlSalesDvc == devicecode && s.snoRtlSalesLoc == shop && s.snoRtlSalesCode == receiptno
							&& s.snoStatus == "REDEEM"
							select new SerialNoView
							{
								snoCode = s.snoCode,
								snoStatus = s.snoStatus,
								snoItemCode = s.snoItemCode,
								snoRtlSalesCode = s.snoRtlSalesCode,
								snoRtlSalesSeq = s.snoRtlSalesSeq,
								snoRtlSalesDate = s.snoRtlSalesDate,
								snoBatchCode = s.snoBatchCode,
								snoValidThru = s.snoValidThru
							}
										  ).ToList();


			model.companyinfo = (from c in context.ComInfoes
								 select new ComInfoView
								 {
									 comName = c.comName,
									 comEmail = c.comEmail,
									 comAddress1 = c.comAddress1,
									 comAddress2 = c.comAddress2
								 }
					   ).FirstOrDefault();

			model.receipt = new ReceiptViewModel();
			switch (lang)
			{
				case 1:
					model.receipt = (from r in context.Receipts
									 where r.deviceCode == device && r.shopCode == shop
									 select new ReceiptViewModel
									 {
										 HeaderTitle = r.HeaderTitleCN,
										 HeaderMessage = r.HeaderMessageCN,
										 FooterTitle1 = r.FooterTitle1CN,
										 FooterTitle2 = r.FooterTitle2CN,
										 FooterTitle3 = r.FooterTitle3CN,
										 FooterMessage = r.FooterMessageCN
									 }
						   ).FirstOrDefault();
					break;
				case 2:
					model.receipt = (from r in context.Receipts
									 where r.deviceCode == device && r.shopCode == shop
									 select new ReceiptViewModel
									 {
										 HeaderTitle = r.HeaderTitleEng,
										 HeaderMessage = r.HeaderMessageEng,
										 FooterTitle1 = r.FooterTitle1Eng,
										 FooterTitle2 = r.FooterTitle2Eng,
										 FooterTitle3 = r.FooterTitle3Eng,
										 FooterMessage = r.FooterMessageEng
									 }
						   ).FirstOrDefault();
					break;
				default:
				case 0:
					model.receipt = (from r in context.Receipts
									 where r.deviceCode == device && r.shopCode == shop
									 select new ReceiptViewModel
									 {
										 HeaderTitle = r.HeaderTitle,
										 HeaderMessage = r.HeaderMessage,
										 FooterTitle1 = r.FooterTitle1,
										 FooterTitle2 = r.FooterTitle2,
										 FooterTitle3 = r.FooterTitle3,
										 FooterMessage = r.FooterMessage
									 }
						   ).FirstOrDefault();
					break;
			}
		}


		public static void SaveSuppliersFrmCentral(MMDbContext context, int apId, ComInfo comInfo = null)
		{
			if (comInfo == null)
				comInfo = context.ComInfoes.FirstOrDefault(x => x.AccountProfileId == apId);
			string ConnectionString = string.Format(@"Driver={0};TYPE=MYOB;UID={1};PWD={2};DATABASE={3};HOST_EXE_PATH={4};NETWORK_PROTOCOL=NONET;DRIVER_COMPLETION=DRIVER_NOPROMPT;KEY={5};ACCESS_TYPE=READ;", comInfo.MYOBDriver, comInfo.MYOBUID, comInfo.MYOBPASS, comInfo.MYOBDb, comInfo.MYOBExe, comInfo.MYOBKey);
			try
			{
				DateTime dateTime = DateTime.Now;
				List<MyobSupplierModel> suplist = MYOBHelper.GetSupplierList(ConnectionString);
				/* remove current records first: */
				List<Supplier> suppliers = context.Suppliers.Where(x => x.AccountProfileId == apId && (bool)x.supCheckout).ToList();
				context.Suppliers.RemoveRange(suppliers);
				context.SaveChanges();
				/*********************************/

				List<Supplier> newsuppliers = new List<Supplier>();
				Dictionary<string, int> DicTermsOfPayments = new Dictionary<string, int>();
				var termsofpayments = context.MyobTermsOfPayments.ToList();
				foreach (var term in termsofpayments)
				{
					DicTermsOfPayments[term.TermsOfPaymentID.Trim()] = term.MyobID;
				}
				foreach (var supplier in suplist)
				{
					Supplier msupplier = new Supplier();
					msupplier.supFirstName = supplier.supFirstName;
					msupplier.supId = supplier.supId;
					msupplier.supIsIndividual = supplier.supIsIndividual;
					msupplier.supIsActive = supplier.supIsActive;
					msupplier.supCode = supplier.supCode.StartsWith("*") ? supplier.supCardRecordID.ToString() : supplier.supCode;
					//msupplier.supName = GetForeignCurrencyCardName(supplier.supCode, supplier.supName);
					msupplier.supName = supplier.supName;
					msupplier.supFirstName = supplier.supFirstName;
					msupplier.supLastName = supplier.supLastName;
					msupplier.supIdentifierID = supplier.supIdentifierID;
					msupplier.supCustomField1 = supplier.supCustomField1;
					msupplier.supCustomField2 = supplier.supCustomField2;
					msupplier.supCustomField3 = supplier.supCustomField3;
					msupplier.CurrencyID = supplier.CurrencyID;
					msupplier.TaxIDNumber = supplier.TaxIDNumber;
					msupplier.TaxCodeID = supplier.TaxCodeID;
					msupplier.CreateTime = dateTime;
					msupplier.ModifyTime = dateTime;
					msupplier.AccountProfileId = apId;
					msupplier.supAbss = true;
					msupplier.supCheckout = true;

					msupplier.LatePaymentChargePercent = supplier.Terms.LatePaymentChargePercent == null ? 0 : Convert.ToDecimal(supplier.Terms.LatePaymentChargePercent);
					msupplier.EarlyPaymentDiscountPercent = supplier.Terms.EarlyPaymentDiscountPercent == null ? 0 : Convert.ToDecimal(supplier.Terms.EarlyPaymentDiscountPercent);
					msupplier.TermsOfPaymentID = supplier.Terms.TermsOfPaymentID == null ? null : supplier.Terms.TermsOfPaymentID.Trim();

					if (DicTermsOfPayments.ContainsKey(msupplier.TermsOfPaymentID))
						msupplier.PaymentIsDue = DicTermsOfPayments[msupplier.TermsOfPaymentID];

					msupplier.DiscountDays = supplier.Terms.DiscountDays == null ? 0 : supplier.Terms.DiscountDays;
					msupplier.BalanceDueDays = supplier.Terms.BalanceDueDays == null ? 0 : supplier.Terms.BalanceDueDays;
					msupplier.ImportPaymentIsDue = supplier.Terms.ImportPaymentIsDue == null ? 0 : supplier.Terms.ImportPaymentIsDue;
					msupplier.DiscountDate = supplier.Terms.DiscountDate == null ? null : supplier.Terms.DiscountDate;
					msupplier.BalanceDueDate = supplier.Terms.BalanceDueDate == null ? null : supplier.Terms.BalanceDueDate;
					msupplier.PaymentTermsDesc = supplier.PaymentTermsDesc == null ? null : supplier.PaymentTermsDesc;

					var address = supplier.AddressList.FirstOrDefault();
					if (address != null)
					{
						msupplier.supEmail = address.Email;
						msupplier.supPhone = address.Phone1;
						msupplier.supAddrCountry = address.Country;
						msupplier.supAddrCity = address.City;
						msupplier.supAddrStreetLine1 = address.StreetLine1;
						msupplier.supAddrStreetLine2 = address.StreetLine2;
						msupplier.supAddrStreetLine3 = address.StreetLine3;
						msupplier.supAddrStreetLine4 = address.StreetLine4;
					}

					newsuppliers.Add(msupplier);
				}
				context.Suppliers.AddRange(newsuppliers);
				WriteLog(context, "Import Supplier data from Central done", "ImportFrmCentral");
				context.SaveChanges();
			}
			catch (DbEntityValidationException e)
			{

				StringBuilder sb = new StringBuilder();
				foreach (var eve in e.EntityValidationErrors)
				{
					sb.AppendFormat("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
						eve.Entry.Entity.GetType().Name, eve.Entry.State);
					foreach (var ve in eve.ValidationErrors)
					{
						sb.AppendFormat("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"",
		ve.PropertyName,
		eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName),
		ve.ErrorMessage);
					}
				}
				WriteLog(context, string.Format("Import Supplier data from Central failed:{0}", sb.ToString()), "ExportFrmCentral");
				context.SaveChanges();
			}
		}
		public static void SaveEmployeesFrmCentral(int apId, MMDbContext context, string ConnectionString, SessUser curruser = null, int managerId = 0)
		{
			DateTime dateTime = DateTime.Now;
			List<MyobEmployeeModel> emplist = MYOBHelper.GetEmployeeList(ConnectionString);
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					/* remove current records first: */
					List<MyobEmployee> employees = context.MyobEmployees.Where(x => x.AccountProfileId == apId).ToList();
					context.MyobEmployees.RemoveRange(employees);
					context.SaveChanges();
					/*********************************/

					List<SysUser> users = context.SysUsers.Where(x => x.AccountProfileId == apId && x.surIsAbss).ToList();
					context.SysUsers.RemoveRange(users);
					context.SaveChanges();
					/*
                     *  public static string ExportEmployeeColList { get { StringBuilder sb = new StringBuilder(); sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}","EmployeeID","CardRecordID","CardIdentification","Name","LastName","FirstName","IsIndividual","IsInactive","Gender","Notes","IdentifierID","CustomField1", "CustomField2", "CustomField3"); return sb.ToString(); } }
                     */
					List<MyobEmployee> newemployees = new List<MyobEmployee>();
					List<SysUser> newusers = new List<SysUser>();
					int posAdminId = curruser == null ? managerId : GetPosAdminID(curruser.Device);
					string dvccode = curruser == null ? "" : curruser.dvcCode;
					string shopcode = curruser == null ? "" : curruser.shopCode;

					if (curruser == null)
					{
						var user = context.SysUsers.FirstOrDefault(x => x.AccountProfileId == apId && x.surUID == managerId);
						dvccode = user.dvcCode;
						shopcode = user.shopCode;

					}
					string usercode = "";

					foreach (var employee in emplist)
					{
						MyobEmployee memployee = new MyobEmployee();
						memployee.empFirstName = employee.empFirstName;
						memployee.empId = employee.empId;
						memployee.empIsIndividual = employee.empIsIndividual;
						memployee.empIsActive = employee.empIsActive;
						memployee.empCode = employee.empCode.StartsWith("*") ? employee.empCardRecordID.ToString() : employee.empCode;
						memployee.empName = employee.empName;
						memployee.empFirstName = employee.empFirstName;
						memployee.empLastName = employee.empLastName;
						memployee.empGender = employee.empGender;
						memployee.empIdentifierID = employee.empIdentifierID;
						memployee.empCustomField1 = employee.empCustomField1;
						memployee.empCustomField2 = employee.empCustomField2;
						memployee.empCustomField3 = employee.empCustomField3;
						memployee.empCreateTime = dateTime;
						memployee.empModifyTime = dateTime;
						memployee.AccountProfileId = apId;

						if (memployee.empCode.ToLower() != "admin")
						{
							newemployees.Add(memployee);
						}

						if (memployee.empCode.ToLower() != "admin")
						{
							usercode = ModelHelper.GetUserCode(context);
							AddressModel address = new AddressModel();
							string email = "";
							if (employee.AddressList.Count > 0)
							{
								address = employee.AddressList.FirstOrDefault();
								email = address.Email;
							}
							var newId = context.SysUsers.Max(x => x.surUID) + 1;
							SysUser user = new SysUser
							{
								surUID = newId,
								surIsActive = employee.empIsActive,
								Password = "sBcipqs4nH2+Kh658Y4ZU9kCUeCq8Q+H",
								UserCode = usercode,
								AbssCardID = employee.empCode.StartsWith("*") ? employee.empCardRecordID.ToString() : employee.empCode,
								UserName = employee.empName,
								FirstName = employee.empFirstName,
								LastName = employee.empLastName,
								dvcCode = dvccode,
								shopCode = shopcode,
								ManagerId = posAdminId,
								surScope = "pos",
								AccountProfileId = apId,
								UserRole = "SalesPerson",
								Email = email,
								surIsAbss = true,
								surLicensed = false,
								surCreateTime = dateTime,
								surModifyTime = dateTime,
							};
							context.SysUsers.Add(user);
							context.SaveChanges();
							newusers.Add(user);
						}
					}
					context.MyobEmployees.AddRange(newemployees);
					//context.SysUsers.AddRange(newusers);
					context.SaveChanges();
					ModelHelper.WriteLog(context, "Import Employee data from Central done", "ImportFrmCentral");

					var salesroldId = context.SysRoles.FirstOrDefault(x => x.rlCode.ToLower() == "salesperson").Id;
					int[] adminrolIds = new int[] { 2, 3, 5, 6, 7 };
					List<UserRole> newuserroles = new List<UserRole>();
					foreach (var newuser in newusers)
					{
						if (newuser.AbssCardID.ToLower() != "admin")
						{
							ModelHelper.GrantPosUserDefaultAccessRights(newuser, context);
							UserRole userRole = new UserRole
							{
								UserId = newuser.surUID,
								RoleId = salesroldId,
								CreateTime = dateTime,
								ModifyTime = dateTime,
							};
							newuserroles.Add(userRole);

						}
					}
					context.UserRoles.AddRange(newuserroles);
					context.SaveChanges();
					transaction.Commit();
				}
				catch (DbEntityValidationException e)
				{
					transaction.Rollback();
					StringBuilder sb = new StringBuilder();
					foreach (var eve in e.EntityValidationErrors)
					{
						sb.AppendFormat("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State);
						foreach (var ve in eve.ValidationErrors)
						{
							sb.AppendFormat("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"",
			ve.PropertyName,
			eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName),
			ve.ErrorMessage);
						}
					}
					ModelHelper.WriteLog(context, string.Format("Import Employee data from Central failed:{0}", sb.ToString()), "ExportFrmCentral");
					context.SaveChanges();
				}
			}
		}


		public static void SaveItemsFrmCentral(int apId, MMDbContext context, string ConnectionString)
		{
			DateTime dateTime = DateTime.Now;
			List<MyobLocationModel> locationlist = MYOBHelper.GetLocationList(ConnectionString);

			List<MyobItemLocModel> itemloclist = MYOBHelper.GetItemLocList(ConnectionString, apId);

			List<MyobItemModel> itemlist = MYOBHelper.GetItemList(ConnectionString);

			#region Handle Location                    
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					#region remove current data first:
					//context.Database.ExecuteSqlCommand("TRUNCATE TABLE [Item]");
					List<MyobLocation> locations = context.MyobLocations.Where(x => x.AccountProfileId == apId).ToList();
					context.MyobLocations.RemoveRange(locations);
					context.SaveChanges();
					#endregion

					//LocationID, LocationName,LocationIdentification,IsInactive
					List<MyobLocation> newlocations = new List<MyobLocation>();
					int idx = 0;
					foreach (var location in locationlist)
					{
						newlocations.Add(new MyobLocation
						{
							IsPrimary = idx == 0,
							LocationID = location.LocationID,
							IsInactive = location.IsInactive,
							LocationName = location.LocationName,
							LocationIdentification = location.LocationIdentification,
							AccountProfileId = apId,
							CreateTime = dateTime,
						});
						idx++;
					}
					context.MyobLocations.AddRange(newlocations);
					context.SaveChanges();
					ModelHelper.WriteLog(context, "Import Location data from Central done", "ImportFrmCentral");
					context.SaveChanges();
					transaction.Commit();
				}
				catch (DbEntityValidationException e)
				{
					transaction.Rollback();
					StringBuilder sb = new StringBuilder();
					foreach (var eve in e.EntityValidationErrors)
					{
						sb.AppendFormat("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State);
						foreach (var ve in eve.ValidationErrors)
						{
							sb.AppendFormat("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"",
				ve.PropertyName,
				eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName),
				ve.ErrorMessage);
						}
					}
					//throw new Exception(sb.ToString());
					ModelHelper.WriteLog(context, string.Format("Import item data from Central failed:{0}", sb.ToString()), "ImportFrmCentral");
					context.SaveChanges();
				}
			}
			#endregion

			#region Handle Item

			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					#region remove current data first:
					//context.Database.ExecuteSqlCommand("TRUNCATE TABLE [Item]");
					List<MyobItem> items = context.MyobItems.Where(x => x.AccountProfileId == apId).ToList();
					context.MyobItems.RemoveRange(items);
					context.SaveChanges();
					#endregion

					List<MyobItem> newitems = new List<MyobItem>();
					foreach (var item in itemlist)
					{
						MyobItem _item = new MyobItem
						{
							itmIsActive = item.IsInactive == 'N',
							itmName = item.ItemName,
							itmCode = item.ItemNumber,
							itmDesc = item.ItemDescription,
							itmUseDesc = item.UseDescription == 'Y',
							itmIsTaxedWhenBought = item.ItemIsTaxedWhenBought == 'Y',
							itmIsTaxedWhenSold = item.ItemIsTaxedWhenSold == 'Y',
							itmBaseSellingPrice = Convert.ToDecimal(item.BaseSellingPrice),
							itmSellUnit = item.SellUnitMeasure,
							itmSellUnitQuantity = item.SellUnitQuantity,
							itmBuyUnit = item.BuyUnitMeasure,
							itmLastUnitPrice = Convert.ToDecimal(item.LastUnitPrice),
							itmBuyStdCost = item.TaxExclusiveStandardCost == 0 ? item.TaxInclusiveStandardCost : item.TaxExclusiveStandardCost,
							itmTaxExclusiveLastPurchasePrice = item.TaxExclusiveLastPurchasePrice,
							itmTaxInclusiveLastPurchasePrice = item.TaxInclusiveLastPurchasePrice,
							itmTaxExclusiveStandardCost = item.TaxExclusiveStandardCost,
							itmTaxInclusiveStandardCost = item.TaxInclusiveStandardCost,
							itmChgCtrl = item.ChangeControl,
							itmCreateTime = dateTime,
							itmModifyTime = dateTime,
							itmItemID = item.ItemID,
							AccountProfileId = apId,
							itmIsNonStock = item.ItemIsInventoried == 'N',
							itmIsSold = item.ItemIsSold == 'Y',
							itmIsBought = item.ItemIsBought == 'Y',
							itmSupCode = item.SupplierItemNumber,
							IncomeAccountID = item.IncomeAccountID,
							ExpenseAccountID = item.ExpenseAccountID,
							InventoryAccountID = item.InventoryAccountID,
						};

						newitems.Add(_item);
					}
					context.MyobItems.AddRange(newitems);
					context.SaveChanges();
					ModelHelper.WriteLog(context, "Import Item data from Central done", "ImportFrmCentral");
					context.SaveChanges();
					transaction.Commit();
				}
				catch (DbEntityValidationException e)
				{
					transaction.Rollback();
					StringBuilder sb = new StringBuilder();
					foreach (var eve in e.EntityValidationErrors)
					{
						sb.AppendFormat("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State);
						foreach (var ve in eve.ValidationErrors)
						{
							sb.AppendFormat("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"",
				ve.PropertyName,
				eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName),
				ve.ErrorMessage);
						}
					}
					//throw new Exception(sb.ToString());
					ModelHelper.WriteLog(context, string.Format("Import item data from Central failed:{0}", sb.ToString()), "ImportFrmCentral");
					context.SaveChanges();
				}
			}
			#endregion

			#region Handle Stock

			#region backup & remove current data first
			//context.Database.ExecuteSqlCommand("TRUNCATE TABLE [LocStock]");
			List<MyobLocStock> stocks = context.MyobLocStocks.Where(x => x.AccountProfileId == apId).ToList();
			Dictionary<string, int> DicLocItemQty = new Dictionary<string, int>();

			foreach (var stock in stocks)
			{
				var Key = string.Concat(stock.lstStockLoc, ":", stock.lstItemCode);
				DicLocItemQty[Key] = stock.lstQuantityAvailable ?? 0;
			}

			context.MyobLocStocks.RemoveRange(stocks);
			context.SaveChanges();
			#endregion

			stocks = new List<MyobLocStock>();
			List<MyobLocStock> newstocks = new List<MyobLocStock>();

			var activeLocationList = locationlist.Where(x => x.IsInactive != null && !(bool)x.IsInactive).ToList();
			var addedstockcode = new List<string>();

			foreach (var item in itemloclist)
			{
				var inCurrLoc = itemloclist.Where(x => x.ItemCode == item.ItemCode);
				if (inCurrLoc.Count() == activeLocationList.Count)
				{
					string Id = CommonHelper.GenerateNonce(50, false);
					stocks.Add(new MyobLocStock
					{
						Id = Id,
						lstItemLocationID = item.ItemLocationID,
						lstLocationID = item.LocationID,
						lstStockLoc = item.LocationCode,
						lstItemID = item.ItemID,
						lstItemCode = item.ItemCode,
						lstAbssQty = item.QuantityOnHand,
						lstQuantityAvailable = item.QuantityOnHand,
						lstCreateTime = dateTime,
						lstModifyTime = dateTime,
						AccountProfileId = apId,
					});
					addedstockcode.Add(item.ItemCode);
				}
			}

			if (stocks.Count > 0)
			{
				context.MyobLocStocks.AddRange(stocks);
				context.SaveChanges();
			}

			var _itemlist = addedstockcode.Count > 0 ? itemlist.Where(x => !addedstockcode.Contains(x.ItemNumber)) : itemlist;

			foreach (var loc in activeLocationList)
			{
				foreach (var item in _itemlist)
				{
					var stock = itemloclist.FirstOrDefault(x => x.LocationCode == loc.LocationIdentification && x.ItemCode == item.ItemNumber);

					var Key = string.Concat(loc.LocationIdentification, ":", item.ItemNumber);

					int qty = DicLocItemQty.Keys.Contains(Key) ? DicLocItemQty[Key] : 0;
					string Id = CommonHelper.GenerateNonce(50, false);
					newstocks.Add(new MyobLocStock
					{
						Id = Id,
						lstItemLocationID = stock.ItemLocationID,
						lstItemID = item.ItemID,
						lstItemCode = item.ItemNumber,
						lstStockLoc = loc.LocationIdentification,
						lstAbssQty = stock == null ? 0 : stock.QuantityOnHand,
						lstQuantityAvailable = qty,
						lstCreateTime = dateTime,
						lstModifyTime = dateTime,
						AccountProfileId = apId,
					});
				}
			}
			context.MyobLocStocks.AddRange(newstocks);
			context.SaveChanges();

			ModelHelper.WriteLog(context, "Import Item Location data from Central done;added office stock:" + newstocks.Where(x => x.lstStockLoc == "office").Count(), "ExportFrmCentral");
			context.SaveChanges();

			#endregion

			#region Handle Price
			using (var transaction = context.Database.BeginTransaction())
			{
				try
				{
					List<MyobItemPriceModel> itemplist = MYOBHelper.GetItemPriceList(ConnectionString);
					List<MyobItemPriceModel> filterediplist = new List<MyobItemPriceModel>();
					foreach (var ip in itemplist)
					{
						if (ip.QuantityBreak == 1)
						{
							filterediplist.Add(ip);
						}
					}

					#region remove current data first
					//context.Database.ExecuteSqlCommand("TRUNCATE TABLE [ItemPrice]");
					List<MyobItemPrice> itemprices = context.MyobItemPrices.Where(x => x.AccountProfileId == apId).ToList();
					context.MyobItemPrices.RemoveRange(itemprices);
					context.SaveChanges();
					#endregion
					List<MyobItemPrice> newitemprices = new List<MyobItemPrice>();

					foreach (var item in filterediplist)
					{
						MyobItemPrice itemPrice = new MyobItemPrice();
						itemPrice.ItemPriceID = item.ItemPriceID;
						itemPrice.ItemID = item.ItemID;
						itemPrice.QuantityBreak = item.QuantityBreak;
						itemPrice.QuantityBreakAmount = item.QuantityBreakAmount;
						itemPrice.PriceLevel = item.PriceLevel.ToString();
						itemPrice.PriceLevelNameID = item.PriceLevelNameID;
						itemPrice.SellingPrice = item.SellingPrice;
						itemPrice.UnitPrice = item.LastUnitPrice;
						itemPrice.ItemCode = item.ItemCode;
						itemPrice.ChangeControl = item.ChangeControl;
						itemPrice.CreateTime = dateTime;
						itemPrice.ModifyTime = dateTime;
						itemPrice.AccountProfileId = apId;
						newitemprices.Add(itemPrice);
					}

					context.MyobItemPrices.AddRange(newitemprices);
					ModelHelper.WriteLog(context, "Import Item Price data from Central done", "ImportFrmCentral");
					context.SaveChanges();
					transaction.Commit();

				}
				catch (DbEntityValidationException e)
				{
					transaction.Rollback();
					StringBuilder sb = new StringBuilder();
					foreach (var eve in e.EntityValidationErrors)
					{
						sb.AppendFormat("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State);
						foreach (var ve in eve.ValidationErrors)
						{
							sb.AppendFormat("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"",
				ve.PropertyName,
				eve.Entry.CurrentValues.GetValue<object>(ve.PropertyName),
				ve.ErrorMessage);
						}
					}
					//throw new Exception(sb.ToString());
					ModelHelper.WriteLog(context, string.Format("Import itemprice data from Central failed:{0}", sb.ToString()), "ImportFrmCentral");
					context.SaveChanges();
				}
			}

			#endregion

			#region Init ItemOption if there isn't any in DB                  
			var currentItemOptions = context.ItemOptions.Where(x => x.AccountProfileId == apId);
			List<ItemOption> itemOptions = new List<ItemOption>();
			foreach (var item in itemlist)
			{
				if (!currentItemOptions.Any(x => x.itemId == item.ItemID))
				{
					itemOptions.Add(new ItemOption
					{
						itemId = item.ItemID,
						AccountProfileId = apId,
						chkBat = false,
						chkSN = false,
						chkVT = false,
						itmCode = item.ItemNumber,
						CreateTime = DateTime.Now,
					});
				}
			}
			if (itemOptions.Count > 0)
			{
				context.ItemOptions.AddRange(itemOptions);
				context.SaveChanges();
			}
			#endregion
		}


		public static List<string> Prepare4UploadPurchaseOrder(ComInfo comInfo, int apId, MMDbContext context, SqlConnection connection, string strfrmdate, string strtodate, bool includeUploaded, ref HashSet<long> checkoutIds)
		{

			bool ismultiuser = (bool)comInfo.MyobMultiUser;
			string currencycode = comInfo.DefaultCurrencyCode;
			double exchangerate = (double)comInfo.DefaultExchangeRate;
			bool approvalmode = (bool)comInfo.ApprovalMode;
			List<string> sqllist = new List<string>();

			DataTransferModel dmodel = new DataTransferModel
			{
				SelectedLocation = comInfo.Shop,
				includeUploaded = includeUploaded,
			};

			#region Date Ranges
			int year = DateTime.Now.Year;
			DateTime frmdate;
			DateTime todate;
			if (string.IsNullOrEmpty(strfrmdate))
			{
				//frmdate = new DateTime(year, 1, 1);
				frmdate = DateTime.Today;
			}
			else
			{
				int mth = int.Parse(strfrmdate.Split('/')[1]);
				int day = int.Parse(strfrmdate.Split('/')[0]);
				year = int.Parse(strfrmdate.Split('/')[2]);
				frmdate = new DateTime(year, mth, day);
			}
			if (string.IsNullOrEmpty(strtodate))
			{
				//todate = new DateTime(year, 12, 31);
				todate = DateTime.Today;
			}
			else
			{
				int mth = int.Parse(strtodate.Split('/')[1]);
				int day = int.Parse(strtodate.Split('/')[0]);
				year = int.Parse(strtodate.Split('/')[2]);
				todate = new DateTime(year, mth, day);
			}
			#endregion

			dmodel.FrmToDate = frmdate;
			dmodel.ToDate = todate;

			var dateformatcode = context.AppParams.FirstOrDefault(x => x.appParam == "DateFormat").appVal;

			GetDataTransferData(context, apId, CheckOutType.Purchase, ref dmodel);

			checkoutIds = dmodel.CheckOutIds_Purchase;

			#region Handle ItemOptions
			var itemcodes = dmodel.ItemCodes;
			var itemoptions = context.GetItemOptionsByItemCodes6(comInfo.AccountProfileId, string.Join(",", itemcodes)).ToList();
			var stritemcodes = string.Join(",", itemcodes);
			var location = dmodel.SelectedLocation;

			var itembtInfo = context.GetBatchVtInfoByItemCodes12(comInfo.AccountProfileId, location, stritemcodes).ToList();
			var itemvtInfo = context.GetValidThruInfo12(comInfo.AccountProfileId, location, stritemcodes).ToList();

			var ibqList = (from item in itembtInfo
						   where item.batCode != null
						   group item by new { item.itmCode, item.batCode } into itemgroup
						   select new
						   {
							   ItemCode = itemgroup.Key.itmCode,
							   BatchCode = itemgroup.Key.batCode,
							   TotalQty = itemgroup.Sum(x => x.batQty)
						   }).ToList();
			ibqList = ibqList.OrderBy(x => x.ItemCode).ThenBy(x => x.BatchCode).ToList();

			var ivqList = (from item in itemvtInfo
						   where item.piValidThru != null
						   group item by new { item.itmCode, item.piValidThru, item.pstCode } into itemgroup
						   select new
						   {
							   PoCode = itemgroup.Key.pstCode,
							   ItemCode = itemgroup.Key.itmCode,
							   ValidThru = CommonHelper.FormatDate((DateTime)itemgroup.Key.piValidThru),
							   TotalQty = itemgroup.Sum(x => x.vtQty)
						   }).ToList();
			ivqList = ivqList.OrderBy(x => x.ItemCode).ToList();

			var serialInfo = context.GetSerialInfo5(comInfo.AccountProfileId, location, stritemcodes, null).ToList();
			var ibvqList = (from item in itembtInfo
							where item.batCode != null
							//group item by new { item.itmCode, item.ivBatCode, item.piValidThru } into itemgroup
							select new
							{
								PoCode = item.pstCode,
								ItemCode = item.itmCode,
								BatchCode = item.batCode,
								ValidThru = item.batValidThru == null ? "" : CommonHelper.FormatDate((DateTime)item.batValidThru),
								BatQty = (int)item.batQty
							}).ToList();
			ibvqList = ibvqList.OrderBy(x => x.ItemCode).ThenBy(x => x.BatchCode).ToList();

			var batchcodelist = ibvqList.Select(x => x.BatchCode).Distinct().ToList();

			var DicItemBatVtList = new Dictionary<string, Dictionary<string, List<string>>>();
			var DicItemBatSnVtList = new Dictionary<string, Dictionary<string, List<Models.Purchase.BatSnVt>>>();
			var DicItemSnVtList = new Dictionary<string, List<SnVt>>();
			var DicItemVtList = new Dictionary<string, List<string>>();

			foreach (var itemcode in itemcodes)
			{
				DicItemBatVtList[itemcode] = new Dictionary<string, List<string>>();
				DicItemBatSnVtList[itemcode] = new Dictionary<string, List<Models.Purchase.BatSnVt>>();
				DicItemSnVtList[itemcode] = new List<SnVt>();
				DicItemVtList[itemcode] = new List<string>();

				foreach (var batchcode in batchcodelist)
				{
					DicItemBatVtList[itemcode][batchcode] = new List<string>();
					DicItemBatSnVtList[itemcode][batchcode] = new List<Models.Purchase.BatSnVt>();
				}
			}
			#endregion

			foreach (var po in dmodel.DicPoPurchaseItemList.Keys)
			{
				string sql = MyobHelper.InsertImportItemPurchasesSql;
				sql = sql.Replace("0", "{0}");
				List<string> values = null;

				List<string> columns = new List<string>();
				for (int j = 0; j < MyobHelper.ImportItemPurchasesColCount; j++)
				{
					columns.Add("'{" + j + "}'");
				}
				string strcolumn = string.Join(",", columns);
				var purchaseitems = dmodel.DicPoPurchaseItemList[po];
				//string status = purchaseitems.Any(x => x.IsPartial != null && (bool)x.IsPartial) ? "B" : "O";
				string status = "O";
				values = new List<string>();
				foreach (var purchaseitem in dmodel.DicPoPurchaseItemList[po])
				{
					string description = string.Empty;
					string value = "";
					purchaseitem.dateformat = dateformatcode == "E" ? @"dd/MM/yyyy" : @"MM/dd/yyyy";
					var itemoption = itemoptions.FirstOrDefault(x => x.itmCode == purchaseitem.itmCode);

					string bat = string.Empty;
					string sn = string.Empty;
					string vt = string.Empty;
					string batvt = string.Empty;
					string snvt = string.Empty;
					string batsn = string.Empty;

					string _batvtInfo = string.Empty;
					List<string> batvtInfo = new List<string>();
					string _snvtInfo = string.Empty;
					List<string> snvtInfo = new List<string>();
					List<string> vtInfo = new List<string>();

					#region Handle Item Options
					if (itemoption != null)
					{
						if (itemoption.chkBat)
						{
							if (ibvqList.Count > 0)
							{
								foreach (var item in ibvqList)
								{
									string strvt = "";
									if (purchaseitem.itmCode == item.ItemCode)
									{
										foreach (var kv in DicItemBatVtList)
										{
											if (!(bool)itemoption.chkSN && (bool)itemoption.chkVT)
											{
												if (kv.Key == item.ItemCode)
												{
													var vtarr = new List<string>();
													foreach (var k in kv.Value.Keys)
													{
														if (k == item.BatchCode && !string.IsNullOrEmpty(item.ValidThru))
														{
															DicItemBatVtList[kv.Key][k].Add(item.ValidThru);
															vtarr.Add(item.ValidThru);
														}
													}
													if (vtarr.Count > 0)
														strvt = string.Concat(" (", string.Join(",", vtarr), ")");
												}
											}
										}

										bat = string.Concat("[", item.BatchCode, "]");
										if (!string.IsNullOrEmpty(strvt))
										{
											batvt = string.Concat(bat, strvt);
										}
										//Response.Write(item.PoCode + ":" + item.ItemCode + ":" + bat + ":" + batvt + "<br>");
										_batvtInfo = string.IsNullOrEmpty(batvt) ? bat : batvt;
										batvtInfo.Add(_batvtInfo);
									}
								}
							}
						}

						if (itemoption.chkSN)
						{
							if (serialInfo.Count > 0)
							{
								foreach (var serial in serialInfo)
								{
									string strvt = "";
									if (purchaseitem.itmCode == serial.snoItemCode)
									{
										foreach (var kv in DicItemSnVtList)
										{
											if (!(bool)itemoption.chkBat && (bool)itemoption.chkVT)
											{
												if (kv.Key == serial.snoItemCode)
												{
													var vtarr = new List<string>();

													foreach (var v in kv.Value)
													{
														if (!string.IsNullOrEmpty(v.vt))
														{
															DicItemSnVtList[kv.Key].Add(v);
															vtarr.Add(v.vt);
														}
													}
													if (vtarr.Count > 0)
														strvt = string.Concat(" (", string.Join(",", vtarr), ")");
												}
											}
										}

										sn = string.Concat("[", serial.snoCode, "]");
										if (!string.IsNullOrEmpty(strvt))
										{
											snvt = string.Concat(sn, strvt);
										}
										_snvtInfo = string.IsNullOrEmpty(snvt) ? sn : snvt;
										snvtInfo.Add(_snvtInfo);
									}
								}
							}
						}

						if (itemoption.chkBat && itemoption.chkSN && itemoption.chkVT)
						{
							if (ivqList != null && ivqList.Count() > 0)
							{
								foreach (var v in ivqList)
								{
									vt = string.Concat("[", v.ValidThru, "]");
									vtInfo.Add(vt);
								}
							}
						}
					}

					if (batvtInfo.Count > 0)
					{
						description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", batvtInfo));
					}
					else if (snvtInfo.Count > 0)
					{
						description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", snvtInfo));
					}
					else if (vtInfo.Count > 0)
					{
						description = string.Concat(purchaseitem.itmNameDesc, Environment.NewLine, string.Join(",", vtInfo));
					}
					else
					{
						description = purchaseitem.itmNameDesc;
					}
					#endregion

					if (description.Length > 255)
						description = description.Substring(0, 255);
					string memo = genMemo(purchaseitem.pstCurrency, purchaseitem.pstExRate, Convert.ToDouble(purchaseitem.piAmt), purchaseitem.pstRemark);

					/*CoLastName,PurchaseNumber,PurchaseDate,SuppliersNumber,DeliveryStatus,ItemNumber,Quantity,Price,Discount,Memo,DeliveryDate,PurchaseStatus,AmountPaid,Ordered,Location,CardID,CurrencyCode,ExchangeRate,TaxCode,Job,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount
                     */
					value = string.Format("(" + strcolumn + ")", StringHandlingForSQL(purchaseitem.SupplierName), purchaseitem.pstCode, purchaseitem.PurchaseDate4ABSS, StringHandlingForSQL(purchaseitem.pstSupplierInvoice), "A", StringHandlingForSQL(purchaseitem.itmCode), purchaseitem.piQty, purchaseitem.piUnitPrice, purchaseitem.piDiscPc, memo, purchaseitem.PromisedDate4ABSS, status, "0", purchaseitem.piReceivedQty, purchaseitem.piStockLoc, StringHandlingForSQL(purchaseitem.supCode), StringHandlingForSQL(purchaseitem.pstCurrency), purchaseitem.pstExRate, purchaseitem.TaxCode, purchaseitem.JobNumber, purchaseitem.Myob_PaymentIsDue, purchaseitem.Myob_DiscountDays, purchaseitem.Myob_BalanceDueDays, "0");
					values.Add(value);
				}
				sql = string.Format(sql, string.Join(",", values));
				sqllist.Add(sql);
			}

			return sqllist;
		}


		public static void GenSql4JournalItem(string dateformatcode, out List<string> columns, out string strcolumn, out string value, ref List<string> values, JournalLnView item, string sqlfields)
		{
			int collength = sqlfields.Split(',').Length;

			string dateformat = dateformatcode == "E" ? @"dd/MM/yyyy" : @"MM/dd/yyyy";

			columns = new List<string>();
			for (int j = 0; j < collength; j++)
			{
				columns.Add("'{" + j + "}'");
			}
			strcolumn = string.Join(",", columns);

			/*
			 * JournalNumber,JournalDate,Memo,Inclusive,AccountNumber,DebitExTaxAmount,DebitIncTaxAmount,CreditExTaxAmount,CreditIncTaxAmount,Job,AllocationMemo
			 */
			var journaldate = CommonHelper.FormatDate4ABSS(item.JournalDate, dateformat);
			var inclusive = item.Inclusive ? "Y" : "N";
			var accno = item.AccountNumber.Replace("-", "");
			var debitex = Convert.ToDouble(item.DebitExTaxAmount ?? 0);
			var debitin = Convert.ToDouble(item.DebitIncTaxAmount ?? 0);
			var creditex = Convert.ToDouble(item.CreditExTaxAmount ?? 0);
			var creditin = Convert.ToDouble(item.CreditIncTaxAmount ?? 0);
			value = string.Format(strcolumn, item.JournalNumber, journaldate, StringHandlingForSQL(item.Memo), inclusive, accno, debitex, debitin, creditex, creditin, item.JobName, StringHandlingForSQL(item.AllocationMemo));

			value = string.Concat("(", value, ")");

			values.Add(value);
		}

		private static string StringHandlingForSQL(string strInput)
		{
			return CommonHelper.StringHandlingForSQL(strInput);
		}

		public static string genMemo(string currencycode, double exchangerate, double paytypeamts, string ordercode, string paytypes = "")
		{
			return StringHandlingForSQL(string.Concat(currencycode, " ", paytypes, " ", paytypeamts, " ", $"({exchangerate})", " ", ordercode));
		}

		private static string GetForeignCurrencyCardName(string cardCode, string cardName)
		{
			string currkeys = cardCode.Length >= 6 && cardCode.StartsWith("CAS") ? cardCode.Substring(3, 3).ToLower() : cardCode;
			return currkeys == "usd" || currkeys == "cny" || currkeys == "eur" || currkeys == "mop" ? string.Concat(cardName, " (", currkeys.ToUpper(), ")") : cardName;
		}

		public static void GetReady4Print(MMDbContext context, ref ReceiptViewModel Receipt, ref List<string> DisclaimerList, ref List<string> PaymentTermsList)
		{
			int reId = 0;
			Receipt = (from r in context.Receipts
					   where r.deviceCode.ToLower() == ComInfo.Device.ToLower() && r.shopCode.ToLower() == ComInfo.Shop.ToLower() && r.AccountProfileId == ComInfo.AccountProfileId
					   select new ReceiptViewModel { Id = r.Id, CompanyName = r.CompanyName + ":" + r.CompanyNameCN + ":" + r.CompanyNameEng, CompanyAddress = r.CompanyAddress + ":" + r.CompanyAddressCN + ":" + r.CompanyAddressEng, CompanyAddress1 = r.CompanyAddress1 + ":" + r.CompanyAddress1CN + ":" + r.CompanyAddress1Eng, CompanyPhone = r.CompanyPhone, CompanyWebSite = r.CompanyWebSite }
						  ).FirstOrDefault();
			int lang = CultureHelper.CurrentCulture;
			Receipt.CompanyName = Receipt.CompanyName.Split(':')[lang];
			Receipt.CompanyAddress = Receipt.CompanyAddress.Split(':')[lang];
			Receipt.CompanyAddress1 = Receipt.CompanyAddress1.Split(':')[lang];

			reId = Receipt.Id;
			var disclaimers = (from ri in context.ReceiptInfoes
							   where ri.reId == reId
							   select ri.DisclaimerTxt + ":" + ri.DisclaimerTxtCN + ":" + ri.DisclaimerTxtEng).ToList();
			if (disclaimers.Count > 0)
			{
				DisclaimerList = new List<string>();
				foreach (var d in disclaimers)
				{
					DisclaimerList.Add(d.Split(':')[CultureHelper.CurrentCulture]);
				}
			}
			var paymentterms = (from ri in context.ReceiptInfoes
								where ri.reId == reId
								select ri.PaymentTermsTxt + ":" + ri.PaymentTermsTxtCN + ":" + ri.PaymentTermsTxtEng).ToList();
			if (paymentterms.Count > 0)
			{
				PaymentTermsList = new List<string>();
				foreach (var d in paymentterms)
				{
					PaymentTermsList.Add(d.Split(':')[CultureHelper.CurrentCulture]);
				}
			}
		}
		public static Dictionary<string, ItemOptions> GetDicItemOptions(int apId, MMDbContext context)
		{
			var DicItemOptions = new Dictionary<string, ItemOptions>();
			var itemoptions = context.GetItemOptions4(apId).ToList();
			foreach (var item in itemoptions)
			{
				if (!DicItemOptions.ContainsKey(item.itmCode))
				{
					DicItemOptions[item.itmCode] = new ItemOptions
					{
						ChkBatch = item.chkBat,
						ChkSN = item.chkSN,
						WillExpire = item.chkVT
					};
				}
			}

			return DicItemOptions;
		}
		public static Dictionary<string, ItemOptions> GetDicItemOptions(int apId, SqlConnection connection)
		{
			var DicItemOptions = new Dictionary<string, ItemOptions>();
			var itemoptions = connection.Query<ItemOption>(@"EXEC dbo.GetItemOptions4 @apId=@apId", new { apId }).ToList();
			foreach (var item in itemoptions)
			{
				if (!DicItemOptions.ContainsKey(item.itmCode))
				{
					DicItemOptions[item.itmCode] = new ItemOptions
					{
						ChkBatch = item.chkBat,
						ChkSN = item.chkSN,
						WillExpire = item.chkVT
					};
				}
			}

			return DicItemOptions;
		}

		public static string HandleCheckoutPortal(string defaultCheckoutPortal = "")
		{
			string defaultcheckoutportal;
			if (string.IsNullOrEmpty(defaultCheckoutPortal))
			{
				defaultcheckoutportal = ComInfo.DefaultCheckoutPortal;
				defaultcheckoutportal = defaultcheckoutportal == "abss" ? defaultcheckoutportal.ToUpper() : CommonHelper.FirstCharToUpper(defaultcheckoutportal);
			}
			else
			{
				defaultcheckoutportal = defaultCheckoutPortal == "abss" ? defaultCheckoutPortal.ToUpper() : CommonHelper.FirstCharToUpper(defaultCheckoutPortal);
			}

			return defaultcheckoutportal;
		}
		public static void GrantPosUserDefaultAccessRights(SysUser salesman, MMDbContext context, string usercode = "staff01")
		{
			//remove current records first, just in case...
			var rights = context.AccessRights.Where(x => x.UserCode.ToLower() == salesman.UserCode.ToLower()).ToList();
			context.AccessRights.RemoveRange(rights);
			context.SaveChanges();

			//rights = context.AccessRights.Where(x => x.UserCode.ToLower() == usercode.ToLower()).ToList();
			var _rights = context.GetDefaultAccessRights(usercode).ToList();
			foreach (var right in _rights)
			{
				right.UserCode = salesman.UserCode;
			}
			context.AccessRights.AddRange(rights);
			context.SaveChanges();
		}
		public static string GetUserCode(MMDbContext context, string usertype = "sales")
		{
			var MaxSalesCode = context.GetMaxUserCode(usertype).FirstOrDefault();
			var resultString = Regex.Match(MaxSalesCode, @"\d+").Value;
			CommonLib.Helpers.ModelHelper.GetCodeDigit(ref resultString);
			return string.Concat(usertype, resultString);
		}
		public static bool CheckIfCrmSalesManager(SessUser User = null)
		{
			SessUser user = User == null ? HttpContext.Current.Session["User"] as SessUser : User;
			return user.Roles.Any(x => x == RoleType.CRMSalesManager);
		}
		public static bool CheckIfCrmAdmin(SessUser User = null)
		{
			SessUser user = User == null ? HttpContext.Current.Session["User"] as SessUser : User;
			return user.Roles.Any(x => x == RoleType.CRMAdmin);
		}
		public static void GetEmailPhoneList(int apId, ref List<string> CusEmailList, ref List<string> CusPhoneList)
		{
			using var connection = new SqlConnection(DefaultConnection);
			connection.Open();
			var emailphonelist = connection.Query<string>(@"EXEC dbo.GetEmailPhoneList @apId=@apId,@type=@type", new { apId, type = "contact" }).ToList();

			//var mailphonelist = context.GetEmailPhoneList("contact").ToList();
			if (emailphonelist != null)
			{
				foreach (var item in emailphonelist)
				{
					var arr = item.Split(new string[] { "||" }, StringSplitOptions.None);
					CusPhoneList.Add(arr[1]);
					CusEmailList.Add(arr[0]);
				}
			}
		}

		public static string GetSalesmanName(UserModel item)
		{
			if (item != null)
			{
				return item.FirstName == "" && item.LastName == "" ? item.UserName : string.Concat(item.FirstName, " ", item.LastName);
			}
			return "N/A";

		}


		public static string GetAccountProfileName(MMDbContext context)
		{
			var accname = "";
			var accountprofile = context.AccountProfiles.FirstOrDefault(x => x.Id == AccountProfileId && x.CompanyId == CompanyId);
			if (accountprofile != null)
			{
				accname = accountprofile.ProfileName;
			}
			return accname;
		}


		public static string GetManagerName(List<UserModel> users, int managerId)
		{
			var manager = users.Where(x => x.surUID == managerId).FirstOrDefault();
			if (manager != null)
			{
				return manager.UserName;
			}
			return null;
		}

		public static int GetAccountProfileId(MMDbContext context)
		{
			return GetCurrentSession(context).AccountProfileId;
		}
		public static string GetNewPurchaseOrderCode(DeviceModel model, MMDbContext context, bool plusone = true)
		{
			var device = context.Devices.Find(model.dvcUID);
			var purchaseprefix = device.dvcPurchaseOrderPrefix;
			var nextpurchaseno = plusone ? device.dvcNextPurchaseOrderNo + 1 : device.dvcNextPurchaseOrderNo;
			string purchaseno = device.dvcNextPurchaseOrderNo > device.dvcPurchaseInitNo ? $"{nextpurchaseno:000000}" : device.dvcPurchaseInitNo.ToString();
			return string.Concat(purchaseprefix, purchaseno);
		}
		public static string GetNewPurchaseRequestCode(DeviceModel model, MMDbContext context, bool plusone = true)
		{
			var device = context.Devices.Find(model.dvcUID);
			var purchaseprefix = device.dvcPurchaseRequestPrefix;
			var nextpurchaseno = plusone ? device.dvcNextPurchaseRequestNo + 1 : device.dvcNextPurchaseRequestNo;
			string purchaseno = device.dvcNextPurchaseRequestNo > device.dvcPurchaseInitNo ? $"{nextpurchaseno:000000}" : device.dvcPurchaseInitNo.ToString();
			return string.Concat(purchaseprefix, purchaseno);
		}
		public static string GetNewWholeSalesCode(DeviceModel model, MMDbContext context, bool plusone = true)
		{
			var device = context.Devices.Find(model.dvcUID);
			var purchasecode = device.dvcWholesalesPrefix;
			var nextpurchaseno = plusone ? device.dvcNextWholeSalesNo + 1 : device.dvcNextWholeSalesNo;
			string purchaseno = device.dvcNextWholeSalesNo > device.dvcWholeSalesInitNo ? $"{nextpurchaseno:000000}" : device.dvcWholeSalesInitNo.ToString();
			return string.Concat(purchasecode, purchaseno);
		}
		public static string GetNewPurchaseReturnCode(DeviceModel model, MMDbContext context)
		{
			var device = context.Devices.FirstOrDefault(x => x.AccountProfileId == model.AccountProfileId && x.dvcShop == model.dvcShop && x.dvcIsActive);
			var returncode = device.dvcPsReturnCode;
			string returnno = device.dvcNextPsReturnNo > device.dvcPsReturnInitNo ? $"{device.dvcNextPsReturnNo++:000000}" : device.dvcPsReturnInitNo.ToString();
			return string.Concat(returncode, returnno);
		}
		public static string GetNewWholeSalesReturnCode(DeviceModel model, MMDbContext context)
		{
			var device = context.Devices.FirstOrDefault(x => x.AccountProfileId == model.AccountProfileId && x.dvcShop == model.dvcShop && x.dvcIsActive);
			var returncode = device.dvcWsReturnCode;
			string returnno = device.dvcNextWsReturnNo > device.dvcWsReturnInitNo ? $"{device.dvcNextWsReturnNo++:000000}" : device.dvcWsReturnInitNo.ToString();
			return string.Concat(returncode, returnno);
		}

		public static string GetNewRefundCode(string devicecode, DeviceModel device, MMDbContext context)
		{
			string refundcode = "";
			if (device.dvcCode == devicecode)
			{
				refundcode = getNewRefundCode(device);
			}
			else
			{
				var _device = context.Devices.FirstOrDefault(x => x.dvcCode == devicecode && x.AccountProfileId == ComInfo.AccountProfileId);
				if (_device != null)
				{
					refundcode = getNewRefundCode((DeviceModel)_device);
				}
			}
			return refundcode;
		}

		private static string getNewRefundCode(DeviceModel device)
		{
			string refundcode;
			var refundinitcode = device.dvcRtlRefundCode ?? "RF";
			var refundno = $"{device.dvcNextRefundNo:000000}";
			refundcode = string.Concat(refundinitcode, refundno);
			return refundcode;
		}

		public static string GetItemCodes4PayServices(List<string> salesitemcodes)
		{
			var codes = string.Join(",", salesitemcodes);
			return codes.Length <= 127 ? codes : codes.Substring(0, 127);
		}





		public static string GetItemDesc4PayService(MMDbContext context, int apId, List<string> salesitemcodes, bool usedesc = false)
		{
			var itemlist = context.MyobItems.Where(x => x.AccountProfileId == apId).ToList();
			List<ItemModel> items = new List<ItemModel>();
			foreach (var salesitemcode in salesitemcodes)
			{
				var _item = itemlist.FirstOrDefault(x => x.itmCode == salesitemcode);
				if (_item != null)
				{
					ItemModel item = new ItemModel
					{

						itmName = _item.itmName,
						itmUseDesc = _item.itmUseDesc,
						itmDesc = _item.itmDesc,
					};
					items.Add(item);
				}
			}
			string desc = usedesc ? string.Join(",", items.Select(x => x.NameDesc).ToList()) : string.Join(",", items.Select(x => x.itmName).ToList());
			return desc.Length <= 127 ? desc : desc.Substring(0, 127);
		}



		public static List<UserModel> GetStaffList(MMDbContext context, SessUser sessUser)
		{
			List<UserModel> stafflist = new List<UserModel>();
			stafflist = (from u in context.SysUsers
						 where u.ManagerId == sessUser.surUID
						 select new UserModel
						 {
							 surUID = u.surUID,
							 UserCode = u.UserCode,
							 UserName = u.UserName,
							 surIsActive = u.surIsActive,
							 ManagerId = u.ManagerId,
							 dvcCode = u.dvcCode,
							 shopCode = u.shopCode
						 }
						 ).ToList();
			return stafflist;
		}

		public static RoleType GetUserRole(SysUser user)
		{
			if (user.ManagerId > 0)
			{
				return RoleType.SalesPerson;
			}
			else
			{
				if (user.UserCode.ToLower().Contains("admin"))
				{
					return user.UserCode.ToLower() == "superadmin" ? RoleType.SuperAdmin : RoleType.Admin;
				}
				else if (user.UserCode.ToLower().Contains("crmadmin"))
				{
					return RoleType.CRMAdmin;
				}
				else
				{
					return RoleType.CRMSalesManager;
				}
			}
		}




		public static void GetDataTransferData(MMDbContext context, int apId, CheckOutType checkOutType, ref DataTransferModel model, string connectionString = null)
		{
			if (checkOutType == CheckOutType.PayBills)
			{
				DateTime frmdate = model.FrmToDate;
				DateTime todate = model.ToDate;
				string location = model.SelectedLocation;

				#region PurchaseModels
				model.PurchaseModels = (from ps in context.Purchases
											//join sup in context.Suppliers
											//on ps.supCode equals sup.supCode
										where ps.pstPurchaseDate >= frmdate && ps.pstPurchaseDate <= todate && (ps.pstStatus.ToLower() == PurchaseStatus.opened.ToString() || ps.pstStatus.ToLower() == PurchaseStatus.partialreceival.ToString()) && ps.pstType == "PS"
										&& ps.pstSalesLoc.ToLower() == location && ps.AccountProfileId == apId
										select new PurchaseModel
										{
											Id = ps.Id,
											pstCode = ps.pstCode,
											supCode = ps.supCode,
											pstSupplierInvoice = ps.pstSupplierInvoice,
											pstSalesLoc = ps.pstSalesLoc,
											//pstLocStock = ps.pstLocStock,
											pstPurchaseDate = ps.pstPurchaseDate,
											pstPromisedDate = ps.pstPromisedDate,
											pstStatus = ps.pstStatus,
											pstCurrency = ps.pstCurrency,
											pstExRate = ps.pstExRate,
											pstRemark = ps.pstRemark,
											//SupplierName = sup.supName,
											CreateTime = ps.CreateTime,
											ModifyTime = ps.ModifyTime,
											AccountProfileId = ps.AccountProfileId,
											pstRefCode = ps.pstRefCode,
											pstType = ps.pstType,
											pstCheckout = ps.pstCheckout,
											pstIsPartial = ps.pstIsPartial,
										}
						   ).ToList();

				if (!model.includeUploaded && model.PurchaseModels.Count > 0)
				{
					model.PurchaseModels = model.PurchaseModels.Where(x => !(bool)x.pstCheckout).ToList();
				}

				model.DicPoPurchaseItemList = new Dictionary<string, List<PurchaseItemModel>>();
				model.CheckOutIds_Purchase = new HashSet<long>();
				model.ItemCodes = new HashSet<string>();

				if (model.PurchaseModels.Count > 0)
				{
					var pstCodes = string.Join(",", model.PurchaseModels.Select(x => x.pstCode).Distinct().ToList());
					var pstStatuss = string.Join(",", model.PurchaseModels.Select(x => x.pstStatus).Distinct().ToList());
					var supCodes = string.Join(",", model.PurchaseModels.Select(x => x.supCode).Distinct().ToList());

					using var connection = new SqlConnection(connectionString ?? DefaultConnection);
					connection.Open();
					var psilist = connection.Query<PurchaseItemModel>(@"EXEC dbo.GetPurchaseItemListByCodesStatus @apId=@apId,@pstCodes=@pstCodes,@pstStatuss=@pstStatuss", new { apId, pstCodes, pstStatuss }).ToList();
					var supplierlist = connection.Query<SupplierModel>(@"EXEC dbo.GetSupplierInfoByCodes @apId=@apId,@supCodes=@supCodes", new { apId, supCodes }).ToList();

					var myobtaxes = context.MyobTaxCodes.Where(x => x.AccountProfileId == apId).ToList();
					var JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();

					foreach (var ps in model.PurchaseModels)
					{
						model.DicPoPurchaseItemList[ps.pstCode] = new List<PurchaseItemModel>();
						model.CheckOutIds_Purchase.Add(ps.Id);

						var supplier = supplierlist.FirstOrDefault(x => x.supCode == ps.supCode);
						string suppliername = supplier != null ? supplier.supName : null;
						var tax = supplier != null ? myobtaxes.FirstOrDefault(x => x.TaxCodeID == supplier.TaxCodeID) : null;
						string taxcode = tax != null ? tax.TaxCode : "";

						foreach (var item in psilist)
						{
							model.ItemCodes.Add(item.itmCode);

							var job = JobList.FirstOrDefault(x => x.JobID == item.JobID);
							item.JobNumber = job != null ? job.JobNumber : "";

							if (supplier != null)
							{
								item.Myob_PaymentIsDue = supplier.PaymentIsDue != null ? (int)supplier.PaymentIsDue : 0;
								item.Myob_BalanceDueDays = supplier.BalanceDueDays != null ? (int)supplier.BalanceDueDays : 0;
								item.Myob_DiscountDays = supplier.DiscountDays != null ? (int)supplier.DiscountDays : 0;
							}
							
							model.DicPoPurchaseItemList[ps.pstCode].Add(new PurchaseItemModel
							{
								pstId = ps.Id,
								SupplierName = suppliername,
								pstSupplierInvoice = ps.pstSupplierInvoice,
								AccountProfileId = item.AccountProfileId,
								piSeq = item.piSeq,
								pstCode = ps.pstCode,
								itmCode = item.itmCode,
								itmName = item.itmName,
								itmDesc = item.itmDesc,
								itmUseDesc = item.itmUseDesc,
								piBaseUnit = item.piBaseUnit,
								piQty = item.piQty,
								piReceivedQty = item.piReceivedQty,
								piStatus = item.piStatus,
								ivBatCode = item.ivBatCode,
								piHasSN = item.piHasSN,
								piValidThru = item.piValidThru,
								piUnitPrice = item.piUnitPrice,
								piTaxPc = item.piTaxPc,
								piTaxAmt = item.piTaxAmt,
								piDiscPc = item.piDiscPc,
								piAmt = item.piAmt,
								piAmtPlusTax = item.piAmtPlusTax,
								pstLocStock = ps.pstLocStock,
								piStockLoc = item.piStockLoc,
								CreateTime = item.CreateTime,
								ModifyTime = item.ModifyTime,
								pstPurchaseDate = ps.pstPurchaseDate == null ? DateTime.Now : ps.pstPurchaseDate,
								pstPromisedDate = ps.pstPromisedDate ==null? DateTime.Now.AddDays(1):ps.pstPromisedDate,
								Myob_PaymentIsDue = item.Myob_PaymentIsDue,
								Myob_BalanceDueDays = item.Myob_BalanceDueDays,
								Myob_DiscountDays = item.Myob_DiscountDays,
								IsPartial = ps.pstIsPartial,
								supCode = ps.supCode,
								pstCurrency = ps.pstCurrency,
								pstExRate = Convert.ToDouble(ps.pstExRate),
								pstRemark = ps.pstRemark,
								JobID = item.JobID,
								TaxCode = taxcode,
								JobNumber = item.JobNumber
							});
						}
					}
				}
				#endregion
			}

			if (checkOutType == CheckOutType.Suppliers)
			{
				model.Supplierlist = (from c in context.Suppliers
									  where c.supCheckout == false && c.AccountProfileId == apId
									  select new SupplierModel
									  {
										  supId = c.supId,
										  supFirstName = c.supFirstName,
										  supIsOrganization = !c.supIsIndividual,
										  supAddrPhone1 = c.supAddrPhone1,
										  supAddrPhone2 = c.supAddrPhone2,
										  supAddrPhone3 = c.supAddrPhone3,
										  supAddrStreetLine1 = c.supAddrStreetLine1,
										  supAddrStreetLine2 = c.supAddrStreetLine2,
										  supAddrStreetLine3 = c.supAddrStreetLine3,
										  supAddrStreetLine4 = c.supAddrStreetLine4,
										  supAddrCity = c.supAddrCity,
										  supAddrCountry = c.supAddrCountry,
										  supAddrWeb = c.supAddrWeb,
										  supCode = c.supCode,
										  supName = c.supName,
										  supPhone = c.supPhone,
										  supIsActive = c.supIsActive,
										  CreateTime = c.CreateTime,
										  ModifyTime = c.ModifyTime,
										  supEmail = c.supEmail,
										  supPhone1Whatsapp = c.supPhone1Whatsapp,
										  supPhone2Whatsapp = c.supPhone2Whatsapp,
										  supPhone3Whatsapp = c.supPhone3Whatsapp
									  }
										).ToList();
				foreach (var supplier in model.Supplierlist)
				{
					model.CheckOutIds_Supplier.Add(supplier.supId);
				}
			}

			if (checkOutType == CheckOutType.Purchase)
			{
				DateTime frmdate = model.FrmToDate;
				DateTime todate = model.ToDate;
				string location = model.SelectedLocation;

				#region PurchaseModels
				model.PurchaseModels = (from ps in context.Purchases
											//join sup in context.Suppliers
											//on ps.supCode equals sup.supCode
										where ps.pstPurchaseDate >= frmdate && ps.pstPurchaseDate <= todate && (ps.pstStatus.ToLower() == PurchaseStatus.opened.ToString() || ps.pstStatus.ToLower() == PurchaseStatus.partialreceival.ToString()) && ps.pstType == "PS"
										&& ps.pstSalesLoc.ToLower() == location && ps.AccountProfileId == apId
										select new PurchaseModel
										{
											Id = ps.Id,
											pstCode = ps.pstCode,
											supCode = ps.supCode,
											pstSupplierInvoice = ps.pstSupplierInvoice,
											pstSalesLoc = ps.pstSalesLoc,
											//pstLocStock = ps.pstLocStock,
											pstPurchaseDate = ps.pstPurchaseDate,
											pstPromisedDate = ps.pstPromisedDate,
											pstStatus = ps.pstStatus,
											pstCurrency = ps.pstCurrency,
											pstExRate = ps.pstExRate,
											pstRemark = ps.pstRemark,
											//SupplierName = sup.supName,
											CreateTime = ps.CreateTime,
											ModifyTime = ps.ModifyTime,
											AccountProfileId = ps.AccountProfileId,
											pstRefCode = ps.pstRefCode,
											pstType = ps.pstType,
											pstCheckout = ps.pstCheckout,
											pstIsPartial = ps.pstIsPartial,
										}
						   ).ToList();

				if (!model.includeUploaded && model.PurchaseModels.Count > 0)
				{
					model.PurchaseModels = model.PurchaseModels.Where(x => !(bool)x.pstCheckout).ToList();
				}

				model.DicPoPurchaseItemList = new Dictionary<string, List<PurchaseItemModel>>();
				model.CheckOutIds_Purchase = new HashSet<long>();
				model.ItemCodes = new HashSet<string>();

				if (model.PurchaseModels.Count > 0)
				{
					var pstCodes = string.Join(",", model.PurchaseModels.Select(x => x.pstCode).Distinct().ToList());
					var pstStatuss = string.Join(",", model.PurchaseModels.Select(x => x.pstStatus).Distinct().ToList());
					var supCodes = string.Join(",", model.PurchaseModels.Select(x => x.supCode).Distinct().ToList());

					using var connection = new SqlConnection(connectionString ?? DefaultConnection);
					connection.Open();
					var psilist = connection.Query<PurchaseItemModel>(@"EXEC dbo.GetPurchaseItemListByCodesStatus @apId=@apId,@pstCodes=@pstCodes,@pstStatuss=@pstStatuss", new { apId, pstCodes, pstStatuss }).ToList();
					var supplierlist = connection.Query<SupplierModel>(@"EXEC dbo.GetSupplierInfoByCodes @apId=@apId,@supCodes=@supCodes", new { apId, supCodes }).ToList();

					var myobtaxes = context.MyobTaxCodes.Where(x => x.AccountProfileId == apId).ToList();
					var JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();

					foreach (var ps in model.PurchaseModels)
					{
						model.DicPoPurchaseItemList[ps.pstCode] = new List<PurchaseItemModel>();
						model.CheckOutIds_Purchase.Add(ps.Id);

						var supplier = supplierlist.FirstOrDefault(x => x.supCode == ps.supCode);
						string suppliername = supplier != null ? supplier.supName : null;
						var tax = supplier != null ? myobtaxes.FirstOrDefault(x => x.TaxCodeID == supplier.TaxCodeID) : null;
						string taxcode = tax != null ? tax.TaxCode : "";

						foreach (var item in psilist)
						{
							model.ItemCodes.Add(item.itmCode);
							var job = JobList.FirstOrDefault(x => x.JobID == item.JobID);
							item.JobNumber = job != null ? job.JobNumber : "";

							if (supplier != null)
							{
								item.Myob_PaymentIsDue = supplier.PaymentIsDue != null ? (int)supplier.PaymentIsDue : 0;
								item.Myob_BalanceDueDays = supplier.BalanceDueDays != null ? (int)supplier.BalanceDueDays : 0;
								item.Myob_DiscountDays = supplier.DiscountDays != null ? (int)supplier.DiscountDays : 0;
							}

							model.DicPoPurchaseItemList[ps.pstCode].Add(new PurchaseItemModel
							{
								pstId = ps.Id,
								SupplierName = suppliername,
								pstSupplierInvoice = ps.pstSupplierInvoice,
								AccountProfileId = item.AccountProfileId,
								piSeq = item.piSeq,
								pstCode = ps.pstCode,
								itmCode = item.itmCode,
								itmName = item.itmName,
								itmDesc = item.itmDesc,
								itmUseDesc = item.itmUseDesc,
								piBaseUnit = item.piBaseUnit,
								piQty = item.piQty,
								piReceivedQty = item.piReceivedQty,
								piStatus = item.piStatus,
								ivBatCode = item.ivBatCode,
								piHasSN = item.piHasSN,
								piValidThru = item.piValidThru,
								piUnitPrice = item.piUnitPrice,
								piTaxPc = item.piTaxPc,
								piTaxAmt = item.piTaxAmt,
								piDiscPc = item.piDiscPc,
								piAmt = item.piAmt,
								piAmtPlusTax = item.piAmtPlusTax,
								pstLocStock = ps.pstLocStock,
								piStockLoc = item.piStockLoc,
								CreateTime = item.CreateTime,
								ModifyTime = item.ModifyTime,
								pstPurchaseDate = ps.pstPurchaseDate==null? DateTime.Now:ps.pstPurchaseDate,
								pstPromisedDate = ps.pstPromisedDate==null? DateTime.Now.AddDays(1):ps.pstPromisedDate,
								Myob_PaymentIsDue = item.Myob_PaymentIsDue,
								Myob_BalanceDueDays = item.Myob_BalanceDueDays,
								Myob_DiscountDays = item.Myob_DiscountDays,
								IsPartial = ps.pstIsPartial,
								supCode = ps.supCode,
								pstCurrency = ps.pstCurrency,
								pstExRate = Convert.ToDouble(ps.pstExRate),
								pstRemark = ps.pstRemark,
								JobID = item.JobID,
								TaxCode = taxcode,
								JobNumber = item.JobNumber
							});
						}
					}
				}
				#endregion
			}



			if (checkOutType == CheckOutType.Items)
			{
				using var connection = new SqlConnection(DefaultConnection);
				connection.Open();
				model.ItemList = connection.Query<ItemModel>(@"EXEC dbo.GetItemList4Checkout1 @apId=@apId", new { apId = apId }).ToList();
				foreach (var item in model.ItemList)
				{
					model.CheckOutIds_Item.Add(item.itmItemID);
				}
			}
			//if (checkOutType == CheckOutType.PGLocStocks)
			//{
			//    model.LocStockList = GetPGLocStockList(context, apId, companyId, false);
			//    foreach (var item in model.LocStockList)
			//    {
			//        model.CheckOutIds_Stock.Add(item.lstItemLocationID);
			//    }
			//}

			if (checkOutType == CheckOutType.Device)
			{
				model.DeviceList = GetDeviceList4Export(context, apId);
			}
		}


		private static List<DeviceModel> GetDeviceList4Export(MMDbContext context, int accountProfileId)
		{
			List<DeviceModel> DeviceList = new List<DeviceModel>();
			var devices = context.Devices.Where(x => x.dvcIsActive == true && x.AccountProfileId == accountProfileId).ToList();
			foreach (var d in devices)
			{
				var _device = new DeviceModel();
				_device.dvcIsActive = d.dvcIsActive;
				_device.dvcCode = d.dvcCode;
				//_device.dvcName = d.dvcName;
				_device.dvcNextRtlSalesNo = d.dvcNextRtlSalesNo;
				_device.dvcNextRefundNo = d.dvcNextRefundNo;
				_device.dvcNextDepositNo = d.dvcNextDepositNo;
				_device.dvcShop = d.dvcShop;
				DeviceList.Add(_device);
			}
			return DeviceList;
		}


		public static int GetLatestCustomerID(MMDbContext context)
		{
			var apId = GetAccountProfileId(context);
			int pgId = 0;
			int mcId = 0;
			string sql;

			if (!NonABSS)
			{
				sql = $"Select Max(cusCustomerID) From MyobCustomer Where AccountProfileId={apId}";
				mcId = context.Database.SqlQuery<int>(sql).FirstOrDefault();
			}

			return pgId > mcId ? pgId : mcId;
		}


		public static string GetAccountNumber(MMDbContext context, int accountId, int accountProfileId)
		{
			string accountNumber = string.Empty;
			Account account = null;
			account = context.Accounts.FirstOrDefault(x => x.AccountID == accountId && x.AccountProfileId == accountProfileId);
			if (account != null)
			{
				accountNumber = account.AccountNumber;
			}
			return accountNumber;

		}



		public static bool IsItemTaxable(MMDbContext context, string itemcode)
		{
			return context.MyobItems.Any(x => x.itmCode.ToLower() == itemcode.ToLower() && x.itmIsTaxedWhenSold == true);
		}
		public static List<ItemModel> GetTaxableItemList(MMDbContext context, string checkoutportal)
		{
			return (from i in context.MyobItems
					where i.itmIsActive == true && i.itmIsTaxedWhenSold == true && i.AccountProfileId == ComInfo.AccountProfileId
					select new ItemModel
					{
						itmCode = i.itmCode,
						itmName = i.itmName,
						itmDesc = i.itmDesc,
						itmTaxCode = i.itmTaxCode,
						itmTaxPc = i.itmTaxPc
					}
					 ).ToList();

		}
		public static CommonLib.Models.TaxModel GetTaxInfo(SqlConnection connection)
		{
			CommonLib.Models.TaxModel taxModel = new CommonLib.Models.TaxModel();

			var enabletax = connection.QueryFirstOrDefault<OtherSettingsView>(@"EXEC dbo.GetTaxInfo @apId=@apId", new { apId });
			if (enabletax != null)
			{
				taxModel.EnableTax = enabletax.appVal == "1";
			}
			taxModel.TaxType = TaxType.Exclusive;
			taxModel.TIN = ComInfo.TIN;
			return taxModel;
		}
		public static CommonLib.Models.TaxModel GetTaxInfo(MMDbContext context)
		{
			CommonLib.Models.TaxModel taxModel = new CommonLib.Models.TaxModel();
			var enabletax = context.AppParams.FirstOrDefault(x => x.appParam == "EnableTax" && x.AccountProfileId == apId);
			if (enabletax != null)
			{
				taxModel.EnableTax = enabletax.appVal == "1";
			}

			taxModel.TaxType = TaxType.Exclusive;
			taxModel.TIN = ComInfo.TIN;
			return taxModel;
		}
		public static DateTime GetLastSessionTime(MMDbContext context)
		{
			var lastsess = context.Sessions.Where(x => x.sesDateFr != null && x.sesDateTo != null).OrderByDescending(x => x.sesUID).FirstOrDefault();
			if (lastsess != null)
			{
				return (DateTime)lastsess.sesTimeTo;
			}
			return DateTime.MinValue;
		}

		public static DeviceModel GetDevice(int userId, MMDbContext context = null)
		{
			DeviceModel device = new DeviceModel();

			device.dvcShop = ComInfo.Shop;
			device.dvcCode = ComInfo.Device;

			var devlist = context.Devices.Where(x => x.AccountProfileId == ComInfo.AccountProfileId && x.dvcIsActive).ToList();
			Device dev = null;

			dev = devlist.FirstOrDefault(x => x.dvcSalesId == userId);

			//var dev = context.Devices.FirstOrDefault(x => x.dvcIsActive);
			device.dvcUID = dev.dvcUID;
			device.dvcName = dev.dvcName;
			device.AccountNo = (int)dev.AccountNo;
			device.AccountProfileId = dev.AccountProfileId;
			device.dvcStockLoc = dev.dvcStockLoc;

			device.dvcInvoicePrefix = dev.dvcInvoicePrefix;
			device.dvcRefundPrefix = dev.dvcRefundPrefix;
			device.dvcPurchaseRequestPrefix = dev.dvcPurchaseRequestPrefix;
			device.dvcPurchaseOrderPrefix = dev.dvcPurchaseOrderPrefix;
			device.dvcWholesalesPrefix = dev.dvcWholesalesPrefix;
			device.dvcDepositPrefix = dev.dvcDepositPrefix;
			device.dvcPreorderPrefix = dev.dvcPreorderPrefix;

			device.dvcRtlSalesCode = dev.dvcRtlSalesCode;
			device.dvcRtlRefundCode = dev.dvcRtlRefundCode;

			device.dvcPsReturnCode = dev.dvcPsReturnCode;
			device.dvcRtlSalesInitNo = dev.dvcRtlSalesInitNo;
			device.dvcRtlRefundInitNo = dev.dvcPsReturnInitNo;
			device.dvcPurchaseInitNo = dev.dvcPurchaseInitNo;
			device.dvcPsReturnInitNo = dev.dvcPsReturnInitNo;
			device.dvcNextRtlSalesNo = dev.dvcNextRtlSalesNo;
			device.dvcNextRefundNo = dev.dvcNextRefundNo;
			device.dvcNextDepositNo = dev.dvcNextDepositNo;
			device.dvcNextPurchaseRequestNo = dev.dvcNextPurchaseRequestNo;
			device.dvcNextPurchaseOrderNo = dev.dvcNextPurchaseOrderNo;
			device.dvcNextPsReturnNo = dev.dvcNextPsReturnNo;
			device.dvcNextDepositNo = dev.dvcNextDepositNo;
			device.dvcNextPreorderNo = dev.dvcNextPreorderNo;

			device.dvcTransferCode = dev.dvcTransferCode;
			device.dvcNextTransferNo = dev.dvcNextTransferNo;

			device.dvcSalesId = dev.dvcSalesId;
			return device;
		}


		public static int GetCurrentCulture(MMDbContext context = null)
		{
			if (context == null)
			{
				using (context = new MMDbContext())
				{
					return GetCurrentSession(context).sesLang;
				}
			}
			return GetCurrentSession(context).sesLang;

		}


		public static string GetDSNbyMyob(string myobfile)
		{
			using (var context = new MMDbContext())
			{
				var dsn = context.AccountProfiles.FirstOrDefault(x => x.ProfilePath.ToLower().Contains(myobfile.ToLower()));
				if (dsn != null)
				{
					return dsn.DsnName.ToString();
				}
				return "";
			}
		}

		public static List<AccountProfileView> GetAccountProfiles(MMDbContext context)
		{
			return (from d in context.AccountProfiles
					where d.IsActive == true
					select new AccountProfileView
					{
						DsnName = d.DsnName,
						//ProfilePath = d.ProfilePath,
						ProfileName = d.ProfileName,
						//Comment = d.Comment
					}
									 ).ToList();
		}

		public static AccountProfileView GetAccountProfile(int accountProfileId = 0, MMDbContext context = null)
		{
			if (context == null)
			{
				context = new MMDbContext();
			}

			return getAccountProfile(accountProfileId, context);
		}

		private static AccountProfileView getAccountProfile(int accountProfileId, MMDbContext context)
		{
			using (context)
			{
				if (accountProfileId == 0)
				{
					Session currsess = GetCurrentSession(context);
					accountProfileId = (int)currsess.AccountProfileId;
				}

				AccountProfileView av = (from d in context.AccountProfiles
										 where d.IsActive == true && d.Id == accountProfileId
										 select new AccountProfileView
										 {
											 DsnName = d.DsnName,
											 //ProfilePath = d.ProfilePath,
											 ProfileName = d.ProfileName,
											 //Comment = d.Comment
										 }
									 ).FirstOrDefault();
				return av;
			}
		}

		public static string GetMyobFile()
		{
			using (var context = new MMDbContext())
			{
				Session currsess = GetCurrentSession(context);
				//return "";
				var dsnfile = context.AccountProfiles.FirstOrDefault(x => x.DsnName.ToLower().EndsWith(currsess.sesShop.ToLower()));
				return dsnfile.ProfilePath;
			}
		}
		public static List<ItemModel> GetItemStockList(MMDbContext context = null, bool forstocklist = false)
		{
			string location = ComInfo.Shop.ToLower();
			List<ItemModel> nonstockitems = new List<ItemModel>();
			List<ItemModel> stocklist = new List<ItemModel>();
			List<MyobLocStock> mstocks = new List<MyobLocStock>();

			if (forstocklist)
			{
				mstocks = context.MyobLocStocks.Where(st => st.AccountProfileId == AccountProfileId).ToList();
			}
			else
			{
				mstocks = context.MyobLocStocks.Where(st => st.lstStockLoc.ToLower() == location && st.AccountProfileId == AccountProfileId).ToList();
			}


			//stocks must not be cached as it involve quantityavailable, which often change!!!		

			List<ItemModel> mstocklist = (from st in mstocks
										  select new ItemModel
										  {
											  lstItemCode = st.lstItemCode,
											  lstItemID = (int)st.lstItemID,
											  QuantityAvailable = (int)st.lstQuantityAvailable,
											  lstStockLoc = st.lstStockLoc,
										  }
										  ).ToList();
			return mstocklist;
		}

		public static List<ItemModel> GetStockList4Sales(Session currsess, MMDbContext context, int startIndex, int pageSize)
		{
			string location = currsess.sesShop.ToLower();
			//stocks must not be cached as it involve quantityavailable, which often change!!!		

			var stocks = context.MyobLocStocks.Where(x => x.lstStockLoc.ToLower() == location && x.AccountProfileId == currsess.AccountProfileId).OrderBy(x => x.lstItemCode).Skip(startIndex).Take(pageSize).ToList();
			return (from st in stocks
					select new ItemModel
					{
						lstItemCode = st.lstItemCode,
						lstItemID = (int)st.lstItemID,
						QuantityAvailable = (int)st.lstQuantityAvailable,
						lstStockLoc = st.lstStockLoc,
					}
				).ToList();
		}

		public static int GetPGItemCount(MMDbContext context)
		{
			var sql = $"Select count(*) from PGItem where AccountProfileId={ComInfo.AccountProfileId} and itmIsActive=1;";
			return context.Database.SqlQuery<int>(sql).First();
		}
		public static int GetMyobItemCount(int accountProfileId, MMDbContext context)
		{
			var sql = $"Select count(*) from MyobItem where AccountProfileId={accountProfileId} and itmIsActive=1;";
			return context.Database.SqlQuery<int>(sql).First();
		}

		public static string GetShops(SqlConnection connection, ref List<string> shops, ref List<string> shopnames, int apId)
		{
			string primarylocation = "";
			shops = new List<string>();
			List<MyobLocationModel> locationList = connection.Query<MyobLocationModel>(@"EXEC dbo.GetLocationInfo @apId=@apId", new { apId }).ToList();
			if (locationList.Count > 0)
			{
				shops = locationList.Select(x => x.LocationIdentification).Distinct().ToList();
				//primarylocation = locationList.FirstOrDefault(x => (bool)x.IsPrimary).LocationIdentification;
				primarylocation = connection.QueryFirstOrDefault<string>(@"EXEC dbo.GetPrimaryLocation @apId=@apId", new { apId });
				shopnames = locationList.Select(x => x.LocationName).Distinct().ToList();
			}
			return primarylocation;
		}

		public static List<ItemModel> GetItemList4Sales(MMDbContext context)
		{
			return (from i in context.MyobItems.Where(i => i.itmIsActive == true && i.AccountProfileId == apId)
					select new ItemModel
					{
						itmItemID = i.itmItemID,
						itmCode = i.itmCode,
						itmName = i.itmName,
						itmDesc = i.itmDesc,
						itmTaxPc = i.itmTaxPc,
						itmIsNonStock = i.itmIsNonStock,
						itmSupCode = i.itmSupCode,
						itmLastSellingPrice = i.itmBaseSellingPrice == 0 ? i.itmLastUnitPrice : i.itmBaseSellingPrice,
						IncomeAccountID = i.IncomeAccountID,
						InventoryAccountID = i.InventoryAccountID,
						ExpenseAccountID = i.ExpenseAccountID,
						itmIsBought = i.itmIsBought,
						itmIsSold = i.itmIsSold,
						itmUseDesc = i.itmUseDesc,
					}
						 ).ToList();
		}

		public static List<ItemModel> GetMergedItemList(int accountProfileId, MMDbContext context = null)
		{
			return (from i in context.MyobItems.Where(i => i.itmIsActive == true && i.AccountProfileId == accountProfileId)
					select new ItemModel
					{
						itmItemID = i.itmItemID,
						itmCode = i.itmCode,
						itmName = i.itmName,
						itmDesc = i.itmDesc,
						itmTaxPc = i.itmTaxPc,
						itmIsNonStock = i.itmIsNonStock,
						itmSupCode = i.itmSupCode,
						itmLastSellingPrice = i.itmBaseSellingPrice == 0 ? i.itmLastUnitPrice : i.itmBaseSellingPrice,
						IncomeAccountID = i.IncomeAccountID,
						InventoryAccountID = i.InventoryAccountID,
						ExpenseAccountID = i.ExpenseAccountID,
						itmIsBought = i.itmIsBought,
						itmIsSold = i.itmIsSold,
						itmUseDesc = i.itmUseDesc,
						AccountProfileId = accountProfileId,
					}
						 ).ToList();
		}
		public static List<ItemModel> GetItemList(int apId, MMDbContext context, List<GetStockInfo6_Result> stockinfo, int startIndex, int pageSize, out int totalCount, string keyword = "", string location = "", string type = null)
		{
			var items = getItemList(apId, context, stockinfo, startIndex, pageSize, out totalCount, keyword, location, type);
			return items;
		}

		public static List<ItemModel> getItemList(int accountProfileId, MMDbContext context, List<GetStockInfo6_Result> stockinfo, int startIndex, int pageSize, out int totalCount, string keyword = "", string location = "", string type = null)
		{
			List<ItemModel> items = new List<ItemModel>();

			int apId = accountProfileId;

			var itempromotionInfo = context.GetItemPromotionList4Sales1().ToList();
			var groupedIPList = itempromotionInfo.GroupBy(x => x.itemCode).ToList();

			totalCount = context.MyobItems.Where(x => x.AccountProfileId == accountProfileId && x.itmIsActive).Count();

			if (string.IsNullOrEmpty(location)) //stock info
			{
				var _items = context.GetItemList4Stock7(accountProfileId, startIndex, pageSize, keyword).ToList();
				HashSet<string> itemcodelist = _items.Select(x => x.itmCode).Distinct().ToHashSet();
				var onhandstockInfo = context.GetOnHandStockByCodes4(string.Join(",", itemcodelist), apId).ToList();

				foreach (var i in _items)
				{
					var onhandstock = onhandstockInfo.FirstOrDefault(x => x.lstItemCode == i.itmCode);

					int sbqty = (onhandstock == null || onhandstock.OnHandStock == null) ? 0 : (int)onhandstock.OnHandStock;
					int abssqty = (onhandstock == null || onhandstock.AbssStock == null) ? 0 : (int)onhandstock.AbssStock;

					string itemcode = i.itmCode;

					var salesitem = new ItemModel
					{
						itmItemID = i.itmItemID,
						itmCode = i.itmCode,
						itmName = i.itmName,
						itmDesc = i.itmDesc,
						itmUseDesc = i.itmUseDesc,
						itmTaxPc = i.itmTaxPc,
						itmIsNonStock = i.itmIsNonStock,
						itmSupCode = i.itmSupCode,
						itmBaseSellingPrice = i.itmBaseSellingPrice,
						itmLastUnitPrice = i.itmLastUnitPrice,
						itmLastSellingPrice = i.itmLastSellingPrice,
						itmIsTaxedWhenSold = i.itmIsTaxedWhenSold,
						AccountProfileId = accountProfileId,
						IncomeAccountID = (int)i.IncomeAccountID,
						InventoryAccountID = (int)i.InventoryAccountID,
						ExpenseAccountID = (int)i.ExpenseAccountID,
						itmSellUnit = i.itmSellUnit,
						itmBuyUnit = i.itmBuyUnit,
						OnHandStock = sbqty,
						Qty = sbqty,
						QuantityAvailable = sbqty,
						AbssQty = abssqty,
						lstStockLoc = "",
						chkBat = i.chkBat ?? false,
						chkSN = i.chkSN ?? false,
						chkVT = i.chkVT ?? false,
						itmBuyStdCost = i.itmBuyStdCost,
						hasItemVari = i.ivId > 0,
					};

					GetItemPromotions(groupedIPList, itemcode, ref salesitem);
					salesitem.DicItemLocQty[i.itmCode] = new Dictionary<string, int>();
					salesitem.DicItemAbssQty[i.itmCode] = new Dictionary<string, int>();
					items.Add(salesitem);
				}

				getStockInfo(stockinfo, items);
			}
			else //for sales
			{
				var _items = context.GetItemList5(accountProfileId, location, startIndex, pageSize, keyword).ToList();
				foreach (var i in _items)
				{
					var stocks = stockinfo.Where(x => x.lstItemCode == i.itmCode);
					int qty = stocks == null ? 0 : stocks.Sum(x => (int)x.lstQuantityAvailable);

					var salesitem = new ItemModel
					{
						itmItemID = i.itmItemID,
						itmCode = i.itmCode,
						itmName = i.itmName,
						itmDesc = i.itmDesc,
						itmTaxPc = i.itmTaxPc,
						itmIsNonStock = i.itmIsNonStock,
						itmSupCode = i.itmSupCode,
						itmBaseSellingPrice = i.itmBaseSellingPrice,
						itmLastUnitPrice = i.itmLastUnitPrice,
						itmLastSellingPrice = i.itmLastSellingPrice,
						itmUseDesc = i.itmUseDesc,
						itmIsTaxedWhenSold = i.itmIsTaxedWhenSold,
						itmIsTaxedWhenBought = i.itmIsTaxedWhenBought,
						itmTaxExclusiveLastPurchasePrice = i.itmTaxExclusiveLastPurchasePrice,
						itmTaxInclusiveLastPurchasePrice = i.itmTaxInclusiveLastPurchasePrice,
						itmTaxExclusiveStandardCost = i.itmTaxExclusiveStandardCost,
						itmTaxInclusiveStandardCost = i.itmTaxInclusiveStandardCost,
						AccountProfileId = accountProfileId,
						IncomeAccountID = (int)i.IncomeAccountID,
						InventoryAccountID = (int)i.InventoryAccountID,
						ExpenseAccountID = (int)i.ExpenseAccountID,
						itmSellUnit = i.itmSellUnit,
						itmBuyUnit = i.itmBuyUnit,
						Qty = qty,
						QuantityAvailable = qty,
						lstStockLoc = location,
						chkBat = i.chkBat ?? false,
						chkSN = i.chkSN ?? false,
						chkVT = i.chkVT ?? false,
						itmBuyStdCost = i.itmBuyStdCost,
						hasItemVari = i.ivId > 0,
					};

					GetItemPromotions(groupedIPList, i.itmCode, ref salesitem);
					salesitem.DicItemLocQty[i.itmCode] = new Dictionary<string, int>();
					salesitem.DicItemAbssQty[i.itmCode] = new Dictionary<string, int>();
					if (string.IsNullOrEmpty(type))
					{
						items.Add(salesitem);
					}
					else
					{
						if (type.ToLower() == "order")
						{
							items.Add(salesitem);
						}
						if (type.ToLower() == "preorder")
						{
							if (qty <= 0)
							{
								items.Add(salesitem);
							}
						}
					}
				}
				getStockInfo(stockinfo, items);
			}
			return items;
		}

		private static void getStockInfo(List<GetStockInfo6_Result> stockinfo, List<ItemModel> items)
		{
			foreach (var item in items)
			{
				var gst = stockinfo.Where(x => x.lstItemCode == item.itmCode).ToList();
				//var dicLocQty = new Dictionary<string, int>();                    
				int currentStockQty = 0;
				item.JsStockList = new List<JsStock>();
				if (gst != null && gst.Count > 0)
				{
					foreach (var g in gst)
					{
						//dicLocQty[g.lstStockLoc] = g.lstQuantityAvailable;
						item.DicItemLocQty[item.itmCode][g.lstStockLoc] = g.lstQuantityAvailable ?? 0;
						item.DicItemAbssQty[item.itmCode][g.lstStockLoc] = g.lstAbssQty ?? 0;
						currentStockQty += (int)g.lstQuantityAvailable;

						item.JsStockList.Add(new JsStock
						{
							LocCode = g.lstStockLoc,
							Id = g.Id
						});
					}
					item.OutOfBalance = item.OnHandStock - currentStockQty;
					//item.OutOfBalance = item.AbssQty - item.OnHandStock;
				}
			}
		}

		private static string[] GetPromotionNameDescDisplay(GetItemPromotionList4Sales1_Result promotion)
		{
			List<string> prolist = new List<string>();
			switch (CultureHelper.CurrentCulture)
			{
				case 2:
					prolist.Add(promotion.proName);
					prolist.Add(promotion.proDesc);
					break;
				case 1:
					prolist.Add(promotion.proNameSC);
					prolist.Add(promotion.proDescSC);
					break;
				default:
				case 0:
					prolist.Add(promotion.proNameTC);
					prolist.Add(promotion.proDescTC);
					break;
			}
			return prolist.ToArray();
		}

		private static void GetItemPromotions(List<IGrouping<string, GetItemPromotionList4Sales1_Result>> groupedIPList, string itemcode, ref ItemModel salesitem)
		{
			salesitem.ItemPromotions = new List<Models.Promotion.ItemPromotionModel>();
			foreach (var group in groupedIPList)
			{
				var ipitem = group.FirstOrDefault();
				if (ipitem.itemCode == itemcode)
				{
					foreach (var g in group)
					{
						var namedesc = GetPromotionNameDescDisplay(g);
						salesitem.ItemPromotions.Add(new Models.Promotion.ItemPromotionModel
						{
							proId = g.proId,
							itemCode = g.itemCode,
							CreateTime = g.CreateTime,
							ModifyTime = g.ModifyTime,
							proDateFrm = g.proDateFrm,
							proDateTo = g.proDateTo,
							proQty = g.proQty,
							proPrice = g.proPrice,
							proDiscPc = g.proDiscPc,
							pro4Period = g.pro4Period,
							NameDisplay = namedesc[0],
							DescDisplay = namedesc[1]
						});
					}
				}
			}
		}
		private static void GetItemPromotions(List<IGrouping<string, GetItemPromotionList4Sales1_Result>> groupedIPList, string itemcode, ref ItemVariModel iv)
		{
			iv.ItemPromotions = new List<Models.Promotion.ItemPromotionModel>();
			foreach (var group in groupedIPList)
			{
				var ipitem = group.FirstOrDefault();
				if (ipitem.itemCode == itemcode)
				{
					foreach (var g in group)
					{
						var namedesc = GetPromotionNameDescDisplay(g);
						iv.ItemPromotions.Add(new Models.Promotion.ItemPromotionModel
						{
							proId = g.proId,
							itemCode = g.itemCode,
							CreateTime = g.CreateTime,
							ModifyTime = g.ModifyTime,
							proDateFrm = g.proDateFrm,
							proDateTo = g.proDateTo,
							proQty = g.proQty,
							proPrice = g.proPrice,
							proDiscPc = g.proDiscPc,
							pro4Period = g.pro4Period,
							NameDisplay = namedesc[0],
							DescDisplay = namedesc[1]
						});
					}
				}
			}
		}
		public static void GetItemPriceLevelList(ref List<ItemModel> itemlist)
		{
			SqlConnection connection;
			int validitemscount;
			getValidItemsCount(out connection, out validitemscount);

			if (validitemscount > 0)
			{
				var itemIds = string.Join(",", itemlist.Select(x => x.itmItemID).ToList());
				List<ItemPriceModel> itemprices = getItemPriceList(itemIds, connection);

				if (itemlist.Count > 0 && itemprices.Count() > 0)
				{
					foreach (var item in itemlist)
					{
						foreach (var itemprice in itemprices)
						{
							if (item.itmItemID == itemprice.ItemID)
							{
								if (itemprice.PriceLevel == "A")
								{
									item.PLA = itemprice.SellingPrice;
								}
								if (itemprice.PriceLevel == "B")
								{
									item.PLB = itemprice.SellingPrice;
								}
								if (itemprice.PriceLevel == "C")
								{
									item.PLC = itemprice.SellingPrice;
								}
								if (itemprice.PriceLevel == "D")
								{
									item.PLD = itemprice.SellingPrice;
								}
								if (itemprice.PriceLevel == "E")
								{
									item.PLE = itemprice.SellingPrice;
								}
								if (itemprice.PriceLevel == "F")
								{
									item.PLF = itemprice.SellingPrice;
								}
							}
						}
					}
				}
			}
		}

		private static List<ItemPriceModel> getItemPriceList(string itemIds, SqlConnection connection)
		{
			var itemprices = connection.Query<ItemPriceModel>(@"EXEC dbo.GetItemPricesByIdsCodes @apId=@apId,@itemIds=@itemIds", new { apId, itemIds }).ToList();
			return itemprices;
		}

		private static void getValidItemsCount(out SqlConnection connection, out int validitemscount)
		{
			connection = new SqlConnection(DefaultConnection);
			connection.Open();
			validitemscount = connection.QueryFirstOrDefault<int>(@"EXEC dbo.GetValidItemPriceCount2 @apId=@apId", new { apId });
		}

		public static ReceiptViewModel GetReceipt(MMDbContext context, CommonLib.Models.TaxModel taxModel = null)
		{
			Session currsess = GetCurrentSession(context);

			ReceiptViewModel receipt = (from r in context.Receipts
										where r.deviceCode.ToLower() == ComInfo.Device.ToLower() && r.shopCode.ToLower() == ComInfo.Shop.ToLower() && r.AccountProfileId == ComInfo.AccountProfileId
										select new ReceiptViewModel
										{
											Id = r.Id,
											HeaderTitle = r.HeaderTitle + ":" + r.HeaderTitleCN + ":" + r.HeaderTitleEng,
											HeaderMessage = r.HeaderMessage + ":" + r.HeaderMessageCN + ":" + r.HeaderMessageEng,
											FooterMessage = r.FooterMessage + ":" + r.FooterMessageCN + ":" + r.FooterMessageEng,
											FooterTitle1 = r.FooterTitle1 + ":" + r.FooterTitle1CN + ":" + r.FooterTitle1Eng,
											FooterTitle2 = r.FooterTitle2 + ":" + r.FooterTitle2CN + ":" + r.FooterTitle2Eng,
											FooterTitle3 = r.FooterTitle3 + ":" + r.FooterTitle3CN + ":" + r.FooterTitle3Eng,
											CompanyAddress = r.CompanyAddress + ":" + r.CompanyAddressCN + ":" + r.CompanyAddressEng,
											CompanyAddress1 = r.CompanyAddress1 + ":" + r.CompanyAddress1CN + ":" + r.CompanyAddress1Eng,
											CompanyName = r.CompanyName + ":" + r.CompanyNameCN + ":" + r.CompanyNameEng,
											CompanyPhone = r.CompanyPhone,
											CompanyWebSite = r.CompanyWebSite,
											//Disclaimer = r.Disclaimer + ":" + r.DisclaimerCN + ":" + r.DisclaimerEng
										}).FirstOrDefault();

			if (receipt != null)
			{
				receipt.Disclaimers = (from ri in context.ReceiptInfoes
									   where ri.reId == receipt.Id
									   select ri.DisclaimerTxt + ":" + ri.DisclaimerTxtCN + ":" + ri.DisclaimerTxtEng).ToList();

				receipt.PaymentTerms = (from ri in context.ReceiptInfoes
										where ri.reId == receipt.Id
										select ri.PaymentTermsTxt + ":" + ri.PaymentTermsTxtCN + ":" + ri.PaymentTermsTxtEng).ToList();

				int lang = CultureHelper.CurrentCulture;

				receipt.HeaderTitle = receipt.HeaderTitle.Split(':')[lang];
				receipt.HeaderMessage = receipt.HeaderMessage.Split(':')[lang];
				receipt.FooterMessage = receipt.FooterMessage.Split(':')[lang];
				receipt.FooterTitle1 = receipt.FooterTitle1.Split(':')[lang];
				receipt.FooterTitle2 = receipt.FooterTitle2.Split(':')[lang];
				receipt.FooterTitle3 = receipt.FooterTitle3.Split(':')[lang];
				receipt.CompanyAddress = receipt.CompanyAddress.Split(':')[lang];
				receipt.CompanyAddress1 = receipt.CompanyAddress1.Split(':')[lang];
				receipt.CompanyName = receipt.CompanyName.Split(':')[lang];

				receipt.DisclaimerTxtList = new List<string>();
				receipt.PaymentTermsTxtList = new List<string>();
				foreach (var disclaimer in receipt.Disclaimers)
				{
					receipt.DisclaimerTxtList.Add(disclaimer.Split(':')[lang]);
				}
				foreach (var pt in receipt.PaymentTerms)
				{
					receipt.PaymentTermsTxtList.Add(pt.Split(':')[lang]);
				}

				receipt.Disclaimer = string.Join("<br/>", receipt.DisclaimerTxtList);
			}
			return receipt;
		}

		public static Dictionary<string, string> GetDicAR(MMDbContext context = null, int lang = -1)
		{
			if (lang == -1)
			{
				lang = (int)HttpContext.Current.Session["CurrentCulture"];
			}

			if (context == null)
			{
				using (context = new MMDbContext())
				{
					return getDicAR(context, lang);
				}
			}
			else
			{
				return getDicAR(context, lang);
			}

		}

		private static Dictionary<string, string> getDicAR(MMDbContext context, int lang)
		{
			//List<SysFunc> funcs = new List<SysFunc>();
			//funcs = context.SysFuncs.Where(x => x.sfnSettings == true && x.Assignable == true).ToList();
			var funcs = context.GetDefaultAccessRights("staff01").ToList();
			Dictionary<string, string> dicAR = new Dictionary<string, string>();

			switch (lang)
			{
				case 2:
					foreach (var func in funcs)
					{
						if (!dicAR.ContainsKey(func.FuncCode))
							dicAR.Add(func.FuncCode, func.sfnNameEng);
					}
					break;
				case 1:
					foreach (var func in funcs)
					{
						if (!dicAR.ContainsKey(func.FuncCode))
							dicAR.Add(func.FuncCode, func.sfnNameChs);
					}
					break;
				default:
				case 0:
					foreach (var func in funcs)
					{
						if (!dicAR.ContainsKey(func.FuncCode))
							dicAR.Add(func.FuncCode, func.sfnNameCht);
					}
					break;
			}

			return dicAR;
		}

		public static List<DeviceModel> GetDeviceList(MMDbContext context)
		{
			return (from d in context.Devices
					where d.dvcIsActive == true && d.AccountProfileId == ComInfo.AccountProfileId
					select new DeviceModel
					{
						dvcUID = d.dvcUID,
						dvcIsActive = d.dvcIsActive,
						dvcCode = d.dvcCode,
						dvcNextRtlSalesNo = d.dvcNextRtlSalesNo,
						dvcNextRefundNo = d.dvcNextRefundNo,
						dvcShop = d.dvcShop,
						dvcStockLoc = d.dvcStockLoc,
						dvcReceiptPrinter = d.dvcReceiptPrinter,
						dvcDayEndPrinter = d.dvcDayEndPrinter,
						dvcDefaultCusRefund = d.dvcDefaultCusRefund,
						dvcDefaultCusSales = d.dvcDefaultCusSales,
						dvcRmks = d.dvcRmks,
						dvcShopName = d.dvcShopName,
						dvcShopInfo = d.dvcShopInfo,
						dvcName = d.dvcName,
						dvcIP = d.dvcIP,
						dvcInvoicePrefix = d.dvcInvoicePrefix,
						dvcRefundPrefix = d.dvcRefundPrefix,
						AccountProfileId = d.AccountProfileId,
						AccountNo = (int)d.AccountNo,
						dvcRtlSalesCode = d.dvcRtlSalesCode,
						dvcRtlRefundCode = d.dvcRtlRefundCode,
						dvcPurchaseRequestPrefix = d.dvcPurchaseRequestPrefix,
						dvcPurchaseOrderPrefix = d.dvcPurchaseOrderPrefix,
						dvcPsReturnCode = d.dvcPsReturnCode,
						dvcRtlSalesInitNo = d.dvcRtlSalesInitNo,
						dvcRtlRefundInitNo = d.dvcRtlRefundInitNo,
						dvcPurchaseInitNo = d.dvcPurchaseInitNo,
						dvcPsReturnInitNo = d.dvcPsReturnInitNo
					}
												  ).ToList();
		}

		public static ComInfoView GetCompanyInfo(MMDbContext context)
		{
			return (from c in context.ComInfoes
					select new ComInfoView
					{
						comName = c.comName,
						comEmail = c.comEmail,
						comAddress1 = c.comAddress1,
						comAddress2 = c.comAddress2,
						comAccountNo = c.comAccountNo
					}
								   ).FirstOrDefault();
		}
		public static Session GetCurrentSession(MMDbContext context = null, string usercode = "")
		{
			if (HttpContext.Current.Session["Session"] == null) //the worst case...the following code is just for Contingency measures
			{
				DateTime frmDate = DateTime.Now.Date;
				// var session = context.GetCurrentPCSession(usercode, frmDate, user.Device.dvcCode, user.Device.dvcShop).FirstOrDefault(); don't use this code!!!
				if (string.IsNullOrEmpty(usercode))
				{
					Session session = (from s in context.Sessions
									   where s.sesDateFr == frmDate && s.sesIsActive == true
									   orderby s.sesUID descending
									   select s
									 ).FirstOrDefault();
					return session;
				}
				else
				{
					Session session = (from s in context.Sessions
									   where s.UserCode == usercode && s.sesDateFr == frmDate && s.sesIsActive == true
									   orderby s.sesUID descending
									   select s
									 ).FirstOrDefault();
					return session;
				}
			}
			else
			{
				return HttpContext.Current.Session["Session"] as Session;
			}
		}


		public static DeviceModel GetDeviceInfo(LoginUserModel model)
		{
			DeviceModel device = new DeviceModel();
			string infotxt = MMCommonLib.CommonHelpers.FileHelper.Read(device.DeviceInfoFileName);
			if (!string.IsNullOrEmpty(infotxt))
			{
				string[] deviceinfo = infotxt.Split(new string[] { ";;" }, StringSplitOptions.None);
				string hashdevice = deviceinfo[0];
				string hashshop = deviceinfo[1];

				if (HashHelper.ComputeHash(model.SelectedDevice) == hashdevice && HashHelper.ComputeHash(model.SelectedShop) == hashshop)
				{
					device.dvcShop = model.SelectedShop;
					device.dvcCode = model.SelectedDevice;
					return device;
				}
			}
			return null;
		}

		public static string GetDSNByAccountProfileId(MMDbContext context, int accountprofileId)
		{
			return context.AccountProfiles.FirstOrDefault(x => x.Id == accountprofileId).DsnName;
		}

		public static void WriteActionLog(ActionLogModel model, MMDbContext context = null)
		{
			if (context == null)
			{
				using (context = new MMDbContext())
				{
					writeActionLog(model, context);
				}
			}
			else
			{
				writeActionLog(model, context);
			}
		}

		private static void writeActionLog(ActionLogModel model, MMDbContext context)
		{
			context.AddActionLog(model.actUserCode, model.actName, model.actType, model.actOldValue, model.actNewValue, model.actRemark, model.actLogTime, model.actCusCode, model.actCustomerId, model.AccountProfileId, model.actContactId);
			context.SaveChanges();
		}

		public static string GetShopCode(MMDbContext context, bool tolowerstring = true)
		{
			Session currsess = GetCurrentSession(context);
			return tolowerstring ? currsess.sesShop.ToLower() : currsess.sesShop;
		}
		public static void WriteLog(MMDbContext context, string message, string type)
		{
			DebugLog debugLog = new DebugLog();
			writelog(message, type, ref debugLog);
			context.DebugLogs.Add(debugLog);
		}
		private static void writelog(string message, string type, ref DebugLog debugLog)
		{
			debugLog.Message = message;
			debugLog.LogType = type;
			debugLog.CreateTime = CommonHelper.GetLocalTime();
		}

		public static Dictionary<string, DeviceModel> GetDicDeviceInfo(MMDbContext context)
		{
			Dictionary<string, DeviceModel> DicDeviceInfo = new Dictionary<string, DeviceModel>();
			var devicelist = (from d in context.Devices
							  join ap in context.AccountProfiles
							  on d.AccountProfileId equals ap.Id
							  where d.dvcIsActive == true
							  select new DeviceModel
							  {
								  AccountProfileName = ap.ProfileName,
								  AccountNo = (int)d.AccountNo,
								  dvcInvoicePrefix = d.dvcInvoicePrefix,
								  dvcRefundPrefix = d.dvcRefundPrefix
							  }
							  ).ToList();

			foreach (var device in devicelist)
			{
				if (!DicDeviceInfo.ContainsKey(device.AccountProfileName))
				{
					DicDeviceInfo.Add(device.AccountProfileName, device);
				}
			}
			return DicDeviceInfo;
		}

		public static Dictionary<int, int> GetDicAcNo(MMDbContext context)
		{
			Dictionary<int, int> DicAcNo = new Dictionary<int, int>();
			var devicelist = (from d in context.Devices
							  join ap in context.AccountProfiles
							  on d.AccountProfileId equals ap.Id
							  where d.dvcIsActive == true
							  select new DeviceModel
							  {
								  AccountProfileId = (int)d.AccountProfileId,
								  AccountNo = (int)d.AccountNo
							  }
							  ).ToList();

			foreach (var device in devicelist)
			{
				if (!DicAcNo.ContainsKey(device.AccountProfileId))
				{
					DicAcNo.Add(device.AccountProfileId, (int)device.AccountNo);
				}
			}
			return DicAcNo;
		}


		public static int GetAccountProfileIdByDSN(MMDbContext context, string dsn)
		{
			return context.AccountProfiles.FirstOrDefault(x => x.DsnName.ToLower() == dsn.ToLower()).Id;
		}



		public static int GetPosAdminID(DeviceModel device)
		{
			return -1;
		}

		public static AbssConn GetCompanyProfiles(int selectedProfileId)
		{
			using var context = new MMDbContext();
			var dto = (from a in context.AccountProfiles
					   join c in context.ComInfoes
					   on a.Id equals c.AccountProfileId
					   where a.Id == selectedProfileId && a.IsActive == true
					   select new AccountProfileDTO
					   {
						   Id = a.Id,
						   ProfileName = a.ProfileName,
						   CompanyId = a.CompanyId,
						   MYOBDriver = c.MYOBDriver,
						   MYOBUID = c.MYOBUID,
						   MYOBPASS = c.MYOBPASS,
						   MYOBDB = c.MYOBDb,
						   MYOBKey = c.MYOBKey,
						   MYOBExe = c.MYOBExe
					   }
						  ).FirstOrDefault();
			AbssConn abssConn = new AbssConn
			{
				Database = dto.MYOBDB,
				Driver = dto.MYOBDriver,
				UserId = dto.MYOBUID,
				Password = dto.MYOBPASS,
				KeyLocation = dto.MYOBKey,
				ExeLocation = dto.MYOBExe
			};
			return abssConn;
		}

		public static void SaveStocksToDB(int selectedProfileId, List<AddOnItem> inventories)
		{
			DateTime dateTime = DateTime.Now;
			List<MyobLocStock> stocks = new List<MyobLocStock>();
			using var context = new MMDbContext();
			var currentstocks = context.MyobLocStocks.Where(x => x.AccountProfileId == selectedProfileId).ToList();

			foreach (var item in inventories)
			{
				var qtyav = decimal.ToInt32((decimal)item.QuantityAvailable);
				var sellorder = decimal.ToInt32((decimal)item.SellOnOrder);
				var purchaseorder = decimal.ToInt32((decimal)item.PurchaseOnOrder);
				var qtyhand = decimal.ToInt32((decimal)item.QuantityOnHand);
				string Id = CommonHelper.GenerateNonce(50, false);
				MyobLocStock locStock = new MyobLocStock
				{
					Id = Id,
					lstItemLocationID = item.ItemLocationID,
					lstItemID = item.ItemID,
					lstItemCode = item.ItemNumber,
					lstStockLoc = item.LocationName,
					lstQuantityAvailable = qtyav,
					lstCreateTime = dateTime,
					lstModifyTime = dateTime,
					AccountProfileId = selectedProfileId,
					//CompanyId = companyId
				};
				locStock.lstQuantityAvailable = qtyhand;
				stocks.Add(locStock);
			}

			#region remove current records first:   
			if (currentstocks.Count > 0)
			{
				context.MyobLocStocks.RemoveRange(currentstocks);
				context.SaveChanges();
			}
			#endregion
			#region add records:          
			context.MyobLocStocks.AddRange(stocks);
			context.SaveChanges();
			#endregion
		}



		public static void SaveItemVariations(MMDbContext context, ItemModel myobItem = null)
		{
			DateTime dateTime = DateTime.Now;
			List<ItemVariation> variations = new List<ItemVariation>();
			if (myobItem != null)
			{
				#region remove current records first:
				var iaIds = myobItem.SelectedAttrList4V.Select(x => x.Id).ToArray();
				var ianames = myobItem.SelectedAttrList4V.Select(x => x.iaName).ToArray();
				var iavals = myobItem.SelectedAttrList4V.Select(x => x.iaValue).ToArray();
				var iavars = context.ItemVariations.Where(x => iaIds.Contains((long)x.iaId) && ianames.Contains(x.iaName) && iavals.Contains(x.iaValue)).ToList();
				if (iavars != null)
				{
					context.ItemVariations.RemoveRange(iavars);
					context.SaveChanges();
				}
				#endregion

				#region add records:
				foreach (var attr in myobItem.SelectedAttrList4V)
				{
					variations.Add(new ItemVariation
					{
						iaId = attr.Id,
						iaName = attr.iaName,
						iaValue = attr.iaValue,
						itemID = myobItem.itmItemID,
						AccountProfileId = apId,
						CreateTime = dateTime
					});
				}
				#endregion
			}

			context.ItemVariations.AddRange(variations);
			context.SaveChanges();
		}

		private static void UpdateCategory(MMDbContext context, int apId, ItemModel myobItem)
		{

			if (myobItem != null)
			{
				ItemCategory category = context.ItemCategories.FirstOrDefault(x => x.itmCode == myobItem.itmCode && x.AccountProfileId == apId);
				category.catId = (int)myobItem.catId;
			}
		}

		public static void UpdateItemStockInfo(MMDbContext context, DateTime dateTime, ItemVariation iv, ItemModel MyobItem)
		{
			if (MyobItem != null && !MyobItem.itmIsNonStock)
			{
				List<LocStock4ItemVariation> newstocklist = new List<LocStock4ItemVariation>();
				foreach (var key in MyobItem.DicLocQty.Keys)
				{
					LocStock4ItemVariation locStock = context.LocStock4ItemVariation.FirstOrDefault(x => x.ivId == iv.Id && x.lstStockLoc == key);

					if (locStock != null)
					{
						locStock.lstQuantityAvailable = MyobItem.DicLocQty[key];
						locStock.ModifyTime = dateTime;
					}
					else
					{
						newstocklist.Add(new LocStock4ItemVariation
						{
							ivId = iv.Id,
							lstItemCode = MyobItem.itmCode,
							lstStockLoc = key,
							lstQuantityAvailable = MyobItem.DicLocQty[key],
							CreateTime = dateTime,
							ModifyTime = dateTime,
							AccountProfileId = AccountProfileId,
						});
					}
				}
				if (newstocklist.Count > 0)
				{
					context.LocStock4ItemVariation.AddRange(newstocklist);
				}
				context.SaveChanges();
			}
		}

		public static void UpdateItemPrices(ItemVariModel ItemVari, MMDbContext context, DateTime dateTime, string itemcode, ItemVariation iv, ItemModel MyobItem = null)
		{
			#region remove current records first, if any...                    
			List<ItemPrice4ItemVariation> itemPrices = context.ItemPrice4ItemVariation.Where(x => x.ivId == iv.Id && x.ItemCode == itemcode && x.AccountProfileId == AccountProfileId).ToList();
			if (itemPrices != null && itemPrices.Count > 0)
			{
				context.ItemPrice4ItemVariation.RemoveRange(itemPrices);
				context.SaveChanges();
			}
			#endregion

			#region add records
			if (ItemVari != null)
			{
				itemPrices = new List<ItemPrice4ItemVariation>
					{
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = ItemVari.itmCode,
							QuantityBreak = 1,
							PriceLevel = "A",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLA",
							SellingPrice = ItemVari.PLA,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = ItemVari.itmCode,
							QuantityBreak = 1,
							PriceLevel = "B",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLB",
							SellingPrice = ItemVari.PLB,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						 new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = ItemVari.itmCode,
							QuantityBreak = 1,
							PriceLevel = "C",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLC",
							SellingPrice = ItemVari.PLC,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = ItemVari.itmCode,
							QuantityBreak = 1,
							PriceLevel = "D",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLD",
							SellingPrice = ItemVari.PLD,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = ItemVari.itmCode,
							QuantityBreak = 1,
							PriceLevel = "E",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLE",
							SellingPrice = ItemVari.PLE,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = ItemVari.itmCode,
							QuantityBreak = 1,
							PriceLevel = "F",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLF",
							SellingPrice = ItemVari.PLF,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						}
					};
			}
			if (MyobItem != null)
			{
				itemPrices = new List<ItemPrice4ItemVariation>
					{
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = MyobItem.itmCode,
							QuantityBreak = 1,
							PriceLevel = "A",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLA",
							SellingPrice = MyobItem.PLA,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = MyobItem.itmCode,
							QuantityBreak = 1,
							PriceLevel = "B",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLB",
							SellingPrice = MyobItem.PLB,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						 new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = MyobItem.itmCode,
							QuantityBreak = 1,
							PriceLevel = "C",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLC",
							SellingPrice = MyobItem.PLC,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = MyobItem.itmCode,
							QuantityBreak = 1,
							PriceLevel = "D",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLD",
							SellingPrice = MyobItem.PLD,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = MyobItem.itmCode,
							QuantityBreak = 1,
							PriceLevel = "E",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLE",
							SellingPrice = MyobItem.PLE,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						},
						new ItemPrice4ItemVariation
						{
							ivId = iv.Id,
							ItemCode = MyobItem.itmCode,
							QuantityBreak = 1,
							PriceLevel = "F",
							AccountProfileId = AccountProfileId,
							PriceLevelNameID = "PLF",
							SellingPrice = MyobItem.PLF,
							CreateTime = dateTime,
							ModifyTime = dateTime,
						}
					};
			}

			context.ItemPrice4ItemVariation.AddRange(itemPrices);
			context.SaveChanges();
			#endregion
		}

		public static void EditIV(ItemVariModel ItemVari, List<ItemAttributeModel> AttrList, ItemModel MyobItem)
		{
			using (var context = new MMDbContext())
			{
				using (var transaction = context.Database.BeginTransaction())
				{
					DateTime dateTime = DateTime.Now;

					#region Get Current IV, if any...
					List<string> comboIvIds = new List<string>();
					string comboIvId = string.Empty;
					string itemcode = ItemVari != null ? ItemVari.itmCode : MyobItem.itmCode;
					if (ItemVari != null && ItemVari.SelectedAttrList4V != null && ItemVari.SelectedAttrList4V.Count > 0)
					{
						foreach (var attr in ItemVari.SelectedAttrList4V)
						{
							var _iv = context.ItemVariations.FirstOrDefault(x => x.itmCode == ItemVari.itmCode && x.comboIvId == null && x.iaName == attr.iaName && x.iaValue == attr.iaValue && x.AccountProfileId == AccountProfileId);
							if (_iv != null)
							{
								comboIvIds.Add(_iv.Id.ToString());
							}
						}
					}

					if (MyobItem != null && MyobItem.SelectedAttrList4V != null && MyobItem.SelectedAttrList4V.Count > 0)
					{
						foreach (var attr in MyobItem.SelectedAttrList4V)
						{
							var _iv = context.ItemVariations.FirstOrDefault(x => x.itmCode == MyobItem.itmCode && x.comboIvId == null && x.iaName == attr.iaName && x.iaValue == attr.iaValue && x.AccountProfileId == AccountProfileId);
							if (_iv != null)
							{
								comboIvIds.Add(_iv.Id.ToString());
							}
						}
					}

					comboIvId = string.Join("|", comboIvIds);
					var currentIV = context.ItemVariations.FirstOrDefault(x => x.itmCode == itemcode && x.comboIvId == comboIvId && x.AccountProfileId == AccountProfileId);
					#endregion

					#region remove current record first, if any...
					if (currentIV != null)
					{
						context.ItemVariations.Remove(currentIV);
						context.SaveChanges();
					}
					#endregion

					#region add record:
					ItemVariation iv = null;
					if (ItemVari != null)
					{
						iv = new ItemVariation
						{
							iaId = 0,
							comboIvId = comboIvId,
							iaName = null,
							iaValue = null,
							itemID = ItemVari.itemID,
							AccountProfileId = AccountProfileId,
							CreateTime = DateTime.Now,
							ModifyTime = DateTime.Now,
						};
					}
					if (MyobItem != null)
					{
						iv = new ItemVariation
						{
							iaId = 0,
							comboIvId = comboIvId,
							iaName = null,
							iaValue = null,
							itemID = MyobItem.itmItemID,
							AccountProfileId = AccountProfileId,
							CreateTime = DateTime.Now,
							ModifyTime = DateTime.Now,
						};
					}
					context.ItemVariations.Add(iv);
					context.SaveChanges();
					#endregion

					#region Edit/Add Attr:
					if (AttrList != null && AttrList.Count > 0)
					{
						if (MyobItem != null)
						{
							var _Item = context.MyobItems.AsNoTracking().FirstOrDefault(x => x.itmCode == ItemVari.itmCode && x.AccountProfileId == AccountProfileId);
							SaveItemAttributes(context, AttrList, _Item);
						}
					}
					#endregion

					#region Update ItemPrice:                 
					UpdateItemPrices(ItemVari, context, dateTime, itemcode, iv, MyobItem);
					#endregion

					#region Update Stock:                   
					if (MyobItem != null)
						UpdateItemStockInfo(context, dateTime, iv, MyobItem);
					#endregion

					#region commit transaction
					try
					{
						transaction.Commit();
					}
					catch (Exception ex)
					{
						transaction.Rollback();
						throw new Exception(ex.Message);
					}
					#endregion
				}
			}
		}






		public static void GetMyobItemCategory(MMDbContext context, string itmCode, ref ItemModel item)
		{
			ItemCategory category = context.ItemCategories.FirstOrDefault(x => x.itmCode == itmCode && x.AccountProfileId == apId);
			if (category != null)
			{
				item.catId = category.catId;
			}
		}

		public static void SaveItemCategory(MMDbContext context, string itemcode, int catId)
		{
			ItemCategory itemCategory = context.ItemCategories.FirstOrDefault(x => x.itmCode == itemcode && x.AccountProfileId == apId);
			if (itemCategory != null)
			{
				itemCategory.catId = catId;
				itemCategory.ModifyTime = DateTime.Now;
			}
			else
			{
				itemCategory = new ItemCategory
				{
					itmCode = itemcode,
					catId = catId,
					AccountProfileId = apId,
					CreateTime = DateTime.Now
				};
				context.ItemCategories.Add(itemCategory);
			}
			context.SaveChanges();
		}

		public static void HandleLastSellingPrice4Customer(MMDbContext context, int cusID, Dictionary<string, decimal> DicItemPrice)
		{
			var customer = context.GetCustomer4SalesById1(cusID, apId).FirstOrDefault();
			if (customer != null && customer.IsLastSellingPrice != null && (bool)customer.IsLastSellingPrice)
			{
				var itemcodes = string.Join(",", DicItemPrice.Keys);
				var cusItems = context.CustomerItems.Where(x => x.cusCode == customer.cusCode && itemcodes.Contains(x.itmCode) && x.AccountProfileId == apId).ToList();
				List<CustomerItem> newCusItems = new List<CustomerItem>();
				foreach (var key in DicItemPrice.Keys)
				{
					var cusItem = cusItems.FirstOrDefault(x => x.itmCode == key);
					if (cusItem == null)
					{
						newCusItems.Add(new CustomerItem
						{
							AccountProfileId = apId,
							cusCode = customer.cusCode,
							itmCode = key,
							LastSellingPrice = DicItemPrice[key],
							CreateTime = DateTime.Now,
						});
					}
					else
					{
						cusItem.LastSellingPrice = DicItemPrice[key];
						cusItem.ModifyTime = DateTime.Now;
					}
				}
				context.CustomerItems.AddRange(newCusItems);
				context.SaveChanges();
			}
		}
		public static string GetNewPurchaseRequestCode(SessUser user, MMDbContext context)
		{
			Device device = context.Devices.FirstOrDefault(x => x.dvcSalesId == user.surUID);
			return device != null ? string.Concat(device.dvcPurchaseRequestPrefix, device.dvcNextPurchaseRequestNo) : string.Empty;
		}
		public static string GetNewWholeSalesCode(SessUser user, MMDbContext context)
		{
			Device device = (ApprovalMode) ? context.Devices.FirstOrDefault(x => x.dvcSalesId == user.surUID) : HttpContext.Current.Session["Device"] as Device;
			return device != null ? string.Concat(device.dvcWholesalesPrefix, device.dvcNextWholeSalesNo) : string.Empty;
		}
		public static string GetNewSalesCode(SessUser user, MMDbContext context, string type = "")
		{
			Device device = ApprovalMode ? context.Devices.FirstOrDefault(x => x.dvcSalesId == user.surUID) : HttpContext.Current.Session["Device"] as Device;
			//int nextsalesno = string.IsNullOrEmpty(type) ? device.dvcNextRtlSalesNo : device.dvcNextPreorderNo;
			int nextsalesno = device.dvcNextRtlSalesNo ?? 100001;
			//if (device.dvcUsedInvoiceNo.Split(',').Contains(nextsalesno.ToString())) nextsalesno++;
			nextsalesno++;
			return string.IsNullOrEmpty(type) ? string.Concat(device.dvcInvoicePrefix, nextsalesno) : string.Concat(device.dvcPreorderPrefix, nextsalesno);
		}

		public static string GetPreorderPrefix(Device device)
		{
			return device != null ? device.dvcPreorderPrefix : string.Empty;
		}
		public static string GetInvoicePrefix(Device device)
		{
			return device != null ? device.dvcInvoicePrefix : string.Empty;
		}


		public static bool SendNotificationEmail(Dictionary<string, string> DicReviewUrl, ReactType reactType, string rejectreason = null, string suppernames=null)
		{
			int okcount = 0;
			int ngcount = 0;

			EmailEditModel model = new EmailEditModel();
			var mailsettings = model.Get();

			MailAddress frm = new MailAddress(mailsettings.emEmail, mailsettings.emDisplayName);

			while (okcount == 0)
			{
				if (ngcount >= mailsettings.emMaxEmailsFailed || okcount > 0)
				{
					break;
				}

				bool addbc = int.Parse(ConfigurationManager.AppSettings["AddBccToDeveloper"]) == 1;
				MailAddress addressBCC = new MailAddress(ConfigurationManager.AppSettings["DeveloperEmailAddress"], ConfigurationManager.AppSettings["DeveloperEmailName"]);
				MailMessage message = new()
				{
					From = frm
				};
				if (addbc)
				{
					message.Bcc.Add(addressBCC);
				}

				message.Subject = string.Format(Resource.PendingApprovalFormat, Resource.PurchaseOrder);
				message.BodyEncoding = Encoding.UTF8;
				message.IsBodyHtml = true;

				string approvaltxt = reactType == ReactType.Rejected ? Resource.Rejected : Resource.Approval;

				foreach (var item in DicReviewUrl)
				{
					var arr = item.Key.Split(':');
					var name = arr[0];
					var email = arr[1];
					var ordercode = arr[2];
					var orderlnk = $"<a href='{item.Value}' target='_blank'>{ordercode}</a>";
					message.To.Add(new MailAddress(email, name));
					string strorder = string.Format(Resource.RequestFormat, Resource.Purchase);

					string mailbody = string.Empty;

					if (reactType == ReactType.RequestingByStaff || reactType == ReactType.RequestingByDeptHead || reactType == ReactType.RequestingByFinanceDept)
					{
						//<h3>Hi {name}</h3><p>The following {strorder} is pending for your {approvaltxt}:</p>{orderlnk}
						mailbody = EnableReviewUrl ? string.Format(Resource.RequestWLinkHtmlFormat, name, strorder, approvaltxt, orderlnk) : string.Format(Resource.RequestHtmlFormat, name, strorder, approvaltxt);
					}

					if (reactType == ReactType.PassedByDeptHead || reactType == ReactType.PassedByFinanceDept || reactType == ReactType.PassedToBoard)
					{
						mailbody = EnableReviewUrl ? string.Format(Resource.RespondWLinkHtmlFormat, name, strorder, approvaltxt, orderlnk) : string.Format(Resource.RespondHtmlFormat, name, strorder, approvaltxt);
					}
					if (reactType == ReactType.Approved)
					{
						//ApprovedPoMsgWLnkFormat	The PO {0} with the supplier(s) {1} is approved and here is the link {2}.	
						mailbody = EnableReviewUrl ? string.Format(Resource.ApprovedPoMsgWLnkFormat, strorder, suppernames, orderlnk) : string.Format(Resource.ApprovedPoMsgFormat, strorder, suppernames);
					}
					if (reactType == ReactType.Rejected)
					{
						//<h3>Hi {0}</h3><p>The following {1} is pending for your review:</p><ul>{2}</ul><h4>{3}</h4><p>{4}</p>
						var rejectreasontxt = string.Format(Resource.ReasonForFormat, Resource.Reject);
						//mailbody = $"<h3>Hi {name}</h3><p>The following invoice is pending for your review:</p><ul>{lilist}</ul><h4>{rejectreasontxt}</h4><p>{rejectreason}</p>";
						mailbody = string.Format(Resource.RejectHtmlFormat, name, strorder, ordercode, rejectreasontxt, rejectreason);
					}

					message.Body = mailbody;
					using (SmtpClient smtp = new SmtpClient(mailsettings.emSMTP_Server, mailsettings.emSMTP_Port))
					{
						smtp.UseDefaultCredentials = false;
						smtp.EnableSsl = mailsettings.emSMTP_EnableSSL;
						smtp.Credentials = new NetworkCredential(mailsettings.emSMTP_UserName, mailsettings.emSMTP_Pass);
						try
						{
							smtp.Send(message);
							okcount++;
						}
						catch (Exception)
						{
							ngcount++;
						}
					}
				}
			}
			return okcount > 0;
		}



		public static void UpdateSerialStatus4Refund(MMDbContext context, List<string> snocodes)
		{
			foreach (var snocode in snocodes)
			{
				var serial = context.SerialNoes.FirstOrDefault(x => x.snoCode == snocode && x.AccountProfileId == AccountProfileId);
				if (serial != null)
				{
					serial.snoStatus = "REDEEM READY";
					serial.ModifyTime = DateTime.Now;
				}
			}
			context.SaveChanges();
		}




		public static void SetNewPurchaseRequestCode(MMDbContext context, bool save)
		{
			var Device = HttpContext.Current.Session["Device"] as DeviceModel;
			Device device = context.Devices.Find(Device.dvcUID);
			device.dvcNextPurchaseRequestNo++;
			if (save)
				context.SaveChanges();
		}
		public static void SetNewWholeSalesCode(MMDbContext context, bool save)
		{
			var Device = HttpContext.Current.Session["Device"] as DeviceModel;
			Device device = context.Devices.Find(Device.dvcUID);
			device.dvcNextWholeSalesNo++;
			if (save)
				context.SaveChanges();
		}

		public static string GetItemOptionsItemCodes(string connectionString, int apId)
		{
			using var connection = new SqlConnection(connectionString);
			connection.Open();

			var itemoptionsItemCodes = connection.Query<string>(@"EXEC dbo.GetItemOptionsItemCodeList1 @apId=@apId", new { apId }).ToList();
			return itemoptionsItemCodes == null ? string.Empty : string.Join(",", itemoptionsItemCodes);
		}


		public static Dictionary<string, int> GetDicIvTotalQty(List<IvTotalQty> ivTotalQtyList)
		{
			var DicIvTotalQty = new Dictionary<string, int>();
			foreach (var item in ivTotalQtyList)
			{
				var key = string.Concat(item.ivIdList, ":", item.itmCode);
				DicIvTotalQty[key] = item.totalIvQty;
			}
			return DicIvTotalQty;
		}
		public static Dictionary<string, int> GetDicBatTotalQty(List<BatTotalQty> batTotalQtyList)
		{
			var DicBatTotalQty = new Dictionary<string, int>();
			foreach (var item in batTotalQtyList)
			{
				var key = string.Concat(item.batCode, ":", item.batItemCode);
				DicBatTotalQty[key] = item.totalBatQty;
			}
			return DicBatTotalQty;
		}
		public static Dictionary<string, int> GetDicVtTotalQty(List<VtTotalQty> vtTotalQtyList)
		{
			var DicVtTotalQty = new Dictionary<string, int>();
			foreach (var item in vtTotalQtyList)
			{
				DicVtTotalQty[item.Id.ToString()] = item.totalVtQty;
			}
			return DicVtTotalQty;
		}




		private static void initDictionaries(ItemViewModel itmodel = null, CentralDataModel cdmodel = null)
		{
			if (itmodel != null)
			{
				itmodel.DicItemBVList = new Dictionary<string, Dictionary<string, List<string>>>();

				itmodel.DicItemBatchQty = new Dictionary<string, List<BatchQty>>();
				itmodel.DicItemBatDelQty = new Dictionary<string, List<BatDelQty>>();

				itmodel.DicItemBatVtList = new Dictionary<string, Dictionary<string, List<string>>>();

				itmodel.DicItemSnos = new Dictionary<string, List<string>>();

				itmodel.DicItemBatSnVtList = new Dictionary<string, Dictionary<string, List<Models.Purchase.BatSnVt>>>();

				itmodel.DicItemSnVtList = new Dictionary<string, List<SnVt>>();

				itmodel.DicSeqSnVtList = new Dictionary<int, List<SeqSnVtList>>();

				itmodel.DicItemVtQtyList = new Dictionary<string, List<VtQty>>();
				itmodel.DicItemVtDelQtyList = new Dictionary<string, List<VtDelQty>>();

				if (itmodel.DicItemOptions == null)
					itmodel.DicItemOptions = new Dictionary<string, ItemOptions>();

				itmodel.PoItemBatVQList = new List<PoItemBatVQ>();

				itmodel.DicItemAttrList = new Dictionary<string, List<ItemAttribute>>();
				itmodel.DicItemSelectedIVList = new Dictionary<string, List<ItemVariModel>>();
				itmodel.DicItemGroupedVariations = new Dictionary<string, List<IGrouping<string, ItemVariModel>>>();
				itmodel.DicItemVariations = new Dictionary<string, List<ItemVariModel>>();

			}
			if (cdmodel != null)
			{
				cdmodel.DicItemBVList = new Dictionary<string, Dictionary<string, List<string>>>();

				cdmodel.DicItemBatchQty = new Dictionary<string, List<BatchQty>>();
				cdmodel.DicItemBatDelQty = new Dictionary<string, List<BatDelQty>>();

				cdmodel.DicItemBatVtList = new Dictionary<string, Dictionary<string, List<string>>>();

				cdmodel.DicItemSnos = new Dictionary<string, List<string>>();

				cdmodel.DicItemBatSnVtList = new Dictionary<string, Dictionary<string, List<Models.Purchase.BatSnVt>>>();


				cdmodel.DicItemSnVtList = new Dictionary<string, List<SnVt>>();

				cdmodel.DicSeqSnVtList = new Dictionary<int, List<SeqSnVtList>>();

				cdmodel.DicItemVtQtyList = new Dictionary<string, List<VtQty>>();
				cdmodel.DicItemVtDelQtyList = new Dictionary<string, List<VtDelQty>>();

				if (cdmodel.DicItemOptions == null)
					cdmodel.DicItemOptions = new Dictionary<string, ItemOptions>();

				cdmodel.PoItemBatVQList = new List<PoItemBatVQ>();

				cdmodel.DicItemAttrList = new Dictionary<string, List<ItemAttribute>>();
				cdmodel.DicItemSelectedIVList = new Dictionary<string, List<ItemVariModel>>();
				cdmodel.DicItemGroupedVariations = new Dictionary<string, List<IGrouping<string, ItemVariModel>>>();
				cdmodel.DicItemVariations = new Dictionary<string, List<ItemVariModel>>();
			}
		}

		public static void GetItemOptionsInfo(MMDbContext context, ref Dictionary<string, List<DistinctItem>> dicLocItemList, HashSet<string> itemcodelist, List<string> Shops, Microsoft.Data.SqlClient.SqlConnection connection)
		{
			string stritemcodes = string.Join(",", itemcodelist);

			var itembtInfo = context.GetBatchVtInfoByItemCodesNoLoc(AccountProfileId, stritemcodes).ToList();
			var itemvtInfo = context.GetValidThruInfoByItemCodes(AccountProfileId, stritemcodes).ToList();

			var ibqList = from item in itembtInfo
						  where item.batCode != null
						  group item by new { item.itmCode, item.batCode, item.batSeq, item.Id, item.pstCode } into itemgroup
						  select new
						  {
							  PoCode = itemgroup.Key.pstCode,
							  ItemCode = itemgroup.Key.itmCode,
							  BatchCode = itemgroup.Key.batCode,
							  TotalQty = itemgroup.Sum(x => x.batQty),
							  itemgroup.Key.batSeq,
							  batId = itemgroup.Key.Id
						  };

			ibqList = ibqList.OrderBy(x => x.ItemCode).ThenBy(x => x.BatchCode).ToList();

			var ivqList = from item in itemvtInfo
						  where item.piValidThru != null
						  group item by new { item.itmCode, item.piValidThru, item.pstCode, item.vtSeq, item.Id } into itemgroup
						  select new
						  {
							  PoCode = itemgroup.Key.pstCode,
							  ItemCode = itemgroup.Key.itmCode,
							  itemgroup.Key.vtSeq,
							  ValidThru = CommonHelper.FormatDate((DateTime)itemgroup.Key.piValidThru),
							  TotalQty = itemgroup.Sum(x => x.vtQty),
							  vtId = itemgroup.Key.Id
						  };
			ivqList = ivqList.OrderBy(x => x.ItemCode).ToList();

			var serialInfo = context.GetSerialInfoByItemCodes(AccountProfileId, stritemcodes, null).ToList();

			var ibvqList = from item in itembtInfo
						   where item.batCode != null
						   //group item by new { item.itmCode, item.ivBatCode, item.piValidThru } into itemgroup
						   select new
						   {
							   PoCode = item.pstCode,
							   ItemCode = item.itmCode,
							   BatchCode = item.batCode,
							   ValidThru = item.batValidThru == null ? "" : CommonHelper.FormatDate((DateTime)item.batValidThru),
							   BatQty = (int)item.batQty,
							   item.batSeq,
							   item.Id
						   };
			ibvqList = ibvqList.OrderBy(x => x.ItemCode).ThenBy(x => x.BatchCode).ToList();

			var batchcodelist = ibvqList.Select(x => x.BatchCode).Distinct().ToList();

			var locationlist = dicLocItemList.Keys.ToList();



			foreach (string location in locationlist)
			{
				var itemlist = dicLocItemList[location];
				foreach (var itemcode in itemcodelist)
				{
					var dItem = itemlist.FirstOrDefault(x => x.ItemCode == itemcode);
					if (dItem != null)
					{
						dItem.BVList = new Dictionary<string, List<string>>();
						dItem.SnBatVtList = new Dictionary<string, List<Models.Purchase.BatSnVt>>();
						dItem.BatchQtyList = new List<BatchQty>();
						dItem.BatDelQtyList = new List<BatDelQty>();
						dItem.Snos = new List<string>();
						dItem.SnVtList = new List<SnVt>();
						dItem.VtQtyList = new List<VtQty>();
						dItem.VtDelQtyList = new List<VtDelQty>();
						dItem.PoItemBatVQList = new List<PoItemBatVQ>();
						dItem.AttrList = new List<ItemAttribute>();
						dItem.SelectedIVList = new List<ItemVariModel>();
						dItem.GroupedVariations = new List<IGrouping<string, ItemVariModel>>();
						dItem.Variations = new List<ItemVariModel>();

						foreach (var batchcode in batchcodelist)
						{
							dItem.BVList[batchcode] = new List<string>();
							dItem.SnBatVtList[batchcode] = new List<Models.Purchase.BatSnVt>();
						}
						dItem.AttrList = context.ItemAttributes.Where(x => x.itmCode == itemcode && x.AccountProfileId == AccountProfileId).ToList();
						if (dItem.AttrList != null && dItem.AttrList.Count > 0)
						{
							var attrIds = string.Join(",", dItem.AttrList.Select(x => x.Id).ToList());

							dItem.SelectedIVList = connection.Query<ItemVariModel>(@"EXEC dbo.GetSelectedItemVariationListByCode @itmCode=@itmCode,@apId=@apId", new { itmCode = itemcode, apId = AccountProfileId }).ToList();


							dItem.Variations = connection.Query<ItemVariModel>(@"EXEC dbo.GetItemVariationsByIds1 @iaIds=@iaIds,@apId=@apId", new { iaIds = attrIds, apId = AccountProfileId }).ToList();

							if (dItem.Variations.Count > 0)
							{
								dItem.GroupedVariations = dItem.Variations.GroupBy(x => x.iaName).ToList();
							}
						}

						if (ibvqList != null && ibvqList.Count() > 0)
						{
							dItem.PoItemBatVQList = new List<PoItemBatVQ>();
							foreach (var item in ibvqList)
							{
								foreach (var kv in dItem.BVList)
								{
									if (kv.Key == item.ItemCode)
									{
										foreach (var k in kv.Value)
										{
											if (k == item.BatchCode)
											{
												dItem.BVList[k].Add(item.ValidThru);
											}
										}
									}
								}

								dItem.PoItemBatVQList.Add(
								new PoItemBatVQ
								{
									id = string.Concat(item.PoCode, "_", item.ItemCode, "_", item.BatchCode),
									pocode = item.PoCode,
									itemcode = item.ItemCode,
									batchcode = item.BatchCode,
									vt = item.ValidThru,
									batchqty = item.BatQty,
								}
								);
							}
						}

						string strbatcodes = string.Join(",", itembtInfo.Select(x => x.batCode).Distinct().ToList());

						if (ibqList != null && ibqList.Count() > 0)
						{
							foreach (var item in ibqList)
							{
								var batqty = new BatchQty
								{
									batcode = item.BatchCode,
									batqty = (int)item.TotalQty,
									sellableqty = (int)item.TotalQty,
									seq = item.batSeq,
									itemcode = item.ItemCode,
									batId = item.batId,
									pocode = item.PoCode
								};

								dItem.BatchQtyList.Add(batqty);

								foreach (var pibvq in dItem.PoItemBatVQList)
								{
									if (pibvq.pocode == item.PoCode && pibvq.itemcode == item.ItemCode && pibvq.batchcode == item.BatchCode)
									{
										pibvq.sellableqty = (int)item.TotalQty;
										pibvq.seq = item.batSeq;
										pibvq.batId = item.batId;
									}
								}
							}
						}

						if (serialInfo.Count > 0)
						{
							foreach (var serial in serialInfo)
							{

								dItem.Snos.Add(serial.snoCode);

								if (dItem.SnBatVtList.ContainsKey(serial.snoCode))
								{
									foreach (var batcode in batchcodelist)
									{
										if (dItem.SnBatVtList.ContainsKey(batcode))
										{
											dItem.SnBatVtList[batcode].Add(new Models.Purchase.BatSnVt
											{
												sn = serial.snoCode,
												batcode = serial.snoBatchCode,
												vt = serial.snoValidThru == null ? "" : CommonHelper.FormatDate((DateTime)serial.snoValidThru)
											});
										}
									}
								}

								dItem.SnVtList.Add(new SnVt
								{
									sn = serial.snoCode,
									vt = serial.snoValidThru == null ? "" : CommonHelper.FormatDate((DateTime)serial.snoValidThru)
								});

							}
						}

						if (ivqList != null && ivqList.Count() > 0)
						{

							foreach (var vi in ivqList)
							{
								int delqty = 0;
								var vtqty = new VtQty
								{
									pocode = vi.PoCode,
									vt = vi.ValidThru,
									qty = (int)vi.TotalQty,
									vtseq = vi.vtSeq,
									itemcode = vi.ItemCode,
									delqty = delqty,
									sellableqty = (int)vi.TotalQty - delqty,
									vtId = vi.vtId
								};
								//if (vtqty.totalqty > 0)
								dItem.VtQtyList.Add(vtqty);

							}

						}
					}
				}
			}
		}


		public static void GetDicIvInfo(MMDbContext context, List<PoItemVariModel> PoIvInfo, ref Dictionary<string, List<PoItemVariModel>> DicIvInfo)
		{
			foreach (var item in PoIvInfo)
			{
				foreach (var ivId in item.ivIdList.Split(','))
				{
					var _ivid = long.Parse(ivId);
					var ia = context.ItemVariations.FirstOrDefault(x => x.Id == _ivid);
					if (ia != null)
					{
						item.iaName = ia.iaName;
						item.iaValue = ia.iaValue;
					}
				}
				if (DicIvInfo.ContainsKey(item.itmCode))
				{
					DicIvInfo[item.itmCode].Add(item);
				}
			}
		}

		public static void GetDicItemGroupedVariations(MMDbContext context, SqlConnection connection, HashSet<string> itemcodes, ref Dictionary<string, List<ItemAttribute>> DicItemAttrList, ref Dictionary<string, List<ItemVariModel>> DicItemVariations, ref Dictionary<string, List<IGrouping<string, ItemVariModel>>> DicItemGroupedVariations, List<string> Shops)
		{
			foreach (var itemcode in itemcodes)
			{
				DicItemAttrList[itemcode] = context.ItemAttributes.Where(x => x.itmCode == itemcode && x.AccountProfileId == apId).ToList();
				if (DicItemAttrList[itemcode] != null && DicItemAttrList[itemcode].Count > 0)
				{
					var attrIds = string.Join(",", DicItemAttrList[itemcode].Select(x => x.Id).ToList());
					DicItemVariations[itemcode] = connection.Query<ItemVariModel>(@"EXEC dbo.GetItemVariationsByIds1 @iaIds=@iaIds,@apId=@apId", new { iaIds = attrIds, apId }).ToList();

					if (DicItemVariations[itemcode].Count > 0)
					{
						DicItemGroupedVariations[itemcode] = DicItemVariations[itemcode].GroupBy(x => x.iaName).ToList();
					}
				}
			}
		}

		public static string GetDbName(int apId)
		{
			return (apId == 1) ? "SmartBusinessWeb_db" : string.Concat("SBA", (apId - 2).ToString());
		}
		public static Dictionary<string, double> GetDicCurrencyExRate(MMDbContext context)
		{
			var DicCurrencyExRate = new Dictionary<string, double>();
			var myobcurrencylist = context.GetMyobCurrencyList(apId).ToList();
			if (myobcurrencylist != null && myobcurrencylist.Count > 0)
			{

				foreach (var currency in myobcurrencylist)
				{
					DicCurrencyExRate[currency.CurrencyCode] = currency.ExchangeRate ?? 1;
				}
			}
			return DicCurrencyExRate;
		}
		public static Dictionary<string, double> GetDicCurrencyExRate(SqlConnection connection)
		{
			var DicCurrencyExRate = new Dictionary<string, double>();
			var myobcurrencylist = connection.Query<MyobCurrencyModel>(@"EXEC dbo.GetMyobCurrencyList @apId=@apId", new { apId }).ToList();
			if (myobcurrencylist != null && myobcurrencylist.Count > 0)
			{

				foreach (var currency in myobcurrencylist)
				{
					DicCurrencyExRate[currency.CurrencyCode] = currency.ExchangeRate ?? 1;
				}
			}
			return DicCurrencyExRate;
		}

		public static void GetItemOptionsVariInfo(int apId, string location, MMDbContext context, HashSet<string> itemcodelist, ItemViewModel model)
		{
			throw new NotImplementedException();
		}
	}

	public class VtTotalQty
	{
		public string Id { get; set; }
		public int totalVtQty { get; set; }
	}
	public class BatTotalQty
	{
		public string batCode { get; set; }
		public string batItemCode { get; set; }
		public int totalBatQty { get; set; }
	}

	public class IvTotalQty
	{
		public string ivIdList { get; set; }
		public string itmCode { get; set; }
		public int totalIvQty { get; set; }
	}

	public class VtDelQty
	{
		public string vt { get; set; }
		public int delqty { get; set; }
		public string pocode { get; set; }
		public int? seq { get; set; }
		public string itemcode { get; set; }
		public int vtId { get; set; }
		public long vtdelId { get; set; }
		public string type { get; set; }
		public string salescode { get; set; }
	}
	public class VtQty
	{
		public string vt { get; set; }
		public int qty { get; set; }
		public string pocode { get; set; }
		public int? vtseq { get; set; }
		public string itemcode { get; set; }
		public int? delqty { get; set; }
		public int sellableqty { get; set; }
		public int vtId { get; set; }
	}
	public class IvQty
	{
		public string Id { get; set; }
		public string ivIdList { get; set; }
		public int qty { get; set; }
		public string pocode { get; set; }
		public int? ivseq { get; set; }
		public string itemcode { get; set; }
		public int? delqty { get; set; }
		public int sellableqty { get; set; }
	}
	public class IvDelQty : IvQty
	{

		public int? seq { get; set; }
		//public string Id { get; set; }
	}
	public class BatchVtQty
	{
		public string batchcode { get; set; }
		public string validthru { get; set; }
		public int batchqty { get; set; }
	}

	public class BatDelQty
	{
		public string batcode { get; set; }
		public int? batdelqty { get; set; }
		public int batqty { get; set; }
		public int? seq { get; set; }
		public int batId { get; set; }
		public long batdelId { get; set; }
		public string itemcode { get; set; }
		public string pocode { get; set; }
		public DateTime? batVt { get; set; }
		public string batSn { get; set; }
		public string VtDisplay { get { return batVt == null ? "" : CommonHelper.FormatDate((DateTime)batVt, true); } }

		public string ivIdList { get; set; }
		public string type { get; set; }
		public string salescode { get; set; }
	}
	public class BatchQty
	{
		public string batcode { get; set; }
		public int batqty { get; set; }
		public int sellableqty { get; set; }
		public string itemcode { get; set; }
		public int? delqty { get; set; }
		public int? seq { get; set; }
		public int batId { get; set; }
		public string pocode { get; set; }
	}

	public class PoBatVQ
	{
		public string pocode { get; set; }
		public string batchcode { get; set; }
		public int batchqty { get; set; }
		public string vt { get; set; }
		public string id { get; set; }
		public int sellableqty { get; set; }
		public int? seq { get; set; }
		public int batId { get; set; }
	}
	public class PoItemBatVQ : PoBatVQ
	{
		public string itemcode { get; set; }
	}

	public class SeqSnVtList
	{
		public int seq { get; set; }
		public string sn { get; set; }
		public List<string> vtlist { get; set; }
	}

	//public class BatSnVt
	//{
	//	public string pocode { get; set; }
	//	//public int? seq { get; set; }
	//	public string batcode { get; set; }
	//	//public List<string> snlist { get; set; }
	//	public string sn { get; set; }
	//	public string vt { get; set; }
	//	public string status { get; set; }
	//}
	public enum CusFollowUpStatus
	{
		need = 1,
		noneed = 0,
		completed = 2
	}
}
