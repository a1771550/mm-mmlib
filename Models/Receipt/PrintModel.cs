using MMCommonLib.BaseModels;
using MMDAL;
using System.Collections.Generic;
using System.Linq;
using Resources = CommonLib.App_GlobalResources;
using CommonLib.Helpers;
using System.Web;
using MMLib.Models.Purchase;

namespace MMLib.Models
{
	public class PrintModel : PrintModelBase
    {       
        public Dictionary<string, string> DicItemBatVt { get; set; }
        public Dictionary<string, string> DicItemSnVt { get; set; }
        public Dictionary<string, string> DicItemVt { get; set; }
        public Dictionary<string, Dictionary<string, List<string>>> DicItemBatVtList { get; set; }
        public Dictionary<string, Dictionary<string, List<Purchase.BatSnVt>>> DicItemBatSnVtList { get; set; }
        public Dictionary<string, List<SnVt>> DicItemSnVtList { get; set; }
        public Dictionary<string, List<string>> DicItemVtList { get; set; }
        public ReceiptViewModel Receipt { get; set; }
        public OtherSettingsView PrintingFields { get; set; }
        public DeviceModel Device { get; set; }

     
        public new Dictionary<string, List<SerialNoView>> DicItemSNs { get; set; }
        public List<SerialNoView> SerialNoList { get; set; }
        public PrintModel() : base() { }

        public PrintModel(bool issales, string salesrefundcode, string currency) : base(issales)
        {
            Currency = currency;
            using (var context = new MMDbContext())
            {
                SysUser user = HttpContext.Current.Session["User"] as SysUser;
                Device = Helpers.ModelHelper.GetDevice(user.surUID, context);
                //var companyinfo = Helpers.ModelHelper.GetCompanyInfo(context);
                CompanyName = ComInfo.comName.ToUpper();
                CompanyAddr1 = ComInfo.comAddress1;
                CompanyAddr2 = ComInfo.comAddress2;

                ShowSN = context.AppParams.FirstOrDefault(x => x.appParam == "EnableSNonReceipt").appVal == "1";

                int apId = ComInfo.AccountProfileId;

                Lang = CultureHelper.CurrentCulture;

                UserName = context.SysUsers.FirstOrDefault(x => x.UserCode == user.UserCode).UserName;

             
                    ReceiptTitle = Resources.Resource.Receipt;
                

                forPreorder = salesrefundcode.ToLower().IndexOf('o') >= 0;
             

                if (forPreorder) ReceiptTitle = string.Concat(ReceiptTitle, " (", Resources.Resource.Preorder, ")");

               

                DicPayAmt = new Dictionary<string, decimal>();

               

                OtherSettingsView otherSettingsView = (from a in context.AppParams
                                                       where a.appParam == "LogoReceipt"
                                                       select new OtherSettingsView
                                                       {
                                                           appVal = a.appVal
                                                       }
                                                       ).FirstOrDefault();
                UseLogo = otherSettingsView.appVal == "1";
                if (UseLogo)
                {
                    LogoPath = string.Format(ComInfo.ReceiptLogoUrl, UriHelper.GetBaseUrl());
                }

                PrintingFields = (from a in context.AppParams
                                  where a.appParam == "ReceiptPrintFields"
                                  select new OtherSettingsView
                                  {
                                      appParam = a.appParam,
                                      appVal = a.appVal
                                  }
                               ).FirstOrDefault();
                printingfieldlist = new List<string>();
                if (!string.IsNullOrEmpty(PrintingFields.appVal))
                {
                    printingfieldlist = PrintingFields.appVal.Split(',').ToList();
                }

                TaxModel = Helpers.ModelHelper.GetTaxInfo(context);

                Receipt = Helpers.ModelHelper.GetReceipt(context, TaxModel);
            }
        }
    }
}