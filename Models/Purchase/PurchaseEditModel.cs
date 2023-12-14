using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CommonLib.Helpers;
using CommonLib.Models;
using MMDAL;
using Resources = CommonLib.App_GlobalResources;
using Device = MMDAL.Device;
using Purchase = MMDAL.Purchase;
using System.Configuration;
using MMLib.Models.POS.Settings;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Text;
using ItemModel = MMLib.Models.Item.ItemModel;
using MMLib.Models.MYOB;
using MMLib.Models.User;
using MMLib.Models.Item;
using System.Text.Json;
using CommonLib.BaseModels;
using MMLib.Models.Supplier;
using System.IO;
using System.Drawing.Drawing2D;

namespace MMLib.Models.Purchase
{
	public class PurchaseEditModel : PagingBaseModel
	{
		public Dictionary<string, List<SupplierModel>> DicSupInfoes { get; set; }
		public Dictionary<string, ItemOptions> DicItemOptions { get; set; }
		public string JsonDicItemOptions { get { return JsonSerializer.Serialize(DicItemOptions); } }
		public PurchaseOrderReviewModel PurchaseOrderReview { get; set; }
		public Dictionary<string, string> DicSupCodeName { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, string> DicLocation { get; set; }
		public Dictionary<string, double> DicCurrencyExRate { get; set; }
		public string JsonDicCurrencyExRate { get { return JsonSerializer.Serialize(DicCurrencyExRate); } }
		public string JsonDicLocation { get { return DicLocation == null ? "" : JsonSerializer.Serialize(DicLocation); } }
		public List<SelectListItem> SupplierList { get; set; }
		public List<SelectListItem> LocationList { get; set; }
		public List<MyobJobModel> JobList { get; set; }
		public string JsonJobList { get { return JobList == null ? "" : JsonSerializer.Serialize(JobList); } }

		public Dictionary<string, List<ItemAttribute>> DicItemAttrList;
		public Dictionary<string, List<ItemVariModel>> DicItemVariations;
		public Dictionary<string, List<IGrouping<string, ItemVariModel>>> DicItemGroupedVariations;
		public string JsonDicItemGroupedVariations { get { return DicItemGroupedVariations == null ? "" : JsonSerializer.Serialize(DicItemGroupedVariations); } }
		public List<string> ImgList;
		public List<string> FileList;
		public bool Readonly { get; set; }
		public bool DoApproval { get; set; }
		static string PurchaseType = "PS";
		public PurchaseStatus ListMode { get; set; }
		public PurchaseModel Purchase { get; set; }

		public ReceiptViewModel Receipt;
		public List<string> DisclaimerList;
		public List<string> PaymentTermsList;

		public PurchaseEditModel()
		{
			DicItemOptions = new Dictionary<string, ItemOptions>();
			DicCurrencyExRate = new Dictionary<string, double>();
			DicLocation = new Dictionary<string, string>();
			JobList = new List<MyobJobModel>();
			ImgList = new List<string>();
			FileList = new List<string>();

			DicItemAttrList = new Dictionary<string, List<ItemAttribute>>();
			DicItemVariations = new Dictionary<string, List<ItemVariModel>>();
			DicItemGroupedVariations = new Dictionary<string, List<IGrouping<string, ItemVariModel>>>();
			DicSupInfoes = new Dictionary<string, List<SupplierModel>>();
		}
		public PurchaseEditModel(string receiptno, int? ireadonly, int? idoapproval) : this()
		{
			Get(0, receiptno, ireadonly, idoapproval);
		}
		public PurchaseEditModel(long Id, string status, int? ireadonly = 0, int? idoapproval = 1, bool forprint = false) : this()
		{
			Get(Id, null, ireadonly, idoapproval, status, forprint);
		}

		public void Get(long Id = 0, string receiptno = null, int? ireadonly = 0, int? idoapproval = 1, string status = "", bool forprint = false)
		{
			bool isapprover = (bool)HttpContext.Current.Session["IsApprover"];
			get0(isapprover, out MMDbContext context, out Device device);
			SqlConnection connection = get1(Id, receiptno);
			int apId = ComInfo.AccountProfileId;
			DateTime dateTime = DateTime.Now;

			if (Id > 0 || !string.IsNullOrEmpty(receiptno))
			{
				Readonly = ireadonly == 1;
				DoApproval = idoapproval == 1;

				if (forprint)
				{
					Receipt = new ReceiptViewModel();
					DisclaimerList = new List<string>();
					PaymentTermsList = new List<string>();
					Helpers.ModelHelper.GetReady4Print(context, ref Receipt, ref DisclaimerList, ref PaymentTermsList);
				}

				var suppurchaseInfoes = connection.Query<SupplierModel>(@"EXEC dbo.GetSuppliersInfoesByCode @apId=@apId,@pstCode=@pstCode", new { apId, Purchase.pstCode }).ToList();
				if (suppurchaseInfoes != null && suppurchaseInfoes.Count > 0)
				{
					var groupedsupPurchaseInfoes = suppurchaseInfoes.GroupBy(x => x.supCode).ToList();
					foreach (var group in groupedsupPurchaseInfoes)
					{
						var g = group.FirstOrDefault();
						if (!DicSupInfoes.ContainsKey(g.supCode))
						{
							DicSupInfoes[g.supCode] = new List<SupplierModel>();
						}
					}
					foreach (var group in groupedsupPurchaseInfoes)
					{
						var g = group.FirstOrDefault();
						if (DicSupInfoes.ContainsKey(g.supCode))
						{
							foreach (var spi in group)
							{
								var _file = Path.Combine("Purchase", apId.ToString(), Purchase.pstCode, g.supCode, spi.fileName);
								spi.fileName = $"<a class='' href='/{_file}' target='_blank'><img src='{UriHelper.GetBaseUrl()}/Images/pdf.jpg' class='thumbnail'/>{spi.fileName}</a>";
								DicSupInfoes[g.supCode].Add(spi);
							}

						}
					}
				}

				#region Handle View File
				Helpers.ModelHelper.HandleViewFile(Purchase.UploadFileName, AccountProfileId, Purchase.pstCode, ref ImgList, ref FileList);
				#endregion
			}
			else
			{
				status = PurchaseStatus.requesting.ToString();
				var latestpurchase = context.Purchases.OrderByDescending(x => x.Id).FirstOrDefault();
				if (latestpurchase != null)
				{
					if (string.IsNullOrEmpty(latestpurchase.pstType))
					{
						var _Id = latestpurchase.Id;
						Purchase = new PurchaseModel
						{
							Id = _Id,
							IsEditMode = false,
							pstCode = latestpurchase.pstCode,
							pstStatus = latestpurchase.pstStatus,
							pqStatus = RequestStatus.requestingByStaff.ToString(),
						};
					}
					else addNewPurchase(context, apId, dateTime, device, status);
				}
				else addNewPurchase(context, apId, dateTime, device, status);
			}
			get2(context, device);
			JobList = connection.Query<MyobJobModel>(@"EXEC dbo.GetJobList @apId=@apId", new { apId }).ToList();
			Purchase.Mode = ireadonly == 1 ? "readonly" : "";

		}

		private long addNewPurchase(MMDbContext context, int apId, DateTime dateTime, Device device, string status)
		{
			var purchaseinitcode = device.dvcPurchaseRequestPrefix;
			var purchaseno = $"{device.dvcNextPurchaseRequestNo:000000}";
			device.dvcNextPurchaseRequestNo++;
			device.dvcModifyTime = dateTime;

			string pstcode = string.Concat(purchaseinitcode, purchaseno);
			string pqstatus = RequestStatus.requestingByStaff.ToString();
			MMDAL.Purchase ps = new()
			{
				pstCode = pstcode,
				//pstType = PurchaseType,
				pstPurchaseDate = dateTime.Date,
				pstPurchaseTime = dateTime,
				pstPromisedDate = dateTime.AddDays(1),
				pstStatus = status,
				AccountProfileId = apId,
				CreateTime = dateTime,
				CreateBy = user.UserName,
				pstCheckout = false,
				pstIsPartial = false,
				pqStatus = pqstatus,
			};
			ps = context.Purchases.Add(ps);
			context.SaveChanges();

			Purchase = new PurchaseModel
			{
				Id = ps.Id,
				IsEditMode = false,
				pstCode = pstcode,
				pstStatus = status,
				pqStatus = pqstatus,
			};

			return ps.Id;
		}

		private void get2(MMDbContext context, Device device)
		{
			Purchase.Device = device != null ? device as DeviceModel : null;
			SupplierEditModel model = new(Purchase.supCode);
			Purchase.Supplier = model.Supplier;
			var supplierlist = context.Suppliers.Where(x => x.AccountProfileId == ComInfo.AccountProfileId && (bool)x.supCheckout).OrderBy(x => x.supName).ToList();
			SupplierList = new List<SelectListItem>();
			DicSupCodeName = new Dictionary<string, string>();
			foreach (var supplier in supplierlist)
			{
				SupplierList.Add(
					new SelectListItem
					{
						Value = supplier.supCode,
						Text = supplier.supName,
					}
				);
				if (!DicSupCodeName.ContainsKey(supplier.supCode)) DicSupCodeName[supplier.supCode] = supplier.supName;
			}

			var stocklocationlist = NonABSS ? ComInfo.Shops.Split(',').ToList() : context.GetStockLocationList1(ComInfo.AccountProfileId).ToList();
			LocationList = new List<SelectListItem>();
			foreach (var item in stocklocationlist)
			{
				LocationList.Add(new SelectListItem
				{
					Value = item,
					Text = item
				});
			}

			var myobcurrencylist = context.MyobCurrencies.Where(x => x.AccountProfileId == ComInfo.AccountProfileId).ToList();
			if (myobcurrencylist != null && myobcurrencylist.Count > 0)
			{
				DicCurrencyExRate = new Dictionary<string, double>();
				foreach (var currency in myobcurrencylist)
				{
					DicCurrencyExRate[currency.CurrencyCode] = currency.ExchangeRate ?? 1;
				}
			}

			Purchase.TaxModel = Helpers.ModelHelper.GetTaxInfo(context);
			Purchase.UseForexAPI = ExchangeRateEditModel.GetForexInfo(context);
			DicLocation = new Dictionary<string, string>();
			foreach (var shop in stocklocationlist)
			{
				DicLocation[shop] = shop;
			}
		}

		private SqlConnection get1(long Id = 0, string receiptno = null)
		{
			var connection = new SqlConnection(DefaultConnection);
			connection.Open();
			Purchase = connection.QueryFirstOrDefault<PurchaseModel>(@"EXEC dbo.GetPurchaseByCodeId1 @apId=@apId,@code=@code,@Id=@Id,@openonly=@openonly", new { apId, Id, code = receiptno, openonly = false });
			return connection;
		}

		private void get0(bool isapprover, out MMDbContext context, out Device device)
		{
			device = null;
			Purchase = new PurchaseModel();
			context = new MMDbContext();
			if (!isapprover)
			{
				DeviceModel dev = HttpContext.Current.Session["Device"] as DeviceModel;
				device = context.Devices.FirstOrDefault(x => x.dvcUID == dev.dvcUID);
			}
		}


		public List<PurchaseModel> PSList { get; set; }
		public PagedList.IPagedList<PurchaseModel> PagingPSList { get; set; }
		public string PrintMode { get; set; }

		public List<PurchaseModel> GetList(SessUser user, string strfrmdate, string strtodate, string keyword, PurchaseStatus type = PurchaseStatus.all)
		{
			/* Dates Handling */
			SearchDates = new SearchDates();
			CommonHelper.GetSearchDates(strfrmdate, strtodate, ref SearchDates);
			DateFromTxt = SearchDates.DateFromTxt;
			DateToTxt = SearchDates.DateToTxt;
			/******************/
			//using var context = new MMDbContext();         
			using var connection = new SqlConnection(DefaultConnection);
			connection.Open();
			PSList = new List<PurchaseModel>();

			PSList = connection.Query<PurchaseModel>(@"EXEC dbo.GetPurchaseList5 @apId=@apId,@username=@username,@frmdate=@frmdate,@todate=@todate,@keyword=@keyword", new { apId = ComInfo.AccountProfileId, username = user.UserName, SearchDates.frmdate, SearchDates.todate, keyword }).ToList();

			ListMode = type;
			return PSList;
		}

		public static List<PurchaseReturnMsg> Edit(PurchaseModel model, List<SupplierModel> SupplierList)
		{
			List<PurchaseReturnMsg> msglist = new List<PurchaseReturnMsg>();
			List<Superior> SuperiorList = new List<Superior>();
			string supnames = string.Join(",", SupplierList.Select(x => x.supName).Distinct().ToList());

			string status = "";
			string msg = string.Format(Resources.Resource.SavedFormat, Resources.Resource.PurchaseOrder);
			string purchasestatus = string.Empty;
			IsUserRole IsUserRole = UserEditModel.GetIsUserRole(user);

			DeviceModel dev = HttpContext.Current.Session["Device"] as DeviceModel;

			using var context = new MMDbContext();
			DateTime dateTime = DateTime.Now;

			DateTime purchasedate = CommonHelper.GetDateFrmString4SQL(model.JsPurchaseDate, DateTimeFormat.YYYYMMDD);
			DateTime promiseddate = CommonHelper.GetDateFrmString4SQL(model.JsPromisedDate, DateTimeFormat.YYYYMMDD);

			purchasestatus = IsUserRole.isdirectorboard ? model.pstStatus : !model.IsEditMode ? PurchaseStatus.requesting.ToString() : model.pstStatus;

			List<string> reviewurls = new();
			List<GetSuperior4Notification2_Result> superiors = new();
			Dictionary<string, string> DicReviewUrl = new Dictionary<string, string>();
			Dictionary<string, Dictionary<string, int>> dicItemLocQty = new Dictionary<string, Dictionary<string, int>>();

			if (IsUserRole.isdirectorboard)
			{
				if (!model.IsEditMode)
				{
					addPurchaseOrder(model, SupplierList, context, dateTime, purchasedate, promiseddate, purchasestatus, ref dicItemLocQty);
				}
				else
				{
					processPurchase(model, dev, context, dateTime, purchasedate, promiseddate, purchasestatus);
				}
			}
			else
			{
				sqlConnection.Open();
				SuperiorList = sqlConnection.Query<Superior>(@"EXEC dbo.GetSuperior4Notification2 @apId=@apId,@userId=@userId,@shopcode=@shopcode", new { apId, userId = user.surUID, shopcode = comInfo.Shop }).ToList();

				foreach (var superior in SuperiorList)
				{
					var reviewurl = UriHelper.GetReviewPurchaseOrderUrl(ConfigurationManager.AppSettings["ReviewPurchaseOrderBaseUrl"], model.pstCode, 0, superior.surUID);
					var key = string.Concat(superior.UserName, ":", superior.Email, ":", model.pstCode);
					if (!DicReviewUrl.ContainsKey(key))
						DicReviewUrl[key] = reviewurl;
					reviewurls.Add(reviewurl);
				}

				if (!model.IsEditMode)
				{
					addPurchaseOrder(model, SupplierList, context, dateTime, purchasedate, promiseddate, purchasestatus, ref dicItemLocQty);
				}
				else
				{
					processPurchase(model, dev, context, dateTime, purchasedate, promiseddate, purchasestatus);
				}

				status = "purchaseordersaved";
				if (model.pstStatus.ToLower() != PurchaseStatus.opened.ToString())
				{
					HandlingPurchaseOrderReview(model.pstCode, model.pqStatus, context);

					if (!IsUserRole.isdirectorboard && !IsUserRole.ismuseumdirector)
					{
						#region Send Notification Email   
						if ((bool)comInfo.enableEmailNotification)
						{
							ReactType reactType = ReactType.RequestingByStaff;
							if (IsUserRole.isfinancedept) reactType = ReactType.PassedByFinanceDept;
							if (IsUserRole.ismuseumdirector || IsUserRole.isdirectorboard) reactType = ReactType.Approved;
							if (Helpers.ModelHelper.SendNotificationEmail(DicReviewUrl, reactType))
							{
								var purchase = context.Purchases.FirstOrDefault(x => x.Id == model.Id);
								purchase.pstSendNotification = true;
								context.SaveChanges();
							}
						}
						#endregion
					}
					GenReturnMsgList(model, supnames, ref msglist, SuperiorList, status, IsUserRole.isdirectorboard, DicReviewUrl, IsUserRole.ismuseumdirector);

				}
			}
			//#region Update Stock
			//if (purchasestatus == "opened")
			//	UpdateStockQty(model, PurchaseItems, context, user, dateTime, dicItemLocQty);
			//#endregion

			return msglist;
		}



		private static void GenReturnMsgList(PurchaseModel model, string supnames, ref List<PurchaseReturnMsg> msglist, List<Superior> SuperiorList, string status, bool isdirectorboard, Dictionary<string, string> DicReviewUrl, bool ismuseumdirector, string msg = null)
		{
			foreach (var superior in SuperiorList)
			{
				if (string.IsNullOrEmpty(msg))
					msg = (!isdirectorboard && !ismuseumdirector) ? string.Format(Resources.Resource.NotificationEmailWillBeSentToFormat, superior.UserName) : Resources.Resource.OrderSavedSuccessfully;

				var key = string.Concat(superior.UserName, ":", superior.Email, ":", model.pstCode);
				msglist.Add(new PurchaseReturnMsg
				{
					msg = msg,
					status = status,
					superiorphone = superior.Phone,
					purchasecode = model.pstCode,
					reviewurl = DicReviewUrl[key],
					supnames = supnames,					
					superioremail = superior.Email,
					isdirectorboard = isdirectorboard,
					remark = model.pstRemark
				});
			}
		}

		private static void processPurchase(PurchaseModel model, DeviceModel dev, MMDbContext context, DateTime dateTime, DateTime purchasedate, DateTime promiseddate, string purchasestatus)
		{
			MMDAL.Purchase ps = context.Purchases.Find(model.Id);
			ps.pstType = PurchaseType;
			ps.pstRemark = model.pstRemark;
			ps.pstSalesLoc = model.pstSalesLoc;
			ps.pstPurchaseDate = purchasedate;
			ps.pstPromisedDate = promiseddate;
			ps.pstStatus = purchasestatus;
			ps.pstCurrency = model.pstCurrency;
			ps.pstExRate = model.pstExRate;
			ps.ModifyTime = dateTime;
			ps.pstSupplierInvoice = model.pstSupplierInvoice;
			ps.pstAllLoc = model.pstAllLoc;
			context.SaveChanges();

			if (purchasestatus == "opened")
			{
				#region Update Purchase Status
				// update wholesales status:                
				ps.pstStatus = PurchaseStatus.opened.ToString();				
				ps.ModifyTime = dateTime;
				context.SaveChanges();
				#endregion

				#region Add New Purchase Order				
				var device = context.Devices.AsNoTracking().FirstOrDefault(x => x.dvcUID == dev.dvcUID);
				var purchaseinitcode = device.dvcPurchaseOrderPrefix;
				var purchaseno = $"{device.dvcNextPurchaseOrderNo:000000}";
				string code = string.Concat(purchaseinitcode, purchaseno);
				MMDAL.Purchase purchase = new MMDAL.Purchase
				{
					pstCode = code,
					pstRefCode = ps.pstCode,
					AccountProfileId = apId,
					CreateTime = dateTime,
				};
				context.Purchases.Add(purchase);
				device.dvcNextPurchaseOrderNo++;
				context.SaveChanges();
				#endregion
			}			
		}


		

		private static long addPurchaseOrder(PurchaseModel model, List<SupplierModel> SupplierList, MMDbContext context, DateTime dateTime, DateTime purchasedate, DateTime promiseddate, string purchasestatus, ref Dictionary<string, Dictionary<string, int>> dicItemLocQty)
		{
			var pstTime = purchasedate.Add(dateTime.TimeOfDay);
			var supcodes = string.Join(",", SupplierList.Select(x => x.supCode).Distinct().ToList());
			MMDAL.Purchase ps = context.Purchases.Find(model.Id);
			if (ps != null)
			{
				ps.pstAllLoc = model.pstAllLoc;
				ps.pstSalesLoc = model.pstSalesLoc;
				//ps.pstCode = model.pstCode;
				ps.supCode = supcodes;
				ps.pstType = PurchaseType;
				ps.pstDesc = model.pstDesc;
				ps.pstPurchaseDate = purchasedate;
				ps.pstPurchaseTime = pstTime;
				ps.pstPromisedDate = promiseddate;
				ps.pstStatus = purchasestatus;
				ps.pstCurrency = model.pstCurrency;
				ps.pstSupplierInvoice = model.pstSupplierInvoice;
				ps.pstRemark = model.pstRemark;
				ps.pstExRate = model.pstExRate;
				ps.AccountProfileId = comInfo.AccountProfileId;
				ps.ModifyTime = dateTime;
				//ps.CreateTime = dateTime;
				//ps.CreateBy = user.UserName;
				ps.pstCheckout = false;
				ps.pstIsPartial = false;
			}
			context.SaveChanges();		
			AddSupplierList(model.pstCode, SupplierList, context);
			return ps.Id;
		}

		private static void AddSupplierList(string pstCode, List<SupplierModel> SupplierList, MMDbContext context)
		{
			List<PurchaseSupplier> purchaseSuppliers = new List<PurchaseSupplier>();
			foreach (var supplier in SupplierList)
			{
				purchaseSuppliers.Add(new PurchaseSupplier
				{
					pstCode = pstCode,
					supCode = supplier.supCode,
					AccountProfileId = apId,
					CreateTime = DateTime.Now,
				});
			}

			context.PurchaseSuppliers.AddRange(purchaseSuppliers);
			context.SaveChanges();
		}

		private static void HandlingPurchaseOrderReview(string purchasecode, string pqstatus, MMDbContext context)
		{
			DateTime dateTime = DateTime.Now;
			var review = context.PurchaseOrderReviews.FirstOrDefault(x => x.PurchaseOrder == purchasecode);
			if (review == null)
			{
				review = new PurchaseOrderReview
				{
					PurchaseOrder = purchasecode,
					pqStatus = pqstatus,
					AccountProfileId = apId,
					CreateTime = dateTime,
					ModifyTime = dateTime
				};
				context.PurchaseOrderReviews.Add(review);
				context.SaveChanges();
			}
		}

		

		public static void Delete(int id)
		{
			using var context = new MMDbContext();
			var ps = context.Purchases.FirstOrDefault(x => x.Id == id);
			var psilist = context.PurchaseItems.Where(x => x.AccountProfileId == ps.AccountProfileId && x.pstCode == ps.pstCode);
			context.PurchaseItems.RemoveRange(psilist);
			context.Purchases.Remove(ps);
			context.SaveChanges();
		}

		public static List<string> GetUploadPurchaseSqlList(int accountprofileId, ref DataTransferModel dmodel, string strfrmdate, string strtodate, ComInfo comInfo, MMDbContext context, DateTime? dateFrm = null, DateTime? dateTo = null, string connectionString = null)
		{
			if (dateFrm == null && dateTo == null)
			{
				#region Date Ranges
				int year = DateTime.Now.Year;
				DateTime frmdate;
				DateTime todate;
				if (string.IsNullOrEmpty(strfrmdate))
				{
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
			}
			else
			{
				dmodel.FrmToDate = (DateTime)dateFrm;
				dmodel.ToDate = (DateTime)dateTo;
			}

			var dateformatcode = context.AppParams.FirstOrDefault(x => x.appParam == "DateFormat").appVal;
			List<string> sqllist = new List<string>();

			Helpers.ModelHelper.GetDataTransferData(context, accountprofileId, CheckOutType.Purchase, ref dmodel, connectionString);

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
			var DicItemBatSnVtList = new Dictionary<string, Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>>();
			var DicItemSnVtList = new Dictionary<string, List<SnVt>>();
			var DicItemVtList = new Dictionary<string, List<string>>();
			foreach (var itemcode in itemcodes)
			{
				DicItemBatVtList[itemcode] = new Dictionary<string, List<string>>();
				DicItemBatSnVtList[itemcode] = new Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>();
				DicItemSnVtList[itemcode] = new List<SnVt>();
				DicItemVtList[itemcode] = new List<string>();

				foreach (var batchcode in batchcodelist)
				{
					DicItemBatVtList[itemcode][batchcode] = new List<string>();
					DicItemBatSnVtList[itemcode][batchcode] = new List<MMLib.Models.Purchase.BatSnVt>();
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

					/*PurchaseNumber,PurchaseDate,SuppliersNumber,DeliveryStatus,ItemNumber,Quantity,Price,Discount,Memo,DeliveryDate,PurchaseStatus,AmountPaid,Ordered,Location,CardID,CurrencyCode,ExchangeRate,TaxCode,Job,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount
                     */
					value = string.Format("(" + strcolumn + ")", purchaseitem.pstCode, purchaseitem.PurchaseDate4ABSS, StringHandlingForSQL(purchaseitem.pstSupplierInvoice), "A", StringHandlingForSQL(purchaseitem.itmCode), purchaseitem.piQty, purchaseitem.piUnitPrice, purchaseitem.piDiscPc, memo, purchaseitem.PromisedDate4ABSS, status, "0", purchaseitem.piReceivedQty, purchaseitem.piStockLoc, StringHandlingForSQL(purchaseitem.supCode), StringHandlingForSQL(purchaseitem.pstCurrency), purchaseitem.pstExRate, purchaseitem.TaxCode, purchaseitem.JobNumber, purchaseitem.Myob_PaymentIsDue, purchaseitem.Myob_DiscountDays, purchaseitem.Myob_BalanceDueDays, "0");
					values.Add(value);
				}
				sql = string.Format(sql, string.Join(",", values));
				sqllist.Add(sql);
			}

			return sqllist;
		}

		private static string genMemo(string currencycode, double exchangerate, double paytypeamts, string pstRemark)
		{
			//return StringHandlingForSQL(string.Concat(currencycode, " ", paytypeamts, $"({exchangerate})", " ", pstRemark));
			return Helpers.ModelHelper.genMemo(currencycode, exchangerate, paytypeamts, pstRemark);
		}
		private static string StringHandlingForSQL(string str)
		{
			return CommonHelper.StringHandlingForSQL(str);
		}

		public static List<string> GetUploadPayBillSqlList(int accountprofileId, ref DataTransferModel dmodel, string strfrmdate, string strtodate, ComInfo comInfo, MMDbContext context, DateTime? dateFrm = null, DateTime? dateTo = null, string connectionString = null)
		{
			if (dateFrm == null && dateTo == null)
			{
				#region Date Ranges
				int year = DateTime.Now.Year;
				DateTime frmdate;
				DateTime todate;
				if (string.IsNullOrEmpty(strfrmdate))
				{
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
			}
			else
			{
				dmodel.FrmToDate = (DateTime)dateFrm;
				dmodel.ToDate = (DateTime)dateTo;
			}

			var dateformatcode = context.AppParams.FirstOrDefault(x => x.appParam == "DateFormat").appVal;
			List<string> sqllist = new List<string>();

			Helpers.ModelHelper.GetDataTransferData(context, accountprofileId, CheckOutType.PayBills, ref dmodel, connectionString);

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
			var DicItemBatSnVtList = new Dictionary<string, Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>>();
			var DicItemSnVtList = new Dictionary<string, List<SnVt>>();
			var DicItemVtList = new Dictionary<string, List<string>>();
			foreach (var itemcode in itemcodes)
			{
				DicItemBatVtList[itemcode] = new Dictionary<string, List<string>>();
				DicItemBatSnVtList[itemcode] = new Dictionary<string, List<MMLib.Models.Purchase.BatSnVt>>();
				DicItemSnVtList[itemcode] = new List<SnVt>();
				DicItemVtList[itemcode] = new List<string>();

				foreach (var batchcode in batchcodelist)
				{
					DicItemBatVtList[itemcode][batchcode] = new List<string>();
					DicItemBatSnVtList[itemcode][batchcode] = new List<MMLib.Models.Purchase.BatSnVt>();
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

					/*PurchaseNumber,PurchaseDate,SuppliersNumber,DeliveryStatus,ItemNumber,Quantity,Price,Discount,Memo,DeliveryDate,PurchaseStatus,AmountPaid,Ordered,Location,CardID,CurrencyCode,ExchangeRate,TaxCode,Job,PaymentIsDue,DiscountDays,BalanceDueDays,PercentDiscount
                     */
					value = string.Format("(" + strcolumn + ")", purchaseitem.pstCode, purchaseitem.PurchaseDate4ABSS, StringHandlingForSQL(purchaseitem.pstSupplierInvoice), "A", StringHandlingForSQL(purchaseitem.itmCode), purchaseitem.piQty, purchaseitem.piUnitPrice, purchaseitem.piDiscPc, memo, purchaseitem.PromisedDate4ABSS, status, "0", purchaseitem.piReceivedQty, purchaseitem.piStockLoc, StringHandlingForSQL(purchaseitem.supCode), StringHandlingForSQL(purchaseitem.pstCurrency), purchaseitem.pstExRate, purchaseitem.TaxCode, purchaseitem.JobNumber, purchaseitem.Myob_PaymentIsDue, purchaseitem.Myob_DiscountDays, purchaseitem.Myob_BalanceDueDays, "0");
					values.Add(value);
				}
				sql = string.Format(sql, string.Join(",", values));
				sqllist.Add(sql);
			}

			return sqllist;
		}

	}

	public class PurchaseReturnMsg
	{
		public string msg { get; set; }
		public string purchasecode { get; set; }
		public string reviewurl { get; set; }
		public string superiorphone { get; set; }
		public string status { get; set; }
		public int purchaselnlength { get; set; }
		public string superioremail { get; set; }
		public bool isdirectorboard { get; set; }
		public string supnames { get; set; }
		public string remark { get; set; }
	}
}
