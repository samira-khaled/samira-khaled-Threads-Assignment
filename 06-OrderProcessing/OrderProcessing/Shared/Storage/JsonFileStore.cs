using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using OrderProcessing.Shared.Exceptions;

namespace OrderProcessing.Shared.Storage;

public sealed class JsonFileStore
{
    // Each file has its own lock.
    // Async and sync operations use the same lock.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static SemaphoreSlim GetGate(string path) =>
        _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

    private static string PathOf(string fileName) =>
        Path.Combine(DataFiles.DataFolder, fileName);
    private async Task<List<T>> ReadCoreAsync<T>(string fileName,CancellationToken cancellationToken)
    {
        var path = PathOf(fileName);

        if (!File.Exists(path))
        {
            return new List<T>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(path,cancellationToken);
            return JsonSerializer.Deserialize<List<T>>(json,JsonSettings.Options) ?? new List<T>();
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptedException(fileName,ex);
        }
    }
    public async Task<List<T>> ReadAsync<T>(string fileName,CancellationToken cancellationToken = default)
    {
        var gate = GetGate(fileName);//هتعملى سيمافور لكل ملف على حسب الى هيخش 

        await gate.WaitAsync(cancellationToken);// هيقف لو فى واحده شغاله
        try
        {
            return await ReadCoreAsync<T>(fileName, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }
    private async Task WriteCoreAsync<T>(string fileName,List<T> records,CancellationToken cancellationToken)
    {
        var path = PathOf(fileName);

        Directory.CreateDirectory(DataFiles.DataFolder);// هعمل فايل عشان اكتب فيه الى جاى من الاوبجكت

        var json = JsonSerializer.Serialize( records, JsonSettings.Options);// حولت هنا 

        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";//عملت ملف مؤقت عشان لو الكتابه باظت فى النص الاصل ميروحش 
        try
        {
            await File.WriteAllTextAsync(tempPath,json,Encoding.UTF8,cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();// معملش كانسل للطلب فى نص الكتابه 
            File.Move(tempPath, path, overwrite: true);// نقلت المؤقت فى الاصل
        }
        finally
        {
            if (File.Exists(tempPath))// لو مش موجود يبقى نقلتو للاصل ف امسح 
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task WriteAsync<T>(
        string fileName,
        List<T> records,
        CancellationToken cancellationToken = default)
    {
        var gate = GetGate(fileName);

        await gate.WaitAsync(cancellationToken);

        try
        {
            await WriteCoreAsync(fileName, records, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    // Protects the complete Read -> Modify -> Write operation.
    public async Task UpdateAsync<T>(string fileName,Action<List<T>> update,CancellationToken cancellationToken = default)
    {
        var gate = GetGate(fileName);

        await gate.WaitAsync(cancellationToken);

        try
        {
            var records = await ReadCoreAsync<T>(fileName, cancellationToken);

            update(records);

            await WriteCoreAsync(fileName, records, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }


    private List<T> ReadCore<T>(string fileName)
    {
        var path = PathOf(fileName);

        if (!File.Exists(path))
        {
            return new List<T>();
        }

        try
        {
            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<List<T>>(json,JsonSettings.Options) ?? new List<T>();
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptedException(fileName,ex);
        }
    }

    public List<T> Read<T>(string fileName)
    {
        var gate = GetGate(fileName);

        gate.Wait();
        try
        {
            return ReadCore<T>(fileName);
        }
        finally
        {
            gate.Release();
        }
    }
    private void WriteCore<T>(string fileName, List<T> records)
    {
        var path = PathOf(fileName);

        Directory.CreateDirectory(DataFiles.DataFolder);

        var json = JsonSerializer.Serialize(records, JsonSettings.Options);

        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public void Write<T>(string fileName, List<T> records)
    {
        var gate = GetGate(fileName);

        gate.Wait();

        try
        {
            WriteCore(fileName, records);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Update<T>(string fileName,Action<List<T>> update)
    {
        var gate = GetGate(fileName);

        gate.Wait();

        try
        {
            var records = ReadCore<T>(fileName);

            update(records);

            WriteCore(fileName, records);
        }
        finally
        {
            gate.Release();
        }
    }
}
