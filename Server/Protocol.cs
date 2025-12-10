using System;
using System.Text;

namespace GameServer
{
    // 패킷 ID (1바이트)
    public enum PacketId : byte
    {
        CS_LOGIN = 1,
        SC_LOGIN_RESULT,
        CS_MOVE,
        SC_MOVE,
        CS_CHAT,
        SC_CHAT
    }

    // 논리 패킷 구조체들
    public struct CSMOVE
    {
        public float X;
        public float Y;
        public float Z;
    }

    public struct SCMOVE
    {
        public int PlayerId;
        public float X;
        public float Y;
        public float Z;
    }

    // 프로토콜 인코더/디코더 
    public static class Protocol
    {
        // [size(2) + id(1)]
        public const int HeaderSize = 3;

        public static byte[] Encode(PacketId id, object payload)
        {
            byte[] body;

            switch (id)
            {
                // 문자열 계열 패킷
                case PacketId.CS_LOGIN:
                case PacketId.SC_LOGIN_RESULT:
                case PacketId.CS_CHAT:
                case PacketId.SC_CHAT:
                    {
                        string text = (string)payload;
                        body = Encoding.UTF8.GetBytes(text);
                        break;
                    }

                // 클라 -> 서버 이동
                case PacketId.CS_MOVE:
                    {
                        CSMOVE m = (CSMOVE)payload;
                        body = new byte[12];
                        Array.Copy(BitConverter.GetBytes(m.X), 0, body, 0, 4);
                        Array.Copy(BitConverter.GetBytes(m.Y), 0, body, 4, 4);
                        Array.Copy(BitConverter.GetBytes(m.Z), 0, body, 8, 4);
                        break;
                    }

                // 서버 -> 클라 이동
                case PacketId.SC_MOVE:
                    {
                        SCMOVE m = (SCMOVE)payload;
                        body = new byte[16];
                        Array.Copy(BitConverter.GetBytes(m.PlayerId), 0, body, 0, 4);
                        Array.Copy(BitConverter.GetBytes(m.X), 0, body, 4, 4);
                        Array.Copy(BitConverter.GetBytes(m.Y), 0, body, 8, 4);
                        Array.Copy(BitConverter.GetBytes(m.Z), 0, body, 12, 4);
                        break;
                    }

                default:
                    throw new ArgumentException($"Unknown packet id in Encode: {id}");
            }

            // 헤더까지 붙이기
            ushort size = (ushort)(HeaderSize + body.Length);
            byte[] packet = new byte[size];

            // size (0~1)
            Array.Copy(BitConverter.GetBytes(size), 0, packet, 0, 2);
            // id (2)
            packet[2] = (byte)id;
            // body (3~)
            Array.Copy(body, 0, packet, HeaderSize, body.Length);

            return packet;
        }

        public static object Decode(PacketId id, byte[] body)
        {
            switch (id)
            {
                case PacketId.CS_LOGIN:
                case PacketId.SC_LOGIN_RESULT:
                case PacketId.CS_CHAT:
                case PacketId.SC_CHAT:
                    return Encoding.UTF8.GetString(body);

                case PacketId.CS_MOVE:
                    {
                        if (body.Length < 12)
                            throw new ArgumentException("CS_MOVE body too short");

                        CSMOVE m;
                        m.X = BitConverter.ToSingle(body, 0);
                        m.Y = BitConverter.ToSingle(body, 4);
                        m.Z = BitConverter.ToSingle(body, 8);
                        return m;
                    }

                case PacketId.SC_MOVE:
                    {
                        if (body.Length < 16)
                            throw new ArgumentException("SC_MOVE body too short");

                        SCMOVE m;
                        m.PlayerId = BitConverter.ToInt32(body, 0);
                        m.X = BitConverter.ToSingle(body, 4);
                        m.Y = BitConverter.ToSingle(body, 8);
                        m.Z = BitConverter.ToSingle(body, 12);
                        return m;
                    }

                default:
                    throw new ArgumentException($"Unknown packet id in Decode: {id}");
            }
        }
    }
}
