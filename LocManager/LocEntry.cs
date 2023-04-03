using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace LocManager
{
    public enum Language
    {
        [Description("EN")] Debug,
        [Description("EN")] English,
        [Description("PL")] Polish,
        [Description("ES")] Spanish,
        [Description("PT")] Portuguese,
        [Description("ZH")] Chinese,
    }

    public static class Enumeration
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }
    }

    public class LocEntry
    {
        public LocEntry(string hierarchyPath, string entryName)
        {
            this.EntryName = entryName;
            this.HierarchyPath = hierarchyPath;

            this.Translations = new Dictionary<Language, string>();

            this.LocKey = $"{this.HierarchyPath}-{this.EntryName}".Hash();
        }

        public string LocKey { get; private set; }

        public string HierarchyPath { get; private set; }

        public string EntryName { get; private set; }

        public Dictionary<Language, string> Translations { get; set; }

        public override string ToString()
        {
            return $"LocKey#{this.LocKey}";
        }
    }

    public static class StringHash
    {
        static readonly int MaxBytes = 4;

        public static string Hash(this string locEntry)
        {
            using (SHA256 sHA256 = SHA256.Create())
            {
                byte[] bytes = sHA256.ComputeHash(Encoding.UTF8.GetBytes(locEntry));

                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < StringHash.MaxBytes; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
