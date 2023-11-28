using CommonLib.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Promotion
{
    public class ItemPromotionModel
    {
        public long proId { get; set; }
        public string NameDisplay { get; set; }
        public string DescDisplay { get; set; } = string.Empty;
        public string itemCode { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? ModifyTime { get; set; }
        public DateTime? proDateFrm { get; set; }
        public DateTime? proDateTo { get; set; }
        public int? proQty { get; set; }
        public decimal? proPrice { get; set; }
        public decimal? proDiscPc { get; set; }
        public bool pro4Period { get; set; }
        public string DateFrmDisplay { get { return proDateFrm == null ? "" : CommonHelper.FormatDate((DateTime)proDateFrm, DateFormat.YYYYMMDD); } }
        public string DateToDisplay { get { return proDateTo == null ? "" : CommonHelper.FormatDate((DateTime)proDateTo, DateFormat.YYYYMMDD); } }        
        public string PriceDisplay { get { return proPrice == null ? CommonHelper.FormatNumber(0) : CommonHelper.FormatNumber((decimal)proPrice); } }
        public string DiscPcDisplay { get { return proDiscPc == null ? CommonHelper.FormatNumber(0) : CommonHelper.FormatNumber((decimal)proDiscPc); } }
        public string IPCreateTimeDisplay { get { return CreateTime == null ? "" : CommonHelper.FormatDateTime(CreateTime); } }
        public string IPModifyTimeDisplay { get { return ModifyTime == null ? "" : CommonHelper.FormatDateTime((DateTime)ModifyTime); } }
    }
}
