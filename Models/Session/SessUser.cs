using MMLib.Models.User;
using System.Collections.Generic;

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
		public new List<RoleType> Roles { get; set; }
        public int? SalesGroupId { get; set; }
		public string PurchasePrefix { get; set; }

		public SessUser()
		{
			StaffList = new List<UserModel>();
		}
	}
}