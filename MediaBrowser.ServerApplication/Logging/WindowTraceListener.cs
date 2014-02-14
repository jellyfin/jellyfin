using System.Diagnostics;

namespace MediaBrowser.ServerApplication.Logging
{
    /// <summary>
    /// Class WindowTraceListener
    /// </summary>
    public class WindowTraceListener : DefaultTraceListener
    {
        /// <summary>
        /// The _window
        /// </summary>
        private readonly LogForm _window;
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowTraceListener" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        public WindowTraceListener(LogForm window)
        {
            _window = window;
            _window.Show();
            Name = "MBLogWindow";
        }

        /// <summary>
        /// Writes the value of the object's <see cref="M:System.Object.ToString" /> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object" /> whose fully qualified class name you want to write.</param>
        public override void Write(object o)
        {
            var str = o as string;
            if (str != null)
                Write(str);
            else
                base.Write(o);
        }

        /// <summary>
        /// Writes the output to the OutputDebugString function and to the <see cref="M:System.Diagnostics.Debugger.Log(System.Int32,System.String,System.String)" /> method.
        /// </summary>
        /// <param name="message">The message to write to OutputDebugString and <see cref="M:System.Diagnostics.Debugger.Log(System.Int32,System.String,System.String)" />.</param>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="ControlEvidence" />
        ///   </PermissionSet>
        public override void Write(string message)
        {
            _window.LogMessage(message);
        }

        /// <summary>
        /// Writes the output to the OutputDebugString function and to the <see cref="M:System.Diagnostics.Debugger.Log(System.Int32,System.String,System.String)" /> method, followed by a carriage return and line feed (\r\n).
        /// </summary>
        /// <param name="message">The message to write to OutputDebugString and <see cref="M:System.Diagnostics.Debugger.Log(System.Int32,System.String,System.String)" />.</param>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="ControlEvidence" />
        ///   </PermissionSet>
        public override void WriteLine(string message)
        {
            Write(message+"\n");
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Diagnostics.TraceListener" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_window != null)
                _window.ShutDown();
            base.Dispose(disposing);
        }
    }
}
