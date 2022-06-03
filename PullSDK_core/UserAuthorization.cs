namespace PullSDK_core;

public class UserAuthorization
{
    public UserAuthorization(string pin, int zone, int doors)
    {
        Pin = pin;
        Timezone = zone;
        Doors = doors;
    }

    public string Pin { get; set; }
    public int Timezone { get; set; }
    public int Doors { get; set; }
}