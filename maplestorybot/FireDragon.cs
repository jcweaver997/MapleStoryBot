

using System;
using System.Diagnostics;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Cuda;
using System.Drawing;
using Emgu.CV.Structure;
using Interceptor;


namespace maplestorybot
{
    public class FireDragon
    {
        private MS2 ms2;
        private Phase currentPhase;
        private GpuMat fireimg;
        private Point[] firstholes = { new Point(312,524), new Point(342,514), new Point(374,524), new Point(273,452),
         new Point(404,492), new Point(358,492), new Point(381,487), new Point(311,492),
         new Point(273,497), new Point(328,482), new Point(388,470), new Point(404,459),
         new Point(366,443), new Point(351,464), new Point(335,453), new Point(304,465),new Point(304,443)};
        private Point Buff = new Point(320, 426);

        private enum Phase
        {
            WalkAcrossBridge,First,Second
        }
        
        
        
        public FireDragon(MS2 ms2)
        {
            this.ms2 = ms2;
            currentPhase = Phase.WalkAcrossBridge;
            fireimg = new GpuMat(new Image<Bgr,byte>("images/fire.png"));
        }

        public void Start()
        {
                MS2.PlayerBossPos pos2 = ms2.GetPlayerAndBossLocation();
                Console.WriteLine(pos2.px + ", " + pos2.py);
                Thread.Sleep(2000);



            //            System.Threading.Thread.Sleep(2000);
            //ms2.WalkRel(5,5);
            //ms2.WalkTo(230,587);
            //ms2.WalkTo(198,612);
            //return;
            for (;;)
            {

                switch (currentPhase)
                {
                       case Phase.WalkAcrossBridge:
                           WalkBridge();
                           break;
                       case Phase.First:
                           FirstPhase();
                           break;
                    case Phase.Second:

                        break;
                       default:
                           break;    
                }
                
            }
        }

        public void WalkBridge()
        {
            ms2.WalkTo(270,561);
            ms2.WalkTo(280,570);
            ms2.WalkRel(0,-40);
            currentPhase = Phase.First;
        }

        public bool OnFire()
        {
            ms2.GrabImage(690,816,170,29);
            Mat temp = new Mat(ms2.imgGrab.ToMat(), new Rectangle(0, 0, 170, 29));
            GpuMat result = new GpuMat(temp);
            GpuMat output = new GpuMat(temp);
            temp.Dispose();

            ms2.ctm.Match(result, fireimg, output);
            double minval = 0, maxval = 0;
            Point maxloc = new Point(), minloc = new Point();
            CudaInvoke.MinMaxLoc(output, ref minval, ref maxval, ref minloc, ref maxloc);
            if (maxval>.9f)
            {
                return true;
            }
            return false;
        }

        public Point getClosestFairy(int px, int py)
        {
            // left 235, 490
            // right 357, 576
            Point p = new Point();
            int distleft = Math.Abs(235 - px) + Math.Abs(490 - py);
            int distright = Math.Abs(357 - px) + Math.Abs(576 - py);
            if (distleft < distright)
            {
                p.X = 235;
                p.Y = 490;
            }
            else
            {
                p.X = 357;
                p.Y = 576;
            }
            return p;
        }

        public bool inHole(int px, int py)
        {
            for (int i = 0; i < firstholes.Length; i++)
            {
                if(Math.Abs(px-firstholes[i].X)<=6 && Math.Abs(py - firstholes[i].Y) <= 4)
                {
                    return true;
                }
            }

            return false;
        }

