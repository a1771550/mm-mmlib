using CommonLib.Models.MYOB;
using MMDAL;
using System.Collections.Generic;

namespace MMLib.Models
{
    public class ComInfoModel:ComInfo
    {
        public DataType dataType { get; set; }
        public List<string> StockLocations { get; set; }
        public ComInfoModel() { StockLocations = new List<string>(); }
    }

}
