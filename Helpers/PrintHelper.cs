using CommonLib.Helpers;
using MMCommonLib.BaseModels;
using MMCommonLib.CommonHelpers;
using MMLib.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Web;
using System.Web.UI;
using System.Windows.Input;

namespace MMLib.Helpers
{
    public class PrintHelper
    {
        public static List<string> InstalledPrinters()
        {
            return (from PrintQueue printer in new LocalPrintServer().GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local,
                EnumeratedPrintQueueTypes.Connections }).ToList()
                    select printer.Name).ToList();
        }
        public static string GetPrintHtml(PrintModel model)
        {
            StringWriter stringWriter = new StringWriter();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                #region Header
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "header");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                writer.AddStyleAttribute("text-align", "center");
                writer.AddStyleAttribute("margin-bottom", "8px");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                if (model.UseLogo)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Id, "receiptlogo");
                    writer.AddAttribute(HtmlTextWriterAttribute.Src, model.LogoPath);
                    //writer.AddAttribute(HtmlTextWriterAttribute.Class, "py-3");
                    writer.RenderBeginTag(HtmlTextWriterTag.Img);
                    writer.RenderEndTag();
                }
                writer.AddStyleAttribute("font-weight", "700");
                writer.AddStyleAttribute("font-size", "1.5em");
                writer.RenderBeginTag(HtmlTextWriterTag.H2);
                writer.Write(model.Receipt.CompanyName);
                writer.RenderEndTag();

                if (model.Receipt.CompanyAddress != null)
                {
                    writer.AddStyleAttribute("font-size", ".8em");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.CompanyAddress);
                    writer.RenderEndTag();
                }

                if (model.Receipt.CompanyAddress1 != null)
                {
                    writer.AddStyleAttribute("font-size", ".8em");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.CompanyAddress1);
                    writer.RenderEndTag();
                }
                if (model.Receipt.CompanyPhone != null)
                {
                    writer.AddStyleAttribute("font-size", ".8em");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "mx-3 fa fa-phone");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.Write(string.Concat(":", model.Receipt.CompanyPhone));
                    writer.RenderEndTag();
                    if (model.Receipt.CompanyWebSite != null)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "mx-3 fa fa-globe");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.Write(string.Concat(":", model.Receipt.CompanyWebSite));
                        writer.RenderEndTag();
                    }
                    writer.RenderEndTag();
                }

                #region TIN
                if (model.IsPNG || model.TaxModel.TaxType == CommonLib.Models.TaxType.Inclusive)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "mt-5" : "row mx-1");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "font-weight-bold" : "text-left font-weight-bold");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.TIN);
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                }
                #endregion

                if (model.Receipt.HeaderTitle != null)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.HeaderTitle);
                    writer.RenderEndTag();
                }

                if (model.Receipt.HeaderMessage != null)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.HeaderMessage);
                    writer.RenderEndTag();
                }

                writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "1.2em");
                writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "center");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "py-1");
                writer.RenderBeginTag(HtmlTextWriterTag.H2);
                if (model.issales)
                {
                    writer.Write(model.ReceiptTitle);
                }
                else
                {
                    writer.Write(model.RefundTitle);
                }
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "" : "row mx-1");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "font-weight-bold" : "text-left font-weight-bold");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.Write(string.Format("{0}-{1}", model.Sales.rtsDvc, model.Sales.rtsCode));
                writer.RenderEndTag();
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "" : "row mx-1");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "font-weight-bold" : "text-left font-weight-bold");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.Write(model.Sales.DateTimeDisplay);
                writer.RenderEndTag();
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "" : "row mx-1");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "font-weight-bold" : "text-left font-weight-bold");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.Write(model.UserName);
                writer.RenderEndTag();
                writer.RenderEndTag();

                #region Div Table Header 
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "row mx-1 my-3");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                #region Table Header
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "table table-borderless");
                writer.AddStyleAttribute("font-size", "1.2em");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                #region Thead
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "font-weight-bold");
                writer.RenderBeginTag(HtmlTextWriterTag.Thead);

                #region Tr
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                if (!string.IsNullOrEmpty(model.itemfield))
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-left bordertopbottom");
                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    writer.Write(model.itemfield);
                    writer.RenderEndTag();
                }

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right bordertopbottom");
                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write(model.qtyheader);
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right bordertopbottom");
                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write(model.priceheader);
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right bordertopbottom");
                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write(model.discpcheader);
                writer.RenderEndTag();

                if (model.TaxModel.EnableTax)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right bordertopbottom");
                    writer.RenderBeginTag(HtmlTextWriterTag.Th);
                    writer.Write(model.taxpcheader);
                    writer.RenderEndTag();
                }

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right bordertopbottom");
                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.Write(model.amtheader);
                writer.RenderEndTag();

                writer.RenderEndTag();
                #endregion
                writer.RenderEndTag();
                #endregion
                writer.RenderEndTag();
                #endregion
                writer.RenderEndTag();
                #endregion

                writer.RenderEndTag();
                writer.RenderEndTag();
                #endregion


                #region Table
                writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "1.2em");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "table table-borderless");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);

                #region Tbody
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "table-borderless");
                writer.RenderBeginTag(HtmlTextWriterTag.Tbody);

                //int colspan = model.issales ? 4 : 3;
                int colspan = 4;
                if (model.issales)
                {
                    foreach (var item in model.SalesLnViews)
                    {
                        //var item = group.FirstOrDefault();
                        var disc = item.rtlLineDiscPc == 0 ? "-" : CommonHelper.FormatNumber((decimal)item.rtlLineDiscPc);
                        var taxpc = item.rtlTaxPc != null ? CommonHelper.FormatNumber((decimal)item.rtlTaxPc) : "-";

                        #region Tr
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, (colspan + 1).ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-left");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(model.CheckoutPortal == "kingdee" ? item.KItem.itmCode : item.Item.itmCode);


                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, (colspan + 1).ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-center");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);

                        #region wrapper div
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "row justify-content-center");
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        #region table tblitem
                        writer.AddAttribute("style", "line-height:.9;");
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "tblitem table-borderless");
                        writer.RenderBeginTag(HtmlTextWriterTag.Table);

                        #region itemdesc	
                        #region itemname/itemdesc
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(model.CheckoutPortal == "kingdee" ? item.KItem.NameDesc : item.Item.NameDesc);
                        writer.RenderEndTag();
                        writer.RenderEndTag();
                        #endregion
                        #endregion

                        #region tr item Bat	  
                        if (model.forPreorder)
                        {
                            if (model.IsProcessRemaining)
                            {
                                writeBat(model, writer, item);
                            }
                        }
                        else
                        {
                            writeBat(model, writer, item);
                        }


                        #endregion

                        #region tr item Sn	
                        if (model.forPreorder)
                        {
                            if (model.IsProcessRemaining)
                            {
                                writeSN(model, writer, item);
                            }
                        }
                        else
                        {
                            writeSN(model, writer, item);
                        }


                        #endregion

                        #region tr item Vt
                        if (model.forPreorder)
                        {
                            if (model.IsProcessRemaining)
                            {
                                writeVT(model, writer, item);
                            }
                        }
                        else
                        {
                            writeVT(model, writer, item);
                        }
                        
                        #endregion
                        writer.RenderEndTag();

                        #endregion
                        writer.RenderEndTag();
                        #endregion

                        writer.RenderEndTag();
                        writer.RenderEndTag();

                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.RenderEndTag();

                        #region qty
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(item.Qty);
                        writer.RenderEndTag();
                        #endregion

                        #region price
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        if (model.TaxModel.TaxType == CommonLib.Models.TaxType.Inclusive)
                        {
                            writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)item.rtlSellingPriceMinusInclTax));
                        }
                        else
                        {
                            writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)item.rtlSellingPrice));
                        }
                        writer.RenderEndTag();
                        #endregion

                        #region discpc
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(disc);
                        writer.RenderEndTag();
                        #endregion

                        #region taxpc
                        if (model.TaxModel.EnableTax)
                        {
                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            writer.Write(taxpc);
                            writer.RenderEndTag();
                        }
                        #endregion

                        #region amt
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)item.rtlSalesAmt));
                        //if (model.TaxModel.TaxType == CommonLib.Models.TaxType.Inclusive)
                        //{
                        //	writer.Write(CommonHelper.FormatMoney(model.Currency, item.LineSalesAmtMinusInclTax));
                        //}
                        //else
                        //{
                        //	writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)item.dLineSalesAmt));
                        //}
                        writer.RenderEndTag();
                        #endregion

                        writer.RenderEndTag();
                        #endregion
                    }
                }
                else
                {
                    foreach (var item in model.RefundLnViews)
                    {
                        var disc = item.rtlLineDiscPc == 0 ? "-" : CommonHelper.FormatNumber((decimal)item.rtlLineDiscPc);
                        var taxpc = item.rtlTaxPc != null ? CommonHelper.FormatNumber((decimal)item.rtlTaxPc) : "-";
                        #region Tr
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, (colspan + 1).ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-left");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(model.CheckoutPortal == "kingdee" ? item.KItem.itmCode : item.Item.itmCode);
                        #region itemdesc row						
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, (colspan + 1).ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-center");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(model.CheckoutPortal == "kingdee" ? item.KItem.itmDesc : item.Item.itmDesc);
                        writer.RenderEndTag();
                        writer.RenderEndTag();
                        #endregion
                        writer.RenderEndTag();

                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.RenderEndTag();

                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(item.Qty);
                        writer.RenderEndTag();

                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        if (model.TaxModel.TaxType == CommonLib.Models.TaxType.Inclusive)
                        {
                            writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)item.rtlSellingPriceMinusInclTax));
                        }
                        else
                        {
                            writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)item.rtlSellingPrice));
                        }
                        writer.RenderEndTag();

                        #region discpc
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(disc);
                        writer.RenderEndTag();
                        #endregion

                        #region taxpc
                        if (model.TaxModel.EnableTax)
                        {
                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            writer.Write(taxpc);
                            writer.RenderEndTag();
                        }
                        #endregion

                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)item.dLineSalesAmt));
                        writer.RenderEndTag();
                        writer.RenderEndTag();
                        #endregion
                    }
                }

                writer.RenderEndTag();
                #endregion

                writer.RenderEndTag();
                #endregion


                #region Footer
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "mx-1 my-4 py-4 px-2");
                writer.RenderBeginTag("footer");

                #region Payment Detail
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "row justify-content-center");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                #region footer table
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "table table-borderless");
                writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "1.2em");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                #region footer table tbody
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "font-weight-bold");
                writer.RenderBeginTag(HtmlTextWriterTag.Tbody);
                #region Tr:remark & subtotal
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                #region remark
                writer.AddAttribute(HtmlTextWriterAttribute.Colspan, "2");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-left bordertop");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-center");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.RenderBeginTag(HtmlTextWriterTag.H4);
                writer.Write(model.notesheader);
                writer.RenderEndTag();
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(model.Sales.rtsRmks);
                writer.RenderEndTag();
                writer.RenderEndTag();
                writer.RenderEndTag();
                #endregion
                #region subtotal
                int subcolspan = colspan - 2;
                writer.AddAttribute(HtmlTextWriterAttribute.Colspan, subcolspan.ToString());
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right bordertop");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(model.subtotalheader);
                writer.RenderEndTag();
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right bordertop");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                var subtotal = (decimal)(model.Sales.rtsLineTotal + model.Sales.rtsFinalDiscAmt);
                if (!model.issales) subtotal = (-1) * subtotal;
                //if(model.TaxModel.EnableTax && model.TaxModel.TaxType == CommonLib.Models.TaxType.Exclusive)
                //{
                //    subtotal = (decimal)model.Sales.rtsLineTotal - model.TaxModel.TaxAmt;
                //    if (!model.issales)
                //        subtotal = subtotal * -1;
                //}
                writer.Write(CommonHelper.FormatMoney(model.Currency, subtotal));
                writer.RenderEndTag();
                #endregion
                writer.RenderEndTag();
                #endregion

                #region Tr:DiscAmt
                if (model.TotalDiscAmt > 0)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(model.discountamtheader);
                    writer.RenderEndTag();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(CommonHelper.FormatMoney(model.Currency, model.TotalDiscAmt));
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                }
                #endregion

                #region Tr:TaxAmt
                if (model.TaxModel.TaxType == CommonLib.Models.TaxType.Inclusive)
                {
                    if (model.TaxModel.InclTaxAmt > 0)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(model.taxamtheader + " <span class='small'>(" + model.inctaxamtheader + ")</span>");
                        writer.RenderEndTag();
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(CommonHelper.FormatMoney(model.Currency, model.TaxModel.InclTaxAmt));
                        writer.RenderEndTag();
                        writer.RenderEndTag();
                    }
                }
                else
                {
                    if (model.TaxModel.TaxAmt > 0)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(model.taxamtheader);
                        writer.RenderEndTag();
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(CommonHelper.FormatMoney(model.Currency, model.TaxModel.TaxAmt));
                        writer.RenderEndTag();
                        writer.RenderEndTag();
                    }
                }

                #endregion

                #region Tr:Roundings
                if (model.Sales.Roundings != 0)
                {
                    #region Tr
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    #region Td
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(model.roundingsheader);
                    writer.RenderEndTag();
                    #endregion
                    #region Td
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)model.Sales.Roundings));
                    writer.RenderEndTag();
                    #endregion
                    writer.RenderEndTag();
                    #endregion
                }
                #endregion

                #region Tr:totalamt
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(model.totalamtheader);
                writer.RenderEndTag();
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                var totalamttxt = CommonHelper.FormatMoney(model.Currency, model.issales ? (decimal)model.Sales.rtsFinalTotal : -1 * (decimal)model.Sales.rtsFinalTotal);
                if (model.Sales.rtsExRate != 1)
                {
                    var totalamt = model.issales ? (decimal)(model.Sales.rtsFinalTotal / model.Sales.rtsExRate) : -1 * (decimal)(model.Sales.rtsFinalTotal / model.Sales.rtsExRate);
                    totalamttxt = CommonHelper.FormatMoney(model.Sales.rtsCurrency, totalamt);
                }
                writer.Write(totalamttxt);
                writer.RenderEndTag();
                writer.RenderEndTag();
                #endregion

                if (model.IsDeposit || model.forPreorder)
                {
                    #region Tr:deposit
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(model.depositheader);
                    writer.RenderEndTag();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(CommonHelper.FormatMoney(model.Currency, model.DepositAmt));
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                    #endregion

                    #region Tr:remain
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(model.remainheader);
                    writer.RenderEndTag();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(CommonHelper.FormatMoney(model.Currency, model.RemainAmt));
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                    #endregion
                }
                else if (model.IsProcessRemaining)
                {
                    #region Tr:depositpaid
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(model.depositpaidheader);
                    writer.RenderEndTag();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(CommonHelper.FormatMoney(model.Currency, model.DepositAmt));
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                    #endregion

                    #region Tr:remainpaid
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(model.remainpaidheader);
                    writer.RenderEndTag();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(CommonHelper.FormatMoney(model.Currency, model.RemainAmt));
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                    #endregion
                    HttpContext.Current.Session["RemainList"] = null;//reset remainlist session to null	
                }

                if (!model.Sales.rtsMonthBase)
                {
                    var dicpayamts = model.DicPayAmt;

                    foreach (var ditem in dicpayamts)
                    {
                        var paytype = "";
                        var payamttxt = "";

                        var paymenttype = model.Sales.DicPayTypes.FirstOrDefault(x => x.Key == ditem.Key);
                        paytype = paymenttype.Value;

                        payamttxt = CommonHelper.FormatMoney(model.Currency, dicpayamts[ditem.Key]);
                        #region Tr:paytype & payamt
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        #region Td
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(paytype);
                        writer.RenderEndTag();
                        #endregion
                        #region Td
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(payamttxt);
                        writer.RenderEndTag();
                        #endregion
                        writer.RenderEndTag();
                        #endregion
                    }

                }
                else
                {
                    #region Tr
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    #region Td
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(model.monthlypayheader);
                    writer.RenderEndTag();
                    #endregion
                    #region Td
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write("");
                    writer.RenderEndTag();
                    #endregion
                    writer.RenderEndTag();
                    #endregion
                }

                #region Tr:Change
                if (model.Sales.Change != 0)
                {
                    #region Tr
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    #region Td
                    writer.AddAttribute(HtmlTextWriterAttribute.Colspan, colspan.ToString());
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(model.changeheader);
                    writer.RenderEndTag();
                    #endregion
                    #region Td
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "text-right");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(CommonHelper.FormatMoney(model.Currency, (decimal)model.Sales.Change));
                    writer.RenderEndTag();
                    #endregion
                    writer.RenderEndTag();
                    #endregion
                }
                #endregion

                writer.RenderEndTag();
                #endregion
                writer.RenderEndTag();
                #endregion
                writer.RenderEndTag();
                #endregion

                #region Receipt Footer
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "row justify-content-center");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                #region footertitle1
                if (model.Receipt.FooterTitle1 != null)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.FooterTitle1);
                    writer.RenderEndTag();
                }
                #endregion
                #region footertitle2
                if (model.Receipt.FooterTitle2 != null)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.FooterTitle2);
                    writer.RenderEndTag();
                }

                #endregion
                #region footertitle3
                if (model.Receipt.FooterTitle3 != null)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.FooterTitle3);
                    writer.RenderEndTag();
                }

                #endregion
                #region footermsg
                if (model.Receipt.FooterMessage != null)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.FooterMessage);
                    writer.RenderEndTag();
                }

                #endregion
                writer.RenderEndTag();
                #endregion

                #region Disclaimer
                if (model.Receipt.Disclaimer != null)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, model.IsPNG ? "row justify-content-center my-4" : "row justify-content-start my-4");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "small");
                    writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, model.IsPNG ? "" : "justify");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.Write(model.Receipt.Disclaimer);
                    writer.RenderEndTag();
                    writer.RenderEndTag();
                }
                #endregion

                writer.RenderEndTag();
                #endregion
            }
            // Return the result.
            return stringWriter.ToString();
        }
       
       
    }
}