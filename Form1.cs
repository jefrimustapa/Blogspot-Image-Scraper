using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;


// coder : jefri mustapa (https://github.com/jefrimustapa)
// description : scrape images from specific blogspot based blog 
/* method: 
 * - making use of feeds from blogspot to fetch sitemaps. 
 * - retrieve each blog entries 
 * - search for all images link
 * - download image links
 */


namespace blogspotImgDownload
{

    public partial class Form1 : Form
    {
        public Thread thrd = null;

        public Form1()
        {
            InitializeComponent();
        }

        public void updateProgress(int progress)
        {
            if (thrd.ThreadState == ThreadState.AbortRequested)
                return;
            MethodInvoker action = delegate
            {
                toolStripProgressBar1.Value = progress;
            };
            this.BeginInvoke(action);

        }

        public void updateListBox(string messageToAdd)
        {
            if (thrd.ThreadState == ThreadState.AbortRequested)
                return;
            MethodInvoker action = delegate
            {
                listBox1.Items.Add(messageToAdd);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            };
            this.BeginInvoke(action);

        }

        public void clearListBox()
        {
            if (thrd.ThreadState == ThreadState.AbortRequested)
                return;
            MethodInvoker action = delegate
            {
                listBox1.Items.Clear();
            };
            this.BeginInvoke(action);

        }

        public void updateStripLbl1(string str)
        {
            if (thrd.ThreadState == ThreadState.AbortRequested)
                return;
            MethodInvoker action = delegate
            {
                toolStripStatusLabel1.Text = str;
            };
            this.BeginInvoke(action);

        }

        public void updateStripLbl2(string str)
        {
            if (thrd.ThreadState == ThreadState.AbortRequested)
                return;
            MethodInvoker action = delegate
            {
                toolStripStatusLabel2.Text = str;
            };
            this.BeginInvoke(action);

        }


        public void toggleButton()
        {
            MethodInvoker action = delegate
            {

                if (button1.Enabled == true)
                {
                    button1.Enabled = false;
                    button2.Enabled = true;
                    button3.Enabled = true;
                }
                else
                {
                    button1.Enabled = true;
                    button2.Enabled = false;
                    button3.Enabled = false;
                }


            };

            this.BeginInvoke(action);

        }

        private void button1_Click(object sender, EventArgs e)
        {

            CDownload dl = new CDownload();
            dl.url = textBox1.Text;

            dl.fm = this;

            resetStrip();
            thrd = new Thread(new ThreadStart(dl.download));
            thrd.Start();

            Application.DoEvents();


            return;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (thrd.ThreadState == ThreadState.Suspended)
            {
                button2.Text = "Pause";
                toolStripStatusLabel1.Text = toolStripProgressBar1.Value + "%";
                thrd.Resume();
            }
            else
            {
                button2.Text = "Resume";
                toolStripStatusLabel1.Text = "Paused - " + toolStripProgressBar1.Value + "%";
                thrd.Suspend();
            }

        }


        private void resizeProgressBar()
        {
            toolStripProgressBar1.Width = this.ClientSize.Width - 300;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            resizeProgressBar();
        }


        private void resetStrip()
        {
            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel2.Text = "";
            resizeProgressBar();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            resetStrip();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (thrd != null)
                thrd.Abort();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (thrd.ThreadState == ThreadState.Suspended)
                thrd.Resume();
            thrd.Abort();
            resetStrip();
            updateListBox("Download Aborted By User!");
            toggleButton();
        }
    }

    public class CDownload
    {
        public string url;
        public Form1 fm = null;


        public CDownload()
        {

        }

        public CDownload(string url)
        {
            this.url = url;
        }

        private int processUrl(ref string dir)
        {
            int iRet = 0;

            if (url == "")
            {
                iRet = -1;
                goto cleanup;
            }

            if (url.Substring(0, 7) != "http://")
                url = "http://" + url;

            url = url.TrimEnd(new char[] { '/' });

            string[] tmp1 = url.Split('/');
            dir = tmp1[2];

            System.IO.Directory.CreateDirectory(dir);

        cleanup:
            return iRet;
        }

        public void crawl()
        {

        }

        public void download()
        {
            string dir = "";
            string data = "";

            WebClient wc = new WebClient();

            fm.toggleButton();
            fm.clearListBox();

            int iRet = processUrl(ref dir);

            fm.updateListBox("Fetching sitemap...");
            try
            {
                data = wc.DownloadString(url + "/feeds/posts/default?orderby=UPDATED&max-results=1000000");
            }
            catch (Exception ex)
            {

            }

            if (data.Length < 10)
            {
                fm.updateListBox("Failed to get sitemap for " + url);
                fm.toggleButton();
                return;
            }

            //fetching the images link
            fm.updateListBox("Fetching image links...");
            data = data.Replace("&lt;", "<");
            data = data.Replace("&gt;", ">");
            data = data.Replace("&quot;", "'");


            List<Uri> links = FetchLinksFromSource(data);

            int progress = 0;

            fm.updateProgress(0);
            fm.updateListBox("Download Starting...");

            //download images

            for (int i = 0; i < links.Count; i++)
            {
                string link = links[i].ToString();
                string[] tmp = link.Split('/');

                string fname = dir + "/" + i + "_" + tmp[tmp.Length - 1];

                try
                {
                    fm.updateListBox("Downloading " + link);


                    byte[] dwnByte = wc.DownloadData(link);

                    FileStream fs = new FileStream(fname, FileMode.OpenOrCreate);
                    BinaryWriter bw = new BinaryWriter(fs);

                    bw.Write(dwnByte);

                    bw.Close();
                    fs.Close();



                }
                catch (Exception ex)
                {
                    fm.updateListBox("Failed Download " + fname + ", err:" + ex.Message);

                }

                //update the progress
                progress = (int)(((double)(i + 1) / (double)(links.Count)) * 100);
                fm.updateProgress(progress);
                fm.updateStripLbl1(progress + "%");
                fm.updateStripLbl2((i + 1) + " of " + links.Count);
            }

            fm.updateListBox("Download Complete");
            fm.toggleButton();

        }

        public static List<Uri> FetchLinksFromSource(string htmlSource)
        {

            List<Uri> links = new List<Uri>();
            List<string> tmp = new List<string>();

            string regexImgSrc = @"['|""]https?[\S]*\.(jpg|jpeg|png|bmp|gif)['|""]";
            MatchCollection matchesImgSrc = Regex.Matches(htmlSource, regexImgSrc, RegexOptions.IgnoreCase);
            foreach (Match m in matchesImgSrc)
            {
                tmp.Add(m.Value);
            }

            // clean links we got
            for (int i = 0; i < tmp.Count; i++)
            {
                string cleanLink = tmp[i].Replace("\'", "").Replace("\"", "");
                links.Add(new Uri(cleanLink));

            }

            return links;
        }
    }



    public class imageDownloader
    {
        public struct _image
        {
            string url;
            bool downloaded;
        }

        public struct _link
        {
            string url;
            bool crawled;
            bool downloaded;

        }

        public List<_link> linkList;
        public List<_image> imageList;
        public string blogUrl;
        public int progress;

    }

}