        public void walkAroundBoss()
        {
            int offset = 20;
            float radians = 0;
            Thread attackt = new Thread(Attack);
            bool notincorner = true;
            bool direction = false;
            
            attackt.Start();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            MS2.PlayerBossPos posprev = new MS2.PlayerBossPos();
            while (notincorner)
            {
                MS2.PlayerBossPos pos = ms2.GetPlayerAndBossLocation();
                if (pos.by<415&&false)
                {
                    Console.WriteLine("Second Phase "+pos.by);
                    ms2.StopMoving();
                    currentPhase = Phase.Second;
                    break;
                }

                if (OnFire())
                {
                    Point p = getClosestFairy(pos.px, pos.py);
                    ms2.WalkTowards(pos.px, pos.py, p.X, p.Y,3);
                    if (inHole(pos.px, pos.py))
                    {
                        ms2.WalkTowards(pos.px, pos.py, pos.px, pos.py - 1, 0);
                        Thread.Sleep(100);
                        ms2.input.SendKey(Keys.RightAlt, KeyState.Down);
                        Thread.Sleep(100);
                        ms2.input.SendKey(Keys.RightAlt, KeyState.Up);
                    }
                    continue;
                }else if (pos.px<=265)
                {
                    ms2.WalkTowards(pos.px, pos.py, pos.px+1, pos.py,0);
                    Thread.Sleep(250);
                    ms2.input.SendKey(Keys.RightAlt, KeyState.Down);
                    Thread.Sleep(100);
                    ms2.input.SendKey(Keys.RightAlt, KeyState.Up);
                    Thread.Sleep(100);
                    ms2.input.SendKey(Keys.RightAlt, KeyState.Down);
                    Thread.Sleep(100);
                    ms2.input.SendKey(Keys.RightAlt, KeyState.Up);
                    continue;
                }
                else if (pos.py>=550)
                {
                    ms2.WalkTowards(pos.px, pos.py, pos.px, pos.py-1,0);
                    Thread.Sleep(250);
                    ms2.input.SendKey(Keys.RightAlt, KeyState.Down);
                    Thread.Sleep(100);
                    ms2.input.SendKey(Keys.RightAlt, KeyState.Up);
                    Thread.Sleep(100);
                    ms2.input.SendKey(Keys.RightAlt, KeyState.Down);
                    Thread.Sleep(100);
                    ms2.input.SendKey(Keys.RightAlt, KeyState.Up);
                    continue;
                }
                else if(posprev.px == pos.px && pos.py == posprev.py){
                    if (inHole(pos.px, pos.py))
                    {
                        ms2.WalkTowards(pos.px, pos.py, pos.px, pos.py - 1,0);
                        Thread.Sleep(100);
                        ms2.input.SendKey(Keys.RightAlt, KeyState.Down);
                        Thread.Sleep(100);
                        ms2.input.SendKey(Keys.RightAlt, KeyState.Up);
                    }
                    continue;
                }

                int targetx = 0, targety = 0;
                int mag = GetMag(pos.px,pos.py,pos.bx,pos.by);
                Console.WriteLine("pr " + mag);
                if (mag>40)
                {
                    float rad = (float)(Math.Atan((pos.by-pos.py)/(pos.bx-pos.py))+Math.PI/2);
                    
                    targetx = (int)(Math.Cos(rad) * mag*.75f + pos.bx);
                    targety = (int)(Math.Sin(rad) * mag*.75f + pos.by);
                }
                else
                {
                    targetx = (int)(Math.Cos(radians) * offset + pos.bx);
                    targety = (int)(Math.Sin(radians) * offset + pos.by);
                }

                if (targetx > 413)
                {
                    direction = !direction;
                    targetx = 413;
                }
                if (targety > 522)
                {
                    direction = !direction;
                    targety = 522;
                }
                if (targetx < 275)
                {
                    direction = !direction;
                    targetx = 275;
                }
                if (targety < 437)
                {
                    direction = !direction;
                    targety = 437;
                }
                ms2.WalkTowards(pos.px, pos.py, targetx, targety,0);

                radians = (float) (sw.ElapsedMilliseconds/1000f*Math.PI/3*(direction?1:-1));
                //Console.WriteLine("rad "+radians/Math.PI);
                //Thread.Sleep(10);
                posprev = pos;
            }
            
            attackt.Interrupt();

        }

        public int GetMag(int px, int py, int bx, int by)
        {
            return (int)Math.Sqrt(Math.Pow(px-bx,2)+Math.Pow(py-by,2));
        }

       

        public void Attack()
        {
            while (true)
            {
                if (ms2.Ms2Focus())
                {
                    try
                    {
                        ms2.input.SendKey(Keys.Q, KeyState.Down);
                        Thread.Sleep(100);
                        ms2.input.SendKey(Keys.Q, KeyState.Up);
                        Thread.Sleep(1600);
                    }
                    catch (Exception e)
                    {
                        ms2.input.SendKey(Keys.Q, KeyState.Up);
                    }

                }

            }
        }
        

        public void FirstPhase()
        {
            walkAroundBoss();
        }
        
    }
    

    
}