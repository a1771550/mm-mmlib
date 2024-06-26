using MMCommonLib.BaseModels;
using MMDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using MMLib.Helpers;
using MMLib.Models.MYOB;
using System.Configuration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Web;

namespace MMLib.Models
{
    public class EmailEditModel:BaseModel
    {
        public void Edit(EmailModel model)
        {
            using (var context = new MMDbContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {  
                        EmailSetting EmailSetting = context.EmailSettings.FirstOrDefault(x => x.AccountProfileId == ComInfo.AccountProfileId);
                        DateTime dateTime = DateTime.Now;
                        if (EmailSetting != null)
                        {
                            if (model.emOffice365)
                            {
                                EmailSetting.emEmail = model.emSMTP_UserName;
                            }
                            else
                            {
                                EmailSetting.emEmail = model.emEmail;
                            }
                            EmailSetting.emDisplayName = model.emDisplayName;
                            EmailSetting.emSMTP_Auth = model.emSMTP_Auth;
                            EmailSetting.emSMTP_Server = model.emSMTP_Server;
                            EmailSetting.emSMTP_Pass = model.emSMTP_Pass;
                            EmailSetting.emSMTP_EnableSSL = model.emSMTP_EnableSSL;
                            EmailSetting.emSMTP_Port = model.emSMTP_Port;
                            EmailSetting.emSMTP_UserName = model.emSMTP_UserName;

                            EmailSetting.emEmailsPerSecond = model.emEmailsPerSecond;
                            EmailSetting.emMaxEmailsFailed = model.emMaxEmailsFailed;
                            EmailSetting.emEmailTrackingURL = model.emEmailTrackingURL;
                            EmailSetting.emTestEmail = model.emTestEmail;

                            EmailSetting.AccountProfileId = model.AccountProfileId;
                            EmailSetting.ModifyTime = dateTime;
                            EmailSetting.emOffice365 = model.emOffice365;
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

        public EmailModel Get()
        {
            EmailModel model = new EmailModel();
            using (var context = new MMDbContext())
            {
                var _apId = model.AccountProfileId;
                EmailSetting EmailSetting = context.EmailSettings.FirstOrDefault(x => x.AccountProfileId == _apId);
                if (EmailSetting != null)
                {
                    model.Id = EmailSetting.Id;
                    model.emDisplayName = EmailSetting.emDisplayName;
                    model.emEmail = EmailSetting.emEmail;
                    model.emSMTP_Auth = EmailSetting.emSMTP_Auth;
                    model.emSMTP_Server = EmailSetting.emSMTP_Server;
                    model.emSMTP_Pass = EmailSetting.emSMTP_Pass;
                    model.emSMTP_EnableSSL = EmailSetting.emSMTP_EnableSSL;
                    model.emSMTP_Port = EmailSetting.emSMTP_Port;
                    model.emSMTP_UserName = EmailSetting.emSMTP_UserName;
                    model.emEmailsPerSecond = EmailSetting.emEmailsPerSecond;
                    model.emMaxEmailsFailed = EmailSetting.emMaxEmailsFailed;
                    model.emEmailTrackingURL = EmailSetting.emEmailTrackingURL;
                    model.emTestEmail = EmailSetting.emTestEmail;
                    model.AccountProfileId = EmailSetting.AccountProfileId;
                    model.CreateTime = EmailSetting.CreateTime;
                    model.ModifyTime = EmailSetting.ModifyTime;
                    model.emOffice365 = EmailSetting.emOffice365;
                }
                return model;
            }
        }

    }
}
