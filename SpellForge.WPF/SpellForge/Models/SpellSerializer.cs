using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpellForge.Models;

public static class SpellSerializer
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(Spell spell) => JsonSerializer.Serialize(spell, _opts);

    public static Spell? Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<Spell>(json, _opts); }
        catch { return null; }
    }

    public static void SaveToFile(Spell spell, string path) =>
        File.WriteAllText(path, Serialize(spell));

    public static Spell? LoadFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        return Deserialize(File.ReadAllText(path));
    }
}
