using CommonLib.Helpers;
using MMDAL;
using MMLib.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Resources = CommonLib.App_GlobalResources;

namespace MMLib.Helpers
{
    public static class MenuHelper
    {
        public static void UpdateMenus(MMDbContext context = null)
        {
            HttpContext.Current.Session["GeneralReports"] = GenReportMenus(context);
            HttpContext.Current.Session["Menus"] = GenMenus(context);
        }
        public static List<ReportView> GenReportMenus(MMDbContext context = null)
        {
            if (context == null)
            {
                using (context = new MMDbContext())
                {
                    return genReportViews(context);
                }
            }
            else
            {
                return genReportViews(context);
            }

        }
        private static List<ReportView> genReportViews(MMDbContext context)
        {
            var _reportlist = context.GetPosReportList().ToList();

            List<ReportView> reports = (from r in _reportlist
                                        select new ReportView
                                        {
                                            Code = r.Code,
                                            Name = r.Name
                                        }
                                                 ).ToList();
            foreach (var report in reports)
            {
                if (report.Code.ToLower() == "cps")
                {
                    report.Name = Resources.Resource.CountPaymentsSummary;
                }
                if (report.Code.ToLower() == "cpd")
                {
                    report.Name = Resources.Resource.CountPaymentsDetail;
                }
                if (report.Code.ToLower() == "sis")
                {
                    report.Name = Resources.Resource.SessionItemSales;
                }
                if (report.Code.ToLower() == "txd")
                {
                    report.Name = Resources.Resource.TransactionDetails;
                }
                if (report.Code.ToLower() == "pmd")
                {
                    report.Name = Resources.Resource.PaymentMethodsDetails;
                }
                if (report.Code.ToLower() == "its")
                {
                    report.Name = Resources.Resource.ItemSalesDetail;
                }
            }
            return reports;
        }

        public static Dictionary<string, Menu> GenMenus(MMDbContext context = null)
        {
            if (context == null)
            {
                using (context = new MMDbContext())
                {
                    return genMenus(context);
                }
            }
            else
            {
                return genMenus(context);
            }
        }

        private static Dictionary<string, Menu> genMenus(MMDbContext context)
        {
            Dictionary<string, Menu> DicMenus = new Dictionary<string, Menu>();
            int lang = CultureHelper.CurrentCulture;

            SetupModel model = new SetupModel();
            var mainfuncs = context.GetSysMainFuncList(lang).ToList();
            List<FuncView> funcslist = new List<FuncView>();
            foreach (var func in mainfuncs)
            {
                FuncView funcView = new FuncView
                {
                    sfnCode = func.sfnCode,
                    DisplayName = func.DisplayName,
                    ActionName = func.ActionName,
                    ControllerName = func.ControllerName
                };
                funcslist.Add(funcView);
            }

            model.MainFuncs = funcslist.OrderBy(x=>x.DisplayName).ToList();

            foreach (var func in model.MainFuncs)
            {
                DicMenus.Add(func.sfnCode, new Menu { ActionName = func.ActionName, ControllerName = func.ControllerName, DisplayText = func.DisplayName });
            }

            bool enableCRM = ConfigurationManager.AppSettings["enableCRM"] == "1";
            if (!enableCRM)
            {
                string[] crmmodules = ConfigurationManager.AppSettings["CRMModules"].Split(',');
                foreach (string crmmodule in crmmodules)
                {
                    DicMenus.Remove(crmmodule);
                }                
            }

            return DicMenus;
        }
    }
}
