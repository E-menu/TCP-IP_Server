using System;
using System.Collections.Generic;
using System.Text;

namespace Server_TCP_IP
{
    public class UsersData
    {
        //Nick-IP
        public Dictionary<String, String> Rpi_users;
        public Dictionary<String, String> Desktop_users;
        const int lengthofNick=6;
        public static Packet MakePackettoSend(byte[] bytes)
        {
            //pierwszy bajt ilosc adresatow
            int numberofrecivers = (int)bytes[1];
            string recivers = Encoding.ASCII.GetString(bytes, 1, numberofrecivers *lengthofNick);
            string[] reciversarray = recivers.Split('#');
            byte[] data = new byte[bytes.Length - numberofrecivers * lengthofNick - 1];
            bytes.CopyTo(data, numberofrecivers * lengthofNick + 1);
            return new Packet(reciversarray,data);

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
        Register= 0x11,
        SendToRpi=0x22,
        SendToDesktop=0x33
    };



}
