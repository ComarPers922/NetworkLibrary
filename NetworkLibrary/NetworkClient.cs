using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetworkLibrary
{
    public delegate void MessageArrivedHandler(object sender, byte[] data);
    public delegate void ConnectionLostHandler(object sender);

    public class NetworkClient: IDisposable, IEquatable<NetworkClient>
    {
        public string Name { set; get; } = "Untitled";

        private readonly TcpClient client;
        private Thread thread;
        private Thread ConnectionChecker;
        private NetworkStream networkStream;

        private readonly BinaryFormatter formatter = new BinaryFormatter();

        public event MessageArrivedHandler MessageArrivedEvent;
        public event ConnectionLostHandler ConnectionLostEvent;

        public bool Connected { get; private set; } = false;

        public NetworkClient(TcpClient tcpClient)
        {
            client = tcpClient;
            if(client.Connected)
            {
                InitializeClient();
            }
        }

        public NetworkClient():this(new TcpClient())
        {
            
        }

        public void Connect(IPAddress ip, int port)
        {
            client.Connect(ip, port);
            InitializeClient();
        }
        public void Disconnect()
        {
            if(!Connected)
            {
                return;
            }
            Connected = false;
            ConnectionLostEvent?.Invoke(this);
            Dispose();
        }
        public bool IsOnline()
        {
            try
            {
                var testData = NetworkData.GetNetworkBytes(new NetworkData(DataType.Heartbeat, null));
                networkStream.Write(testData, 0, testData.Length);
                return true;
            }
            catch (Exception)
            {
                Disconnect();
                return false;
            }
        }
        private void InitializeClient()
        {
            Connected = true;
            networkStream = client.GetStream();
            ConnectionChecker = new Thread(e =>
            {
                while (Connected)
                {
                    if (!IsOnline())
                    {
                        Disconnect();
                        break;
                    }
                    Thread.Sleep(500);
                }
            })
            {
                IsBackground = true
            };
            ConnectionChecker.Start();

            thread = new Thread(e =>
            {
                while (Connected)
                {
                    if (!client.Connected && !IsOnline())
                    {
                        Disconnect();
                        break;
                    }
                    if (client.Available <= 0)
                    {
                        continue;
                    }
                    lock (networkStream)
                    {
                        var data = new byte[client.Available];
                        networkStream.Read(data, 0, data.Length);
                        var networkdata = NetworkData.GetNetworkData(data);
                        if (networkdata.DataType != DataType.Heartbeat)
                        {
                            MessageArrivedEvent?.Invoke(this, networkdata.Data);
                        }
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public void Connect(IPEndPoint endPoint)
        {
            Connect(endPoint.Address, endPoint.Port);
        }
        public void Close()
        {
            Dispose();
        }
        public void Dispose()
        {
            Connected = false;
            client.Close();
            client.Dispose();
            networkStream.Dispose();
        }

        public void Send(byte [] data)
        {
            if(!Connected)
            {
                return;
            }
            try
            {
                var networkdata = new NetworkData(DataType.Normal, data);
                var sendingdata = NetworkData.GetNetworkBytes(networkdata);
                networkStream.Write(sendingdata, 0, sendingdata.Length);
            }
            catch
            {
                Disconnect();
            }
        }

        public void Send(object obj)
        {
            if(!client.Connected)
            {
                throw new IOException("Not Connected!");
            }
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                Send(stream.ToArray());
            }
        }

        public void Send(string message)
        {
            Send(Encoding.Default.GetBytes(message));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NetworkClient);
        }

        public bool Equals(NetworkClient other)
        {
            return other != null &&
                   Name == other.Name &&
                   EqualityComparer<TcpClient>.Default.Equals(client, other.client) &&
                   EqualityComparer<Thread>.Default.Equals(thread, other.thread) &&
                   EqualityComparer<Thread>.Default.Equals(ConnectionChecker, other.ConnectionChecker) &&
                   EqualityComparer<NetworkStream>.Default.Equals(networkStream, other.networkStream) &&
                   EqualityComparer<BinaryFormatter>.Default.Equals(formatter, other.formatter) &&
                   Connected == other.Connected;
        }

        public override int GetHashCode()
        {
            var hashCode = 1058046655;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<TcpClient>.Default.GetHashCode(client);
            hashCode = hashCode * -1521134295 + EqualityComparer<Thread>.Default.GetHashCode(thread);
            hashCode = hashCode * -1521134295 + EqualityComparer<Thread>.Default.GetHashCode(ConnectionChecker);
            hashCode = hashCode * -1521134295 + EqualityComparer<NetworkStream>.Default.GetHashCode(networkStream);
            hashCode = hashCode * -1521134295 + EqualityComparer<BinaryFormatter>.Default.GetHashCode(formatter);
            hashCode = hashCode * -1521134295 + Connected.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(NetworkClient client1, NetworkClient client2)
        {
            return EqualityComparer<NetworkClient>.Default.Equals(client1, client2);
        }

        public static bool operator !=(NetworkClient client1, NetworkClient client2)
        {
            return !(client1 == client2);
        }
    }
}
