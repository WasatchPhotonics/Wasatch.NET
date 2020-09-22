using System.Windows.Forms;

namespace WinFormDemo
{
    class DemoUtil
    {
        public static void expandNUD(NumericUpDown nud, int value)
        {
            if (nud.Minimum > value)
                nud.Minimum = value;
            if (nud.Maximum < value)
                nud.Maximum = value;
            nud.Value = value;
        }
    }
}
