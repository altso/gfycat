using System;

namespace Gfycat
{
    public class GfycatRetryException : GfycatException
    {
        public TimeSpan RetryInterval { get; set; }
    }
}