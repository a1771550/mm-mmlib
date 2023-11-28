using CommonLib.Helpers;
using System;

namespace MMLib.Models.Purchase
{
    public class BatchModel:MMDAL.Batch
    {
        public string validthru { get; set; }       
        public string BatVtDisplay { get { return batValidThru == null ? "" : CommonHelper.FormatDate((DateTime)batValidThru, true); } }
        public string JsStockInDate { get { return batStockInDate == null ? "" : CommonHelper.FormatDate((DateTime)batStockInDate, true); } }

    }
}
