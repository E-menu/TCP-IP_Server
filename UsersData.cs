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
            data[0] = (byte)(data.Length-1);

            
            
            return new Packet(reciversarray,data);
        }

        public void register_desktop(byte[] bytes, TcpClient tcp, int i)
        {
            Console.WriteLine( "Register"+tcp.Client.RemoteEndPoint.ToString());
            string Nick = Encoding.ASCII.GetString(bytes, 1, lengthofNick);
            lock(SyncDesktop_users) {
                if (!Desktop_users.ContainsKey(Nick))
                    Desktop_users.Add(Nick, new SyncTCPClient(tcp));
                else {
                    try
                    {
                        lock (Desktop_users[Nick].sync)
                        { //ping procedure
                            var stream = Desktop_users[Nick].client.GetStream();
                            Byte[] dummy = new Byte[1];
                            dummy[0] = 0;
                            stream.Write(dummy, 0,1);
                        }

                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message+ "Registered once again Nick: "+Nick);
                        Desktop_users.Remove(Nick);
                        Desktop_users.Add(Nick, new SyncTCPClient(tcp));// Asigned once again
                    }

                }




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
                else
                {
                    try
                    {
                        lock (Rpi_users[Nick].sync)
                        { //ping procedure
                            var stream = Rpi_users[Nick].client.GetStream();
                            Byte[] dummy = new Byte[1];
                            dummy[0] = 0;
                            stream.Write(dummy, 0, 1);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "Registered once again Nick: " + Nick);
                        Rpi_users.Remove(Nick);
                        Rpi_users.Add(Nick, new SyncTCPClient(tcp));// Asigned once again
                    }

                }
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
