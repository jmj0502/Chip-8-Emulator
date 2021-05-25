using System;
using System.IO; //Will allow to interact with FS.
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SdlDotNet.Core;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;
using SdlDotNet.Input;


namespace CHIP8
{
    class Emulator
    {
        #region base emulator variables
        //Defining our emulator constants(Memory space, characters, display size, etc).
        const int MEMORY_SPACE = 0xFFF;
        const int REGISTERS = 16;
        const int SCREEN_WIDTH = 64;
        const int SCREEN_HEIGTH = 32;
        const int STARTING_ADDRESS = 0x200;
        const int STACK_SIZE = 16;
        const int AMOUNT_OF_REGISTERS = 16;
        const int PIXEL_SIZE = 5; //This will allow us to scalate our emulation.
        const int OPER_PER_SECOND = 60;
        const int OPER_PER_FRAMME = OPER_PER_SECOND / 60;

        //Defining the elements that will represent the "hardware" of our emulator.
        public int[,] screenArray = new int[SCREEN_WIDTH, SCREEN_HEIGTH];
        //This array represents the memory of our CHIP-8 Emu.
        public int[] memory = new int[MEMORY_SPACE];
        //This array represents our registers.
        public int[] V = new int[REGISTERS];
        //This array represents our stack.
        public int[] stack = new int[STACK_SIZE];

        //Defining the variables that will hold our instructions.
        //Represents a 8 instruction, it has a size of 2 bytes (16 bits).
        public int instruction;
        //Program Counter (PC).
        public int PC;
        //Stack Pointer (SP).
        public int SP;
        public int I;
        public int KK;

        //Optcode manipulators.
        public int optcode1 = 0;
        //X 4-bit register.
        public int optcode2 = 0;
        //Y 4-bit register.
        public int optcode3 = 0;
        public int optcode4 = 0;

        //defining a general porpouse opcode. It can represent any opcode and it's general porpouse.
        public int opcode;

        //definning our Surfaces. They'll represent our balck and white pixels.
        public Surface WhitePixel;
        public Surface BlackPixel;

        //Direction.
        public int NNN = 0;

        //Defining what we need in order to implement a timer and sound.
        public int delayTimer;
        public int soundTimer;
        public bool executeSound;

        //This will allow us to simulate our timers.
        private int TimeUntilTimerUpdate;

        //The sprites that represents the fundamental keys of the CHIP-8 on its screen.
        private byte[] characters =
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // necessary bytes to draw 0.
            0x20, 0x60, 0x20, 0x20, 0x70, // " " 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // " " 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // " " 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // " " 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // " " 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // " " 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // " " 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // " " 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // " " 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // " " A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // " " B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // " " C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // " " D
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // " " E
            0xF0, 0x90, 0xF0, 0x90, 0x90, // " " F
        };

        private bool[] pressedKeys = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

        //key positions, this will allow us to access each key on our "key_pressed" Array.
        const int KEY_1 = 0;
        const int KEY_2 = 1;
        const int KEY_3 = 2;
        const int KEY_4 = 3;
        const int KEY_Q = 4;
        const int KEY_W = 5;
        const int KEY_E = 6;
        const int KEY_R = 7;
        const int KEY_A = 8;
        const int KEY_S = 9;
        const int KEY_D = 10;
        const int KEY_F = 11;
        const int KEY_Z = 12;
        const int KEY_X = 13;
        const int KEY_C = 14;
        const int KEY_V = 15;

        //Literal chip-8 keyboard mapping.
        private byte[] keyMapping =
        {
            0x01, 0x02, 0x03, 0x0C,
            0x04, 0x05, 0x06, 0x0D,
            0x07, 0x08, 0x09, 0x0E,
            0x0A, 0x00, 0x0B, 0x0F
        };

        Random rndm = new Random();
        #endregion

        [DllImport("Kernel32.dll")]
        public static extern bool Beep(UInt32 frequency, UInt32 duration);

