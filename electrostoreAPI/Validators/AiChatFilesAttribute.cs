using ElectrostoreAPI.Constants;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Validators;

public enum AiChatFileKind
{
    Unsupported,
    Image,
    Text
}

public static class AiChatFileClassifier
{
    public static AiChatFileKind Classify(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (FieldLengths.AllowedAiTextExtensions.Contains(extension))
        {
            return AiChatFileKind.Text;
        }
        var mimeType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        if (FieldLengths.AllowedImageMimeTypes.ContainsKey(mimeType) && FieldLengths.AllowedImageMimeTypes.Values.Contains(extension))
        {
            return AiChatFileKind.Image;
        }
        return AiChatFileKind.Unsupported;
    }

    public static int MaxSizeMB(AiChatFileKind kind) => kind == AiChatFileKind.Image ? FieldLengths.MaxImageSizeMB : FieldLengths.MaxDocumentSizeMB;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class AiChatFilesAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }
        if (value is not IReadOnlyCollection<IFormFile> files)
        {
            return new ValidationResult($"{validationContext.DisplayName} must be a list of files.");
        }
        if (files.Count > FieldLengths.MaxAiChatFileCount)
        {
            return new ValidationResult($"{validationContext.DisplayName} cannot contain more than {FieldLengths.MaxAiChatFileCount} files.");
        }
        foreach (var file in files)
        {
            var kind = AiChatFileClassifier.Classify(file);
            if (kind == AiChatFileKind.Unsupported)
            {
                return new ValidationResult(
                    $"{validationContext.DisplayName}: '{file.FileName}' has an invalid file type, allowed extensions are: " +
                    $"[{string.Join(", ", FieldLengths.AllowedAiTextExtensions.Concat(FieldLengths.AllowedImageMimeTypes.Values.Distinct()))}].");
            }
            var maxSizeMB = AiChatFileClassifier.MaxSizeMB(kind);
            if (file.Length <= 0 || file.Length > maxSizeMB * 1024L * 1024L)
            {
                return new ValidationResult($"{validationContext.DisplayName}: '{file.FileName}' must not be empty and cannot exceed {maxSizeMB} MB in size.");
            }
        }
        return ValidationResult.Success;
    }
}
