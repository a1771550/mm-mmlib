using MMCommonLib.BaseModels;
using MMLib.Models.MYOB;
using System.Collections.Generic;

namespace MMLib.Models
{
    public class SessionStartModel:SessionStartModelBase
	{
	
		public ComInfoView CompanyInfo { get; set; }
		public ReceiptViewModel Receipt { get; set; }
		
		

		public List<OtherSettingsView> OtherSettings { get; set; }
        public string TaxCode { get; set; }

		public List<SalesmanModel> PosAbssSalesmanList { get; set; }
        public bool EnableTax { get; set; }
        public Dictionary<string, double> DicCurrencyExRate { get; set; }
        public bool UseForexAPI { get; set; }
        public List<MyobJobModel> JobList { get; set; }

        public SessionStartModel()
		{
			
			OtherSettings = new List<OtherSettingsView>();
			PosAbssSalesmanList = new List<SalesmanModel>();
		}
	}
}