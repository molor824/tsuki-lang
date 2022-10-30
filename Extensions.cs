public static class Extensions
{
    public static bool TryGet(this string a, int index, out char result)
    {
        if (index < 0 || index >= a.Length)
        {
            result = default;
            return false;
        }

        result = a[index];
        return true;
    }
    public static bool TryGet<T>(this ReadOnlySpan<T> a, int index, out T? result)
    {
        if (index < 0 || index >= a.Length)
        {
            result = default;
            return false;
        }

        result = a[index];
        return true;
    }
    public static bool TryGet<T>(this IReadOnlyList<T> a, int index, out T? result)
    {
        if (index < 0 || index >= a.Count)
        {
            result = default;
            return false;
        }

        result = a[index];
        return true;
    }
}