        public Emulator()
        {
            try
            {
                Video.SetVideoMode(SCREEN_WIDTH * PIXEL_SIZE, SCREEN_HEIGTH * PIXEL_SIZE);
                Video.Screen.Fill(Color.Black);
                Events.Fps = 60;
                Events.TargetFps = 60;
                this.WhitePixel = Video.CreateRgbSurface(PIXEL_SIZE, PIXEL_SIZE);
                this.BlackPixel = Video.CreateRgbSurface(PIXEL_SIZE, PIXEL_SIZE);
                this.WhitePixel.Fill(Color.White);
                this.BlackPixel.Fill(Color.Black);

                //Keyboard events.
                Events.KeyboardDown += new EventHandler<KeyboardEventArgs>(this.KeyPressed);
                Events.KeyboardUp += new EventHandler<KeyboardEventArgs>(this.KeyReleased);
                Events.Quit += new EventHandler<QuitEventArgs>(this.EventsQuit);
                Events.Tick += new EventHandler<TickEventArgs>(this.EventsTick);

                //Initializing our emulator.
                ResetHardware();
                LoadGame("PONG");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error " + e.Message + e.StackTrace);
                Events.QuitApplication();
            }
        }

        private void EventsQuit(object sender, QuitEventArgs e)
        {
            Events.QuitApplication();
        }

        private void EventsTick(object sender, TickEventArgs e)
        {
            Video.WindowCaption = "CHIP-8 Emulator - FPS" + Events.Fps;
            this.EmulateFrames();
        }

        public void Run() 
        {
            Events.Run();
        }

        private void ResetHardware()
        {
            //Re initializing our timers.
            delayTimer = 0x0;
            soundTimer = 0x0;

            //Re initializing our general registers.
            instruction = 0x0;
            PC = STARTING_ADDRESS;
            SP = 0x0;
            I = 0x0;

            //Cleaning our registers.
            for (int currentRegister = 0; currentRegister < AMOUNT_OF_REGISTERS; currentRegister++)
            {
                V[currentRegister] = 0x0;
            }

            //Cleaning our memory.
            for (int dir = 0; dir < MEMORY_SPACE; dir++)
            {
                memory[dir] = 0x0;
            }

            //Cleaning our stack.
            for (int item = 0; item < STACK_SIZE; item++)
            {
                stack[item] = 0x0;
            }

            //Loading our fonts into the system.
            for (int i = 0; i < characters.Length; i++)
            {
                memory[i] = characters[i];
            }


            Video.Screen.Fill(Color.Black);
            Video.Screen.Update();
        }

        public void KeyPressed(object sender, KeyboardEventArgs e)
        {
            //We close the game if scape is pressed.
            if (e.Key == Key.Escape)
            {
                Events.QuitApplication();
            }

            if (e.Key == Key.One) { pressedKeys[KEY_1] = true; }
            if (e.Key == Key.Two) { pressedKeys[KEY_2] = true; }
            if (e.Key == Key.Three) { pressedKeys[KEY_3] = true; }
            if (e.Key == Key.Four) { pressedKeys[KEY_4] = true; }
            if (e.Key == Key.Q) { pressedKeys[KEY_Q] = true; }
            if (e.Key == Key.W) { pressedKeys[KEY_W] = true; }
            if (e.Key == Key.E) { pressedKeys[KEY_E] = true; }
            if (e.Key == Key.R) { pressedKeys[KEY_R] = true; }
            if (e.Key == Key.A) { pressedKeys[KEY_A] = true; }
            if (e.Key == Key.S) { pressedKeys[KEY_S] = true; }
            if (e.Key == Key.D) { pressedKeys[KEY_D] = true; }
            if (e.Key == Key.F) { pressedKeys[KEY_F] = true; }
            if (e.Key == Key.Z) { pressedKeys[KEY_Z] = true; }
            if (e.Key == Key.X) { pressedKeys[KEY_X] = true; }
            if (e.Key == Key.C) { pressedKeys[KEY_C] = true; }
            if (e.Key == Key.V) { pressedKeys[KEY_V] = true; }
        }

        public void KeyReleased(object sender, KeyboardEventArgs e)
        {
            if (e.Key == Key.One) { pressedKeys[KEY_1] = false; }
            if (e.Key == Key.Two) { pressedKeys[KEY_2] = false; }
            if (e.Key == Key.Three) { pressedKeys[KEY_3] = false; }
            if (e.Key == Key.Four) { pressedKeys[KEY_4] = false; }
            if (e.Key == Key.Q) { pressedKeys[KEY_Q] = false; }
            if (e.Key == Key.W) { pressedKeys[KEY_W] = false; }
            if (e.Key == Key.E) { pressedKeys[KEY_E] = false; }
            if (e.Key == Key.R) { pressedKeys[KEY_R] = false; }
            if (e.Key == Key.A) { pressedKeys[KEY_A] = false; }
            if (e.Key == Key.S) { pressedKeys[KEY_S] = false; }
            if (e.Key == Key.D) { pressedKeys[KEY_D] = false; }
            if (e.Key == Key.F) { pressedKeys[KEY_F] = false; }
            if (e.Key == Key.Z) { pressedKeys[KEY_Z] = false; }
            if (e.Key == Key.X) { pressedKeys[KEY_X] = false; }
            if (e.Key == Key.C) { pressedKeys[KEY_C] = false; }
            if (e.Key == Key.V) { pressedKeys[KEY_V] = false; }
        }

