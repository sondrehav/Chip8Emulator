using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace Core.Graphics
{

    // https://www.opengl.org/wiki/Vertex_Specification_Best_Practices#Attribute_sizes

    public class VertexBuffer : Buffer
    {

       
        public VertexBuffer(byte[] data, BufferUsageHint bufferUsage, bool keepData = false) : base(data, bufferUsage, keepData, BufferTarget.ArrayBuffer)
        {
            Upload();
            Bind();
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);
            Unbind();
        }

        private static VertexBuffer currentlyBound = null;

        public override void Bind()
        {
            base.Bind();
            if (currentlyBound != this)
            {
                GL.BindBuffer(base.bufferTarget, id);
                currentlyBound = this;
            }   
        }

        public override void Unbind()
        {
            base.Unbind();
            if (currentlyBound == this)
            {
                GL.BindBuffer(base.bufferTarget, 0);
                currentlyBound = null;
            }  
        }

        public override void Draw(int count = 0)
        {
            base.Draw(count);
            Bind();
            GL.DrawArrays(drawMode, 0, count == 0 ? (int)(dataLength / 2) : count);
            Unbind();
        }
    }
}
