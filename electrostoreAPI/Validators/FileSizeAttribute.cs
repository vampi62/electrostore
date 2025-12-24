using System.ComponentModel.DataAnnotations;

namespace electrostore.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class FileSizeAttribute : ValidationAttribute
{
    private readonly long _maxSizeInMB;

    public FileSizeAttribute(long maxSizeInMB)
    {
        _maxSizeInMB = maxSizeInMB;
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