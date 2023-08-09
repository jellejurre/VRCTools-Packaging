using System.Text.Json.Serialization;
using FluentValidation;

namespace VRLabs.VRCTools.Packaging.JsonModels;

public class Author
{
    [JsonPropertyOrder(1)] public string Name { get; set; } = string.Empty;
    [JsonPropertyOrder(2)] public string Email { get; set; } = string.Empty;
    [JsonPropertyOrder(3)] public string Url { get; set; } = string.Empty;
}

public class AuthorValidator : AbstractValidator<Author>
{
    public AuthorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Author name cannot be empty.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Author email cannot be empty.");
        RuleFor(x => x.Url).NotEmpty().WithMessage("Author url cannot be empty.");
    }
}