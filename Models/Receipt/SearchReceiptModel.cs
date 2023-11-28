using MMLib.Models.Item;
using System.Collections.Generic;

namespace MMLib.Models
{
	public class SearchReceiptModel
	{
	
		public string retmsg { get; set; }
		public List<ItemModel> items { get; set;}
		public Dictionary<string, List<SerialNoView>> dicItemSNs { get; set; }
		public bool IsEpay { get; set; }
	}
}
