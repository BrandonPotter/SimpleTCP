using System;

namespace SimpleTCP
{
    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; set; }

        public MessageEventArgs(Message message)
        {
            this.Message = message;
        }
    }
}
