using CommonLib.Helpers;
using System;
using MMDAL;
using System.Web;

namespace MMLib.Models.Item
{
    public class PromotionModel : MMDAL.Promotion
    {
        public ComInfo ComInfo { get { return HttpContext.Current.Session["ComInfo"] == null ? null : HttpContext.Current.Session["ComInfo"] as ComInfo; } }
        public static ComInfo comInfo { get { return HttpContext.Current.Session["ComInfo"] == null ? null : HttpContext.Current.Session["ComInfo"] as ComInfo; } }
        public bool IsObsolete { get
            {
                if (pro4Period && proDateTo!=null)
                {
                    if (DateTime.Today > (DateTime)proDateTo)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            } }

        #region JS Properties
        public string JsDateFrm { get; set; }
        public string JsDateTo { get; set; }
        #endregion
        public string CreateTimeDisplay { get { return CreateTime == null ? "N/A" : CommonHelper.FormatDateTime(CreateTime); } }      
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime); } }

        public string DateFrmDisplay { get { return (proDateFrm == null||!pro4Period) ? "N/A" : CommonHelper.FormatDate((DateTime)proDateFrm, DateFormat.YYYYMMDD); } }
        public string DateToDisplay { get { return (proDateTo == null||!pro4Period) ? "N/A" : CommonHelper.FormatDate((DateTime)proDateTo, DateFormat.YYYYMMDD); } }

        public string NameDisplay { get; set; }
        public string DiscountDisplay { get { return proDiscPc == null ? "0" : CommonHelper.FormatNumber((decimal)proDiscPc); } }
        public string PriceDisplay { get { return proPrice == null ? "0" : CommonHelper.FormatNumber((decimal)proPrice); } }
    }
}
