using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace Core.Graphics
{
    public abstract class Buffer : IDisposable
    {

        protected byte[] data;
        protected long dataLength;
        protected BufferUsageHint bufferUsage;
        protected int id;
        protected BufferTarget bufferTarget;

        protected PrimitiveType drawMode = PrimitiveType.Triangles;

        private bool keepData;

        public PrimitiveType DrawMode
        {
            get { return drawMode; }
            set { drawMode = value; }
        }

        public Buffer(byte[] data, BufferUsageHint bufferUsage, bool keepData, BufferTarget bufferTarget)
        {
            this.data = data;
            this.bufferUsage = bufferUsage;
            this.bufferTarget = bufferTarget;
            this.keepData = keepData;
            dataLength = data.Length;
        }

        public bool IsUploaded { get { return id != 0; } }

        public void Upload()
        {
            if (IsUploaded)
            {
                throw new Exception("Buffer already uploaded.");
            }
            id = GL.GenBuffer();
            if (id == 0)
            {
                throw new Exception("ID returned was 0.");
            }
            Bind();
            GL.BufferData(bufferTarget, new IntPtr(data.Length), data, bufferUsage);
            Unbind();
            if (!keepData)
            {
                data = null;
            }
        }

        public virtual void SubData(byte[] data, int offset, int size = 0)
        {
            if (!IsUploaded)
            {
                throw new Exception("Buffer must be uploded when using SubData().");
            }
            if (bufferUsage == BufferUsageHint.StaticCopy || bufferUsage == BufferUsageHint.StaticDraw ||
                bufferUsage == BufferUsageHint.StaticRead)
            {
                Console.WriteLine("Buffer usage is static. Should be dynamic or stream when using SubData().");
            }
            if ((size == 0 ? data.Length : size) + offset > dataLength)
            {
                throw new Exception("Buffer out of range.");
            }
            Bind();
            if (offset == 0 && data.Length == dataLength && size == 0)
            {
                GL.BufferData(bufferTarget, new IntPtr(data.Length), IntPtr.Zero, bufferUsage); // TODO: Size NULL?
            }
            GL.BufferSubData(bufferTarget, new IntPtr(offset), new IntPtr(size == 0 ? data.Length : size), data);
            if (this.data != null)
            {
                Array.Copy(data, 0, this.data, offset, size > 0 ? size : data.Length);
                //System.Buffer.BlockCopy(data, offset, this.data, offset, size > 0 ? size : data.Length);
                
            }

        }

        public virtual void Draw(int count = 0)
        {
            if (!IsUploaded)
            {
                throw new Exception("Buffer must be uploded when using Draw().");
            }
        }


        public virtual void Bind()
        {
            if (!IsUploaded)
            {
                throw new Exception("Buffer must be uploded when using Bind().");
            }
        }

        public virtual void Unbind()
        {
            if (!IsUploaded)
            {
                throw new Exception("Buffer must be uploded when using Unbind().");
            }
        }

        private bool isDisposed;
        public virtual void Dispose()
        {
            if (!isDisposed)
            {
                if (IsUploaded)
                {
                    GL.DeleteBuffer(id);
                    id = 0;
                }
                isDisposed = true;
            }
        }
    }
}
