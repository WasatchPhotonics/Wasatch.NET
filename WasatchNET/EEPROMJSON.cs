using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasatchNET
{
    public class EEPROMJSON
    {
        public string Serial;
        public string Model;
        public int SlitWidth;
        public double BaudRate;
        public bool IncBattery;
        public bool IncCooling;
        public bool IncLaser;
        public int StartupIntTimeMS;
        public int StartupTempC;
        public int StartupTriggerMode;
        public double DetectorGain;
        public double DetectorGainOdd;
        public int DetectorOffset;
        public int DetectorOffsetOdd;
        public double[] WavecalCoeffs;
        public double[] TempToDACCoeffs;
        public double[] ADCToTempCoeffs;
        public double[] LinearityCoeffs;
        public int DetectorTempMax;
        public int DetectorTempMin;
        public int ThermistorBeta;
        public int ThermistorResAt298K;
        public string CalibrationDate;
        public string CalibrationBy;
        public string DetectorName;
        public int ActualPixelsHoriz;
        public int ActivePixelsHoriz;
        public int ActivePixelsVert;
        public int MinIntegrationTimeMS;
        public int MaxIntegrationTimeMS;
        public int ROIHorizStart;
        public int ROIHorizEnd;
        public int[] ROIVertRegionStarts;
        public int[] ROIVertRegionEnds;
        public double[] LaserPowerCoeffs;
        public double MaxLaserPowerMW;
        public double MinLaserPowerMW;
        public double ExcitationWavelengthNM;
        public double AvgResolution;
        public int[] BadPixels;
        public string UserText;
        public string ProductConfig;
        public int RelIntCorrOrder;
        public double[] RelIntCorrCoeffs;
        public bool Bin2x2;
        public bool FlipXAxis;

        public override bool Equals(object obj)
        {
            var item = obj as EEPROMJSON;

            //public string Serial;
            if (item.Serial != this.Serial)
                return false;
            //public string Model;
            if (item.Model != this.Model)
                return false;
            //public int SlitWidth;
            if (item.SlitWidth != this.SlitWidth)
                return false;
            //public double BaudRate;
            if (Math.Abs(item.BaudRate - this.BaudRate) > 0.00001)
                return false;
            //public bool IncBattery;
            if (item.IncBattery != this.IncBattery)
                return false;
            //public bool IncCooling;
            if (item.IncCooling != this.IncCooling)
                return false;
            //public bool IncLaser;
            if (item.IncLaser != this.IncLaser)
                return false;
            //public int StartupIntTimeMS;
            if (item.StartupIntTimeMS != this.StartupIntTimeMS)
                return false;
            //public int StartupTempC;
            if (item.StartupTempC != this.StartupTempC)
                return false;
            //public int StartupTriggerMode;
            if (item.StartupTriggerMode != this.StartupTriggerMode)
                return false;
            //public double DetectorGain;
            if (Math.Abs(item.DetectorGain - this.DetectorGain) > 0.00001)
                return false;
            //public double DetectorGainOdd;
            if (Math.Abs(item.DetectorGainOdd - this.DetectorGainOdd) > 0.00001)
                return false;
            //public double DetectorOffset;
            if (item.DetectorOffset != this.DetectorOffset)
                return false;
            //public double DetectorOffsetOdd;
            if (item.DetectorOffsetOdd != this.DetectorOffsetOdd)
                return false;
            //public double[] WavecalCoeffs;
            if (item.WavecalCoeffs.Length != this.WavecalCoeffs.Length)
                return false;
            for (int i = 0; i < this.WavecalCoeffs.Length; ++i)
            {
                if (Math.Abs(item.WavecalCoeffs[i] - this.WavecalCoeffs[i]) > 0.00001)
                    return false;
            }
            //public double[] TempToDACCoeffs;
            if (item.TempToDACCoeffs.Length != this.TempToDACCoeffs.Length)
                return false;
            for (int i = 0; i < this.TempToDACCoeffs.Length; ++i)
            {
                if (Math.Abs(item.TempToDACCoeffs[i] - this.TempToDACCoeffs[i]) > 0.00001)
                    return false;
            }
            //public double[] ADCToTempCoeffs;
            if (item.ADCToTempCoeffs.Length != this.ADCToTempCoeffs.Length)
                return false;
            for (int i = 0; i < this.ADCToTempCoeffs.Length; ++i)
            {
                if (Math.Abs(item.ADCToTempCoeffs[i] - this.ADCToTempCoeffs[i]) > 0.00001)
                    return false;
            }
            //public double[] LinearityCoeffs;
            if (item.LinearityCoeffs.Length != this.LinearityCoeffs.Length)
                return false;
            for (int i = 0; i < this.LinearityCoeffs.Length; ++i)
            {
                if (Math.Abs(item.LinearityCoeffs[i] - this.LinearityCoeffs[i]) > 0.00001)
                    return false;
            }
            //public int DetectorTempMax;
            if (item.DetectorTempMax != this.DetectorTempMax)
                return false;
            //public int DetectorTempMin;
            if (item.DetectorTempMin != this.DetectorTempMin)
                return false;
            //public int ThermistorBeta;
            if (item.ThermistorBeta != this.ThermistorBeta)
                return false;
            //public int ThermistorResAt298K;
            if (item.ThermistorResAt298K != this.ThermistorResAt298K)
                return false;
            //public string CalibrationDate;
            if (item.CalibrationDate != this.CalibrationDate)
                return false;
            //public string CalibrationBy;
            if (item.CalibrationBy != this.CalibrationBy)
                return false;
            //public string DetectorName;
            if (item.DetectorName != this.DetectorName)
                return false;
            //public int ActualPixelsHoriz;
            if (item.ActualPixelsHoriz != this.ActualPixelsHoriz)
                return false;
            //public int ActivePixelsHoriz;
            if (item.ActivePixelsHoriz != this.ActivePixelsHoriz)
                return false;
            //public int ActivePixelsVert;
            if (item.ActivePixelsVert != this.ActivePixelsVert)
                return false;
            if (item.MinIntegrationTimeMS != this.MinIntegrationTimeMS)
                return false;
            if (item.MaxIntegrationTimeMS != this.MaxIntegrationTimeMS)
                return false;
            //public int ROIHorizStart;
            if (item.ROIHorizStart != this.ROIHorizStart)
                return false;
            //public int ROIHorizEnd;
            if (item.ROIHorizEnd != this.ROIHorizEnd)
                return false;
            //public double[] ROIVertRegionStarts;
            if (item.ROIVertRegionStarts.Length != this.ROIVertRegionStarts.Length)
                return false;
            for (int i = 0; i < this.ROIVertRegionStarts.Length; ++i)
            {
                if (item.ROIVertRegionStarts[i] != this.ROIVertRegionStarts[i])
                    return false;
            }
            //public double[] ROIVertRegionEnds;
            if (item.ROIVertRegionEnds.Length != this.ROIVertRegionEnds.Length)
                return false;
            for (int i = 0; i < this.ROIVertRegionEnds.Length; ++i)
            {
                if (item.ROIVertRegionEnds[i] != this.ROIVertRegionEnds[i])
                    return false;
            }
            if (item.LaserPowerCoeffs.Length != this.LaserPowerCoeffs.Length)
                return false;
            for (int i = 0; i < this.LaserPowerCoeffs.Length; ++i)
            {
                if (Math.Abs(item.LaserPowerCoeffs[i] - this.LaserPowerCoeffs[i]) > 0.00001)
                    return false;
            }
            //public double MaxLaserPowerMW;
            if (Math.Abs(item.MaxLaserPowerMW - this.MaxLaserPowerMW) > 0.00001)
                return false;
            //public double MinLaserPowerMW;
            if (Math.Abs(item.MinLaserPowerMW - this.MinLaserPowerMW) > 0.00001)
                return false;
            //public double ExcitationWavelengthNM;
            if (Math.Abs(item.ExcitationWavelengthNM - this.ExcitationWavelengthNM) > 0.00001)
                return false;
            if (Math.Abs(item.AvgResolution - this.AvgResolution) > 0.00001)
                return false;
            //public double[] BadPixels;
            if (item.BadPixels.Length != this.BadPixels.Length)
                return false;
            for (int i = 0; i < this.BadPixels.Length; ++i)
            {
                if (item.BadPixels[i] != this.BadPixels[i])
                    return false;
            }
            //public string UserText;
            if (item.UserText != this.UserText)
                return false;
            //public string ProductConfig;
            if (item.ProductConfig != this.ProductConfig)
                return false;
            //public int RelIntCorrOrder;
            if (item.RelIntCorrOrder != this.RelIntCorrOrder)
                return false;
            //public double[] RelIntCorrCoeff;
            if (item.RelIntCorrCoeffs != null)
            {
                if (item.RelIntCorrCoeffs.Length != this.RelIntCorrCoeffs.Length)
                    return false;
                for (int i = 0; i < this.RelIntCorrCoeffs.Length; ++i)
                {
                    if (Math.Abs(item.RelIntCorrCoeffs[i] - this.RelIntCorrCoeffs[i]) > 0.00001)
                        return false;
                }
            }
            //public bool Bin2x2;
            //public bool FlipXAxis;
            if (item.Bin2x2 != this.Bin2x2)
                return false;
            if (item.FlipXAxis != this.FlipXAxis)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 2014834114;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Serial);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Model);
            hashCode = hashCode * -1521134295 + SlitWidth.GetHashCode();
            hashCode = hashCode * -1521134295 + BaudRate.GetHashCode();
            hashCode = hashCode * -1521134295 + IncBattery.GetHashCode();
            hashCode = hashCode * -1521134295 + IncCooling.GetHashCode();
            hashCode = hashCode * -1521134295 + IncLaser.GetHashCode();
            hashCode = hashCode * -1521134295 + StartupIntTimeMS.GetHashCode();
            hashCode = hashCode * -1521134295 + StartupTempC.GetHashCode();
            hashCode = hashCode * -1521134295 + StartupTriggerMode.GetHashCode();
            hashCode = hashCode * -1521134295 + DetectorGain.GetHashCode();
            hashCode = hashCode * -1521134295 + DetectorGainOdd.GetHashCode();
            hashCode = hashCode * -1521134295 + DetectorOffset.GetHashCode();
            hashCode = hashCode * -1521134295 + DetectorOffsetOdd.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<double[]>.Default.GetHashCode(WavecalCoeffs);
            hashCode = hashCode * -1521134295 + EqualityComparer<double[]>.Default.GetHashCode(TempToDACCoeffs);
            hashCode = hashCode * -1521134295 + EqualityComparer<double[]>.Default.GetHashCode(ADCToTempCoeffs);
            hashCode = hashCode * -1521134295 + EqualityComparer<double[]>.Default.GetHashCode(LinearityCoeffs);
            hashCode = hashCode * -1521134295 + DetectorTempMax.GetHashCode();
            hashCode = hashCode * -1521134295 + DetectorTempMin.GetHashCode();
            hashCode = hashCode * -1521134295 + ThermistorBeta.GetHashCode();
            hashCode = hashCode * -1521134295 + ThermistorResAt298K.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CalibrationDate);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CalibrationBy);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DetectorName);
            hashCode = hashCode * -1521134295 + ActualPixelsHoriz.GetHashCode();
            hashCode = hashCode * -1521134295 + ActivePixelsHoriz.GetHashCode();
            hashCode = hashCode * -1521134295 + ActivePixelsVert.GetHashCode();
            hashCode = hashCode * -1521134295 + MinIntegrationTimeMS.GetHashCode();
            hashCode = hashCode * -1521134295 + MaxIntegrationTimeMS.GetHashCode();
            hashCode = hashCode * -1521134295 + ROIHorizStart.GetHashCode();
            hashCode = hashCode * -1521134295 + ROIHorizEnd.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<int[]>.Default.GetHashCode(ROIVertRegionStarts);
            hashCode = hashCode * -1521134295 + EqualityComparer<int[]>.Default.GetHashCode(ROIVertRegionEnds);
            hashCode = hashCode * -1521134295 + EqualityComparer<double[]>.Default.GetHashCode(LaserPowerCoeffs);
            hashCode = hashCode * -1521134295 + MaxLaserPowerMW.GetHashCode();
            hashCode = hashCode * -1521134295 + MinLaserPowerMW.GetHashCode();
            hashCode = hashCode * -1521134295 + ExcitationWavelengthNM.GetHashCode();
            hashCode = hashCode * -1521134295 + AvgResolution.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<int[]>.Default.GetHashCode(BadPixels);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UserText);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProductConfig);
            hashCode = hashCode * -1521134295 + RelIntCorrOrder.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<double[]>.Default.GetHashCode(RelIntCorrCoeffs);
            hashCode = hashCode * -1521134295 + Bin2x2.GetHashCode();
            hashCode = hashCode * -1521134295 + FlipXAxis.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(EEPROMJSON left, EEPROMJSON right)
        {
            return EqualityComparer<EEPROMJSON>.Default.Equals(left, right);
        }

        public static bool operator !=(EEPROMJSON left, EEPROMJSON right)
        {
            return !(left == right);
        }

        public string ToString(int level, int indentSize = 2)
        {
            StringBuilder sb = new StringBuilder();
            string indent = new string(' ', level * indentSize);

            string finalIndent = new string(' ', (level - 1) * indentSize);

            addField(sb, indent,"Serial", Serial);
            addField(sb, indent,"Model", Model);
            addField(sb, indent,"SlitWidth", SlitWidth);
            addField(sb, indent,"BaudRate", BaudRate);
            addField(sb, indent,"IncBattery", IncBattery);
            addField(sb, indent,"IncCooling", IncCooling);
            addField(sb, indent,"IncLaser", IncLaser);
            //
            addField(sb, indent,"StartupIntTimeMS", StartupIntTimeMS);
            addField(sb, indent,"StartupTempC", StartupTempC);
            //
            addField(sb, indent,"StartupTriggerMode", StartupTriggerMode);
            //
            addField(sb, indent,"DetectorGain", DetectorGain);
            //
            addField(sb, indent,"DetectorGainOdd", DetectorGainOdd);
            addField(sb, indent,"DetectorOffset", DetectorOffset);
            //
            addField(sb, indent,"DetectorOffsetOdd", DetectorOffsetOdd);
            //
            addField(sb, indent,"WavecalCoeffs", WavecalCoeffs);
            //
            addField(sb, indent,"TempToDACCoeffs", TempToDACCoeffs);
            addField(sb, indent,"ADCToTempCoeffs", ADCToTempCoeffs);
            addField(sb, indent,"LinearityCoeffs", LinearityCoeffs);
            //
            addField(sb, indent,"DetectorTempMax", DetectorTempMax);
            addField(sb, indent,"DetectorTempMin", DetectorTempMin);
            addField(sb, indent,"ThermistorBeta", ThermistorBeta);
            addField(sb, indent,"ThermistorResAt298K", ThermistorResAt298K);
            addField(sb, indent,"CalibrationDate", CalibrationDate);
            addField(sb, indent,"CalibrationBy", CalibrationBy);
            
            addField(sb, indent,"DetectorName", DetectorName);
            addField(sb, indent,"ActualPixelsHoriz", ActualPixelsHoriz);
            addField(sb, indent,"ActivePixelsHoriz", ActivePixelsHoriz);
            addField(sb, indent,"ActivePixelsVert", ActivePixelsVert);
            addField(sb, indent,"MinIntegrationTimeMS", MinIntegrationTimeMS);
            addField(sb, indent,"MaxIntegrationTimeMS", MaxIntegrationTimeMS);
            addField(sb, indent,"ROIHorizStart", ROIHorizStart);
            addField(sb, indent,"ROIHorizEnd", ROIHorizEnd);
            
            addField(sb, indent,"ROIVertRegionStarts", ROIVertRegionStarts);
            addField(sb, indent,"ROIVertRegionEnds", ROIVertRegionEnds);
            
            addField(sb, indent,"LaserPowerCoeffs", LaserPowerCoeffs);
            
            addField(sb, indent,"MaxLaserPowerMW", MaxLaserPowerMW);
            addField(sb, indent,"MinLaserPowerMW", MinLaserPowerMW);
            addField(sb, indent,"ExcitationWavelengthNM", ExcitationWavelengthNM);
            addField(sb, indent,"AvgResolution", AvgResolution);
            
            addField(sb, indent,"BadPixels", BadPixels);
            
            addField(sb, indent,"UserText", UserText);
            addField(sb, indent,"ProductConfig", ProductConfig);
            
            addField(sb, indent,"RelIntCorrOrder", RelIntCorrOrder);
            if (RelIntCorrCoeffs != null)
                addField(sb, indent,"RelIntCorrCoeff", RelIntCorrCoeffs);
            
            addField(sb, indent,"Bin2x2", Bin2x2);
            sb.AppendFormat("{0}\"{1}\": {2}", indent, "FlipXAxis", FlipXAxis ? "true" : "false");

            return "{\n" + sb.ToString() + "\n" + finalIndent + "}";
        }

        void addField(StringBuilder sb, string indent, string name, string value)
        {
            sb.AppendFormat("{0}\"{1}\": \"{2}\",\n", indent, name, value);
        }

        void addField(StringBuilder sb, string indent, string name, int value)
        {
            sb.AppendFormat("{0}\"{1}\": {2},\n", indent, name, value);
        }

        void addField(StringBuilder sb, string indent, string name, bool value)
        {
            sb.AppendFormat("{0}\"{1}\": {2},\n", indent, name, value ? "true" : "false");
        }

        void addField(StringBuilder sb, string indent, string name, double value)
        {
            sb.AppendFormat("{0}\"{1}\": {2},\n", indent, name, value);
        }

        void addField(StringBuilder sb, string indent, string name, double[] value)
        {
            sb.AppendFormat("{0}\"{1}\": ", indent, name);
            sb.Append("[");
            sb.Append(" " + string.Join(", ", value));
            sb.Append(" ],\n");
        }

        void addField(StringBuilder sb, string indent, string name, int[] value)
        {
            sb.AppendFormat("{0}\"{1}\": [ {2} ],\n", indent, name, string.Join(", ", value));
        }

        public override string ToString()
        {
            return ToString(0);
        }


    }

}
