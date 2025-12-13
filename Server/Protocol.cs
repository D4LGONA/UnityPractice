using System;
using System.Text;

namespace PacketProtocol
{
    // 패킷 ID (1바이트)
    public enum PacketId : byte
    {
        CS_LOGIN = 1,
        SC_LOGIN_RESULT,
        CS_MOVE,
        SC_MOVE,
        CS_CHAT,
        SC_CHAT,
        CS_JOIN_GAME,
        CS_LEAVE_GAME,
        SC_SPAWN_PLAYER,
        SC_DESPAWN_PLAYER
    }

    // 논리 패킷 구조체들
    public struct CSJOINGAME
    {
        public string PlayerName;
    }

    public struct SCLOGINRESULT // 스폰 위치와 나의 아이디를 보내줌.
    {
        public int PlayerId;
        public float X;
        public float Y;
        public float Z;
    }

    public struct CSLEAVEGAME // 접속 종료
    {
    }

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

    public struct SCSPAWNPLAYER
    {
        public int PlayerId;
        public float X;
        public float Y;
        public float Z;
    }

    public struct SCDESPWNPLAYER
    {
        public int PlayerId;
    }

    public struct CSCHAT
    {
        public string Msg;
    }

    public struct SCCHAT
    {
        public int PlayerId;
        public string Msg;
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
                // 조인/로그인 결과/나가기/스폰/디스폰/이동 등
                case PacketId.CS_JOIN_GAME:
                    body = Encode_CSJOINGAME((CSJOINGAME)payload);
                    break;

                case PacketId.CS_LEAVE_GAME:
                    body = Encode_CSLEAVEGAME((CSLEAVEGAME)payload);
                    break;

                case PacketId.CS_MOVE:
                    body = Encode_CSMOVE((CSMOVE)payload);
                    break;

                case PacketId.SC_MOVE:
                    body = Encode_SCMOVE((SCMOVE)payload);
                    break;

                case PacketId.SC_SPAWN_PLAYER:
                    body = Encode_SCSPAWNPLAYER((SCSPAWNPLAYER)payload);
                    break;

                case PacketId.SC_DESPAWN_PLAYER:
                    body = Encode_SCDESPWNPLAYER((SCDESPWNPLAYER)payload);
                    break;

                case PacketId.SC_CHAT:
                    body = Encode_SCCHAT((SCCHAT)payload);
                    break;
                case PacketId.CS_CHAT:
                    body = Encode_CSCHAT((CSCHAT)payload);
                    break;
                case PacketId.SC_LOGIN_RESULT:
                    body = Encode_SCLOGINRESULT((SCLOGINRESULT)payload);
                    break;


                default:
                    throw new ArgumentException($"Unknown packet id in Encode: {id}");
            }

            ushort size = (ushort)(HeaderSize + body.Length);
            byte[] packet = new byte[size];

            // 헤더 추가
            Array.Copy(BitConverter.GetBytes(size), 0, packet, 0, 2);
            packet[2] = (byte)id;
            // 바디 추가
            Array.Copy(body, 0, packet, HeaderSize, body.Length);

