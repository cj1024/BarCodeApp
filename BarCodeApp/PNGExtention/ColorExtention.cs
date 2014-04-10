using System;
using System.Windows.Media;

namespace CtripWP7.PNGExtention
{
    public static class ColorExtention
    {

        public static byte[] GetColorByteARGB(this Color color)
        {
            return new[] { color.A, color.R, color.G, color.B };
        }

        public static byte[] GetColorByteRGBA(this Color color)
        {
            return new[] { color.R, color.G, color.B, color.A };
        }

        public static byte[] GetColorByteARGB(this int color)
        {
            var bytes = BitConverter.GetBytes(color);
            var array = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                array[i] = bytes[4 - i - 1];
            }
            return array;
        }

        public static byte[] GetColorByteRGBA(this int color)
        {
            var array = color.GetColorByteARGB();
            var result = new byte[4];
            result[0] = array[1];
            result[1] = array[2];
            result[2] = array[3];
            result[3] = array[0];
            return result;
        }

        public static byte[] GetColorByte(this int[] pixels)
        {
            var result = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i].GetColorByteRGBA();
                pixel.CopyTo(result, (i + 1)*4 - pixel.Length);
            }
            return result;
        }

        public static int GetColorInteger(this Color color)
        {
            return color.GetColorByteARGB().GetColorInteger();
        }

        public static int GetColorInteger(this byte[] color)
        {
            var array = new byte[color.Length];
            for (int i = 0; i < color.Length; i++)
            {
                array[i] = color[color.Length - i - 1];
            }
            return BitConverter.ToInt32(array, 0);
        }

        public static Color GetColor(this byte[] color)
        {
            if (color.Length != 4 && color.Length != 3) throw new ArgumentOutOfRangeException("color");
            var array = new byte[4];
            if (color.Length == 3)
            {
                color.CopyTo(array, 1);
                array[0] = 0xff;
            }
            else
            {
                color.CopyTo(array, 0);
            }
            return Color.FromArgb(array[0], array[1], array[2], array[3]);
        }

        public static Color GetColor(this int color)
        {
            return color.GetColorByteARGB().GetColor();
        }

    }

}
