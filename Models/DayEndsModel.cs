using CommonLib.Helpers;
using MMCommonLib.BaseModels;
using MMCommonLib.CommonHelpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Resources = CommonLib.App_GlobalResources;

namespace MMLib.Models
{
	public class DayEndsModel : DayEndsModelBase
	{	
		
		public List<DayEndsItem> DayEndsItems { get; set; }
		public List<ReportView> Reports { get; set; }
		public List<PaymentType> PaymentTypes { get; set; }
		

		
		public List<DayEndsItem> ExImViews { get; set; }

	
		//public bool IsLocal { getPG; set; }
		//public List<string> AccountProfileList { getPG; set; }

		public CheckInOutModel CheckInOut { get; set; }
		public SessUser ShopKeeper { get; set; }

		public string[] CentralFileNames { get; set; }
		public string[] CentralFileCategories { get; set; }

		public List<AccountProfileView> AccountProfiles { get; set; }		
		
		public Dictionary<string, DeviceModel> DicDeviceInfo { get; set; }
		public List<DeviceModel> DeviceList { get; set; }		
		public DayEndsModel():base()
		{			
			int lang = Helpers.ModelHelper.GetCurrentCulture();
			ShopCodeList = new List<string>();
			CentralFileCategories = new string[] { "PGCustomer", "MyobItem" };
			CentralFileNames = new string[] { "Customers_", "Items_", "ItemLoc_", "ItemPrice_" };

		

			AccountProfiles = new List<AccountProfileView>();

			FileNameList = new List<string>();
			FileInfoList = new List<FileInfo>();
			ExImViews = new List<DayEndsItem>();
			//IsLocal = HttpContext.Current.Request.IsLocal;
			Session currsess = null;
			DicDeviceInfo = new Dictionary<string, DeviceModel>();
			
			DeviceList = new List<DeviceModel>();
			
			using (var context = new MMDbContext())
			{
				DicDeviceInfo = Helpers.ModelHelper.GetDicDeviceInfo(context);			

				ShopCodeList = (from st in context.MyobLocStocks
								select st.lstStockLoc
								).Distinct().ToList();

				PaymentTypes = context.PaymentTypes.Where(x => x.pmtIsActive == true).ToList();
				currsess = MMLib.Helpers.ModelHelper.GetCurrentSession(context);
				ShopCode = currsess.sesShop;

				IsCentral = (bool)currsess.IsCentral;

				AccountProfiles = (from d in context.AccountProfiles
								   where d.IsActive == true
								   select new AccountProfileView
								   {
									   Id = d.Id,
									   ProfileName = d.ProfileName,
									   DsnName = d.DsnName,
									   ProfilePath = d.ProfilePath
								   }
						   ).ToList();
				//AccountProfileList = new SelectList(AccountProfiles, "Id", "ProfileName");

				if (!IsCentral)
				{
					MyobFileName = AccountProfiles.FirstOrDefault(x => x.Id == currsess.AccountProfileId).ProfileName;
					SelectedAccountProfileId = (int)currsess.AccountProfileId;
				}


				var enablecashdrawer = context.AppParams.FirstOrDefault(x => x.appParam == "EnableCashDrawerAmt");
				EnableCashDrawer = enablecashdrawer.appVal == "1";
				if (EnableCashDrawer)
				{
					CashDrawerAmtStart = currsess.sesCashAmtStart == null ? 0 : (decimal)currsess.sesCashAmtStart;
					CashDrawerAmtStartTxt = CommonHelper.FormatMoney(null, CashDrawerAmtStart, true);
				}
				ComInfoView comInfoView = Helpers.ModelHelper.GetCompanyInfo(context);
				AccountNo = int.Parse(comInfoView.comAccountNo);

				LastDayendsSessionTimeDisplay = CommonHelper.FormatDateTime(Helpers.ModelHelper.GetLastSessionTime(context));

			}
			DicPayTypes = MMCommonLib.CommonHelpers.ModelHelper.GetDicPayTypes(lang);

			Reports = new List<ReportView>();

			DeviceModel device = (DeviceModel)HttpContext.Current.Session["Device"];

			DayEndsItems = new List<DayEndsItem>();
			DayEndsItem ExView = null;
			DayEndsItem ImView = null;


			if (IsCentral)
			{
				ExView = new DayEndsItem
				{
					Code = "DayendsExportFrmCentral",
					Name = Resources.Resource.DayendsImportFrmCentral
				};
				ImView = new DayEndsItem
				{
					Code = "DayendsImportFrmShop",
					Name = Resources.Resource.DayendsImportFrmShop
				};
				DayendsFolderPathList = new List<string>();
			}
			else
			{
				ExView = new DayEndsItem
				{
					Code = "DayendsExportFrmShop",
					Name = Resources.Resource.DayendsImportFrmShop
				};
				ImView = new DayEndsItem
				{
					Code = "DayendsImportFrmCentral",
					Name = Resources.Resource.DayendsImportFrmCentral
				};
			}
			ExImViews.Add(ExView);
			ExImViews.Add(ImView);

			if (currsess != null)
			{				
				if (IsCentral)
				{
                    ExportBasePath = MMCommonLib.CommonHelpers.FileHelper.GetExportBasePath();
				}
				else
				{
					var myobfilename = AccountProfiles.FirstOrDefault(x => x.Id == currsess.AccountProfileId).ProfileName;
                    ImportBasePath = MMCommonLib.CommonHelpers.FileHelper.GetImportBasePath(myobfilename);
				}
						
			}

		}

	}

	
}