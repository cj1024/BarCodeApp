using System.Windows.Media;
using System.Windows.Media.Imaging;
using CtripWP7.PNGExtention;
using com.google.zxing.common;

namespace BarCodeApp.Util
{

    public static class BarcodeEncodeExtention
    {

        /// <summary>
        /// 生成QRCode的WriteableBitmap.Pixels颜色数据
        /// </summary>
        /// <param name="encodedByteMatrix">QRCode数据</param>
        /// <returns></returns>
        public static int[] WriteableBitmapPixels(this ByteMatrix encodedByteMatrix)
        {
            return encodedByteMatrix.WriteableBitmapPixels(Colors.White, Colors.Black, Colors.Red);
        }

        /// <summary>
        /// 生成QRCode的WriteableBitmap.Pixels颜色数据
        /// </summary>
        /// <param name="encodedByteMatrix">QRCode数据</param>
        /// <param name="lightColor">浅色</param>
        /// <param name="darkColor">深色</param>
        /// <param name="oddColor">奇点色（一般用不上）</param>
        /// <returns></returns>
        public static int[] WriteableBitmapPixels(this ByteMatrix encodedByteMatrix, Color lightColor, Color darkColor, Color oddColor)
        {
            var lightInteger = lightColor.GetColorInteger();
            var darkInteger = darkColor.GetColorInteger();
            var oddInteger = oddColor.GetColorInteger();
            var result = new int[encodedByteMatrix.Width * encodedByteMatrix.Height];
            for (int i = 0; i < encodedByteMatrix.Array.Length; i++)
            {
                var lineData = encodedByteMatrix.Array[i];
                for (int j = 0; j < lineData.Length; j++)
                {
                    int color;
                    switch (lineData[j])
                    {
                        case 0:
                            color = darkInteger;
                            break;

                        case 1:
                            color = oddInteger;
                            break;

                        default:
                            color = lightInteger;
                            break;
                    }
                    result[i*encodedByteMatrix.Array.Length + j] = color;
                }
            }
            return result;
        }

    }

    public static class WriteableImageExtention
    {

        /// <summary>
        /// 生成QRCode的WriteableBitmap，只支持UI线程
        /// </summary>
        /// <param name="encodedByteMatrix">QRCode数据</param>
        /// <returns>生成的WriteableBitmap</returns>
        public static WriteableBitmap GenerateWriteableBitmap(this ByteMatrix encodedByteMatrix)
        {
            return encodedByteMatrix.GenerateWriteableBitmap(Colors.White, Colors.Black, Colors.Red);
        }

        /// <summary>
        /// 生成QRCode的WriteableBitmap，只支持UI线程
        /// </summary>
        /// <param name="encodedByteMatrix">QRCode数据</param>
        /// <param name="lightColor">浅色</param>
        /// <param name="darkColor">深色</param>
        /// <param name="oddColor">奇点色（一般用不上）</param>
        /// <returns>生成的WriteableBitmap</returns>
        public static WriteableBitmap GenerateWriteableBitmap(this ByteMatrix encodedByteMatrix, Color lightColor, Color darkColor, Color oddColor)
        {
            var result = new WriteableBitmap(encodedByteMatrix.Width, encodedByteMatrix.Height);
            var pixels = encodedByteMatrix.WriteableBitmapPixels();
            pixels.CopyTo(result.Pixels, 0);
            return result;
        }

    }

}
