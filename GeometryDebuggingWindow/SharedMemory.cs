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


namespace GeometryDebuggingWindow
{

    internal class SharedMemory
    {
        public static MemoryMappedFile mmf;
        public static MemoryMappedViewStream mmfvs;

        static public bool MemOpen()
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

            MemoryMappedFile sharedMemory = MemoryMappedFile.CreateOrOpen("MySharedMemory", 2 * size + 4);
            //Создаем объект для записи в разделяемый участок памяти

            using (MemoryMappedViewAccessor writer = sharedMemory.CreateViewAccessor(0, 2 * size + 4))
            {
                writer.Write(0, size);
                //запись сообщения с четвертого байта в разделяемой памяти
                writer.WriteArray<char>(4, message, 0, message.Length);
            }
            //System.Threading.Thread.Sleep(1000);
            MessageBox.Show(data + " ! " + size);
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
                        return len.ToString();
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }

        }
    }
}

