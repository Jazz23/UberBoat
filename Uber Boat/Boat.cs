using Lib_K_Relay;
using Lib_K_Relay.GameData;
using Lib_K_Relay.Interface;
using Lib_K_Relay.Networking;
using Lib_K_Relay.Networking.Packets;
using Lib_K_Relay.Networking.Packets.Client;
using Lib_K_Relay.Networking.Packets.DataObjects;
using Lib_K_Relay.Networking.Packets.Server;
using Lib_K_Relay.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Uber_Boat.Binaries;
using static Uber_Boat.WinApi;
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
        DateTime Kernel = new DateTime(2018, 02, 19, 22, 44, 21, DateTimeKind.Utc);
        public static Location Target;
        public static Location[] coords;
        public static int Speed = 50;
        public static List<Player> Players = new List<Player>();
        public static Entity Tele = null;
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
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            proxy.HookCommand("enterq", Command);
            proxy.HookCommand("leaveq", Command);
            proxy.HookCommand("heck", Command);
            proxy.HookCommand("ip", Command);
            proxy.HookPacket<NewTickPacket>(NT);
            proxy.HookPacket<UpdatePacket>(OnUpdate);
            proxy.HookPacket<UpdateAckPacket>(OnUpdateAck);
            proxy.HookPacket<GotoPacket>(OnTeleport);
            proxy.ClientDisconnected += onDisconnect;
            Task.Run(() => { while (true) { Update(); Thread.Sleep(1000); } });
            Task.Run(() => { while (true) { Move(); Thread.Sleep(Interval); } });
        }

        private void onDisconnect(Client client)
        {
            Player player;
            if (!Exists(client, out player)) return;
        }

        private void OnTeleport(Client client, GotoPacket packet)
        {
            Player player;
            if (!Exists(client, out player)) return;
            player.WaitTele = true;
            player.Virgin = false;
            Task.Run(() => { Thread.Sleep(10100); player.WaitTele = false; });
        }

        private void OnUpdate(Client client, UpdatePacket packet)
        {
            Player player;
            if (!Exists(client, out player)) return;
            player.blockcount++;
            if (!player.InRealm || player.LastConnection != "Realm of the Mad God") return;
            foreach (Entity ent in packet.NewObjs)
                if (Enum.IsDefined(typeof(Classes), (short)ent.ObjectType))
                    player.RenderedPlayers.Add(ent);
        }

        private void OnUpdateAck(Client client, UpdateAckPacket packet)
        {
            Player player;
            if (!Exists(client, out player)) return;
            if (player.blockcount == 0) packet.Send = false;
            else player.blockcount--;
        }

        private void Update()
        {
            string[] Data;
            try
            {
                Data = web.DownloadString("https://supremacy.gq/Boat/data.txt").Split(':');
            }
            catch
            {
                return;
            }
            if (Data.Count() != 6) return;
            if (Boat.Data.Count() == 6 && (Boat.Data[1] != Data[1] | Boat.Data[2] != Data[2]))
                foreach (Player player in Players)
                {
                    player.Virgin = true;
                    SendReconnect(player.client, Data[1], Data[2]);
                }

            Boat.Data = Data;
            string world = web.DownloadString("https://supremacy.gq/Boat/Worlds/" + Data[0] + ".txt");

            coords = world.Split('\n').Select(x => { return new Location(float.Parse(x.Split(':')[0]), float.Parse(x.Split(':')[1])); }).ToArray(); // Stupidly long line of code but it saves a couple of lines in the end ¯\_(ツ)_/¯
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
                int coord = ((int)DateTime.Now.ToUniversalTime().Subtract(Kernel).TotalMilliseconds / Speed) % (coords.Count() - 1);
                Target = coords[coord];
                foreach (Player player in Players)
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
            catch { } // if the player leaves mid loop its rip
        }

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

        private void Gotowards(Player player, Location Target)
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
            Player player;
            if (!Exists(client, out player)) return;
            if (player.InRealm && player.LastConnection == "Realm of the Mad God") SetClosestPlayer(player, packet);
            if (client.ObjectId != player.client.ObjectId)
            {
                player.client = client;
                SahDude(player, packet);
            }
        }

        private void SetClosestPlayer(Player player, NewTickPacket packet)
        {
            float closest = GetVecDistance(player.client.PlayerData.Pos, Target);
            if (Tele != null)
            {
                //closest = GetVecDistance(Tele.Status.Position, Target);
                if (packet.Statuses.Select(x => x.ObjectId).Contains(Tele.Status.ObjectId))
                    Tele.Status.Position = packet.Statuses.Single(x => x.ObjectId == Tele.Status.ObjectId).Position;
            }
            foreach (Status ent in packet.Statuses)
                if (player.RenderedPlayers.Select(x => x.Status.ObjectId).Contains(ent.ObjectId))
                {
                    float distance = GetVecDistance(ent.Position, Target);

                    if (distance < closest)
                    {
                        if (Tele != null) Console.WriteLine(closest + ", " + Tele.Status.Data.Single(x => x.Id == StatsType.Name).StringValue);
                        closest = distance;
                        Tele = player.RenderedPlayers.Single(x => x.Status.ObjectId == ent.ObjectId);
                        Console.WriteLine(distance + ", " + Tele.Status.Data.Single(x => x.Id == StatsType.Name).StringValue);
                    }
                }
        }

        private void Command(Client client, string command, string[] args)
        {
            if (command == "enterq")
            {
                if (client.PlayerData.Speed < 50) { client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Requires 50 or more speed.")); return; }
                Player heck;
                if (Exists(client, out heck)) { client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Already in Queue")); return; }
                Players.Add(new Player(GetForegroundWindow(), client));
                client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Entered Boat"));
                SendReconnect(client, Data[1], Data[2]);
            }
            else if (command == "leaveq")
            {
                Player player;
                if (Exists(client, out player))
                {
                    Players.Remove(player);
                    client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Left Boat"));
                    UnPressAll(player);
                }
                else client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "Your not in a queue to leave."));
            }
            else if (command == "ip" && client.State.LastRealm != null)
            {
                client.SendToClient(PluginUtils.CreateOryxNotification("Realm IP:", client.State.LastRealm.Name.ToString() + ", " + client.State.LastRealm.Host));
            }
            else
            {
                // Test command code here
            }
        }

        private void SahDude(Player player, NewTickPacket NT)
        {
            string LastConnection = player.LastConnection;
            UnPressAll(player);
            player.RenderedPlayers.Clear();

            if (player.InRealm && player.Virgin && player.WaitTele == false && LastConnection == "Realm of the Mad God")
            {
                Task.Run(() => { Teleport(player); });
            }
            else if (player.InRealm && player.client.PlayerData.Health < player.client.PlayerData.MaxHealth && LastConnection == "Nexus")
            {
                player.InRealm = false;
                Task.Run(() => Heal(player));
            }
            else if (player.InRealm && LastConnection == "Realm of the Mad God" && player.WaitTele == false)
            {
                SendTele(player);
            }
            else if (player.InRealm && LastConnection == "Nexus")
            {
                Command(player.client, "leaveq", new string[0]);
            }
        }

        
        private void Heal(Player player)
        {
            while (player.client.PlayerData.Health < player.client.PlayerData.MaxHealth)
            {
                Gotowards(player, new Location(134f, 136f));
                Thread.Sleep(1);
            }
            player.InRealm = true;
            SendReconnect(player.client, Data[1], Data[2]);
        }

        private void Teleport(Player player)
        {
            while (player.Virgin)
            {
                if (Tele != null)
                {
                    UnPressAll(player);
                    SendTele(player);
                }
                Thread.Sleep(10000);
            }
            //System.Threading.Thread.Sleep(125000);
        }

        private void SendTele(Player player)
        {
            var idk = GetVecDistance(player.client.PlayerData.Pos, Target);
            var tit = GetVecDistance(Tele.Status.Position, Target);
            if (player.WaitTele || Tele == null || player.client.Connected == false || idk <= tit) return;
            PlayerTextPacket packet = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);
            packet.Text = "/teleport " + Tele.Status.Data.Single(x => x.Id == StatsType.Name).StringValue;
            player.client.SendToServer(packet);
        }
    }

    public class Player
    {
        public bool holdup = false;
        public short[] CreateTile = { 0, 0 };
        public int MoveCount = 0;
        public bool WaitTele = false;
        public int blockcount = 0;
        public string LastConnection { get { return ((MapInfoPacket)client.State["MapInfo"]).Name; } }
        public List<Entity> RenderedPlayers = new List<Entity>();
        public bool InRealm = true;
        public bool Virgin = true;       // jdjfjklfadskjlfdsljk;fsd;lkjasfjkl;fsdjkl;fsdlkjdfslkjfdsjkl;jlk
        public string Name;
        public IntPtr Handle;
        public Client client;
        public List<int> Pressed = new List<int>();
        public Player(IntPtr Handle, Client client)
        {
            this.Name = client.PlayerData.Name;
            this.Handle = Handle;
            this.client = client;
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
