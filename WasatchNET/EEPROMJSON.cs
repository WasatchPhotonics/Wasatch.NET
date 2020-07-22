﻿using System;
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

            if (item.Serial != this.Serial)
                return false;
            if (item.Model != this.Model)
                return false;
            if (item.SlitWidth != this.SlitWidth)
                return false;
            if (!floatEq(item.BaudRate, this.BaudRate))
                return false;
            if (item.IncBattery != this.IncBattery)
                return false;
            if (item.IncCooling != this.IncCooling)
                return false;
            if (item.IncLaser != this.IncLaser)
                return false;
            if (item.StartupIntTimeMS != this.StartupIntTimeMS)
                return false;
            if (item.StartupTempC != this.StartupTempC)
                return false;
            if (item.StartupTriggerMode != this.StartupTriggerMode)
                return false;
            if (!floatEq(item.DetectorGain, this.DetectorGain))
                return false;
            if (!floatEq(item.DetectorGainOdd, this.DetectorGainOdd))
                return false;
            if (item.DetectorOffset != this.DetectorOffset)
                return false;
            if (item.DetectorOffsetOdd != this.DetectorOffsetOdd)
                return false;
            if (!floatEq(item.WavecalCoeffs, this.WavecalCoeffs))
                return false;
            if (!floatEq(item.TempToDACCoeffs, this.TempToDACCoeffs))
                return false;
            if (!floatEq(item.ADCToTempCoeffs, this.ADCToTempCoeffs))
                return false;
            if (!floatEq(item.LinearityCoeffs, this.LinearityCoeffs))
                return false;
            if (item.DetectorTempMax != this.DetectorTempMax)
                return false;
            if (item.DetectorTempMin != this.DetectorTempMin)
                return false;
            if (item.ThermistorBeta != this.ThermistorBeta)
                return false;
            if (item.ThermistorResAt298K != this.ThermistorResAt298K)
                return false;
            if (item.CalibrationDate != this.CalibrationDate)
                return false;
            if (item.CalibrationBy != this.CalibrationBy)
                return false;
            if (item.DetectorName != this.DetectorName)
                return false;
            if (item.ActualPixelsHoriz != this.ActualPixelsHoriz)
                return false;
            if (item.ActivePixelsHoriz != this.ActivePixelsHoriz)
                return false;
            if (item.ActivePixelsVert != this.ActivePixelsVert)
                return false;
            if (item.MinIntegrationTimeMS != this.MinIntegrationTimeMS)
                return false;
            if (item.MaxIntegrationTimeMS != this.MaxIntegrationTimeMS)
                return false;
            if (item.ROIHorizStart != this.ROIHorizStart)
                return false;
            if (item.ROIHorizEnd != this.ROIHorizEnd)
                return false;
            if (!intArrayEq(item.ROIVertRegionStarts, this.ROIVertRegionStarts))
                return false;
            if (!intArrayEq(item.ROIVertRegionEnds, this.ROIVertRegionEnds))
                return false;
            if (!floatEq(item.LaserPowerCoeffs, this.LaserPowerCoeffs))
                return false;
            if (!floatEq(item.MaxLaserPowerMW, this.MaxLaserPowerMW))
                return false;
            if (!floatEq(item.MinLaserPowerMW, this.MinLaserPowerMW))
                return false;
            if (!floatEq(item.ExcitationWavelengthNM, this.ExcitationWavelengthNM))
                return false;
            if (!floatEq(item.AvgResolution, this.AvgResolution))
                return false;
            if (!intArrayEq(item.BadPixels, this.BadPixels))
                return false;
            if (item.UserText != this.UserText)
                return false;
            if (item.ProductConfig != this.ProductConfig)
                return false;
            if (item.RelIntCorrOrder != this.RelIntCorrOrder)
                return false;
            if (item.RelIntCorrCoeffs != null)
            {
                if (!floatEq(item.RelIntCorrCoeffs, this.RelIntCorrCoeffs))
                    return false;
            }

            if (item.Bin2x2 != this.Bin2x2)
                return false;
            if (item.FlipXAxis != this.FlipXAxis)
                return false;

            return true;
        }

        bool floatEq(double a, double b, double thresh = 0.00001f)
        {
            if (Math.Abs(a - b) > thresh)
                return false;

            return true;
        }

        bool floatEq(double[] a, double[] b, double thresh = 0.00001f)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; ++i)
                if (!floatEq(a[i], b[i], thresh))
                    return false;
                
            return true;
        }

        bool intArrayEq(int[] a, int[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; ++i)
                if (a[i] != b[i])
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
            
            addField(sb, indent,"StartupIntTimeMS", StartupIntTimeMS);
            addField(sb, indent,"StartupTempC", StartupTempC);
            
            addField(sb, indent,"StartupTriggerMode", StartupTriggerMode);
            
            addField(sb, indent,"DetectorGain", DetectorGain);
            
            addField(sb, indent,"DetectorGainOdd", DetectorGainOdd);
            addField(sb, indent,"DetectorOffset", DetectorOffset);
            
            addField(sb, indent,"DetectorOffsetOdd", DetectorOffsetOdd);
            
            addField(sb, indent,"WavecalCoeffs", WavecalCoeffs);
            
            addField(sb, indent,"TempToDACCoeffs", TempToDACCoeffs);
            addField(sb, indent,"ADCToTempCoeffs", ADCToTempCoeffs);
            addField(sb, indent,"LinearityCoeffs", LinearityCoeffs);
            
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
