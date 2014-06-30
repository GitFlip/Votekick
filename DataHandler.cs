﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using TShockAPI;
using TShockAPI.Net;
using System.IO.Streams;


namespace VoteKick
{
    public delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);
    public class GetDataHandlerArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public Player TPlayer
        {
            get { return Player.TPlayer; }
        }

        public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }
    public static class GetDataHandlers
    {
        //static string EditHouse = "house.edit";
        //static string TPHouse = "house.rod";
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;

        public static void InitGetDataHandler()
        {
            GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.PlayerUpdate, HandlePlayerUpdate},     // PacketType 13
                {PacketTypes.Tile, HandleTile},                     // PacketType 17
                {PacketTypes.TileSendSquare, HandleSendTileSquare}, // PacketType 20
            };
        }

        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (GetDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            return false;
        }

        private static bool HandlePlayerUpdate(GetDataHandlerArgs args)
        {
            // int Index = (int)args.Data.ReadByte();
            //TShock player = TShock.Players[args.Player.Index];
            var player = TShock.Players[args.Player.Index];
            VKPlayer vkplyr = new VKPlayer(player.Index);

            //args.Player.SendMessage("Hello from HandlePlayerUpdate: " + vkplyr.X, Color.Yellow);

            if ( vkplyr.X < Votekick.config.WestPosition )
            {
                vkplyr.Connect(Votekick.config.WestServer);
            }

            //args.Player.SendMessage("Hello from HandlePlayerUpdate: ", Color.Yellow);
            return false;
        }

        private static bool HandleSendTileSquare(GetDataHandlerArgs args)
        {
            Console.WriteLine("HandleSendTileSquare");
            var Start = DateTime.Now;

            short size = args.Data.ReadInt16();
            int tilex = args.Data.ReadInt16();
            int tiley = args.Data.ReadInt16();

            //args.Player.SendMessage("Hello from HandleSendTileSquare: " + tilex, Color.Yellow);

            return false;
        }
        private static bool HandleTile(GetDataHandlerArgs args)
        {
            Console.WriteLine("HandleTile");
            var Start = DateTime.Now;

            byte type = args.Data.ReadInt8();
            int x = args.Data.ReadInt16();
            int y = args.Data.ReadInt16();
            ushort tiletype = args.Data.ReadUInt16();

            int tilex = Math.Abs(x); //Not Used yet
            int tiley = Math.Abs(y); //Not Used yet

            //args.Player.SendMessage("Hello from HandleTile: " + x, Color.Yellow);

            //args.Player.SendTileSquare(x, y); //We might want to send this everytime we return true.... NO That makes the world not editable
            return false;
        }
    }
}
