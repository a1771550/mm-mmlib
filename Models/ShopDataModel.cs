using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace MMLib.Models
{
	public class ShopDataModel
	{
		public List<SerialNoView> SerialNoList { get; set; }
	
		public ShopDataModel()
		{
			SerialNoList = new List<SerialNoView>();
			
		}
	}
}
