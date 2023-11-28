using CommonLib.Helpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Resources = CommonLib.App_GlobalResources;

namespace MMLib.Models.Item
{
    public class TransferModel : StockTransfer
    {   
        public string stockId { get; set; }
        public string TransferDateDisplay { get { return stDate == null ? "N/A" : CommonHelper.FormatDate(stDate, true); } }
        public string CreateTimeDisplay { get { return CommonHelper.FormatDateTime(CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime==null?"N/A": CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }

        public int? outQtySum { get; set; }
        public int? VarianceSum { get; set; }
        public string ItemCodeList { get; set; }
        public string ItemNameDescList { get; set; }
        public string SenderList { get; set; }
        public bool Checked { get; set; }
        public string CheckedDisplay { get { return Checked ? Resources.Resource.Yes : Resources.Resource.Not; } }
    }

    public class TransferLnModel : StockTransferLn
    {
        public string CreateTimeDisplay { get { return CommonHelper.FormatDateTime(CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }
    }
}
