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
                    t.Start(client);
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
                client.Close();
                    switch ((Comand_Type)bytes[0])
                    {
                        case Comand_Type.Register:
                            register_user(bytes);
                            break;
                        case Comand_Type.SendToDesktop:
                            send_to_desktop(bytes);
                            break;
                        case Comand_Type.SendToRpi:
                            send_To_Rpi(bytes);
                            break;

                    }
                client.Close();
                    string hex = BitConverter.ToString(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("{1}: Received: {0}", data, Thread.CurrentThread.ManagedThreadId);
                    string str = "Hey Device!";
                    Byte[] reply = System.Text.Encoding.ASCII.GetBytes(str);
                    stream.Write(reply, 0, reply.Length);
                    Console.WriteLine("{1}: Sent: {0}", str, Thread.CurrentThread.ManagedThreadId);
                
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }


        }


        private void register_user(byte[] data)
        {


        }

        private void send_to_desktop(byte[] data)
        {

           Packet packettosend= UsersData.MakePackettoSend(data);
            foreach (string reciver in packettosend.recivers){
                string ip_number = usersData.Desktop_users[reciver];
                TcpClient client = new TcpClient();
                client.Connect(ip_number, 13000);
            var stream = client.GetStream();
                stream.Write(packettosend.data, 0, packettosend.data.Length);
                client.Close();

            }

        }
        private void send_To_Rpi(byte[] data)
        {
            Packet packettosend = UsersData.MakePackettoSend(data);
            foreach (string reciver in packettosend.recivers)
            {
                string ip_number = usersData.Desktop_users[reciver];
                TcpClient client = new TcpClient();
                client.Connect(ip_number, 13000);
                var stream = client.GetStream();
                stream.Write(packettosend.data, 0, packettosend.data.Length);
                client.Close();

            }

        }
    
    }





}