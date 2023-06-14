using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace WasatchNET
{
    public class HOCTSpectrometer : Spectrometer
    {
        public static class OctUsb
        {
            /// <summary>Use the first read endpoint</summary>
            public static readonly byte TRANSFER_ENDPOINT = UsbConstants.ENDPOINT_DIR_MASK;

            /// <summary>Number of transfers to sumbit before waiting begins</summary>
            public static readonly int TRANSFER_MAX_OUTSTANDING_IO = 4;

            /// <summary>Number of transfers before terminating the test</summary>
            public static readonly int TRANSFER_COUNT = 30;

            /// <summary>Maximum number of pixels the sensor can provide</summary>
            public static readonly int MAX_NUM_OF_PIXELS = 2048;

            /// <summary>Number of Bytes Per Pixel</summary>
            public static readonly int NUM_OF_BYTES_PER_PIXEL = 2;

            /// <summary>Number of Lines Per USB Transfer</summary>
            public static readonly int NUM_OF_LINES_PER_USB = 4;

            /// <summary>Number of Lines Per Frame</summary>
            public static readonly int NUM_OF_LINES_PER_FRAME = 500;

            /// <summary>Initial Integration Time</summary>
            public static readonly int INTEGRATION_TIME = 400;



            public static byte[] bVerArm = new byte[4];
            public static byte[] bVerArmBuildDate = new byte[12];
            public static byte[] bVerArmBuildTime = new byte[9];
            public static byte[] bVerFpga = new byte[4];

            public static int iFifoMax = 0;
            public static int iFifoProcessingCount = 0;
            public static int iFifoReadyCount = 0;
            public static int iFifoValidCount = 0;

            public static int iNumOfPixels = 1024;


            private static int iLinesPerFrame = NUM_OF_LINES_PER_FRAME;
            private static int iTransferSize = 0;


            private static readonly byte[] CmdArmCapture = new byte[] { 0x03, 0x01 };
            private static readonly byte[] CmdConfigLinesPerFrame = new byte[] { 0x01, 0x03, (byte)((NUM_OF_LINES_PER_FRAME & 0xFF)       >>  0),
                                                                                           (byte)((NUM_OF_LINES_PER_FRAME & 0xFF00)     >>  8),
                                                                                           (byte)((NUM_OF_LINES_PER_FRAME & 0xFF0000)   >> 16),
                                                                                           (byte)((NUM_OF_LINES_PER_FRAME & 0xFF000000) >> 24) };
            private static readonly byte[] CmdConfigIntegrationTime = new byte[] { 0x01, 0x00, (byte)((INTEGRATION_TIME & 0xFF)   >> 0),
                                                                                           (byte)((INTEGRATION_TIME & 0xFF00) >> 8) };
            private static readonly byte[] CmdConfigPixelCount = new byte[] { 0x01, 0x06, (byte)((iNumOfPixels & 0xFF)   >> 0),
                                                                                               (byte)((iNumOfPixels & 0xFF00) >> 8) };
            private static readonly byte[] CmdControlBoardSim = new byte[] { 0x73, 0x00, 0x00, 0x00, 0x00 };
            private static readonly byte[] CmdDisarmCapture = new byte[] { 0x03, 0x00 };
            private static readonly byte[] CmdReadCalibration = new byte[] { 0x04, 0x00 };
            private static readonly byte[] CmdResetSequence = new byte[] { 0x02 };
            private static readonly byte[] CmdRetrieveFifoStatus = new byte[] { 0x60 };
            private static readonly byte[] CmdTestPattern = new byte[] { 0x70, 0x00, 0x00 };
            private static readonly byte[] CmdVersionArm = new byte[] { 0x00, 0x01 };
            private static readonly byte[] CmdVersionArmBuildDate = new byte[] { 0x00, 0x02 };
            private static readonly byte[] CmdVersionArmBuildTime = new byte[] { 0x00, 0x03 };
            private static readonly byte[] CmdVersionFpga = new byte[] { 0x00, 0x00 };
            private static readonly byte[] CmdWriteCalibration = new byte[] { 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };



            private static ushort[] RawPixelData = new ushort[NUM_OF_LINES_PER_FRAME * MAX_NUM_OF_PIXELS * NUM_OF_BYTES_PER_PIXEL / 2];


            private static UsbDevice OCT;
            private static UsbDeviceFinder OCTDevFinder = new UsbDeviceFinder(0x24AA, 0x5000);

            private static UsbInterfaceInfo usbInterfaceInfo = null;
            private static UsbEndpointInfo usbEndpointInfo = null;

            private static UsbEndpointReader b1_reader = null;
            private static UsbEndpointWriter b1_writer = null;
            private static UsbEndpointReader b2_reader = null;
            private static UsbEndpointWriter b2_writer = null;

            private const byte SET_READ_BIT = 0x80;
            private const int LONG_TIMEOUT_MS = 1000;
            private const int SHORT_TIMEOUT_MS = 100;


            /// <summary>
            /// Initialize the USB Device to a Clean Starting Point
            /// </summary>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            private static bool InitUSB()
            {
                byte[] bReturnBuffer = new byte[64];

                int uiTemp;

                ErrorCode eReturn;


                try
                {
                    IUsbDevice OCTWhole = OCT as IUsbDevice;

                    UsbEndpointBase.LookupEndpointInfo(OCT.Configs[0], TRANSFER_ENDPOINT, out usbInterfaceInfo, out usbEndpointInfo);

                    if (!ReferenceEquals(OCTWhole, null))
                    {
                        OCTWhole.SetConfiguration(1);

                        OCTWhole.ClaimInterface(usbInterfaceInfo.Descriptor.InterfaceID);
                    }
                    else
                    {
                        return false;
                    }

                    OCTWhole.SetConfiguration(0x01);

                    b1_reader = OCT.OpenEndpointReader(ReadEndpointID.Ep01);
                    b1_writer = OCT.OpenEndpointWriter(WriteEndpointID.Ep01);
                    b2_reader = OCT.OpenEndpointReader(ReadEndpointID.Ep02);
                    b2_writer = OCT.OpenEndpointWriter(WriteEndpointID.Ep02);

                    b1_reader.Reset();
                    b1_reader.Flush();

                    b2_reader.Reset();
                    b2_reader.Flush();

                    CmdVersionFpga[0] |= SET_READ_BIT;
                    b1_writer.Write(CmdVersionFpga, LONG_TIMEOUT_MS, out uiTemp);
                    if ((eReturn = b1_reader.Read(bReturnBuffer, LONG_TIMEOUT_MS, out uiTemp)) == ErrorCode.None)
                    {
                        for (int i = 0; i < uiTemp; i++)
                        {
                            bVerFpga[i] = bReturnBuffer[i];
                        }

                        Console.WriteLine("FPGA Version : " + (int)(bVerFpga[3]) + "." + (int)(bVerFpga[2]) + "." + (int)(bVerFpga[1]) + "." + (int)(bVerFpga[0]));
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Reading Veriong FPGA");
                    }

                    CmdVersionArm[0] |= SET_READ_BIT;
                    b1_writer.Write(CmdVersionArm, LONG_TIMEOUT_MS, out uiTemp);
                    if ((eReturn = b1_reader.Read(bReturnBuffer, LONG_TIMEOUT_MS, out uiTemp)) == ErrorCode.None)
                    {
                        for (int i = 0; i < uiTemp; i++)
                        {
                            bVerArm[i] = bReturnBuffer[i];
                        }

                        Console.WriteLine("ARM Version  : " + (int)(bVerFpga[3]) + "." + (int)(bVerFpga[2]) + "." + (int)(bVerFpga[1]) + "." + (int)(bVerFpga[0]));
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Reading Veriong ARM");
                    }

                    CmdVersionArmBuildDate[0] |= SET_READ_BIT;
                    b1_writer.Write(CmdVersionArmBuildDate, LONG_TIMEOUT_MS, out uiTemp);
                    if ((eReturn = b1_reader.Read(bReturnBuffer, LONG_TIMEOUT_MS, out uiTemp)) == ErrorCode.None)
                    {
                        for (int i = 0; i < uiTemp; i++)
                        {
                            bVerArmBuildDate[i] = bReturnBuffer[i];
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Reading Veriong ARM Build Date");
                    }

                    CmdVersionArmBuildTime[0] |= SET_READ_BIT;
                    b1_writer.Write(CmdVersionArmBuildTime, LONG_TIMEOUT_MS, out uiTemp);
                    if ((eReturn = b1_reader.Read(bReturnBuffer, LONG_TIMEOUT_MS, out uiTemp)) == ErrorCode.None)
                    {
                        for (int i = 0; i < uiTemp; i++)
                        {
                            bVerArmBuildTime[i] = bReturnBuffer[i];
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Reading Veriong ARM Build Time");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.InitUsb - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.InitUsb - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Opens the USB Device Specified
            /// </summary>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool OpenDevice(int iVID, int iPID)
            {
                OCT = UsbDevice.OpenUsbDevice(OCTDevFinder);

                if (OCT == null)       //throw new Exception("Device Not Found");
                {
                    return false;
                }
                else
                {
                    return InitUSB();
                }
            }


            /// <summary>
            /// Close the USB Device
            /// </summary>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool CloseDevice()
            {
                try
                {
                    OCT.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.CloseDevice - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.CloseDevice - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Default Values to Retrieve
            /// </summary>
            /// <returns>
            /// Default Values of USB Config
            /// </returns>
            public static int DefaultIntegrationTime() { return INTEGRATION_TIME; }
            public static int DefaultNumOfLinesPerPixel() { return NUM_OF_BYTES_PER_PIXEL; }
            public static int DefaultNumOfLinesPerFrame() { return NUM_OF_LINES_PER_FRAME; }
            public static int DefaultNumOfLinesPerUsb() { return NUM_OF_LINES_PER_USB; }
            public static int DefaultNumOfPixels() { return iNumOfPixels; }
            public static int DefaultTransferCount() { return TRANSFER_COUNT; }
            public static int DefaultTransferSize() { return iTransferSize; }


            /// <summary>
            /// Capture an Entire Frame of Data to be Displayed as a BitMap
            /// </summary>
            /// <param name="iFramesTransmitted">
            /// Number of Frames to be Transmitted
            /// </param>
            /// <returns>
            /// Unsinged Short Array of an Entire Frame of Data
            /// </returns>
            public static ushort[] CaptureBitMap(int iFramesTransmitted, bool bInternalTrigger, bool bFirst, ref bool bError)
            {
                byte[] bReadBuffer = new byte[iTransferSize];

                int uiTransmitted;

                ErrorCode eReturn;

                bError = false;


                if (bFirst)
                {
                    Console.WriteLine("CaptureBitMap: ConfigIntegrationTime");
                    b1_writer.Write(CmdConfigIntegrationTime, LONG_TIMEOUT_MS, out uiTransmitted);
                    Console.WriteLine("CaptureBitMap: ResetSequence");
                    b1_writer.Write(CmdResetSequence, LONG_TIMEOUT_MS, out uiTransmitted);
                    Console.WriteLine("CaptureBitMap: ArmCapture");
                    b1_writer.Write(CmdArmCapture, LONG_TIMEOUT_MS, out uiTransmitted);
                }

                for (int j = 0; j < (iLinesPerFrame / NUM_OF_LINES_PER_USB); j++)
                {
                    if ((eReturn = b2_reader.Read(bReadBuffer, LONG_TIMEOUT_MS, out uiTransmitted)) == ErrorCode.None)
                    {
                        Buffer.BlockCopy(bReadBuffer, 0, RawPixelData, j * NUM_OF_LINES_PER_USB * iNumOfPixels * NUM_OF_BYTES_PER_PIXEL, iTransferSize);
                    }
                    else
                    {
                        if (GetFifoStatus())
                        {
                            Console.WriteLine("Failure on readback, last status of FIFO buffer: {0}", iFifoProcessingCount);
                        }
                        Console.WriteLine("ERROR: Reading Line " + j);
                        bError = true;
                    }
                }

                return RawPixelData;
            }


            /// <summary>
            /// Capture a Spectra from the Sensor
            /// </summary>
            /// <param name="iFramesTransmitted">
            /// Number of Frames to Grab
            /// </param>
            /// <returns>
            /// Unsigned Short Array of Pixels
            /// </returns>
            public static ushort[] CaptureSpectra(int iFramesTransmitted, bool bInternalTrigger, bool bFirst, ref bool bError)
            {
                byte[] bReadBuffer = new byte[iNumOfPixels * NUM_OF_BYTES_PER_PIXEL * NUM_OF_LINES_PER_USB]; //  
                byte[] bReturnBuffer = new byte[iNumOfPixels * NUM_OF_BYTES_PER_PIXEL * NUM_OF_LINES_PER_USB]; //  

                ushort[] uReturnBuffer = new ushort[iNumOfPixels];

                int uiTransmitted;

                ErrorCode eReturn;

                bError = false;


                if (bFirst)
                {
                    Console.WriteLine("CaptureSpectra: ConfigIntegrationTime");
                    b1_writer.Write(CmdConfigIntegrationTime, LONG_TIMEOUT_MS, out uiTransmitted);
                    Console.WriteLine("CaptureSpectra: ResetSequence");
                    b1_writer.Write(CmdResetSequence, LONG_TIMEOUT_MS, out uiTransmitted);
                    Console.WriteLine("CaptureSpectra: ArmCapture");
                    b1_writer.Write(CmdArmCapture, LONG_TIMEOUT_MS, out uiTransmitted);
                }

                for (int j = 0; j < (iLinesPerFrame / NUM_OF_LINES_PER_USB); j++)
                {
                    if (j == (iLinesPerFrame / NUM_OF_LINES_PER_USB) / 2)
                    {
                        // Capture to Display Performed Here
                        if ((eReturn = b2_reader.Read(bReturnBuffer, LONG_TIMEOUT_MS, out uiTransmitted)) != ErrorCode.None)
                        {
                            Console.WriteLine("ERROR: Reading Line " + j);
                            bError = true;
                            return uReturnBuffer;
                        }
                    }
                    else
                    {
                        // Read Out Buffer But Don't Do Anthing With the Data
                        if ((eReturn = b2_reader.Read(bReadBuffer, LONG_TIMEOUT_MS, out uiTransmitted)) != ErrorCode.None)
                        {
                            Console.WriteLine("ERROR: Reading Line " + j);
                            bError = true;
                            return uReturnBuffer;
                        }
                    }
                }

                Buffer.BlockCopy(bReturnBuffer, 0, uReturnBuffer, 0, iNumOfPixels * NUM_OF_BYTES_PER_PIXEL);

                return uReturnBuffer;
            }


            /// <summary>
            /// Disarms the Capture Mode - Disabling the Clock Outputs
            /// </summary>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool ClearProcessingBuffer()
            {
                byte[] bReturnBuffer = new byte[iNumOfPixels * NUM_OF_BYTES_PER_PIXEL * NUM_OF_LINES_PER_USB]; // 

                ErrorCode eReturn;

                if (GetFifoStatus())
                {
                    Console.WriteLine("ClearProcessingBuffer: Found " + iFifoProcessingCount + " Tokens Still in Processing");

                    if (iFifoProcessingCount > 0)
                    {
                        for (int x = 0; x < iFifoProcessingCount; x++)
                        {
                            Console.WriteLine("ClearProcessingBuffer: Clearing Line " + x);

                            if ((eReturn = b2_reader.Read(bReturnBuffer, LONG_TIMEOUT_MS, out int uiTransmitted)) != ErrorCode.None)
                            {
                                Console.WriteLine("ERROR: Removing Line " + x);
                                b2_reader.Reset();
                                return false;
                            }

                            //System.Threading.Thread.Sleep(100);
                        }
                    }
                }

                return true;
            }


            /// <summary>
            /// Disarms the Capture Mode - Disabling the Clock Outputs
            /// </summary>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool ControlBoardSim(int iFramesTransmitted)
            {
                CmdControlBoardSim[1] = (byte)((iFramesTransmitted & 0x000000FF) >> 0);
                CmdControlBoardSim[2] = (byte)((iFramesTransmitted & 0x0000FF00) >> 8);
                CmdControlBoardSim[3] = (byte)((iFramesTransmitted & 0x00FF0000) >> 16);
                CmdControlBoardSim[4] = (byte)((iFramesTransmitted & 0xFF000000) >> 24);

                Console.WriteLine("CaptureSpectra: ControlBoardSim");
                b1_writer.Write(CmdControlBoardSim, LONG_TIMEOUT_MS, out int uiTransmitted);

                return true;
            }


            /// <summary>
            /// Disarms the Capture Mode - Disabling the Clock Outputs
            /// </summary>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool DisarmCapture()
            {
                ErrorCode eReturn;

                try
                {
                    Console.WriteLine("DisarmCapture: Sending Command");
                    eReturn = b1_writer.Write(CmdDisarmCapture, LONG_TIMEOUT_MS, out int uiTransmitted);

                    if (eReturn != ErrorCode.None)
                    {
                        Console.WriteLine("ERROR: Disarming Capture - " + DateTime.Now.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetAdcGain - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetAdcGain - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Retrieve the FIFO Count Status for the Ready, Valid, and Processing FIFOs
            /// </summary>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool GetFifoStatus()
            {
                byte[] bReturnBuffer = new byte[16];
                int[] iReturnBuffer = new int[4];

                ErrorCode eReturn;


                //sUsb.WaitOne();

                CmdRetrieveFifoStatus[0] |= SET_READ_BIT;

                try
                {
                    Console.WriteLine("GetFifoStatus: RetrieveFifoStatus");
                    eReturn = b1_writer.Write(CmdRetrieveFifoStatus, SHORT_TIMEOUT_MS, out int uiTransmitted);

                    if (eReturn == ErrorCode.None)
                    {
                        eReturn = b1_reader.Read(bReturnBuffer, SHORT_TIMEOUT_MS, out uiTransmitted);

                        if (eReturn == ErrorCode.None)
                        {
                            Buffer.BlockCopy(bReturnBuffer, 0, iReturnBuffer, 0, uiTransmitted);

                            iFifoReadyCount = iReturnBuffer[0];
                            iFifoValidCount = iReturnBuffer[1];
                            iFifoProcessingCount = iReturnBuffer[2];
                            iFifoMax = iReturnBuffer[3];
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Retrieving FIFO Status - " + DateTime.Now.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Requesting FIFO Status - " + DateTime.Now.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.RetrieveFifoStatus - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.RetrieveFifoStatus - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Read the Calibration Data (Currently Page 0 Only)
            /// </summary>
            /// <param name="bError">
            /// Returned Error Code
            /// </param>
            /// <returns>
            /// Array of Values
            /// </returns>
            public static byte[] ReadCalibration(ref bool bError)
            {
                byte[] bReturnBuffer = new byte[32];

                ErrorCode eReturn;


                bError = false;

                CmdReadCalibration[0] |= SET_READ_BIT;

                try
                {
                    eReturn = b1_writer.Write(CmdReadCalibration, SHORT_TIMEOUT_MS, out int uiTransmitted);

                    if (eReturn == ErrorCode.None)
                    {

                        eReturn = b1_reader.Read(bReturnBuffer, SHORT_TIMEOUT_MS, out uiTransmitted);

                        if (eReturn != ErrorCode.None)
                        {
                            bError = true;
                        }
                    }
                    else
                    {
                        bError = true;
                    }
                }
                catch (Exception e)
                {
                    bError = true;
                    Console.WriteLine("ERROR Usb.ReadCalibration - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.ReadCalibration - Source : " + e.Source);
                    return bReturnBuffer;
                }

                return bReturnBuffer;
            }


            /// <summary>
            /// Read a Single Line from the Sensor
            /// </summary>
            /// <param name="bError">
            /// Returned Error Code
            /// </param>
            /// <returns>
            /// Status of Success (0) or Failure (0xFF)
            /// </returns>
            public static int ReadLine(ref bool bError)
            {
                byte[] bReturnBuffer = new byte[iNumOfPixels * NUM_OF_BYTES_PER_PIXEL * NUM_OF_LINES_PER_USB]; //  

                ErrorCode eReturn;


                bError = false;

                // Capture to Display Performed Here
                if ((eReturn = b2_reader.Read(bReturnBuffer, LONG_TIMEOUT_MS, out int uiTransmitted)) != ErrorCode.None)
                {
                    Console.WriteLine("ERROR: Reading Line");
                    bError = true;
                    return 0xFF;
                }

                return 0;
            }


            /// <summary>
            /// Set the ADC Gain Value
            /// </summary>
            /// <param name="NewGain">
            /// Raw Setting - Valid Values of 0-63 corresponding to gain of 0.0dB to 15.5dB
            /// </param>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool SetAdcGain(int NewGain)
            {
                byte[] bSend = new byte[3];

                try
                {
                    bSend[0] = (byte)(0x74);
                    bSend[1] = (byte)((NewGain & 0xff));
                    bSend[2] = (byte)(0x2 << 3);

                    Console.WriteLine("SetAdcGain: " + NewGain);
                    b1_writer.Write(bSend, LONG_TIMEOUT_MS, out int uiTransmitted);

                    Console.WriteLine("Set ADC Gain to " + (20.0f * System.Math.Log10(5.9f / (1f + 4.9f * ((63.0f - (float)NewGain) / 63.0f)))) + "dB");
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetAdcGain - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetAdcGain - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Set the ADC Offset Voltage
            /// </summary>
            /// <param name="NewOffset">
            /// Raw Value to Set
            /// </param>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool SetAdcOffset(int NewOffset)
            {
                byte[] bSend = new byte[3];

                try
                {
                    bSend[0] = (byte)(0x74);
                    bSend[1] = (byte)((NewOffset & 0xff));
                    bSend[2] = (byte)((0x6 << 3) + ((NewOffset & 0x100) >> 8));

                    Console.WriteLine("SetAdcOffset: " + NewOffset);
                    b1_writer.Write(bSend, LONG_TIMEOUT_MS, out int uiTransmitted);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetAdcGain - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetAdcGain - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Set the Delay for ADC Conversion
            /// </summary>
            /// <param name="NewDelay">
            /// Number of Clocks
            /// </param>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool SetDelayAdc(int NewDelay)
            {
                byte[] bSend = new byte[6];

                try
                {
                    bSend[0] = (byte)(0x01);
                    bSend[1] = (byte)(0x04);
                    bSend[2] = (byte)(NewDelay);
                    bSend[3] = (byte)(0x00);
                    bSend[4] = (byte)(0x00);
                    bSend[5] = (byte)(0x00);

                    Console.WriteLine("SetDelayAdc: " + NewDelay);
                    b1_writer.Write(bSend, LONG_TIMEOUT_MS, out int uiTransmitted);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetDelayAdc - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetDelayAdc - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Set the Delay of Sensor Conversion
            /// </summary>
            /// <param name="NewOffset">
            /// Number of Clocks
            /// </param>
            /// <returns>
            /// Boolean of Success or Failure
            /// </returns>
            public static bool SetDelaySensor(int NewDelay)
            {
                byte[] bSend = new byte[6];

                try
                {
                    bSend[0] = (byte)(0x01);
                    bSend[1] = (byte)(0x05);
                    bSend[2] = (byte)(NewDelay);
                    bSend[3] = (byte)(0x00);
                    bSend[4] = (byte)(0x00);
                    bSend[5] = (byte)(0x00);

                    Console.WriteLine("SetDelaySensor: " + NewDelay);
                    b1_writer.Write(bSend, LONG_TIMEOUT_MS, out int uiTransmitted);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetDelaySensor - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetDelaySensor - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Set the Integration Time
            /// </summary>
            /// <param name="NewTime">
            /// Valid Range of 0-900
            /// </param>
            /// <returns>
            /// Boolean Returns Success or Failure
            /// </returns>
            public static bool SetIntegrationTime(int NewTime)
            {
                try
                {
                    CmdConfigIntegrationTime[2] = (byte)(NewTime & 0xFF);
                    CmdConfigIntegrationTime[3] = (byte)((NewTime >> 8) & 0xFF);

                    Console.WriteLine("SetIntegrationTime: " + NewTime);
                    b1_writer.Write(CmdConfigIntegrationTime, LONG_TIMEOUT_MS, out int uiTransmitted);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetIntegrationTime - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetIntegrationTime - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Set Lines Per Frame
            /// </summary>
            /// <param name="NewLineCount">
            /// Valid Range of 1 to 512
            /// </param>
            /// <returns>
            /// Boolean Returns Success or Failure
            /// </returns>
            public static bool SetLinesPerFrame(int NewLineCount)
            {
                try
                {
                    CmdConfigLinesPerFrame[2] = (byte)(NewLineCount & 0xFF);
                    CmdConfigLinesPerFrame[3] = (byte)((NewLineCount >> 8) & 0xFF);
                    CmdConfigLinesPerFrame[4] = (byte)((NewLineCount >> 16) & 0xFF);
                    CmdConfigLinesPerFrame[5] = (byte)((NewLineCount >> 24) & 0xFF);

                    Console.WriteLine("SetLinesPerFrame: " + NewLineCount);
                    b1_writer.Write(CmdConfigLinesPerFrame, LONG_TIMEOUT_MS, out int uiTransmitted);

                    iLinesPerFrame = NewLineCount;

                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetLinesPerFrame - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetLinesPerFRame - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Set the Pixel Count per Line
            /// </summary>
            /// <param name="NewValue">
            /// Valid Values of 512, 1024, 2048
            /// </param>
            /// <returns>
            /// Boolean Returns Success or Failure
            /// </returns>
            public static bool SetPixelCount(int NewValue)
            {
                try
                {
                    CmdConfigPixelCount[2] = (byte)(NewValue & 0xFF);
                    CmdConfigPixelCount[3] = (byte)((NewValue >> 8) & 0xFF);

                    Console.WriteLine("SetPixelCount: " + NewValue);
                    b1_writer.Write(CmdConfigPixelCount, LONG_TIMEOUT_MS, out int uiTransmitted);

                    iNumOfPixels = NewValue;

                    iTransferSize = iNumOfPixels * NUM_OF_BYTES_PER_PIXEL * NUM_OF_LINES_PER_USB;
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetPixelCount - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetPixelCount - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Set the Test Pattern
            /// </summary>
            /// <param name="Pattern">Valid Range of 0-8</param>
            /// <param name="bExtTrig">Enable External Trigger or Freerun the Test Pattern</param>
            /// <returns></returns>
            public static bool SetTestPattern(int Pattern, bool bExtTrig)
            {
                try
                {
                    if (Pattern == 255)
                    {
                        CmdTestPattern[1] = 0x00;
                        CmdTestPattern[2] = 0x00;
                    }
                    else
                    {
                        CmdTestPattern[1] = 0x01;
                        CmdTestPattern[2] = (byte)(Pattern & 0xFF);
                    }

                    if (bExtTrig)
                    {
                        CmdTestPattern[1] |= 0x04;
                    }

                    Console.WriteLine("SetTestPattern: Pattern(" + Pattern + ") ExternalTrig(" + bExtTrig + ")");
                    b1_writer.Write(CmdTestPattern, LONG_TIMEOUT_MS, out int uiTransmitted);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetIntegrationTime - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetIntegrationTime - Source : " + e.Source);
                    return false;
                }

                return true;
            }


            /// <summary>
            /// Write Calibration Data
            /// </summary>
            /// <param name="bDataArray">
            /// Array of 32 bytes containing the specified page's data
            /// </param>
            /// <returns>
            /// Boolean Returns Success or Failure
            /// </returns>
            public static bool WriteCalibration(byte bPage, byte[] bDataArray)
            {
                try
                {
                    CmdWriteCalibration[1] = bPage;

                    bDataArray.CopyTo(CmdWriteCalibration, 2);

                    Console.WriteLine("Write Calibration - Page 0: " + BitConverter.ToString(bDataArray));
                    b1_writer.Write(CmdWriteCalibration, LONG_TIMEOUT_MS, out int uiTransmitted);

                    ErrorCode eReturn;
                    int uiTemp;
                    byte[] bReturnBuffer = new byte[iNumOfPixels * NUM_OF_BYTES_PER_PIXEL * NUM_OF_LINES_PER_USB]; //  


                    if ((eReturn = b1_reader.Read(bReturnBuffer, LONG_TIMEOUT_MS, out uiTemp)) == ErrorCode.None)
                    {
                        Console.WriteLine("USB Read Back from EEPROM Write, Success?");
                    }

                    else
                        Console.WriteLine("USB Read Back from EEPROM Write, Failure?");



                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Usb.SetLinesPerFrame - Message: " + e.Message);
                    Console.WriteLine("ERROR Usb.SetLinesPerFRame - Source : " + e.Source);
                    return false;
                }

                return true;
            }
        }

        internal HOCTSpectrometer(UsbRegistry usbReg, int index = 0) : base(usbReg)
        {
            isOCT = true;
            //OctUsb.SetLinesPerFrame(500);
            integrationTimeMS_ = (uint)OctUsb.DefaultIntegrationTime();
        }

        protected CancellationTokenSource _cancellationTokenSource { get; set; } = new CancellationTokenSource();
        ushort[] lastFrame;
        protected object frameLock = new object();
        protected object lineLock = new object();
        bool commsOpen = false;
        protected Task FrameProcess { get; set; } = null;
        const int ONE_BILLION = 1000000000;

        public int sampleLine
        {
            get
            {
                return sampleLine_;
            }
            set
            {
                lock (lineLock)
                {
                    if (value > linesPerFrame)
                        sampleLine_ = linesPerFrame;
                    else if (value < 0)
                        sampleLine_ = 0;
                    else
                        sampleLine_ = value;
                }
            }

        }

        //public int linesPerFrame = 500;

        int sampleLine_ = 100;

        internal override bool open()
        {
            Task<bool> task = Task.Run(async () => await openAsync());
            return task.Result;
        }
        internal override async Task<bool> openAsync()
        {
            if (!commsOpen)
            {
                bool openOk = OctUsb.OpenDevice(0x24AA, 0x5000);

                if (openOk)
                {
                    linesPerFrame = 500;
                    OctUsb.SetDelayAdc(3);
                    OctUsb.SetLinesPerFrame(linesPerFrame);
                    
                    //
                    // This discrepency probably seems strange but it exists for a reason.
                    //
                    // The HOCT unit has a 2048 pixel detector but only the first 1024 of them are actually exposed to light.
                    // There is hardware level support for sending out frames and spectra that are only 1024 pixels wide,
                    // however, doing this leads to untenable amounts of lag in feedback (on the order of seconds vs. fractions
                    // of a second if we set to 2048). It also defaults to 1024 in the class above so we must manually set
                    // it to 2048.
                    //
                    // It is worth discussing whether the pixels should be set to 2048 instead of 1024 and allow higher level
                    // software to deal with the dead detector. As of this writing there is a bit of a discrepancy: getSectrum
                    // returns an array that is 1024 pixels wide while getFrame returns frames that are 2048 pixels wide, i.e.
                    // none are thrown away. Consistency between the two (either throwing away in both, or surfacing all pixels for both)
                    // is probably preferable. 
                    //
                    OctUsb.SetPixelCount(2048);
                    pixels = (uint)1024;

                    eeprom = new HOCTEEPROM(this);
                    if (!(await eeprom.readAsync()))
                    {
                        logger.error("Spectrometer: failed to GET_MODEL_CONFIG");
                        return false;
                    }

                    FrameProcess = Task.Run(() => collectFrames(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
                }

                commsOpen = openOk;
                regenerateWavelengths();
                return openOk;
            }

            else
                return false;

        }

        private void collectFrames(CancellationToken CT)
        {
            lock (acquisitionLock)
            {
                OctUsb.SetIntegrationTime((int)integrationTimeMS_);
            }

            // To-Do: find out from Jesse or other EE what in the world this command does and why this thing only
            // seems to work when we set it very very high
            OctUsb.ControlBoardSim(ONE_BILLION);

            ushort[] RawPixelData = Enumerable.Repeat((ushort)0, OctUsb.iNumOfPixels).ToArray();
            int iFramesTransmitted = 1;

            bool readError = false;
            bool firstFrame = true;

            while (!CT.IsCancellationRequested)
            {
                lock (acquisitionLock)
                {
                    RawPixelData = OctUsb.CaptureBitMap(iFramesTransmitted, true, firstFrame, ref readError);
                }
                if (readError == false)
                {
                    lock (frameLock)
                        lastFrame = RawPixelData;
                    firstFrame = false;
                }
                
                Thread.Sleep(10);
            }

            OctUsb.ControlBoardSim(0);
            OctUsb.DisarmCapture();
            OctUsb.ClearProcessingBuffer();
        }



        public override void close()
        {
            Task task = Task.Run(async () => await closeAsync());
            task.Wait();
        }
        public async override Task closeAsync()
        {
            if (commsOpen)
            {
                _cancellationTokenSource.Cancel();

                await FrameProcess;

                bool closeOk = await Task.Run(() => OctUsb.CloseDevice());
                if (closeOk)
                    commsOpen = false;

            }
        }

        public override double[] getSpectrum(bool forceNew = false)
        {
            Task<double[]> task = Task.Run(async () => await getSpectrumAsync(forceNew));
            return task.Result;
        }
        public override async Task<double[]> getSpectrumAsync(bool forceNew = false)
        {
            if (forceNew)
            {
                // 11.25 is based on the HOCT clock rate (11.25), and dividing by 5 is for *200 lines per frame and /1000
                // to convert from us to ms
                // The line rate is at least 100 us, in practice we've seen returns take up to ~60-70 ms, hard to say where exactly
                // all the delay comes from. 20 ms is theoretical minimum. We give an extra buffer for long reads then an extra
                // 10 for the frame loop's sleep
                //
                // May reduce the minimum or change the frame vs. usb split with more experimentation
                int minWait = 20;

                int wait = (int)((integrationTimeMS / 11.25) / 5);
                if (wait < minWait)
                    wait = minWait;

                //give time for loop wait and usb read back
                wait += 150;

                Thread.Sleep(wait);
            }

            Task<ushort[]> frameTask = Task.Run(() => getFrame());

            ushort[] RawPixelData = await frameTask;
            double[] data = new double[pixels];

            lock (lineLock)
            {
                if (RawPixelData != null)
                {
                    for (int i = 0; i < pixels; ++i)
                        data[i] = RawPixelData[i + (sampleLine_ * 1024)];
                }
            }
            return data;  
        }

        public override ushort[] getFrame()
        {
            lock (frameLock)
            {
                ushort[] cutFrame = new ushort[lastFrame.Length / 2];
                for (int i = 0; i < linesPerFrame; ++i)
                {
                    for (int j = 0; j < 1024; ++j)
                    {
                        
                        if (j < 5)
                            cutFrame[j + i * 1024] = (ushort)(lastFrame[5 + i * 2048] + j);
                        else
                            cutFrame[j + i * 1024] = lastFrame[j + i * 2048];
                    }
                }

                return cutFrame;
            }
        }

        public override string serialNumber
        {
            get { return eeprom.serialNumber; }
        }

        public override float detectorGain
        {
            get 
            { 
                return 0.0f; 
            }
            set 
            {
                
            }
        }

        public override float detectorGainOdd
        {
            get
            {
                return 0.0f;
            }
            set
            {

            }
        }

        public override short detectorOffset
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        public override short detectorOffsetOdd
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        public override bool highGainModeEnabled
        {
            get { return false; }
            set { return; }
        }

        public override bool detectorTECEnabled
        {
            get
            {
                return false;
            }
            set
            {

            }
        }

        public override ushort detectorTECSetpointRaw
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }
        public override float detectorTemperatureDegC
        { 
            get
            {
                return 0.0f;
            }
        }

        public override string firmwareRevision
        {
            get
            {
                return "";
            }
        }

        public override string fpgaRevision
        {
            get
            {
                return "";
            }
            
        }

        public override uint integrationTimeMS
        {
            get
            {
                return integrationTimeMS_;
            }
            set
            {
                lock (acquisitionLock)
                {
                    bool ok = OctUsb.SetIntegrationTime((int)value);
                    if (ok)
                        integrationTimeMS_ = (uint)value;
                }
            }
        }

        public override bool isARM => false;
        public override bool isInGaAs => false;
        public override float excitationWavelengthNM
        {
            get => 840f;
            set { }
        }

        public override bool laserEnabled // dangerous one to cache...
        {
            get
            {
                return false;
            }
            set
            {

            }
        }

        public override ushort laserTemperatureRaw
        {
            get
            {
                return 0;
            }
            
        }

        public override TRIGGER_SOURCE triggerSource
        {
            get
            {
                return TRIGGER_SOURCE.INTERNAL;
            }
            set
            {

            }
        }


        }
}
