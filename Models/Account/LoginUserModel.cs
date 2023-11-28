using System.Collections.Generic;
using System.Linq;
using System;
using System.Web.Mvc;
using MMDAL;
using System.Configuration;
using System.Web;

namespace MMLib.Models
{
    public class LoginUserModel : SysUser
    {
        //public ComInfo ComInfo { getPG; set; } //comInfo should be unknown before login!
        public bool enableCRM { get; set; }
        public string LoginMode { get; set; }
        public bool IsPNG { get { return ConfigurationManager.AppSettings["IsPNG"] == "1"; } }

        public string SelectedShop { get; set; }

        public string SelectedDevice { get; set; }

        public List<SelectListItem> ShopList { get; set; }
        public List<SelectListItem> DeviceList { get; set; }

        public List<AccountProfileView> AccountProfiles { get; set; }
        public string SelectedAccountProfile { get; set; }
        public SelectList AccountProfileList { get; set; }

        public int iCentral { get; set; }
        //public bool IsCentral { getPG { return ConfigurationManager.AppSettings["IsCentral"] == "1"; } }
        public string RedirectUrl { get; set; }
        public List<SelectListItem> UserNameIdList { get; set; }
   
        public string SelectedEmail { get; set; }
        public string CompanyCode { get; set; }

        public LoginUserModel()
        {           
            AccountProfiles = new List<AccountProfileView>();
            UserNameIdList = new List<SelectListItem>();  
        }

    }
}