using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WasatchNET
{
    public enum LogLevel { DEBUG, INFO, ERROR, NEVER };

    /// <summary>
    /// This interface is provided for COM clients (Delphi etc) who seem to find it useful.
    /// I don't know that .NET users would find much benefit in it.
    /// </summary>
    [ComVisible(true)]
    [Guid("7AF402E8-3051-43CE-AF24-29F44513A266")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface ILogger
    {
        LogLevel level { get; set; }

        /// <summary>
        /// If you're developing in WinForms, pass a TextBook into the Logger 
        /// for instant visualization!
        /// </summary>
        /// <param name="tb">the TextBox control where you would like log messages to appear</param>
        void setTextBox(TextBox tb);

        /// <summary>
        /// If you'd like log messages written to a text file, specify the path here.
        /// </summary>
        /// <remarks>Make sure the directory exists and is writable.</remarks>
        /// <param name="path">output path (e.g. "\\tmp\\WasatchNET.log")</param>
        void setPathname(string path);

        /// <summary>
        /// Whether debugging is enabled.
        /// </summary>
        /// <returns>true if debugging is enabled</returns>
        bool debugEnabled();

        /// <summary>
        /// peel-off the most recent error
        /// </summary>
        /// <returns>the most recent error message</returns>
        /// <remarks>other errors will remain in the "recent" queue; this does not necessary clear hasError()</remarks>
        string getLastError();

        /// <summary>
        /// Returns a list of recent errors.
        /// </summary>
        /// <returns>list of queued error strings</returns>
        /// <remarks>clears hasError()</remarks>
        List<string> getErrors();

        /// <summary>
        /// whether any recent errors have occurred
        /// </summary>
        /// <returns>true if one or more error messages are pending retrieval</returns>
        bool hasError();

        /// <summary>
        /// log an error message
        /// </summary>
        /// <param name="fmt">see String.Format() fmt</param>
        /// <param name="obj">see String.Format() args</param>
        void error(string fmt, params Object[] obj);

        /// <summary>
        /// log an info message
        /// </summary>
        /// <param name="fmt">see String.Format() fmt</param>
        /// <param name="obj">see String.Format() args</param>
        void info(string fmt, params Object[] obj);

        /// <summary>
        /// log a debug message
        /// </summary>
        /// <param name="fmt">see String.Format() fmt</param>
        /// <param name="obj">see String.Format() args</param>
        void debug(string fmt, params Object[] obj);

        /// <summary>
        /// write TextBox contents to a text file
        /// </summary>
        /// <param name="pathname">path to create</param>
        /// <remarks>only works if setTextBox() has been called; otherwise, use setPath()</remarks>
        void save(string pathname);

        void hexdump(byte[] buf, string prefix = "");
    }
}