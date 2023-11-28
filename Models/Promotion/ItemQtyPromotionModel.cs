using CommonLib.Helpers;
using System;
using System.Collections.Generic;

namespace MMLib.Models.Item
{
    public class ItemQtyPromotionModel:MMDAL.ItemQtyPromotion
    {
        public int proQty { get; set; } = 0;
        public decimal? proPrice { get; set; }
        public decimal? proDiscPc { get; set; }
       public string catName { get; set; }
        public string catNameSC { get; set; }
        public string catNameTC { get; set; }
        public string itmName { get; set; }        
        public string ipIds { get; set; }
        public string[] itemCodes { get; set; } = new string[0];
        public DateTime IPCreateTime { get; set; }
        public DateTime IPModifyTime { get; set; }
        public int catId { get; set; } = 0; 
        public string proName { get; set; }
        public string proNameTC { get; set; }
        public string proNameSC { get; set; }
        public string proNameDisplay { get; set; }
        public string proDescDisplay { get; set; }
     
        public string PriceDisplay { get { return proPrice == null ? CommonHelper.FormatNumber(0) : CommonHelper.FormatNumber((decimal)proPrice); } }
        public string DiscPcDisplay { get { return proDiscPc == null ? CommonHelper.FormatNumber(0) : CommonHelper.FormatNumber((decimal)proDiscPc); } }
        public string CreateTimeDisplay { get { return CreateTime == null ? "" : CommonHelper.FormatDateTime(CreateTime); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "" : CommonHelper.FormatDateTime((DateTime)ModifyTime); } }
        public string IPCreateTimeDisplay { get { return IPCreateTime == null ? "" : CommonHelper.FormatDateTime(IPCreateTime); } }
        public string IPModifyTimeDisplay { get { return IPModifyTime == null ? "" : CommonHelper.FormatDateTime((DateTime)IPModifyTime); } }
        //public ItemModel SelectedItem { getPG; set; }
        public List<ItemCategoryModel> CategoryList { get; set; }
        public List<PromotionModel> PromotionList { get; set; }
    }
}