        public void EmulateFrames()
        {
            if (soundTimer > 0)
            {
                Console.Beep(350, 50);
                executeSound = false;
            }
            //For each tick or frame, 600/60 = 10 instructions will be emulated.
            for (int i = 0; i < OPER_PER_FRAMME; i++)
            {
                EmulateOpcodes();
            }
        }

        public void EmulateOpcodes()
        {
            if (TimeUntilTimerUpdate == 0)
            {
                if (delayTimer > 0)
                {
                    delayTimer--;
                }

                if (soundTimer > 0)
                {
                    soundTimer--;
                }

                TimeUntilTimerUpdate = OPER_PER_FRAMME;
            }
            else
            {
                TimeUntilTimerUpdate--;
            }
            ExecuteOpcodes();
        }

        public void HandleTimers()
        {
            if (delayTimer > 0)
            {
                delayTimer--;
            }

            if (soundTimer > 0)
            {
                soundTimer--;
            }
        }

        public void LoadGame(string romName)
        {
            FileStream rom;
            try
            {
                rom = new FileStream(@romName, FileMode.Open);

                if (rom.Length == 0)
                {
                    throw new Exception("The ROM is either, corrupted or empty.");
                }

                //From this point and on we load the rom into our memory starting from the postion 0x200 onwards.
                for (int i = 0; i < rom.Length; i++)
                {
                    memory[STARTING_ADDRESS + i] = (byte)rom.ReadByte();
                }

                rom.Close();
            }
            catch (Exception e)
            {
                throw new Exception("Error while processing the ROM" + e.Message);
            }
        }

