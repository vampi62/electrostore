using System.ComponentModel.DataAnnotations;
using System.Reflection;
using electrostore.Dto;
using System.Collections.Immutable;

namespace electrostore.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class FileTypeAttribute : ValidationAttribute
{
    private readonly string[] _allowedMimeTypes;
    private readonly string[] _allowedExtensions;
    public FileTypeAttribute(string mimeTypesPropertyName)
    {
        var _mimeTypesField = (typeof(Constants).GetField(
                mimeTypesPropertyName,
                BindingFlags.Public | BindingFlags.Static
            ) ?? null) ?? throw new InvalidOperationException($"Field '{mimeTypesPropertyName}' not found in Constants class.");
        _allowedMimeTypes = [.. ((ImmutableDictionary<string, string>)_mimeTypesField.GetValue(null)!).Keys];
        _allowedExtensions = [.. ((ImmutableDictionary<string, string>)_mimeTypesField.GetValue(null)!).Values];
    }
    public override bool IsValid(object? value)
    {
        if (value is IFormFile file && _allowedMimeTypes.Contains(file.ContentType) && _allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
        {
            return true;
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString, name, string.Join(", ", _allowedMimeTypes), string.Join(", ", _allowedExtensions));
    }
}