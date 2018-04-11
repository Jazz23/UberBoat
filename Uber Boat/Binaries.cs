using Lib_K_Relay.Networking;
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
using System.Drawing;
using static Uber_Boat.Binaries;
using Uber_Boat;
using System.Threading;

namespace Uber_Boat
{
    class Binaries
    {
        // Get the focused window.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();
        // Send a message to a specific process via the handle.
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        // Gets the positions of the corners of a window via the MainWindowHandle.
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        // Converts a point in screen space to a point relative to hWnd's window.
        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        public static int[] Keys = new int[] { 0x41, 0x44, 0x57, 0x53 };

        public static void PressPlay(bPlayer player)
        {
            Thread.Sleep(5000);

            if ((player.client?.Connected ?? false))
            {
                return;
            }
            player.DontLeave = true;

            // Get the window details before pressing the button in case
            // it has changed size or position on the desktop.
            RECT windowRect = new RECT();
            GetWindowRect(player.Handle, ref windowRect);
            var size = windowRect.GetSize();

            // The play button is located half way across the
            // window and roughly 92% of the way to the bottom.
            int playButtonX = size.Width / 2 + windowRect.Left;
            int playButtonY = (int)((double)size.Height * 0.92) + windowRect.Top;

            // Convert the screen point to a window point.
            POINT relativePoint = new POINT(playButtonX, playButtonY);
            ScreenToClient(player.Handle, ref relativePoint);

            // Press the buttons.
            SendMessage(player.Handle, (uint)MouseButton.LeftButtonDown, new IntPtr(0x1), new IntPtr((relativePoint.Y << 16) | (relativePoint.X & 0xFFFF)));
            SendMessage(player.Handle, (uint)MouseButton.LeftButtonUp, new IntPtr(0x1), new IntPtr((relativePoint.Y << 16) | (relativePoint.X & 0xFFFF)));

            PressPlay(player);
        }

        public static string ExtractData(string content)
        {
            return content.Split("contentsinner\">")[1].Split("<div")[0].Replace("<br />", "").Replace("\n", "");
        }

        public static void PressAll(int[] Keys, bPlayer player)
        {
            //foreach (var Key in Keys) PressKey(false, Key, player.Handle);
            //foreach (int Key in new int[] { 0x41, 0x44, 0x57, 0x53 })
            //{
            //    if (!Keys.Contains(Key))
            //        PressKey(true, Key, player.Handle);
            //}
            foreach (var Key in Keys)
            {
                if (!player.Pressed.Contains(Key))
                {
                    player.Pressed.Add(Key);
                    PressKey(false, Key, player.Handle);
                }
            }
            foreach (var Key in player.Pressed.Where(x => !Keys.Contains(x)).ToList())
            {
                player.Pressed.Remove(Key);
                PressKey(true, Key, player.Handle);
            }
        }

        public static void ResetKeys(bPlayer player)
        {
            UnPressAll(player);
            foreach (int key in player.Pressed)
            {
                PressKey(false, key, player.Handle);
            }
        }

        public static void PressKey(bool Up, int Key, IntPtr Handle)
        {
            SendMessage(Handle, (uint)(Up ? 0x101 : 0x100), new IntPtr(Key), new IntPtr(0));
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

        public static void CreateTile(bPlayer player)
        {
            var coords = player.CreateTile;
            var Heck = new List<Tile>();
            UpdatePacket packet = (UpdatePacket)Packet.Create(PacketType.UPDATE);
            packet.Drops = new int[0];
            packet.NewObjs = new Entity[0];
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
            player.client.SendToClient(packet);
        }

        public static bool Exists(Client client, out bPlayer player)
        {
            if (Players.Count() == 0) { player = null; return false; }
            try { string Name = client.PlayerData.Name; } catch { player = null; return false; }
            if (Players.Select(x => x.Name).Contains(client.PlayerData.Name))
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

        public static void UnPressAll(bPlayer player)
        {
            foreach (int Key in Keys)
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

        public static void SetBoatStuff(bPlayer player)
        {
            UpdatePacket packet = (UpdatePacket)Packet.Create(PacketType.UPDATE);
            packet.Drops = new int[0];
            packet.Tiles = new Tile[coords.Length];
            packet.NewObjs = new Entity[0];
            List<Tile> heck = new List<Tile>();
            //coords = new short[] { (short)Math.Floor(player.client.PlayerData.Pos.X - 1), (short)Math.Floor(player.client.PlayerData.Pos.Y - 1), (short)Math.Floor(player.client.PlayerData.Pos.X), (short)Math.Floor(player.client.PlayerData.Pos.Y) };
            for (int i = 0; i <= coords.Length - 1; i++)
            {
                Tile paintedTile2 = new Tile();
                paintedTile2.X = (short)Math.Floor(coords[i].X);
                paintedTile2.Y = (short)Math.Floor(coords[i].Y);
                paintedTile2.Type = 43796;
                heck.Add(paintedTile2);
            }
            packet.Tiles = heck.ToArray();
            player.block = true;
            player.client.SendToClient(packet);
        }

        public static string[] Blacklist = new string[]
        {
            "Yangu",
            "Seus",
            "Radph",
            "Darq",
            "Zhiar",
            "Uoro",
            "Utanu",
            "Urake",
            "Eashy",
            "Vorck",
            "Orothi",
            "Oalei",
            "Oshyu",
            "Issz",
            "Eendi",
            "Ril",
            "Laen",
            "Idrae",
            "Ehoni",
            "Risrr",
            "Tal",
            "Rayr",
            "Vorv",
            "Iatho",
            "Deyst",
            "Eango",
            "Rilr",
            "Yimi",
            "Scheev",
            "Saylt",
            "Lorz",
            "Lauk",
            "Iri",
            "Iawa",
            "Oeti",
            "Tiar"
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public enum MouseButton
    {
        LeftButtonDown = 0x201,
        LeftButtonUp = 0x202
    }
}

public static class Extensions
{
    public static string[] Split(this string Input, string Seperator)
    {
        return Input.Split(new[] { Seperator }, StringSplitOptions.RemoveEmptyEntries);
    }

    public static Size GetSize(this RECT rect)
    {
        return new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
}