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
            base.SendData((PacketTypes)77, ip);
        }
    }
}
