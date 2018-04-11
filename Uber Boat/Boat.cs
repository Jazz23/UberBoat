using Lib_K_Relay;
using Lib_K_Relay.Interface;
using Lib_K_Relay.Networking;
using Lib_K_Relay.Networking.Packets;
using Lib_K_Relay.Networking.Packets.Client;
using Lib_K_Relay.Networking.Packets.DataObjects;
using Lib_K_Relay.Networking.Packets.Server;
using Lib_K_Relay.Utilities;
using PlayerAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Uber_Boat.Binaries;
//var idk = DateTime.Now.ToUniversalTime().Subtract(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds % 800;    - Algorithm to calcuate coordinate point from starting point.
namespace Uber_Boat
{
    public class Boat : IPlugin
    {
        public string GetAuthor()
        {
            return "Jazz";
        }

        public string GetName()
        {
            return "Uber Boat";
        }

        public string GetDescription()
        {
            return "https://discord.gg/zxV7HfT";
        }
        
        public static string[] Data = new string[] { "dank", "memes" };
        Dictionary<string, string> worlds = new Dictionary<string, string>();
        DateTime Kernel = new DateTime(2018, 02, 19, 22, 44, 21, DateTimeKind.Utc);
        public static Location Target = new Location(100000, 100000);
        public static Location[] coords;
        public static int Speed = 50;
        public static List<bPlayer> Players = new List<bPlayer>();
        WebClient web = new WebClient();
        public static int Interval = 25;
        public static float Thiccness = 2;

        public string[] GetCommands()
        {
            return new string[]
            {
                "/enterq",
                "/leaveq",
            };
        }

        public void Initialize(Proxy proxy)
        {
            PlayerAPI.PlayerAPI.Start(proxy);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            proxy.HookCommand("enterq", Command);
            proxy.HookCommand("leaveq", Command);
            proxy.HookCommand("heck", Command);
            proxy.HookCommand("ip2", Command);
            proxy.HookCommand("ip", Command);
            proxy.HookPacket<NewTickPacket>(NT);
            proxy.HookPacket<UpdateAckPacket>(OnUpdateAck);
            proxy.HookPacket<GotoPacket>(OnTeleport);
            worlds["3"] = "http://freetexthost.com/0efejc035p";
            worlds["10"] = "http://freetexthost.com/wqwuerhg1b";
            worlds["12"] = "http://freetexthost.com/wzeew1ot5l";
            worlds["test"] = "http://freetexthost.com/3s0grzs5v3";
            Task.Run(() => { while (true) { Update(); Thread.Sleep(1000); } });
            Task.Run(() => { while (true) { Move(); Thread.Sleep(Interval); } });

            proxy.ClientDisconnected += (client) =>
            {
                bPlayer player;
                if (!Exists(client, out player)) return;
                Task.Run(() => PressPlay(player));
            };
        }

        private void onDisconnect(Client client)
        {
            bPlayer player;
            if (!Exists(client, out player)) return;
        }

        private void OnTeleport(Client client, GotoPacket packet)
        {
            bPlayer player;
            if (!Exists(client, out player)) return;
            player.WaitTele = true;
            player.Virgin = false;
            PluginUtils.Delay(10100, () => player.WaitTele = false);
        }

        private void OnUpdateAck(Client client, UpdateAckPacket packet)
        {
            bPlayer player;
            if (!Exists(client, out player)) return;
            packet.Send = !player.block;
            player.block = false;
        }

        private void Update()
        {
            string[] Data;
            try
            {
                Data = ExtractData(web.DownloadString("http://freetexthost.com/2nvcvw20rw")).Split(':');
            }
            catch
            {
                return;
            }
            if (Data.Count() != 6) return;
            if (Boat.Data.Count() == 6 && (Boat.Data[1] != Data[1] | Boat.Data[2] != Data[2]))
                foreach (bPlayer player in Players)
                {
                    player.Virgin = true;
                    player.client.State.ConTargetAddress = Data[1];
                    Proxy.DefaultServer = Data[1];
                    player.GotoRealm();
                }

            Boat.Data = Data;
            string world = ExtractData(web.DownloadString(worlds[Data[0]]));

            coords = world.Split(':').Select(x => { return new Location(float.Parse(x.Split(',')[0]), float.Parse(x.Split(',')[1])); }).ToArray(); // Stupidly long line of code but it saves a couple of lines in the end ¯\_(ツ)_/¯
            Interval = int.Parse(Data[4]);
            Thiccness = float.Parse(Data[5]);
            Speed = int.Parse(Data[3]);
            //foreach (var player in Players)
            //{
            //    if (player.InRealm == false && player.Virgin == true)
            //    {
            //        player.InRealm = true;
            //        SendReconnect(player.client, Data[1], Data[2]);
            //    }
            //}
        }

