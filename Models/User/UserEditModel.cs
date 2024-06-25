using System.Collections.Generic;

using System.Linq;
using System;
using MMLib.Helpers;
using System.Configuration;
using System.Text.RegularExpressions;
using MMDAL;
using Dapper;
using System.Runtime.Remoting.Contexts;
using MMLib.Models.Account;
using CommonLib.Helpers;
using ModelHelper = MMLib.Helpers.ModelHelper;
using System.Web.Mvc;
using PagedList;

namespace MMLib.Models.User
{
    public class UserEditModel : PagingBaseModel
    {
        public UserModel Staff { get; set; }
        public IPagedList<UserModel> PagingUserList { get; set; }
        public int? LicensedUserCount { get; private set; }
        public string BtnClass { get; private set; }
        public List<UserModel> UserList { get; set; }

        public bool checkpass { get; set; }
        public int icheckpass { get; set; }
        public int MaxCodeLength { get { return int.Parse(ConfigurationManager.AppSettings["MaxEmployeeCodeLength"]); } }

        public new UserModel User { get; set; }
        public List<long> AssignedContactIds { get; set; }

        public List<UserModel> StaffList { get; set; }

        public string MaxUserCode { get; set; }
        public Dictionary<string, string> DicAR { get; set; }

        public List<UserModel> SuperiorList { get; set; }
        public List<AccessRightModel> DefaultAccessRights { get; set; }
        public UserEditModel()
        {
            checkpass = false; StaffCodeList = new List<string>();

            AssignedContactIds = new List<long>(); StaffList = new List<UserModel>();
        }


