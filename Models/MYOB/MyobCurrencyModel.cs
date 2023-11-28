using CommonLib.Helpers;
using MMDAL;
using System;

namespace MMLib.Models.MYOB
{
    public class MyobCurrencyModel:MyobCurrency
    {
        public string CreateTimeDisplay { get { return CreateTime==null?"N/A": CommonHelper.FormatDate((DateTime)CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDate((DateTime)ModifyTime, true); } }
    }
}
