using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WasatchNET
{
    public class LoggerTB : Logger
    {
        private TextBox textBox = null;
        public void setTextBox(TextBox tb)
        {
            textBox = tb;
        }

        public override void close()
        {
            lock (instance)
            {
                textBox = null;
                if (outfile != null)
                {
                    outfile.Flush();
                    outfile.Close();
                    outfile = null;
                }
            }
        }

        public override void save(string pathname)
        {
            try
            {
                TextWriter tw = new StreamWriter(pathname);
                tw.WriteLine(textBox.Text);
                tw.Close();
            }
            catch (Exception e)
            {
                error("can't write {0}: {1}", pathname, e.Message);
            }
        }

        protected override void log(LogLevel lvl, string fmt, params Object[] obj)
        {
            string threadName = null;
            if (Thread.CurrentThread.Name != null)
                threadName = $"[{Thread.CurrentThread.Name}] ";
            else if (Task.CurrentId != null)
                threadName = $"[Task 0x{Task.CurrentId:x4}] ";

            string msg = string.Format("{0}: {1}{2}: {3}",
                getTimestamp(),
                threadName,
                lvl,
                string.Format(fmt, obj));

            lock (instance)
            {
                Console.WriteLine(msg);

                if (outfile != null && outfile.BaseStream != null)
                {
                    outfile.WriteLine(msg);
                    outfile.Flush();
                }
                
                if (textBox != null)
                    textBox.BeginInvoke(new MethodInvoker(delegate { if (textBox != null) textBox.AppendText(msg + Environment.NewLine); }));
            }
        }

    }

}

