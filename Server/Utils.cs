class ConcurrentPriorityQueue<T>
{
    private readonly PriorityQueue<T, uint> _pq = new();
    private readonly object _lock = new();

    public void Enqueue(T item, uint due)
    {
        lock (_lock) _pq.Enqueue(item, due);
    }

    public bool TryDequeueReady(uint now, out T item)
    {
        lock (_lock)
        {
            if (!_pq.TryPeek(out item, out uint due))
            {
                item = default;
                return false;
            }

            if (due > now)
            {
                item = default;
                return false;
            }

            _pq.Dequeue();
            return true;
        }
    }
}
