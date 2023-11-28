using CommonLib.Helpers;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.MYOB
{
    public class MyobLocationModel:MyobLocation
    {
        public string CreateTimeDisplay { get { return CommonHelper.FormatDate(CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDate((DateTime)ModifyTime, true); } }
    }
}
