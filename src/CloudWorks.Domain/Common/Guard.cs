using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Domain.Common;

internal static class Guard
{
    public static string RequireText<TEntity>(string? value, string propertyName)
    {
        var trimmed = value?.Trim();
        var attributes = typeof(TEntity)
            .GetProperty(propertyName)!
            .GetCustomAttributes<ValidationAttribute>();
        var context = new ValidationContext(new object())
        {
            MemberName = propertyName,
            DisplayName = propertyName,
        };
        var failures = new List<ValidationResult>();

        return Validator.TryValidateValue(trimmed, context, failures, attributes)
            ? trimmed!
            : throw new DomainRuleException(failures[0].ErrorMessage!);
    }

    public static Guid RequireId(Guid value, string description) =>
        value == Guid.Empty ? throw new DomainRuleException($"{description} is required.") : value;

    public static TEnum RequireDefined<TEnum>(TEnum value, string description)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value)
            ? value
            : throw new DomainRuleException($"{description} is not valid.");

    public static DateTimeOffset RequireUtc(DateTimeOffset value, string description) =>
        value.Offset == TimeSpan.Zero
            ? value
            : throw new DomainRuleException($"{description} must be in UTC.");
}
