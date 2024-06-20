namespace WasatchNET
{
    public class AndorEEPROMJSON
    {
        public string detector_serial_number;
        public string detector_type;
        public double[] wavelength_coeffs;
        public double excitation_nm_float;
        public string wp_serial_number;
        public string wp_model;
        public bool invert_x_axis;
        public double[] raman_intensity_coeffs;
        public uint roi_horizontal_start;
        public uint roi_horizontal_end;
    }
}
