using MMLib.Models.POS.MYOB;
using MMLib.Models.Purchase;
using MMLib.Models.Supplier;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Models
{
	public class PagingBaseModel:MMCommonLib.BaseModels.PagingBaseModel
	{
		public bool EnableAssistant { get { return ConfigurationManager.AppSettings["EnableAssistant"] == "1"; } }
        public static bool enableAssistant { get { return ConfigurationManager.AppSettings["EnableAssistant"] == "1"; } }
        public bool EnableItemOptions { get { return int.Parse(ConfigurationManager.AppSettings["EnableItemOptions"]) == 1; } }
		public bool EnableItemVari { get { return int.Parse(ConfigurationManager.AppSettings["EnableItemVariations"]) == 1; } }
		public static bool enableItemOptions { get { return int.Parse(ConfigurationManager.AppSettings["EnableItemOptions"]) == 1; } }
		public static bool enableItemVari { get { return int.Parse(ConfigurationManager.AppSettings["EnableItemVariations"]) == 1; } }
	
		public SessUser User => HttpContext.Current.Session["User"] as SessUser;
		public static SessUser user => HttpContext.Current.Session["User"] as SessUser;

        public List<string> ImgList;
        public List<string> FileList;

        public MyobSupplierModel SelectedSupplier { get; set; }
        public List<PoQtyAmtModel> PoQtyAmtList { get; set; }
        public Dictionary<string, double> DicCurrencyExRate { get; set; }
        public string JsonDicCurrencyExRate { get { return JsonSerializer.Serialize(DicCurrencyExRate); } }
    }
}