        private void Move()
        {
            if (Players.Count() == 0) return;
            try
            {
                // Algorithim to calcuate which coordinate point to target relative to the computers clock (timezones don't matter).
                var meme1 = DateTime.Now.ToUniversalTime();
                var meme2 = meme1.Subtract(Kernel).TotalMilliseconds;
                var meme6 = Math.Floor(meme2).ToString();
                var meme3 = int.Parse(meme6.Substring(meme6.Length - 8)) / Speed;
                var meme4 = coords.Count() - 1;
                var meme5 = meme3 % meme4;
                int coord =  meme5;
                Target = coords[coord];
                foreach (bPlayer player in Players)
                {
                    //if (!((short)Math.Floor(coords[coord - 1].X) == player.CreateTile[0] && (short)Math.Floor(coords[coord - 1].Y) == player.CreateTile[1]))
                    //{
                    //    player.CreateTile = new short[] { (short)Math.Floor(coords[coord - 1].X), (short)Math.Floor(coords[coord - 1].Y), (short)Math.Floor(Target.X), (short)Math.Floor(Target.Y) };
                    //    CreateTile(player);
                    //}
                    if (!player.InRealm || player.Virgin || Data.Count() == 0 || Data.Contains("heck")) return;
                    if (GetVecDistance(player.client.PlayerData.Pos, Target) >= Thiccness + 15 && player.WaitTele == false) SendTele(player);
                    //if (player.holdup == false) Task.Run(() => { UseAbility(player); });
                    Gotowards(player, Target);
                }
            }
            catch (Exception ex)
            { 
                Console.WriteLine("something ripped\n" + ex.Message);

            } // if the player leaves mid loop its rip
        }

        // GOING TO ADDT HIS PLAYRE API

        //private void UseAbility(Player player)
        //{
        //    player.holdup = true;
        //    UseItemPacket packet = (UseItemPacket)Packet.Create(PacketType.USEITEM);
        //    SlotObject slot = new SlotObject();
        //    slot.ObjectId = player.
        //    packet.ItemUsePos = player.client.PlayerData.Pos;
        //    packet.
        //    player.client.SendToClient()
        //    Thread.Sleep(1500);
        //    player.holdup = false;
        //}

        private void Gotowards(bPlayer player, Location Target)
        {
            float x, y; x = Target.X; y = Target.Y;
            float X, Y; X = player.client.PlayerData.Pos.X; Y = player.client.PlayerData.Pos.Y;
            List<int> KeysDown = new List<int>();
            if (X > x && Math.Abs(X - x) > Thiccness) KeysDown.Add(0x41);
            else if (X < x && Math.Abs(X - x) > Thiccness) KeysDown.Add(0x44);
            if (Y > y && Math.Abs(Y - y) > Thiccness) KeysDown.Add(0x57);
            else if (Y < y && Math.Abs(Y - y) > Thiccness) KeysDown.Add(0x53);
            player.MoveCount++;
            if (player.MoveCount > 50)
            {
                ResetKeys(player);
                player.MoveCount = 0;
            }
            PressAll(KeysDown.ToArray(), player);
        }

        private void NT(Client client, NewTickPacket packet)
        {
            bPlayer player;
            if (!Exists(client, out player)) return;
            if (client.ObjectId != player.client.ObjectId)
            {
                Console.WriteLine(client.ObjectId.ToString() + ", " + player.client.ObjectId.ToString());
                player.client = client;
                SahDude(player, packet);
            }
        }

