using CommonLib.BaseModels;
using MMCommonLib.BaseModels;
using MMCommonLib.CommonHelpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public class OtherSettingsModel:OtherSettingsModelBase
	{
		public List<OtherSettingsView> OtherSettings { get; set; }
		
		public DeviceModel Device { get; set; }
		public OtherSettingsModel():base()
		{			
			OtherSettings = new List<OtherSettingsView>();
		}
	}
}