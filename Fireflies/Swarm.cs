using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Background;

namespace Fireflies
{
    public class Swarm
    {
        public const ushort Light_HI = 0;
        public const ushort Light_OFF = 4096;

        //13 when debug on
        //higher when off
        public const ushort interval =18;
        public const ushort MaxInitialDelay = 10000;
        public const int ProcessTime =30000;
        public byte address = 0x40;
        public bool SwarmInitialized { get; set; }
        public int SwarmGroupCount { get; set; }
        public List<SwarmGroup> SwarmGroupList { get; set; }
        private List<Fireflies.Species> SpeciesList;

        public int LiveQuantity { get; set; }


        public Swarm(int quantity, IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            SwarmGroupList = new List<SwarmGroup>();
            loadSpeciesList();
            LoadUpSwarms(quantity);
            SwarmInitialized = true;
        }
        private async void loadSpeciesList()
        {
            XDocument doc = XDocument.Load("FireFlyData.xml");
            var xmldoc = XDocument.Parse(doc.ToString());
            var SpList = (from mainRequest in xmldoc.Descendants("Species")
                          select new
                          {
                              Name = mainRequest.Element("Name").Value,
                              ShortName = mainRequest.Element("ShortName").Value,
                              Flashes = (from flash in mainRequest.Descendants("Flash")
                                         select new
                                         {
                                             sex = flash.Element("sex").Value,
                                             ResponseTime = flash.Element("ResponseTime").Value,
                                             QorA = flash.Element("QorA").Value,
                                             quantity = flash.Element("quantity").Value,
                                             TaperMultiple = flash.Element("TaperMultiple").Value,
                                             delay = flash.Element("delay").Value,
                                             Tapers = (from taper in flash.Descendants("Taper")
                                                       select new
                                                       {
                                                           Repeat = taper.Element("Repeat").Value,
                                                           RepeatQty = taper.Element("RepeatQty").Value,
                                                           RepeatDelay = taper.Element("RepeatDelay").Value,
                                                           StartIntensity = taper.Element("StartIntensity").Value,
                                                           Duration = taper.Element("Duration").Value,
                                                           EndIntensity = taper.Element("EndIntensity").Value,
                                                           TaperDirection = taper.Element("TaperDirection").Value
                                                       })
                                         })
                          });


            SpeciesList = new List<Fireflies.Species>();

            foreach (var i in SpList)
            {
                //    Fireflies.FireFly f = new Fireflies.FireFly();
                Species s = new Species();

                s.Name = i.Name;
                s.ShortName = i.ShortName;
                s.Flashes = new List<Flash>();
                foreach (var j in i.Flashes)
                {
                    Flash fl = new Flash();
                    fl.Tapers = new List<Taper>();
                    fl.sex = j.sex;
                    fl.QorA = j.QorA;
                    fl.ResponseTime = Convert.ToDouble(j.ResponseTime);
                    //   fl.Duration = Convert.ToDouble(j.Duration);
                    fl.delay = Convert.ToDouble(j.delay);
                    fl.TaperMultiple = Convert.ToBoolean(j.TaperMultiple);
                    fl.quantity = Convert.ToInt16(j.quantity);
                    //       fl.Intensity = Convert.ToInt16(j.Intensity);

                    foreach (var k in j.Tapers)
                    {
                        Taper t = new Taper();
                        t.Repeat = Convert.ToBoolean(k.Repeat);
                        t.RepeatQty = Convert.ToInt16(k.RepeatQty);
                        t.RepeatDelay = Convert.ToDouble(k.RepeatDelay);
                        t.Duration = Convert.ToUInt16(k.Duration);
                        t.EndIntensity = Convert.ToUInt16(k.EndIntensity);
                        t.StartIntensity = Convert.ToUInt16(k.StartIntensity);
                        switch (k.TaperDirection)
                        {
                            case "UP":
                                t.TaperDirection = Taper.TaperType.UP;
                                break;
                            case "DOWN":
                                t.TaperDirection = Taper.TaperType.DOWN;
                                break;
                            case "NONE":
                                t.TaperDirection = Taper.TaperType.NONE;
                                break;
                            case "FLAT":
                                t.TaperDirection = Taper.TaperType.FLAT;
                                break;
                            default:
                                t.TaperDirection = Taper.TaperType.NONE;
                                break;
                        }
                        fl.Tapers.Add(t);
                    }
                    s.Flashes.Add(fl);
                }
                SpeciesList.Add(s);
            }
        }
        private Species GetRandomSpecies()
        {
            Random rand = new Random();
            int RandomNumber = rand.Next(SpeciesList.Count - 1);
            return SpeciesList[RandomNumber];
        }

        public void reset()
        {


        }

        private async void LoadUpSwarms(int qty)
        {
            LiveQuantity = qty;
            int StandAloneCount = LiveQuantity % 16;

            SwarmGroupCount = (int)Math.Ceiling((decimal)(LiveQuantity / 16));
            int RemainingGroupCount = SwarmGroupCount;

            for (int i = 0; i < SwarmGroupCount; i++)
            {
                int FillLevel;

                if (RemainingGroupCount == 1 && StandAloneCount != 0)
                    FillLevel = StandAloneCount;
                else
                    FillLevel = 16;
                Species s = GetRandomSpecies();
                SwarmGroup sg = new SwarmGroup(address, FillLevel, s);
                //         await sg.InitPCA9685Async();

                RemainingGroupCount -= 1;
                SwarmGroupList.Add(sg);
                address += 1;
            }
        }

        public async Task StartFlashing(int step)
        {

            //generate the pattern

            while (true)
            {
                Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " Start");
                foreach (SwarmGroup sg in SwarmGroupList)
                               await   sg.GenerateBuffer();
                Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " End Buffer Generation");

              
                for (int i = 0; i < Swarm.ProcessTime; i++)
                                    foreach (SwarmGroup sg in SwarmGroupList)
                        sg.SetAllLights(i);
                Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " End Flash");

                
                foreach (SwarmGroup sg in SwarmGroupList)
               await     sg.ResetSwarmGroup();
                Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " End Reset");

                foreach (SwarmGroup sg in SwarmGroupList)
                    sg.allOff();
                Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " End Off");

            }
        
            

        }
    }
}
