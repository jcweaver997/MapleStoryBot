
using System;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Interceptor;

using Keys = Interceptor.Keys;

namespace maplestorybot
{
    public class MS2
    {
        public Bitmap imageGrab;
        public GpuMat playerimg, bossimg, imgGrab;
        public Graphics graphics;
        public IntPtr mainWindowHandle;
        public Process mainProcess;
        public Rect windowRect;
        public const int windowSizeX = 1286, windowSizeY = 989;
        public int borderx, bordery;
        public Input input;
        public CudaTemplateMatching ctm = new CudaTemplateMatching(DepthType.Cv8U, 1, TemplateMatchingType.CcorrNormed);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public struct PlayerBossPos
        {
            public int px{ get; set; }
            public int py{ get; set; } 
            public int bx{ get; set; } 
            public int by{ get; set; }
        }
        
        public struct Rect {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }
        
        public MS2()
        {
            Form fr = new Form();
            fr.Width = 300;
            fr.Height = 200;
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Color.Red;
            fr.Controls.Add(p);
            fr.Show();
            borderx = (300 - p.Width) /2;
            bordery = 200 - p.Height - borderx;
            fr.Dispose();
            Console.WriteLine(borderx + ", "+bordery);

        }

        public void Start()
        {
            FindGame();

            imageGrab = new Bitmap(windowSizeX, windowSizeY);
            graphics = Graphics.FromImage(imageGrab);
            imgGrab = new GpuMat(new Image<Bgr, byte>(imageGrab));
            playerimg = new GpuMat(new Image<Bgr, byte>("images/player.png"));
            bossimg = new GpuMat(new Image<Bgr, byte>("images/boss.png"));
            input = new Input();
            input.KeyboardFilterMode = KeyboardFilterMode.All;
            input.Load();
        }

        public bool CheckGameSize()
        {
            GetWindowRect(mainWindowHandle, ref windowRect);
            return true;
            if (windowRect.Right-windowRect.Left != windowSizeX || windowRect.Bottom-windowRect.Top != windowSizeY)
            {
                Console.WriteLine("wrong size "+ (windowRect.Right-windowRect.Left)+" "+(windowRect.Bottom-windowRect.Top));
                return false;
            }
            
        }
        
        public void FindGame()
        {
            if (mainProcess != null)
            {
                if (!mainProcess.HasExited)
                {
                    return;
                }
            }
            Process[] processes = Process.GetProcesses();
            foreach (Process proc in processes)
            {
                if (proc.ProcessName.Equals("MapleStory2"))
                {
                    mainProcess = proc;
                    mainWindowHandle = mainProcess.MainWindowHandle;
                    break;
                }
           }
        }

        public bool Ms2Focus()
        {
            return mainWindowHandle == GetForegroundWindow();
        }
        
        public void WaitGameToFront()
        {
            FindGame();
            while (!Ms2Focus() || !CheckGameSize())
            {
                //CvInvoke.DestroyWindow("debug");
                System.Threading.Thread.Sleep(100);
            }

            
        }

        public void GrabImage(int left = 0, int top = 0, int sizex = windowSizeX, int sizey = windowSizeY)
        {
            WaitGameToFront();
            graphics.CopyFromScreen(windowRect.Left+left+borderx-8, windowRect.Top+top+bordery-31, 0, 0, new Size(sizex,sizey));
            imgGrab.Dispose();
            imgGrab = new GpuMat(new Image<Bgr, Byte>(imageGrab));
        }

        public PlayerBossPos GetPlayerAndBossLocation()
        {
            PlayerBossPos ret = new PlayerBossPos();
            GrabImage(548,82,730,706);
            Mat temp = new Mat(imgGrab.ToMat(), new Rectangle(0, 0, 730, 706));
            GpuMat result = new GpuMat(temp);
            GpuMat output = new GpuMat(temp);
            temp.Dispose();
            
            ctm.Match(result,playerimg,output);
            double minval=0, maxval=0;
            Point maxloc = new Point(), minloc = new Point();
            CudaInvoke.MinMaxLoc(output,ref minval,ref maxval,ref minloc,ref maxloc);

            ctm.Match(result,bossimg,output);
            double minvalb=0, maxvalb=0;
            Point maxlocb = new Point(), minlocb = new Point();
            CudaInvoke.MinMaxLoc(output,ref minvalb,ref maxvalb,ref minlocb,ref maxlocb);


            ret.px = maxloc.X;
            ret.py = maxloc.Y;
            ret.bx = maxlocb.X;
            ret.by = maxlocb.Y;
            result.Dispose();
            output.Dispose();
            return ret;
        }

