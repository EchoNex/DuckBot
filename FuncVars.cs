﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuckBot
{
    public static class FuncVars
    {
        public delegate string CmdAct(string[] args, CmdParams msg);

        private static Dictionary<string, CmdAct> vars = new Dictionary<string, CmdAct>();

        static FuncVars()
        {
            vars.Add("user", (args, msg) => { return msg.sender.Name; });
            vars.Add("nickOrUser", (args, msg) =>
            {
                return string.IsNullOrWhiteSpace(msg.sender.Nickname) ? msg.sender.Name : msg.sender.Nickname;
            });
            vars.Add("input", (args, msg) =>
            {
                if (args.Length >= 1)
                    try
                    {
                        int ix = int.Parse(args[0]);
                        return msg.args.Split(' ')[ix];
                    }
                    catch { return "ERROR"; }
                return msg.args;
            });
            vars.Add("mention", (args, msg) =>
            {
                if (args.Length >= 1)
                {
                    Discord.User u = Program.FindUser(msg.server, args[0]);
                    return u == null ? "ERROR" : u.Mention;
                }
                else return msg.sender.Mention;
            });
            vars.Add("rand", (args, msg) =>
            {
                try
                {
                    int i1 = int.Parse(args[0]);
                    if (args.Length >= 2)
                        return (Program.Rand.Next(int.Parse(args[1]) - i1) + i1).ToString();
                    else return Program.Rand.Next(i1).ToString();
                }
                catch { return "ERROR"; }
            });
            vars.Add("command", (args, msg) =>
            {
                if (args.Length >= 1)
                {
                    Session s = Program.Inst.CreateSession(msg.server);
                    SoftCmd c;
                    lock (s) c = s.Cmds.ContainsKey(args[0]) ? s.Cmds[args[0]] : null;
                    if (c != null) return c.Run(new CmdParams(msg, args.Length >= 2 ? args[1] : ""));
                }
                return "ERROR";
            });
            vars.Add("if", (args, msg) =>
            {
                if (args.Length >= 3)
                    return args[0].Length == args[1].Length ? args[2] : args[3];
                else return "ERROR";
            });
            vars.Add("length", (args, msg) =>
            {
                return args.Length >= 1 ? args[0].Length.ToString() : "ERROR";
            });
            vars.Add("substr", (args, msg) =>
            {
                try
                {
                    string s = args[0];
                    int i1 = int.Parse(args[1]);
                    if (args.Length >= 3)
                    {
                        int i2 = int.Parse(args[2]);
                        return s.Substring(i1 >= 0 ? i1 : s.Length + i1, i2);
                    }
                    else return s.Substring(i1 >= 0 ? i1 : s.Length + i1);
                }
                catch { return "ERROR"; }
            });
            vars.Add("date", (args, msg) =>
            {
                if (args.Length >= 1) return DateTime.UtcNow.ToString(args[0]);
                else return DateTime.UtcNow.ToShortDateString();
            });
            vars.Add("time", (args, msg) =>
            {
                if (args.Length >= 1 && args[0] == "long") return DateTime.UtcNow.ToLongTimeString();
                else return DateTime.UtcNow.ToShortTimeString();
            });
            vars.Add("get", (args, msg) =>
            {
                Session s = Program.Inst.CreateSession(msg.server);
                lock (s)
                    return args.Length >= 1 && s.Vars.ContainsKey(args[0]) ? s.Vars[args[0]] : "ERROR";
            });
            vars.Add("set", (args, msg) =>
            {
                if (args.Length >= 2)
                {
                    Session s = Program.Inst.CreateSession(msg.server);
                    lock (s)
                        if (s.Vars.ContainsKey(args[0])) s.Vars[args[0]] = args[1];
                        else s.Vars.Add(args[0], args[1]);
                    s.SetPending();
                    return "";
                }
                else return "ERROR";
            });
            vars.Add("eval", (args, msg) =>
            {
                string arg = string.Join(",", args);
                return SoftCmd.CmdEngine(arg.Replace("^{", "{"), msg);
            });
            vars.Add("find", (args, msg) =>
            {
                return args.Length >= 2 ? args[0].IndexOf(args[1]).ToString() : "ERROR";
            });
        }

        public static bool Has(string name)
        {
            return vars.ContainsKey(name);
        }

        public static string Run(string name, string[] args, CmdParams msg)
        {
            return vars[name](args, msg);
        }

        public static void Add(string name, CmdAct action)
        {
            if (!Has(name)) vars.Add(name, action);
        }
    }
}
