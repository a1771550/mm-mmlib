using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Helpers;
using MMDAL;

namespace MMLib.Models.Item
{
    public class ItemCategoryModel:Category
    {
        public string NameDisplay { get; set; }
        public string DescriptionDisplay { get; set; } = string.Empty;
        public string CreateTimeDisplay { get { return CreateTime == null ? "" : CommonHelper.FormatDateTime(CreateTime); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "" : CommonHelper.FormatDateTime((DateTime)ModifyTime); } }
        public bool Removable { get; set; }
    }
}
