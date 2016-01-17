using System;
using System.Net;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace ProxyScraper
{
    class ScraperLogic
    {
        static ConcurrentBag<string> directory = new ConcurrentBag<string>();
        static ConcurrentBag<string> proxyPages = new ConcurrentBag<string>();
        static ConcurrentBag<string> proxies = new ConcurrentBag<string>();
        static CancellationTokenSource cts = new CancellationTokenSource();

        internal static void buildScrapeList()
        {
                for (var i = 1; i < 30; i++)
                {
                    if (i < 10)
                        directory.Add("http://www.samair.ru/proxy/time-0" + i + ".htm");
                    else
                        directory.Add("http://www.samair.ru/proxy/time-" + i + ".htm");
                }

        }
        internal static ConcurrentBag<string> scrapeProxies()
        {
            ConcurrentBag<string> returnList = new ConcurrentBag<string>();


                Parallel.ForEach(proxyPages, item =>
                    {
                        using (WebClient wb = new WebClient())
                        {
                            string url = item;
                            string html = wb.DownloadString(url);
                            var temp = ScraperLogic.proxyGetter(html);
                            foreach (var proxy in temp)
                            {
                                proxies.Add(proxy);
                                returnList.Add(proxy);
                            }
                        }
                    });
            
            return returnList;
        }

        internal static void scrapeProxyPages()
        {

                 Parallel.ForEach(directory, item =>
                 {
                     using (WebClient wb = new WebClient())
                     {
                         string url = item;
                         string html = wb.DownloadString(url);
                         var temp = ScraperLogic.NumberExtractor2(html);
                         Parallel.ForEach(temp, link => { proxyPages.Add(link); });
                     }
                 });

        }

        public static List<string> NumberExtractor2(string file)
        {
            List<string> list = new List<string>();

            MatchCollection m1 = Regex.Matches(file, @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);

            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();

                Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;
                }
                string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                RegexOptions.Singleline);
                i.Text = t;
                if (i.Text == "You can do it there")
                {
                    list.Add("http://www.samair.ru" + i.Href);
                }
            }
            return list;
        }

        public struct LinkItem
        {
            public string Href;
            public string Text;

            public override string ToString()
            {
                return Href + "\n\t" + Text;
            }
        }

        internal static List<string> proxyGetter(string html)
        {
            List<string> list = new List<string>();
            MatchCollection proxy = Regex.Matches(html, @"[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\:[0-9]{1,5}");
            foreach(var item in proxy)
            {
                list.Add(item.ToString());
            }
            return list;
        }
        
        internal static bool isAnonProxy(string ip)
        {
            string ipReturned;
            using (WebClient wb = new WebClient())
            {
                WebProxy wp = new WebProxy(ip);
                wb.Proxy = wp;
                try
                {
                    ipReturned = wb.DownloadString("http://web.engr.oregonstate.edu/~boseakc/ipcheck.php");
                    Match m = Regex.Match(ip, @".+?(?=:)");
                    if (m.ToString() != ipReturned)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch { }

                return false;

            }
        }

    }
}