        public void WalkRel(int locx, int locy)
        {
            var pos = GetPlayerAndBossLocation();
            float xspeed = 1f/28*1000;
            float yspeed = 1f/19*1000;
            float xspeedd = 1f/20f*1000;
            float yspeedd = 1f/13.75f*1000;
            
            bool ih=false, id=false;
            if (locx>0)
            {
                ih = true;
                Console.WriteLine("right");
                input.SendKey(Keys.F12,KeyState.Down);
            }
            else if(locx<0)
            {
                ih = true;
                Console.WriteLine("left");
                input.SendKey(Keys.F11,KeyState.Down);
            }
            if (locy>0)
            {
                if (ih) id = true;
                Console.WriteLine("down");
                input.SendKey(Keys.F10,KeyState.Down);
            }
            else if(locy<0)
            {
                if (ih) id = true;
                Console.WriteLine("up");
                input.SendKey(Keys.F9,KeyState.Down);
            }

            float xmag = Math.Abs(locx);
            float ymag = Math.Abs(locy);
            if (id)
            {
                Thread.Sleep((int) Math.Min(ymag*yspeedd,xmag*xspeedd));
                if (ymag*yspeedd<=xmag*xspeedd)
                {
                    input.SendKey(Keys.F9,KeyState.Up);
                    input.SendKey(Keys.F10,KeyState.Up);
                    Thread.Sleep((int)((xmag*xspeedd-ymag*yspeedd)/xspeedd*xspeed));
                }
                else
                {
                    input.SendKey(Keys.F11,KeyState.Up);
                    input.SendKey(Keys.F12,KeyState.Up);
                    Thread.Sleep((int)((ymag*yspeedd-xmag*xspeedd)/yspeedd*yspeed));
                }
            }
            else
            {
                Thread.Sleep((int) Math.Max(ymag*yspeed,xmag*xspeed));
            }

            StopMoving();
            
            var pos2 = GetPlayerAndBossLocation();
            Console.WriteLine("Moved "+(pos2.px-pos.px)+", "+(pos2.py-pos.py));
            Console.WriteLine("Target: "+(locx)+", "+(locy));
            Console.WriteLine("Error: "+(locx-(pos2.px-pos.px))+", "+(locy-(pos2.py-pos.py)));
            
            
        }

        public void StopMoving()
        {
            input.SendKey(Keys.F9, KeyState.Up);
            input.SendKey(Keys.F10, KeyState.Up);
            input.SendKey(Keys.F11, KeyState.Up);
            input.SendKey(Keys.F12, KeyState.Up);
        }

        public void WalkTowards(int px, int py, int targetx, int targety, int accuracy = 2)
        {
            if (targetx-px > accuracy)
            {
                //Console.WriteLine("right");
                input.SendKey(Keys.F11, KeyState.Up);
                input.SendKey(Keys.F12, KeyState.Down);
            }
            else if (targetx - px < -accuracy)
            {
                //Console.WriteLine("left");
                input.SendKey(Keys.F12, KeyState.Up);
                input.SendKey(Keys.F11, KeyState.Down);
            }
            else
            {
                input.SendKey(Keys.F11, KeyState.Up);
                input.SendKey(Keys.F12, KeyState.Up);
            }

            if (targety-py> accuracy)
            {
                //Console.WriteLine("down");
                input.SendKey(Keys.F9, KeyState.Up);
                input.SendKey(Keys.F10, KeyState.Down);
            }
            else if (targety - py < -accuracy)
            {
                //Console.WriteLine("up");
                input.SendKey(Keys.F10, KeyState.Up);
                input.SendKey(Keys.F9, KeyState.Down);
            }
            else
            {
                input.SendKey(Keys.F9, KeyState.Up);
                input.SendKey(Keys.F10, KeyState.Up);
            }
        }
        
