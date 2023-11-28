using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Models
{
	public class PagingBaseModel:MMCommonLib.BaseModels.PagingBaseModel
	{
		public SessUser User => HttpContext.Current.Session["User"] as SessUser;
	}
}
