﻿using System;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;
using Discord;

namespace DuckBot
{
    public static class Utils
    {
        public static Task RunAsync<T>(Action<T> func, T arg, bool longRunning = false) =>
            Task.Factory.StartNew(() => func(arg), longRunning ? TaskCreationOptions.LongRunning : TaskCreationOptions.DenyChildAttach);
        
        public static string StartCase(this string data) => data == null ? null : char.ToUpper(data[0]) + data.Substring(1);

        public static bool IsCultureAvailable(string langCode, out CultureInfo culture)
        {
            culture = null;
            try { culture = CultureInfo.GetCultureInfo(langCode); }
            catch (CultureNotFoundException) { return false; }
            using (ResourceSet rs = Resources.Strings.ResourceManager.GetResourceSet(culture, true, false))
                return rs != null;
        }

        public static IGuildUser FindUser(this IGuild guild, string user)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (!string.IsNullOrWhiteSpace(user))
                foreach (IGuildUser u in guild.GetUsersAsync().GetAwaiter().GetResult())
                    if (u.Username.Equals(user, StringComparison.OrdinalIgnoreCase) || u.Mention == user)
                        return u;
            return null;
        }

        public static bool UserActive(this IPresence user) => user != null && (user.Status == UserStatus.Online || user.Status == UserStatus.DoNotDisturb);

        public static int Similarity(string data1, string data2)
        {
            if (string.IsNullOrEmpty(data1) || string.IsNullOrEmpty(data2)) return 0;
            data1 = data1.ToLower(); data2 = data2.ToLower();
            int pos1 = 0, pos2 = 0, ret = 0;
            while (pos1 < data1.Length && pos2 < data2.Length)
                if (data1[pos1] == data2[pos2])
                {
                    ++pos1; ++pos2; ++ret;
                }
                else if (data1.Length - pos1 > data2.Length - pos2) ++pos1;
                else ++pos2;
            return ret;
        }

        internal static string Extract(string input, string start, string end)
        {
            int ix = input.LastIndexOf(start);
            if (ix == -1) return null;
            ix += start.Length;
            int ie = input.IndexOf(end, ix);
            if (ie == -1) return null;
            return input.Substring(ix, ie - ix);
        }

        public static string ErrorCode(string error)
        {
            StringBuilder code = new StringBuilder(error);
            for (int i = code.Length - 1; i > 0; --i)
                if (char.IsUpper(code[i]))
                    code.Insert(i--, '_');
            return $"!{code.ToString().ToUpper()}_ERROR!";
        }

        public static string ErrorCode(this Exception ex) => ErrorCode(ex.GetType().Name);
    }
}
