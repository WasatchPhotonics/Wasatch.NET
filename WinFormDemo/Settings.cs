using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using WasatchNET;

namespace WinFormDemo
{
    /// <summary>
    /// Encapsulates the mapping and updates of the TreeView on the main GUI.
    /// </summary>
    ///
    /// <remarks>
    /// It would be nice to use an enumeration instead of strings as the key 
    /// field, to reduce bugs introduced by typos and the like. It was for
    /// that reason that the enumeration WasatchNET.Opcodes was created.
    ///
    /// However, a significant number of settings aren't associated with a
    /// unique Opcode, but rather come from parsed EEPROM, 
    /// FPGACompilationOptions etc. Maybe we'll make those into their own
    /// classes someday and we can add a little rigor here, but for now you
    /// get strings :-/
    /// </remarks>
    public class Settings
    {
        Dictionary<string, Tuple<TreeNode, string>> treeNodes = new Dictionary<string, Tuple<TreeNode, string>>();
        TreeView tv;
        Logger logger = Logger.getInstance();

        public delegate T MyDelegate<T>();
        AutoResetEvent autoResetEvent = new AutoResetEvent(true);

        public Settings(TreeView treeView)
        {
            tv = treeView;
            stubAll();
        }

        void stubAll()
        {
            // note that while TreeView nodes CAN be sorted, they currently AREN'T,
            // so insertion order is physical display order

            ////////////////////////////////////////////////////////////////////
            // EEPROM
            ////////////////////////////////////////////////////////////////////

            stub("serialNumber",                "EEPROM/Page 0/Serial Number");
            stub("model",                       "EEPROM/Page 0/Model");
            stub("baudRate",                    "EEPROM/Page 0/Baud Rate");
            stub("hasCooling",                  "EEPROM/Page 0/Features/Cooling");
            stub("hasBattery",                  "EEPROM/Page 0/Features/Battery");
            stub("hasLaser",                    "EEPROM/Page 0/Features/Laser");
            stub("excitationNM",                "EEPROM/Page 0/Excitation (nm)");
            stub("slitSizeUM",                  "EEPROM/Page 0/Slit Size (µm)");
            stub("startupIntegrationTimeMS",    "EEPROM/Page 0/Startup/Integration Time (ms)");
            stub("startupTemperatureDegC",      "EEPROM/Page 0/Startup/Temperature (°C)");
            stub("startupTriggerScheme",        "EEPROM/Page 0/Startup/Trigger Scheme");
            stub("detectorGain",                "EEPROM/Page 0/Detector Gain/value (InGaAs even pixels)");
            stub("detectorGainOdd",             "EEPROM/Page 0/Detector Gain/odd pixels (InGaAs only)");
            stub("detectorOffset",              "EEPROM/Page 0/Detector Offset/value (InGaAs even pixels)");
            stub("detectorOffsetOdd",           "EEPROM/Page 0/Detector Offset/odd pixels (InGaAs only)");

            for (int i = 0; i < 5; i++)
                stub("wavecalCoeff" + i,        "EEPROM/Page 1/Wavelength Calibration/Coeff" + i);
            for (int i = 0; i < 3; i++)
                stub("degCToDACCoeff" + i,      "EEPROM/Page 1/Detector TEC Calibration (°C->DAC)/Coeff" + i);
            stub("detectorTempMax",             "EEPROM/Page 1/Detector Temperature (°C)/Max");
            stub("detectorTempMin",             "EEPROM/Page 1/Detector Temperature (°C)/Min");
            for (int i = 0; i < 3; i++)
                stub("adcToDegCCoeff" + i,      "EEPROM/Page 1/Thermistor/Calibration (ADC->°C)/Coeff" + i);
            stub("thermistorResistanceAt298K",  "EEPROM/Page 1/Thermistor/Ω@298K");
            stub("thermistorBeta",              "EEPROM/Page 1/Thermistor/β");
            stub("calibrationDate",             "EEPROM/Page 1/Calibration/Date");
            stub("calibrationBy",               "EEPROM/Page 1/Calibration/By");
                                                
            stub("detector",                    "EEPROM/Page 2/Detector");
            stub("activePixelsHoriz",           "EEPROM/Page 2/Active Pixels/Horizontal");
            stub("activePixelsVert",            "EEPROM/Page 2/Active Pixels/Vertical");
            stub("actualPixelsHoriz",           "EEPROM/Page 2/Actual Pixels Horizontal");
            stub("ROIHorizStart",               "EEPROM/Page 2/ROI Horizontal/Start");
            stub("ROIHorizEnd",                 "EEPROM/Page 2/ROI Horizontal/End");
            for (int i = 1; i < 4; i++)
            {
                stub(String.Format("ROIVertRegion{0}Start", i), String.Format("EEPROM/Page 2/ROI Vertical Region {0}/Start", i));
                stub(String.Format("ROIVertRegion{0}End",   i), String.Format("EEPROM/Page 2/ROI Vertical Region {0}/End",   i));
            }
            for (int i = 0; i < 5; i++)
                stub("linearityCoeff" + i,      "EEPROM/Page 2/Linearity/Coeff" + i);

            stub("maxLaserPowerMW",             "EEPROM/Page 3/Laser Power (mW)/Max");
            stub("minLaserPowerMW",             "EEPROM/Page 3/Laser Power (mW)/Min");
            for (int i = 0; i < 4; i++)
                stub("laserPowerCoeff" + i,     "EEPROM/Page 3/Laser Power Calibration (% -> mW)/Coeff" + i);
            stub("minIntegrationTimeMS",        "EEPROM/Page 3/Integration Time (ms)/Min");
            stub("maxIntegrationTimeMS",        "EEPROM/Page 3/Integration Time (ms)/Max");

            stub("userText",                    "EEPROM/Page 4/User Text");

            for (int i = 0; i < 15; i++)
                stub("badPixels" + i,           "EEPROM/Page 5/Bad Pixels/Index " + i);
            stub("productConfiguration",        "EEPROM/Page 5/Product Configuration");

            ////////////////////////////////////////////////////////////////////
            // FPGA Compilation Options
            ////////////////////////////////////////////////////////////////////

            stub("fpgaDataHeader",                "FPGA Compilation Options/Data Header");
            stub("fpgaHasCFSelect",               "FPGA Compilation Options/Has CF Select");
            stub("fpgaLaserType",                 "FPGA Compilation Options/Laser Type");
            stub("fpgaLaserControl",              "FPGA Compilation Options/Laser Control");
            stub("fpgaHasAreaScan",               "FPGA Compilation Options/Has Area Scan");
            stub("fpgaHasActualIntegTime",        "FPGA Compilation Options/Has Actual Integration Time");
            stub("fpgaHasHorizBinning",           "FPGA Compilation Options/Has Horiz Binning");
            stub("fpgaIntegrationTimeResolution", "FPGA Compilation Options/Integ Time Resolution");

            ////////////////////////////////////////////////////////////////////
            // Miscellaneous
            ////////////////////////////////////////////////////////////////////

            stub("featureBoardType",            "Hardware/Board Type");
            stub("featureDesc",                 "Hardware/Description");
            stub("VID",                         "Hardware/USB/VID");
            stub("PID",                         "Hardware/USB/PID");

            ////////////////////////////////////////////////////////////////////
            // Firmware Versions
            ////////////////////////////////////////////////////////////////////

            stub("firmwareRev",                 "Firmware/Microcontroller");
            stub("fpgaRev",                     "Firmware/FPGA");

            ////////////////////////////////////////////////////////////////////
            // Spectrometer State
            ////////////////////////////////////////////////////////////////////

            stub("integrationTimeMS",           "Spectrometer State/Acquisition/Integration Time (ms)");
            stub("actualIntegrationTimeUS",     "Spectrometer State/Acquisition/Integration/Actual (µs)");
            stub("frame",                       "Spectrometer State/Acquisition/Frame");
                                                
            stub("detectorTemperatureDegC",     "Spectrometer State/Detector/TEC/Temperature/°C");
            stub("detectorTemperatureRaw",      "Spectrometer State/Detector/TEC/Temperature/raw");
            stub("detectorTECEnabled",          "Spectrometer State/Detector/TEC/Enabled");
            stub("detectorTECSetpointDegC",     "Spectrometer State/Detector/TEC/Setpoint/°C");
            stub("detectorTECSetpointRaw",      "Spectrometer State/Detector/TEC/Setpoint/raw");
            stub("detectorSensingThresholdEnabled","Spectrometer State/Detector/Sensing/Enabled");
            stub("detectorSensingThreshold",    "Spectrometer State/Detector/Sensing/Threshold");
            stub("horizBinning",                "Spectrometer State/Detector/Horizontal Binning/Mode");
            stub("areaScanEnabled",             "Spectrometer State/Detector/Area Scan Enabled");
                                                
            stub("laserEnabled",                "Spectrometer State/Laser/Enabled");
            stub("laserTemperatureDegC",        "Spectrometer State/Laser/TEC/Temperature (°C)");
            stub("laserTemperatureRaw",         "Spectrometer State/Laser/TEC/Temperature (raw)");
          //stub("laserTemperatureSetpointRaw", "Spectrometer State/Laser/TEC/Setpoint (raw)"); 
            stub("laserModEnabled",             "Spectrometer State/Laser/Modulation/Enabled");
            stub("laserModDuration",            "Spectrometer State/Laser/Modulation/Duration (µs)");
            stub("laserModPeriod",              "Spectrometer State/Laser/Modulation/Period (µs)");
            stub("laserModPulseDelay",          "Spectrometer State/Laser/Modulation/Pulse Delay (µs)");
            stub("laserModPulseWidth",          "Spectrometer State/Laser/Modulation/Pulse Width (µs)");
            stub("laserModLinkedToIntegrationTime", 
                                                "Spectrometer State/Laser/Modulation/Linked to Integration Time");
            stub("laserRampingEnabled",         "Spectrometer State/Laser/Ramping Enabled");
            stub("laserInterlock",              "Spectrometer State/Laser/Interlock");

            stub("triggerSource",               "Spectrometer State/Triggering/Source");
            stub("triggerOutput",               "Spectrometer State/Triggering/Output");
            stub("triggerDelay",                "Spectrometer State/Triggering/Delay (us)");
            stub("continuousAcquisition",       "Spectrometer State/Triggering/Continuous/Enabled");
            stub("continuousFrames",            "Spectrometer State/Triggering/Continuous/Frames");

            stub("batteryPercentage",           "Spectrometer State/Battery/Charge Level (%)");
            stub("batteryCharging",             "Spectrometer State/Battery/Currently Charging");
        }

