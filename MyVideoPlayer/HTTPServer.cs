using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Data;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MyVideoPlayer
{
    class HTTPServer
    {
        private TcpListener listener;
        private bool running = false;
        Main main = new Main();

        public HTTPServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            Thread serverThread = new Thread(new ThreadStart(Run));
            serverThread.Start();
        }

        private void Run()
        {
            running = true;
            listener.Start();

            while (running)
            {
                TcpClient client = listener.AcceptTcpClient();

                Console.WriteLine("Cliente conectado");

                HandleClient(client);

                client.Close();
            }

            running = false;

            listener.Stop();
        }

        private void HandleClient(TcpClient client)
        {
            StreamReader rd = new StreamReader(client.GetStream()); 
            
            String msg = rd.ReadToEnd();

            string output = "[" + msg.Substring(msg.IndexOf('[') + 1, msg.IndexOf(']') - msg.IndexOf('[') - 1) + "]";

            DataTable dt = (DataTable)JsonConvert.DeserializeObject(output, (typeof(DataTable)));

            main.updateData(dt);
            Console.Write(output);
        }
    }
}
