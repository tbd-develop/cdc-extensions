using System.Dynamic;
using TbdDevelop.CDC.Extensions.Contracts;

namespace TbdDevelop.CDC.Extensions.Infrastructure;

internal static class CdcHelperExtensions
{
    public static object ToObject(this IDictionary<string, object> entity)
    {
        var result = new ExpandoObject();

        var setter = result as IDictionary<string, object>;

        foreach (var property in entity)
        {
            setter.Add(property.Key, property.Value);
        }

        return result;
    }

    public static ChangeOperation AsChangeOperation(this CdcOperation operation)
    {
        return operation switch
        {
            CdcOperation.Insert => ChangeOperation.Insert,
            CdcOperation.UpdateAfter => ChangeOperation.Update,
            CdcOperation.Delete => ChangeOperation.Delete,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static int CompareLsn(this byte[]? lsn1, byte[]? lsn2)
    {
        return lsn1 switch
        {
            null when lsn2 == null => 0,
            null => -1,
            _ => lsn2 == null ? 1 : lsn1.CompareByteArrays(lsn2)
        };
    }

    private static int CompareByteArrays(this byte[] array1, byte[] array2)
    {
        for (var i = 0; i < Math.Min(array1.Length, array2.Length); i++)
        {
            var comparison = array1[i].CompareTo(array2[i]);
            if (comparison != 0) return comparison;
        }

        return array1.Length.CompareTo(array2.Length);
    }

    public static bool AreLsnsEqual(this byte[]? lsn1, byte[]? lsn2)
    {
        if (lsn1 == null && lsn2 == null) return true;
        if (lsn1 == null || lsn2 == null) return false;

        return lsn1.SequenceEqual(lsn2);
    }

    public static string AsReadableString(this byte[]? lsn)
    {
        if (lsn == null)
        {
            return "NULL";
        }

        return "0x" + BitConverter.ToString(lsn).Replace("-", "");
    }
}