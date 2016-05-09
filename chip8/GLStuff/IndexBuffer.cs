using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace Core.Graphics
{
    public class IndexBuffer : Buffer
    {
        
        private DrawElementsType dataType = DrawElementsType.UnsignedInt;
        private int countMult = 4;

        public IndexBuffer(byte[] data, BufferUsageHint bufferUsage, bool keepData = false) : base(data, bufferUsage, keepData, BufferTarget.ElementArrayBuffer)
        {
            Upload();
        }
        
        public void SetType(DrawElementsType dataType)
        {
            this.dataType = dataType;
            switch (dataType)
            {
                case DrawElementsType.UnsignedByte:
                    countMult = 0;
                    break;
                case DrawElementsType.UnsignedShort:
                    countMult = 1;
                    break;
                case DrawElementsType.UnsignedInt:
                    countMult = 2;
                    break;
            }
            
        }

        public override void Draw(int count = 0)
        {
            base.Draw(count);
            Bind();
            int c = count == 0 ? (int) (dataLength >> countMult) : count;
            GL.DrawElements(base.drawMode, c, dataType, 0);
            Unbind();
        }

        private static IndexBuffer currentlyBound = null;

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

    }
}
