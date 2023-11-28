using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Helpers;
using MMDAL;

namespace MMLib.Models.Purchase
{
    public class PurchaseReturnItemModel : PurchaseItemModel
    {       
        public DateTime? pstReturnDate { get; set; }
        public string ReturnDateDisplay { get { return pstReturnDate == null ? "N/A" : CommonHelper.FormatDate((DateTime)pstReturnDate?.Date, true); } }
    }

}
