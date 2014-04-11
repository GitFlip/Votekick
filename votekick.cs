﻿using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using TShockAPI;
using TShockAPI.Extensions;
using Terraria;
using Newtonsoft.Json;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI.DB;

namespace Votekick
{
    [ApiVersion(1, 15)]
    public class Votekick : TerrariaPlugin
    {
        public override Version Version
        {
            get { return new Version("0.5"); }
        }

        public override string Name
        {
            get { return "Votekick"; }
        }

        public override string Author
        {
            get { return "CAWCAWCAW"; }
        }

        public override string Description
        {
            get { return "Vote to kick players"; }
        }

        public Votekick(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            TShockAPI.Commands.ChatCommands.Add(new Command("caw.votekick", Vote, "votekick", "vk"));
            TShockAPI.Commands.ChatCommands.Add(new Command("caw.reloadvotekick", Reload_Config, "reloadvotekick", "rvk"));
            ServerApi.Hooks.GameUpdate.Register(this, VoteEnder);
            ReadConfig();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, VoteEnder);
            }
            base.Dispose(disposing);
        }
        public static DateTime VoteCountDown = DateTime.UtcNow;
        int timestorun = 1;
        public static bool VoteKickRunning = false;
        public static Poll poll = new Poll();
        public class Poll
        {
            public List<TSPlayer> voters;
            public List<TSPlayer> votedyes;
            public List<TSPlayer> votedno;
            public TSPlayer playertobekicked;
            public Poll()
            {
                voters = new List<TSPlayer>();
                votedyes = new List<TSPlayer>();
                votedno = new List<TSPlayer>();
            }
        }

       
        private void VoteEnder(EventArgs args)
        {
            int active = TShock.Utils.ActivePlayers();
            double HalfVoteTime = (config.VoteTime / 2);
            int VoteTime = config.VoteTime;

            if (VoteKickRunning)
            {
                if ((DateTime.UtcNow-VoteCountDown).TotalSeconds > HalfVoteTime && timestorun > 0)
                {
                    TSPlayer.All.SendSuccessMessage("Votekick ending in {0} seconds to kick {1} ",(config.VoteTime/2), poll.playertobekicked.Name);
                    timestorun--;
                }

                if ((DateTime.UtcNow-VoteCountDown).TotalSeconds > VoteTime || active <= poll.voters.Count)
                {
                    double percentageofactive = ((active) * (config.PercentofPlayersVoteYesToKick / 100));
                    double totalvoters = poll.voters.Count;

                    if (VoteKickRunning && poll.votedyes.Count > poll.votedno.Count && poll.votedyes.Count >= percentageofactive)
                    {
                        TShock.Utils.Kick(poll.playertobekicked, config.KickMessage, true, false);
                        VoteKickRunning = false;
                        poll.voters.Clear();
                        poll.votedno.Clear();
                        poll.votedyes.Clear();
                        poll.playertobekicked = null;
                        timestorun = 1;
                    }

                    if (VoteKickRunning && poll.votedno.Count >= poll.votedyes.Count)
                    {
                        TSPlayer.All.SendInfoMessage("The votekick on " + poll.playertobekicked.Name + " has failed.");
                        VoteKickRunning = false;
                        poll.voters.Clear();
                        poll.votedno.Clear();
                        poll.votedyes.Clear();
                        poll.playertobekicked = null;
                        timestorun = 1;
                    }
                }
            }
        }

        public static void Vote(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /votekick [kick/voteyes/voteno/info/cancel]");
                return;
            }
                    switch (args.Parameters[0])
                    {
                        case "voteyes":
                if (!poll.voters.Contains(args.Player) && VoteKickRunning)
                {
                    args.Player.SendSuccessMessage("You have voted yes in the vote kick.");
                            poll.voters.Add(args.Player);
                            poll.votedyes.Add(args.Player);
                }
                else if (poll.voters.Contains(args.Player))
                {
                    args.Player.SendErrorMessage("You have already voted for this vote!");
                }
                else
                {
                    args.Player.SendInfoMessage("A vote is not running at this time.");
                }
                            break;
                        case "voteno":
                if (!poll.voters.Contains(args.Player) && VoteKickRunning)
                {
                    args.Player.SendSuccessMessage("You have voted no in the vote kick.");
                            poll.voters.Add(args.Player);
                            poll.votedno.Add(args.Player);
                }
                else if (poll.voters.Contains(args.Player))
                {
                    args.Player.SendErrorMessage("You have already voted for this vote!");
                }
                else
                {
                    args.Player.SendInfoMessage("A vote is not running at this time.");
                }
                            break;

                        case "kick":
                            if (args.Parameters.Count > 1 && !VoteKickRunning)
                            {
                                string playerstring = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                var players = TShock.Utils.FindPlayer(playerstring);
                                var plyr = players[0];
                                if (players.Count == 0)
                                {
                                    args.Player.SendErrorMessage("No player matched your query '{0}'", playerstring);
                                }
                                else if (players.Count > 1)
                                {
                                    TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                                }

                                TSPlayer.All.SendWarningMessage(args.Player.Name + " has started a votekick against " + plyr.Name);
                                VoteCountDown = DateTime.UtcNow;
                                VoteKickRunning = true;
                                poll.playertobekicked = plyr;
                            }
                            else if (VoteKickRunning)
                            {
                                args.Player.SendErrorMessage("A player has already started a votekick on " + poll.playertobekicked.Name);
                            }
                            else
                            {
                                args.Player.SendErrorMessage("Error! Please use /votekick kick playername");
                            }
                            break;

                        case "info":
                            if (VoteKickRunning)
                            args.Player.SendInfoMessage("Total Players: {0} Player to be kicked: {1}, Votes Yes: {2}, Votes No: {3}", TShock.Utils.ActivePlayers(), poll.playertobekicked.Name, poll.votedyes.Count, poll.votedno.Count);
                        else
                            args.Player.SendErrorMessage("There is no votekick running at this time");
                            break;

                        case "cancel":
                            if (args.Player.Group.HasPermission("caw.cancelvotekick"))
                            {
                                poll.voters.Clear();
                                poll.votedno.Clear();
                                poll.votedyes.Clear();
                                poll.playertobekicked = null;
                                VoteKickRunning = false;
                                
                                TSPlayer.All.SendInfoMessage(args.Player.Name + " has canceled the votekick to kick: " + poll.playertobekicked.Name);
                            }
                            else
                            {
                                args.Player.SendErrorMessage("You do not have permission to use that command!");
                            }
                            break;
                    }
        }

        private static Config config;
        private static void CreateConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "Votekick.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        config = new Config();
                        var configString = JsonConvert.SerializeObject(config, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
        }

        private static bool ReadConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "Votekick.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            config = JsonConvert.DeserializeObject<Config>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    Log.ConsoleError("Votekick config not found. Creating new one...");
                    CreateConfig();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
            return false;
        }

        public class Config
        {
            public int PercentofPlayersVoteYesToKick = 75;
            public string seconds = "Vote time in seconds";
            public int VoteTime = 10;
            public string KickMessage = "You have been vote kicked from the server";

        }

        private void Reload_Config(CommandArgs args)
        {
            if (ReadConfig())
            {
                args.Player.SendMessage("Votekick config reloaded sucessfully.", Color.Yellow);
            }
            else
            {
                args.Player.SendErrorMessage("Votekick config reloaded unsucessfully. Check logs for details.");
            }
        }
    }
}
