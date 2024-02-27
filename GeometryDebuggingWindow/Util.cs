using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeometryDebuggingWindow
{
    internal class Util
    {
        public string GetFirstWord(string text)
        {
            var candidate = text.Trim();
            if (!candidate.Any(Char.IsWhiteSpace))
                return text;

            return candidate.Split(' ').FirstOrDefault();
        }

        public IntPtr ConvertFromStringToIntPtr(string str)
        {
            //Конвертируем из стринг в IntPtr
            IntPtr funptr = IntPtr.Zero;
            string firstword_ptr = GetFirstWord(str);
            if (str != "")
            {
                ulong lon = (ulong)new System.ComponentModel.UInt64Converter().ConvertFromString(firstword_ptr);
                funptr = (IntPtr)lon;
            }
            else
            {
                MessageBox.Show("fnptr or processID in null");
            }

            return funptr;
        }
    }
}
