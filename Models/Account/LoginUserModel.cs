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
        public string SelectedShop { get; set; }
        public string SelectedDevice { get; set; }
        public string SelectedAccountProfile { get; set; }
        public SelectList AccountProfileList { get; set; }
        public string RedirectUrl { get; set; }
        public string CompanyCode { get; set; }

        public MMDbContext DBContext { get { return new MMDbContext(); } }

        public LoginUserModel()
        {
        }

        public LoginUserModel(string redirectUrl, int? superiorId = 0)
        {
            RedirectUrl = redirectUrl;
            if (superiorId > 0)
            {
                using (DBContext)
                {
                    var user = DBContext.SysUsers.FirstOrDefault(x => x.surUID == superiorId);
                    if (user != null) { HttpContext.Current.Session["User"] = user; Email = user.Email; }
                }
            }
        }
    }
}