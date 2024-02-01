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

namespace MMLib.Models.User
{
    public class UserEditModel : PagingBaseModel
    {
        public UserModel Staff { get; set; }
        public PagedList.IPagedList<UserModel> PagingUserList { get; set; }
        public List<UserModel> UserList { get; set; }

        public bool checkpass { get; set; }
        public int icheckpass { get; set; }
        //public int isActive { getPG; set; }
        public int MaxCodeLength { get { return int.Parse(ConfigurationManager.AppSettings["MaxEmployeeCodeLength"]); } }

        public new UserModel User { get; set; }
        public List<long> AssignedContactIds { get; set; }

        public List<UserModel> StaffList { get; set; }

        public string MaxSalesCode { get; set; }
        public Dictionary<string, string> DicAR { get; set; }

        public List<UserModel> SuperiorList { get; set; }
        public List<AccessRightModel> DefaultAccessRights { get; set; }
        public UserEditModel()
        {
            checkpass = false; StaffCodeList = new List<string>();

            AssignedContactIds = new List<long>(); StaffList = new List<UserModel>();
        }


        public static List<UserModel> GetUserList(int? isactive = 1)
        {
            if (sqlConnection.State == System.Data.ConnectionState.Closed) sqlConnection.Open();
            bool active = (isactive == null) ? true : (int)isactive == 1;
            return sqlConnection.Query<UserModel>(@"EXEC dbo.GetUsers @apId=@apId,@active=@active", new { apId, active }).ToList();
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

            //GetAccessRights
            DefaultAccessRights = SqlConnection.Query<AccessRightModel>(@"EXEC dbo.GetAccessRights @apId=@apId,@usercode=@usercode", new { apId, usercode = "staff03" }).ToList();
            DicAR = ModelHelper.GetDicAR(DefaultAccessRights);

            UserList = GetUserList(apId);
            SuperiorList = UserList.Where(x => !x.UserRole.Contains(RoleType.Staff.ToString())).ToList();

            User = new UserModel();

            if (userId > 0)
            {
                User = UserList.FirstOrDefault(x => x.surUID == userId);
                if (User != null)
                {
                    var accessRights = SqlConnection.Query<AccessRightModel>(@"EXEC dbo.GetAccessRights @apId=@apId,@usercode=@usercode", new { apId, usercode = User.UserCode }).ToList();
                    if (accessRights != null && accessRights.Count > 0) User.AccessRights = accessRights.Select(x => x.FuncCode).ToList();
                }
            }
            else
            {
                MaxSalesCode = SqlConnection.QueryFirstOrDefault<string>(@"EXEC dbo.GetMaxUserCode @role=@role", new { role = "staff" });

                if (string.IsNullOrEmpty(MaxSalesCode)) MaxSalesCode = "staff00";

                var resultString = Regex.Match(MaxSalesCode, @"\d+").Value;
                var icode = int.Parse(resultString) + 1;

                resultString = icode < 10 ? icode.ToString().PadLeft(2, '0') : icode.ToString(); //RIGHT HERE!!!

                MaxSalesCode = string.Concat("staff", resultString);

                User.AccessRights = DefaultAccessRights.Select(x => x.FuncCode).ToList();
            }
        }

        public void Edit(UserModel User)
        {
            int userId = User.surUID;
            using var context = new MMDbContext();
            if (userId > 0)
            {
                SysUser user = context.SysUsers.Find(userId);
                if (user != null)
                {
                    user.UserName = User.UserName;
                    user.Email = User.Email;

                    user.SuperiorIds = string.Join(",", User.SuperiorIdList);

                    if (User.checkpass) user.Password = HashHelper.ComputeHash(User.Password);

                    user.surModifyTime = DateTime.Now;
                }

                List<string> funcCodes = new List<string>();
                var dicARs = ModelHelper.GetDicAR(context, CultureHelper.CurrentCulture);
                for (var i = 0; i < dicARs.Count; i++)
                {
                    if (formCollection["FuncCodes[" + i + "]"] != null)
                    {
                        funcCodes.Add(formCollection["FuncCodes[" + i + "]"]);
                    }
                }

                if (funcCodes.Count == 0)
                {
                    //the user is removed from all access rights
                    context.AccessRights.RemoveRange(context.AccessRights.Where(x => x.AccountProfileId == apId && x.UserCode == user.UserCode));
                }
                else
                {
                    //remove current accessright first before adding:
                    context.AccessRights.RemoveRange(context.AccessRights.Where(x => x.AccountProfileId == apId && x.UserCode == user.UserCode));
                    List<AccessRight> ars = new List<AccessRight>();
                    foreach (string code in funcCodes)
                    {
                        AccessRight ar = new AccessRight
                        {
                            UserCode = user.UserCode,
                            FuncCode = code,
                            AccountProfileId = apId,
                            CreateTime = DateTime,
                            ModifyTime = DateTime
                        };
                        ars.Add(ar);
                    }
                    context.AccessRights.AddRange(ars);
                }
                TempData["message"] = string.Format(Resources.Resource.ARsaved, user.UserName);
                context.SaveChanges();

            }
            else
            {

                var usercode = formCollection["UserCode"];

                string shop = ComInfo.Shop;
                string device = ComInfo.Device;
                int apId = ModelHelper.GetAccountProfileId(context);
                SysUser user = null;
                int newUserId = context.SysUsers.OrderByDescending(x => x.surUID).FirstOrDefault().surUID + 1;

                user = new SysUser()
                {
                    surUID = newUserId,
                    UserCode = usercode,
                    UserName = formCollection["UserName"],
                    Password = HashHelper.ComputeHash(formCollection["Password"]),
                    //surIsActive = formCollection["userstatus"] == "1",
                    surIsActive = true,
                    shopCode = shop,
                    dvcCode = device,
                    ManagerId = -1,
                    AccountProfileId = apId,
                    surScope = "pos",
                    Email = formCollection["Email"],
                    UserRole = "Staff",
                    surIsAbss = false,
                    surLicensed = true,
                    surCreateTime = DateTime,
                    surModifyTime = DateTime
                };
                context.SysUsers.Add(user);
                context.SaveChanges();

                var roleId = context.SysRoles.Where(x => x.rlCode == "Staff").FirstOrDefault().Id;
                UserRole userRole = new UserRole
                {
                    UserId = user.surUID,
                    RoleId = roleId,
                    CreateTime = DateTime,
                    ModifyTime = DateTime,
                    AccountProfileId = apId
                };
                context.UserRoles.Add(userRole);
                context.SaveChanges();

                List<string> funcCodes = new List<string>();
                var dicARs = ModelHelper.GetDicAR(context, CultureHelper.CurrentCulture);
                for (var i = 0; i < dicARs.Count; i++)
                {
                    if (formCollection["FuncCodes[" + i + "]"] != null)
                    {
                        funcCodes.Add(formCollection["FuncCodes[" + i + "]"]);
                    }
                }
                if (funcCodes.Count > 0)
                {
                    //remove current accessright first before adding:
                    context.AccessRights.RemoveRange(context.AccessRights.Where(x => x.UserCode == user.UserCode));
                    context.SaveChanges();
                    //add records:
                    List<AccessRight> ars = new List<AccessRight>();
                    foreach (string code in funcCodes)
                    {
                        AccessRight ar = new AccessRight
                        {
                            UserCode = user.UserCode,
                            FuncCode = code,
                            AccountProfileId = apId,
                            CreateTime = DateTime,
                            ModifyTime = DateTime
                        };
                        ars.Add(ar);
                    }
                    context.AccessRights.AddRange(ars);
                    context.SaveChanges();
                }

            }
        }
    }
}