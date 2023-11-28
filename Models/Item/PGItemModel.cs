using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMDAL;
using MMLib.Helpers;

namespace MMLib.Models.Item
{
    public class PGItemModel:ItemView
    {
       

        public PGItemModel()
        {
            DicLocQty = new Dictionary<string, int>();
            PLs = new Dictionary<string, decimal?>();
            DicItemAccounts = new Dictionary<string, string>();
            AttrList = new List<ItemAttributeModel>();
            SelectedAttrList4V = new List<ItemAttribute>();
        }
    }
}
