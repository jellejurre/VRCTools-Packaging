using System.CommandLine;
using Serilog;
using VRLabs.VRCTools.Packaging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
    
var pathArg = new Argument<string>(name: "path", description: "Unity Project Directory.");
var outputArg = new Argument<string>(name: "output", description: "Output directory for the packages");
var releaseUrlOpt = new Option<string?>(name: "--releaseUrl", getDefaultValue: () => null, description: "Url of the release");
var noVccOpt = new Option<bool>(name: "--novcc", getDefaultValue: () => false, description: "don't build the vcc zip file");
var noUnityOpt = new Option<bool>(name: "--nounity", getDefaultValue: () => false, description: "don't build the unitypackage");

var command = new RootCommand("Packs the assets inside a folder in a Unity Project based on an info file")
{
    pathArg,
    outputArg,
    releaseUrlOpt,
    noVccOpt,
    noUnityOpt
};

command.SetHandler(async (source, output, releaseUrl, noVcc, noUnity) =>
{
    var result = await Packager.CreatePackage(source, output, releaseUrl, noVcc, noUnity);
    
    if (!result)
    {
        Log.Error("Failed to create package");
        Environment.Exit(1);
    }
    
}, pathArg, outputArg, releaseUrlOpt, noVccOpt, noUnityOpt);

command.Invoke(args);