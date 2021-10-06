using Dynmap.API;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynmap
{
    public class MinecraftClient
    {
        // your "client" protocol version to tell the server 
        // doesn't really matter, server will return its own version independently
        // for detailed protocol version codes see here: https://wiki.vg/Protocol_version_numbers
        private const int Proto = 47;
        private const int BufferSize = short.MaxValue;

        public static async Task<ServerStatus> GetAsync(string address, int port, CancellationToken? token = null)
        {
            var ct = token ?? CancellationToken.None;

            using var client = new TcpClient();
            await client.ConnectAsync(address, port);
            using var stream = client.GetStream();

            var _offset = 0;
            var writeBuffer = new List<byte>();

            WriteVarInt(writeBuffer, Proto);
            WriteString(writeBuffer, address);
            WriteShort(writeBuffer, Convert.ToInt16(port));
            WriteVarInt(writeBuffer, 1);
            Flush(ct, writeBuffer, stream, 0);
            // yep, twice.
            Flush(ct, writeBuffer, stream, 0);

            var readBuffer = new byte[BufferSize];
            await stream.ReadAsync(readBuffer, 0, readBuffer.Length, ct);
            // done
            stream.Close();
            client.Close();
            // IF an IOException arises here, thie server is probably not a minecraft-one
            var length = ReadVarInt(ref _offset, readBuffer);
            var packet = ReadVarInt(ref _offset, readBuffer);
            var jsonLength = ReadVarInt(ref _offset, readBuffer);
            var json = ReadString(ref _offset, readBuffer, jsonLength);

            return JsonConvert.DeserializeObject<ServerStatus>(json);
        }


        #region request helper methods

        internal static byte ReadByte(ref int _offset, byte[] buffer)
        {
            var b = buffer[_offset];
            _offset += 1;
            return b;
        }

        internal static byte[] Read(ref int _offset, byte[] buffer, int length)
        {
            var data = new byte[length];
            Array.Copy(buffer, _offset, data, 0, length);
            _offset += length;
            return data;
        }

        internal static int ReadVarInt(ref int _offset, byte[] buffer)
        {
            var value = 0;
            var size = 0;
            int b;
            while (((b = ReadByte(ref _offset, buffer)) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                {
                    throw new IOException("This VarInt is an imposter!");
                }
            }
            return value | ((b & 0x7F) << (size * 7));
        }

        internal static string ReadString(ref int _offset, byte[] buffer, int length)
        {
            var data = Read(ref _offset, buffer, length);
            return Encoding.UTF8.GetString(data);
        }

        internal static void WriteVarInt(List<byte> buffer, int value)
        {
            while ((value & 128) != 0)
            {
                buffer.Add((byte)(value & 127 | 128));
                value = (int)((uint)value) >> 7;
            }
            buffer.Add((byte)value);
        }

        internal static void WriteShort(List<byte> buffer, short value)
        {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal static void WriteString(List<byte> buffer, string data)
        {
            var buff = Encoding.UTF8.GetBytes(data);
            WriteVarInt(buffer, buff.Length);
            buffer.AddRange(buff);
        }

        internal static void Write(NetworkStream stream, byte b)
        {
            stream.WriteByte(b);
        }

        internal static async void Flush(CancellationToken ct, List<byte> buffer, NetworkStream stream, int id = -1)
        {
            var buff = buffer.ToArray();
            buffer.Clear();

            var add = 0;
            var packetData = new[] { (byte)0x00 };
            if (id >= 0)
            {
                WriteVarInt(buffer, id);
                packetData = buffer.ToArray();
                add = packetData.Length;
                buffer.Clear();
            }

            WriteVarInt(buffer, buff.Length + add);
            var bufferLength = buffer.ToArray();
            buffer.Clear();

            await stream.WriteAsync(bufferLength, 0, bufferLength.Length, ct);
            await stream.WriteAsync(packetData, 0, packetData.Length, ct);
            await stream.WriteAsync(buff, 0, buff.Length, ct);
        }

        #endregion
    }
}
