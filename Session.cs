﻿using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Audio;

namespace DuckBot
{
    public sealed class Session
    {
        private static readonly int Version = 1;

        public ulong ServerID { get; private set; }
        public Dictionary<string, SoftCmd> Cmds { get; private set; }
        public Dictionary<string, string> Vars { get; private set; }
        internal Dictionary<ulong, Inbox> Msgs { get; private set; }

        internal Audio.AudioStreamer AudioPlayer { get; set; }

        public bool ShowChanges { get; internal set; }
        public string Language { get; private set; }
        public bool PendingSave { get; private set; }
        public string MusicChannel { get; internal set; }

        public int PesistentVars
        {
            get
            {
                int ret = 0;
                lock (this)
                    foreach (string var in Vars.Keys)
                        if (!var.StartsWith("_")) ++ret;
                return ret;
            }
        }

        public Session(ulong sid)
        {
            ServerID = sid;
            Cmds = new Dictionary<string, SoftCmd>();
            Vars = new Dictionary<string, string>();
            Msgs = new Dictionary<ulong, Inbox>();
            ShowChanges = false;
            PendingSave = false;
            Language = "en-US";
            MusicChannel = "";
        }

        public void SetPending() { PendingSave = true; }

        public bool SetLanguage(string langCode)
        {
            if (Utils.IsCultureAvailable(langCode))
            {
                Language = langCode;
                SetPending();
                return true;
            }
            else return false;
        }

        public void Load(BinaryReader br)
        {
            int ver = br.ReadInt32();
            int count = br.ReadInt32();
            lock (this)
            {
                while (count-- > 0)
                {
                    string name = br.ReadString();
                    SoftCmd cmd = new SoftCmd();
                    cmd.Load(br);
                    Cmds.Add(name, cmd);
                }
                count = br.ReadInt32();
                while (count-- > 0)
                {
                    ulong user = br.ReadUInt64();
                    Inbox inb = new Inbox();
                    inb.Load(br);
                    Msgs.Add(user, inb);
                }
                ShowChanges = br.ReadBoolean();
                count = br.ReadInt32();
                while (count-- > 0)
                {
                    string name = br.ReadString();
                    string value = br.ReadString();
                    Vars.Add(name, value);
                }
                Language = br.ReadString();
                if (ver >= 1) MusicChannel = br.ReadString();
            }
        }

        public void Save()
        {
            string file = Path.Combine(DuckData.SessionsDir.FullName, "session_" + ServerID + ".dat");
            lock (this)
                using (BinaryWriter bw = new BinaryWriter(new FileStream(file, FileMode.Create, FileAccess.Write)))
                {
                    PendingSave = false;
                    bw.Write(Version);
                    bw.Write(Cmds.Count);
                    foreach (KeyValuePair<string, SoftCmd> kvp in Cmds)
                    {
                        bw.Write(kvp.Key);
                        kvp.Value.Save(bw);
                    }
                    bw.Write(Msgs.Count);
                    foreach (KeyValuePair<ulong, Inbox> kvp in Msgs)
                    {
                        bw.Write(kvp.Key);
                        kvp.Value.Save(bw);
                    }
                    bw.Write(ShowChanges);
                    bw.Write(PesistentVars);
                    foreach (KeyValuePair<string, string> kvp in Vars)
                        if (!kvp.Key.StartsWith("_"))
                        {
                            bw.Write(kvp.Key);
                            bw.Write(kvp.Value);
                        }
                    bw.Write(Language);
                    bw.Write(MusicChannel);
                }
        }

        public string AddMessage(ulong sender, ulong recv, string msg)
        {
            Inbox i;
            lock (this)
                if (!Msgs.ContainsKey(recv))
                {
                    i = new Inbox();
                    Msgs.Add(recv, i);
                }
                else i = Msgs[recv];
            return i.AddMessage(sender, msg);
        }

        public async void JoinAudio(Discord.Channel c)
        {
            IAudioClient client = await c.JoinAudio();
            if (AudioPlayer != null) AudioPlayer.AudioClient = client;
            else AudioPlayer = new Audio.AudioStreamer(client);
        }

        internal void AutoJoinAudio(Discord.Server srv)
        {
            foreach (Discord.Channel c in srv.FindChannels(MusicChannel, Discord.ChannelType.Voice))
            {
                JoinAudio(c);
                break;
            }
        }

        public void PlayAudio(string url)
        {
            if (AudioPlayer != null) AudioPlayer.Dispose();
            using (System.Net.WebClient wc = new System.Net.WebClient())
                AudioPlayer.Play(2, wc.OpenRead(url));
        }
    }
}