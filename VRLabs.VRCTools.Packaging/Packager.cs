using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;
using Serilog;
using VRLabs.VRCTools.Packaging.JsonModels;

namespace VRLabs.VRCTools.Packaging;

public static class Packager
{
    public static async Task<bool> CreatePackage(string workingDirectory, string outputDirectory, string? releaseUrl = null, bool skipVcc = false, bool skipUnityPackage = false)
    {
        if(skipVcc && skipUnityPackage) return false;
        var data = GetPackageJson(workingDirectory);
        if (data == null) return false;
        Console.WriteLine("BB");
        string tempPath = Path.GetTempPath();
        tempPath += "/VRLabs/Packaging";
        if(Directory.Exists(tempPath)) DeleteDirectory(tempPath);
        Directory.CreateDirectory(tempPath);
        
        string? sha256String = null;
        data.ZipSHA256 = null;

        if (!skipVcc)
        {
            if (releaseUrl is not null)
                data.Url = releaseUrl;
            else
                data.Url = null;
            CopyDirectory(workingDirectory, tempPath, true, true);
            string outputFileName = $"{data.Name}-{data.Version}.zip";
            string outputFilePath = $"{outputDirectory}/{outputFileName}";

            string jsonPath = tempPath + "/package.json";
            if (File.Exists(jsonPath))
            {
                await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize<VccPackageJson>(data, new JsonSerializerOptions{ DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
                Log.Information("Zipping {WorkingDirectory}", tempPath);
                if(File.Exists(outputFilePath))
                    File.Delete(outputFilePath);
                ZipFile.CreateFromDirectory(tempPath, outputFilePath);
                Log.Information("Finished Zipping, available at {OutputFilePath}", outputFilePath);

                using var sha256 = SHA256.Create();
                await using (var stream = File.OpenRead(outputFilePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    sha256String = BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            DeleteDirectory(tempPath);
        }

        if (!skipUnityPackage)
        {
            CreateExtraFolders(tempPath, data.UnitypackageDestinationFolderMetas);

            CopyDirectory(workingDirectory, tempPath + "/" + data.UnityPackageDestinationFolder, true);

            string outputFileName = $"{data.Name}-{data.Version}.unitypackage";
            string outputFilePath = $"{outputDirectory}/{outputFileName}";
        
            string jsonPath = tempPath + "/" + data.UnityPackageDestinationFolder + "/package.json";
            if (File.Exists(jsonPath))
            {
                await File.WriteAllTextAsync(jsonPath,JsonSerializer.Serialize<VccPackageJson>(data, new JsonSerializerOptions{ DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
            }
            Log.Information("Packaging {WorkingDirectory}", tempPath);
            
            if(File.Exists(outputFilePath))
                File.Delete(outputFilePath);

            // Make the output file (touch it) so we can exclude
            await File.WriteAllBytesAsync(outputFilePath, Array.Empty<byte>());
            
            await using var packer = new unityPackagePacker(tempPath, outputFilePath);

            // Match all the assets we need
            var assetMatcher = new Matcher();
            assetMatcher.AddIncludePatterns(new[] { "**.*" });
            assetMatcher.AddExcludePatterns(new[] { "Library/**.*", "**/.*" });
            assetMatcher.AddExclude(outputFileName);

            IEnumerable<string> matchedAssets = assetMatcher.GetResultsInFullPath(tempPath);
            await packer.AddAssetsAsync(matchedAssets);
        
            // Finally flush and tell them we done
            await packer.FlushAsync();
            Log.Information("Finished Packaging, available at {OutputFilePath}", outputFilePath);
            DeleteDirectory(tempPath);
        }
        if(sha256String is not null)
            data.ZipSHA256 = sha256String;
        
        await File.WriteAllTextAsync($"{outputDirectory}/server-package.json", JsonSerializer.Serialize(data));
        Log.Information("Finished creating server-package.json, available at {OutputFilePath}", $"{outputDirectory}/server-package.json");
        
        return true;
    }
    
    private static VrlPackageJson? GetPackageJson(string workingDirectory)
    {
        string packagePath = workingDirectory + "/package.json";
        if (!File.Exists(packagePath)) return null;
        var package = JsonSerializer.Deserialize<VrlPackageJson>(File.ReadAllText(packagePath), new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        var validationResult = new VrlPackageJsonValidator().Validate(package!);
        
        if (!validationResult.IsValid)
        {
            Log.Error("package.json is invalid");
            foreach (var error in validationResult.Errors)
            {
                Log.Error(" -{ValidationError}",error.ErrorMessage);
            }
            return null;
        }
        
        return package;
    }

    private static void CreateExtraFolders(string path, Dictionary<string, string>? folders)
    {
        if (folders is null) return;
        foreach (var folder in folders)
        {
            Directory.CreateDirectory($"{path}/{folder.Key}");
            File.WriteAllText($"{path}/{folder.Key}.meta", "fileFormatVersion: 2\nguid: " + folder.Value + "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: ");
        }
    }
    
    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, bool ignoreDotPaths = false)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            if(ignoreDotPaths && file.Name.StartsWith(".")) continue;
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }
        
        if (!recursive) return;
        // If recursive and copying subdirectories, recursively call this method
        foreach (DirectoryInfo subDir in dirs)
        {
            if(ignoreDotPaths && subDir.Name.StartsWith(".")) continue;
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true, ignoreDotPaths);
        }
    }
    
    public static void DeleteDirectory(string targetDir)
    {
        File.SetAttributes(targetDir, FileAttributes.Normal);

        string[] files = Directory.GetFiles(targetDir);
        string[] dirs = Directory.GetDirectories(targetDir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }
        
        Directory.Delete(targetDir, false);
    }
}