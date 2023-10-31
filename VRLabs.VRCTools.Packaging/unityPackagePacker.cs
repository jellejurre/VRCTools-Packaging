using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Serilog;

namespace VRLabs.VRCTools.Packaging;

/// <summary>Packs file into a Unity Package</summary>
public class unityPackagePacker : IDisposable, IAsyncDisposable
{
    /// <summary>Path to the Unity Project</summary>
    public string ProjectPath { get; }
    /// <summary>Output file path. If a stream is given, this is null.</summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? OutputPath { get; }

    private readonly Stream _outStream;
    private readonly GZipOutputStream _gzStream;
    private readonly TarOutputStream _tarStream;

    private readonly HashSet<string> _files;
    public IReadOnlyCollection<string> Files => _files;

    /// <summary>
    /// Creates a new Packer that writes to the output file
    /// </summary>
    /// <param name="projectPath">Path to the Unity Project</param>
    /// <param name="output">The .unitypackage file</param>
    public unityPackagePacker(string projectPath, string output) : this(projectPath, new FileStream(output, FileMode.OpenOrCreate))
    {
        OutputPath = output;
    }

    /// <summary>
    /// Creates a new Packer that writes to the outputStream
    /// </summary>
    /// <param name="projectPath">Path to the Unity Project</param>
    /// <param name="stream">The stream the contents will be written to</param>
    public unityPackagePacker(string projectPath, Stream stream)
    {
        ProjectPath = projectPath;
        OutputPath = null;

        _files = new HashSet<string>();
        _outStream = stream;
        _gzStream = new GZipOutputStream(_outStream);
        _tarStream = new TarOutputStream(_gzStream, Encoding.ASCII);

    }

    /// <summary>
    /// Adds an asset to the pack.
    /// <para>If the asset is already in the pack, then it will be skipped.</para>
    /// </summary>
    /// <param name="filePath">The full path to the asset</param>
    /// <returns>If the asset was written to the pack. </returns>
    public async Task<bool> AddAssetAsync(string filePath)
    {
        var file = new FileInfo(Path.GetExtension(filePath) == ".meta" ? filePath[..^5] : filePath);
        if (!file.Exists) return false;// throw new FileNotFoundException();
        if (!_files.Add(file.FullName)) return false;

        string relativePath = Path.GetRelativePath(ProjectPath, file.FullName);
        if(file.Name == ".icon.png")
        {
            Log.Information("Writing unityPackage Icon {Path}", relativePath);
            await _tarStream.WriteFileAsync(file.FullName, ".icon.png");
            return true;
        }
        
        string metaFile = $"{file.FullName}.meta";
        if (!File.Exists(metaFile))
        {
            //Meta file is missing so we will skip it.
            Log.Warning("Missing .meta for {Path}, skipping", relativePath);
            return false;
        }

        //Read the meta contents
        string metaContents = await File.ReadAllTextAsync(metaFile);

        int guidIndex = metaContents.IndexOf("guid: ", StringComparison.Ordinal);
        string guidString = metaContents.Substring(guidIndex + 6, 32);

        Log.Information("Writing File {Path} ( {Guid} )", relativePath, guidString);
        await _tarStream.WriteFileAsync(file.FullName, $"{guidString}/asset");
        await _tarStream.WriteAllTextAsync($"{guidString}/asset.meta", metaContents);
        await _tarStream.WriteAllTextAsync($"{guidString}/pathname", relativePath.Replace('\\', '/'));
        return true;
    }

    /// <summary>
    /// Adds assets to the pack
    /// <para>If an asset is already in the pack then it will be skipped</para>
    /// </summary>
    /// <param name="assets"></param>
    /// <returns></returns>
    public async Task AddAssetsAsync(IEnumerable<string> assets)
    {
        foreach (string asset in assets)
            await AddAssetAsync(asset);
    }

    public Task FlushAsync()
        => _tarStream.FlushAsync();

    public void Dispose()
    {
        _tarStream.Dispose();
        _gzStream.Dispose();
        _outStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _tarStream.DisposeAsync();
        await _gzStream.DisposeAsync();
        await _outStream.DisposeAsync();
    }
}