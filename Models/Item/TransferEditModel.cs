using CommonLib.Helpers;
using CommonLib.Models;
using PagedList;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Device = MMDAL.Device;

namespace MMLib.Models.Item
{
    public class TransferEditModel : StockModel
    {
        //public PrintModel printModel { get; set; }

        public Dictionary<string, ItemModel> dicItemQty = new Dictionary<string, ItemModel>();

        public List<IGrouping<string, TransferModel>> GroupedTransferList { get; set; }

        public TransferEditModel()
        {
        }

        public TransferEditModel(string code) : this()
        {
            Code = code;
            TransferList = new List<TransferModel>();
            using var context = new MMDbContext();
            var transferlist = context.GetStockTransferListByCode5(ComInfo.AccountProfileId, code).ToList();
            if (transferlist.Count > 0)
            {
                foreach (var v in transferlist)
                {
                    TransferList.Add(
                        new TransferModel
                        {
                            Id = v.Id,
                            stCode = v.stCode,
                            stSender = v.stSender,
                            stReceiver = v.stReceiver,
                            itmCode = v.itmCode,
                            itmNameDesc = v.ItemNameDesc,
                            inQty = v.inQty,
                            outQty = v.outQty,
                            stCounted = v.stCounted,
                            stVariance = v.stVariance,
                            stSignedUp_Sender = v.stSignedUp_Sender,
                            stSignedUp_Receiver = v.stSignedUp_Receiver,
                            stShop = v.stShop,
                            stDate = v.stDate,
                            ivIdList = v.ivIdList,
                            poIvId = v.poIvId,
                            CreateTime = v.CreateTime,
                            ModifyTime = v.ModifyTime,
                            AccountProfileId = comInfo.AccountProfileId,
                        }
                        );
                }
            }
        }

        public string PrimaryLocation { get; set; }
        public List<ItemModel> ItemList { get; set; }
        public List<TransferModel> TransferList { get; set; }
        public IPagedList<TransferModel> TransferPagingList { get; set; }
        public string TransferNumber { get; set; }
        public string TransferDateDisplay { get; set; }
        public string Code { get; }

        public string GetTransferNumber(MMDbContext context)
        {
            //DeviceModel device = HttpContext.Current.Session["Device"] as DeviceModel;// don't use session here, otherwise non-updatable!!!
            Device device = context.Devices.Find((HttpContext.Current.Session["Device"] as DeviceModel).dvcUID);
            return string.Concat(device.dvcTransferCode, $"{device.dvcNextTransferNo:000000}");
        }

        public void GetStockList(int apId, int PageNo = 1, int Size_Of_Page = 10, int SortCol = 0, string SortOrder = "desc", string Keyword = null)
        {
            int No_Of_Page = PageNo;
            ItemList = new List<ItemModel>();

            using var context = new MMDbContext();
            TransferNumber = GetTransferNumber(context);

            string lastupdatetime = string.Empty;
            var allownegativestock = context.AppParams.Single(x => x.appParam == "EnableAllowNegativeStockOnSales" && x.AccountProfileId == ComInfo.AccountProfileId).appVal;

            int startIndex = (PageNo - 1) * Size_Of_Page;
            var stockinfo = context.GetStockInfo6(apId).ToList();
            int totalCount = 0;
            ItemList = Helpers.ModelHelper.GetItemList(apId, context, stockinfo, startIndex, Size_Of_Page, out totalCount, Keyword);

            DateTime _lastupdatetime = (DateTime)context.MyobLocStocks.FirstOrDefault(x => x.AccountProfileId == ComInfo.AccountProfileId).lstModifyTime;
            LastUpdateTime = CommonHelper.FormatDateTime(_lastupdatetime);

            EnableBuySellUnits = context.AppParams.Single(x => x.appParam == "EnableBuySellUnits" && x.AccountProfileId == ComInfo.AccountProfileId).appVal == "1";

            foreach (var item in ItemList)
            {
                Dictionary<string, int> dic = new Dictionary<string, int>();
                if (item.lstStockLoc != null)
                {
                    dic[item.lstStockLoc] = item.QuantityAvailable;
                    var ditem = new DistinctItem
                    {
                        ItemCode = item.itmCode.Trim(),
                        ItemName = item.itmName,
                        ItemDesc = item.itmDesc,
                        ItemTaxRate = item.itmTaxPc == null ? 0 : (double)item.itmTaxPc,
                        IsNonStock = item.itmIsNonStock,
                        ItemSupCode = item.itmSupCode,

                    };
                    DicItemLocQty.Add(ditem, dic);
                }
            }
            var itemlist = DicItemLocQty.Keys.ToList();
            if (!string.IsNullOrEmpty(Keyword))
            {
                string keyword = Keyword.ToLower();
                itemlist = itemlist.Where(x => x.ItemCode.ToLower().Contains(keyword) || ((x.ItemDesc != null) && x.ItemDesc.ToLower().Contains(keyword)) || x.ItemName.ToLower().Contains(keyword) || ((x.ItemSupCode != null) && x.ItemSupCode.ToLower().Contains(keyword))
                                         ).ToList();
            }
            var sortColumnIndex = SortCol;
            var sortDirection = SortOrder;
            if (sortColumnIndex == 0)
            {
                itemlist = sortDirection == "asc" ? itemlist.OrderBy(c => c.ItemCode).ToList() : itemlist.OrderByDescending(c => c.ItemCode).ToList();
            }
            else if (sortColumnIndex == 1)
            {
                itemlist = sortDirection == "asc" ? itemlist.OrderBy(c => c.ItemName).ToList() : itemlist.OrderByDescending(c => c.ItemName).ToList();
            }

            CurrentSortOrder = SortOrder == "desc" ? "asc" : "desc";
            StockList = itemlist.ToPagedList(No_Of_Page, Size_Of_Page);

        }

