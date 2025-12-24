using System.ComponentModel.DataAnnotations;

namespace electrostore.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class OptionalNotEmptyAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true;
        }
        if (value is string strValue)
        {
            return !string.IsNullOrWhiteSpace(strValue);
        }
        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(ErrorMessageString, name);
    }
}
