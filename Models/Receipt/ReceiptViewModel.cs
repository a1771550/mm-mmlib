using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models
{
	public class ReceiptViewModel:Receipt
	{

		public List<string> Disclaimers { get; set; }
		public List<string> DisclaimerTxtList { get; set; }
        public List<string> PaymentTerms { get; set; }
        public List<string> PaymentTermsTxtList { get; set; }
    }

	public class Disclaimer
	{
		public string DisclaimerTxt { get; set; }
        public string DisclaimerCNTxt { get; set; }
        public string DisclaimerEngTxt { get; set; }
    }
}