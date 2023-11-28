using MMLib.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	//[Serializable]
	public class SessUser:UserModel
	{
		public bool EnableCheckDayends { get; set; }
		public string NetworkName { get; set; }
		public string PrinterName { get; set; }
		public string FullPrinterName { get { return System.IO.Path.Combine(string.Format(@"\\{0}",NetworkName), PrinterName); } }
				
		public List<UserModel> StaffList { get; set; }
        public DeviceModel Device { get; set; }
		public List<RoleType> Roles { get; set; }
        public int? SalesGroupId { get; set; }

        public SessUser()
		{
			StaffList = new List<UserModel>();
		}
	}
}