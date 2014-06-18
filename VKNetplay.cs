using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Terraria;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI.DB;

namespace VoteKick
{
    class VKNetplay : Netplay
    {
        public static void ClientLoop(object threadContext)
        {
            Netplay.ResetNetDiag();
            Main.ServerSideCharacter = false;
            if (Main.rand == null)
            {
                Main.rand = new Random((int)DateTime.Now.Ticks);
            }
            if (WorldGen.genRand == null)
            {
                WorldGen.genRand = new Random((int)DateTime.Now.Ticks);
            }
            Main.player[Main.myPlayer].hostile = false;
            Main.clientPlayer = (Player)Main.player[Main.myPlayer].clientClone();
            for (int i = 0; i < 255; i++)
            {
                if (i != Main.myPlayer)
                {
                    Main.player[i] = new Player();
                }
            }
            Main.menuMode = 10;
            Main.menuMode = 14;
            if (!Main.autoPass)
            {
                Main.statusText = "Connecting to " + Netplay.serverIP;
            }
            Main.netMode = 1;
            Netplay.disconnect = false;
            Netplay.clientSock = new ClientSock();
            Netplay.clientSock.tcpClient.NoDelay = true;
            Netplay.clientSock.readBuffer = new byte[1024];
            Netplay.clientSock.writeBuffer = new byte[1024];
            bool flag = true;
            while (flag)
            {
                flag = false;
                try
                {
                    Netplay.clientSock.tcpClient.Connect(Netplay.serverIP, Netplay.serverPort);
                    Netplay.clientSock.networkStream = Netplay.clientSock.tcpClient.GetStream();
                    flag = false;
                }
                catch
                {
                    if (!Netplay.disconnect && Main.gameMenu)
                    {
                        flag = true;
                    }
                }
            }
            NetMessage.buffer[256].Reset();
            int num = -1;
            while (!Netplay.disconnect)
            {
                if (Netplay.clientSock.tcpClient.Connected)
                {
                    if (NetMessage.buffer[256].checkBytes)
                    {
                        NetMessage.CheckBytes(256);
                    }
                    Netplay.clientSock.active = true;
                    if (Netplay.clientSock.state == 0)
                    {
                        Main.statusText = "Found server";
                        Netplay.clientSock.state = 1;
                        NetMessage.SendData(1, -1, -1, "", 0, 0f, 0f, 0f, 0);
                    }
                    if (Netplay.clientSock.state == 2 && num != Netplay.clientSock.state)
                    {
                        Main.statusText = "Sending player data...";
                    }
                    if (Netplay.clientSock.state == 3 && num != Netplay.clientSock.state)
                    {
                        Main.statusText = "Requesting world information";
                    }
                    if (Netplay.clientSock.state == 4)
                    {
                        WorldGen.worldCleared = false;
                        Netplay.clientSock.state = 5;
                        if (Main.cloudBGActive >= 1f)
                        {
                            Main.cloudBGAlpha = 1f;
                        }
                        else
                        {
                            Main.cloudBGAlpha = 0f;
                        }
                        Main.windSpeed = Main.windSpeedSet;
                        // TODO GitFlip - Add cloud reset!
                        //Cloud.resetClouds();
                        Main.cloudAlpha = Main.maxRaining;
                        WorldGen.clearWorld();
                        if (Main.mapEnabled)
                        {
                            // TODO GitFlip - When we connect to the server we run this?
                            //Map.loadMap();
                        }
                    }
                    if (Netplay.clientSock.state == 5 && Main.loadMapLock)
                    {
                        float num2 = (float)Main.loadMapLastX / (float)Main.maxTilesX;
                        Main.statusText = string.Concat(new object[]
						{
							Lang.gen[68],
							" ",
							(int)(num2 * 100f + 1f),
							"%"
						});
                    }
                    else
                    {
                        if (Netplay.clientSock.state == 5 && WorldGen.worldCleared)
                        {
                            Netplay.clientSock.state = 6;
                            Main.player[Main.myPlayer].FindSpawn();
                            NetMessage.SendData(8, -1, -1, "", Main.player[Main.myPlayer].SpawnX, (float)Main.player[Main.myPlayer].SpawnY, 0f, 0f, 0);
                        }
                    }
                    if (Netplay.clientSock.state == 6 && num != Netplay.clientSock.state)
                    {
                        Main.statusText = "Requesting tile data";
                    }
                    if (!Netplay.clientSock.locked && !Netplay.disconnect && Netplay.clientSock.networkStream.DataAvailable)
                    {
                        Netplay.clientSock.locked = true;
                        Netplay.clientSock.networkStream.BeginRead(Netplay.clientSock.readBuffer, 0, Netplay.clientSock.readBuffer.Length, new AsyncCallback(Netplay.clientSock.ClientReadCallBack), Netplay.clientSock.networkStream);
                    }
                    if (Netplay.clientSock.statusMax > 0 && Netplay.clientSock.statusText != "")
                    {
                        if (Netplay.clientSock.statusCount >= Netplay.clientSock.statusMax)
                        {
                            Main.statusText = Netplay.clientSock.statusText + ": Complete!";
                            Netplay.clientSock.statusText = "";
                            Netplay.clientSock.statusMax = 0;
                            Netplay.clientSock.statusCount = 0;
                        }
                        else
                        {
                            Main.statusText = string.Concat(new object[]
							{
								Netplay.clientSock.statusText,
								": ",
								(int)((float)Netplay.clientSock.statusCount / (float)Netplay.clientSock.statusMax * 100f),
								"%"
							});
                        }
                    }
                    Thread.Sleep(1);
                }
                else
                {
                    if (Netplay.clientSock.active)
                    {
                        Main.statusText = "Lost connection";
                        Netplay.disconnect = true;
                    }
                }
                num = Netplay.clientSock.state;
            }
            try
            {
                Netplay.clientSock.networkStream.Close();
                Netplay.clientSock.networkStream = Netplay.clientSock.tcpClient.GetStream();
            }
            catch
            {
            }
            if (!Main.gameMenu)
            {
                Main.netMode = 0;
                // TODO GitFlip - See why we need to save a player
                //Player.SavePlayer(Main.player[Main.myPlayer], Main.playerPathName);
                Main.gameMenu = true;
                Main.menuMode = 14;
            }
            NetMessage.buffer[256].Reset();
            if (Main.menuMode == 15 && Main.statusText == "Lost connection")
            {
                Main.menuMode = 14;
            }
            if (Netplay.clientSock.statusText != "" && Netplay.clientSock.statusText != null)
            {
                Main.statusText = "Lost connection";
            }
            Netplay.clientSock.statusCount = 0;
            Netplay.clientSock.statusMax = 0;
            Netplay.clientSock.statusText = "";
            Main.netMode = 0;
        }

        public static void StartClient()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(VKNetplay.ClientLoop), 1);
        }
    }
}