        public void ProcessTransfer(List<JsStock> JsStockList, List<TransferModel> TransferList)
        {
            using var context = new MMDbContext();
            DateTime datetime = DateTime.Now;

            foreach (var item in JsStockList)
            {
                var stock = context.MyobLocStocks.FirstOrDefault(x => x.Id == item.Id);
                if (stock != null)
                {
                    stock.lstQuantityAvailable = item.Qty;
                    stock.lstModifyTime = datetime;
                }
            }
            context.SaveChanges();

            var newstLnlist = new List<StockTransferLn>();
            foreach (var stock in TransferList)
            {
                if (stock.inQty > 0 || stock.outQty > 0)
                {
                    StockTransferLn st = new StockTransferLn
                    {
                        stCode = stock.stCode,
                        stSender = stock.stSender,
                        stReceiver = stock.stReceiver,
                        itmCode = stock.itmCode,
                        inQty = stock.inQty,
                        outQty = stock.outQty,
                        stShop = stock.stShop,
                        stDate = datetime.Date,                       
                        AccountProfileId = ComInfo.AccountProfileId,
                        CreateTime = datetime,
                        ModifyTime = datetime
                    };
                    newstLnlist.Add(st);
                }
            }

            context.StockTransferLns.AddRange(newstLnlist);
            context.SaveChanges();

            var grouplist = getGroupedTransferList(newstLnlist, context);

            getTransferList(grouplist, true, context);
            var devId = (HttpContext.Current.Session["Device"] as DeviceModel).dvcUID;
            Device device = context.Devices.Find(devId);
            if (device != null)
            {
                device.dvcNextTransferNo++;
                device.dvcModifyTime = datetime;
                context.SaveChanges();
            }
            HttpContext.Current.Session["GroupedTransferList"] = grouplist;
        }

        private static List<IGrouping<string, TransferModel>> getGroupedTransferList(List<StockTransferLn> newstLnlist, MMDbContext context = null)
        {
            var st = newstLnlist.FirstOrDefault();

            var sendergrouplist = newstLnlist.GroupBy(x => new { x.stSender, x.itmCode, x.outQty }).ToList();
            var receivergrouplist = newstLnlist.GroupBy(x => new { x.stReceiver, x.itmCode, x.inQty }).ToList();
            var finallist = new List<TransferModel>();

            foreach (var sender in sendergrouplist)
            {
                foreach (var receiver in receivergrouplist)
                {
                    if (sender.Key.itmCode == receiver.Key.itmCode)
                    {
                        string itemnamedesc = string.Empty;
                        int transferQty = sender.Sum(x => (int)x.outQty);
                        if (context != null)
                        {
                            itemnamedesc = context.GetItemNameDescByCode(comInfo.AccountProfileId, st.itmCode).FirstOrDefault();
                        }
                        finallist.Add(
                            new TransferModel
                            {
                                stDate = st.stDate,
                                stCode = st.stCode,
                                stSender = sender.Key.stSender,
                                stReceiver = receiver.Key.stReceiver,
                                itmCode = sender.Key.itmCode,
                                itmNameDesc = itemnamedesc,
                                ivIdList = st.ivIdList,
                                poIvId = st.poIvId,
                                outQty = transferQty,
                                inQty = transferQty,
                                CreateTime = st.CreateTime,
                                ModifyTime = st.ModifyTime,
                            }
                            );
                    }
                }
            }
            return finallist.GroupBy(x => string.Concat(x.stSender, ":", x.stReceiver)).ToList();
        }

