using MMCommonLib.CommonModels;
using MMDAL;
using System.Collections.Generic;

namespace MMLib.Models.User
{
    public class UserInfo:SysUser
    {
        public int ID { get; set; }        
        public List<AccountProfileDTO> AccountProfiles { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsUpdated { get; set; }
        public string Api { get; set; }
        public List<Report> Reports { get; set; }
        public List<UserEmployee> UserEmployees { get; set; }
        public List<UserSalesPerson> UserSalesPeople { get; set; }

        public List<Location> Locations { get; set; }
        public List<QuotationView> Quotations { get; set; }
        public List<AccountReceivableView> AccountReceivables { get; set; }
        public List<SPPView> SalesPersonPerformances { get; set; }
        public List<CIView> CustomersInvoicess { get; set; }
        public List<AccessRight> AccessRights { get; set; }
        public int FirstProfileId { get; set; }
        public int SelectedProfileId { get; set; }
        public List<int> ReportIds { get; set; }
        public List<int> SalesPersonIds { get; set; }
        public Dictionary<string, int> DicReportCount { get; set; }

        public UserInfo()
        {
            IsAuthenticated = false;
            IsUpdated = false;
        }
    }
}
