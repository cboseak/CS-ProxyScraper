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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProxyScraper
{
    public partial class Form1 : Form
    {
        ConcurrentQueue<string> proxyList = new ConcurrentQueue<string>();
        SortedList<int, string> successfulProxies = new SortedList<int, string>();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ScraperLogic.buildScrapeList();
            ScraperLogic.scrapeProxyPages();
            proxyList = ScraperLogic.scrapeProxies();
            textBox1.Lines = proxyList.ToArray();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(proxyList, proxy =>
                {
                    using (WebClient wb = new WebClient())
                    {

                        WebProxy wp = new WebProxy(proxy);
                        wb.Proxy = wp;
                        Stopwatch s = new Stopwatch();
                        s.Start();
                        try
                        {
                            var download = wb.DownloadString("http://www.aol.com");
                        }
                        catch
                        {
                            //add to fail list
                        }
                        s.Stop();
                        if (s.ElapsedMilliseconds < 30000)
                        {
                            successfulProxies.Add(Convert.ToInt32(s.ElapsedMilliseconds), proxy);
                            textBox2.BeginInvoke(new Action(() =>
                            {
                                textBox2.AppendText(s.ElapsedMilliseconds.ToString() + "ms  |  " + proxy + Environment.NewLine);
                            }));

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
