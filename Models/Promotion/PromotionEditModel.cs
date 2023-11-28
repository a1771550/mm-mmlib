using System;
using System.Collections.Generic;
using System.Linq;
using MMCommonLib.BaseModels;
using System.Configuration;
using Dapper;
using PagedList;
using CommonLib.Helpers;
using Microsoft.Data.SqlClient;

namespace MMLib.Models.Item
{
    public class PromotionEditModel : PagingBaseModel
    {    
        public PromotionEditModel() { }

        public PromotionEditModel(int Id) : this()
        {
            Get(Id);
        }

        private void Get(int Id)
        {
            if (Id > 0)
            {
                using var connection = new SqlConnection(ConnectionString);
                connection.Open();
                Promotion = connection.QueryFirstOrDefault<PromotionModel>(@"EXEC dbo.GetPromotion @Id=@Id", new { Id });
            }
            else
            {
                Promotion = new PromotionModel();
            }
        }

        public PromotionEditModel(int sortCol, string sortOrder, string keyword, int? pageNo) : this()
        {
            SortCol = sortCol;
            CurrentSortOrder = sortOrder;
            SortOrder = sortOrder == "desc" ? "asc" : "desc";
            Keyword = keyword ?? null;
            PageNo = pageNo;
        }

        public List<PromotionModel> PromotionList { get; set; }
        public IPagedList<PromotionModel> PagingPromotionList { get; set; }
        public PromotionModel Promotion { get; set; }

        public void Delete(long Id)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            connection.Execute(@"EXEC dbo.DeletePromotion @Id=@Id", new { Id });
        }

        public void Add(PromotionModel model)
        {
            model.proDateFrm = CommonHelper.GetDateFrmString4SQL(model.JsDateFrm, DateTimeFormat.YYYYMMDD);
            model.proDateTo = CommonHelper.GetDateFrmString4SQL(model.JsDateTo, DateTimeFormat.YYYYMMDD);
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();            
            connection.Execute(@"EXEC dbo.AddPromotion @proName=@proName,@proDesc=@proDesc,@proNameTC=@proNameTC,@proDescTC=@proDescTC,@proNameSC=@proNameSC,@proDescSC=@proDescSC,@proDateFrm=@proDateFrm,@proDateTo=@proDateto,@proDiscPc=@proDiscPc,@pro4Period=@pro4Period,@proQty=@proQty,@proPrice=@proPrice", new { model.proName,model.proDesc, model.proNameTC, model.proDescTC, model.proNameSC, model.proDescSC, model.proDateFrm,model.proDateTo,model.proDiscPc,model.pro4Period,model.proQty,model.proPrice });
        }

        public void Edit(PromotionModel model)
        {
            model.proDateFrm = CommonHelper.GetDateFrmString4SQL(model.JsDateFrm, DateTimeFormat.YYYYMMDD);
            model.proDateTo = CommonHelper.GetDateFrmString4SQL(model.JsDateTo, DateTimeFormat.YYYYMMDD);
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            connection.Execute(@"EXEC dbo.EditPromotion @Id=@Id,@proName=@proName,@proDesc=@proDesc,@proNameTC=@proNameTC,@proDescTC=@proDescTC,@proNameSC=@proNameSC,@proDescSC=@proDescSC,@proDateFrm=@proDateFrm,@proDateTo=@proDateto,@proDiscPc=@proDiscPc,@pro4Period=@pro4Period,@proQty=@proQty,@proPrice=@proPrice", new {model.Id, model.proName, model.proDesc, model.proNameTC, model.proDescTC, model.proNameSC, model.proDescSC, model.proDateFrm, model.proDateTo, model.proDiscPc, model.pro4Period, model.proQty, model.proPrice });
        }

        public void GetList()
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            PromotionList = Helpers.ModelHelper.GetPromotionList(connection,SortCol,CurrentSortOrder, Keyword);
            PagingPromotionList = PromotionList.ToPagedList((int)PageNo, PageSize);
        }
    }
}
