using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using AdafruitClassLibrary;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using System.Xml.Linq;
using Fireflies;
//using Bekker.Adafruit.PCA9685PwmDriver;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace flasher
{
    public sealed class StartupTask : IBackgroundTask
    {
        PCA9685 p;
        Swarm s;
        public int TimerStep { get; set; }

        public ThreadPoolTimer timer { get; private set; }

  

        public async void Run(IBackgroundTaskInstance taskInstance)
        {

            int FireFlyCount = 32;


            //    loadSpeciesList();
            //    while (true)

            s = new Swarm(FireFlyCount, taskInstance);
            s.StartFlashing(TimerStep);
            

            //            this.timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(100));

            //    for (int i = 0; i < 1000; i++)
            //    {
            //           TimerStep++;

            //      }
            //BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            //p = new PCA9685(0x40);
            // await p.InitPCA9685Async();

            //p.SetPWMFrequency(1000);
            //while (true)
            //{
            // //   p.SetPWM(1, 4096, 0); // full on wired to GND, off wired to +3.3v
            //   // await Task.Delay(1500);
            //    //p.SetPWM(1, 2000, 0);
            //    //await Task.Delay(500);
            //    p.SetPWM(1, 0, 2000); //full off wired to GND, on wired to +3.3v
            //    await Task.Delay(500);
            //    p.SetPWM(1, 4096, 0);
            //    await Task.Delay(1500);
            //    p.SetPWM(1, 0, 4096);
            //    await Task.Delay(5000);


            //    //for (ushort fadeValue = 4095; fadeValue >= 0; fadeValue -= 500)
            //    //{
            //    //    // sets the value (range from 0 to 255):
            //    //    p.SetPWM(1, fadeValue, 0);
            //    //    // wait for 30 milliseconds to see the dimming effect
            //    //    await Task.Delay(5);
            //    //}

            //    //p.SetAllPWM(14096, 0);
            //    //await Task.Delay(500);
            //    //p.SetAllPWM( 0, 4096);
            //    //await Task.Delay(500);
            //}
        }
        private void Timer_Tick(ThreadPoolTimer timer)
        {
            if (s.SwarmInitialized)
            {
                TimerStep++;
                s.StartFlashing(TimerStep);
            }
        }

    }

}
