using MMDAL;
using MMLib.Models.MYOB;
using System;
using System.Collections.Generic;

namespace MMLib.Helpers
{
    public static class AccountHelper
    {
        public static List<Account> ConvertModel(List<AccountModel> selectedAccounts, int apId)
        {
            List<Account> accounts = new List<Account>();
            try
            {                
                DateTime dateTime = DateTime.Now;
                foreach (var account in selectedAccounts)
                {
                    accounts.Add(new Account
                    {
                        AccountProfileId = apId,
                        AccountName = account.AccountName ?? "",
                        AccountNumber = account.AccountNumber ?? "",
                        AccountID = account.AccountID,
                        AccountClassificationID = account.AccountClassificationID ?? "",
                        AccountTypeID = account.AccountTypeID ?? "",
                        AccountLevel = account.AccountLevel,
                        CreateTime = dateTime
                    });
                }
            }
            catch (Exception)
            {

            }
            
            return accounts;
        }
    }

}
