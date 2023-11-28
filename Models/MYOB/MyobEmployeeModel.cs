using CommonLib.BaseModels.MYOB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMDAL;
using CommonLib.Models.MYOB;

namespace MMLib.Models.MYOB
{
    public class MyobEmployeeModel:MyobEmployee
    { 
        public List<AddressModel> AddressList { get; set; }
    }
}
