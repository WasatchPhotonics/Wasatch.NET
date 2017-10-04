using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// unique Opcode, but rather come from parsed ModelConfig, 
    /// FPGACompilationOptions etc. Maybe we'll make those into their own
    /// classes someday and we can add a little rigor here, but for now you
    /// get strings :-/
    /// </remarks>
    public class Settings
    {
        Dictionary<string, Tuple<TreeNode, string>> treeNodes = new Dictionary<string, Tuple<TreeNode, string>>();
        TreeView tv;
        Logger logger = Logger.getInstance();

        public Settings(TreeView treeView)
        {
            tv = treeView;
            stubAll();
        }

        void stubAll()
        {
            // note that while TreeView nodes CAN be sorted, they currently AREN'T,
            // so insertion order is physical display order

            stub("model",                    "Identity/Model");
            stub("serialNumber",             "Identity/Serial Number");

            stub("firmwareRev",              "Version/Firmware Rev");
            stub("fpgaRev",                  "Version/FPGA Rev");

            stub("slitSizeUM",               "Optics/Slit Size (µm)");

            stub("baudRate",                 "Comms/Baud Rate");

            stub("hasCooling",               "Features/Has Cooling");
            stub("hasLaser",                 "Features/Has Laser");
            stub("hasBattery",               "Features/Has Battery");

            stub("integrationTimeMS",        "Acquisition/Integration Time (ms)");
            stub("actualIntegrationTimeMS",  "Acquisition/Integration/Actual (ms)");
            stub("minIntegrationTimeMS",     "Acquisition/Integration/Min (ms)");
            stub("maxIntegrationTimeMS",     "Acquisition/Integration/Max (ms)");
            stub("frame",                    "Acquisition/Frame");

            stub("wavecalCoeff0",            "Wavecal/Coeff0");
            stub("wavecalCoeff1",            "Wavecal/Coeff1");
            stub("wavecalCoeff2",            "Wavecal/Coeff2");
            stub("wavecalCoeff3",            "Wavecal/Coeff3");

            stub("detectorName",             "Detector/Name");
            stub("detectorTempCoeff0",       "Detector/Temperature Calibration/Coeff0");
            stub("detectorTempCoeff1",       "Detector/Temperature Calibration/Coeff1");
            stub("detectorTempCoeff2",       "Detector/Temperature Calibration/Coeff2");
            stub("detectorTempMin",          "Detector/Temperature Limits/Min");
            stub("detectorTempMax",          "Detector/Temperature Limits/Max");
            stub("pixels",                   "Detector/Pixels/Spectrum Length");
            stub("activePixelsHoriz",        "Detector/Pixels/Active/Horizontal");
            stub("activePixelsVert",         "Detector/Pixels/Active/Vertical");
            stub("actualHoriz",              "Detector/Pixels/Actual/Horizontal");
            stub("ccdTempEnable",            "Detector/TEC/Enabled");
            stub("ccdTempSetpoint",          "Detector/TEC/Setpoint");
            stub("ccdTemp",                  "Detector/TEC/Temperature");
            stub("thermistorResistanceAt298K", "Detector/TEC/Thermistor/Resistance at 298K");
            stub("thermistorBeta",           "Detector/TEC/Thermistor/Beta value");
            stub("ccdOffset",                "Detector/CCD/Offset");
            stub("ccdGain",                  "Detector/CCD/Gain");
            stub("ccdSensingThreshold",      "Detector/CCD/Sensing Threshold");
            stub("ccdThresholdSensingEnabled", "Detector/CCD/Threshold Sensing Enabled");
            stub("horizBinning",             "Detector/Horizontal Binning");
            stub("ROIHorizStart",            "Detector/ROI/Horiz/Start)");
            stub("ROIHorizEnd",              "Detector/ROI/Horiz/End");
            for (int i = 1; i < 4; i++)
            {
                stub(String.Format("ROIVertRegion{0}Start", i), String.Format("Detector/ROI/Vert/Region{0}/Start", i));
                stub(String.Format("ROIVertRegion{0}End",   i), String.Format("Detector/ROI/Vert/Region{0}/End", i));
            }
            for (int i = 0; i < 15; i++)
                stub("badPixels" + i, "Detector/Bad Pixels/Index " + i);

            stub("laserEnabled",             "Laser/Enabled");
            stub("excitationNM",             "Laser/Excitation (nm)");
            stub("dac",                      "Laser/DAC"); // MZ: output power?
            stub("laserTemperature",         "Laser/TEC/Temperature");
            stub("laserTemperatureSetpoint", "Laser/TEC/Setpoint"); 
            stub("laserModEnabled",          "Laser/Modulation/Enabled");
            stub("laserModDuration",         "Laser/Modulation/Duration");
            stub("laserModPeriod",           "Laser/Modulation/Period");
            stub("laserModPulseDelay",       "Laser/Modulation/Pulse Delay");
            stub("laserModPulseWidth",       "Laser/Modulation/Pulse Width");
            stub("laserModLinkedToIntegrationTime", "Laser/Modulation/Linked to Integration Time");
            stub("laserRampingEnabled",      "Laser/Ramping Enabled");
            stub("laserSelection",           "Laser/Selected");
            stub("interlock",                "Laser/Interlock");
            for (int i = 0; i < 3; i++)
                stub("adcCoeff" + i, "Laser/TEC/ADC/Coeff" + i);

            stub("ccdTriggerDelay",          "Triggering/Delay (us)");
            stub("ccdTriggerSource",         "Triggering/Source");
            stub("externalTriggerOutput",    "Triggering/External Output");

            stub("fpgaIntegrationTimeResolution", "FPGA/Integ Time Resolution (enum)");
            stub("fpgaDataHeader",           "FPGA/Data Header");
            stub("fpgaHasCFSelect",          "FPGA/Has CF Select");
            stub("fpgaLaserType",            "FPGA/Laser Type");
            stub("fpgaLaserControl",         "FPGA/Laser Control");
            stub("fpgaHasAreaScan",          "FPGA/Has Area Scan");
            stub("fpgaHasActualIntegTime",   "FPGA/Has Actual Integration Time");
            stub("fpgaHasHorizBinning",      "FPGA/Has Horiz Binning");

            stub("calibrationDate",          "Manufacture/Date");
            stub("calibrationBy",            "Manufacture/Technician");

            stub("userText",                 "User Data/Text");
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
            update("activePixelsHoriz", spec.modelConfig.activePixelsHoriz);
            update("activePixelsVert", spec.modelConfig.activePixelsVert);
            update("actualHoriz", spec.modelConfig.actualHoriz);
            update("baudRate", spec.modelConfig.baudRate);
            update("calibrationBy", spec.modelConfig.calibrationBy);
            update("calibrationDate", spec.modelConfig.calibrationDate);
            update("detectorName", spec.modelConfig.detectorName);
            update("detectorTempMax", spec.modelConfig.detectorTempMax);
            update("detectorTempMin", spec.modelConfig.detectorTempMin);
            update("excitationNM", spec.modelConfig.excitationNM);
            update("fpgaDataHeader", spec.fpgaOptions.dataHeader);
            update("fpgaHasActualIntegTime", spec.fpgaOptions.hasActualIntegTime);
            update("fpgaHasAreaScan", spec.fpgaOptions.hasAreaScan);
            update("fpgaHasCFSelect", spec.fpgaOptions.hasCFSelect);
            update("fpgaHasHorizBinning", spec.fpgaOptions.hasHorizBinning);
            update("fpgaIntegrationTimeResolution", spec.fpgaOptions.integrationTimeResolution);
            update("fpgaLaserControl", spec.fpgaOptions.laserControl);
            update("fpgaLaserType", spec.fpgaOptions.laserType);
            update("hasBattery", spec.modelConfig.hasBattery);
            update("hasCooling", spec.modelConfig.hasCooling);
            update("hasLaser", spec.modelConfig.hasLaser);
            update("maxIntegrationTimeMS", spec.modelConfig.maxIntegrationTimeMS);
            update("minIntegrationTimeMS", spec.modelConfig.minIntegrationTimeMS);
            update("model", spec.model);
            update("pixels", spec.pixels);
            update("ROIHorizEnd", spec.modelConfig.ROIHorizEnd);
            update("ROIHorizStart", spec.modelConfig.ROIHorizStart);
            update("serialNumber", spec.serialNumber);
            update("slitSizeUM", spec.modelConfig.slitSizeUM);
            update("thermistorBeta", spec.modelConfig.thermistorResistanceAt298K);
            update("thermistorResistanceAt298K", spec.modelConfig.thermistorResistanceAt298K);
            update("userText", spec.modelConfig.userText);

            // arrays
            for (int i = 0; i < spec.modelConfig.wavecalCoeffs.Length; i++)
                update("wavecalCoeff" + i, spec.modelConfig.wavecalCoeffs[i]);
            for (int i = 0; i < spec.modelConfig.detectorTempCoeffs.Length; i++)
                update("detectorTempCoeff" + i, spec.modelConfig.detectorTempCoeffs[i]);
            for (int i = 0; i < spec.modelConfig.adcCoeffs.Length; i++)
                update("adcCoeff" + i, spec.modelConfig.adcCoeffs[i]);
            for (int i = 0; i < spec.modelConfig.ROIVertRegionStart.Length; i++)
                update(String.Format("ROIVertRegion{0}Start", i + 1), spec.modelConfig.ROIVertRegionStart[i]);
            for (int i = 0; i < spec.modelConfig.ROIVertRegionEnd.Length; i++)
                update(String.Format("ROIVertRegion{0}End", i + 1), spec.modelConfig.ROIVertRegionEnd[i]);
            for (int i = 0; i < spec.modelConfig.badPixels.Length; i++)
                update("badPixels" + i, spec.modelConfig.badPixels[i] == -1 ? "" : spec.modelConfig.badPixels[i].ToString());
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
            // 2017-10-03 16:49:58.790:  ERROR: getCmd: failed to get GET_CCD_TEMP_SETPOINT (0xd9) with index 0x0001 via DEVICE_TO_HOST (0 bytes read)
            // 2017-10-03 16:49:59.794:  ERROR: getCmd: failed to get GET_EXTERNAL_TRIGGER_OUTPUT (0xe1) with index 0x0000 via DEVICE_TO_HOST (0 bytes read)
            // 2017-10-03 16:50:00.798:  ERROR: getCmd: failed to get GET_ACTUAL_FRAMES (0xe4) with index 0x0000 via DEVICE_TO_HOST (0 bytes read)
            //
            // update("ccdTriggerDelay", spec.getCCDTriggerDelay());
            // update("externalTriggerOutput", spec.getExternalTriggerOutput());
            // update("frame", spec.getActualFrames());
            // update("ccdTempSetpoint", spec.getCCDTempSetpoint());

            updateFast(spec);

            update("ccdGain", spec.getCCDGain());
            update("ccdOffset", spec.getCCDOffset());
            update("ccdSensingThreshold", spec.getCCDSensingThreshold());
            update("ccdThresholdSensingEnabled", spec.getCCDThresholdSensingEnabled());
            update("ccdTriggerSource", spec.getCCDTriggerSource());
            update("dac", spec.getDAC());
            update("firmwareRev", spec.getFirmwareRev());
            update("fpgaRev", spec.getFPGARev());
            update("integrationTimeMS", spec.getIntegrationTimeMS());

            if (spec.modelConfig.hasCooling)
                update("ccdTempEnable", spec.getCCDTempEnabled());

            if (spec.fpgaOptions.hasActualIntegTime)
                update("actualIntegrationTimeMS", spec.getActualIntegrationTime());

            if (spec.fpgaOptions.hasHorizBinning)
                update("horizBinning", spec.getHorizBinning());

            if (spec.modelConfig.hasLaser)
            {
                update("interlock", spec.getInterlockEnabled());
                update("laserEnabled", spec.getLaserEnabled());
                update("laserModDuration", spec.getLaserModulationDuration());
                update("laserModEnabled", spec.getLaserModulationEnabled());
                update("laserModLinkedToIntegrationTime", spec.getLaserModulationLinkedToIntegrationTime());
                update("laserModPeriod", spec.getLaserModulationPeriod());
                update("laserModPulseDelay", spec.getLaserModulationPulseDelay());
                update("laserModPulseWidth", spec.getLaserModulationPulseWidth());
                update("laserRampingEnabled", spec.getLaserRampingEnabled());
                update("laserSelection", spec.getSelectedLaser());
                update("laserTemperatureSetpoint", spec.getLaserTemperatureSetpoint());
            }
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
            {
                string name = names[i];
                if (children.ContainsKey(name))
                {
                    children = children[name].Nodes;
                }
                else
                {
                    children = children.Add(name, name).Nodes;
                }
            }

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