using System.Text.Json.Serialization;
using FluentValidation;

namespace VRLabs.VRCTools.Packaging.JsonModels;

public class VccPackageJson : UnityPackageJson
{
    [JsonPropertyOrder(8)] public Dictionary<string, string> VpmDependencies { get; set; } = new();
    [JsonPropertyOrder(9)] public string? Url { get; set; }
    // ReSharper disable once InconsistentNaming
    [JsonPropertyOrder(10)] public Dictionary<string, string> LegacyFolders { get; set; } = new();
    [JsonPropertyOrder(11)] public string? ZipSHA256 { get; set; }
    [JsonPropertyOrder(12)] public string? ChangelogUrl { get; set; }
}

public class VccPackageJsonValidator : AbstractValidator<VccPackageJson>
{
    public VccPackageJsonValidator()
    {
        RuleFor(x => x).SetValidator(new UnityPackageJsonValidator());
        RuleFor(x => x.VpmDependencies).NotNull().WithMessage("VPM dependencies array cannot be null.");
    }
}