
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace WootingAnalogSDKNET {
    public enum KeycodeType {
        /// <summary>
        /// USB HID Keycodes https://www.usb.org/document-library/hid-usage-tables-112 pg53
        /// </summary>
        HID,
        /// <summary>
        /// Scan code set 1
        /// </summary>
        ScanCode1,
        /// <summary>
        /// Windows Virtual Keys
        /// </summary>
        VirtualKey,
        /// <summary>
        /// Windows Virtual Keys which are translated to the current keyboard locale
        /// </summary>
        VirtualKeyTranslate
    }    
    
    public enum WootingAnalogResult  {
        /// <summary>
        /// Success
        /// </summary>
        Ok = 1,

        /// <summary>
        /// Item hasn't been initialized
        /// </summary>
        UnInitialized = -2000,

        /// <summary>
        /// No Devices are connected
        /// </summary>
        NoDevices,

        /// <summary>
        /// Device has been disconnected
        /// </summary>
        DeviceDisconnected,

        /// <summary>
        /// Generic Failure
        /// </summary>
        Failure,

        /// <summary>
        /// A given parameter was invalid
        /// </summary>
        InvalidArgument,

        /// <summary>
        /// No Plugins were found
        /// </summary>
        NoPlugins,

        /// <summary>
        /// The specified function was not found in the library
        /// </summary>
        FunctionNotFound,

        /// <summary>
        /// No Keycode mapping to HID was found for the given Keycode
        /// </summary>
        NoMapping,

        /// <summary>
        /// Indicates that it isn't available on this platform
        /// </summary>
        NotAvailable,

        /// <summary>
        /// Indicates that the operation that is trying to be used is for an older version
        /// </summary>
        IncompatibleVersion,

        /// <summary>
        /// Indicates that the Analog SDK could not be found on the system
        /// </summary>
        DLLNotFound

    }

    public enum DeviceType  {
        /// <summary>
        /// Device is of type Keyboard
        /// </summary>
        Keyboard = 1,

        /// <summary>
        /// Device is of type Keypad
        /// </summary>
        Keypad,

        /// <summary>
        /// Other type of device
        /// </summary>
        Other
    }

    public enum DeviceEventType  {
        /// <summary>
        /// Device has been connected
        /// </summary>
        Connected = 1,

        /// <summary>
        /// Device has been disconnected
        /// </summary>
        Disconnected
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceInfo {
        /// <summary>
        /// Vendor ID of the Device
        /// </summary>
        public readonly ushort vendor_id;
        /// <summary>
        /// Product ID of the Device
        /// </summary>
        public readonly ushort product_id;
        /// <summary>
        /// Name of the manufacturer of the Device
        /// </summary>
        public readonly string manufacturer_name;
        /// <summary>
        /// Device model name
        /// </summary>
        public readonly string device_name;
        /// <summary>
        /// ID of the Device used with ReadAnalog and ReadFullBuffer to read specifically from this device
        /// </summary>
        public readonly ulong device_id;
        /// <summary>
        /// Type of the Device
        /// </summary>
        public readonly DeviceType device_type;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(
                this, Formatting.Indented);
        }
    }

    public delegate void DeviceEventHandler(DeviceEventType eventType, DeviceInfo deviceInfo);
    
    public static class WootingAnalogSDK {
        #region Private members
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void RawDeviceEventCb(DeviceEventType eventType, IntPtr deviceInfo);

        private const string SdkLib = "wooting_analog_wrapper";

        [DllImport(SdkLib, EntryPoint = "wooting_analog_initialise")]
        private static extern int initialise();

        [DllImport(SdkLib)]
        private static extern bool wooting_analog_is_initialised();

        [DllImport(SdkLib)]
        private static extern float wooting_analog_read_analog(ushort code);

        [DllImport(SdkLib)]
        private static extern float wooting_analog_read_analog_device(ushort code, ulong deviceId);

        [DllImport(SdkLib, EntryPoint = "wooting_analog_set_device_event_cb")]
        private static extern WootingAnalogResult setDeviceEventCallback(RawDeviceEventCb cb);

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

        [DllImport(SdkLib)]
        private static extern int wooting_analog_read_full_buffer([In][Out][MarshalAs(UnmanagedType.LPArray)] short[] codeBuffer, [In][Out][MarshalAs(UnmanagedType.LPArray)] float[] analogBuffer, uint len);
        
        [DllImport(SdkLib)]
        private static extern int wooting_analog_read_full_buffer_device([In][Out][MarshalAs(UnmanagedType.LPArray)] short[] codeBuffer, [In][Out][MarshalAs(UnmanagedType.LPArray)] float[] analogBuffer, uint len, ulong deviceID);

        [DllImport(SdkLib)]
        private static extern int wooting_analog_get_connected_devices_info([In][Out][MarshalAs(UnmanagedType.LPArray)] IntPtr[] buffer, uint len);
        #endregion

        #region Public Interface
        /// <summary>
        /// Gets a bool indicating if the Analog SDK has been Initialised
        /// </summary>
        /// <value>Is Analog SDK Initialised</value>
        public static bool IsInitialised
        {
            get => wooting_analog_is_initialised();
        }

        /// <summary>
        /// Initialise the Analog SDK
        /// </summary>
        /// <returns>
        /// If successful the first element will be >=0 indicating the number of devices connected and the second WootingAnalogResult.Ok
        /// If unnsuccessful the first element will be -1 and the second will be the Initialisation error
        /// 
        /// The possible errors are:
        /// * NoPlugins: Meaning that either no plugins were found or some were found but none were successfully initialised
        /// * FunctionNotFound: Indicates that the wrapper could not load the function from the SDK, which will usually mean that the SDK is not installed or could not be found
        /// * IncompatibleVersion: Indicates that the wrapper you're using is designed for a different major version of the SDK than what is installed. Major versions are unlikely to change very often
        /// </returns>
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

        /// <summary>
        /// Uninitialises the SDK, returning it to an empty state, similar to how it would be before first initialisation
        /// </summary>
        /// <returns>
        /// * Ok: Indicates that the SDK was successfully uninitialised
        /// </returns>
        [DllImport(SdkLib, EntryPoint = "wooting_analog_uninitialise")]
        public static extern WootingAnalogResult UnInitialise();

        /// <summary>
        /// Sets the type of Keycodes the Analog SDK will receive (in `ReadAnalog`) and output (in `ReadFullBuffer`).
        /// By default, the mode is set to HID
        /// </summary>
        /// <param name="mode">
        /// The KeycodeType which wil determine the Keycode set returned from all functions
        /// NOTE: VirtualKey and VirtualKeyTranslate will only work on Windows
        /// </param>
        /// <returns>
        /// - Ok: The Keycode mode was changed successfully
        /// - InvalidArgument: The given `KeycodeType` is not one supported by the SDK
        /// - NotAvailable: The given `KeycodeType` is present, but not supported on the current platform
        /// - UnInitialized: The SDK is not initialised
        /// </returns>
        [DllImport(SdkLib, EntryPoint = "wooting_analog_set_keycode_mode")]
        public static extern WootingAnalogResult SetKeycodeMode(KeycodeType mode);

        /// <summary>
        /// Reads the Analog value of the key with identifier '<paramref name="code"/>' from any connected device (or the device with id '<paramref name="deviceID"/>' if specified). The set of key identifiers that is used depends on the Keycode mode set using <c>SetKeycodeMode</c>.
        /// The '<paramref name="deviceID"/>' can be found through calling <c>GetDeviceInfo</c> and getting the DeviceID from one of the DeviceInfo structs
        /// </summary>
        /// <param name="code">
        /// The keycode of the key you wish to get the analog value for (from the keycode set that was set using <c>SetKeycodeMode</c>)
        /// </param>
        /// <param name="deviceID">
        /// The DeviceID of the device you wish to read the analog value from. If none specified, the max analog value of the key from all devices will be given
        /// </param>
        /// <returns>
        /// If the call is successful Item1 of the tuple will be the Analog value between 0.0f->1.0f and Item2 being WootingAnalogResult.Ok
        /// If unsuccessful, Item1 of the tuple will be -1.0f and Item2 will be one of the following errors:
        /// * NoMapping: No keycode mapping was found from the selected mode (set by <c>SetKeycodeMode</c> and HID.
        /// * UnInitialized: The SDK is not initialised
        /// * NoDevices: There are no connected devices (with id <paramref name="deviceID"/> if specified)
        /// </returns>
        public static (float, WootingAnalogResult) ReadAnalog(ushort code, ulong deviceID = 0)
        {
            float res = wooting_analog_read_analog_device(code, deviceID);
            if (res >= 0)
                return (res, WootingAnalogResult.Ok);
            else
                return (-1.0f, (WootingAnalogResult) (int) res);
        }

        /// <summary>
        /// Event which is called whenever a Device is connected or disconnected
        /// </summary>        
        public static event DeviceEventHandler DeviceEvent;
       
        /// <summary>
        /// Reads all the analog values for pressed keys for all devices and combines their values (or reads from a single device with id `<paramref name="deviceID"/>` [if specified]),
        /// - If two devices have the same key pressed, the greater value will be given (if no `<paramref name="deviceID"/>` has been given)
        /// - When a key is released it will be returned with an analog value of 0.0f in the first call of this function after the key has been released
        /// </summary>
        /// <param name="length">
        /// The maximum number of keys you want to accept
        /// </param>
        /// <param name="deviceID">
        /// The DeviceID of the device you wish to read the analog values from. DeviceID's can be found through <c>GetConnectedDevices</c>
        /// </param>
        /// <returns>
        /// If successful, Item1 will be a List of the pairs of the keycodes of pressed keys and their corresponding analog value between 0.0f and 1.0f. Item2 will be <c>WootingAnalogResult.Ok</c>
        /// If unsuccessful, Item1 will be <c>null</c> and Item2 will be one of the following errors:
        /// * `WootingAnalogResult.UnInitialized`: Indicates that the Analog SDK hasn’t been initialised
        /// * `WootingAnalogResult.NoDevices`: Indicates no devices are connected (or that there is no device with id `<paramref name="deviceID"/>` [if specified])
        /// </returns>
        public static (List<(short, float)>, WootingAnalogResult) ReadFullBuffer(uint length = 20, ulong deviceID = 0)
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

        /// <summary>
        /// Get a list of the connected devices and associated information
        /// </summary>
        /// <returns>
        /// Item1 will always be a List of the connected devices.
        /// Item2 can be one of the following values:
        /// * Ok: Indicates that the call was successful
        /// * UnInitialized: Indicates that the Analog SDK hasn’t been initialised
        /// </returns>        
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

        #endregion
    }
}