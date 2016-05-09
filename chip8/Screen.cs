using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core.Graphics;
using OpenTK.Graphics.OpenGL;

namespace chip8
{
    public partial class Screen : Form
    {
        public Screen()
        {
            InitializeComponent();
        }

        private Chip8Emulator emulator;
        private bool emulatorRunning = false;
        private ShaderBase shader;
        private void glInit(object sender, EventArgs e)
        {
            shader = new ShaderBase("default.vs", "default.fs");
            shader.Bind();
            GL.Viewport(0, 0, glControlThreaded1.Width, glControlThreaded1.Height);
            emulator = new Chip8Emulator();
            
            emulatorRunning = false;
            emulator.Redraw = () =>
            {
                GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

                byte[] data = emulator.ScreenData;

                float nw = 2f / 64f;
                float nh = 2f / 32f;

                GL.Begin(PrimitiveType.Quads);
                for (int x = 0; x < 64; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        int index = x + y*64;
                        if (data[index] == 1)
                        {
                            float nx = 2f * x/64f - 1f;
                            float ny = -2f * y/32f + 1f;
                            GL.Vertex2(nx, ny);
                            GL.Vertex2(nx + nw, ny);
                            GL.Vertex2(nx + nw, ny + nh);
                            GL.Vertex2(nx, ny + nh);
                        }
                    }
                }
                GL.End();

                glControlThreaded1.SwapBuffers();
            };
            glControlThreaded1.FPSCap = 60f;
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
            glControlThreaded1.SwapBuffers();
        }

        private void glLoop(object sender, EventArgs e)
        {
            if (emulatorRunning)
                emulator.SingleCycle();
        }

        private void glCleanup(object sender, EventArgs e)
        {
            
        }

        private void glResize(object sender, EventArgs e)
        {
            glControlThreaded1.InvokeAction(() =>
            {
                GL.Viewport(0, 0, glControlThreaded1.Width, glControlThreaded1.Height);
            });
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulatorRunning = false;
            OpenFileDialog ofd = new OpenFileDialog();
            DialogResult result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                emulator.Init();
                try
                {
                    emulator.Load(ofd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Console.WriteLine(ex.Message);
                }
            }
            emulatorRunning = true;
            
        }

        private int KeyMap(int input)
        {
            Console.WriteLine(input);
            switch (input)
            {
                
            }
            return 0;
        }

        private void KeyDownMethod(object sender, KeyEventArgs e)
        {
            int emulatorKey = KeyMap(e.KeyValue);
            if (emulatorRunning)
            {
                glControlThreaded1.InvokeAction(() =>
                {
                    emulator.SetKey(emulatorKey, 1);
                });
            }
        }

        private void KeyUpMethod(object sender, KeyEventArgs e)
        {
            int emulatorKey = KeyMap(e.KeyValue);
            if (emulatorRunning)
            {
                glControlThreaded1.InvokeAction(() =>
                {
                    emulator.SetKey(emulatorKey, 0);
                });
            }
        }

    }
}
