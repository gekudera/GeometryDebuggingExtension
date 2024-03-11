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


namespace GeometryDebuggingWindow
{

    internal class SharedMemory
    {
        public MemoryMappedFile mmf;
        public MemoryMappedViewStream mmfvs;
        public int count;

        public SharedMemory()
        {
            count = 0;
        }

        public void Dispose()
        {
            count = 0;
            mmf?.Dispose();
            mmfvs?.Dispose();
        }

        public bool MemOpen()
        {
            try
            {
                mmf = MemoryMappedFile.OpenExisting("MySharedMemory2");
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
                mmf = MemoryMappedFile.CreateOrOpen("MySharedMemory2", 2 * size + 4);
            
            using (MemoryMappedViewAccessor writer = mmf.CreateViewAccessor(0, 2 * size + 4))
            {
                 writer.Write(0, size);
                 writer.WriteArray<char>(4, message, 0, message.Length);
            }
            
            //if (count == 0)
            //{
            //     System.Threading.Thread.Sleep(10000);
            //     count++;
            //}
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
                        // Read from the shared memory, just for this example we know there is a string
                        BinaryReader reader = new BinaryReader(mmfvs);
                        StringBuilder strb = new StringBuilder();
                        string str;
                        do
                        {
                            str = reader.ReadString();
                        } while (!String.IsNullOrEmpty(str));
                        return str;
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }

        }
    }
}