        public static List<UserModel> GetUsers(int? isactive = 1, int SortCol = 0, string SortOrder = "desc", string Keyword = "")
        {
            if (sqlConnection.State == System.Data.ConnectionState.Closed) sqlConnection.Open();
            bool active = (isactive == null) ? true : (int)isactive == 1;
            return sqlConnection.Query<UserModel>(@"EXEC dbo.GetUsers @apId=@apId,@active=@active,@SortCol=@SortCol,@SortOrder=@SortOrder,@Keyword=@Keyword", new { apId, active, SortCol, SortOrder, Keyword }).ToList();
        }
        public void GetUserList(int? isactive = 1, int PageNo = 1, int SortCol = 0, string SortOrder = "desc", string Keyword = "")
        {
            if (Keyword == "") Keyword = null;
            this.SortCol = SortCol;
            this.Keyword = Keyword;
            this.PageNo = PageNo;
            CurrentSortOrder = SortOrder;
            //if(filter==0)
            this.SortOrder = SortOrder == "desc" ? "asc" : "desc";
            UserList = GetUsers(isactive, SortCol, SortOrder, Keyword);
            PagingUserList = UserList.ToPagedList(PageNo, PageSize);
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

        public List<SelectListItem> RoleList { get; set; }

        public static UserModel Get(int staffId, int? isactive = 1)
        {
            if (sqlConnection.State == System.Data.ConnectionState.Closed) sqlConnection.Open();
            bool active = (isactive == null) ? true : (int)isactive == 1;
            return sqlConnection.QueryFirstOrDefault<UserModel>(@"EXEC dbo.GetUsers @apId=@apId,@active=@active,@staffId=@staffId", new { apId, active, staffId });
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


        public static IsUserRole GetIsUserRole(SessUser user)
        {
            return new IsUserRole
            {
                isstaff = user.Roles.Contains(RoleType.Staff),
                isdepthead = user.Roles.Contains(RoleType.DeptHead),
                isfinancedept = user.Roles.Contains(RoleType.FinanceDept),
                ismuseumdirector = user.Roles.Contains(RoleType.MuseumDirector),
                isdirectorboard = user.Roles.Contains(RoleType.DirectorBoard),
                isapprover = user.Roles.Any(x => x != RoleType.Staff),
                isdirectorassistant = user.Roles.Contains(RoleType.DirectorAssistant),
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
                            u.surModifyTime = DateTime.Now;
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

        public void GetUser(int userId)
        {
            if (SqlConnection.State == System.Data.ConnectionState.Closed) SqlConnection.Open();
            List<SysFunc> funcs = new List<SysFunc>();
            GetUserList(apId);
            SuperiorList = UserList.Where(x => !x.UserRole.Contains(RoleType.Staff.ToString())).ToList();
            RoleList = SqlConnection.Query<SelectListItem>(@"EXEC dbo.GetRoleList @lang=@lang", new { lang = CultureHelper.CurrentCulture }).ToList();

            User = new UserModel();

            if (userId > 0)
            {
                User = UserList.FirstOrDefault(x => x.surUID == userId);
                if (User != null)
                {
                    User.RoleId = SqlConnection.QueryFirstOrDefault<int>(@"EXEC dbo.GetUserRole @apId=@apId,@userId=@userId", new { apId, userId = User.surUID });
                }
            }           
        }

        public void Edit(UserModel User)
        {
            using var context = new MMDbContext();

            SysUser user = null;

            bool editmode = User.surUID > 0;

            if (editmode)
            {
                user = context.SysUsers.Find(User.surUID);
                if (user != null)
                {
                    user.UserName = User.UserName;
                    user.Email = User.Email;

                    user.SuperiorIds = string.Join(",", User.SuperiorIdList);

                    if (User.checkpass) user.Password = HashHelper.ComputeHash(User.Password);

                    user.surModifyTime = DateTime.Now;
                    context.SaveChanges();
                }
            }
            else
            {
                User.surUID = context.SysUsers.OrderByDescending(x => x.surUID).FirstOrDefault().surUID + 1;
                User.UserCode = GetUserCode(User);
                string shop = ComInfo.Shop;
                string device = ComInfo.Device;
                int apId = ModelHelper.GetAccountProfileId(context);

                user = context.SysUsers.Add(new SysUser()
                {
                    surUID = User.surUID,
                    UserCode = User.UserCode,
                    UserName = User.UserName,
                    Password = HashHelper.ComputeHash(User.Password),
                    SuperiorIds = string.Join(",", User.SuperiorIdList),
                    surIsActive = true,
                    shopCode = shop,
                    dvcCode = device,
                    ManagerId = -1,
                    AccountProfileId = apId,
                    surScope = "pos",
                    Email = User.Email,
                    UserRole = "Staff",
                    surIsAbss = false,
                    surLicensed = true,
                    surCreateTime = DateTime.Now,
                });
                context.SaveChanges();
            }
            var rolecode = context.SysRoles.FirstOrDefault(x => x.Id == User.RoleId).rlCode;         
            UpdateUserRole(User, rolecode, context, user, editmode);

            static string GetUserCode(UserModel User)
            {
                if (sqlConnection.State == System.Data.ConnectionState.Closed) sqlConnection.Open();
                var MaxUserCode = sqlConnection.QueryFirstOrDefault<string>(@"EXEC dbo.GetMaxUserCodeById @roleId=@roleId", new { roleId = User.RoleId });

                if (string.IsNullOrEmpty(MaxUserCode)) return "staff00";

                Regex regex = new Regex("[^a-z]", RegexOptions.IgnoreCase);
                var role = regex.Replace(MaxUserCode, "");
                var resultString = Regex.Match(MaxUserCode, @"\d+").Value;
                var icode = int.Parse(resultString) + 1;

                resultString = icode < 10 ? icode.ToString().PadLeft(2, '0') : icode.ToString();

                return string.Concat(role, resultString);
            }

            static void UpdateUserRole(UserModel User, string rolecode, MMDbContext context, SysUser user, bool editmode)
            {
                //remove current record first:
                var userrole = context.UserRoles.FirstOrDefault(x => x.AccountProfileId == apId && x.UserId == User.surUID);
                if (userrole != null)
                {
                    context.UserRoles.Remove(userrole);
                    context.SaveChanges();
                }
                //add record:    
                UserRole userRole = new UserRole
                {
                    UserId = User.surUID,
                    RoleId = User.RoleId,
                    CreateTime = DateTime.Now,
                    AccountProfileId = apId
                };

                context.UserRoles.Add(userRole);

                user.UserRole = rolecode;
                user.surModifyTime = DateTime.Now;
                context.SaveChanges();
            }

       
        }
    }
}