        private static List<TransferModel> getTransferList(List<IGrouping<string, TransferModel>> grouplist, bool saveList = false, MMDbContext context = null)
        {
            var transferlist = new List<TransferModel>();
            List<StockTransfer> stocklist = new List<StockTransfer>();

            foreach (var g in grouplist)
            {
                var key = g.Key.Split(':');
                var sender = key[0];
                var receiver = key[1];
                if (!string.IsNullOrEmpty(sender) && !string.IsNullOrEmpty(receiver))
                {
                    if (saveList)
                    {
                        foreach (var v in g)
                        {
                            if (v.stSender == sender && v.stReceiver == receiver && !string.IsNullOrEmpty(v.stSender) && !string.IsNullOrEmpty(v.stReceiver))
                            {
                                stocklist.Add(
                                    new StockTransfer
                                    {
                                        stCode = v.stCode,
                                        stSender = v.stSender,
                                        stReceiver = v.stReceiver,
                                        itmCode = v.itmCode,
                                        itmNameDesc = v.itmNameDesc,
                                        inQty = v.inQty,
                                        outQty = v.outQty,
                                        stCounted = 0,
                                        stVariance = 0,
                                        stSignedUp_Receiver = false,
                                        stSignedUp_Sender = false,
                                        stShop = v.stShop,
                                        stDate = v.stDate,
                                        ivIdList = v.ivIdList,
                                        poIvId = v.poIvId,
                                        stChecked = false,
                                        CreateTime = v.CreateTime,
                                        ModifyTime = v.ModifyTime,                                      
                                        AccountProfileId = comInfo.AccountProfileId,
                                    }
                                    );
                            }
                        }
                    }
                    else
                    {
                        foreach (var v in g)
                        {
                            if (v.stSender == sender && v.stReceiver == receiver && !string.IsNullOrEmpty(v.stSender) && !string.IsNullOrEmpty(v.stReceiver))
                            {
                                transferlist.Add(
                                    new TransferModel
                                    {
                                        stDate = v.stDate,
                                        stCode = v.stCode,
                                        stSender = v.stSender,
                                        stReceiver = v.stReceiver,
                                        itmCode = v.itmCode,
                                        itmNameDesc = v.itmNameDesc,
                                        inQty = v.inQty,
                                        outQty = v.outQty,
                                        stShop = v.stShop,
                                        CreateTime = v.CreateTime,
                                        ModifyTime = v.ModifyTime
                                    }
                                    );
                            }
                        }
                    }

                }
            }

            if (saveList)
            {
                context.StockTransfers.AddRange(stocklist);
                context.SaveChanges();
            }
            return transferlist;
        }

        public List<IGrouping<string, TransferModel>> GetGroupedTransferList(int? start, int? end)
        {
            using var context = new MMDbContext();
            var stlist = context.GetStockTransferListByIdRange3(start, end).ToList();
            var newstlist = new List<StockTransferLn>();
            var _st = stlist.FirstOrDefault();

            foreach (var stock in stlist)
            {
                if (stock.inQty > 0 || stock.outQty > 0)
                {
                    StockTransferLn st = new StockTransferLn
                    {
                        stDate = _st.stDate,
                        stCode = _st.stCode,
                        stSender = stock.stSender,
                        stReceiver = stock.stReceiver,
                        itmCode = stock.itmCode,
                        itmNameDesc = stock.itmNameDesc,
                        inQty = stock.inQty,
                        outQty = stock.outQty,
                        stShop = stock.stShop,
                        ivIdList = stock.ivIdList,
                        poIvId = stock.poIvId,
                        CreateTime = stock.CreateTime,
                        ModifyTime = stock.ModifyTime
                    };
                    newstlist.Add(st);
                }
            }

            DeviceModel device = HttpContext.Current.Session["Device"] as DeviceModel;
            TransferNumber = string.Concat(device.dvcTransferCode, "-", _st.stCode);
            TransferDateDisplay = CommonHelper.FormatDate(newstlist.FirstOrDefault().stDate, true);
            return getGroupedTransferList(newstlist);
        }

