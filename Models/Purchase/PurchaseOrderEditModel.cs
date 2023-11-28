using CommonLib.Helpers;
using Dapper;
using PagedList;
using MMCommonLib.BaseModels;
using MMDAL;
using MMLib.Models.Item;
using MMLib.Models.Purchase.Supplier;
using MMLib.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MMLib.Models.Purchase
{
    public class PurchaseOrderEditModel : PagingBaseModel
    {
        public PurchaseModel PurchaseOrder { get; set; }
        public List<PurchaseModel> PurchaseOrderList { get; set; }
        public List<PurchaseItemModel> PurchaseItems { get; set; }
        public List<SerialNoView> SerialNoList { get; set; }
        public Dictionary<string, List<SerialNoView>> DicItemSNs;
        public List<ItemModel> Items { get; set; }
        public SupplierModel Supplier;
        public PurchaseOrderEditModel() { }
        public PagedList.IPagedList<PurchaseModel> PagingPurchaseOrderList { get; set; }
        public string KPurchasemanCode { get; set; }
        public string SearchModeList { get; set; }
        public void GetPurchaseOrderList(string strfrmdate, string strtodate, int PageNo, string SortName, string SortOrder, string Keyword, int filter, string searchmode)
        {
            CommonHelper.DateRangeHandling(strfrmdate, strtodate, out DateTime frmdate, out DateTime todate, DateFormat.YYYYMMDD, '-');
            using var context = new MMDbContext();
            if (Keyword == "") Keyword = null;
            DateFromTxt = strfrmdate;
            DateToTxt = strtodate;
            this.SortName = SortName;
            this.Keyword = Keyword;
            this.PageNo = PageNo;
            CurrentSortOrder = SortOrder;
            //if(filter==0)
            this.SortOrder = SortOrder == "desc" ? "asc" : "desc";

            if (filter == 1)
            {
                SortOrder = SortOrder == "desc" ? "asc" : "desc";
            }
            PurchaseOrderList = new List<PurchaseModel>();


            SessUser user = HttpContext.Current.Session["User"] as SessUser;
            if (user.Roles.Any(x => x == RoleType.Admin || x == RoleType.SalesManager))
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
                connection.Open();
                var orderlist = connection.Query<PurchaseOrderModel>(@"EXEC dbo.GetPurchaseOrderList4Approval4Admin1 @shop=@shop,@frmdate=@frmdate,@todate=@todate,@sortName=@sortName,@sortOrder=@sortOrder", new { shop = ShopCode, frmdate, todate, sortName = SortName, sortOrder = SortOrder }).ToList();
                orderlist = filterOrderList(Keyword, searchmode, orderlist);

                if (orderlist.Count > 0)
                {
                    var groupedorderlist = orderlist.GroupBy(x => x.pstCode).ToList();
                    foreach (var group in groupedorderlist)
                    {
                        var g = group.FirstOrDefault();
                        var itemnamedescs = string.Join(",", group.Select(x => x.ItemNameDesc).ToList());
                        var itemcodes = string.Join(",", group.Select(x => x.itmCode).ToList());

                        PurchaseOrderList.Add(
                    new PurchaseModel
                    {
                        Id = g.Id,
                        pstLocStock = g.pstLocStock,
                        pstCode = g.pstCode,
                        pstRefCode = g.pstRefCode,
                        pstType = g.pstType,
                        pstStatus = g.pstStatus,
                        pstPurchaseDate = g.pstPurchaseDate,
                        pstPurchaseTime = g.pstPurchaseTime,
                        supCode = g.supCode,
                        pstRemark = g.pstRemark,
                        pstReviewUrl = g.pstReviewUrl,
                        pstSendNotification = g.pstSendNotification,
                        pstSupplierInvoice = g.pstSupplierInvoice,
                        pstPromisedDate = g.pstPromisedDate,
                        pstRecurName = g.pstRecurName,
                        pstCheckout = g.pstCheckout,
                        CreateBy = g.CreateBy,
                        CreateTime = g.CreateTime,
                        ModifyTime = g.ModifyTime,
                        Supplier = new SupplierModel
                        {
                            supCode = g.supCode,
                            supName = g.supName,
                            supFirstName = g.supFirstName,
                        },
                        ItemCodes = itemcodes,
                        ItemsNameDesc = itemnamedescs,
                        PurchasePersonName = g.CreateBy,
                        PurchaseOrderReview = new PurchaseOrderReviewModel
                        {
                            IsApproved = g.IsApproved,
                            ApprovedBy = g.ApprovedBy,
                            IsRejected = g.IsRejected,
                            RejectedBy = g.RejectedBy,
                            Reason = g.Reason,
                            PassedToManager = g.PassedToManager,
                            EmailNotified = g.EmailNotified,
                        }
                    }
                        );
                    }
                    PagingPurchaseOrderList = PurchaseOrderList.ToPagedList(PageNo, PageSize);
                }
            }
            else
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
                connection.Open();
                var orderlist = connection.Query<PurchaseOrderModel>(@"EXEC dbo.GetPurchaseOrderList4Approval1 @shop=@shop,@username=@username,@frmdate=@frmdate,@todate=@todate,@sortName=@sortName,@sortOrder=@sortOrder", new { shop = ShopCode, username = user.UserName, frmdate, todate, sortName = SortName, sortOrder = SortOrder }).ToList();
                orderlist = filterOrderList(Keyword, searchmode, orderlist);
                if (orderlist.Count > 0)
                {
                    var groupedorderlist = orderlist.GroupBy(x => x.pstCode).ToList();
                    foreach (var group in groupedorderlist)
                    {
                        var g = group.FirstOrDefault();
                        var itemnamedescs = string.Join(",", group.Select(x => x.ItemNameDesc).ToList());
                        var itemcodes = string.Join(",", group.Select(x => x.itmCode).ToList());

                        PurchaseOrderList.Add(
                   new PurchaseModel
                   {
                       Id = g.Id,
                       pstLocStock = g.pstLocStock,
                       pstCode = g.pstCode,
                       pstRefCode = g.pstRefCode,
                       pstType = g.pstType,
                       pstStatus = g.pstStatus,
                       pstPurchaseDate = g.pstPurchaseDate,
                       pstPurchaseTime = g.pstPurchaseTime,
                       supCode = g.supCode,
                       pstRemark = g.pstRemark,
                       pstReviewUrl = g.pstReviewUrl,
                       pstSendNotification = g.pstSendNotification,
                       pstSupplierInvoice = g.pstSupplierInvoice,
                       pstPromisedDate = g.pstPromisedDate,
                       pstRecurName = g.pstRecurName,
                       pstCheckout = g.pstCheckout,
                       CreateBy = g.CreateBy,
                       CreateTime = g.CreateTime,
                       ModifyTime = g.ModifyTime,
                       Supplier = new SupplierModel
                       {
                           supCode = g.supCode,
                           supName = g.supName,
                           supFirstName = g.supFirstName,
                       },
                       ItemCodes = itemcodes,
                       ItemsNameDesc = itemnamedescs,
                       PurchasePersonName = g.CreateBy,
                       PurchaseOrderReview = new PurchaseOrderReviewModel
                       {
                           IsApproved = g.IsApproved,
                           ApprovedBy = g.ApprovedBy,
                           IsRejected = g.IsRejected,
                           RejectedBy = g.RejectedBy,
                           Reason = g.Reason,
                           PassedToManager = g.PassedToManager,
                           EmailNotified = g.EmailNotified,
                       }
                   }
                        );
                    }
                    PagingPurchaseOrderList = PurchaseOrderList.ToPagedList(PageNo, PageSize);
                }
            }
        }

        private List<PurchaseOrderModel> filterOrderList(string Keyword, string searchmode, List<PurchaseOrderModel> orderlist)
        {
            if (!string.IsNullOrEmpty(Keyword))
            {
                SearchModeList = searchmode;
                var keyword = Keyword.ToLower();
                var SearchMode = searchmode.Split(',');

                if (SearchMode.Length == 1)
                {
                    if (SearchMode[0] == "0")//default: salesperson
                    {
                        orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "1")
                    {
                        orderlist = orderlist.Where(x => x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstLocStock != null && x.pstLocStock.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "2")
                    {
                        orderlist = orderlist.Where(x => x.itmCode != null && x.itmCode.ToLower().Contains(keyword) || x.itmName != null && x.itmName.ToLower().Contains(keyword) || x.itmDesc != null && x.itmDesc.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "3")
                    {
                        orderlist = orderlist.Where(x => x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.supName != null && x.supName.ToLower().Contains(keyword)).ToList();
                    }
                }
                if (SearchMode.Length == 2)
                {
                    if (SearchMode[0] == "0" && SearchMode[1] == "1")
                    {
                        orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstLocStock != null && x.pstLocStock.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "0" && SearchMode[1] == "2")
                    {
                        orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.itmCode != null && x.itmCode.ToLower().Contains(keyword) || x.itmName != null && x.itmName.ToLower().Contains(keyword) || x.itmDesc != null && x.itmDesc.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "0" && SearchMode[1] == "3")
                    {
                        orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.supName != null && x.supName.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "1" && SearchMode[1] == "2")
                    {
                        orderlist = orderlist.Where(x => x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstLocStock != null && x.pstLocStock.ToLower().Contains(keyword) || x.itmCode != null && x.itmCode.ToLower().Contains(keyword) || x.itmName != null && x.itmName.ToLower().Contains(keyword) || x.itmDesc != null && x.itmDesc.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "1" && SearchMode[1] == "3")
                    {
                        orderlist = orderlist.Where(x => x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstLocStock != null && x.pstLocStock.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.supName != null && x.supName.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "2" && SearchMode[1] == "3")
                    {
                        orderlist = orderlist.Where(x => x.itmCode != null && x.itmCode.ToLower().Contains(keyword) || x.itmName != null && x.itmName.ToLower().Contains(keyword) || x.itmDesc != null && x.itmDesc.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.supName != null && x.supName.ToLower().Contains(keyword)).ToList();
                    }
                }

                if (SearchMode.Length == 3)
                {
                    if (SearchMode[0] == "0" && SearchMode[1] == "1" && SearchMode[2] == "2")
                    {
                        orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstLocStock != null && x.pstLocStock.ToLower().Contains(keyword) || x.itmCode != null && x.itmCode.ToLower().Contains(keyword) || x.itmName != null && x.itmName.ToLower().Contains(keyword) || x.itmDesc != null && x.itmDesc.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "0" && SearchMode[1] == "1" && SearchMode[2] == "3")
                    {
                        orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstLocStock != null && x.pstLocStock.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.supName != null && x.supName.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "0" && SearchMode[1] == "2" && SearchMode[2] == "3")
                    {
                        orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.itmCode != null && x.itmCode.ToLower().Contains(keyword) || x.itmName != null && x.itmName.ToLower().Contains(keyword) || x.itmDesc != null && x.itmDesc.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.supName != null && x.supName.ToLower().Contains(keyword)).ToList();
                    }
                    if (SearchMode[0] == "1" && SearchMode[1] == "2" && SearchMode[2] == "3")
                    {
                        orderlist = orderlist.Where(x => x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstLocStock != null && x.pstLocStock.ToLower().Contains(keyword) || x.itmCode != null && x.itmCode.ToLower().Contains(keyword) || x.itmName != null && x.itmName.ToLower().Contains(keyword) || x.itmDesc != null && x.itmDesc.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.supName != null && x.supName.ToLower().Contains(keyword)).ToList();
                    }
                }
                if (SearchMode.Length == 4)
                {
                    orderlist = orderlist.Where(x => x.CreateBy != null && x.CreateBy.ToLower().Contains(keyword) || x.pstSupplierInvoice != null && x.pstSupplierInvoice.ToLower().Contains(keyword) || x.supCode != null && x.pstCode.ToLower().Contains(keyword) || x.pstLocStock != null && x.pstLocStock.ToLower().Contains(keyword) || x.itmCode != null && x.itmCode.ToLower().Contains(keyword) || x.itmName != null && x.itmName.ToLower().Contains(keyword) || x.itmDesc != null && x.itmDesc.ToLower().Contains(keyword) || x.supCode != null && x.supCode.ToLower().Contains(keyword) || x.supName != null && x.supName.ToLower().Contains(keyword)).ToList();
                }
            }

            return orderlist;
        }
    }
}
