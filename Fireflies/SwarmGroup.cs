using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.Security.Cryptography;
//using Windows.Security.Cryptography;

namespace Fireflies
{
    public class SwarmGroup : AdafruitClassLibrary.PCA9685
    {
        public int FireflyCount { get; set; }
        public byte Address { get; set; }
        public Species SwarmSpecies { get; set; }
        public List<FireFly> FireFlies { get; set; }
        public List<ushort> LEDstatus { get; set; }
        public string MateString { get; set; }

        public List<string> PossibleMateStrings { get; set; }

        public byte[,,] buffer;

        public SwarmGroup(byte address, int count, Species s) : base(address)
        {

            buffer = new byte[16, Swarm.ProcessTime, 5];
            LEDstatus = new List<ushort>();
            Address = address;
            FireflyCount = count;
            initSwarmGroup();
            FireFlies = new List<FireFly>();
            SwarmSpecies = s;
            GenerateMateString();
            InitializeFireFlies();
            SetUpMates();
        }
        public void allOff()
        {
            SetAllPWM(Swarm.Light_OFF, 0);

        }

        public async void initSwarmGroup()
        {
            await InitPCA9685Async();
            SetPWMFrequency(1500);
            allOff();
            //fill hex table for debug
            CreateLookup32();
        }

        public void InitializeFireFlies()
        {
            string Sex = "M";

            for (int i = 0; i < FireflyCount; i++)
            {
                FireFly f = new FireFly(SwarmSpecies, Sex);
                f.ID = i;
                FireFlies.Add(f);

                //alternate gender
                Sex = (Sex == "M") ? "F" : "M";
            }
        }
        public void SetUpMates()
        {
            List<FireFly> males = (from c in FireFlies where c.Sex == "M" select c).ToList();
        //    List<FireFly> females = (from c in FireFlies where c.Sex == "F" select c).ToList();

            foreach (FireFly f in males)
            {
                int FemaleID = DecodeMateString(f.ID);
                f.MateID = FemaleID;
                FireFlies[FemaleID].MateID = f.ID;
                syncDelays(f, FireFlies[FemaleID]);
            }
        }

        public void syncDelays(FireFly male, FireFly female)
        {
            female.InitialDelay = male.InitialDelay;
            female.DelayInstructionSet = male.DelayInstructionSet;
        }

        public int DecodeMateString(int MaleID)
        {
            char FemaleIDstr;
            int FemaleID;
            List<char> mates = MateString.ToList();

            if (MaleID != 0)
                MaleID = MaleID / 2;

            FemaleIDstr = mates[MaleID];

            switch (FemaleIDstr)
            {
                case '1':
                    FemaleID = 1;
                    break;
                case '3':
                    FemaleID = 3;
                    break;
                case '5':
                    FemaleID = 5;
                    break;
                case '7':
                    FemaleID = 7;
                    break;
                case '9':
                    FemaleID = 9;
                    break;
                case 'B':
                    FemaleID = 11;
                    break;
                case 'D':
                    FemaleID = 13;
                    break;
                case 'F':
                    FemaleID = 15;
                    break;
                default:
                    FemaleID = 1;
                    break;
            }
            return FemaleID;
        }

        #region Statistics

        static IEnumerable<IEnumerable<T>>
GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
        private void GenerateMateString()
        {
            PossibleMateStrings = new List<string>();
              var list = new List<string> { "1", "3", "5", "7", "9", "B", "D", "F" };
            var result = GetPermutations(list, 8);
            foreach (var perm in result)
            {
                string str = "";
                foreach (var c in perm)
                    str += c;
                PossibleMateStrings.Add(str);
            }
            SetNewMateString();
                 }
        public void SetNewMateString()
        {
            ushort StringNumber = GetRandom(Convert.ToUInt16(PossibleMateStrings.Count()));
            MateString = PossibleMateStrings[StringNumber];

        }
        public ushort GetRandom(ushort max)
        {
            uint rand = CryptographicBuffer.GenerateRandomNumber();
            uint high = uint.MaxValue;
            double randD = Convert.ToDouble(rand);
            double HighD = Convert.ToDouble(high);
            double fraction = randD / HighD;
            double result = max * fraction;
            ushort retval = Convert.ToUInt16(result);
            return retval;
        }

        #endregion

