using MMDAL;
using MMLib.Models.MYOB;
using System;
using System.Collections.Generic;

namespace MMLib.Helpers
{
    public static class AccountHelper
    {
        public static IEnumerable<Account> ConvertModel(List<AccountModel> selectedAccounts, int apId)
        {
            List<Account> accounts = new List<Account>();
            foreach (var account in selectedAccounts)
            {
                Account ac = new Account();
                ac.AccountProfileId = apId;
                ac.AccountName = account.AccountName;
                ac.AccountNumber = account.AccountNumber;
                ac.AccountID = account.AccountID;
                ac.AccountClassificationID = account.AccountClassificationID;
                ac.AccountTypeID = account.AccountTypeID;
                ac.AccountLevel = account.AccountLevel;
                ac.CreateTime = DateTime.Now;
                accounts.Add(ac);
            }
            return accounts;
        }
    }

}
