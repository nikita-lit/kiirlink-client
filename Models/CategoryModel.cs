using System.Text.Json.Serialization;

namespace KiirLink.Models;

public class CategoryModel
{
    [JsonPropertyName( "id" )] public int Id { get; set; }

    [JsonPropertyName( "name" )] public string Name { get; set; } = string.Empty;
}