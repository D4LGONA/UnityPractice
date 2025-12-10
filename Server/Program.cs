using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("C# Game Server Start");

            var server = new Server(IPAddress.Any, 7777);
            await server.StartAsync();
        }
    }

    public class Server
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<int, ClientSession> _sessions = new();
        private int _sessionIdGen = 0;

        public Server(IPAddress ip, int port)
        {
            _listener = new TcpListener(ip, port);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("Listening on " + _listener.LocalEndpoint);

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                client.NoDelay = true; // Nagle 끄기 (게임용)

                int sessionId = ++_sessionIdGen;
                var session = new ClientSession(sessionId, client, this);
                _sessions[sessionId] = session;

                Console.WriteLine($"Client {sessionId} connected");

                _ = session.RunAsync(); // 세션 루프 비동기 실행
            }
        }

        public void RemoveSession(int sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
            Console.WriteLine($"Client {sessionId} removed");
        }
    }

    public class ClientSession
    {
        private readonly int _sessionId;
        private readonly TcpClient _client;
        private readonly Server _server;
        private readonly NetworkStream _stream;

        public ClientSession(int sessionId, TcpClient client, Server server)
        {
            _sessionId = sessionId;
            _client = client;
            _server = server;
            _stream = client.GetStream();
        }

        public async Task RunAsync()
        {
            try
            {
                while (true)
                {
                    // 1) 길이(2바이트) 읽기
                    byte[] sizeBuf = await ReadExactAsync(2);
                    if (sizeBuf == null) break;

                    ushort size = BitConverter.ToUInt16(sizeBuf, 0);
                    if (size < Protocol.HeaderSize) // 최소: size(2) + id(1)
                        break;

                    // 2) 나머지 (size-2) 바이트 읽기 (id + body)
                    byte[] packetBuf = await ReadExactAsync(size - 2);
                    if (packetBuf == null) break;

                    // id (1바이트)
                    PacketId packetId = (PacketId)packetBuf[0];

                    // body
                    byte[] body = new byte[packetBuf.Length - 1];
                    Array.Copy(packetBuf, 1, body, 0, body.Length);

                    object msg = Protocol.Decode(packetId, body);

                    await HandlePacketAsync(packetId, msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Session {_sessionId} error: {ex}");
            }
            finally
            {
                Console.WriteLine($"Client {_sessionId} disconnected");
                _client.Close();
                _server.RemoveSession(_sessionId);
            }
        }

        // 정확히 N바이트 읽기
        private async Task<byte[]> ReadExactAsync(int size)
        {
            byte[] buf = new byte[size];
            int offset = 0;

            while (offset < size)
            {
                int read = await _stream.ReadAsync(buf, offset, size - offset);
                if (read <= 0)
                    return null; // 연결 끊김

                offset += read;
            }

            return buf;
        }

        // 패킷 처리: 이제 body 대신 object로 받음
        private async Task HandlePacketAsync(PacketId id, object msg)
        {
            switch (id)
            {
                case PacketId.CS_LOGIN:
                    {
                        string name = (string)msg;
                        Console.WriteLine($"[CS_LOGIN] session:{_sessionId}, name:{name}");
                        // TODO: 로그인 처리 + SC_LOGIN_RESULT 보내기
                        break;
                    }

                case PacketId.CS_CHAT:
                    {
                        string chat = (string)msg;
                        Console.WriteLine($"[CS_CHAT] {_sessionId}: {chat}");
                        // TODO: 나중에 Server.BroadcastChat 같은 거 호출
                        break;
                    }

                case PacketId.CS_MOVE:
                    {
                        CSMOVE move = (CSMOVE)msg;
                        Console.WriteLine($"[CS_MOVE] {_sessionId} -> ({move.X}, {move.Y}, {move.Z})");
                        break;
                    }

                default:
                    Console.WriteLine($"Unknown packet id: {id}");
                    break;
            }
        }

        // 공통: 패킷 전송 — 이제 body 대신 payload(object)를 넣으면 됨
        private async Task SendPacketAsync(PacketId id, object payload)
        {
            byte[] packet = Protocol.Encode(id, payload);
            await _stream.WriteAsync(packet, 0, packet.Length);
        }
    }
}
