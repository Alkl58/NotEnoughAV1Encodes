namespace NotEnoughAV1Encodes.Queue
{
    public class ChunkProgress
    {
        public string ChunkName { get; set; }
        public long Progress { get; set; } = 0;
        public long ProgressSecondPass { get; set; } = 0;
    }
}
