// SeaBreezeWrapper.cs
//
// Wrap the SeaBreezeWrapper C interface with C# bindings.
//
// For documentation on these functions, see include/api/SeaBreezeWrapper.h

using System.Runtime.InteropServices;
using System;

public partial class SeaBreezeWrapper
{
    public const int SLOT_LENGTH = 15;
    public const int ERROR_SUCCESS = 0;

    // NOTE: To Debug SeaBreeze.dll set the full absolute path to your debug build of SeaBreeze.dll
    //       For example: const string DLL = @"C:\Code\seabreeze-code\trunk\SeaBreeze\os-support\windows\VisualStudio2013\x64\Debug\SeaBreeze.dll";
    const string DLL = @"SeaBreeze.dll";

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    public static bool IsDllLoaded()
    {
        return GetModuleHandle(DLL) != IntPtr.Zero;
    }

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern double seabreeze_read_double                        (int index, ref int errorCode, int slot_number); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern double seabreeze_read_tec_temperature               (int index, ref int errorCode); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern float  seabreeze_read_irrad_collection_area         (int index, ref int errorCode);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_close_spectrometer                 (int index, ref int errorCode); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_api_version_string             (ref byte[] buffer, int len);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_edc_indices                    (int index, ref int errorCode, ref int buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_eeprom_slot_count              (int index, ref int errorCode);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_electric_dark_pixel_indices    (int index, ref int errorCode, ref int indices, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_error_string                   (int errorCode, ref byte buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_formatted_spectrum             (int index, ref int errorCode, ref double buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_formatted_spectrum_length      (int index, ref int errorCode); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_serial_number                  (int index, ref int errorCode, ref byte buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_model                          (int index, ref int errorCode, ref byte buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_unformatted_spectrum           (int index, ref int errorCode, ref byte buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_unformatted_spectrum_length    (int index, ref int errorCode);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_usb_descriptor_string          (int index, ref int errorCode, int id, ref byte buffer, int length);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_get_wavelengths                    (int index, ref int errorCode, ref double wavelengths, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_has_irrad_collection_area          (int index, ref int errorCode);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_open_spectrometer                  (int index, ref int errorCode); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_read_eeprom_slot                   (int index, ref int errorCode, int slot_number, ref byte buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_read_irrad_calibration             (int index, ref int errorCode, ref float buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_read_usb                           (int index, ref int errorCode, byte endpoint, ref byte buffer, int length);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_set_continuous_strobe_period       (int index, ref int errorCode, float msec); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_write_eeprom_slot                  (int index, ref int errorCode, int slot_number, ref byte buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_write_irrad_calibration            (int index, ref int errorCode, ref float buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern int    seabreeze_write_usb                          (int index, ref int errorCode, byte endpoint, ref byte buffer, int length); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern long   seabreeze_get_min_integration_time_microsec  (int index, ref int errorCode); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern void   seabreeze_override_eeprom_slot_count         (int index, ref int errorCode, int count);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern void   seabreeze_set_integration_time_microsec      (int index, ref int errorCode, long integration_time_micros); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern void   seabreeze_set_shutter_open                   (int index, ref int errorCode, int opened); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern void   seabreeze_set_strobe_enable                  (int index, ref int errorCode, int strobe_enable); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern void   seabreeze_set_tec_enable                     (int index, ref int errorCode, int tec_enable); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern void   seabreeze_set_tec_fan_enable                 (int index, ref int errorCode, int tec_fan_enable); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern void   seabreeze_set_tec_temperature                (int index, ref int errorCode, double temperature_degrees_celsius); 
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)] public static extern void   seabreeze_set_trigger_mode                   (int index, ref int errorCode, int mode); 
}
