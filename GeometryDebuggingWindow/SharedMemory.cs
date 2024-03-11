using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Windows.Markup;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;


namespace GeometryDebuggingWindow
{

    internal class SharedMemory
    {
        public MemoryMappedFile mmf;
        public MemoryMappedViewStream mmfvs;


        public void Dispose()
        {
            mmfvs?.Dispose();
            mmf?.Dispose();
        }

        public bool MemOpen()
        {
            try
            {
                mmf = MemoryMappedFile.OpenExisting("MySharedMemory");
                mmfvs = mmf.CreateViewStream();
                return true;
            }
            catch
            {
                return false;
            }

        }

        public void WriteToMemory(string data)
        {
            //Ввод выражения для записи в общую память
            char[] message = data.ToCharArray();
            //Размер введенного сообщения
            int size = message.Length;

            if (mmf == null)
                mmf = MemoryMappedFile.CreateOrOpen("MySharedMemory", 2 * size + 4);
            
            using (MemoryMappedViewAccessor writer = mmf.CreateViewAccessor(0, 2 * size + 4))
            {
                 writer.Write(0, size);
                 writer.WriteArray<char>(4, message, 0, message.Length);
            }
        }

        public string ReadFromMemory()
        {
            while (true)
            {
                if (MemOpen())
                {
                    byte[] blen = new byte[4];
                    mmfvs.Read(blen, 0, 4);
                    int len = blen[0] + blen[1] * 256 + blen[2] * 65536 + blen[3] * 16777216;                

                    if (len == 1)
                    {
                        MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor();
                        byte[] bytes = new byte[200];
                        viewAccessor.ReadArray(4, bytes, 0, bytes.Length);
                        string text = Encoding.UTF8.GetString(bytes);
                        return text;
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }

        }
    }
}