        public List<IGrouping<string, TransferModel>> GetGroupedTransferListByCode(string code)
        {
            using var context = new MMDbContext();
            var stlist = context.GetStockTransferListByCode5(ComInfo.AccountProfileId, code).ToList();
            var newstlist = new List<StockTransferLn>();
            var _st = stlist.FirstOrDefault();

            foreach (var stock in stlist)
            {
                if (stock.inQty > 0 || stock.outQty > 0)
                {
                    StockTransferLn st = new StockTransferLn
                    {
                        stDate = _st.stDate,
                        stCode = _st.stCode,
                        stSender = stock.stSender,
                        stReceiver = stock.stReceiver,
                        itmCode = stock.itmCode,
                        itmNameDesc = stock.ItemNameDesc,
                        inQty = stock.inQty,
                        outQty = stock.outQty,
                        stShop = stock.stShop,
                        ivIdList = stock.ivIdList,
                        poIvId = stock.poIvId,
                        CreateTime = stock.CreateTime,
                        ModifyTime = stock.ModifyTime
                    };
                    newstlist.Add(st);
                }
            }

            DeviceModel device = HttpContext.Current.Session["Device"] as DeviceModel;
            TransferNumber = string.Concat(device.dvcTransferCode, "-", _st.stCode);
            TransferDateDisplay = CommonHelper.FormatDate(newstlist.FirstOrDefault().stDate, true);
            return getGroupedTransferList(newstlist);
        }

        public void PreparePrint(int? start, int? end)
        {
            if (start != null && end != null)
            {
                GroupedTransferList = GetGroupedTransferList(start, end);
            }
            else
            {
                GroupedTransferList = HttpContext.Current.Session["GroupedTransferList"] as List<IGrouping<string, TransferModel>>;
                DeviceModel device = HttpContext.Current.Session["Device"] as DeviceModel;
                var group = GroupedTransferList.FirstOrDefault();
                var st = group.FirstOrDefault();
                TransferNumber = string.Concat(device.dvcTransferCode, "-", st.stCode);
                TransferDateDisplay = CommonHelper.FormatDate(st.stDate, true);
            }
        }

        public void PreparePrint(string code)
        {
            GroupedTransferList = GetGroupedTransferListByCode(code);
            DeviceModel device = HttpContext.Current.Session["Device"] as DeviceModel;
            var group = GroupedTransferList.FirstOrDefault();
            var st = group.FirstOrDefault();
            TransferNumber = string.Concat(device.dvcTransferCode, "-", st.stCode);
            TransferDateDisplay = CommonHelper.FormatDate(st.stDate, true);
        }

        public void GetTransferList(string strfrmdate = "", string strtodate = "", int PageNo = 1, int Size_Of_Page = 10, int SortCol = 0, string SortOrder = "desc", string Keyword = null)
        {
            SearchDates = new SearchDates();
            CommonHelper.GetSearchDates(strfrmdate, strtodate, ref SearchDates);
            DateFromTxt = SearchDates.DateFromTxt;
            DateToTxt = SearchDates.DateToTxt;

            int No_Of_Page = PageNo;

            using var context = new MMDbContext();

            int startIndex = (PageNo - 1) * Size_Of_Page;

            if (string.IsNullOrEmpty(Keyword))
                Keyword = null;
            var stlist = context.GetStockTransferGroupList5(SearchDates.frmdate, SearchDates.todate, ComInfo.AccountProfileId, startIndex, Size_Of_Page, Keyword).ToList();

            var transferlist = new List<TransferModel>();
            if (stlist.Count > 0)
            {
                foreach (var stock in stlist)
                {
                    var listinfo = context.GetStockTransferListByCode5(ComInfo.AccountProfileId, stock.stCode).ToList();

                    TransferModel st = new TransferModel
                    {
                        stCode = stock.stCode,
                        outQtySum = stock.outQtySum,
                        SenderList = string.Join(",", listinfo.Select(x => x.stSender).Distinct().ToList()),
                        stReceiver = stock.stReceiver,
                        ItemCodeList = string.Join(",", listinfo.Select(x => x.itmCode).Distinct().ToList()),
                        ItemNameDescList = string.Join(",", listinfo.Select(x => x.ItemNameDesc).Distinct().ToList()),
                        VarianceSum = listinfo.Sum(x => x.stVariance),
                        Checked = listinfo.FirstOrDefault().stChecked,
                        stDate = stock.stDate,                      
                        CreateTime = stock.CreateTime,
                        ModifyTime = stock.ModifyTime
                    };
                    transferlist.Add(st);
                }

                transferlist = DoSorting4List(SortCol, SortOrder, transferlist);

                this.SortCol = SortCol;
                CurrentSortOrder = SortOrder;
                this.SortOrder = SortOrder == "desc" ? "asc" : "desc";
                TransferPagingList = transferlist.ToPagedList(No_Of_Page, Size_Of_Page);
            }
        }

