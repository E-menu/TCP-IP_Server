using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;


namespace Server_TCP_IP
{
    public class UsersData
    {
        public readonly object SyncRpi_users = new object();//Potrzebne do operacji na slowniku(kluczach) a nie wartosciach
        public readonly object SyncDesktop_users = new object();//Potrzebne do operacji na slowniku(kluczach) a nie wartosciach
        public volatile Dictionary<String, SyncTCPClient> Rpi_users;
        public volatile Dictionary<String, SyncTCPClient> Desktop_users;
        const int lengthofNick=6;
        public static Packet MakePackettoSend(byte[] bytes,int i)
        {
            //pierwszy bajt ilosc adresatow
            int numberofrecivers = (int)bytes[1];
            string recivers = Encoding.ASCII.GetString(bytes, 2, numberofrecivers *lengthofNick);
            string[] reciversarray = recivers.Split('#');
            byte[] data = new byte[i - numberofrecivers * lengthofNick-1];
            Array.ConstrainedCopy(bytes, numberofrecivers * lengthofNick + 2, data,1, i - numberofrecivers * lengthofNick - 2);
            data[0] = (byte)data.Length;

            
            
            return new Packet(reciversarray,data);
        }

        public void register_desktop(byte[] bytes, TcpClient tcp, int i)
        {
            Console.WriteLine( "Register"+tcp.Client.RemoteEndPoint.ToString());
            string Nick = Encoding.ASCII.GetString(bytes, 1, lengthofNick);
            lock(SyncDesktop_users) {
                if (!Desktop_users.ContainsKey(Nick))
                    Desktop_users.Add(Nick, new SyncTCPClient(tcp));
                else throw new ArgumentException();
                 }

        }
        public void register_Rpi(byte[] bytes, TcpClient tcp, int i)
        {
            Console.WriteLine("Register" + tcp.Client.RemoteEndPoint.ToString());
            string Nick = Encoding.ASCII.GetString(bytes, 1, lengthofNick);
            lock (SyncRpi_users)
            {
                if (!Rpi_users.ContainsKey(Nick))
                    Rpi_users.Add(Nick, new SyncTCPClient(tcp));
                else throw new ArgumentException();

            }
        }


        public UsersData()
        {
            Desktop_users = new Dictionary<string, SyncTCPClient>();
            Rpi_users = new Dictionary<string, SyncTCPClient>();
        }
    }
    public class SyncTCPClient
    {
        public TcpClient client;
        public readonly object sync;

        public SyncTCPClient(TcpClient _client)
        {
            client = _client;
            sync = new object();
        }
    }

    public class Packet
    {
        public Packet(string[] recivers, byte[] data)
        {
            this.recivers = recivers;
            this.data = data;

        }
        public string[] recivers;
        public byte[] data;
    }

    enum Comand_Type
    {
        Register_Rpi = 0x11,
        Register_desktop = 0x22,
        SendToRpi= 0x33,
        SendToDesktop=0x44
    };



}
