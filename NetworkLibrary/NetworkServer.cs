using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkLibrary
{
    public delegate void NewClientArrivedHandler(object sender, NetworkClient newClient);

    public class NetworkServer: IDisposable
    {
        private readonly TcpListener listener;
        private Thread thread;
        private bool IsThreadRunning = true;
        public event NewClientArrivedHandler NewClientArrivedEvent;

        public NetworkServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }
        public void Start()
        {
            listener.Start();
            IsThreadRunning = true;
            thread = new Thread(e =>
            {
                while (IsThreadRunning)
                {
                    NewClientArrivedEvent?.Invoke(this, new NetworkClient(listener.AcceptTcpClient()));
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }
        public void Stop()
        {
            listener.Stop();
            IsThreadRunning = false;
        }
        public void Dispose()
        {
            listener.Stop();
            IsThreadRunning = false;
        }
    }
}
