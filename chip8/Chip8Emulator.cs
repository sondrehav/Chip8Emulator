using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace chip8
{


    public class Chip8Emulator
    {
        
        private ushort opcode;
        private byte[] memory = new byte[4096];
        private byte[] registers = new byte[16];
        private ushort indexRegister;
        private ushort programCounter;
        
        private byte[] screenData = new byte[64 * 32];
        private byte delayTimer;
        private byte soundTimer;

        private ushort[] stack = new ushort[16];
        private ushort stackPointer;

        private byte[] key = new byte[16];

        private byte[] chip8_fontset = new byte[80]
        { 
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        private bool drawFlag = false;
        
        //

        private Action redraw;
        private Action sound;
        private Action init;
        private Action cleanup;

        public byte[] ScreenData
        {
            get
            {
                lock (screenData)
                {
                    /*byte[] newBuffer = new byte[screenData.Length];
                    Buffer.BlockCopy(screenData, 0, newBuffer, 0, screenData.Length);*/
                    return screenData;
                }
            }
        }

        public Action Redraw
        {
            set { redraw = value; }
        }

        public Action Sound
        {
            set { sound = value; }
        }

        public Action Init1
        {
            set { init = value; }
        }

        public Action Cleanup
        {
            set { cleanup = value; }
        }

        public Chip8Emulator()
        {

            Init();
            
        }


        public void SetKey(int key, byte value)
        {
            if (key >= 16 || key < 0)
            {
                throw new ArgumentOutOfRangeException("Key index must be between 0 and 15.");
            }
            this.key[key] = value;
        }

        public void SingleCycle()
        {

            Cycle();

            if (drawFlag)
            {
                DrawGraphics();
            }
            
        }

        public void Init()
        {
            if (init != null) init();
            programCounter = 0x200;
            opcode = 0;
            indexRegister = 0;
            stackPointer = 0;
            for (int i = 0; i < memory.Length; i++)
            {
                memory[i] = 0;
            }
            for (int i = 0; i < registers.Length; i++)
            {
                registers[i] = 0;
            }
            for (int i = 0; i < screenData.Length; i++)
            {
                screenData[i] = 0;
            }
            for (int i = 0; i < stack.Length; i++)
            {
                stack[i] = 0;
            }
            // Load fontset
            for (int i = 0; i < chip8_fontset.Length; i++)
            {
                memory[i] = chip8_fontset[i];
            }
        }

        public void Load(string file)
        {
            //if(running) throw new Exception("Emulation already running.");
            using (FileStream reader = new FileStream(file, FileMode.Open))
            {
                if (reader.Length > memory.Length)
                {
                    throw new Exception("File to big for memory.");
                }
                reader.Read(memory, 0x200, (int)reader.Length);
            }
        }

        private void Cycle()
        {
            // FETCH
            if (!keyWait)
            {
                opcode = (ushort)((memory[programCounter]) << 8 | memory[programCounter + 1]);
                programCounter += 2;
                //System.Diagnostics.Debug.WriteLine(opcode.ToString("X"));
                // DECODE
                switch (opcode & 0xF000)
                {
                    case 0x0000:
                        switch (opcode & 0x000F)
                        {
                            case 0x0000:
                                for (int i = 0; i < screenData.Length; i++)
                                {
                                    screenData[i] = 0;
                                }
                                break;
                            case 0x000E:
                                programCounter = stack[stackPointer];
                                stackPointer--;
                                break;
                        }
                        break;
                    case 0x1000:
                        {
                            programCounter = (ushort)(0x0FFF & opcode);
                            break;
                        }
                    case 0x2000:
                        {
                            stackPointer++;
                            stack[stackPointer] = programCounter;
                            programCounter = (ushort)(0x0FFF & opcode);
                            break;
                        }
                    case 0x3000:
                        {
                            int registerIndex = (opcode & 0x0F00) >> 8;
                            byte val = (byte)(opcode & 0x00FF);
                            if (registers[registerIndex] == val)
                            {
                                programCounter += 2;
                            }
                            break;
                        }
                    case 0x4000:
                        {
                            int registerIndex = (opcode & 0x0F00) >> 8;
                            byte val = (byte)(opcode & 0x00FF);
                            if (registers[registerIndex] != val)
                            {
                                programCounter += 2;
                            }
                            break;
                        }
                    case 0x5000:
                        {
                            int registerIndexX = (opcode & 0x0F00) >> 8;
                            int registerIndexY = (opcode & 0x00F0) >> 4;
                            if (registers[registerIndexX] == registers[registerIndexY])
                            {
                                programCounter += 2;
                            }
                            break;
                        }
                    case 0x6000:
                        {
                            int registerIndex = (opcode & 0x0F00) >> 8;
                            byte val = (byte)(opcode & 0x00FF);
                            registers[registerIndex] = val;
                            break;
                        }
                    case 0x7000:
                        {
                            int registerIndex = (opcode & 0x0F00) >> 8;
                            byte val = (byte)(opcode & 0x00FF);
                            registers[registerIndex] += val;
                            break;
                        }
                    case 0x8000:
                        {
                            switch (opcode & 0x000F)
                            {
                                case 0x0000:
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        int registerIndexY = (opcode & 0x00F0) >> 4;
                                        registers[registerIndexX] = registers[registerIndexY];
                                        break;
                                    }
                                case 0x0001:
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        int registerIndexY = (opcode & 0x00F0) >> 4;
                                        registers[registerIndexX] = (byte)(registers[registerIndexY] | registers[registerIndexX]);
                                        break;
                                    }
                                case 0x0002:
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        int registerIndexY = (opcode & 0x00F0) >> 4;
                                        registers[registerIndexX] = (byte)(registers[registerIndexY] & registers[registerIndexX]);
                                        break;
                                    }
                                case 0x0003:
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        int registerIndexY = (opcode & 0x00F0) >> 4;
                                        registers[registerIndexX] = (byte)(registers[registerIndexY] ^ registers[registerIndexX]);
                                        break;
                                    }
                                case 0x0004:
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        int registerIndexY = (opcode & 0x00F0) >> 4;
                                        int val = registers[registerIndexY] + registers[registerIndexX];
                                        if (val > 255)
                                        {
                                            registers[15] = 1;
                                        }
                                        else
                                        {
                                            registers[15] = 0;
                                        }
                                        registers[registerIndexX] = (byte)(0x00FF & val);
                                        break;
                                    }
                                case 0x0005: // TODO: Should sub if Vx <= Vy?
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        int registerIndexY = (opcode & 0x00F0) >> 4;
                                        int val = registers[registerIndexX] - registers[registerIndexY];
                                        if (val > 0)
                                        {
                                            registers[15] = 1;
                                        }
                                        else
                                        {
                                            registers[15] = 0;
                                        }
                                        registers[registerIndexX] = (byte)(0x00FF & val);
                                        break;
                                    }
                                case 0x0006:
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        if ((registers[registerIndexX] & 0x0001) == 0x0001)
                                        {
                                            registers[15] = 1;
                                        }
                                        else
                                        {
                                            registers[15] = 0;
                                        }
                                        registers[registerIndexX] = (byte)(registers[registerIndexX] >> 1);
                                        break;
                                    }
                                case 0x0007:
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        int registerIndexY = (opcode & 0x00F0) >> 4;
                                        int val = registers[registerIndexY] - registers[registerIndexX];
                                        if (val > 0)
                                        {
                                            registers[15] = 1;
                                        }
                                        else
                                        {
                                            registers[15] = 0;
                                        }
                                        registers[registerIndexX] = (byte)(0x00FF & val);
                                        break;
                                    }
                                case 0x000E:
                                    {
                                        int registerIndexX = (opcode & 0x0F00) >> 8;
                                        if ((registers[registerIndexX] & 0x0080) == 0x0080)
                                        {
                                            registers[15] = 1;
                                        }
                                        else
                                        {
                                            registers[15] = 0;
                                        }
                                        registers[registerIndexX] = (byte)(registers[registerIndexX] << 1);
                                        break;
                                    }
                            }
                            break;
                        }
                    case 0x9000:
                        {
                            int registerIndexX = (opcode & 0x0F00) >> 8;
                            int registerIndexY = (opcode & 0x00F0) >> 4;
                            if (registers[registerIndexX] != registers[registerIndexY])
                            {
                                programCounter += 2;
                            }
                            break;
                        }
                    case 0xA000:
                        {
                            indexRegister = (ushort)(0x0FFF & opcode);
                            break;
                        }
                    case 0xB000:
                        {
                            programCounter = (ushort)((0x0FFF & opcode) + registers[0]);
                            break;
                        }
                    case 0xC000:
                        {
                            int registerIndexX = (opcode & 0x0F00) >> 8;
                            registers[registerIndexX] = (byte)(_RandomByte() & (0x00FF & opcode));
                            break;
                        }
                    case 0xD000:
                        {

                            byte n = (byte)(0x000F & opcode);
                            byte x = registers[(0x0F00 & opcode) >> 8];
                            byte y = registers[(0x00F0 & opcode) >> 4];
                            byte pixel;

                            registers[15] = 0;

                            for (int y_line = 0; y_line < n; y_line++)
                            {
                                pixel = memory[indexRegister + y_line];
                                for (int x_line = 0; x_line < 8; x_line++)
                                {
                                    if ((pixel & (0x80 >> x_line)) != 0)
                                    {
                                        lock (screenData)
                                        {
                                            if (screenData[(x + x_line + ((y + y_line) * 64))] == 1)
                                            {
                                                registers[15] = 1;
                                            }
                                            screenData[x + x_line + (y + y_line) * 64] ^= 1;
                                            Monitor.Pulse(screenData);
                                        }
                                    }
                                }
                            }
                            drawFlag = true;
                            break;
                        }
                    case 0xE000:
                        {
                            switch (opcode & 0x00FF)
                            {
                                case 0x009E:
                                    {
                                        int x = (opcode & 0x0F00) >> 8;
                                        if (key[x] == 1)
                                        {
                                            programCounter += 2;
                                        }
                                        break;
                                    }
                                case 0x00A1:
                                    {
                                        int x = (opcode & 0x0F00) >> 8;
                                        if (key[x] == 0)
                                        {
                                            programCounter += 2;
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                    case 0xF000:
                        {
                            switch (opcode & 0x00FF)
                            {
                                case 0x0007:
                                    {
                                        int x = (opcode & 0x0F00) >> 8;
                                        registers[x] = delayTimer;
                                        break;
                                    }
                                case 0x000A:
                                    {
                                        // Stop execution
                                        byte key = KeyWait();
                                        int x = (0x0F00 & opcode) >> 8;
                                        registers[x] = key;
                                        break;
                                    }
                                case 0x0015:
                                    {
                                        delayTimer = registers[(opcode & 0x0F00) >> 8];
                                        break;
                                    }
                                case 0x0018:
                                    {
                                        soundTimer = registers[(opcode & 0x0F00) >> 8];
                                        break;
                                    }
                                case 0x001E:
                                    {
                                        indexRegister += (byte)((0x0F00 & opcode) >> 8);
                                        break;
                                    }
                                case 0x0029: // TODO: Probably wrong
                                    {
                                        int x = (0x0F00 & opcode) >> 8;
                                        indexRegister = registers[x];
                                        break;
                                    }
                                case 0x0033:
                                    {
                                        memory[indexRegister] = (byte)(registers[(opcode & 0x0F00) >> 8] / 100);
                                        memory[indexRegister + 1] = (byte)((registers[(opcode & 0x0F00) >> 8] / 10) % 10);
                                        memory[indexRegister + 2] = (byte)((registers[(opcode & 0x0F00) >> 8] % 100) % 10);
                                        break;
                                    }
                                case 0x0055:
                                    {
                                        int x = (0x0F00 & opcode) >> 8;
                                        for (int i = 0; i < x; i++)
                                        {
                                            memory[indexRegister + i] = registers[i];
                                        }
                                        break;
                                    }
                                case 0x0065:
                                    {
                                        int x = (0x0F00 & opcode) >> 8;
                                        for (int i = 0; i < x; i++)
                                        {
                                            registers[i] = memory[indexRegister + i];
                                        }
                                        break;
                                    }

                            }
                        }
                        break;
                }
            }
            

            if (delayTimer > 0) --delayTimer;
            if (soundTimer > 0)
            {
                if (soundTimer == 1)
                {
                    if (sound != null) sound();
                }
                --soundTimer;
            }
        }

        private void DrawGraphics()
        {
            drawFlag = false;
            if (redraw != null)
            {
                redraw();
            }
        }

        private bool keyWait = false;
        private byte KeyWait()
        {
            keyWait = true;
            return 0;
        }

        private static Random random = new Random();
        private static byte _RandomByte()
        {
            byte[] buf = new byte[1];
            random.NextBytes(buf);
            return buf[0];
        }

        

    }


}
