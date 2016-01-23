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
        SynchronizationContext _sync = new SynchronizationContext();

        ConcurrentBag<string> proxyList = new ConcurrentBag<string>();
        List<string> successfulAnonProxies = new List<string>();
        List<string> successfulTransparentProxies = new List<string>();
        List<string> badProxies = new List<string>();
        System.Windows.Forms.Timer tmr = new System.Windows.Forms.Timer();
        int currentPosition = 0;


        scrapeStatus status { get; set; }

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
            tmr.Tick += tmr_Tick;
            tmr.Start();
        }

        private async void tmr_Tick(object sender, object e)
        {
            await Task.Run(() => { 
                if(status == scrapeStatus.stopped)
                {
                    label1.BeginInvoke(new Action(() => {
                        label1.Text = "Idle";
                        label1.ForeColor = Color.DarkRed;
                        label2.Visible = false;
                    }));
                }
                else if (status == scrapeStatus.scrapingProxies)
                {
                    label1.BeginInvoke(new Action(() =>
                    {
                        label1.Text = "Scraping Proxies";
                        label1.ForeColor = Color.DarkGreen;
                    }));
                }
                else if (status == scrapeStatus.testingProxies)
                {
                    label1.BeginInvoke(new Action(() =>
                    {
                        label1.Text = "Testing Proxies";
                        label1.ForeColor = Color.DarkGreen;
                        label2.Visible = true;
                        label2.Text = currentPosition + " / " + proxyList.Count;
                    }));

                }
            });
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            status = scrapeStatus.scrapingProxies;
            await Task.Run(() =>
            {
                label1.BeginInvoke(new Action(() =>
                {
                    label1.ForeColor = Color.DarkGreen;
                    label1.Text = "Scraping Proxies";
                }));
                ScraperLogic.buildScrapeList();
                ScraperLogic.scrapeProxyPages();
                proxyList = new ConcurrentBag<string>(ScraperLogic.scrapeProxies().Distinct().ToList());
                textBox1.BeginInvoke(new Action(() =>
                {
                    textBox1.Lines = proxyList.ToArray();
                }));
            });


        }

        private void button2_Click(object sender, EventArgs e)
        {
            testProxyList();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            status = scrapeStatus.scrapingProxies;
            await Task.Run(() =>
            {
                ScraperLogic.buildScrapeList();
                ScraperLogic.scrapeProxyPages();
                proxyList = new ConcurrentBag<string>(ScraperLogic.scrapeProxies().Distinct().ToList());
                textBox1.BeginInvoke(new Action(() =>
                {
                    textBox1.Lines = proxyList.ToArray();
                }));
            });

            status = scrapeStatus.testingProxies;
            testProxyList();
        }




        private async void testProxyList()
        {
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            progressBar1.Maximum = proxyList.Count();

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
                        string countryReturned = "";
                        try
                        {

                            progressBar1.BeginInvoke(new Action(() =>
                            {
                                progressBar1.Increment(1);
                            }));

                            var download = wb.DownloadString("http://www.aol.com");
                            ipReturned = wb.DownloadString("http://ipinfo.io/ip");
                            countryReturned = wb.DownloadString("http://ipinfo.io/country");

                            Match m = Regex.Match(proxy, @".+?(?=:)");
                            bool legitIpAddr = Regex.IsMatch(ipReturned, @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
                            if (legitIpAddr && m.ToString() != ipReturned)
                                anon = true;
                            else
                                anon = false;
                        }
                        catch
                        { }
                        s.Stop();
                        if (s.ElapsedMilliseconds < 10000)
                        {
                            if (anon)
                            {
                                successfulAnonProxies.Add(proxy);
                                currentPosition++;
                                textBox2.BeginInvoke(new Action(() =>
                                {
                                    textBox2.AppendText(countryReturned +"  |  "+ s.ElapsedMilliseconds.ToString() + "ms  |  " + proxy + Environment.NewLine);
                                    tabPage3.Text = "Anonymous - " + successfulAnonProxies.Count();
                                }));
                            }
                            else
                            {
                                successfulTransparentProxies.Add(proxy);
                                currentPosition++;
                                textBox3.BeginInvoke(new Action(() =>
                                {
                                    textBox3.AppendText(s.ElapsedMilliseconds.ToString() + "ms  |  " + proxy + Environment.NewLine);
                                    tabPage4.Text = "Non Anonymous - " + successfulTransparentProxies.Count();
                                }));
                            }
                        }
                        else
                        {
                            badProxies.Add(proxy);
                            currentPosition++;
                            textBox4.BeginInvoke(new Action(() =>
                            {
                                textBox4.AppendText(proxy + Environment.NewLine);
                            }));
                        }
                    }
                    textBox1.BeginInvoke(new Action(() =>
                    {
                        textBox1.Lines = proxyList.ToArray();
                    }));
                });

            });
            progressBar1.Visible = false;
        }
    }
}