        public void WalkTo(int locx, int locy)
        {
            var pos = GetPlayerAndBossLocation();
            float xspeed = 1f/28*1000;
            float yspeed = 1f/19*1000;
            float xspeedd = 1f/20f*1000;
            float yspeedd = 1f/13.75f*1000;

            bool ih=false, id=false;
            if (pos.px<locx)
            {
                ih = true;
                Console.WriteLine("right");
                input.SendKey(Keys.F12,KeyState.Down);
            }
            else if(pos.px>locx)
            {
                ih = true;
                Console.WriteLine("left");
                input.SendKey(Keys.F11,KeyState.Down);
            }
            if (pos.py<locy)
            {
                if (ih) id = true;
                Console.WriteLine("down");
                input.SendKey(Keys.F10,KeyState.Down);
            }
            else if(pos.py>locy)
            {
                if (ih) id = true;
                Console.WriteLine("up");
                input.SendKey(Keys.F9,KeyState.Down);
            }

            float xmag = Math.Abs(pos.px - locx);
            float ymag = Math.Abs(pos.py - locy);
            if (id)
            {
                Thread.Sleep((int) Math.Min(ymag*yspeedd,xmag*xspeedd));
                if (ymag*yspeedd<=xmag*xspeedd)
                {
                    input.SendKey(Keys.F9,KeyState.Up);
                    input.SendKey(Keys.F10,KeyState.Up);
                    Thread.Sleep((int)((xmag*xspeedd-ymag*yspeedd)/xspeedd*xspeed));
                }
                else
                {
                    input.SendKey(Keys.F11,KeyState.Up);
                    input.SendKey(Keys.F12,KeyState.Up);
                    Thread.Sleep((int)((ymag*yspeedd-xmag*xspeedd)/yspeedd*yspeed));
                }
            }
            else
            {
                Thread.Sleep((int) Math.Max(ymag*yspeed,xmag*xspeed));
            }

            input.SendKey(Keys.F9,KeyState.Up);
            input.SendKey(Keys.F10,KeyState.Up);
            input.SendKey(Keys.F11,KeyState.Up);
            input.SendKey(Keys.F12,KeyState.Up);
            
            var pos2 = GetPlayerAndBossLocation();
            Console.WriteLine("Moved "+(pos2.px-pos.px)+", "+(pos2.py-pos.py));
            Console.WriteLine("Target: "+(locx-pos.px)+", "+(locy-pos.py));
            Console.WriteLine("Error: "+(locx-pos2.px)+", "+(locy-pos2.py));

        }

        public void movetests()
        {
            //            Horizontal speed tests
//            input.SendKey(Keys.F12,KeyState.Down);
//            Thread.Sleep(1000);
//            input.SendKey(Keys.F12,KeyState.Up);
//            var pos3 = GetPlayerAndBossLocation();
//            Console.WriteLine("moved "+(pos3.px-pos.px));
//            
//            input.SendKey(Keys.F9,KeyState.Down);
//            Thread.Sleep(1000);
//            input.SendKey(Keys.F9,KeyState.Up);
//            var pos3 = GetPlayerAndBossLocation();
//            Console.WriteLine("moved "+(pos3.py-pos.py));

//            diag speed tests
//            input.SendKey(Keys.F12,KeyState.Down);
//            input.SendKey(Keys.F9,KeyState.Down);
//            Thread.Sleep(1000);
//            input.SendKey(Keys.F12,KeyState.Up);
//            input.SendKey(Keys.F9,KeyState.Up);
//            var pos3 = GetPlayerAndBossLocation();
//            Console.WriteLine("moved "+(pos3.px-pos.px));
//            Console.WriteLine("moved "+(pos3.py-pos.py));
        }

        public void ProcessImagePlayerLocation()
        {
            GrabImage();
            Mat result = new Mat();

            CvInvoke.MatchTemplate(new Mat(imgGrab.ToMat(), new Rectangle(545,80,740,720)), playerimg, result,TemplateMatchingType.CcoeffNormed);
            double minval=0, maxval=0;
            Point maxloc = new Point(), minloc = new Point();
            CvInvoke.MinMaxLoc(result,ref minval,ref maxval,ref minloc,ref maxloc);
            CvInvoke.Rectangle(imgGrab,new Rectangle(maxloc.X+545,maxloc.Y+80,10,10), new MCvScalar(1,0,0));
            CvInvoke.Imshow("debug", imgGrab);
            CvInvoke.WaitKey(1);
        }
        
    }
}