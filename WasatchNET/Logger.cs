using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Globalization;

namespace WasatchNET
{
    public class Logger
    {
        static readonly Logger instance = new Logger();

        public enum LogLevel { DEBUG, INFO, ERROR, NEVER };
        public LogLevel level = LogLevel.INFO;
        public string lastError { get; private set; }

        TextBox textBox = null;
        List<string> bufferedMessages;
        StreamWriter outfile;

        static public Logger getInstance()
        {
            return instance;
        }

        private Logger()
        {
        }

        // assign or change textbox after construction
        public void setTextBox(TextBox tb)
        {
            bufferedMessages = new List<string>();
            textBox = tb;
        }

        public void setPathname(string path)
        {
            outfile = new StreamWriter(path);
        }

        string getTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff: ", CultureInfo.InvariantCulture);
        }

        public bool debugEnabled() { return level <= LogLevel.DEBUG; }

        public void error(string fmt, params Object[] obj)
        {
            lock (instance)
                lastError = String.Format(String.Format(fmt, obj));

            if (level <= LogLevel.ERROR)
                log(LogLevel.ERROR, fmt, obj);
        }

        public void info(string fmt, params Object[] obj)
        {
            if (level <= LogLevel.INFO)
                log(LogLevel.INFO, fmt, obj);
        }

        public void debug(string fmt, params Object[] obj)
        {
            if (level <= LogLevel.DEBUG)
                log(LogLevel.DEBUG, fmt, obj);
        }

        void log(LogLevel lvl, string fmt, params Object[] obj)
        {
            string msg = String.Format("{0} {1}: {2}", getTimestamp(), lvl, String.Format(fmt, obj));

            lock (instance)
            {
                Console.WriteLine(msg);

                if (outfile != null)
                    outfile.WriteLine(msg);

                if (bufferedMessages != null)
                    bufferedMessages.Add(msg);
            }
        }

        // display any queued messages to the associated TextBox, if there is one; clear the queue
        public void flush()
        {
            lock (instance)
            {
                if (textBox != null && bufferedMessages.Count != 0)
                {
                    String joined = String.Join(Environment.NewLine, bufferedMessages) + Environment.NewLine;
                    bufferedMessages.Clear();
                    textBox.BeginInvoke(new MethodInvoker(delegate { textBox.AppendText(joined); }));
                }
            }
        }

        // write (not append) log to textfile
        public void save(string pathname)
        {
            if (textBox == null)
            {
                info("ERROR: can't save a log without a TextBox");
                return;
            }

            try
            {
                TextWriter tw = new StreamWriter(pathname);
                tw.WriteLine(textBox.Text);
                tw.Close();
            }
            catch (Exception e)
            {
                info("ERROR: can't write {0}: {1}", pathname, e.Message);
            }
        }

    }
}
