using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace NetworkLibrary
{
    internal enum DataType
    {
        Normal, Heartbeat
    }
    [Serializable]
    internal class NetworkData
    {
        public DataType DataType { private set; get; }
        public byte[] Data { private set; get; }

        private readonly static BinaryFormatter formatter = new BinaryFormatter();

        public NetworkData(DataType dataType, byte[] data)
        {
            DataType = dataType;
            Data = data;
        }

        public static NetworkData GetNetworkData(byte[] data)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                var result = formatter.Deserialize(stream) as NetworkData;
                return result;
            }
        }

        public static byte[] GetNetworkBytes(NetworkData data)
        {
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, data);
                return stream.ToArray();
            }
        }
    }
}
