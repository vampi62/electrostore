using System.ComponentModel.DataAnnotations;
using System.Reflection;
using electrostore.Dto;

namespace electrostore.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class FileTypeAttribute : ValidationAttribute
{
    private readonly string _mimeTypesPropertyName;
    public FileTypeAttribute(string mimeTypesPropertyName)
    {
        _mimeTypesPropertyName = mimeTypesPropertyName;
    }
    public override bool IsValid(object? value)
    {
        var mimeTypesField = typeof(MimeTypes).GetField(
                _mimeTypesPropertyName,
                BindingFlags.Public | BindingFlags.Static
            );
            
        if (mimeTypesField == null)
        {
            return false;
        }
        var allowedMimeTypes = (string[])mimeTypesField.GetValue(null)!;

        if (value is IFormFile file && allowedMimeTypes.Contains(file.ContentType))
        {
            return true;
        }
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The file type of {name} is not allowed.";
    }
}