        public void update(Spectrometer spec)
        {
            if (spec == null)
                return;

            updateFast(spec);
        }

        /// <summary>
        /// Update the TreeView with all those parameters which are cached in memory for high-speed access.
        /// </summary>
        /// <param name="spec">The spectrometer whose settings to render on the TreeView</param>
        void updateFast(Spectrometer spec)
        {
            if (spec == null)
                return;

            update("activePixelsHoriz",             spec.eeprom.activePixelsHoriz);
            update("activePixelsVert",              spec.eeprom.activePixelsVert);
            update("actualPixelsHoriz",             spec.eeprom.actualPixelsHoriz);
            update("baudRate",                      spec.eeprom.baudRate);
            update("calibrationBy",                 spec.eeprom.calibrationBy);
            update("calibrationDate",               spec.eeprom.calibrationDate);
            update("detector",                      spec.eeprom.detectorName);
            update("detectorTempMax",               spec.eeprom.detectorTempMax);
            update("detectorTempMin",               spec.eeprom.detectorTempMin);
            update("excitationNM",                  spec.excitationWavelengthNM);
            update("featureBoardType",              spec.featureIdentification.boardType);
            update("featureDesc",                   spec.featureIdentification.firmwareDesc);
            update("VID",                           string.Format("0x{0:x4}", spec.featureIdentification.vid));
            update("PID",                           string.Format("0x{0:x4}", spec.featureIdentification.pid));
            update("fpgaDataHeader",                spec.fpgaOptions.dataHeader);
            update("fpgaHasActualIntegTime",        spec.fpgaOptions.hasActualIntegTime);
            update("fpgaHasAreaScan",               spec.fpgaOptions.hasAreaScan);
            update("fpgaHasCFSelect",               spec.fpgaOptions.hasCFSelect);
            update("fpgaHasHorizBinning",           spec.fpgaOptions.hasHorizBinning);
            update("fpgaIntegrationTimeResolution", spec.fpgaOptions.integrationTimeResolution);
            update("fpgaLaserControl",              spec.fpgaOptions.laserControl);
            update("fpgaLaserType",                 spec.fpgaOptions.laserType);
            update("hasBattery",                    spec.eeprom.hasBattery);
            update("hasCooling",                    spec.eeprom.hasCooling);
            update("hasLaser",                      spec.eeprom.hasLaser);
            update("maxIntegrationTimeMS",          spec.eeprom.maxIntegrationTimeMS);
            update("minIntegrationTimeMS",          spec.eeprom.minIntegrationTimeMS);
            update("model",                         spec.model);
            update("ROIHorizEnd",                   spec.eeprom.ROIHorizEnd);
            update("ROIHorizStart",                 spec.eeprom.ROIHorizStart);
            update("serialNumber",                  spec.serialNumber);
            update("slitSizeUM",                    spec.eeprom.slitSizeUM);
            update("thermistorBeta",                spec.eeprom.thermistorResistanceAt298K);
            update("thermistorResistanceAt298K",    spec.eeprom.thermistorResistanceAt298K);
            update("userText",                      spec.eeprom.userText);
            update("maxLaserPowerMW",               spec.eeprom.maxLaserPowerMW);
            update("minLaserPowerMW",               spec.eeprom.minLaserPowerMW);
            update("productConfiguration",          spec.eeprom.productConfiguration);

            // arrays
            for (int i = 0; i < spec.eeprom.wavecalCoeffs.Length; i++)
                update("wavecalCoeff" + i, spec.eeprom.wavecalCoeffs[i]);
            for (int i = 0; i < spec.eeprom.degCToDACCoeffs.Length; i++)
                update("degCToDACCoeff" + i, spec.eeprom.degCToDACCoeffs[i]);
            for (int i = 0; i < spec.eeprom.adcToDegCCoeffs.Length; i++)
                update("adcToDegCCoeff" + i, spec.eeprom.adcToDegCCoeffs[i]);
            for (int i = 0; i < spec.eeprom.ROIVertRegionStart.Length; i++)
                update(String.Format("ROIVertRegion{0}Start", i + 1), spec.eeprom.ROIVertRegionStart[i]);
            for (int i = 0; i < spec.eeprom.ROIVertRegionEnd.Length; i++)
                update(String.Format("ROIVertRegion{0}End", i + 1), spec.eeprom.ROIVertRegionEnd[i]);
            for (int i = 0; i < spec.eeprom.linearityCoeffs.Length; i++)
                update("linearityCoeff" + i, spec.eeprom.linearityCoeffs[i]);
            for (int i = 0; i < spec.eeprom.badPixels.Length; i++)
                update("badPixels" + i, spec.eeprom.badPixels[i] == -1 ? "" : spec.eeprom.badPixels[i].ToString());
            for (int i = 0; i < spec.eeprom.laserPowerCoeffs.Length; i++)
                update("laserPowerCoeff" + i, spec.eeprom.laserPowerCoeffs[i]);
        }