        public void ExecuteOpcodes()
        {
            /*Here we are reading instructions from the memory. Each instruction has a length of 2 bytes.
             We'll be reading instructions like: "Jump to next subroutine" or "add register Vx with register KK."
             We generate every instruction by reading two elements of the memory, the first reading will contain the most
             significative byte, and the second one the less significative one.
            Once we've done all of that, we assemble our 2 byte instruction by shifting the most significative byte to thee left 8 spaces,
            and then, we join it with our less significative byte using the | (bitwise |) operator.
             */
            #region instruction reading section.
            instruction = memory[PC] << 8 | memory[PC + 1];
            PC += 2;
            #endregion
            /*
            Here we'll excute our instructions. This process is composed of two stages, the first one is called division.
            **Division: is the process that allow us to split our instructions in 4 blocks of 4 bites each. Each group of bits
            represents an an opcode. Let's work with the instruction 0x6A02 ([0110 1010]->mayor | [0000 0010]->minnor | 0110 1010 0000 0010).
            * Opcode1: Stores the greater 4 bits. -> 0110
            * Opcode2: Stores the minnor 4 bits of the greates byte. -> 1010
            * Opcode3: Stores the greater 4 bits of the minnor byte. -> 0000
            * OpcodeN: Stores the minnor 4 bytes. -> 0010
            * OpcodeNNN: Stores the minnor 12 bytes of the instruction. -> 1010 0000 0010
             */

            #region opcode extraction section.
            //Division process. Getting the KK byte, the smaller of all, it size its 1 bite.
            KK = (instruction & 0x0FF);

            //getting each opcode.
            optcode1 = (instruction & 0xF000) >> 12; //The greater bytes of the instruction.
            optcode2 = (instruction & 0x0F00) >> 8;  // X
            optcode3 = (instruction & 0x00F0) >> 4;  // Y 
            optcode4 = (instruction & 0x000F) >> 0;  // Opcode N, the minnor bytes of the instruction.

            //gettin the value of the opcode NNN.
            NNN = (instruction & 0x0FFF);
            #endregion
            /*
            Instruction execution: This is probably the longest part. In this section we'll emulate every instruction
            (there are 35 in total). To do so we'll use Cowgod super guide in order to keep them in a resonable order 
            (starting with 0NNN, followed by 00E0, 1NNN, 2NNN, 3XKK, etc).
             */
            #region instruction execution
            switch (optcode1)
            {
                case 0x0:
                {
                        switch (instruction)
                        {
                            case 0x00E0:
                            {
                                    ClearScreen();
                                    break;        
                            }
                            case 0x00EE:
                            {
                                    ReturnFromSubRoutine();
                                    break;
                            }
                        }
                        break;
                }
                case 0x1:
                    {
                        JumpToAddress();
                        break;
                    }
                case 0x2:
                    {
                        CallSubRoutine();
                        break;
                    }
                case 0x3:
                    {
                        //opcode 4XKK: Skip the next instruction if Vx = KK.
                        SkipIfEquals();
                        break;
                    }
                case 0x4:
                    {
                        SkipIfNotEquals();
                        break;
                    }
                case 0x5:
                    {
                        SkipIfRegistersAreEqual();
                        break;
                    }
                case 0x6:
                    {
                        AssignNumToRegister();
                        break;
                    }
                case 0x7:
                    {
                        AddNumToRegister();
                        break;
                    }
                case 0x8:
                    {
                        switch (optcode4) 
                        {
                            case 0x0:
                                {
                                    //opcode 8XY0 Assign from register to register.
                                    AssignRegisterToRegister();
                                    break;
                                }
                            case 0x1:
                                {
                                    RegisterOR();
                                    break;
                                }
                            case 0x2:
                                {
                                    RegisterAND();
                                    break;
                                }
                            case 0x3:
                                {
                                    RegisterXOR();
                                    break;
                                }
                            case 0x4:
                                {
                                    AddRegisterToRegister();
                                    break;
                                }
                            case 0x5:
                                {
                                    SubtractRegisterToRegister();
                                    break;
                                }
                            case 0x6:
                                {
                                    ShiftRegisterToRight();
                                    break;
                                }
                            case 0x7:
                                {
                                    ReverseSubtractRegisterToRegister();
                                    break;
                                }
                            case 0xE:
                                {
                                    ShiftRegisterToLeft();
                                    break;
                                }
                        }
                        break;
                    }
                case 0x9:
                    {
                        SkipIfRegistersNotEqual();
                        break;
                    }
                case 0xA:
                    {
                        //Sets the register I to address NNN.
                        AssignIndexToAddress();
                        break;
                    }
                case 0xB:
                    {
                        JumpWithOffset();
                        break;
                    }
                case 0xC:
                    {
                        RandomAndNumber();
                        break;
                    }
                case 0xD:
                    {
                        DrawSprite();
                        break;
                    }
                case 0xE:
                    {
                        switch (KK) 
                        {
                            case 0x9E:
                                {
                                    SkipIfKeyDown();
                                    break;
                                }
                            case 0xA1:
                                {
                                    SkipIfKeyUp();
                                    break;
                                }
                        }
                        break;
                    }
                case 0xF:
                    {
                        switch (KK)
                        {
                            case 0x07:
                                {
                                    //Assing the value of the delay timer to our register.
                                    AssignFromDelay();
                                    break;
                                }
                            case 0x0A:
                                {
                                    //Waits until a key press and stops the execution until that moment. Then stores the key in he register.
                                    StoreKey();
                                    break;
                                }
                            case 0x15:
                                {
                                    //Sets the value of the delay timer to Vx.
                                    AssignToDelay();
                                    break;
                                }
                            case 0x18:
                                {
                                    AssignRegisterToSound();
                                    break;
                                }
                            case 0x1E:
                                {
                                    AddRegisterToIndex();
                                    break;
                                }
                            case 0x29:
                                {
                                    IndexAtFontC8();
                                    break;
                                }
                            case 0x33:
                                {
                                    StoreBSD();
                                    break;
                                }
                            case 0x55:
                                {
                                    SaveRegisters();
                                    break;
                                }
                            case 0x65:
                                {
                                    LoadRegisters();
                                    break;
                                }
                        }
                        break;
                    }
            }
            #endregion

        }

