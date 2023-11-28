using CommonLib.Helpers;
using CommonLib.Models;
using MMCommonLib.BaseModels;
using MMDAL;
using MMLib.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MMLib.Models
{
    public class ActionLogEditModel : PagingBaseModel
    {
        public List<ActionLogModel> ActionLogList { get; set; }
        public PagedList.IPagedList<ActionLogModel> PagingActionLogList { get; set; }       

        public ActionLogEditModel()
        {

        }
        public ActionLogEditModel(string strfrmdate, string strtodate, string keyword = "") : this()
        {
            using (var context = new MMDbContext())
            {
                ActionLogList = GetList(context, strfrmdate, strtodate, keyword);
            }
        }

        public List<ActionLogModel> GetList(MMDbContext context = null, string strfrmdate = "", string strtodate = "", string keyword = "")
        {
            if (context == null)
            {
                using (context = new MMDbContext())
                {
                    return getlist(context, strfrmdate, strtodate, keyword);
                }
            }
            else
            {
                return getlist(context, strfrmdate, strtodate, keyword);
            }
        }

        private List<ActionLogModel> getlist(MMDbContext context, string strfrmdate, string strtodate, string keyword = "")
        {
            SearchDates = new SearchDates();
            CommonHelper.GetSearchDates(strfrmdate, strtodate, ref SearchDates);
            DateFromTxt = SearchDates.DateFromTxt;
            DateToTxt = SearchDates.DateToTxt;

            var apId = Helpers.ModelHelper.GetAccountProfileId(context);
            var user = HttpContext.Current.Session["User"] as SessUser;
            var shop = user.Device.dvcShop; //just in case, may be not in use at all
            var _actionlist = context.GetActionLogList(apId, SearchDates.frmdate, SearchDates.todate, shop, keyword);
            var actionlist = new List<ActionLogModel>();
            foreach (var action in _actionlist)
            {
                var _action = new ActionLogModel
                {
                    Id = action.Id,
                    actUserCode = action.actUserCode,
                    actName = action.actName,
                    actType = action.actType,
                    actOldValue = action.actOldValue,
                    actNewValue = action.actNewValue,
                    actRemark = action.actRemark,
                    actLogTime = action.actLogTime,
                    actCusCode = action.actCusCode,
                    actCustomerId = action.actCustomerId,
                    AccountProfileId = action.AccountProfileId,
                    actContactId = action.actContactId,
                    UserName = action.UserName,
                };
                actionlist.Add(_action);
            }
            return actionlist;
        }
    }
}
