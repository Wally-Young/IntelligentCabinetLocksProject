using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace tools
{
    public class SerFile
    {
        public static string Read(string name)
        {
            try
            {
                FileStream file = new FileStream(name, FileMode.OpenOrCreate);
                int count = 0;
                byte[] arr = new byte[100];
                List<byte> list = new List<byte>();
                while ((count = file.Read(arr,0,100)) != 0)
                {
                    list.AddRange(arr);
                }
                string ret = System.Text.Encoding.Default.GetString(list.ToArray());
                file.Close();
                return ret;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return "";
            }
        }

        public static void Write(string name, string data)
        {
            try
            {
                FileStream file = new FileStream(name, FileMode.Create);
                byte[] arr = System.Text.Encoding.Default.GetBytes(data);
                file.Write(arr, 0, arr.Length);
                file.Flush();
                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void Append(string name, string data)
        {
            try
            {
                FileStream file = new FileStream(name, FileMode.Append);
                byte[] arr = System.Text.Encoding.Default.GetBytes(data);
                file.Write(arr, 0, arr.Length);
                file.Flush();
                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
