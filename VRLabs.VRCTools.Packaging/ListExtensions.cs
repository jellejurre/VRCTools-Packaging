using System.Text.Json.Nodes;
using Serilog;

namespace VRLabs.VRCTools.Packaging;

public static class ListExtensions
{
    public static bool IsFieldNullOrEmpty(this ICollection<string> errorsList, JsonNode? package, string field, string errorMessage)
    {
        if (!string.IsNullOrEmpty(package?[field]?.ToString())) return false;
        errorsList.Add(errorMessage);
        return true;

    }
}