using GameClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkClient : MonoBehaviour
{
    private TcpClient _tcpClient;
    private NetworkStream _stream;

    public string ServerIp = "127.0.0.1";
    public int ServerPort = 7777;

    [Header("Player Prefab")]
    public GameObject remotePrefab;

    public GameObject MyPlayer;

    private readonly ConcurrentQueue<Action> _mainJobs = new ConcurrentQueue<Action>(); // 작업 큐
    private readonly Dictionary<int, RemotePlayerMove> _remotes = new Dictionary<int, RemotePlayerMove>(); // 현재 플레이어들

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        while (_mainJobs.TryDequeue(out var job)) // 큐에서 하나 꺼내서 job에 넣기 시도
            job?.Invoke(); // 그 job 실행
    }

    private async void Start()
    {
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(ServerIp, ServerPort);
        _stream = _tcpClient.GetStream();

        // join.
        byte[] loginPacket = Protocol.Encode(PacketId.CS_JOIN_GAME, new CSJOINGAME { PlayerName= "UnityPlayer" });
        await _stream.WriteAsync(loginPacket, 0, loginPacket.Length);

        _ = ReceiveLoop();
        StartCoroutine(SendMoveLoop());

    }
    private System.Collections.IEnumerator SendMoveLoop()
    {
        while (true)
        {
            if (_stream != null)
            {
                Vector3 pos = MyPlayer.transform.position;

                CSMOVE move = new CSMOVE { X = pos.x, Y = pos.y, Z = pos.z };
                byte[] packet = Protocol.Encode(PacketId.CS_MOVE, move);
                _stream.Write(packet, 0, packet.Length); // 간단히 동기 write (테스트용)

                Debug.Log($"[CS_MOVE] sent: ({move.X}, {move.Y}, {move.Z})");
            }

            // 1초 대기
            yield return new WaitForSeconds(1.0f);
        }
    }


    private async Task ReceiveLoop()
    {
        try
        {
            while (true)
            {
                byte[] sizeBuf = await ReadExactAsync(2);
                if (sizeBuf == null) break;

                ushort size = BitConverter.ToUInt16(sizeBuf, 0);
                byte[] packetBuf = await ReadExactAsync(size - 2);
                if (packetBuf == null) break;

                PacketId id = (PacketId)packetBuf[0];
                byte[] body = new byte[packetBuf.Length - 1];
                Array.Copy(packetBuf, 1, body, 0, body.Length);

                object msg = Protocol.Decode(id, body);

                switch(id) // 여기가 process packet
                {
                    case PacketId.SC_MOVE:
                    {
                        var m = (SCMOVE)msg; // PlayerId, X,Y,Z 있다고 가정

                        _mainJobs.Enqueue(() =>
                        {
                            ApplyRemoteMove(m.PlayerId, new Vector3(m.X, m.Y, m.Z));
                        });
                        break;
                    }
                    case PacketId.SC_SPAWN_PLAYER:
                    {
                        var m = (SCSPAWNPLAYER)msg; // PlayerId, X,Y,Z 있다고 가정
                        _mainJobs.Enqueue(() =>
                        {
                            ApplyAddPlayer(m.PlayerId, new Vector3(m.X, m.Y, m.Z));
                        });
                        break;
                    }
                    case PacketId.SC_DESPAWN_PLAYER:
                    {
                        var m = (SCDESPWNPLAYER)msg;
                        _mainJobs.Enqueue(() =>
                        {
                            ApplyRemovePlayer(m.PlayerId);
                        });
                        break;
                    }

                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void ApplyRemoteMove(int playerId, Vector3 pos)
    {
        if (!_remotes.TryGetValue(playerId, out var remote))
        {
            var go = Instantiate(remotePrefab);
            remote = go.GetComponent<RemotePlayerMove>();
            go.transform.position = pos;   
            _remotes[playerId] = remote;
        }

        remote.ApplyServerState(pos);
    }

    private void ApplyAddPlayer(int playerId, Vector3 pos)
    {
        if(remotePrefab == null)
        {
            Debug.LogError("remotePrefab is null!");
            return;
        }

        // 스폰
        GameObject go = Instantiate(remotePrefab, pos, Quaternion.identity);
        go.name = $"RemotePlayer_{playerId}";

        var remote = go.GetComponent<RemotePlayerMove>();
        if (remote == null)
        {
            Debug.LogError("RemotePlayerMove component missing on remotePrefab!");
            Destroy(go);
            return;
        }

        _remotes[playerId] = remote;
    }

    private void ApplyRemovePlayer(int playerId)
    {
        if (_remotes.TryGetValue(playerId, out var remote) && remote != null)
            Destroy(remote.gameObject);

        _remotes.Remove(playerId);
    }

    private void OnDestroy()
    {
        if (_stream == null || _tcpClient == null || !_tcpClient.Connected)
            return;

        byte[] leavePacket = Protocol.Encode(PacketId.CS_LEAVE_GAME, new CSLEAVEGAME());
        _stream.Write(leavePacket, 0, leavePacket.Length);
        _stream.Flush();

        _stream.Close();
        _tcpClient.Close();
    }

    private async Task<byte[]> ReadExactAsync(int size)
    {
        byte[] buf = new byte[size];
        int offset = 0;
        while (offset < size)
        {
            int read = await _stream.ReadAsync(buf, offset, size - offset);
            if (read <= 0) return null;
            offset += read;
        }
        return buf;
    }
}
