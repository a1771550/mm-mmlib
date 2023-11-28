using CommonLib.Helpers;
using MMCommonLib.BaseModels;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models.Purchase.Supplier
{
    public class SupplierModel : MMDAL.Supplier
    {
        private static ComInfo comInfo { get { return HttpContext.Current.Session["ComInfo"] as ComInfo; } }
        public ComInfo ComInfo
        {
            get { return HttpContext.Current.Session["ComInfo"] as ComInfo; }
        }

        public bool IsLastPurchasePrice { get; set; } = false;
        public string CreateTimeDisplay { get { return CreateTime == null ? "N/A" : CommonHelper.FormatDateTime(CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }
        public string[] StreetLines { get; set; }
        public string IpCountry { get; set; }
        public List<string> Countries { get; set; }
        public bool supIsOrganization { get; set; }
        public string AccountProfileName { get; set; }
        public string purchasecode { get; set; }
        public decimal TaxPercentageRate { get; set; }
        public List<Country> MyobCountries { get; set; }
        public HashSet<string> UploadFileList { get; set; } = new HashSet<string>();
        public List<string> ImgList;
        public List<string> FileList;
        
        /// <summary>
        /// for js only
        /// </summary>
        public decimal ExchangeRate { get; set; }
        public SupplierModel()
        {
            StreetLines = new string[4];
            AccountProfileId = ComInfo==null? 1: ComInfo.AccountProfileId;           
            supAbss = false;

            var helper = new CountryData.Standard.CountryHelper();
            Countries = helper.GetCountries().ToList();
            var region = CultureHelper.GetCountryByIP();
            IpCountry = region.EnglishName;

            ImgList = new List<string>();
            FileList = new List<string>();
        }     
    }
}
