using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Interceptor;

namespace maplestorybot
{
    public class Fishing
    {
        private MS2 ms2;
        private Image<Bgr, byte> bobber;
        private Image<Bgr, byte> bobbermask;
        private FishingState fishingState;
        private bool UseLure;
        private int luresUsed, fishGames,fishAttempts;
        private enum FishingState
        {
            NotCast,FindingFish, CatchingFish
        }
        
        public Fishing(MS2 ms2)
        {
            this.ms2 = ms2;
            bobber = new Image<Bgr, byte>("images/bobber2.png");
            bobbermask = new Image<Bgr, byte>("images/bobber2mask.png");
            fishingState = FishingState.NotCast;
        }

        public void UseLureThread()
        {
            luresUsed += 1;
            Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt")+" Using Lure, used "+luresUsed+" so far");
            ms2.input.SendKey(Keys.R, KeyState.Down);
            Thread.Sleep(300);
            ms2.input.SendKey(Keys.R, KeyState.Up);
            Thread.Sleep(500);
            
            
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Thread.Sleep(2000);
            for (;;)
            {
                if (sw.ElapsedMilliseconds>=1000*60*60*3 && ms2.Ms2Focus()) //1000*60*60*3
                {
                    luresUsed += 1;
                    Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt")+" Using Lure, used "+luresUsed+" so far");
                    ms2.input.SendKey(Keys.R, KeyState.Down);
                    Thread.Sleep(300);
                    ms2.input.SendKey(Keys.R, KeyState.Up);
                    Thread.Sleep(500);
                    sw.Reset();
                    sw.Start();
                    
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
        
        public void Start()
        {
            Console.WriteLine("Starting JC MS2 Fishing Bot v2.2se");
            Console.WriteLine("Limited edition build!");
            Console.WriteLine("Make sure your game resolution is 1280x960");
            Console.WriteLine("Make sure your interface size is 50");
            Console.Write("Would you like to use 3hr Lures on the R key? (y/n): ");
            string input = Console.ReadLine();
            if (input.StartsWith("y"))
            {
                Console.WriteLine("I will use Lures using the R key every 3 hours.");
                UseLure = false;
            }
            else
            {
                Console.WriteLine("Lures disabled.");
                UseLure = true;
            }
            
            Console.WriteLine("Once you cast your line, I will take over");
            
            float testpercent = getFishProgress();
            while (testpercent==0)
            {
                Thread.Sleep(1000);
                testpercent = getFishProgress();
            }
            Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt")+" STARTING");
            if (UseLure)
            {
                new Thread(UseLureThread).Start();
            }
            while (true)
            {
                Thread.Sleep(20);
                float percent;
                float bob;
                switch (fishingState)
                {
                        case FishingState.NotCast:
                            if (!ms2.Ms2Focus())
                            {
                                ms2.WaitGameToFront();
                                Thread.Sleep(2000);
                            }
                            percent = getFishProgress();
                            
                            if (percent>0)
                            {
                                fishingState = FishingState.FindingFish;
                            }
                            else
                            {
                                ms2.input.SendKey(Keys.Space, KeyState.Down);
                                Thread.Sleep(300);
                                ms2.input.SendKey(Keys.Space, KeyState.Up);
                                Thread.Sleep(500);
                            }

                            break;
                        case FishingState.FindingFish:
                            percent = getFishProgress();
                            if (percent==0)
                            {
                                Thread.Sleep(400);
                                bob = getBobberLocation();
                                if (bob==-1)
                                {
                                    fishAttempts += 1;
                                    Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt")+" fishing attempts: " +fishAttempts);
                                    fishingState = FishingState.NotCast;
                                    Thread.Sleep(1000);
                                }
                                else
                                {
                                    fishGames += 1;
                                    Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt")+" CATCHING!!! fish games so far: "+fishGames);
                                    fishingState = FishingState.CatchingFish;
                                }

                            }
                            break;
                        case FishingState.CatchingFish:
                            bob = getBobberLocation();
                            if (bob > 0)
                            {
                                if (bob > .45)
                                {
                                    ms2.input.SendKey(Keys.Space, KeyState.Down);
                                    Thread.Sleep(50);
                                    ms2.input.SendKey(Keys.Space, KeyState.Up);
                                    Thread.Sleep(100);
                                }
                            }
                            else
                            {
                                fishingState = FishingState.NotCast;
                            }
                            
                            break;
                }
            }
        }

        private float getFishProgress()
        {
            ms2.GrabImage(512,747,262,1);
            Image<Hsv, byte> yellowbox = new Image<Hsv, byte>(300, 1);
            yellowbox = new Mat(ms2.imgGrab.ToMat(), new Rectangle(0,0, 262, 1)).ToImage<Hsv,byte>();
            Hsv lowerLimit = new Hsv(45, 175, 175);
            Hsv upperLimit = new Hsv(62, 255, 255);
            Image<Gray, byte> result = yellowbox.InRange(lowerLimit, upperLimit);
           // CvInvoke.Imshow("debug", yellowbox);
            //CvInvoke.WaitKey(1);
            float percent = result.CountNonzero()[0] / 262.0f;
            return percent;
        }

        private float getBobberLocation()
        {
            ms2.GrabImage(580,512,20,123);
            
            Mat result = new Mat(ms2.imgGrab.ToMat(), new Rectangle(0, 0, 20, 123));
            CvInvoke.MatchTemplate(result, bobber, result,TemplateMatchingType.CcorrNormed,bobbermask);
            double minval=0, maxval=0;
            Point maxloc = new Point(), minloc = new Point();
            CvInvoke.MinMaxLoc(result,ref minval,ref maxval,ref minloc,ref maxloc);
            //CvInvoke.Rectangle(ms2.imgGrab.ToMat(),new Rectangle(maxloc.X-5,maxloc.Y-5,10,10), new MCvScalar(1,1,1));
            //Console.WriteLine(maxloc.X+", "+maxloc.Y +" : "+maxval);
            if (maxval<.95)
            {
                return -1;
            }
            return maxloc.Y / 123f;
        }
    }
}