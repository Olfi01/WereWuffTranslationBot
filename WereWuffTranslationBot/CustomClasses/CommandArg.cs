using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace WereWuffTranslationBot.CustomClasses
{
    public class CommandArg
    {
        public Message Message { get; set; }
        public string Command { get; set; }

        public CommandArg(Message msg, string cmd)
        {
            Message = msg;
            Command = cmd;
        }
    }
}
