using System.Text.Json.Serialization;
using FluentValidation;

namespace VRLabs.VRCTools.Packaging.JsonModels;

public class VrlPackageJson : VccPackageJson
{
    [JsonPropertyOrder(13)] public string UnityPackageDestinationFolder { get; set; } = string.Empty;
    [JsonPropertyOrder(14)] public Dictionary<string, string>? UnitypackageDestinationFolderMetas { get; set; }
    [JsonPropertyOrder(15)] public string? VccRepoTag { get; set; }
}

public class VrlPackageJsonValidator : AbstractValidator<VrlPackageJson>
{
    public VrlPackageJsonValidator()
    {
        RuleFor(x => x).SetValidator(new VccPackageJsonValidator());
        RuleFor(x => x.UnityPackageDestinationFolder).NotEmpty().WithMessage("Unity package destination folder cannot be empty.");
    }
}