using Events;
using GameServer;

class Tree : IEventTarget
{
    public void OnEvent(EventType type)
    {
        switch (type)
        {
            case EventType.EVT_TREE_RESPAWN:
                break;
        }
    }

    private void Respawn()
    {
        GameServer.Server.
        // 리스폰 패킷을 보내자
    }
}