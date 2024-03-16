using MMDAL;
using MMLib.Models.MYOB;
using System;
using System.Collections.Generic;

namespace MMLib.Models.Currency
{
    public static class CurrencyHelper
    {
        public static IEnumerable<MyobCurrency> ConvertModel(List<MyobCurrencyModel> selectedCurrencies, int apId)
        {
            List<MyobCurrency> newcurrencies = new();
            foreach (var currency in selectedCurrencies)
            {
                newcurrencies.Add(new MyobCurrency
                {
                    CurrencyID = currency.CurrencyID,
                    CurrencyCode = currency.CurrencyCode,
                    CurrencyName = currency.CurrencyName,
                    ExchangeRate = currency.ExchangeRate,
                    CurrencySymbol = currency.CurrencySymbol,
                    DigitGroupingSymbol = currency.DigitGroupingSymbol,
                    SymbolPosition = currency.SymbolPosition,
                    DecimalPlaces = currency.DecimalPlaces,
                    NumberDigitsInGroup = currency.NumberDigitsInGroup,
                    DecimalPlaceSymbol = currency.DecimalPlaceSymbol,
                    NegativeFormat = currency.NegativeFormat,
                    UseLeadingZero = currency.UseLeadingZero,
                    AccountProfileId = apId,
                    CreateTime = DateTime.Now
                });
            }
            return newcurrencies;
        }
    }

}
