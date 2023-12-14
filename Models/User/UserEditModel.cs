using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Resources = CommonLib.App_GlobalResources;
using System.Linq;
using System;
using MMLib.Helpers;
using MMCommonLib.BaseModels;
using System.Configuration;
using System.Web;
using System.Text.RegularExpressions;
using MMDAL;

namespace MMLib.Models.User
{
	public class UserEditModel : PagingBaseModel
	{
		public UserModel Staff { get; set; }
		public PagedList.IPagedList<UserModel> UserList { get; set; }

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

		[Display(Name = "AccessRights", ResourceType = typeof(Resources.Resource))]
		public List<AccessRight> AccessRights { get; set; }

		[Display(Name = "Status", ResourceType = typeof(Resources.Resource))]
		public bool surIsActive { get; set; }
		public bool checkpass { get; set; }
		public int icheckpass { get; set; }
		//public int isActive { getPG; set; }
		public int MaxCodeLength { get { return int.Parse(ConfigurationManager.AppSettings["MaxEmployeeCodeLength"]); } }



		public List<long> AssignedContactIds { get; set; }

		public List<UserModel> StaffList { get; set; }

		public string MaxSalesCode { get; set; }

		public List<UserModel> SuperiorList { get; set; }
		//public List<UserModel> AddableEmployeeList { getPG; set; }
		//public string JsonAddableEmployeeList { getPG { return Newtonsoft.Json.JsonConvert.SerializeObject(AddableEmployeeList); } }
		public UserEditModel()
		{
			AccessRights = new List<AccessRight>(); checkpass = false; StaffCodeList = new List<string>();

			AssignedContactIds = new List<long>(); StaffList = new List<UserModel>();
		}
		public UserEditModel(bool foradd, MMDbContext context) : base()
		{
			AccessRights = new List<AccessRight>();
			if (foradd)
			{
				//List<SysUser> users = context.SysUsers.Where(x => !x.surIsActive).ToList();
				//if (users != null && users.Count > 0)
				//{
				//	MaxSalesCode = users.OrderBy(x => x.surUID).First().UserCode;
				//}
				//else
				//{
				MaxSalesCode = context.GetMaxUserCode("staff").FirstOrDefault();
				if (string.IsNullOrEmpty(MaxSalesCode))
				{
					MaxSalesCode = "staff00";
				}

				var resultString = Regex.Match(MaxSalesCode, @"\d+").Value;
				var icode = int.Parse(resultString) + 1;

				resultString = icode < 10 ? icode.ToString().PadLeft(2, '0') : icode.ToString(); //RIGHT HERE!!!

				MaxSalesCode = string.Concat("staff", resultString);
				//}


				//GetDefaultAccessRights
				var _accessrights = context.GetDefaultAccessRights(apId, "staff01").ToList();

				foreach (var a in _accessrights)
				{
					AccessRights.Add(new AccessRight
					{
						UserCode = a.UserCode,
						FuncCode = a.FuncCode
					});
				}
			}
		}


		public static List<UserModel> GetUserList(MMDbContext context = null)
		{
			if (context == null)
			{
				using (context = new MMDbContext())
				{
					return getUserList(context);
				}
			}
			return getUserList(context);
		}


		private static List<UserModel> getUserList(MMDbContext context)
		{
			List<UserModel> users = new List<UserModel>();
			var _users = context.GetUserList6(comInfo.AccountProfileId).ToList();
			if (_users != null && _users.Count > 0)
			{
				foreach (var u in _users)
				{
					users.Add(new UserModel
					{
						surUID = u.surUID,
						UserCode = u.UserCode,
						UserName = u.UserName,
						Email = u.Email,
						surIsActive = u.surIsActive,
						ManagerId = u.ManagerId,
						dvcCode = u.dvcCode,
						shopCode = u.shopCode,
						UserRole = u.Roles,
					});
				}
			}

			foreach (var user in users)
			{
				if (user.ManagerId > 0)
				{
					user.ManagerName = ModelHelper.GetManagerName(users, user.ManagerId);
				}
			}

			return users;
		}

