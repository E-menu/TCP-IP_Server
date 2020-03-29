using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;


namespace Server_TCP_IP
{
    public class UsersData
    {
        //Nick-IP
        public Dictionary<String, TcpClient> Rpi_users;
        public Dictionary<String, TcpClient> Desktop_users;
        const int lengthofNick=6;
        public static Packet MakePackettoSend(byte[] bytes,int i)
        {
            //pierwszy bajt ilosc adresatow
            int numberofrecivers = (int)bytes[1];
            string recivers = Encoding.ASCII.GetString(bytes, 2, numberofrecivers *lengthofNick);
            string[] reciversarray = recivers.Split('#');
            byte[] data = new byte[i - numberofrecivers * lengthofNick-2];
            Array.ConstrainedCopy(bytes, number ofrecivers * lengthofNick + 2, data,0, i - numberofrecivers * lengthofNick - 2);
            return new Packet(reciversarray,data);
        }

        public void register_desktop(byte[] bytes,TcpClient tcp, int i)
        {
            Console.WriteLine(tcp.Client.RemoteEndPoint.ToString());
            string Nick = Encoding.ASCII.GetString(bytes, 1, lengthofNick);
            Desktop_users.Add(Nick, tcp);
            /// Tylko do testu
            TcpClient test = Desktop_users[Nick];
            var stream= test.GetStream();
            string str = "Hey Device!";
               Byte[] reply = System.Text.Encoding.ASCII.GetBytes(str);
               stream.Write(reply, 0, reply.Length);
        }
        public void register_Rpi(byte[] bytes, TcpClient tcp, int i)
        {
            string Nick = Encoding.ASCII.GetString(bytes, 1, lengthofNick);
            Rpi_users.Add(Nick, tcp);
        }


        public UsersData()
        {
            Desktop_users = new Dictionary<string, TcpClient>();
            Rpi_users = new Dictionary<string, TcpClient>();
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
