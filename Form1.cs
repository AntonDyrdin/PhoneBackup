using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace PhoneBackup
{
    public partial class Form1 : Form
    {
        private string logPath = DateTime.Now.ToString().Replace(':', '-') + '-' + DateTime.Now.Millisecond.ToString() + ".txt";

        string source;
        string dest;
        public Form1()
        {
            InitializeComponent();
        }

        private FTPclient ftp;
        System.Threading.Thread thread;
        private void Form1_Load(object sender, EventArgs e)
        {
            label2.Text = "ftp://192.168.1.196:6969//";
            ftp = new FTPclient();
            ftp.Host = "192.168.1.196:6969";
            source = "";
              thread = new System.Threading.Thread(CopyingThread);

        }
        private void button1_Click(object sender, EventArgs e)
        {
            dest = "C:\\tryToDownloadFTP";
           
            thread.Start();

            label1.Text = "copying";
        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                ftp.ftpResponse.Close();
            }
            catch { }
            try
            {
                thread.Abort();
            }
            catch { }
        }
        private void onTick(object sender, EventArgs e)
        {
            if (thread.IsAlive == false)
            {
                label1.Text = "sleeping...";
            }
        }

        private void startCopying(string drivePrefix)
        {


        }

        void CopyingThread()
        {
            recurciveCopying(source, dest);
            log("Comlete", Color.Green);
        }
        private void recurciveCopying(string source, string dest)
        {
            var files = ftp.ListDirectory(source);
            foreach (FileStruct file in files)
            {
                if (!file.IsDirectory)
                {

                    // string length = BytesToString(new System.IO.FileInfo(source + "//" + file.Name).Length);
                    log("Copying " + source + "//" + file.Name + "...", Color.Gray);
                    try
                    {

                        Directory.CreateDirectory(dest);
                        log(BytesToString(ftp.DownloadFile(source + "/" + file.Name, dest + "\\" + file.Name)), Color.Gray);
                    }
                    catch
                    {
                        log("Can't copy: ", Color.Red);
                        log(source + "//" + file.Name + " to", Color.Yellow);
                    }
                }
                else
                {
                    if (file.Name != "Android" && file.Name != "Albums")
                        try
                        {
                            recurciveCopying(source + "//" + file.Name, dest + "\\" + file.Name);
                        }
                        catch
                        {
                            log("Can't copy: ", Color.Red);
                            log(source + "//" + file.Name, Color.Yellow);
                        }
                }
            }

        }

        private void CopyDir(DirectoryInfo soursDir, DirectoryInfo destDir)
        {
            while (true)
            {
                try
                {
                    CreateDir(soursDir, destDir);
                    DirectoryInfo[] dirs = soursDir.GetDirectories();
                    if (dirs.Length > 0)
                    {
                        foreach (DirectoryInfo i in dirs)
                        {
                            DirectoryInfo dir = new DirectoryInfo(destDir.FullName.ToString() + @"\" + i.Name.ToString());
                            CopyDir(i, dir);
                        }
                        break;
                    }
                    else
                        break;
                }
                catch { }
            }
        }

        private void CreateDir(DirectoryInfo soursDir, DirectoryInfo destDir)
        {
            try
            {
                if (!destDir.Exists)
                    destDir.Create();
                FileInfo[] FI = soursDir.GetFiles();
                if (FI.Length > 0)
                    foreach (FileInfo i in FI)
                        i.CopyTo(destDir.FullName.ToString() + @"\" + i.Name.ToString(), true);
            }
            catch { }
        }

        private String BytesToString(long byteCount)
        {
            string[] suf = { "Byt", "KB", "MB", "GB", "TB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_Click(object sender, EventArgs e)
        {

        }

        private void log(String s, System.Drawing.Color col)
        {
            s = DateTime.Now.ToLongTimeString() + "  " + s;
            this.logDelegate = new Form1.LogDelegate(this.delegatelog);
            this.logBox.Invoke(this.logDelegate, this.logBox, s, col);
            var strings = new string[1];
            strings[0] = s;
            File.AppendAllLines(logPath, strings);
        }
        public void delegatelog(RichTextBox richTextBox, String s, Color col)
        {
            richTextBox.SelectionColor = col;
            richTextBox.AppendText(s + '\n');
            richTextBox.SelectionColor = Color.White;
            richTextBox.SelectionStart = richTextBox.Text.Length;
        }

        public delegate void LogDelegate(RichTextBox richTextBox, string is_completed, Color col);
        public LogDelegate logDelegate;
        public delegate void StringDelegate(string is_completed);
        public delegate void VoidDelegate();
        public StringDelegate stringDelegate;
        public VoidDelegate voidDelegate;


    }

}
