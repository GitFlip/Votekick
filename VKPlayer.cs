using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Terraria;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI.DB;
using TShockAPI;

namespace VoteKick
{
    class VKPlayer : TSPlayer
    {
        public VKPlayer(int index)
            : base(index)
        {
        }

        public void Connect(string ip)
        {
            //base.SendData((PacketTypes)77, ip);
            //NetMessage.SendData(77, base.Index, -1, ip, 0, 0f, 0f, 0f, 0);
            SendData(77, base.Index, -1, ip, 0, 0f, 0f, 0f, 0);
        }

        private void SendData(int msgType, int remoteClient = -1, int ignoreClient = -1, string text = "", int number = 0, float number2 = 0f, float number3 = 0f, float number4 = 0f, int number5 = 0)
        {
            // TODO
            // base.SendData does this...
            // Check if ( RealPlayer && !ConnectionAlive ) return ;

            if (Main.netMode == 0)
            {
                return;
            }
            int num = 256;
            if (Main.netMode == 2 && remoteClient >= 0)
            {
                num = remoteClient;
            }
            if (!InvokeNetSendData(ref msgType, ref remoteClient, ref ignoreClient, ref text, ref number,
                ref number2, ref number3, ref number4, ref number5))
            {
                lock (NetMessage.buffer[num].writeBuffer)
                {
                    // For some reason cannot access MessageBuffer's public BinaryWriter variable...
                    // BinaryWriter binaryWriter = NetMessage.buffer[num].binaryWriter;
                    BinaryWriter binaryWriter = new BinaryWriter( new MemoryStream( NetMessage.buffer[num].writeBuffer) );
                    long position = 0;
                    binaryWriter.BaseStream.Position = 2L;
                    binaryWriter.Write((byte)msgType);
                    switch (msgType)
                    {
                        // TODO create PacketType enum. It must start at +1 of what
                        // TerrariaApi.Server.PacketType ends with. Currently that means
                        // Packet 77 is are new Connect PacketType
                            
                        case 77:
                            binaryWriter.Write(text);
                            break;
                    }
                    int num16 = (int)binaryWriter.BaseStream.Position;
                    binaryWriter.BaseStream.Position = position;
                    binaryWriter.Write((short)num16);
                    binaryWriter.BaseStream.Position = (long)num16;


                    if (Netplay.serverSock[remoteClient].tcpClient.Connected)
                    {
                        try
                        {
                            NetMessage.buffer[remoteClient].spamCount++;
                            Main.txMsg++;
                            Main.txData += num16;
                            Main.txMsgType[msgType]++;
                            Main.txDataType[msgType] += num16;
                            NetMessage.SendBytes(Netplay.serverSock[remoteClient], NetMessage.buffer[num].writeBuffer, 0, num16, new AsyncCallback(Netplay.serverSock[remoteClient].ServerWriteCallBack), Netplay.serverSock[remoteClient].networkStream);
                        }
                        catch
                        {
                        }
                    }

                    if (Main.verboseNetplay)
                    {
                        for (int num25 = 0; num25 < num16; num25++)
                        {
                            byte arg_2315_0 = NetMessage.buffer[num].writeBuffer[num25];
                        }
                    }
                    NetMessage.buffer[num].writeLocked = false;
                }
            }
        }
        private bool InvokeNetSendData(ref int msgType, ref int remoteClient, ref int ignoreClient, ref string text,
            ref int number, ref float number2, ref float number3, ref float number4, ref int number5)
        {
            // TODO
            // This can be removed because our msgType is never going to be ChatText
            //if (Main.netMode != 2 && msgType == (int)PacketTypes.ChatText && this.InvokeClientChat(ref text))
            //    return true;

            SendDataEventArgs args = new SendDataEventArgs
            {
                MsgId = (PacketTypes)msgType,
                remoteClient = remoteClient,
                ignoreClient = ignoreClient,
                text = text,
                number = number,
                number2 = number2,
                number3 = number3,
                number4 = number4,
                number5 = number5
            };

            ServerApi.Hooks.NetSendData.Invoke(args);

            msgType = (int)args.MsgId;
            remoteClient = args.remoteClient;
            ignoreClient = args.ignoreClient;
            text = args.text;
            number = args.number;
            number2 = args.number2;
            number3 = args.number3;
            number4 = args.number4;
            number5 = args.number5;
            return args.Handled;
        }
    }
}
