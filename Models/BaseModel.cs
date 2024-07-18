using MMCommonLib.Models;
using System.Web;

namespace MMLib.Models
{
    public abstract class BaseModel
	{
		public ComInfoModel ComInfo { get { return HttpContext.Current.Session["ComInfo"] == null ? null : HttpContext.Current.Session["ComInfo"] as ComInfoModel; } }        
        public bool NonAbss { get { return ComInfo.DefaultCheckoutPortal.ToLower()=="nonabss"; } }
		public int apId { get { return ComInfo.AccountProfileId; } }
		public int AccountProfileId { get { return ComInfo.AccountProfileId; } }
    }
}