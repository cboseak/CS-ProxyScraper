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
        static ConcurrentQueue<string> directory = new ConcurrentQueue<string>();
        static ConcurrentQueue<string> proxyPages = new ConcurrentQueue<string>();
        static ConcurrentBag<string> proxies = new ConcurrentBag<string>();
        static CancellationTokenSource cts = new CancellationTokenSource();

        internal static void buildScrapeList()
        {
            for(var i = 1; i < 31; i++ )
            {
                if (i < 10)
                    directory.Enqueue("http://www.samair.ru/proxy/time-0" + i + ".htm");
                else
                    directory.Enqueue("http://www.samair.ru/proxy/time-" + i + ".htm");
            }
        }
        internal static ConcurrentQueue<string> scrapeProxies()
        {
            ConcurrentQueue<string> returnList = new ConcurrentQueue<string>();
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
                        returnList.Enqueue(proxy);
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
                    Parallel.ForEach(temp, link => { proxyPages.Enqueue(link); });
                }
            });
        }

        public static List<string> NumberExtractor2(string file)
        {
            List<string> list = new List<string>();

            // 1.
            // Find all matches in file.
            MatchCollection m1 = Regex.Matches(file, @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();

                // 3.
                // Get href attribute.
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

    }
}
