using System;

namespace Foundation
{
    public class NSErrorException : Exception
    {
        public string Domain { get; set; }

        public string Code { get; set; }
    }
}