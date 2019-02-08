using System;
using System.Composition;

namespace Microsoft.DotNetCore.CodeFormatting
{
    public interface IMessageSender
    {
        void Send(string message);
    }

    [Export(typeof(IMessageSender))]
    public class EmailSender : IMessageSender
    {
        public void Send(string message)
        {
            Console.WriteLine(message);
        }
    }
}
