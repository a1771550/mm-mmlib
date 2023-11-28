using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLib.Models.Purchase
{
    public class ValidThruModel:MMDAL.ValidThru
    {
        public string ValidThruDisplay { get { return vtValidThru==null?"":CommonLib.Helpers.CommonHelper.FormatDate((DateTime)vtValidThru,true); } }
    }
}