        #region Buffer and lights
        public async Task GenerateBuffer()
        {
            for (int i = 0; i < Swarm.ProcessTime; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    ushort CurrentLevel = FireFlies[j].GetCurrentLevel(i + 1);
                    byte[] buf = getPWMBuffer(j,CurrentLevel , Convert.ToUInt16(j * 256));
                    for (int k = 0; k < 5; k++)
                        buffer[j, i, k] = buf[k];
                }
            }
        }
        public byte[] getPWMBuffer(int num, ushort on, ushort off)
        {
            byte[] writeBuffer;
            writeBuffer = new byte[] { (byte)(0x06 + 4 * num), (byte)on, (byte)(on >> 8), (byte)off, (byte)(off >> 8) };
            return writeBuffer;
        }
        public async void SetAllLights(int step)
        {
                byte[,] buf = new byte[16, 5];
                for (int j = 0; j < 16; j++)
                    for (int k = 0; k < 5; k++)
                        buf[j, k] = buffer[j,step, k];
                ProcessRecord(buf);
                //   Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                // await Task.Delay(5);
          //  }
        }
        private void ProcessRecord(byte[,] buf)
        {
            string byteString = "";
            for (int i = 0; i < 16; i++)
            {
                //send the byte array to the board, controlling the lighs
                Write(new byte[] { buf[i, 0], buf[i, 1], buf[i, 2], buf[i, 3], buf[i, 4] });
             //   byteString += ByteArrayToHexViaLookup32(new byte[] { buf[i, 0], buf[i, 1], buf[i, 2], buf[i, 3], buf[i, 4] }) + " ";
            }
         //   Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + byteString);
        }

        #endregion
        public async Task  ResetSwarmGroup()
        {
            //Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " Running Reset");
            //new mate string
            SetNewMateString();
            SetUpMates();

            foreach (FireFly  f in FireFlies)
            {
                f.reset();
            }
         //   Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " Completed Reset");
        }

        #region "Hex data for debug"

        private static readonly uint[] _lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        private static string ByteArrayToHexViaLookup32(byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);


        }

        #endregion
        #region unused


        private async void F_Flash(object sender, EventArgs e)
        {
            FireFly f = (FireFly)sender;
            //    ushort brightness = 0;
            //    Debug.WriteLine(String.Format("{0} On board {1} port {2}: full ON", DateTime.Now.ToString(), Address, f.ID));
            //   SetPWM(f.ID, brightness, 2000); //full off wired to GND, ON wired to +3.3v
            //   await Task.Delay(500);
            //Console.Writeline
            //    Debug.WriteLine(String.Format("{0} On board {1} port {2}: full OFF", DateTime.Now.ToString(), Address, f.ID));
            //    SetPWM(f.ID, 4096, 0); // full on wired to GND, off wired to +3.3v


            //   LEDstatus[f.ID] = f.CurrentBrightness;
        }

        public void BeginFlashing()
        {


        }

        public async void Pulse(int pin, int duration, int percentage)
        {
            //   p.SetPWM(1, 4096, 0); // full on wired to GND, off wired to +3.3v
            // await Task.Delay(1500);
            //p.SetPWM(1, 2000, 0);
            //await Task.Delay(500);
            //p.SetPWM(1, 0, 2000); //full off wired to GND, on wired to +3.3v
            //await Task.Delay(duration);
            //p.SetPWM(1, 4096, 0);
            //await Task.Delay(1500);
            //p.SetPWM(1, 0, 4096);  //full on 
            //await Task.Delay(5000);

        }

        public async void Flutter(int min, int max, int qty, int duration, int gap)
        {
            int pulseLength = (duration / qty) - gap;
            int step = (max - min) / qty;
            int brightness = min;

            for (int i = 0; i < qty; i++)
            {
                // Pulse(pulseLength, brightness);
                brightness += step;
                await Task.Delay(gap);
            }
        }

        public async void ProcessInstruction(FlashInstruction f)
        {
            switch (f.TaperDirection)
            {
                case Taper.TaperType.UP:
                    for (int i = 0; i < f.Duration; i += Swarm.interval)
                    {
                        //  SetPWM()
                        await Task.Delay(Swarm.interval);
                    }
                    break;
                case Taper.TaperType.DOWN:
                    break;
                case Taper.TaperType.NONE:
                    break;
                case Taper.TaperType.FLAT:
                    break;
                default:
                    break;
            }


        }
        public void GetCurrentInstruction(int step)
        {
            for (int i = 0; i < 16; i++)
            {
                LEDstatus[i] = FireFlies[i].GetCurrentLevel(step);
            }
            string DebugString = step + "     ";

            for (int i = 0; i < 16; i++)
            {
                DebugString += LEDstatus[i] + " ";
            }
            Debug.WriteLine(DebugString);
        }
        #endregion
    }
}
