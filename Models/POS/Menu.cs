using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	[Serializable]
	public class Menu
	{
		public string ActionName { get; set; }
		public string ControllerName { get; set; }
		public string DisplayText { get; set; }
	}
}