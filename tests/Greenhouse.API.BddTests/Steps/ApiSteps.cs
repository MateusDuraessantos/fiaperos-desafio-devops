using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Greenhouse.API.BddTests.Support;
using Greenhouse.API.Data;
using Greenhouse.API.Models;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace Greenhouse.API.BddTests.Steps;

[Binding]
public sealed class ApiSteps(FeatureContext feature, ScenarioContext scenario, IReqnrollOutputHelper output)
{
    private readonly GreenhouseFactory factory = new();
    private HttpClient client = null!;
    private HttpResponseMessage response = null!;
    private string body = "";
    private readonly Dictionary<string, int> ids = new();
    private readonly List<HttpExchange> exchanges = new();
    private readonly DateTime started = DateTime.UtcNow;
    private int estufaCount;

    private sealed class HttpExchange
    {
        public required string method { get; init; }
        public required string path { get; init; }
        public required int status { get; init; }
        public required string body { get; init; }
        public bool statusValidated { get; set; }
        public string? contract { get; set; }
        public bool emptyBodyValidated { get; set; }
    }

    private static string ResultsDirectory => Environment.GetEnvironmentVariable("BDD_RESULTS_DIRECTORY")
        ?? Path.Combine(AppContext.BaseDirectory, "results");

    [BeforeScenario]
    public async Task Start()
    {
        output.WriteLine($"Funcionalidade: {feature.FeatureInfo.Title}");
        output.WriteLine($"Cenário: {scenario.ScenarioInfo.Title}");
        client = factory.CreateClient(new() { AllowAutoRedirect = false });
        output.WriteLine($"Banco isolado: {factory.Provider}");
        Directory.CreateDirectory(ResultsDirectory);
        // The coverage gate compares HTTP exchanges against the API's actual OpenAPI document.
        var openApi = await client.GetStringAsync("/swagger/v1/swagger.json");
        var snapshot = Path.Combine(ResultsDirectory, "openapi.json");
        try
        {
            using var stream = new FileStream(snapshot, FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(openApi);
        }
        catch (IOException) when (File.Exists(snapshot)) { }
    }

    [AfterScenario]
    public void Finish()
    {
        try
        {
            var directory = Path.Combine(ResultsDirectory, "http");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, Guid.NewGuid() + ".json"), JsonSerializer.Serialize(new
            {
                scenario = scenario.ScenarioInfo.Title,
                provider = factory.Provider,
                startedUtc = started,
                finishedUtc = DateTime.UtcNow,
                passed = scenario.TestError is null,
                exchanges
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            response?.Dispose();
            client?.Dispose();
            factory.Dispose();
        }
    }

    private string Expand(string text)
    {
        foreach (var (key, value) in ids)
            text = text.Replace("{" + key + "}", value.ToString(CultureInfo.InvariantCulture));
        return text;
    }

    [When("envio {string} para {string}")]
    public Task Send(string method, string path) => SendJson(method, path, null);

    [When("envio {string} para {string} com o JSON")]
    public async Task SendJson(string method, string path, string? json)
    {
        path = Expand(path);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (json is not null)
            request.Content = new StringContent(Expand(json), Encoding.UTF8, "application/json");
        response?.Dispose();
        response = await client.SendAsync(request);
        body = await response.Content.ReadAsStringAsync();
        // Never include the login password, authorization header or usable JWT in evidence.
        var safeBody = body;
        if (path == "/api/auth/login" && response.IsSuccessStatusCode)
        {
            var redacted = JsonNode.Parse(body)!;
            redacted["token"] = "[JWT omitido]";
            safeBody = redacted.ToJsonString();
        }
        exchanges.Add(new HttpExchange { method = method, path = path, status = (int)response.StatusCode, body = safeBody });
        output.WriteLine($"HTTP {method} {path} -> {(int)response.StatusCode}\n{safeBody}");
    }

    [Given("que estou autenticado")]
    public async Task Authenticate()
    {
        await SendJson("POST", "/api/auth/login", JsonSerializer.Serialize(new
        {
            username = GreenhouseFactory.Username,
            password = GreenhouseFactory.Password
        }));
        Status(200);
        Schema("token");
        UseToken();
    }

    [When("uso o token recebido")]
    public void UseToken() => client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", JsonNode.Parse(body)!["token"]!.GetValue<string>());

    [Given("que utilizo um token inválido")]
    public void InvalidToken() => client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", "token-invalido");

    [Given("que existe uma estufa cadastrada")]
    public Task OneEstufa() => Estufas(1);

    [Given("que existem {int} estufas cadastradas")]
    public async Task Estufas(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await SendJson("POST", "/api/estufas", JsonSerializer.Serialize(new
            {
                nome = $"Estufa BDD {++estufaCount}", localizacao = "São Paulo",
                area = 100, tipoCultivo = "Orgânico", ativa = true
            }));
            Status(201);
            Schema("estufa");
            ids["estufaId"] = JsonNode.Parse(body)!["id"]!.GetValue<int>();
        }
    }

    [Given("que existe um sensor do tipo {string}")]
    public async Task Sensor(string tipo)
    {
        await SendJson("POST", "/api/sensores", JsonSerializer.Serialize(new
        {
            estufaId = ids["estufaId"], tipo, valorAtual = 24.5, unidade = "C"
        }));
        Status(201);
        Schema("sensor");
        ids["sensorId"] = JsonNode.Parse(body)!["id"]!.GetValue<int>();
    }

    [Given("que existe um alerta cadastrado")]
    public async Task Alerta()
    {
        await SendJson("POST", "/api/alertas", JsonSerializer.Serialize(new
        {
            estufaId = ids["estufaId"], mensagem = "Verificar irrigação", tipo = "Umidade"
        }));
        Status(201);
        Schema("alerta");
        ids["alertaId"] = JsonNode.Parse(body)!["id"]!.GetValue<int>();
    }

    [Given("que há dados históricos com estufa inativa alerta resolvido e temperaturas 20 e 25.2")]
    public async Task HistoricalData()
    {
        // These states have no HTTP write endpoint. Seed only this historical fixture via EF.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Estufas.FindAsync(ids["estufaId"]))!.Ativa = false;
        (await db.Alertas.FindAsync(ids["alertaId"]))!.Resolvido = true;
        db.LeiturasAmbientais.AddRange(
            new LeituraAmbiental { EstufaId = ids["estufaId"], Temperatura = 20, Umidade = 60 },
            new LeituraAmbiental { EstufaId = ids["estufaId"], Temperatura = 25.2, Umidade = 60 });
        await db.SaveChangesAsync();
    }

    [Then("o status HTTP deve ser {int}")]
    public void Status(int expected)
    {
        Assert.Equal(expected, (int)response.StatusCode);
        exchanges[^1].statusValidated = true;
    }

    [Then("a resposta deve respeitar o contrato {string}")]
    public void Schema(string name)
    {
        Assert.Contains(response.Content.Headers.ContentType?.MediaType, new[] { "application/json", "application/problem+json" });
        Contract.Validate(name, body);
        exchanges[^1].contract = name;
        output.WriteLine($"JSON Schema validado: {name}");
    }

    private JsonNode Field(string path)
    {
        var node = JsonNode.Parse(body);
        foreach (var part in path.Split('.'))
            node = node is JsonArray array ? array[int.Parse(part, CultureInfo.InvariantCulture)] : node?[part];
        return node ?? throw new Xunit.Sdk.XunitException($"Campo JSON ausente: {path}");
    }

    [Then("^o campo JSON \"([^\"]+)\" deve ser '(.+)'$")]
    public void Value(string path, string expected)
    {
        var actual = Field(path);
        var desired = JsonNode.Parse(Expand(expected));
        // JSON numbers such as 100 and 100.0 have the same semantic value.
        if (actual is JsonValue number && number.TryGetValue<double>(out var a)
            && desired is JsonValue expectedNumber && expectedNumber.TryGetValue<double>(out var b))
            Assert.Equal(b, a, 6);
        else Assert.True(JsonNode.DeepEquals(desired, actual), $"{path}: esperado {expected}, obtido {actual.ToJsonString()}");
    }

    [Then("a coleção JSON {string} deve conter {int} item")]
    public void Count(string path, int count) => Assert.Equal(count, Assert.IsType<JsonArray>(Field(path)).Count);

    [Then("deve existir erro de validação para {string}")]
    public void Validation(string field) => Assert.NotEmpty(Assert.IsType<JsonArray>(Field("errors." + field)));

    [Then("o corpo deve estar vazio")]
    public void Empty()
    {
        Assert.Empty(body);
        exchanges[^1].emptyBodyValidated = true;
    }

    [Then("deve haver um desafio Bearer")]
    public void Challenge() => Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");

    [Then("o campo JSON {string} deve registrar o instante atual")]
    public void Recent(string field) => Assert.InRange(DateTime.Parse(Field(field).GetValue<string>(),
        CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal), started.AddSeconds(-2), DateTime.UtcNow.AddSeconds(2));

    [Then("o token deve identificar o administrador e expirar em uma hora")]
    public void TokenClaims()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(Field("token").GetValue<string>());
        Assert.Contains(token.Claims, claim => claim.Value == GreenhouseFactory.Username);
        Assert.Equal("Greenhouse.Bdd", token.Issuer);
        Assert.Contains("Greenhouse.Bdd.Client", token.Audiences);
        Assert.InRange(token.ValidTo, started.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
        Assert.InRange(DateTime.Parse(Field("expiration").GetValue<string>(), CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal), started.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Then("Location deve permitir consultar o mesmo recurso")]
    public async Task Location()
    {
        var original = JsonNode.Parse(body)!;
        var schema = response.RequestMessage!.RequestUri!.AbsolutePath switch
        {
            "/api/estufas" => "estufa", "/api/sensores" => "sensor", "/api/alertas" => "alerta",
            _ => throw new InvalidOperationException("Recurso não mapeado")
        };
        Assert.NotNull(response.Headers.Location);
        var path = response.Headers.Location!.IsAbsoluteUri
            ? response.Headers.Location.PathAndQuery : response.Headers.Location.ToString();
        await Send("GET", path);
        Status(200);
        Schema(schema);
        Assert.True(JsonNode.DeepEquals(original, JsonNode.Parse(body)), "GET Location deve retornar os dados criados.");
    }
}
