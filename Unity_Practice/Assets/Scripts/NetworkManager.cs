using GameClient; // Protocol, PacketId, CsMove
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkClient : MonoBehaviour
{
    private TcpClient _tcpClient;
    private NetworkStream _stream;

    public string ServerIp = "127.0.0.1";
    public int ServerPort = 7777;

    private async void Start()
    {
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(ServerIp, ServerPort);
        _stream = _tcpClient.GetStream();

        Debug.Log("Connected to server");

        // 로그인 한 번
        byte[] loginPacket = Protocol.Encode(PacketId.CS_LOGIN, "UnityPlayer");
        await _stream.WriteAsync(loginPacket, 0, loginPacket.Length);

        // 서버 수신 루프
        _ = ReceiveLoop();

        StartCoroutine(SendMoveLoop());
    }

    private System.Collections.IEnumerator SendMoveLoop()
    {
        while (true)
        {
            if (_stream != null)
            {
                Vector3 pos = transform.position;

                CSMOVE move = new CSMOVE { X = pos.x, Y = pos.y, Z = pos.z }; // 3D면 x,z 쓰거나, x,y 써도 되고
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

                ushort size = System.BitConverter.ToUInt16(sizeBuf, 0);
                if (size < Protocol.HeaderSize) break;

                byte[] packetBuf = await ReadExactAsync(size - 2);
                if (packetBuf == null) break;

                PacketId id = (PacketId)packetBuf[0];

                byte[] body = new byte[packetBuf.Length - 1];
                System.Array.Copy(packetBuf, 1, body, 0, body.Length);

                object msg = Protocol.Decode(id, body);

                // TODO: SC_MOVE/SC_CHAT 등 처리
                Debug.Log($"[SERVER] {id} : {msg}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
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