        /// <summary>
        /// Queries all remaining spectrometer settings (those which are not pre-loaded
        /// for fast access from the EEPROM and FPGACompilationOptions). 
        /// 
        /// This can take a few seconds when errors occur, which is why we're doing it
        /// from a background thread.
        /// </summary>
        /// <param name="spec">Spectrometer from which to load the settings</param>
        public void updateAll(Spectrometer spec)
        {
            // TODO: still figuring out what's wrong with these...
            //
            // 2017-10-03 16:49:57.789:  ERROR: getCmd: failed to get GET_TRIGGER_DELAY (0xab) with index 0x0000 via DEVICE_TO_HOST (0 bytes read)
            // 2017-10-03 16:49:58.790:  ERROR: getCmd: failed to get GET_DETECTOR_TEMPERATURE_SETPOINT (0xd9) with index 0x0001 via DEVICE_TO_HOST (0 bytes read)
            // 2017-10-03 16:49:59.794:  ERROR: getCmd: failed to get GET_EXTERNAL_TRIGGER_OUTPUT (0xe1) with index 0x0000 via DEVICE_TO_HOST (0 bytes read)
            // 2017-10-03 16:50:00.798:  ERROR: getCmd: failed to get GET_ACTUAL_FRAMES (0xe4) with index 0x0000 via DEVICE_TO_HOST (0 bytes read)
            //
            // update("triggerDelay", spec.triggerDelay);
            // update("triggerOutput", spec.triggerOutput);
            // update("frame", spec.actualFrames);

            logger.debug("updateAll: calling updateFast");
            updateFast(spec);

            logger.debug("updateAll: starting long pull");

            update("detectorGain",                        spec.detectorGain);   // should this be eeprom or opcode?
            update("detectorOffset",                      spec.detectorOffset); // eeprom or opcode?
            update("detectorGainOdd",                     spec.eeprom.detectorGainOdd);
            update("detectorOffsetOdd",                   spec.eeprom.detectorOffsetOdd);
            update("detectorSensingThreshold",            spec.detectorSensingThreshold);
            update("detectorSensingThresholdEnabled",     spec.detectorSensingThresholdEnabled);
            update("triggerSource",                       spec.triggerSource); 
            update("firmwareRev",                         spec.firmwareRevision);
            update("fpgaRev",                             spec.fpgaRevision);
            update("integrationTimeMS",                   spec.integrationTimeMS);
            update("continuousAcquisition",               spec.continuousAcquisitionEnable);
            update("continuousFrames",                    spec.continuousFrames);

            if (spec.eeprom.hasCooling)
            {
                update("detectorTemperatureDegC",         spec.detectorTemperatureDegC);
                update("detectorTemperatureRaw",          spec.detectorTemperatureRaw);
                update("detectorTECEnabled",              spec.detectorTECEnabled);
                update("detectorTECSetpointRaw",          spec.detectorTECSetpointRaw);
                update("detectorTECSetpointDegC",         spec.detectorTECSetpointDegC);
            }

            if (spec.fpgaOptions.hasActualIntegTime)
                update("actualIntegrationTimeUS",         spec.actualIntegrationTimeUS);

            if (spec.fpgaOptions.hasHorizBinning)
                update("horizBinning",                    spec.horizontalBinning);

            if (spec.fpgaOptions.hasAreaScan)
                update("areaScanEnabled",                 spec.areaScanEnabled);

            if (spec.eeprom.hasLaser && spec.fpgaOptions.laserType != FPGA_LASER_TYPE.NONE)
            {
                update("laserInterlock",                  spec.laserInterlockEnabled);
                update("laserEnabled",                    spec.laserEnabled);
                update("laserModDuration",                spec.laserModulationDuration);
                update("laserModEnabled",                 spec.laserModulationEnabled);
                update("laserModLinkedToIntegrationTime", spec.laserModulationLinkedToIntegrationTime);
                update("laserModPeriod",                  spec.laserModulationPeriod);
                update("laserModPulseDelay",              spec.laserModulationPulseDelay);
                update("laserModPulseWidth",              spec.laserModulationPulseWidth);
                update("laserRampingEnabled",             spec.laserRampingEnabled);
              //update("laserTemperatureSetpointRaw",     spec.laserTemperatureSetpointRaw);
                update("laserTemperatureRaw",             spec.laserTemperatureRaw);
                update("laserTemperatureDegC",            spec.laserTemperatureDegC);
            }

            if (spec.eeprom.hasBattery)
            {
                update("batteryPercentage", spec.batteryPercentage);
                update("batteryCharging", spec.batteryCharging);
            }
        }

