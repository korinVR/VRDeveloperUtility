using System.Net;
using System.Runtime.InteropServices;

namespace VRDeveloperUtility;

internal static class TcpTable
{
    private const int AfInet = 2;
    private const int TcpTableOwnerPidListener = 3;

    public static int? TryFindListeningProcessId(int localPort)
    {
        var bufferSize = 0;
        _ = GetExtendedTcpTable(
            IntPtr.Zero,
            ref bufferSize,
            true,
            AfInet,
            TcpTableOwnerPidListener,
            0);

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            var result = GetExtendedTcpTable(
                buffer,
                ref bufferSize,
                true,
                AfInet,
                TcpTableOwnerPidListener,
                0);

            if (result != 0)
            {
                return null;
            }

            var rowCount = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, 4);
            var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();

            for (var i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(IntPtr.Add(rowPtr, i * rowSize));
                var port = (ushort)IPAddress.NetworkToHostOrder((short)row.LocalPort);

                if (port == localPort && row.State == 2)
                {
                    return (int)row.OwningPid;
                }
            }

            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int tcpTableLength,
        bool sort,
        int ipVersion,
        int tableClass,
        int reserved);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct MibTcpRowOwnerPid
    {
        public readonly uint State;
        public readonly uint LocalAddr;
        public readonly uint LocalPort;
        public readonly uint RemoteAddr;
        public readonly uint RemotePort;
        public readonly uint OwningPid;
    }
}
