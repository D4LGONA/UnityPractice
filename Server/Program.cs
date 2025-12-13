using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PacketProtocol;

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
        public async Task BroadcastMoveAsync(int fromSessionId, CSMOVE move)
        {
            var scMove = new SCMOVE
            {
                PlayerId = fromSessionId,
                X = move.X,
                Y = move.Y,
                Z = move.Z
            };

            // 여기서 내 위치 저장
            _sessions[fromSessionId].Player.X = move.X;
            _sessions[fromSessionId].Player.Y = move.Y;
            _sessions[fromSessionId].Player.Z = move.Z;

            foreach (var kv in _sessions)
            {
                ClientSession session = kv.Value;
                if (false == kv.Value.Player.InGame) continue;
                if (kv.Key == fromSessionId) 
                    continue;
                await session.SendMoveAsync(scMove);
            }
        }
        public async Task BroadcastDespawnAsync(int SessionId)
        {
            var scDespawn = new SCDESPWNPLAYER
            {
                PlayerId = SessionId,
            };

            foreach (var kv in _sessions)
            {
                ClientSession session = kv.Value;
                if (false == kv.Value.Player.InGame) continue;
                if (kv.Key == SessionId) continue;
                await session.SendDespawnAsync(scDespawn);
            }

            RemoveSession(SessionId);
        }
        public async Task BroadcastSpawnAsync(ClientSession newSession)
        {
            var newPlayer = newSession.Player;
            // 1) 기존 플레이어들을 새로 접속한 클라이언트에게 전송
            foreach (var kv in _sessions)
            {
                ClientSession session = kv.Value;
                if (session.Player.InGame == false) continue;
                if (session == newSession) continue;
                var scSpawn = new SCSPAWNPLAYER
                {
                    PlayerId = session.Player.Id,
                    X = session.Player.X,
                    Y = session.Player.Y,
                    Z = session.Player.Z
                };
                await newSession.SendSpawnAsync(scSpawn);
            }
            // 2) 새로 접속한 플레이어를 기존 클라이언트들에게 전송
            var scSpawnNew = new SCSPAWNPLAYER
            {
                PlayerId = newPlayer.Id,
                X = newPlayer.X,
                Y = newPlayer.Y,
                Z = newPlayer.Z
            };
            foreach (var kv in _sessions)
            {
                ClientSession session = kv.Value;
                if (session.Player.InGame == false) continue;
                if (session == newSession) continue;
                await session.SendSpawnAsync(scSpawnNew);
            }
        }

        public async Task BroadcastChatting(int fromSessionID, CSCHAT chat)
        {
            var scChat = new SCCHAT
            {
                PlayerId = fromSessionID,
                Msg = chat.Msg
            };
            foreach (var kv in _sessions)
            {
                ClientSession session = kv.Value;
                if (false == kv.Value.Player.InGame) continue;
                if (kv.Key == fromSessionID) continue;
                await session.SendChatAsync(scChat);
            }
        }
    }

    public class ClientSession
    {
        private readonly int _sessionId;
        private readonly TcpClient _client;
        private readonly Server _server;
        private readonly NetworkStream _stream;

        public Player Player { get; private set; }

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
                        break;
                    }

                case PacketId.CS_JOIN_GAME:
                    {
                        string name = ((CSJOINGAME)msg).PlayerName;
                        Console.WriteLine($"[CS_JOIN_GAME] session:{_sessionId}, name:{name}");

                        Player = new Player( _sessionId, name );
                        Player.JoinGame(); // 초기 세팅
                        SCLOGINRESULT loginResult = new SCLOGINRESULT
                        {
                            PlayerId = _sessionId,
                            X = Player.X,
                            Y = Player.Y,
                            Z = Player.Z
                        };
                        await SendPacketAsync(PacketId.SC_LOGIN_RESULT, loginResult); // 로그인 결과 전송
                        await _server.BroadcastSpawnAsync(this); // 브로드캐스트

                        break;
                    }

                case PacketId.CS_LEAVE_GAME:
                    {
                        Console.WriteLine($"[CS_LEAVE_GAME] session:{_sessionId}");

                        await _server.BroadcastDespawnAsync(_sessionId);
                        break;
                    }

                case PacketId.CS_MOVE:
                    {
                        CSMOVE move = (CSMOVE)msg;
                        Console.WriteLine($"[CS_MOVE] {_sessionId} -> ({move.X}, {move.Y}, {move.Z})");

                        await _server.BroadcastMoveAsync(_sessionId, move);
                        break;
                    }

                case PacketId.CS_CHAT:
                    {
                        CSCHAT chat = (CSCHAT)msg;
                        Console.WriteLine($"[CS_CHAT] session:{_sessionId}, msg:{chat.Msg}");

                        await _server.BroadcastChatting(_sessionId, chat);
                        break;
                    }

                default:
                    Console.WriteLine($"Unknown packet id: {id}");
                    break;
            }
        }



        private async Task SendPacketAsync(PacketId id, object payload)
        {
            byte[] packet = Protocol.Encode(id, payload);
            await _stream.WriteAsync(packet, 0, packet.Length);
        }
        
        // 패킷 전송 메소드 ... 
        public Task SendMoveAsync(SCMOVE move)
        => SendPacketAsync(PacketId.SC_MOVE, move);

        public Task SendDespawnAsync(SCDESPWNPLAYER despawn)
        => SendPacketAsync(PacketId.SC_DESPAWN_PLAYER, despawn);

        public Task SendSpawnAsync(SCSPAWNPLAYER spawn)
        => SendPacketAsync(PacketId.SC_SPAWN_PLAYER, spawn);

        public Task SendChatAsync(SCCHAT chat)
        => SendPacketAsync(PacketId.SC_CHAT, chat);
    }
}
