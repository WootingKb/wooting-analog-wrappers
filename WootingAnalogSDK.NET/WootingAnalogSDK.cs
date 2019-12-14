
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace WootingAnalogSDKNET {
    public enum KeycodeType {
        HID,
        ScanCode1,
        VirtualKey,
        VirtualKeyTranslate
    }    
    
    public enum WootingAnalogResult  {
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
        NoMapping,
        /// Indicates that it isn't available on this platform
        NotAvailable

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

    public delegate void DeviceEventHandler(DeviceEventType eventType, DeviceInfo deviceInfo);
    
    public static class WootingAnalogSDK {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void RawDeviceEventCb(DeviceEventType eventType, IntPtr deviceInfo);

        public const string SdkLib = "wooting_analog_wrapper";

        [DllImport(SdkLib, EntryPoint = "wooting_analog_initialise")]
        private static extern int initialise();

        public static (int, WootingAnalogResult) Initialise() {
            int initResult = initialise();
            if (initResult >= 0) {
                WootingAnalogResult eventSetResult = setDeviceEventCallback(_internalCallback);
                if (eventSetResult == WootingAnalogResult.Ok) {
                    return (initResult, WootingAnalogResult.Ok);
                }
                else {
                    throw new Exception("Wooting Analog SDK was initialised successfully, but the event handler could not be set!");
                }
            } else {
                return (-1, (WootingAnalogResult)initResult);
            }
        }

        [DllImport(SdkLib)]
        private static extern bool wooting_analog_is_initialised();
        
        public static bool IsInitialised
        {
            get => wooting_analog_is_initialised();
        }

        [DllImport(SdkLib, EntryPoint = "wooting_analog_uninitialise")]
        public static extern WootingAnalogResult UnInitialise();

        [DllImport(SdkLib, EntryPoint = "wooting_analog_set_keycode_mode")]
        public static extern WootingAnalogResult SetKeycodeMode(KeycodeType mode);
        
        [DllImport(SdkLib)]
        private static extern float wooting_analog_read_analog(ushort code);
        
        [DllImport(SdkLib)]
        private static extern float wooting_analog_read_analog_device(ushort code, ulong deviceId);

        public static (float, WootingAnalogResult) ReadAnalog(ushort code, ulong deviceId = 0)
        {
            float res = wooting_analog_read_analog_device(code, deviceId);
            if (res >= 0)
                return (res, WootingAnalogResult.Ok);
            else
                return (-1.0f, (WootingAnalogResult) (int) res);
        }

        
        [DllImport(SdkLib, EntryPoint = "wooting_analog_set_device_event_cb")]
        private static extern WootingAnalogResult setDeviceEventCallback(RawDeviceEventCb cb);

        public static event DeviceEventHandler DeviceEvent;

        
        private static RawDeviceEventCb _internalCallback = new RawDeviceEventCb(internalCallback);
        private static void internalCallback(DeviceEventType eventType, IntPtr deviceInfo) {
            Console.WriteLine("Internal callback");
            var dev = (DeviceInfo)Marshal.PtrToStructure(
                            deviceInfo,
                            typeof(DeviceInfo));
            DeviceEvent?.Invoke(eventType, dev);
        }


        [DllImport(SdkLib, EntryPoint = "wooting_analog_clear_device_event_cb")]
        private static extern WootingAnalogResult wooting_analog_clear_device_event_cb();

        //fn wooting_analog_device_info(buffer: *mut Void, len: c_uint) -> c_int;
        //fn wooting_analog_read_full_buffer(code_buffer: *mut c_ushort, analog_buffer: *mut c_float, len: c_uint) -> c_int;

        [DllImport(SdkLib)]
        private static extern int wooting_analog_read_full_buffer([In][Out][MarshalAs(UnmanagedType.LPArray)] short[] codeBuffer, [In][Out][MarshalAs(UnmanagedType.LPArray)] float[] analogBuffer, uint len);
        
        [DllImport(SdkLib)]
        private static extern int wooting_analog_read_full_buffer_device([In][Out][MarshalAs(UnmanagedType.LPArray)] short[] codeBuffer, [In][Out][MarshalAs(UnmanagedType.LPArray)] float[] analogBuffer, uint len, ulong deviceID);

        public static (List<(short, float)>, WootingAnalogResult) ReadFullBuffer(uint length, ulong deviceID = 0)
        {
            short[] codeBuffer = new short[length];
            float[] analogBuffer = new float[length];
            int count = wooting_analog_read_full_buffer_device(codeBuffer, analogBuffer, length, deviceID);

            if (count < 0)
                return (null, (WootingAnalogResult)count);

            List<(short, float)> data = new List<(short, float)>();
            for (int i = 0; i < count; i++)
            {
                data.Add( (codeBuffer[i], analogBuffer[i]) );
            }
            data.Sort((u, v) => u.Item1.CompareTo(v.Item1));
            return (data, WootingAnalogResult.Ok);
        }

        [DllImport(SdkLib)]
        private static extern int wooting_analog_get_connected_devices_info([In][Out][MarshalAs(UnmanagedType.LPArray)] IntPtr[] buffer, uint len);
        
        public static (List<DeviceInfo>, WootingAnalogResult) GetConnectedDevicesInfo(){
            IntPtr[] buffer = new IntPtr[40];
            int count = wooting_analog_get_connected_devices_info(buffer, (uint)buffer.Length);
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
                }).Where(s => s != null).Cast<DeviceInfo>().ToList(), WootingAnalogResult.Ok);
            }
            else
                return (new List<DeviceInfo>(), (WootingAnalogResult)count);
        }
    }
}