        private static List<TransferModel> DoSorting4List(int SortCol, string SortOrder, List<TransferModel> transferlist)
        {
            if (SortCol == 0)
            {
                transferlist = SortOrder == "asc" ? transferlist.OrderBy(c => c.stCode).ToList() : transferlist.OrderByDescending(c => c.stCode).ToList();
            }
            else if (SortCol == 1)
            {
                transferlist = SortOrder == "asc" ? transferlist.OrderBy(c => c.outQtySum).ToList() : transferlist.OrderByDescending(c => c.outQtySum).ToList();
            }
            else if (SortCol == 2)
            {
                transferlist = SortOrder == "asc" ? transferlist.OrderBy(c => c.stDate).ToList() : transferlist.OrderByDescending(c => c.stDate).ToList();
            }

            return transferlist;
        }      

        public static void Edit(TransferModel v)
        {
            using var context = new MMDbContext();
            var st = context.StockTransfers.Find(v.Id);
            st.stSignedUp_Sender = v.stSignedUp_Sender;
            st.stCounted = v.stCounted;
            st.stVariance = v.stVariance;
            st.stSignedUp_Receiver = v.stSignedUp_Receiver;
            st.stRemark = v.stRemark;
            st.ivIdList = v.ivIdList;
            st.poIvId = v.poIvId;
            st.stChecked = true;
            st.ModifyTime = DateTime.Now;

            var stock = context.MyobLocStocks.FirstOrDefault(x => x.lstStockLoc == st.stReceiver && x.lstItemCode == st.itmCode);
            stock.lstQuantityAvailable += v.stVariance;
            stock.lstModifyTime = DateTime.Now;

            context.SaveChanges();
        }

        public void ProcessTransfer(List<TransferLnModel> TransferList)
        {
            using var context = new MMDbContext();
            DateTime datetime = DateTime.Now;
            SessUser user = HttpContext.Current.Session["User"] as SessUser;

            var newstlist = new List<StockTransferLn>();
            foreach (var item in TransferList)
            {
                var stock = context.MyobLocStocks.FirstOrDefault(x => x.AccountProfileId == apId && x.lstStockLoc == item.stSender && x.lstItemCode == item.itmCode);
                if (stock != null)
                {
                    stock.lstQuantityAvailable -= item.outQty;
                    stock.lstModifyTime = datetime;
                    stock.lstModifyBy = user.UserName;
                    //context.SaveChanges();
                }
                stock = context.MyobLocStocks.FirstOrDefault(x => x.AccountProfileId == apId && x.lstStockLoc == item.stReceiver && x.lstItemCode == item.itmCode);
                if (stock != null)
                {
                    stock.lstQuantityAvailable += item.inQty;
                    stock.lstModifyTime = datetime;
                    stock.lstModifyBy = user.UserName;
                    //context.SaveChanges();
                }

                if (item.inQty > 0 || item.outQty > 0)
                {                    
                    newstlist.Add(new StockTransferLn
                    {
                        stCode = item.stCode,
                        stSender = item.stSender,
                        stReceiver = item.stReceiver,
                        itmCode = item.itmCode,
                        inQty = item.inQty,
                        outQty = item.outQty,
                        stShop = item.stShop,
                        stDate = datetime.Date,
                        ivIdList = item.ivIdList,
                        poIvId = item.poIvId,
                        AccountProfileId = ComInfo.AccountProfileId,
                        CreateTime = datetime,
                        ModifyTime = datetime
                    });
                }
            }

            context.StockTransferLns.AddRange(newstlist);
            context.SaveChanges();

            var grouplist = getGroupedTransferList(newstlist, context);

            getTransferList(grouplist, true, context);
            var devId = (HttpContext.Current.Session["Device"] as DeviceModel).dvcUID;
            Device device = context.Devices.Find(devId);
            if (device != null)
            {
                device.dvcNextTransferNo++;
                device.dvcModifyTime = datetime;
                context.SaveChanges();
            }
            HttpContext.Current.Session["GroupedTransferList"] = grouplist;
        }
    }

    public class Transferer
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
    }
}
