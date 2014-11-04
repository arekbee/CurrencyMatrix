using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyMatrix.Runner
{
    class Program
    {
        //http://currencies.apps.grandtrunk.net/
        static void Main(string[] args)
        {

            var test = new UriBuilder { Fragment = "asd" };

            string baseUrl = @"http://currencies.apps.grandtrunk.net";
            string url_getTrate = @"/getrate/{0}/{1}/{2}"; ///<date>/<fromcode>[/<tocode>]
            const string DateTimeApiFormat = "yyyy-MM-dd";
            var client = new WebClient();
            client.Proxy.Credentials = CredentialCache.DefaultCredentials;

            var iterateDate = (new DateTime(2013, 04, 18)).IterateDate(new DateTime(2013, 04, 21));

            var currencyList = new[]
                {
                    Currence.PLN, Currence.USD, Currence.CAD, Currence.EUR, Currence.CHF, Currence.GBP, Currence.JPY, Currence.CNY, Currence.CZK, Currence.HUF
                     //Currence.BYR, Currence.GBP
                };
            var exchanges = Extensions.CombineExchanges(currencyList);
            IList<Exception> errorsException = new List<Exception>();

            Dictionary<DateTime, string> errorOnDate = new Dictionary<DateTime, string>();
            foreach (var dateTime in iterateDate)
            {
                Console.WriteLine("\nDate: " + dateTime.ToShortDateString());
                try
                {
                    CurrencyMatrix matrix = new CurrencyMatrix(currencyList.Select(x => x.ToString()).ToList());
                    string dateTimeString = dateTime.ToString(DateTimeApiFormat);

                    foreach (var exchage in exchanges)
                    {
                        if (exchage.FromCurrency == exchage.ToCurrency)
                        {
                            continue;
                        }

                        try
                        {
                            var format = string.Format(url_getTrate, dateTimeString, exchage.FromCurrency, exchage.ToCurrency);
                            string value = client.DownloadString(baseUrl + format);
                            var retExchange = new CurrencyExchangeRate(exchage) { RateValue = value.ToDecimal() };
                            matrix.Add(retExchange);
                        }
                        catch (Exception ex)
                        {
                            errorsException.Add(ex);
                        }
                    }
                    matrix.Normalize();

                    matrix.Print();

                    using (TextWriter textWriter = new StreamWriter(dateTimeString + ".csv", false))
                    {
                        textWriter.Write(matrix.PrintCsv());
                        textWriter.Close();
                    }
                }
                catch (Exception ex)
                {
                    errorOnDate.Add(dateTime, ex.Message);
                }

            }

            using (TextWriter vTextWriter = new StreamWriter("Erors.txt", false))
            {
                foreach (var errors in errorOnDate)
                {
                    vTextWriter.WriteLine(errors.Key + " e: " + errors.Value);
                }
                vTextWriter.Close();
            }
        }
    }

    public enum Currence
    {
        AED, AFN, ALL, AMD, ANG, AOA, ARS, ATS, AUD, AWG, AZN, BAM, BBD, BDT, BEF, BGN, BHD, BIF, BMD, BND, BOB, BRL, BSD, BTC, BTN, BWP, BYR, BZD, CAD, CDF, CHF, CLF, CLP, CNH, CNY, COP, CRC, CUP, CVE, CYP, CZK, DEM, DJF, DKK, DOP, DZD, EEK, EGP, ERN, ESP, ETB, EUR, FIM, FJD, FKP, FRF, GBP, GEL, GHS, GIP, GMD, GNF, GRD, GTQ, GYD, HKD, HNL, HRK, HTG, HUF, IDR, IEP, ILS, INR, IQD, IRR, ISK, ITL, JEP, JMD, JOD, JPY, KES, KGS, KHR, KMF, KPW, KRW, KWD, KYD, KZT, LAK, LBP, LKR, LRD, LSL, LTL, LUF, LVL, LYD, MAD, MCF, MDL, MGA, MKD, MMK, MNT, MOP, MRO, MTL, MUR, MVR, MWK, MXN, MYR, MZN, NAD, NGN, NIO, NLG, NOK, NPR, NZD, OMR, PAB, PEN, PGK, PHP, PKR, PLN, PTE, PYG, QAR, RON, RSD, RUB, RWF, SAR, SBD, SCR, SDG, SEK, SGD, SHP, SIT, SKK, SLL, SML, SOS, SRD, STD, SVC, SYP, SZL, THB, TJS, TMT, TND, TOP, TRY, TTD, TWD, TZS, UAH, UGX, USD, UYU, UZS, VAL, VEB, VEF, VND, VUV, WST, XAF, XAG, XAU, XCD, XCP, XDR, XOF, XPD, XPF, XPT, YER, ZAR, ZMK, ZMW, ZWL
    }



    public static class Extensions
    {
        public static IEnumerable<DateTime> IterateDate(this DateTime fromDateTime, DateTime toDateTime, int steps = 1)
        {
            for (DateTime startDate = fromDateTime;; startDate = startDate.AddDays(steps))
            {
                if (steps >= 0 && startDate > toDateTime)
                    break;

                if (steps <= 0 && startDate < toDateTime)
                    break;

                yield return startDate;
            }
        }

        public static IDictionary<T, int> NumerateElements<T>(this IList<T> enumerable)
        {
            var dic = new Dictionary<T, int>();
            int counter = 0;
            foreach (var item in enumerable)
            {
                dic.Add(item, counter);
                counter++;
            }

            return dic;
        }

        public static decimal ToDecimal(this string strDeciam)
        {
            decimal dec;
            decimal.TryParse(strDeciam.Replace('.',','), out dec);
            return dec;
        }

        public static IEnumerable<CurrencyExchangeRate> CombineExchanges(IEnumerable<Currence> currencyList)
        {
            return CombineExchanges(currencyList.Select(x => x.ToString()));
        }

        public static IEnumerable<CurrencyExchangeRate> CombineExchanges(IEnumerable<string> currencyList)
        {
            var list = currencyList.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    string fromCurrency = list[j];
                    string toCurrency = list[i];
                    yield return new CurrencyExchangeRate { FromCurrency = fromCurrency, ToCurrency = toCurrency };
                }

            }
        }
    }

    public struct CurrencyExchangeRate
    {
        public string FromCurrency { get; set; }

        public string ToCurrency { get; set; }

        public decimal RateValue { get; set; }

        public CurrencyExchangeRate(CurrencyExchangeRate oldRate)
            : this()
        {
            FromCurrency = oldRate.FromCurrency;
            ToCurrency = oldRate.ToCurrency;
            RateValue = oldRate.RateValue;
        }
        public override string ToString()
        {
            return FromCurrency + "|" + ToCurrency;
        }
    }

    public class CurrencyMatrix
    {
        public decimal[,] rawMatrix { get; private set; }

        public int dimension
        {
            get
            {
                return CurrencyList.Count;
            }
        }

        private IDictionary<string, int> currencyDic;

        public IList<string> CurrencyList;


        public CurrencyMatrix(IList<string> currencyList)
        {
            CurrencyList = currencyList;
            currencyDic = currencyList.NumerateElements();
            rawMatrix = new decimal[dimension, dimension];
            SetOneOnDiagonal();
        }


        public void SetOneOnDiagonal()
        {
            for (int i = 0; i < dimension; i++)
            {
                rawMatrix[i, i] = 1;
            }
        }

        public void Add(CurrencyExchangeRate rate)
        {
            int rowNum = currencyDic[rate.FromCurrency];
            int columnNum = currencyDic[rate.ToCurrency];
            rawMatrix[rowNum, columnNum] = rate.RateValue;
        }


        public void Normalize()
        {
            for (int i = 0; i < dimension; i++)
            {
                for (int j = 0; j < dimension; j++)
                {
                    decimal valueFromTo = rawMatrix[i, j];
                    if (valueFromTo == 0)
                    {
                        decimal valueToFrom = rawMatrix[j, i];
                        if (valueToFrom == 0)
                        {
                            continue;
                        }

                        valueFromTo = 1 / valueToFrom;
                        rawMatrix[i, j] = valueFromTo;
                    }
                }
            }
        }

        public void Print()
        {
            Console.WriteLine(PrintCsv("\t"));
        }

        public string PrintCsv(string seperator = ",")
        {
            var sb = new StringBuilder();

            sb.Append(@"From\To" + seperator);
            for (int i = 0; i < dimension; i++)
            {
                sb.Append(CurrencyList[i] + seperator);
            }

            for (int i = 0; i < dimension; i++)
            {
                sb.AppendLine();
                sb.Append(CurrencyList[i] + seperator);
                for (int j = 0; j < dimension; j++)
                {
                    decimal value = rawMatrix[i, j];

                    string valueString = value.ToString(CultureInfo.InvariantCulture);
                    sb.Append(valueString + seperator);
                }
            }

            return sb.ToString();
        }
    }
}
