using System;
using System.Globalization;

namespace Domium.Querying.Internal;

/// <summary>
/// Converts raw filter strings into the target property type. Covers the types
/// <c>Convert.ChangeType</c> cannot handle (Guid, enums, DateTimeOffset, TimeSpan) before
/// falling back to it for primitives, and always uses the invariant culture.
/// </summary>
internal static class FilterValueParser
{
    public static object Parse(string raw, Type propertyType, string fieldName)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        try
        {
            if (targetType == typeof(string))
            {
                return raw;
            }

            if (targetType == typeof(Guid))
            {
                return Guid.Parse(raw);
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, raw, ignoreCase: true);
            }

            if (targetType == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }

            if (targetType == typeof(DateTime))
            {
                return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }

            if (targetType == typeof(TimeSpan))
            {
                return TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (
            exception is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new ArgumentException(
                $"Value '{raw}' for field '{fieldName}' is not a valid {targetType.Name}.");
        }
    }
}
