
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace AnalogSDK.NET {
    public enum KeycodeType {
        HID,
        ScanCode1,
        VirtualKey,
        VirtualKeyTranslate
    }    
    
    public enum AnalogSDKError  {
        Ok = 1,
        UnInitialized = -2000,
        NoDevices,
        DeviceDisconnected,
        //Generic Failure
        Failure,
        InvalidArgument,
        NoPlugins,
        FunctionNotFound,
        //No Keycode mapping to HID was found for the given Keycode
        NoMapping

    }

    public enum DeviceEventType  {
        Connected = 1,
        Disconnected
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceInfo {
        public readonly ushort vendor_id;
        public readonly ushort product_id;
        public readonly string manufacturer_name;
        public readonly string device_name;
        public readonly ulong device_id;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(
                this, Formatting.Indented);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void DeviceEventCb(DeviceEventType eventType, IntPtr deviceInfo);
    
    public static class AnalogSDK {
        public const string SdkLib = "libanalog_sdk_wrapper";

        [DllImport(SdkLib, EntryPoint = "sdk_intialise")]
        public static extern AnalogSDKError Initialise();

        [DllImport(SdkLib)]
        private static extern bool sdk_is_initialised();
        
        public static bool IsInitialised
        {
            get => sdk_is_initialised();
        }

        [DllImport(SdkLib, EntryPoint = "sdk_uninitialise")]
        public static extern AnalogSDKError UnInitialise();

        [DllImport(SdkLib, EntryPoint = "sdk_set_mode")]
        public static extern AnalogSDKError SetMode(KeycodeType mode);
        
        [DllImport(SdkLib)]
        private static extern float sdk_read_analog(ushort code);
        
        [DllImport(SdkLib)]
        private static extern float sdk_read_analog_device(ushort code, ulong deviceId);

        public static (float, AnalogSDKError) ReadAnalog(ushort code, ulong deviceId = 0)
        {
            float res = sdk_read_analog_device(code, deviceId);
            if (res >= 0)
                return (res, AnalogSDKError.Ok);
            else
                return (-1.0f, (AnalogSDKError) (int) res);
        }

        
        [DllImport(SdkLib, EntryPoint = "sdk_set_device_event_cb")]
        public static extern AnalogSDKError SetDeviceEventCallback(DeviceEventCb cb);

        [DllImport(SdkLib, EntryPoint = "sdk_clear_device_event_cb")]
        public static extern AnalogSDKError sdk_clear_device_event_cb();

        //fn sdk_device_info(buffer: *mut Void, len: c_uint) -> c_int;
        //fn sdk_read_full_buffer(code_buffer: *mut c_ushort, analog_buffer: *mut c_float, len: c_uint) -> c_int;

        [DllImport(SdkLib)]
        private static extern int sdk_read_full_buffer([In][Out][MarshalAs(UnmanagedType.LPArray)] short[] codeBuffer, [In][Out][MarshalAs(UnmanagedType.LPArray)] float[] analogBuffer, uint len);
        
        [DllImport(SdkLib)]
        private static extern int sdk_read_full_buffer_device([In][Out][MarshalAs(UnmanagedType.LPArray)] short[] codeBuffer, [In][Out][MarshalAs(UnmanagedType.LPArray)] float[] analogBuffer, uint len, ulong deviceID);

        public static (List<(short, float)>, AnalogSDKError) ReadFullBuffer(uint length, ulong deviceID = 0)
        {
            short[] codeBuffer = new short[length];
            float[] analogBuffer = new float[length];
            int count = sdk_read_full_buffer_device(codeBuffer, analogBuffer, length, deviceID);

            if (count < 0)
                return (null, (AnalogSDKError)count);

            List<(short, float)> data = new List<(short, float)>();
            for (int i = 0; i < count; i++)
            {
                data.Add( (codeBuffer[i], analogBuffer[i]) );
            }
            data.Sort((u, v) => u.Item1.CompareTo(v.Item1));
            return (data, AnalogSDKError.Ok);
        }

        [DllImport(SdkLib)]
        private static extern int sdk_device_info([In][Out][MarshalAs(UnmanagedType.LPArray)] IntPtr[] buffer, uint len);
        
        public static (List<DeviceInfo>, AnalogSDKError) GetDeviceInfo(){
            IntPtr[] buffer = new IntPtr[40];
            int count = sdk_device_info(buffer, (uint)buffer.Length);
            if (count > 0)
            {
                return (buffer.Select<IntPtr, DeviceInfo?>((ptr) =>
                {
                    if (ptr != IntPtr.Zero)
                    {
                        return (DeviceInfo)Marshal.PtrToStructure(
                                   ptr,
                                   typeof(DeviceInfo));
                    }
                    return null;
                }).Where(s => s != null).Cast<DeviceInfo>().ToList(), AnalogSDKError.Ok);
            }
            else
                return (new List<DeviceInfo>(), (AnalogSDKError)count);
        }
    }
}