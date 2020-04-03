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
        class Maintainer
        {
            public readonly object sync;
            public Dictionary<string, SyncTCPClient> clients;
            public Maintainer(Dictionary<string,SyncTCPClient> _clients, object obj)
            {
                clients = _clients;
                sync = obj;
            }
        }
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
                Thread Maintainertask = new Thread(new ParameterizedThreadStart(MaintainceUsers));
                Maintainer desktop_maintainer = new Maintainer(usersData.Desktop_users,usersData.SyncDesktop_users);
                Maintainertask.Start(desktop_maintainer);
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    Thread t = new Thread(new ParameterizedThreadStart(RegisterDevice));
                    t.Start(client); //Wjebac tych klientow do slownika z kluczem zarejsetrowanym nicku i tcpclient czyli de facto socket
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }

        }

        public void RegisterDevice(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            string imei = String.Empty;
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

                }
             
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }


        }
        private void MaintainceUsers(object obj)
        {
            Maintainer maintainer = (Maintainer)obj;


             object sync=maintainer.sync;
        Dictionary<string, SyncTCPClient> clients = maintainer.clients;

        Byte[] bytes = new Byte[256];
            int i = 0;
            Dictionary<String, SyncTCPClient> usersLocalCopy;
            while (true)
            {
                lock (sync)
                {//mozna uzyc immutable types, w sumie to i tak sprowadzi sie do głębokiej kopii obiektu
                    usersLocalCopy = new Dictionary<string, SyncTCPClient>( clients);
                }
                foreach (var client in usersLocalCopy)
                {
                    while (client.Value == null)//Ponieważ semafor moze jeszcze nie istnieć np.klient wlasnie w trakcie rejestracji
                        Thread.Sleep(5);
                    lock (client.Value.sync)
                    {
                        try
                        {
                            var stream = client.Value.client.GetStream();
                            if (stream.DataAvailable)
                                i = stream.Read(bytes, 0, bytes.Length);

                            if (i != 0)
                            {
                                switch ((Comand_Type)bytes[0])
                                {
                                    case Comand_Type.SendToDesktop:
                                        send_to_desktop(bytes, i);
                                        break;
                                    case Comand_Type.SendToRpi:
                                        send_To_Rpi(bytes, i);
                                        break;
                                }
                            }
                        }
                        catch (System.InvalidOperationException e)
                        {
                            Console.WriteLine(e.Message);
                            lock (sync)
                            {
                                clients.Remove(client.Key);
                            }
                        }

                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    Thread.Sleep(500);

                }
            }
        }
        

        private void send_to_desktop(byte[] data, int i)
        {
            
                Packet packettosend = UsersData.MakePackettoSend(data, i);
                foreach (string reciver in packettosend.recivers)
                {
                try
                {
                    if (!usersData.Desktop_users.ContainsKey(reciver))
                        throw new Exception("Desktop user do not contains this nick:" + reciver);
                    lock (usersData.Desktop_users[reciver].sync)
                    {
                        sendPacket(packettosend, usersData.Desktop_users[reciver].client);

                    }
                }
                catch (Exception e)
                 {
                     Console.WriteLine(e.Message);
                }
        
                }
            

        }
        private void send_To_Rpi(byte[] data, int i)
        {
            Packet packettosend = UsersData.MakePackettoSend(data, i);
            foreach (string reciver in packettosend.recivers)
            {
                sendPacket(packettosend, usersData.Rpi_users[reciver].client);
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

        private void sendString(string measage, TcpClient client)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(measage);
            var stream = client.GetStream();
            stream.Write(bytes, 0, bytes.Length);


        }
        private void sendPacket(Packet packet,TcpClient client)
        {
            var stream = client.GetStream();
            stream.Write(packet.data, 0, packet.data.Length);


        }






    }
}