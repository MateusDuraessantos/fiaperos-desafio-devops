using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Xunit;

namespace Greenhouse.API.BddTests.Support;

public static class Contract
{
    private static readonly ConcurrentDictionary<string, JsonSchema> Schemas = new();

    public static void Validate(string name, string body)
    {
        var schema = Schemas.GetOrAdd(name, key => JsonSchema.FromFile(
            Path.Combine(AppContext.BaseDirectory, "Schemas", key + ".schema.json")));
        var result = schema.Evaluate(JsonNode.Parse(body), new EvaluationOptions
        {
            RequireFormatValidation = true,
            OutputFormat = OutputFormat.List
        });
        Assert.True(result.IsValid, $"Contrato {name} violado: {JsonSerializer.Serialize(result)}");
    }
}
