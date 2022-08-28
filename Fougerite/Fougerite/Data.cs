using System;
using System.Collections;

namespace Fougerite
{
    public class Data
    {
        public System.Collections.Generic.List<string> chat_history = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> chat_history_username = new System.Collections.Generic.List<string>();
        private static Data _data;
        private static readonly DataStore DS = DataStore.GetInstance();
        public readonly Hashtable Fougerite_shared_data = new Hashtable();

        [Obsolete("Modules hosting plugins will manage plugin config files", false)]
        public static Hashtable inifiles = new Hashtable();

        [Obsolete("Replaced with DataStore.Add", false)]
        public void AddTableValue(string tablename, object key, object val)
        {
            DS.Add(tablename, key, val);
        }

        [Obsolete("Modules hosting plugins will manage plugin config files", false)]
        public string GetConfigValue(string config, string section, string key)
        {
            return null;
        }

        public static Data GetData()
        {
            if (_data == null)
            {
                _data = new Data();
            }

            return _data;
        }

        [Obsolete("Replaced with DataStore.Get", false)]
        public object GetTableValue(string tablename, object key)
        {
            return DS.Get(tablename, key);
        }

        [Obsolete("Modules hosting plugins will manage plugin config files", false)]
        public void Load()
        {
            return;
        }

        [Obsolete("Modules hosting plugins will manage plugin config files", false)]
        public void OverrideConfig(string config, string section, string key, string value)
        {
            return;
        }

        public string[] SplitQuoteStrings(string str)
        {
            return Facepunch.Utility.String.SplitQuotesStrings(str);
        }

        public int StrLen(string str)
        {
            return str.Length;
        }

        public string Substring(string str, int from, int to)
        {
            return str.Substring(from, to);
        }

        public int ToInt(string num)
        {
            return int.Parse(num);
        }

        public int ToInt(double num)
        {
            return (int) num;
        }

        public int ToInt(float num)
        {
            return (int) num;
        }

        public double ToDouble(string num)
        {
            return double.Parse(num);
        }

        public ulong ToUlong(string num)
        {
            return Convert.ToUInt64(num);
        }

        public long Tolong(string num)
        {
            return Convert.ToUInt32(num);
        }

        public string ToLower(string str)
        {
            return str.ToLower();
        }

        public string ToUpper(string str)
        {
            return str.ToUpper();
        }

        public double RoundUp(double value)
        {
            return Math.Ceiling(value);
        }

        public double RoundDown(double value)
        {
            return Math.Floor(value);
        }

        public double Round(double value, bool even)
        {
            return Math.Round(value, even ? MidpointRounding.ToEven : MidpointRounding.AwayFromZero);
        }
    }
}