        void update_NOT_USED<T>(string key, Spectrometer spec, MyDelegate<T> func)
        {
            logger.debug("update: directly getting {0} from {1}", key, func);
            T value = func();
            update(key, value);

            // Not currently using this code, but retaining if needed.  Basically,
            // testing with APITest suggested that our ARM comms may have difficulties
            // when piling lots of USB calls consecutively into a single thread; however,
            // those same calls succeeded when individually dispatched as separate events.
            // It may be we don't need to do that here because update(string, object) is
            // already calling a dispatcher with the RESULT of the USB call, even though
            // the USB traffic itself isn't in a thread? I really don't know, but keeping
            // this for posterity.
            //
            // autoResetEvent.WaitOne();
            // logger.debug("update: invoking a delegate for {0}", key);
            // tv.BeginInvoke(new MethodInvoker(delegate { update(key, func()); autoResetEvent.Set(); }));
        }

        void update(string key, object value)
        {
            if (!treeNodes.ContainsKey(key))
            {
                logger.error("updateSetting: unknown key {0}", key);
                return;
            }

            TreeNode node = treeNodes[key].Item1;
            string prefix = treeNodes[key].Item2;

            // dispatch so this can occur on non-GUI thread
            tv.BeginInvoke(new MethodInvoker(delegate { node.Text = String.Format("{0}: {1}", prefix, value); }));
        }

        void stub(string key, string label)
        {
            string[] names = label.Split('/');

            // create or traverse intervening nodes
            TreeNodeCollection children = tv.Nodes;
            for (int i = 0; i < names.Length - 1; i++)
                if (children.ContainsKey(names[i]))
                    children = children[names[i]].Nodes;
                else
                    children = children.Add(names[i], names[i]).Nodes;

            // now create the leaf node
            string prefix = names[names.Length - 1];
            if (!children.ContainsKey(prefix))
            {
                // do we even need to track TreeNode in the dict, since it has a unique key?
                TreeNode node = children.Add(key, prefix);
                treeNodes.Add(key, new Tuple<TreeNode, string>(node, prefix));
            }
            else
            {
                logger.error("stubSetting: label already exists: {0}", label);
            }
        }
    }
}
