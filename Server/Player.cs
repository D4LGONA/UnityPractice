public class Player
{
    public int Id { get; }
    public string Name { get; set; }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public bool InGame { get; set; }

    public Player(int id, string name)
    {
        Id = id;
        Name = name;
        
    }
    public void JoinGame()
    {
        InGame = true;
        X = 0;
        Y = 0;
        Z = 0;
    }
}
