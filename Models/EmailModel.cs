using MMDAL;
using System;
using System.Collections.Generic;
using CommonLib.Helpers;
using System.Configuration;
using System.Web;
using MMLib.Models.User;

namespace MMLib.Models
{
    public class EmailModel:EmailSetting
    {
        public bool enableCRM { get; set; }
        public List<UserModel> PosAdminList { get; set; }
        public List<SalesmanModel> PosSalesmanList { get; set; }
        public EmailModel()
        {
            //AccountProfileId = int.Parse(ConfigurationManager.AppSettings["AccountProfileId"]);
            AccountProfileId = (int)HttpContext.Current.Session["AccountProfileId"];
        }

        public string CreateTimeDisplay { get { return CreateTime == null ? "" : CommonHelper.FormatDateTime(CreateTime); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "" : CommonHelper.FormatDateTime((DateTime)ModifyTime); } }

        //public string emName { getPG; set; }
        public int iOffice365 { get; set; }
    }
}
