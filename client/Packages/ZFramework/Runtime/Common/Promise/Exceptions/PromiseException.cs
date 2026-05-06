using System;

namespace ZLib.Promises
{
    /// <summary>
    /// Base class for promise exceptions.
    /// </summary>
    public class PromiseException : Exception
    {
        public int RejectValue;
        public object RejectData;

        public PromiseException() { }

        public PromiseException(string message) : base(message) { }

        public PromiseException(string message,int code) : base(message) { RejectValue = code; }

        public PromiseException(string message, int code,object data) : base(message) { RejectValue = code; RejectData = data; }

        public PromiseException(string message, Exception inner) : base(message, inner) { }
    }
}
