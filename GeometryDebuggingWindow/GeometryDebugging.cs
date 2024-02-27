using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

namespace GeometryDebuggingWindow
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("c768f294-60a3-4724-8d19-f8fe15fb530c")]
    public class GeometryDebugging : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryDebugging"/> class.
        /// </summary>
        public GeometryDebugging() : base(null)
        {
            this.Caption = "GeometryDebugging";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new GeometryDebuggingControl();
        }
    }
}
