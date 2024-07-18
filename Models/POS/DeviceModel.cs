using MMCommonLib.Models;
using MMDAL;
using System;
using System.Configuration;
using System.Web;

namespace MMLib.Models
{
	//[Serializable]
	public class DeviceModel:Device
	{	
        public string DeviceInfoFileName { get; set; }
        //public int AccountNo { getPG; set; }
        public string AccountProfileName { get; set; }
        //public int AccountProfileId { getPG; set; }
		public DeviceModel()
		{           
            DeviceInfoFileName = HttpContext.Current.Session["ComInfo"]==null? ConfigurationManager.AppSettings["DeviceInfoFileName"]: (HttpContext.Current.Session["ComInfo"] as ComInfoModel).DeviceInfoFileName;
		}      
    }
}