using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;

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
                Maintainer rpi_maintainer = new Maintainer(usersData.Rpi_users, usersData.SyncRpi_users);
                Maintainer desktop_maintainer = new Maintainer(usersData.Desktop_users, usersData.SyncDesktop_users);
                Thread MaintainerDesktoptask = new Thread(new ParameterizedThreadStart(MaintainceUsers));
                Thread MaintainerRpitask = new Thread(new ParameterizedThreadStart(MaintainceUsers));
                MaintainerDesktoptask.Start(desktop_maintainer);
                MaintainerRpitask.Start(rpi_maintainer);
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
                i = stream.Read(bytes, 0,9);
                switch ((Comand_Type)bytes[0])
                {
                    case Comand_Type.Register_desktop:
                        usersData.register_desktop(bytes, client, i);
                        break;
                    case Comand_Type.Register_Rpi:
                        usersData.register_Rpi(bytes, client, i);
                        break;

                }
                sendString("OK", client);
             
            }
            catch(ArgumentException e)
            {
                sendString("NO_OK", client);
                Console.WriteLine(e.Message+" Attepmt to register the same nick");
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
            byte[] lengthOfMessage = new byte[1];
            object sync=maintainer.sync;
        Byte[] bytes = new Byte[256];
            Dictionary<String, SyncTCPClient> usersLocalCopy;
            while (true)
            {
                lock (sync)
                {//mozna uzyc immutable types, w sumie to i tak sprowadzi sie do głębokiej kopii obiektu
                    usersLocalCopy = new Dictionary<string, SyncTCPClient>(maintainer.clients);
                }
                foreach (var client in usersLocalCopy)
                {
                    while (client.Value == null)//Ponieważ semafor moze jeszcze nie istnieć np.klient wlasnie w trakcie rejestracji
                        Thread.Sleep(50);
                    lock (client.Value.sync)
                    {
                        try
                        {
                            var stream = client.Value.client.GetStream();
                            if (client.Value.client.Available>0)
                            {
                                stream.Read(lengthOfMessage, 0, 1);
                                if (lengthOfMessage[0] != 0)
                                {
                                    int i = stream.Read(bytes, 0, (int)lengthOfMessage[0]);
                                    switch ((Comand_Type)bytes[0])
                                    {
                                        case Comand_Type.SendToDesktop:
                                            send_to_desktop(bytes, i, client.Value.client);
                                            break;
                                        case Comand_Type.SendToRpi:
                                            send_To_Rpi(bytes, i, client.Value.client);
                                            break;
                                        default:
                                            throw new FormatException("Wrong command");
                                    }
                                    stream.FlushAsync();
                                    sendString("OK", client.Value.client); /// Gdy komenda przreszła bez problemów
                                }
                            }
                        }
                        #region Catch
                        catch (System.IO.IOException e)
                        {
                            Console.WriteLine(e.Message);
                            lock (sync) { maintainer.clients.Remove(client.Key); }
                        }

                        catch (System.ObjectDisposedException e)
                        { 
                            Console.WriteLine(e.Message);
                            lock (sync) { maintainer.clients.Remove(client.Key); }
                        }
                        catch (System.InvalidOperationException e)
                        {
                            Console.WriteLine(e.Message);
                            lock (sync) { maintainer.clients.Remove(client.Key); }
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine(e.Message+client.Key);
                            sendString(e.Message,client.Value.client);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                        #endregion
                    }
                    Thread.Sleep(230);
                }
                Thread.Sleep(100);
            }
        }
        
        /// <summary>
        /// Jeżeli ktoregoś z odbiorców nie bedzie to wysylanie do nastepnych tez nie zostanie zrealizowane. Trzeba miec to na uwadze w aplikacji klienckiej
        /// </summary>
        /// <param name="data"></param>
        /// <param name="i"></param>
        private void send_to_desktop(byte[] data, int i,TcpClient sender)
        {
            
                Packet packettosend = UsersData.MakePackettoSend(data, i);
                foreach (string reciver in packettosend.recivers)
                {
                try
                {
                    if (!usersData.Desktop_users.ContainsKey(reciver))
                        throw new ArgumentException("Desktop_user_list do not contains this nick:" + reciver);
                    lock (usersData.Desktop_users[reciver].sync)
                    {
                        sendPacket(packettosend, usersData.Desktop_users[reciver].client);

                    }
                }
                #region Catch
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                    lock (usersData.SyncDesktop_users) { usersData.Desktop_users.Remove(reciver); }
                    sendString("Lost conenction with " + reciver, sender);
                }

                catch (System.ObjectDisposedException e)
                {
                    Console.WriteLine(e.Message);
                    lock (usersData.SyncDesktop_users) { usersData.Desktop_users.Remove(reciver); }
                    sendString("Lost conenction with " + reciver, sender);
                }
                catch (System.InvalidOperationException e)
                {
                    Console.WriteLine(e.Message);
                    lock (usersData.SyncDesktop_users) { usersData.Desktop_users.Remove(reciver); }
                    sendString("Lost conenction with " + reciver, sender);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                    sendString(e.Message, sender);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                #endregion

            }
            

        }
        private void send_To_Rpi(byte[] data, int i,TcpClient sender)
        {
            Packet packettosend = UsersData.MakePackettoSend(data, i);
            foreach (string reciver in packettosend.recivers)
            {
                try
                {
                    if (!usersData.Rpi_users.ContainsKey(reciver))
                        throw new ArgumentException("Rpi_user_list do not contains this nick:" + reciver);
                    lock (usersData.Rpi_users[reciver].sync)
                    {
                        sendPacket(packettosend, usersData.Rpi_users[reciver].client);
                    }
                }
                #region Catch
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                    lock (usersData.SyncRpi_users) { usersData.Rpi_users.Remove(reciver); }
                    sendString("Lost conenction with " + reciver, sender);
                }

                catch (System.ObjectDisposedException e)
                {
                    Console.WriteLine(e.Message);
                    lock (usersData.SyncRpi_users) { usersData.Rpi_users.Remove(reciver); }
                    sendString("Lost conenction with " + reciver, sender);
                }
                catch (System.InvalidOperationException e)
                {
                    Console.WriteLine(e.Message);
                    lock (usersData.SyncRpi_users) { usersData.Rpi_users.Remove(reciver); }
                    sendString("Lost conenction with " + reciver, sender);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                    sendString(e.Message, sender);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
#endregion

            }

        }
        public bool isConnected(TcpClient s)
        {
            {
                bool part1 = s.Client.Poll(1, SelectMode.SelectRead);
                bool part2 = (s.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }
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