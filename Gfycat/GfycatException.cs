using System;

namespace Gfycat
{
    public class GfycatException : Exception
    {
        public GfycatException()
        {
        }

        public GfycatException(string message) : base(message)
        {
        }

        public GfycatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}