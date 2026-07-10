using ElectrostoreAPI.Dto;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class FileSizeAttribute : ValidationAttribute
{
    private readonly long _maxSizeInMB;

    public FileSizeAttribute(string maxSizePropertyName)
    {
        var maxSizeProperty = typeof(Constants).GetProperty(
            maxSizePropertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
        ) ?? throw new InvalidOperationException($"Field '{maxSizePropertyName}' not found in Constants class.");

        _maxSizeInMB = (long)Convert.ToInt64(maxSizeProperty.GetValue(null)!);
    }

    public override bool IsValid(object? value)
    {
        if (value is IFormFile file)
        {
            long maxFileSize = _maxSizeInMB * 1024 * 1024;
            if (file.Length > 0 && file.Length <= maxFileSize)
            {
                return true;
            }
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString, name, _maxSizeInMB);
    }
}