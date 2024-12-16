namespace ThetaStar.Scripts;

internal static class FloatExtension
{
    public static bool IsBetween(this float value, float min, float max) =>
        min <= value && value <= max || value.Equals(min) || value.Equals(max);
}