            return packet;
        }

        public static object Decode(PacketId id, byte[] body)
        {
            switch (id)
            {
                case PacketId.CS_JOIN_GAME:
                    return Decode_CSJOINGAME(body);

                case PacketId.CS_LEAVE_GAME:
                    return Decode_CSLEAVEGAME(body);

                case PacketId.CS_MOVE:
                    return Decode_CSMOVE(body);

                case PacketId.SC_MOVE:
                    return Decode_SCMOVE(body);

                case PacketId.SC_SPAWN_PLAYER:
                    return Decode_SCSPAWNPLAYER(body);

                case PacketId.SC_DESPAWN_PLAYER:
                    return Decode_SCDESPWNPLAYER(body);

                case PacketId.CS_CHAT:
                    return Decode_CSCHAT(body);

                case PacketId.SC_CHAT:
                    return Decode_SCCHAT(body);
                case PacketId.SC_LOGIN_RESULT:
                    return Decode_SCLOGINRESULT(body);


                default:
                    throw new ArgumentException($"Unknown packet id in Decode: {id}");
            }
        }

        // Encode / Decode 함수 구현
        static byte[] EncodeString(string s)
        {
            s ??= "";
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            if (bytes.Length > ushort.MaxValue) throw new ArgumentException("string too long");

            byte[] body = new byte[2 + bytes.Length];
            body[0] = (byte)(bytes.Length & 0xFF);
            body[1] = (byte)((bytes.Length >> 8) & 0xFF);
            Buffer.BlockCopy(bytes, 0, body, 2, bytes.Length);
            return body;
        }

        static string DecodeString(byte[] body, ref int offset)
        {
            if (offset + 2 > body.Length) throw new ArgumentException("bad string header");
            ushort len = (ushort)(body[offset] | (body[offset + 1] << 8));
            offset += 2;
            if (offset + len > body.Length) throw new ArgumentException("bad string len");

            string s = Encoding.UTF8.GetString(body, offset, len);
            offset += len;
            return s;
        }

        private static byte[] Encode_CSJOINGAME(CSJOINGAME m)
        {
            return Encoding.UTF8.GetBytes(m.PlayerName ?? string.Empty);
        }

        private static CSJOINGAME Decode_CSJOINGAME(byte[] body)
        {
            CSJOINGAME m;
            m.PlayerName = Encoding.UTF8.GetString(body);
            return m;
        }

        private static byte[] Encode_CSLEAVEGAME(CSLEAVEGAME m)
        {
            return Array.Empty<byte>();
        }

        private static CSLEAVEGAME Decode_CSLEAVEGAME(byte[] body)
        {
            return new CSLEAVEGAME();
        }

        private static byte[] Encode_CSMOVE(CSMOVE m)
        {
            byte[] body = new byte[12];
            Array.Copy(BitConverter.GetBytes(m.X), 0, body, 0, 4);
            Array.Copy(BitConverter.GetBytes(m.Y), 0, body, 4, 4);
            Array.Copy(BitConverter.GetBytes(m.Z), 0, body, 8, 4);
            return body;
        }

        private static CSMOVE Decode_CSMOVE(byte[] body)
        {
            if (body.Length < 12)
                throw new ArgumentException("CS_MOVE body too short");

            CSMOVE m;
            m.X = BitConverter.ToSingle(body, 0);
            m.Y = BitConverter.ToSingle(body, 4);
            m.Z = BitConverter.ToSingle(body, 8);
            return m;
        }

        private static byte[] Encode_SCMOVE(SCMOVE m)
        {
            byte[] body = new byte[16];
            Array.Copy(BitConverter.GetBytes(m.PlayerId), 0, body, 0, 4);
            Array.Copy(BitConverter.GetBytes(m.X), 0, body, 4, 4);
            Array.Copy(BitConverter.GetBytes(m.Y), 0, body, 8, 4);
            Array.Copy(BitConverter.GetBytes(m.Z), 0, body, 12, 4);
            return body;
        }

        private static SCMOVE Decode_SCMOVE(byte[] body)
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

        private static byte[] Encode_SCSPAWNPLAYER(SCSPAWNPLAYER m)
        {
            byte[] body = new byte[16];
            Array.Copy(BitConverter.GetBytes(m.PlayerId), 0, body, 0, 4);
            Array.Copy(BitConverter.GetBytes(m.X), 0, body, 4, 4);
            Array.Copy(BitConverter.GetBytes(m.Y), 0, body, 8, 4);
            Array.Copy(BitConverter.GetBytes(m.Z), 0, body, 12, 4);
            return body;
        }

        private static SCSPAWNPLAYER Decode_SCSPAWNPLAYER(byte[] body)
        {
            if (body.Length < 16)
                throw new ArgumentException("SC_SPAWN_PLAYER body too short");

            SCSPAWNPLAYER m;
            m.PlayerId = BitConverter.ToInt32(body, 0);
            m.X = BitConverter.ToSingle(body, 4);
            m.Y = BitConverter.ToSingle(body, 8);
            m.Z = BitConverter.ToSingle(body, 12);
            return m;
        }

        private static byte[] Encode_SCDESPWNPLAYER(SCDESPWNPLAYER m)
        {
            byte[] body = new byte[4];
            Array.Copy(BitConverter.GetBytes(m.PlayerId), 0, body, 0, 4);
            return body;
        }

        private static SCDESPWNPLAYER Decode_SCDESPWNPLAYER(byte[] body)
        {
            if (body.Length < 4)
                throw new ArgumentException("SC_DESPAWN_PLAYER body too short");

            SCDESPWNPLAYER m;
            m.PlayerId = BitConverter.ToInt32(body, 0);
            return m;
        }

        private static byte[] Encode_CSCHAT(CSCHAT m)
        {
            return EncodeString(m.Msg);
        }

        private static CSCHAT Decode_CSCHAT(byte[] body)
        {
            int o = 0;
            CSCHAT m;
            m.Msg = DecodeString(body, ref o);
            return m;
        }

        private static byte[] Encode_SCCHAT(SCCHAT m)
        {
            byte[] msgPart = EncodeString(m.Msg);
            byte[] body = new byte[4 + msgPart.Length];

            Buffer.BlockCopy(BitConverter.GetBytes(m.PlayerId), 0, body, 0, 4);
            Buffer.BlockCopy(msgPart, 0, body, 4, msgPart.Length);
            return body;
        }

        private static SCCHAT Decode_SCCHAT(byte[] body)
        {
            if (body.Length < 6) throw new ArgumentException("SC_CHAT body too short");

            SCCHAT m;
            m.PlayerId = BitConverter.ToInt32(body, 0);

            int o = 4;
            m.Msg = DecodeString(body, ref o);
            return m;
        }

        private static byte[] Encode_SCLOGINRESULT(SCLOGINRESULT m)
        {
            byte[] body = new byte[16];
            Array.Copy(BitConverter.GetBytes(m.PlayerId), 0, body, 0, 4);
            Array.Copy(BitConverter.GetBytes(m.X), 0, body, 4, 4);
            Array.Copy(BitConverter.GetBytes(m.Y), 0, body, 8, 4);
            Array.Copy(BitConverter.GetBytes(m.Z), 0, body, 12, 4);
            return body;
        }

        private static SCLOGINRESULT Decode_SCLOGINRESULT(byte[] body)
        {
            if (body.Length < 16)
                throw new ArgumentException("SC_LOGIN_RESULT body too short");

            SCLOGINRESULT m;
            m.PlayerId = BitConverter.ToInt32(body, 0);
            m.X = BitConverter.ToSingle(body, 4);
            m.Y = BitConverter.ToSingle(body, 8);
            m.Z = BitConverter.ToSingle(body, 12);
            return m;
        }


    }
}