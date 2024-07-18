using MMDAL;
using MMLib.Models.Account;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Resources = CommonLib.App_GlobalResources;

namespace MMLib.Models.User
{
    //[Serializable]
    public class UserModel
    {
        [Key]
        [Display(Name = "ID")]
        public int surUID { get; set; }
        [Display(Name = "UserCode", ResourceType = typeof(Resources.Resource))]
        [Required(ErrorMessageResourceName = "UserCodeRequired", ErrorMessageResourceType = typeof(Resources.Resource))]
        public string UserCode { get; set; }


        [Display(Name = "UserName", ResourceType = typeof(Resources.Resource))]
        [Required(ErrorMessageResourceName = "UserNameRequired", ErrorMessageResourceType = typeof(Resources.Resource))]
        public string UserName { get; set; }

        [Display(Name = "Email", ResourceType = typeof(Resources.Resource))]
        [Required(ErrorMessageResourceName = "EmailRequired", ErrorMessageResourceType = typeof(Resources.Resource))]
        [EmailAddress(ErrorMessageResourceName = "EmailFormatErrMsg", ErrorMessageResourceType = typeof(Resources.Resource))]
        public string Email { get; set; }

        [Display(Name = "Password", ResourceType = typeof(Resources.Resource))]
        [Required(ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(Resources.Resource))]
        [StringLength(10, ErrorMessageResourceName = "PasswordStrengthError", ErrorMessageResourceType = typeof(Resources.Resource), MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "ConfirmPassword", ResourceType = typeof(Resources.Resource))]
        [Required(ErrorMessageResourceName = "ConfirmPasswordRequired", ErrorMessageResourceType = typeof(Resources.Resource))]
        [StringLength(10, ErrorMessageResourceName = "PasswordStrengthError", ErrorMessageResourceType = typeof(Resources.Resource), MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Status", ResourceType = typeof(Resources.Resource))]
        public bool surIsActive { get; set; }
        public bool checkpass { get; set; }
    
        public string SalesmanName { get; set; }
        public string CreateTimeDisplay { get { return surCreateTime == null ? "N/A" : CommonLib.Helpers.CommonHelper.FormatDateTime((DateTime)surCreateTime); } }
        public string ModifyTimeDisplay { get { return surModifyTime == null ? "N/A" : CommonLib.Helpers.CommonHelper.FormatDateTime((DateTime)surModifyTime); } }
        public int IsActive { get { return surIsActive ? 1 : 0; } }
       
        public bool EnableAssistant { get; set; }
        public decimal? Threshold4DA { get; set; }
        public string Threshold4DADisplay { get { return Threshold4DA == null ? CommonLib.Helpers.CommonHelper.FormatNumber(0,false) : CommonLib.Helpers.CommonHelper.FormatNumber((decimal)Threshold4DA,false); } }
     
        public int CustomerId { get; set; }
        public string ManagerName { get; set; }
        public RoleType Role { get; set; }
       
        public long ContactId { get; set; }
        public string UserRole { get; set; }
        public string Roles { get; set; }
        public DateTime? surCreateTime { get; set; }
        public DateTime? surModifyTime { get; set; }
        public int ManagerId { get; set; }
        public string SuperiorIds { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int AccountProfileId { get; set; }
        public string Phone { get; set; }
        public HashSet<int> SuperiorIdList { get; set; }
        public int RoleId { get; set; }

        public UserModel()
        {          
            SuperiorIdList = new();
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
        DirectorAssistant=16,
        TransientMD=17,
	}

    public class IsUserRole
    {
        public bool isstaff { get; set; }
        public bool isdepthead { get; set; }
        public bool isfinancedept { get; set; }
        public bool ismuseumdirector { get; set; }
        public bool isdirectorboard { get; set; }
		public bool isapprover { get; set; }
        public bool isdirectorassistant { get; set; }
        public bool istransientmd { get; set; }
        public bool issystemadmin { get; set; }
	}
}