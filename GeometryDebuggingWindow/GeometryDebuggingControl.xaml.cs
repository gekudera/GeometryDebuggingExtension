using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Globalization;
using System.Threading;
using System.Drawing;
using Microsoft.VisualStudio.Threading;


namespace GeometryDebuggingWindow
{
    public class MySampleData
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public string Address { get; set; }
        public bool Selected { get; set; }
    }

    public partial class GeometryDebuggingControl : UserControl
    {
        public ObservableCollection<MySampleData> MyData { get; }
        SharedMemory shmem { get; set; }
        Util util;
        bool is_gv_inited;
        private DebuggerEvents debuggerEvents;
        private DTE dte;

        private bool _autoDraw;


        // Импорт сторонних функций
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
        IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress,
        IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport(@"D:\\LOGOS work\\GeometryDebuggingWindow\\x64\\Release\\GeomViewShell.dll")]
        public static extern void InitGeomView(string b);
        [DllImport(@"D:\\LOGOS work\\GeometryDebuggingWindow\\x64\\Release\\GeomViewShell.dll")]
        public static extern void ReloadGeomView();

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000,
            All = 0x001F0FFF
        }

        private unsafe bool GetLocalValues()
        {
            EnvDTE.DTE DTE = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;

            if (DTE.Debugger.CurrentThread == null) return false;

            EnvDTE.StackFrames stackFrames = DTE.Debugger.CurrentThread.StackFrames;
            //последние два стекфрейма выводят ненужные переменные
            for (int i = 1; i < stackFrames.Count - 3; i++)
            {
                EnvDTE.Expressions expressions = stackFrames.Item(i).Locals;
                foreach (EnvDTE.Expression exp in expressions)
                {
                    var e = DTE.Debugger.GetExpression("&" + exp.Name);
                    //MessageBox.Show(exp.Name + "___" + e.Name + "____"+ e.Type + "_____" + e.Value);
                    MyData.Add(new MySampleData
                    {
                        Name = exp.Name,
                        Type = exp.Type,
                        Address = e.Value,
                        Selected = false
                    });
                }
            }

            return true;
        }

        private void OpenGeomViewWindow()
        {
            if (!is_gv_inited)
            {
                try
                {
                    is_gv_inited = true;
                    InitGeomView("D:\\LOGOS work\\geom_toy\\visualized\\output_serializestring.txt");
                }
                catch (Exception e)
                {
                    //MessageBox.Show("geom view was closed");
                }
            }
            else
            {
                MessageBox.Show("reload");
                ReloadGeomView();
            }
        }

        private void CreateDebuggingRemoteThread()
        {
            //узнать процесс айди
            int processID = 0;
            EnvDTE.Processes processes = dte.Debugger.DebuggedProcesses;
            foreach (EnvDTE.Process proc in processes)
                processID = proc.ProcessID;

            //узнать адрес функции
            var exp = dte.Debugger.GetExpression("&StartRemoteSerialize");
            string data = exp.Value;
            IntPtr funptr = util.ConvertFromStringToIntPtr(data);

            //remote thread
            IntPtr hHandle = OpenProcess(ProcessAccessFlags.All, false, processID);
            IntPtr createThreadRes = CreateRemoteThread(hHandle, IntPtr.Zero, 0, funptr, IntPtr.Zero, 0, out createThreadRes);
            CloseHandle(hHandle);

            //Заморозить
            var currentThread = dte.Debugger.CurrentThread;
            currentThread.Freeze();
            
            //континью
            dte.Debugger.Go(false);

            //получение информации, что все завершилось from SharedMemory
            string output = shmem.ReadFromMemory();

            try
            {
                //размораживаем мейн поток
                dte.Debugger.Break();
                if (currentThread != null)
                {
                    currentThread.Thaw();
                    dte.Debugger.CurrentThread = currentThread;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("the process was break by closing window");
            }

            OpenGeomViewWindow();   
        }

        private void CloseEventHandler()
        {
            if (IsVisible)
            {
                MyData.Clear();
                if (GetLocalValues() == true)
                    Grid.ItemsSource = MyData;
            }
            else
            {
                MessageBox.Show("Window was close");
                _autoDraw = false;
            }
        }

        private void BreakHandler(dbgEventReason reason, ref dbgExecutionAction execAction)
        {
            if (!_autoDraw)
                return;

            if (reason == dbgEventReason.dbgEventReasonStep || reason == dbgEventReason.dbgEventReasonBreakpoint)
            {
                Dispatcher.BeginInvoke(new Action(() => GoFullDrawProcess()));
            }
        }

        public GeometryDebuggingControl()
        {
            MyData = new ObservableCollection<MySampleData>();
            MyData.Clear();
            shmem = new SharedMemory();
            util = new Util();
            is_gv_inited = false;
            dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE.DTE;
            if (GetLocalValues() == true)
                Grid.ItemsSource = MyData;
            this.InitializeComponent();

            // Подписка на событие закрытия
            IsVisibleChanged += (a, b) => CloseEventHandler();

            debuggerEvents = dte.Events.DebuggerEvents;
            debuggerEvents.OnEnterBreakMode += BreakHandler;
        }

        private void GoFullDrawProcess()
        {
            string buffer = "";
            foreach (MySampleData item in MyData)
            {
                if (item.Selected)
                {
                    buffer += item.Name + "|" + item.Type + "|" + item.Address + "|";
                }
            }

            shmem.WriteToMemory(buffer);
            CreateDebuggingRemoteThread();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            GoFullDrawProcess();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            MyData.Clear();
            if (GetLocalValues() == true)
                Grid.ItemsSource = MyData;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _autoDraw = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _autoDraw = false;
        }
    }
}