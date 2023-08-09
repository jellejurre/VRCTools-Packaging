using System.Text.Json.Serialization;
using FluentValidation;

namespace VRLabs.VRCTools.Packaging.JsonModels;

public class UnityPackageJson
{
    [JsonPropertyOrder(1)] public string Name { get; set; } = string.Empty;
    [JsonPropertyOrder(2)] public string DisplayName { get; set; } = string.Empty;
    [JsonPropertyOrder(3)] public string Version { get; set; } = string.Empty;
    [JsonPropertyOrder(4)] public string? License { get; set; }
    [JsonPropertyOrder(5)] public string Unity { get; set; } = string.Empty;
    [JsonPropertyOrder(6)] public string Description { get; set; } = string.Empty;
    [JsonPropertyOrder(7)] public Author Author { get; set; } = new();
}

public class UnityPackageJsonValidator : AbstractValidator<UnityPackageJson>
{
    public UnityPackageJsonValidator()
    {
        RuleFor(x => x).NotNull().WithMessage("package.json cannot be null.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Package name cannot be empty.");
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Display name cannot be empty.");
        RuleFor(x => x.Version).NotEmpty().WithMessage("Version cannot be empty.");
        RuleFor(x => x.Unity).NotEmpty().WithMessage("Unity version cannot be empty.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description cannot be empty.");
        RuleFor(x => x.Author).NotNull().WithMessage("Author cannot be null.");
    }
}