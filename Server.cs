using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace Server_TCP_IP
{
    class Server
    {

        TcpListener server = null;
        UsersData usersData = new UsersData();
        public Server(string ip, int port)
        {
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            server.Start();
            StartListener();
        }

        public void StartListener()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client); //Wjebac tych klientow do slownika z kluczem zarejsetrowanym nicku i tcpclient czyli de facto socket

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }

        }

        public void HandleDeivce(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            string imei = String.Empty;
            string data = null;
            Byte[] bytes = new Byte[256];
            int i;
            try
            {
                i = stream.Read(bytes, 0, bytes.Length);
                switch ((Comand_Type)bytes[0])
                {
                    case Comand_Type.Register_desktop:
                        usersData.register_desktop(bytes, client, i);
                        break;
                    case Comand_Type.Register_Rpi:
                        usersData.register_Rpi(bytes, client, i);
                        break;
                    case Comand_Type.SendToDesktop:
                        send_to_desktop(bytes, i);
                        break;
                    case Comand_Type.SendToRpi:
                        send_To_Rpi(bytes, i);
                        break;

                }
             
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }


        }


        private void send_to_desktop(byte[] data, int i)
        {

            Packet packettosend = UsersData.MakePackettoSend(data, i);
            foreach (string reciver in packettosend.recivers)
            {
                TcpClient client = usersData.Desktop_users[reciver];
                var stream = client.GetStream();
                stream.Write(packettosend.data, 0, packettosend.data.Length);
            }

        }
        private void send_To_Rpi(byte[] data, int i)
        {
            Packet packettosend = UsersData.MakePackettoSend(data, i);
            foreach (string reciver in packettosend.recivers)
            {
                TcpClient client = usersData.Rpi_users[reciver];
                var stream = client.GetStream();
                stream.Write(packettosend.data, 0, packettosend.data.Length);
            }

        }
        public bool isConnected(TcpClient tcp)
        {

            // Detect if client disconnected
            if (tcp.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                if (tcp.Client.Receive(buff, SocketFlags.Peek) == 0)
                {
                    // Client disconnected
                    return false;
                }
            }


            return true;
        }








    }
}