        #region instruction implementation methods
        public void ClearScreen()
        {
            //This logic allow us to properly clear the screen.
            Video.Screen.Fill(Color.Black);
            Video.Screen.Update();
            
            //Here we are going to reference every value of screen array into 0.
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                for (int y = 0; y < SCREEN_HEIGTH; y++)
                {
                    screenArray[x, y] = 0;
                }
            }
        }

        public void ReturnFromSubRoutine()
        {
            //Decrementing the Stack Pointer by one.
            SP--;

            //Making our Program Counter equals to our stack at the current Stack Pointer.
            PC = stack[SP];
        }

        public void JumpToAddress()
        {
            //Here our Program Counter jumps to the NNN instruction. It doesn't just jump there, the PC will go there
            //once it has completed its actual cycle.
            PC = NNN;
        }

        public void CallSubRoutine()
        {
            //Saving the value of our stack pointer on top of the stack, and incrementing our SP value by one.
            stack[SP] = PC;
            SP++;
            //Setting PC to NNN.
            PC = NNN;
        }

        public void SkipIfEquals()
        {
            if (V[optcode2] == KK)
            {
                //jumps to the next instrunction.
                PC += 2;
            }
        }

        public void SkipIfNotEquals()
        {
            if (V[optcode2] != KK)
            {
                //jumps to the next instruction.
                PC += 2;
            }
        }

        public void SkipIfRegistersAreEqual()
        {
            if (V[optcode2] == V[optcode3])
            {
                PC += 2;
            }
        }

        public void AssignNumToRegister()
        {
            //Sets our Vx register to KK.
            V[optcode2] = KK;
        }

        public void AddNumToRegister()
        {
            V[optcode2] += KK;
        }

        public void AssignRegisterToRegister()
        {
            //Vx = Vy
            V[optcode2] = V[optcode3];
        }

        public void RegisterOR()
        {
            //Vx = Vx | Vy
            V[optcode2] |= V[optcode3];  
        }

        public void RegisterAND()
        {
            //Vx = Vx & Vy
            V[optcode2] &= V[optcode3];
        }

        public void RegisterXOR()
        {
            //Vx = Vx ^ Vy
            V[optcode2] ^= V[optcode3];
        }

        public void AddRegisterToRegister()
        {
            /*Here we'll add Vx + Vy and store the result of the operation in Vx
              if the result is greater than 8 bits (255) we'll set Vf to 1, else we set it to 0.
              Only the 8 minnor bits are assigned to Vx. This value turned in to 1 is the carry of the operation.
              A trick to handle the carry in this particular language, relies on knowing after how many bits the carry
              is going to take place. In this case that number will be 8, and can get carry by doing
              carry = (Vx + Vy) >> 8.
             */
            V[0xF] = (V[optcode2] + V[optcode3]) >> 8; //if the result is greater than 8 bits then this will be 1, else 0.
            V[optcode2] += V[optcode3];
        }

        public void SubtractRegisterToRegister()
        {
            //Turning Vf into 1 if Vx > Vy.
            if (V[optcode2] > V[optcode3])
            {
                V[0xF] = 0x1;
            }
            else 
            {
                V[0xF] = 0x0;
            }
            //Vx = Vx - Vy
            V[optcode2] -= V[optcode3];
        }

        public void ShiftRegisterToRight()
        {
            //Vf = Vx & 1 [since we are trying to verify the value of the less significant bite] (Vf could be 1 or 0). 
            //We can optimize this without conditionals by doing:
            V[0xF] = V[optcode2] & 1;
            //This how we can write a division by two using a right shift (like pros).
            V[optcode2] >>= 1;
        }

        public void ReverseSubtractRegisterToRegister()
        {
            //Recheck later. Cowgod guide says >, while implementation guide says >=***
            if (V[optcode3] > V[optcode2])
            {
                V[0xF] = 0x1;
            }
            else
            {
                V[0xF] = 0x0;
            }
            V[optcode2] = V[optcode3] - V[optcode2];
        }

        public void ShiftRegisterToLeft()
        {
            //Vf = Vx & 0x10 [since we are trying to verify the value of the most significant bite] (Vf could be 1 or 0).
            //We can optimize this applying the same logic present on the ShiftRegisterToRight method.
            V[0xF] = V[optcode2] & 0x10;

            //We multiply Vx by two using a left shift (like a pro again).
            V[optcode2] <<= 1;
        }

        public void SkipIfRegistersNotEqual()
        {
            if (V[optcode2] != V[optcode3])
            {
                PC += 2;
            }
        }

        public void AssignIndexToAddress()
        {
            I = NNN;
        }

        public void JumpWithOffset()
        {
            PC = NNN + V[0x0];
        }

        public void RandomAndNumber()
        {
            //Generating a random number using rndm(Random).Next, since it allow to provide a minimun and a maximun. 
            int randomNumber = rndm.Next(0, 255);
            V[optcode2] = randomNumber & KK;
        }

        public void DrawSprite()
        {
            //This method will allow us to write bytes to the screen.
            //In chip-8 a sprite is defined as 8*N.
            int spriteWidthX = 8;

            //reinitializing our collision detection.
            V[0xF] = 0x0;

            if ((instruction & 0x000F) == 0)
            {
                //Super chip-8 method.
            }
            else
            {
                for (int spriteY = 0; spriteY < optcode4; spriteY++)
                {
                    //reading a byte from the memory.

                    for (int spriteX = 0; spriteX < spriteWidthX; spriteX++)
                    {
                        int x = (memory[I + spriteY] & (0x80 >> spriteX));
                        if (x != 0)
                        {
                            //Checking if there are any collisions.
                            int xx = (V[optcode2] + spriteX);
                            int yy = (V[optcode3] + spriteY);

                            if (screenArray[xx % 64, yy % 32] == 1)
                            {
                                screenArray[xx % 64, yy % 32] = 0;
                                Video.Screen.Blit(BlackPixel, new Point((xx % 64) * PIXEL_SIZE, (yy % 32) * PIXEL_SIZE));
                                V[0xF] = 1; //Turning collition on.
                            }
                            else
                            {
                                screenArray[xx % 64, yy % 32] = 1;
                                Video.Screen.Blit(WhitePixel, new Point((xx % 64) * PIXEL_SIZE, (yy % 32) * PIXEL_SIZE));
                            }
                        }
                    }
                }
            }
            //we update the screen in order to draw the updated pixels.
            Video.Screen.Update();
        }

        public void UpdateScreen()
        {
            //Cleaning our buffer.
            Console.Clear();

            //Drawing the upper borders.
            Console.WriteLine("/" + "".PadLeft(SCREEN_WIDTH-1, '-') + "\\");

            //Drawing to the screen.
            for (int y = 0; y < SCREEN_HEIGTH; y++)
            {
                Console.Write("|");
                for (int x = 0; x < SCREEN_WIDTH; x++)
                {
                    if (screenArray[x,y] != 0)
                    {
                        Console.Write("*");
                    }
                    else
                    {
                        Console.Write(" ");
                    }
                }
                Console.WriteLine("|");
            }
            //Drawing the inferior borders.
            Console.WriteLine("\\" + "".PadRight(SCREEN_WIDTH-1, '-') + "/");
            Console.WriteLine("");
        }

        public void SkipIfKeyDown()
        {
            if (pressedKeys[keyMapping[V[optcode2]]] == true)
            {
                PC += 2;
            }
        }

        public void SkipIfKeyUp()
        {
            if (pressedKeys[keyMapping[V[optcode2]]] == false)
            {
                PC += 2;
            }
        }

        public void AssignFromDelay()
        {
            V[optcode2] = delayTimer;
        }

        public void StoreKey()
        {
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                if (pressedKeys[i] == true)
                {
                    V[optcode2] = i;
                }
            }
        }

        public void AssignToDelay()
        {
            delayTimer = V[optcode2];
        }

        public void AssignRegisterToSound()
        {
            soundTimer = V[optcode2];
            executeSound = true;
        }

        public void AddRegisterToIndex()
        {
            I += V[optcode2];
        }

        public void IndexAtFontC8()
        {
            I = (V[optcode2] * 0x05);
        }

        public void StoreBSD()
        {
            int vx = (int)V[optcode2];
            memory[I] = vx / 100; //cents.
            memory[I + 1] = (vx / 10) % 10; //tents.
            memory[I + 2] = vx % 10; //units.
        }

        public void SaveRegisters()
        {
            for (int i = 0; i < optcode2; i++)
            {
                memory[I++] = V[i];
            }
        }

        public void LoadRegisters()
        {
            for (int i = 0; i < optcode2; i++)
            {
                V[i] = memory[I++];
            }
        }
        #endregion

    }

    class Program
    { 
        static void Main(string[] args)
        {
            Console.WriteLine("Emulator");
            //Instantiating our emulator, and calling our Run method.
            Emulator emulator = new Emulator();
            emulator.Run();
            Console.WriteLine("Hello World!");
        }

    }
}
