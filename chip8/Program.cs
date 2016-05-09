using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core.Graphics;
using Core.Utils;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using KeyPressEventArgs = OpenTK.KeyPressEventArgs;

namespace chip8
{
    public class Program : GameWindow
    {

        private Chip8Emulator emulator;
        private ShaderBase shader;

        public static void Main()
        {
            var options = new ToolkitOptions
            {
                Backend = PlatformBackend.PreferNative
            };

            using (Toolkit.Init(options))
            using (Program game = new Program())
            {
                game.Width = 1200;
                game.Height = 800;
                game.Run(60);
            }
        }

        private bool redraw = true;

        protected override void OnLoad(EventArgs e)
        {
            Thread.CurrentThread.Name = "DrawThread";

            emulator = new Chip8Emulator();
            emulator.Load("E:\\Assets\\chip8\\games\\PONG");
            shader = new ShaderBase("default.vs", "default.fs");

            emulator.Redraw = () =>
            {
                redraw = true;
            };

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            emulator.SingleCycle();
            
            if (redraw)
            {
                GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
                GL.ClearColor(Color.Black);
                shader.Bind();
                Draw();
                shader.Unbind();
                redraw = false;
                SwapBuffers();
            }
            
        }

        private void Draw()
        {
            byte[] d = emulator.ScreenData;
            GL.Begin(PrimitiveType.Quads);
            {
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 64; j++)
                    {
                        if (d[i*64 + j] == 1)
                        {
                            float x0 = 2f * j / 64f - 1f;
                            float y0 = 2f * i / 32f - 1f;
                            float x1 = 2f * (j + 1) / 64f - 1f;
                            float y1 = 2f * (i + 1) / 32f - 1f;
                            GL.Vertex2(x0, -y0);
                            GL.Vertex2(x1, -y0);
                            GL.Vertex2(x1, -y1);
                            GL.Vertex2(x0, -y1);
                        }
                    }
                }
            }
            GL.End();
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            switch (e.Key)
            {
                case Key.Space:
                    emulator.SetKey(0, 1);
                    break;
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    emulator.SetKey(0, 0);
                    break;
            }
        }
    }
}
