using CommonLib.Helpers;
using MMCommonLib.BaseModels;
using MMCommonLib.CommonHelpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
    public class SerialNoView : SerialNoViewBase
    {
        #region JS properties
        public int snSeq { get; set; }
        public string snValidThru { get; set; }
        #endregion
        public string itmNameDesc { get; set; }
        public string wslSellUnit { get; set; }
        public DateTime? wslValidThru { get; set; }
        public string ValidThruDisplay { get { return snoValidThru == null ? "" : CommonHelper.FormatDate((DateTime)snoValidThru); } }

        public decimal? wslSellingPrice { get; set; }
        public decimal? wslTaxPc { get; set; }
        public decimal? wslTaxAmt { get; set; }
        public decimal? wslLineDiscAmt { get; set; }
        public decimal? wslLineDiscPc { get; set; }
        public decimal? wslSalesAmt { get; set; }
        public long wslUID { get; set; }
        //public object seq { get; set; }
    }
}