using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace MacAddress;

public class Program
{
    [DllExport]
    public static IntPtr GetMacAddressString()
    {
#if false
        var list = GetMacAddressList();
        return StringToUTF8Addr(String.Join(":", list));
#else
        var list = GetNICList();
        var result = new List<object>();
        foreach (var nic in list)
        {
            result.Add(new
            {
                name = nic.Name,
                addr = String.Join("-", SplitStringByLengthList(nic.GetPhysicalAddress().ToString().ToLower(), 2))

            });
        }
        string json = new ObjectParser().Stringify(result, true);
        return StringToUTF8Addr(json);
#endif
    }
    public static IntPtr StringToUTF8Addr(string s)
    {
        int len = Encoding.UTF8.GetByteCount(s);
        byte[] buffer = new byte[len + 1];
        Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
        IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
        Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
        return nativeUtf8;
    }
    static List<NetworkInterface> GetNICList()
    {
        var list = NetworkInterface
                   .GetAllNetworkInterfaces()
                   .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                   .Select(nic => nic)
                   .ToList();
        return list;
    }
    static List<string> GetMacAddressList()
    {
        var list = NetworkInterface
                   .GetAllNetworkInterfaces()
                   .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                   .Select(nic => String.Join("-", SplitStringByLengthList(nic.GetPhysicalAddress().ToString().ToLower(), 2)))
                   .ToList();
        return list;
    }
    static IEnumerable<string> SplitStringByLengthLazy(string str, int maxLength)
    {
        for (int index = 0; index < str.Length; index += maxLength)
        {
            yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
        }
    }
    static List<string> SplitStringByLengthList(string str, int maxLength)
    {
        return SplitStringByLengthLazy(str, maxLength).ToList();
    }
}
