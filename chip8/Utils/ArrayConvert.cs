using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Core.Utils
{
    public abstract class ArrayConvert
    {

        public static byte[] ToByteArray<T>(T[] data, int size)
        {
            var floatArray1 = data;

            var byteArray = new byte[floatArray1.Length * size];
            Buffer.BlockCopy(floatArray1, 0, byteArray, 0, byteArray.Length);

            return byteArray;

        }

        public static byte[] ToByteArray(short[] data)
        {
            return ToByteArray(data, 2);
        }
        public static byte[] ToByteArray(float[] data)
        {
            return ToByteArray(data, 4);
        }
        public static byte[] ToByteArray(double[] data)
        {
            return ToByteArray(data, 8);
        }
        public static byte[] ToByteArray(int[] data)
        {
            return ToByteArray(data, 4);
        }
        
        public static byte[] ToByteArray(Vector4[] data)
        {
            byte[] outData = new byte[data.Length * 4 * 4];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] x = BitConverter.GetBytes(data[i].X);
                byte[] y = BitConverter.GetBytes(data[i].Y);
                byte[] z = BitConverter.GetBytes(data[i].Z);
                byte[] w = BitConverter.GetBytes(data[i].W);
                Buffer.BlockCopy(x, 0, outData, i * 16, 4);
                Buffer.BlockCopy(y, 0, outData, i * 16 + 4, 4);
                Buffer.BlockCopy(z, 0, outData, i * 16 + 8, 4);
                Buffer.BlockCopy(w, 0, outData, i * 16 + 12, 4);
            }
            return outData;
        }
        public static byte[] ToByteArray(Vector3[] data)
        {
            byte[] outData = new byte[data.Length * 4 * 3];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] x = BitConverter.GetBytes(data[i].X);
                byte[] y = BitConverter.GetBytes(data[i].Y);
                byte[] z = BitConverter.GetBytes(data[i].Z);
                Buffer.BlockCopy(x, 0, outData, i * 12, 4);
                Buffer.BlockCopy(y, 0, outData, i * 12 + 4, 4);
                Buffer.BlockCopy(z, 0, outData, i * 12 + 8, 4);
            }
            return outData;
        }
        public static byte[] ToByteArray(Vector2[] data)
        {
            byte[] outData = new byte[data.Length * 4 * 2];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] x = BitConverter.GetBytes(data[i].X);
                byte[] y = BitConverter.GetBytes(data[i].Y);
                Buffer.BlockCopy(x, 0, outData, i * 8, 4);
                Buffer.BlockCopy(y, 0, outData, i * 8 + 4, 4);
            }
            return outData;
        }

        // Only usable if vertex positions is stored in shorts which they are not.
        // May be an option if mesh is static. If mesh is scaled up, short overflow
        // will occur.
#if false
        public static byte[] PositionToNormalizedShort(Vector3[] data, out BoundingBox boundingBox)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            for (int i = 0; i < data.Length; i++)
            {
                Vector3 g = data[i];
                Vector3.ComponentMin(ref min, ref g, out min);
                Vector3.ComponentMax(ref max, ref g, out max);
            }
            boundingBox = new BoundingBox();
            boundingBox.Lower = min;
            boundingBox.Upper = max;
            short[] array = new short[data.Length * 3];
            for (int i = 0; i < data.Length; i++)
            {
                Vector3 newVector = data[i] - boundingBox.Mid;
                Vector3 scale = boundingBox.Upper - boundingBox.Mid;
                newVector.X /= scale.X != 0 ? scale.X : 1f;
                newVector.Y /= scale.Y != 0 ? scale.Y : 1f;
                newVector.Z /= scale.Z != 0 ? scale.Z : 1f;
                array[3 * i + 0] = (short)(newVector.X == 1f ? newVector.X* 32767 : newVector.X * 32768);
                array[3 * i + 1] = (short)(newVector.Y == 1f ? newVector.Y* 32767 : newVector.Y * 32768);
                array[3 * i + 2] = (short)(newVector.Z == 1f ? newVector.Z* 32767 : newVector.Z * 32768);
            }
            return ToByteArray(array);
        }
#endif
        public static byte[] PositionToShort(Vector3[] position, Matrix4 transform)
        {
            short[] array = new short[position.Length * 3];
            for (int i = 0; i < position.Length; i++)
            {
                Vector3 transformed = Vector3.Transform(position[i], transform) * short.MaxValue;
                array[3*i + 0] = (short) transformed.X;
                array[3*i + 1] = (short) transformed.Y;
                array[3*i + 2] = (short) transformed.Z;
            }
            return ToByteArray(array);
        }

        public static byte[] UVToNormalizedShort(Vector2[] data)
        {
            short[] array = new short[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                Vector2 newVector = data[i] * short.MaxValue;
                array[2 * i + 0] = (short)newVector.X;
                array[2 * i + 1] = (short)newVector.Y;
            }
            return ToByteArray(array);
        }

        public static byte[] NormalToPacked(Vector3[] data)
        {
            byte[] array = new byte[4 * data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                uint x = (uint) ((data[i].X * 0.5f + 0.5f) * 1023 - 512);
                uint y = (uint) ((data[i].Y * 0.5f + 0.5f) * 1023 - 512);
                uint z = (uint) ((data[i].Z * 0.5f + 0.5f) * 1023 - 512);
                uint val = 0x4000;
                val |= 0x3FF00000 & (x << 20);
                val |= 0x000FFC00 & (y << 10);
                val |= 0x000003FF & (z << 0);
                byte[] packed = BitConverter.GetBytes(val);
                Buffer.BlockCopy(packed, 0, array, i * 4, 4);
            }
            return array;
        }

    }
}
