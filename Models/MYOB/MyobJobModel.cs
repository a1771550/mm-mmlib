using CommonLib.Helpers;
using System;

namespace MMLib.Models.MYOB
{
    public class MyobJobModel:MMDAL.MyobJob
    {
        public string CreateTimeDisplay { get { return CommonHelper.FormatDate(CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime==null?"N/A": CommonHelper.FormatDate((DateTime)ModifyTime, true); } }
    }
}
