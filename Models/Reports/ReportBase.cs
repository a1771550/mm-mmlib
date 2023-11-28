using MMCommonLib.BaseModels.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public abstract class ReportBase:ReportBaseModel
	{	
		public DeviceModel Device { get; set; }
	}
}