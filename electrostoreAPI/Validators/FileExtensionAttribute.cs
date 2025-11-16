using System.ComponentModel.DataAnnotations;

namespace electrostore.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class FileExtensionAttribute : ValidationAttribute
{
    private readonly string[] _allowedExtensions;
    public FileExtensionAttribute(string[] allowedExtensions)
    {
        _allowedExtensions = allowedExtensions;
    }
    public override bool IsValid(object? value)
    {
        if (value is IFormFile file &&_allowedExtensions.Contains(file.ContentType))
        {
            return true;
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return base.FormatErrorMessage(name);
    }
}