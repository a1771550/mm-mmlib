﻿using CommonLib.Helpers;
using CommonLib.Models.MYOB;
using MMLib.Models.MYOB;
using System;
using System.Collections.Generic;

namespace MMLib.Models.POS.MYOB
{
    public class MyobSupplierModel:MMDAL.MyobSupplier
    {       
        public decimal? Payment { get; set; } = null;
        public bool Selected { get; set; }
        public string pstCode { get; set; }
        public string fileName { get; set; }
        public string filePath { get; set; }
        public string Files { get; set; }
        public long piId { get; set; }
        public DateTime piCreateTime { get; set; }
        public string piCreateTimeDisplay { get { return CommonHelper.FormatDateTime(piCreateTime); } }
        public bool IsLastPurchasePrice { get; set; } = false;
        public string CreateTimeDisplay { get { return CreateTime == null ? "N/A" : CommonHelper.FormatDateTime(CreateTime, true); } }
        public string ModifyTimeDisplay { get { return ModifyTime == null ? "N/A" : CommonHelper.FormatDateTime((DateTime)ModifyTime, true); } }
        public List<AddressModel> AddressList { get; set; }
        public MyobTerms Terms { get; set; }

        public string[] StreetLines { get; set; }

        public decimal Amount { get; set; }
        public string AmountDisplay { get { return CommonHelper.FormatNumber(Amount); } }
        public bool supIsOrganization { get; set; }
        public string AccountProfileName { get; set; }
        public string purchasecode { get; set; }
        public decimal TaxPercentageRate { get; set; }

        public HashSet<string> UploadFileList { get; set; } = new HashSet<string>();
        public List<string> ImgList;
        public List<string> FileList;
        public bool Removable { get; set; }

        public string Remark { get; set; }
    }
}
