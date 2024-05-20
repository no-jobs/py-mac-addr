using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MacAddress;

public class Program
{
    static ThreadLocal<IntPtr> JsonPtr = new ThreadLocal<IntPtr>();
    [DllExport]
    public static IntPtr GetMacAddressString()
    {
        if (JsonPtr.Value != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(JsonPtr.Value);
            JsonPtr.Value = IntPtr.Zero;
        }
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
        JsonPtr.Value = StringToUTF8Addr(json);
        return JsonPtr.Value;
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
        string localIp = GetLocalIp();
        if (localIp != null)
        {
            //Console.WriteLine($"localIp={localIp}");
#if false
            foreach(NetworkInterface nic in list)
            {
                Console.WriteLine(GetIPAdressFromNIC(nic));
            }
#endif
            list = list.Where(nic => GetIPAdressFromNIC(nic) == localIp).ToList();
        }
        return list;
    }
    static string GetLocalIp()
    {
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            if (!(socket.LocalEndPoint is IPEndPoint endPoint) || endPoint.Address == null)
            {
                return null;
            }
            return endPoint.Address.ToString();
        }
    }
    static string GetIPAdressFromNIC(NetworkInterface nic)
    {
        foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
        {
            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.Address.ToString();
            }
        }
        return null;
    }
#if false
    static List<string> GetMacAddressList()
    {
        var list = NetworkInterface
                   .GetAllNetworkInterfaces()
                   .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                   .Select(nic => String.Join("-", SplitStringByLengthList(nic.GetPhysicalAddress().ToString().ToLower(), 2)))
                   .ToList();
        return list;
    }
#endif
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
