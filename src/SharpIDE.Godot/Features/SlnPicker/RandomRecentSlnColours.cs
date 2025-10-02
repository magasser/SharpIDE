using System.Security.Cryptography;
using System.Text;
using Godot;

namespace SharpIDE.Godot.Features.SlnPicker;

public static class RandomRecentSlnColours
{
    public static readonly Color Teal = new Color("24a0a7");
    public static readonly Color Purple = new Color("9b55e0");
    public static readonly Color Pink = new Color("bd4fa4");
    public static readonly Color Blue = new Color("5390ce");
    public static readonly Color LightGreen = new Color("8fa759");
    public static readonly Color Orange = new Color("e3855e");
    public static readonly Color Green = new Color("53a472");
    public static readonly Color DarkYellow = new Color("b48615");
    public static readonly Color DarkBlue = new Color("4e6ef0");
    
    public static readonly List<Color> AllColours =
    [
        Teal,
        Purple,
        Pink,
        Blue,
        LightGreen,
        Orange,
        Green,
        DarkYellow,
        DarkBlue
    ];
    
    public static Color GetColourForFilePath(string filePath)
    {
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(filePath));

        // Convert first 4 bytes to an int
        var hash = BitConverter.ToInt32(hashBytes, 0);

        var index = Math.Abs(hash) % AllColours.Count;
        return AllColours[index];
    }
}
