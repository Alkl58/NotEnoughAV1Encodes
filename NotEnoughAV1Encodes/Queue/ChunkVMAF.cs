using System.Collections.Generic;
using System.Windows.Documents;

namespace NotEnoughAV1Encodes.Queue
{
    public class ChunkVMAF
    {
        public string ChunkName { get; set; }
        public string CalculatedQuantizer { get; set; }
        public List<double> QValues { get; set; }
        public List<double> VMAFValues { get; set; }
    }
}
