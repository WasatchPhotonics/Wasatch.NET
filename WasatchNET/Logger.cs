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
        const int MAX_ERRORS = 100; // how many to queue for retrieval by getErrors()

        public enum LogLevel { DEBUG, INFO, ERROR, NEVER };
        public LogLevel level = LogLevel.INFO;

        TextBox textBox = null;
        StreamWriter outfile;

        LinkedList<string> errors = new LinkedList<string>();
        int errorCount;

        static public Logger getInstance()
        {
            return instance;
        }

        private Logger()
        {
        }

        /// <summary>
        /// If you're developing in WinForms, pass a TextBook into the Logger 
        /// for instant visualization!
        /// </summary>
        /// <param name="tb">the TextBox control where you would like log messages to appear</param>
        public void setTextBox(TextBox tb)
        {
            textBox = tb;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void setPathname(string path)
        {
            outfile = new StreamWriter(path);
        }

        string getTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff: ", CultureInfo.InvariantCulture);
        }

        public bool debugEnabled() { return level <= LogLevel.DEBUG; }

        // just peel-off the most recent error, but leave the rest
        public string getLastError()
        {
            lock (instance)
            {
                if (errorCount > 0)
                {
                    errorCount--;
                    string msg = errors.Last.ToString();
                    errors.RemoveLast();
                    return msg;
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Returns a list of recent errors.
        /// </summary>
        /// <returns>list of queued error strings</returns>
        public List<string> getErrors()
        {
            lock (instance)
            {
                if (errorCount > 0)
                {
                    List<string> retval = new List<string>(errors);
                    errors.Clear();
                    errorCount = 0;
                    return retval;
                }
                return null;
            }
        }

        public bool hasError()
        {
            lock (instance)
                return errorCount > 0;
        }

        public void error(string fmt, params Object[] obj)
        {
            lock (instance)
            {
                // you'd think there'd be a standard collection that does this
                errors.AddLast(String.Format(String.Format(fmt, obj)));
                errorCount++;
                while (errorCount > MAX_ERRORS)
                {
                    errors.RemoveFirst();
                    errorCount--;
                }
            }

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

                if (textBox != null)
                    textBox.BeginInvoke(new MethodInvoker(delegate { textBox.AppendText(msg + Environment.NewLine); }));
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