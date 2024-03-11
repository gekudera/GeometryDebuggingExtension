﻿using EnvDTE;
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
using System.IO;
using System.Reflection;
using System.Linq.Expressions;


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
        bool clear_;
        private DebuggerEvents debuggerEvents;
        private DTE dte;
        private bool _autoDraw;
        private static string path_to_dll;


        // Импорт сторонних функций
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
        IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress,
        IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("GeomViewShell.dll")]
        public static extern void InitGeomView(string b);
        [DllImport("GeomViewShell.dll")]
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
            string address = "";
            //последние два стекфрейма выводят ненужные переменные
            for (int i = 1; i < stackFrames.Count - 3; i++)
            {
                EnvDTE.Expressions expressions = stackFrames.Item(i).Locals;
                foreach (EnvDTE.Expression exp in expressions)
                {
                    if (exp.Type.Contains("*") && exp.Value.StartsWith("0x"))
                    {
                        address = exp.Value.Split(' ')[0];
                    }
                    else
                    {
                        var e = DTE.Debugger.GetExpression("&" + exp.Name);
                        address = e.Value;
                    }
                    
                    MyData.Add(new MySampleData
                    {
                        Name = exp.Name,
                        Type = exp.Type,
                        Address = address,
                        Selected = false
                    });
                }
            }

            return true;
        }

        private void OpenGeomViewWindow()
        {
            //EnvDTE.DTE DTE = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;
            //string path = "";
            //foreach (Project project in DTE.Solution)
            //{
            //    MessageBox.Show(project.Name + "  " + project.FullName + "    " + project.FileName);
            //    path += project.Name + "\\";
            //}
            //MessageBox.Show("path " + path);

            try
            {
                if (!is_gv_inited)
                {
                    is_gv_inited = true;
                    InitGeomView("visualized\\output_serializestring.txt");
                }
                else
                {
                    MessageBox.Show("reload");
                    ReloadGeomView();
                }
            }
            catch (Exception e)
            {
                //some problems with open gv
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
                ReloadDataGrid();
                checkbox1.IsChecked = false;
            }
            else
            {
                _autoDraw = false;
                shmem.Dispose();
            }
        }

        private void BreakHandler(dbgEventReason reason, ref dbgExecutionAction execAction)
        {
            if (!_autoDraw)
                return;

            if (reason == dbgEventReason.dbgEventReasonStep || reason == dbgEventReason.dbgEventReasonBreakpoint)
            {
                //не успевает обновляться параметр селектед
                Dispatcher.BeginInvoke(new Action(() => GoFullDrawProcess()));
            }
        }

        public GeometryDebuggingControl()
        {
            //Инициализация полей
            this.InitializeComponent();
            MyData = new ObservableCollection<MySampleData>();
            shmem = new SharedMemory();
            util = new Util();
            is_gv_inited = false;
            clear_ = false;
            dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE.DTE;
            ReloadDataGrid();
 
            // Подписка на события 
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

        private void AddNewItem()
        {
            MySampleData item = new MySampleData();
            item.Name = textBox1.Text;

            var exp = dte.Debugger.GetExpression("&"+item.Name);
            item.Address = exp.Value;
            exp = dte.Debugger.GetExpression(item.Name);
            item.Type = exp.Type;
            item.Selected = false;

            MyData.Add(item);
            Grid.ItemsSource = MyData;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            GoFullDrawProcess();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
             ReloadDataGrid();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _autoDraw = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _autoDraw = false;
        }

        private void ReloadDataGrid()
        {
            clear_ = true;
            MyData.Clear();
            if (GetLocalValues() == true)
                Grid.ItemsSource = MyData;
            clear_ = false;
        }

        void OnChecked(object sender, RoutedEventArgs e)
        {
            MyData[Grid.SelectedIndex].Selected = true;
        }

        void OffChecked(object sender, RoutedEventArgs e)
        {
             if (!clear_)
                MyData[Grid.SelectedIndex].Selected = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddNewItem();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            clear_ = true;
            MyData.Clear();
            Grid.ItemsSource = MyData;
            clear_ = false;
        }
    }
}