using MMDAL;
using System;
using System.Collections.Generic;

namespace MMLib.Models.User
{
    //[Serializable]
    public class UserModel : SysUser
    {
        public bool checkpass { get; set; }
        public string ConfirmPassword { get; set; }
        public string SalesmanName { get; set; }
        public string CreateTimeDisplay { get { return surCreateTime == null ? "N/A" : CommonLib.Helpers.CommonHelper.FormatDateTime(surCreateTime); } }
        public string ModifyTimeDisplay { get { return surModifyTime == null ? "N/A" : CommonLib.Helpers.CommonHelper.FormatDateTime((DateTime)surModifyTime); } }
        public int IsActive { get { return surIsActive ? 1 : 0; } }
       
        public List<AccessRight> AccessRights { get; set; }
        public int CustomerId { get; set; }
        public string ManagerName { get; set; }
        public RoleType Role { get; set; }
       
        public long ContactId { get; set; }

        public UserModel()
        {
            AccessRights = new List<AccessRight>();
        }

    }

    
    public enum RoleType
    {
        SuperAdmin = 1,
        Admin = 2,
        CRMAdmin = 3,
        CRMSalesManager = 5,
        SalesPerson = 6,
        CRMSalesPerson = 7,
        SalesManager = 8,
        ARAdmin = 9,
		DeptHead = 10,
		FinanceDept = 11,
		MuseumDirector = 12,
        DirectorBoard = 13,
		Staff = 14,
        SystemAdmin = 15,
	}

    public class IsUserRole
    {
        public bool isstaff { get; set; }
        public bool isdepthead { get; set; }
        public bool isfinancedept { get; set; }
        public bool ismuseumdirector { get; set; }
        public bool isdirectorboard { get; set; }
		public bool isapprover { get; set; }
	}
}