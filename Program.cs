using System;

namespace Server_TCP_IP
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server("127.0.0.1", 13000);
        }
    }
}
