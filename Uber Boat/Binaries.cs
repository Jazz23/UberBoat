﻿using Lib_K_Relay.Networking;
using Lib_K_Relay.Networking.Packets;
using Lib_K_Relay.Networking.Packets.Client;
using Lib_K_Relay.Networking.Packets.DataObjects;
using Lib_K_Relay.Networking.Packets.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Uber_Boat.Boat;

namespace Uber_Boat
{
    class Binaries
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        public static bool block;

        public static void PressKey(bool Up, int Key, IntPtr Handle)
        {
            uint Press = 0;
            if (Up == true) Press = 0x101; else Press = 0x100;
            SendMessage(Handle, Press, new IntPtr(Key), new IntPtr(0));
        }

        public static void SendReconnect(Client client, string ip, string Name)
        {
            ReconnectPacket reconnect = (ReconnectPacket)Packet.Create(PacketType.RECONNECT);
            reconnect.IsFromArena = false;
            reconnect.GameId = 0;
            reconnect.KeyTime = client.Time;
            reconnect.Name = Name;
            reconnect.Stats = "";
            
            client.State.ConTargetAddress = ip;
            client.State.ConTargetPort = 2050;
            client.State.ConRealKey = new byte[0];
            reconnect.Key = Encoding.UTF8.GetBytes(client.State.GUID);
            reconnect.Host = "localhost";
            reconnect.Port = 2050;

            client.SendToClient(reconnect);
        }

        public static void CreateTile(Player player, short[] coords, UpdatePacket packet)
        {
            var Heck = packet.Tiles.ToList();
            //coords = new short[] { (short)Math.Floor(player.client.PlayerData.Pos.X - 1), (short)Math.Floor(player.client.PlayerData.Pos.Y - 1), (short)Math.Floor(player.client.PlayerData.Pos.X), (short)Math.Floor(player.client.PlayerData.Pos.Y) };

            Tile paintedTile = new Tile();
            paintedTile.X = coords[0];
            paintedTile.Y = coords[1];
            paintedTile.Type = 228;
            Heck.Add(paintedTile);

            Tile paintedTile2 = new Tile();
            paintedTile2.X = coords[2];
            paintedTile2.Y = coords[3];
            paintedTile2.Type = 43796;
            Heck.Add(paintedTile2);

            packet.Tiles = Heck.ToArray();
            player.CreateTile[0] = 0;
            player.CreateTile[1] = 0;
        }

        public static void OnUpdateAck(Client client, UpdateAckPacket packet)
        {
            if (block)
            {
                block = false;
                packet.Send = false;
            }
        }

        public static bool Exists(Client client, out Player player)
        {
            if (Players.Count() == 0) { player = null; return false; }
            try { string Name = client.PlayerData.Name; } catch { player = null; return false; }
            List<string> PlayerList = new List<string>();
            foreach (var Player in Players) PlayerList.Add(Player.Name);
            if (PlayerList.Contains(client.PlayerData.Name))
            {
                player = Players.Where(x => x.Name == client.PlayerData.Name).First();
                return true;
            }
            else
            {
                player = null;
                return false;
            }
        }

        public static void UnPressAll(Player player)
        {
            foreach (int Key in new int[] { 0x41, 0x44, 0x57, 0x53 })
                PressKey(true, Key, player.Handle);
        }

        public static float GetVecDistance(float[] one, float[] two)
        {
            return (float)Math.Sqrt(Math.Pow(one[0] - two[0], 2) + Math.Pow(one[1] - two[0], 2));
        }

        public static float GetVecDistance(Location one, Location two)
        {
            return (float)Math.Sqrt(Math.Pow(one.X - two.X, 2) + Math.Pow(one.Y - two.Y, 2));
        }
    }
}
