using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    /// <summary>
    /// Singleton used throughout WasatchNET for logging and debugging.
    /// </summary>
    /// <remarks>
    /// Note that if running applications using Wasatch.NET from within Visual 
    /// Studio, log messages should be visible at the Console Output, even if no
    /// TextBox or Path has been set.
    /// </remarks>
    [ComVisible(true)]
    [Guid("5FED8E72-15B1-4ACA-B730-3B3006364D23")]
    [ProgId("WasatchNET.Logger")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Logger : ILogger
    {
        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        static readonly Logger instance = new Logger();

        const int MAX_ERRORS = 100; // how many to queue for retrieval by getErrors()
        LinkedList<string> errors = new LinkedList<string>();
        int errorCount;

        TextBox textBox = null;
        StreamWriter outfile;

        ////////////////////////////////////////////////////////////////////////
        // Public attributes
        ////////////////////////////////////////////////////////////////////////

        public LogLevel level { get; set; } = LogLevel.INFO;

        /// <summary>
        /// Get a handle to the Logger Singleton.
        /// </summary>
        /// <returns></returns>
        static public Logger getInstance()
        {
            return instance;
        }

        /// <summary>
        /// If you're developing in WinForms, pass a TextBook into the Logger 
        /// for instant visualization!
        /// </summary>
        /// <param name="tb">the TextBox control where you would like log messages to appear</param>
        /// <remarks>Note this creates a dependency on system_windows_forms_tlb in COM clients :-(</remarks>
        public void setTextBox(TextBox tb)
        {
            textBox = tb;
        }

        /// <summary>
        /// If you'd like log messages written to a text file, specify the path here.
        /// </summary>
        /// <remarks>Make sure the directory exists and is writable.</remarks>
        /// <param name="path">output path (e.g. "\\tmp\\WasatchNET.log")</param>
        public void setPathname(string path)
        {
            try
            {
                outfile = new StreamWriter(path);
                debug("log path set to {0}", path);
            }
            catch (Exception e)
            {
                error("Can't set log pathname: {0}", e);
            }
        }

        /// <summary>
        /// Whether debugging is enabled.
        /// </summary>
        /// <returns>true if debugging is enabled</returns>
        public bool debugEnabled()
        {
            return level <= LogLevel.DEBUG;
        }

        /// <summary>
        /// peel-off the most recent error
        /// </summary>
        /// <returns>the most recent error message</returns>
        /// <remarks>other errors will remain in the "recent" queue; this does not necessary clear hasError()</remarks>
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
        /// <remarks>clears hasError()</remarks>
        /// <remarks>returns a generic, hence is inaccessible from COM</remarks>
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

        /// <summary>
        /// whether any recent errors have occurred
        /// </summary>
        /// <returns>true if one or more error messages are pending retrieval</returns>
        public bool hasError()
        {
            lock (instance)
                return errorCount > 0;
        }

        /// <summary>
        /// log an error message
        /// </summary>
        /// <param name="fmt">see String.Format() fmt</param>
        /// <param name="obj">see String.Format() args</param>
        public bool error(string fmt, params Object[] obj)
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

            return false; // convenient for many cases
        }

        /// <summary>
        /// log an info message
        /// </summary>
        /// <param name="fmt">see String.Format() fmt</param>
        /// <param name="obj">see String.Format() args</param>
        public void info(string fmt, params Object[] obj)
        {
            if (level <= LogLevel.INFO)
                log(LogLevel.INFO, fmt, obj);
        }

        /// <summary>
        /// log a debug message
        /// </summary>
        /// <param name="fmt">see String.Format() fmt</param>
        /// <param name="obj">see String.Format() args</param>
        public void debug(string fmt, params Object[] obj)
        {
            if (level <= LogLevel.DEBUG)
                log(LogLevel.DEBUG, fmt, obj);
        }

        /// <summary>
        /// write TextBox contents to a text file
        /// </summary>
        /// <param name="pathname">path to create</param>
        /// <remarks>only works if setTextBox() has been called; otherwise, use setPath()</remarks>
        public void save(string pathname)
        {
            if (textBox == null)
            {
                error("can't save a log without a TextBox");
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
                error("can't write {0}: {1}", pathname, e.Message);
            }
        }

        public void hexdump(byte[] buf, string prefix = "")
        {
            string line = "";
            for (int i = 0;  i < buf.Length; i++)
            {
                if (i % 16 == 0)
                {
                    if (i > 0)
                    {
                        debug("{0}{1}", prefix, line);
                        line = "";
                    }
                    line += String.Format("{0:x4}:", i);
                }
                line += String.Format(" {0:x2}", buf[i]);
            }
            if (line.Length > 0)
                debug("{0}{1}", prefix, line);
        }

        ////////////////////////////////////////////////////////////////////////
        // Private methods
        ////////////////////////////////////////////////////////////////////////

        private Logger()
        {
        }

        string getTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff: ", CultureInfo.InvariantCulture);
        }

        void log(LogLevel lvl, string fmt, params Object[] obj)
        {
            string msg = getTimestamp() + " " + lvl + ": " + String.Format(fmt, obj);

            lock (instance)
            {
                Console.WriteLine(msg);

                if (outfile != null)
                    outfile.WriteLine(msg);

                if (textBox != null)
                    textBox.BeginInvoke(new MethodInvoker(delegate { textBox.AppendText(msg + Environment.NewLine); }));
            }
        }
    }
}