		public static List<UserModel> GetStaffList(List<UserModel> userlist)
		{
			List<UserModel> staff = new List<UserModel>();
			foreach (var user in userlist)
			{
				if (user.ManagerId > 0)
				{
					staff.Add(user);
				}
			}
			return staff;
		}

		public List<string> StaffCodeList { get; set; }
		public int ManagerId { get; set; }
		public string SuperiorIds { get; set; }

		public static UserModel Get(int staffId)
		{
			UserModel staff = new UserModel();
			using (var context = new MMDbContext())
			{
				ComInfo comInfo = HttpContext.Current.Session["ComInfo"] as ComInfo;

				var u = context.SysUsers.FirstOrDefault(x => x.surUID == staffId);
				if (u != null)
				{
					staff.surUID = u.surUID;
					staff.UserCode = u.UserCode;
					staff.UserName = u.UserName;
					staff.Email = u.Email;
					staff.surIsActive = u.surIsActive;
					//staff.IsActive = u.surIsActive ? 1 : 0;
					staff.ManagerId = u.ManagerId;
					staff.dvcCode = u.dvcCode;
					staff.shopCode = u.shopCode;
					staff.ManagerName = ModelHelper.GetManagerName(GetUserList(context), u.ManagerId);
					staff.AccountProfileId = u.AccountProfileId;
				}
				return staff;
			}
		}

		public static void Add(UserModel model)
		{
			using (var context = new MMDbContext())
			{
				var apId = ModelHelper.GetAccountProfileId(context);
				var staffman = new SysUser
				{
					UserCode = model.UserCode,
					UserName = model.UserName,
					surIsActive = true,
					Password = CommonLib.Helpers.HashHelper.ComputeHash(model.Password),
					surScope = "pos",
					surCreateTime = DateTime.Now,
					surModifyTime = DateTime.Now,
					AccountProfileId = apId,
				};
				context.SysUsers.Add(staffman);
				context.SaveChanges();
			}
		}






		public void GetSuperiorList(MMDbContext context)
		{
			var userlist = context.GetUserList6(apId);
			var _superiorlist = userlist.Where(x => !x.Roles.Contains(RoleType.Staff.ToString()));
			SuperiorList = new List<UserModel>();
			foreach (var a in _superiorlist)
			{
				SuperiorList.Add(
					new UserModel
					{
						surUID = a.surUID,
						UserCode = a.UserCode,
						UserName = a.UserName,
						//DisplayName = a.DisplayName,
						Email = a.Email,
						//shopCode = a.shopCode,
					}
					);
			}
		}

		public static IsUserRole GetIsUserRole(SessUser user)
		{
			return new IsUserRole
			{
				isstaff = user.Roles.Contains(RoleType.Staff),
				isdepthead = user.Roles.Contains(RoleType.DeptHead),
				isfinancedept = user.Roles.Contains(RoleType.FinanceDept),
				ismuseumdirector = user.Roles.Contains(RoleType.MuseumDirector),
				isdirectorboard = user.Roles.Contains(RoleType.DirectorBoard),
				isapprover = user.Roles.Any(x => x != RoleType.Staff)
			};
		}

		public static void Delete(int staffId)
		{
			using (var context = new MMDbContext())
			{
				using (var transaction = context.Database.BeginTransaction())
				{
					try
					{
						var u = context.SysUsers.FirstOrDefault(x => x.surUID == staffId);
						if (u != null)
						{
							u.surIsActive = false;
							//context.SysUsers.Remove(u);							
							List<UserRole> userroles = context.UserRoles.Where(x => x.AccountProfileId == comInfo.AccountProfileId && x.UserId == u.surUID).ToList();
							if (userroles.Count > 0)
							{
								context.UserRoles.RemoveRange(userroles);
							}
							context.SaveChanges();
							transaction.Commit();
						}
					}
					catch (Exception ex)
					{
						transaction.Rollback();
						throw new Exception(ex.Message);
					}
				}

			}
		}
	}
}