        private void Command(Client client, string command, string[] args)
        {
            if (command == "enterq")
            {
                if (client.PlayerData.Speed < 50) { client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Requires 50 or more speed.")); return; }
                bPlayer heck;
                if (Exists(client, out heck)) { client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Already in Queue")); return; }
                Players.Add(new bPlayer(GetForegroundWindow(), client));
                client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Entered Boat"));
                SendReconnect(client, Data[1], Data[2]);
            }
            else if (command == "leaveq")
            {
                bPlayer player;
                if (Exists(client, out player))
                {
                    Players.Remove(player);
                    client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Left Boat"));
                    UnPressAll(player);
                }
                else client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Your not in a queue to leave."));
            }
            else if ((command == "ip2" || command == "ip") && client.State.LastRealm != null)
            {
                client.SendToClient(PluginUtils.CreateOryxNotification("Realm IP:", client.State.LastRealm.Name + ", " + client.State.LastRealm.Host));
            }
            else
            {
                // Test command code here
            }
        }

        private void SahDude(bPlayer player, NewTickPacket NT)
        {
            string LastConnection = player.LastConnection;
            UnPressAll(player);
            player.WaitTele = false;

            if (player.InRealm && player.Virgin && LastConnection == "Realm of the Mad God")
            {
                Console.WriteLine("1 sah");
                //SetBoatStuff(player);

                Task.Run(() => Teleport(player));
            }
            else if (player.InRealm && player.client.PlayerData.Health < player.client.PlayerData.MaxHealth && LastConnection == "Nexus")
            {
                Console.WriteLine("2 sah");
                player.InRealm = false;
                Task.Run(() => Heal(player));
            }
            else if (player.InRealm && LastConnection == "Realm of the Mad God")
            {
                Console.WriteLine("3 sah");
                //SetBoatStuff(player);
                SendTele(player);
            }
            else if (player.InRealm && LastConnection == "Nexus")
            {
                Console.WriteLine("4 sah");
                if (player.DontLeave)
                {
                    player.DontLeave = false;
                }
                else
                {
                    Command(player.client, "leaveq", new string[0]);
                }
            }
        }

        
        private void Heal(bPlayer player)
        {
            while (player.client.PlayerData.Health < player.client.PlayerData.MaxHealth)
            {
                Gotowards(player, new Location(160f, 127f));
                Thread.Sleep(1);
            }
            player.InRealm = true;
            player.GotoRealm();
        }

        private void Teleport(bPlayer player)
        {
            while (player.Virgin)
            {
                SendTele(player);
                Thread.Sleep(10000);
            }
        }

        private void SendTele(bPlayer player)
        {
            if (player.WaitTele || player.client.Connected == false || player.client.Self().Players.Count() == 0) return;
            Player closest = player.client.Self().Players.First();
            foreach (Player person in player.client.Self().Players)
            {
                if (!Blacklist.Contains(person.PlayerData.Name) && GetVecDistance(person.PlayerData.Pos, Target) < GetVecDistance(closest.PlayerData.Pos, Target))
                {
                    closest = person;
                }
            }
            if (GetVecDistance(player.client.PlayerData.Pos, Target) <= GetVecDistance(closest.PlayerData.Pos, Target))
            {
                player.Virgin = false;
            }
            else
            {
                Console.WriteLine(GetVecDistance(closest.PlayerData.Pos, Target).ToString() + ", " + GetVecDistance(player.client.PlayerData.Pos, Target).ToString());
                Console.WriteLine(Target.ToString() + ": " + closest.PlayerData.Pos.ToString() + ", " + player.client.PlayerData.Pos.ToString());

                PlayerTextPacket packet = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);
                packet.Text = "/teleport " + closest.PlayerData.Name;
                player.client.SendToServer(packet);
            }
        }
    }

    public class bPlayer
    {
        public bool DontLeave = false;
        public bool holdup = false;
        public short[] CreateTile = { 0, 0 };
        public int MoveCount = 0;
        public bool WaitTele = false;
        public bool block = false;
        public string LastConnection { get { return ((MapInfoPacket)client.State["MapInfo"]).Name; } }
        public bool InRealm = true;
        public bool Virgin = true;       // jdjfjklfadskjlfdsljk;fsd;lkjasfjkl;fsdjkl;fsdlkjdfslkjfdsjkl;jlk
        public string Name;
        public IntPtr Handle;
        public Client client;
        public List<int> Pressed = new List<int>();
        public bPlayer(IntPtr Handle, Client client)
        {
            this.Name = client.PlayerData.Name;
            this.Handle = Handle;
            this.client = client;
        }

        public void GotoRealm()
        {
            SendReconnect(client, Boat.Data[1], Boat.Data[2]);
        }
    }
}



/*case "W":
            Keyy = 0x57;
            break;
        case "A":
            Keyy = 0x41;
            break;
        case "S":
            Keyy = 0x53;
            break;
        case "D":
            Keyy = 0x44;
            break;*/
