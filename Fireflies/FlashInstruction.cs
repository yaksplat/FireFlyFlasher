using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Fireflies.Taper;

namespace Fireflies
{
    public class FlashInstruction
    {
        public ushort StartIntensity { get; set; }
        public ushort EndIntensity { get; set; }
        public TaperType TaperDirection { get; set; }
        public ushort Duration { get; set; }
    }
}
