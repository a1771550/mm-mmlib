using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Helpers;
using MMDAL;

namespace MMLib.Models.Item
{
    public class ItemAttributeModel:ItemAttribute
    {
        #region js properties:
        public string tmpId { get; set; }
        public string CreateTimeDisplay { get { return CreateTime == null ? "" : CommonHelper.FormatDateTime(CreateTime); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "" : CommonHelper.FormatDateTime((DateTime)ModifyTime); } }
        #endregion
    }
}
