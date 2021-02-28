using System;
using System.Runtime.InteropServices;

namespace Pizarra
{
    public class TabletModeController
    {
        private static Guid GUID_GPIOBUTTONS_LAPTOPSLATE_INTERFACE = new Guid(0x317fc439, 0x3f77, 0x41c8, new byte[] { 0xb0, 0x9e, 0x08, 0xad, 0x63, 0x27, 0x2a, 0xa3 });
        private const int INVALID_HANDLE_VALUE = -1;
        private const int BUFFER_SIZE = 1024;

        private enum DesiredAccess : uint
        {
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000
        }

        private enum ShareMode : uint
        {
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002,
            FILE_SHARE_DELETE = 0x00000004
        }

        private enum CreationDisposition : uint
        {
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5
        }

        private enum FlagsAndAttributes : uint
        {
            FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
            FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,
            FILE_ATTRIBUTE_HIDDEN = 0x00000002,
            FILE_ATTRIBUTE_NORMAL = 0x00000080,
            FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
            FILE_ATTRIBUTE_OFFLINE = 0x00001000,
            FILE_ATTRIBUTE_READONLY = 0x00000001,
            FILE_ATTRIBUTE_SYSTEM = 0x00000004,
            FILE_ATTRIBUTE_TEMPORARY = 0x00000100
        }

        private enum DeviceConfig : uint {
            DIGCF_PRESENT = 0x00000002,
            DIGCF_DEVICEINTERFACE = 0x00000010,
        }

        public enum SystemMetric
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1,
            SM_CXVSCROLL = 2,
            SM_CYHSCROLL = 3,
            SM_REMOTECONTROL = 0x2001,
            SM_CONVERTIBLESLATEMODE = 0x2003,
        }

        [DllImport("kernel32", SetLastError = true)]
        static extern System.IntPtr CreateFile(
            string FileName,          // file name
            uint DesiredAccess,       // access mode
            uint ShareMode,           // share mode
            uint SecurityAttributes,  // Security Attributes
            uint CreationDisposition, // how to create
            uint FlagsAndAttributes,  // file attributes
            int hTemplateFile         // handle to template file
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            int lpOverlapped
            );

        [DllImport("kernel32", SetLastError = true)]
        static extern bool CloseHandle(
            System.IntPtr hObject // handle to object
            );

        [DllImport("kernel32", SetLastError = true)]
        static extern uint GetLastError();

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVICE_INTERFACE_DATA
        {
            public  Int32    cbSize;
            public  Guid     interfaceClassGuid;
            public  Int32    flags;
            private UIntPtr  reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVINFO_DATA
        {
            public UInt32 cbSize;
            public Guid ClassGuid;
            public UInt32 DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BUFFER_SIZE)]
            public string DevicePath;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid,
            [MarshalAs(UnmanagedType.LPTStr)] string enumerator,
            IntPtr hwndParent,
            uint flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr hDevInfo,
            IntPtr devInfo,
            ref Guid interfaceClassGuid,
            UInt32 memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
            );
        
        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
            UInt32 deviceInterfaceDetailDataSize,
            ref UInt32 requiredSize,
            ref SP_DEVINFO_DATA deviceInfoData
            );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(
            IntPtr DeviceInfoSet
            );

        [DllImport("setupapi.dll")]
        static extern int CM_Get_Parent(
            out UInt32 pdnDevInst,
            UInt32 dnDevInst,
            int ulFlags
            );

        [DllImport("setupapi.dll", SetLastError=true)]
        static extern int CM_Get_Device_ID(
            UInt32 dnDevInst,
            IntPtr buffer,
            int bufferLen,
            int flags
            );

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(SystemMetric smIndex);

        // https://www.pinvoke.net/default.aspx/setupapi/SetupDiGetDeviceInterfaceDetail.html
        private static string GetDeviceNameFromGuid(Guid guid)
        {
            string instancePath = string.Empty;
            // We start at the "root" of the device tree and look for all
            // devices that match the interface GUID of a disk
            IntPtr h = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, (uint)DeviceConfig.DIGCF_PRESENT | (uint)DeviceConfig.DIGCF_DEVICEINTERFACE);
            if (h.ToInt32() != INVALID_HANDLE_VALUE)
            {
                bool Success = true;
                uint i = 0;
                while (Success)
                {
                    // create a Device Interface Data structure
                    SP_DEVICE_INTERFACE_DATA dia = new SP_DEVICE_INTERFACE_DATA();
                    dia.cbSize = Marshal.SizeOf(dia);

                    // start the enumeration
                    Success = SetupDiEnumDeviceInterfaces(h, IntPtr.Zero, ref guid, i, ref dia);
                    if (Success)
                    {
                        // build a DevInfo Data structure
                        SP_DEVINFO_DATA da = new SP_DEVINFO_DATA();
                        da.cbSize = (uint)Marshal.SizeOf(da);

                        // build a Device Interface Detail Data structure
                        SP_DEVICE_INTERFACE_DETAIL_DATA didd = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                        if (IntPtr.Size == 8) // for 64 bit operating systems
                            didd.cbSize = 8;
                        else
                            didd.cbSize = 4 + Marshal.SystemDefaultCharSize; // for 32 bit systems

                        // now we can get some more detailed information
                        uint nRequiredSize = 0;
                        uint nBytes = BUFFER_SIZE;
                        if (SetupDiGetDeviceInterfaceDetail(h, ref dia, ref didd, nBytes, ref nRequiredSize, ref da))
                        {
                            instancePath = didd.DevicePath;

                            // current InstanceID is at the "USBSTOR" level, so we
                            // need up "move up" one level to get to the "USB" level
                            // uint ptrPrevious;
                            // CM_Get_Parent(out ptrPrevious, da.DevInst, 0);

                            // // Now we get the InstanceID of the USB level device
                            // IntPtr ptrInstanceBuf = Marshal.AllocHGlobal((int)nBytes);
                            // CM_Get_Device_ID(ptrPrevious, ptrInstanceBuf, (int)nBytes, 0);
                            // string InstanceID = Marshal.PtrToStringAuto(ptrInstanceBuf);

                            // Marshal.FreeHGlobal(ptrInstanceBuf);
                        }
                    }
                    i++;
                }
            }
            SetupDiDestroyDeviceInfoList(h);

            return instancePath;
        }

        // https://stackoverflow.com/questions/43106246/how-to-detect-tablet-mode
        public static bool IsTabletMode ()
        {
            int state = GetSystemMetrics(SystemMetric.SM_CONVERTIBLESLATEMODE);
            return (state == 0);
        }

        // https://docs.microsoft.com/en-us/windows-hardware/drivers/gpiobtn/laptop-slate-mode-toggling-between-states
        // this api is only toggle state.
        public static void ToggleTabletMode()
        {
            var DevicePath = GetDeviceNameFromGuid(GUID_GPIOBUTTONS_LAPTOPSLATE_INTERFACE);
            var FileHandle = CreateFile(DevicePath,
                                        (uint)DesiredAccess.GENERIC_WRITE,
                                        0,
                                        0,
                                        (uint)CreationDisposition.OPEN_EXISTING,
                                        0,
                                        0);
            var err = Marshal.GetLastWin32Error(); // if not admin execute, err = 4.

            byte[] buffer = new byte[1];
            buffer[0] = 0;
            WriteFile(FileHandle, buffer, 1, out uint written, 0);
            CloseHandle(FileHandle);
        }

        public static void SetTabletMode(bool isEnable)
        {
            if (IsTabletMode() != isEnable)
            {
                ToggleTabletMode();
            }
        }
    }
}
