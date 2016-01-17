using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProxyScraper
{
    public partial class Form1 : Form
    {
        ConcurrentBag<string> proxyList = new ConcurrentBag<string>();
        SortedList<string, int> successfulAnonProxies = new SortedList<string, int>();
        SortedList<string, int> successfulTransparentProxies = new SortedList<string, int>();
        scrapeStatus _scrapeStatus = scrapeStatus.stopped;

        enum scrapeStatus
        {
            stopped = 0,
            scrapingProxies = 1,
            testingProxies = 2
        }
        public Form1()
        {
            InitializeComponent();
            progressBar1.Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _scrapeStatus = scrapeStatus.scrapingProxies;
            ScraperLogic.buildScrapeList();
            ScraperLogic.scrapeProxyPages();
            proxyList = new ConcurrentBag<string>(ScraperLogic.scrapeProxies().Distinct().ToList());
            textBox1.Lines = proxyList.ToArray();
            _scrapeStatus = scrapeStatus.stopped;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            testProxyList();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _scrapeStatus = scrapeStatus.scrapingProxies;
            ScraperLogic.buildScrapeList();
            ScraperLogic.scrapeProxyPages();
            proxyList = new ConcurrentBag<string>(ScraperLogic.scrapeProxies().Distinct().ToList());
            textBox1.Lines = proxyList.ToArray();
            _scrapeStatus = scrapeStatus.testingProxies;
            testProxyList();
            _scrapeStatus = scrapeStatus.stopped;
        }

        private async void testProxyList()
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(proxyList, proxy =>
                {
                    using (WebClient wb = new WebClient())
                    {
                        bool anon = false;
                        WebProxy wp = new WebProxy(proxy);
                        wb.Proxy = wp;
                        Stopwatch s = new Stopwatch();
                        s.Start();
                        string ipReturned;
                        try
                        {
                            var download = wb.DownloadString("http://www.aol.com");
                            ipReturned = wb.DownloadString("http://web.engr.oregonstate.edu/~boseakc/ipcheck.php");
                            Match m = Regex.Match(proxy, @".+?(?=abc)");
                            if (m.ToString() != ipReturned)
                                anon = true;
                            else
                                anon = false;
                        }
                        catch
                        {
                            //add to fail list
                        }
                        s.Stop();




                        if (s.ElapsedMilliseconds < 30000)
                        {
                            if (anon)
                            {
                                successfulAnonProxies.Add(proxy, Convert.ToInt32(s.ElapsedMilliseconds));
                                textBox2.BeginInvoke(new Action(() =>
                                {
                                    textBox2.AppendText(s.ElapsedMilliseconds.ToString() + "ms  |  " + proxy + Environment.NewLine);
                                    tabPage3.Text = "Anonymous - " + successfulAnonProxies.Count();
                                }));
                            }
                            else
                            {
                                successfulTransparentProxies.Add(proxy, Convert.ToInt32(s.ElapsedMilliseconds));
                                textBox3.BeginInvoke(new Action(() =>
                                {
                                    textBox3.AppendText(s.ElapsedMilliseconds.ToString() + "ms  |  " + proxy + Environment.NewLine);
                                    tabPage4.Text = "Non Anonymous - " + successfulTransparentProxies.Count();
                                }));
                            }


                        }
                    }
                    textBox1.BeginInvoke(new Action(() =>
                    {
                        textBox1.Lines = proxyList.ToArray();
                    }));
                });

            });
        }


    }
}
