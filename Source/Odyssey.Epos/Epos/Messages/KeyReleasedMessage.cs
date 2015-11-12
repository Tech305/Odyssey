﻿using Odyssey.Interaction;

namespace Odyssey.Epos.Messages
{
    public class KeyReleasedMessage : Message
    {
        public Keys Key { get; set; }

        public KeyReleasedMessage(Keys key) : base(false)
        {
            Key = key;
        }
    }
}