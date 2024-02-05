using MMDAL;
using System.Collections.Generic;

namespace MMLib.Models.Purchase
{
    public class PoSettings
	{
		public decimal poThreshold { get; set; }
		public List<PoQtyAmtModel> poSettingItems { get; set; }

		public PoSettings()
		{
			poSettingItems = new List<PoQtyAmtModel>();
		}
    }
	public class PoQtyAmtModel:PoQtyAmt
	{
		
	}
}
