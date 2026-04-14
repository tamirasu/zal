using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class OrderRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Statyczny serializer — XmlSerializer ma cache, tworzymy raz
    private static readonly XmlSerializer XmlSer =
        new(typeof(List<Order>), new XmlRootAttribute("orders"));

    // ─── JSON ────────────────────────────────────────────────────────────────

    public async Task SaveToJsonAsync(IEnumerable<Order> orders, string path)
    {
        EnsureDir(path);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, useAsync: true);
        await JsonSerializer.SerializeAsync(stream, orders.ToList(), JsonOpts);
    }

    public async Task<List<Order>> LoadFromJsonAsync(string path)
    {
        if (!File.Exists(path))
            return new List<Order>();

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        return await JsonSerializer.DeserializeAsync<List<Order>>(stream, JsonOpts)
               ?? new List<Order>();
    }

    // ─── XML ─────────────────────────────────────────────────────────────────

    public async Task SaveToXmlAsync(IEnumerable<Order> orders, string path)
    {
        EnsureDir(path);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, useAsync: true);

        // XmlSerializer jest synchroniczny — piszemy przez XmlWriter z opcją Async
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Async = true });
        XmlSer.Serialize(writer, orders.ToList());
        await writer.FlushAsync();
    }

    public async Task<List<Order>> LoadFromXmlAsync(string path)
    {
        if (!File.Exists(path))
            return new List<Order>();

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        using var reader = XmlReader.Create(stream);
        return (List<Order>)XmlSer.Deserialize(reader)!;
    }

    private static void EnsureDir(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}
