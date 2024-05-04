using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;


namespace SpellGenerator.app.utils
{
    public class FileNameUtil
    {
        static MD5 md5 = MD5.Create();
        static Dictionary<string, string> dic = new Dictionary<string, string>();
        static FileNameUtil()
        {
            foreach(char invc in Path.GetInvalidFileNameChars())
            {
                //System.Diagnostics.Debug.WriteLine(invc + "");
                //System.Diagnostics.Debug.WriteLine(String.Format(@"0x{0:x4}", (ushort)invc));
                dic[invc + ""] = String.Format(@"0x{0:x4}", (ushort)invc);
            }
        }

        private static string Escape(string name)
        {
            foreach (KeyValuePair<string, string> replace in dic)
            {
                name = name.Replace(replace.Key, replace.Value);
            }

            return name;
        }

        public static string Encode(string text)
        {
            string escapedName = Escape(text);
            if(escapedName.Length < 200)
            {
                return escapedName;
            }
            byte[] hashBytes= md5.ComputeHash(Encoding.UTF8.GetBytes(escapedName));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            string md5Str = sb.ToString();
            return escapedName.Substring(0, 100) + md5Str;
        }

        /*
        public static string UnEscape(string name)
        {
            foreach (KeyValuePair<string, string> replace in dic)
            {
                name = name.Replace(replace.Value, replace.Key);
            }

            return name;
        }*/
    }
}
