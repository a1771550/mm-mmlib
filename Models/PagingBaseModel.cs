using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Models
{
	public class PagingBaseModel:MMCommonLib.BaseModels.PagingBaseModel
	{
		public bool EnableItemOptions { get { return int.Parse(ConfigurationManager.AppSettings["EnableItemOptions"]) == 1; } }
		public bool EnableItemVari { get { return int.Parse(ConfigurationManager.AppSettings["EnableItemVariations"]) == 1; } }
		public static bool enableItemOptions { get { return int.Parse(ConfigurationManager.AppSettings["EnableItemOptions"]) == 1; } }
		public static bool enableItemVari { get { return int.Parse(ConfigurationManager.AppSettings["EnableItemVariations"]) == 1; } }
	
		public SessUser User => HttpContext.Current.Session["User"] as SessUser;
		public static SessUser user => HttpContext.Current.Session["User"] as SessUser;
	}
}
