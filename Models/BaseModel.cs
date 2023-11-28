using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public abstract class BaseModel
	{
		public ComInfo ComInfo { get { return HttpContext.Current.Session["ComInfo"] == null ? null : HttpContext.Current.Session["ComInfo"] as ComInfo; } }        
        public bool NonAbss { get { return ComInfo.DefaultCheckoutPortal.ToLower()=="nonabss"; } }
		public int apId { get { return ComInfo.AccountProfileId; } }
		public int AccountProfileId { get { return ComInfo.AccountProfileId; } }
		public string CheckoutPortal { get { return ComInfo.DefaultCheckoutPortal; } }	
        public int CompanyId { get { return ComInfo.Id; } }
        public bool ApprovalMode { get { return (bool)ComInfo.ApprovalMode; } }
    }
}