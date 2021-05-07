using AlloouBot.BotInteraction.Internal;
using System;

namespace AlloouBot
{
    class Program
    {
        static void Main(string[] args)
        {
            TwitchChatBot chatBot = new TwitchChatBot();
            chatBot.Connect();

            string quitmsg = "exit";
            string helpmsg = "Help";
            string comparemsg = string.Empty;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(string.Format("Type '{0}' to close the bot", quitmsg));
            Console.WriteLine(string.Format("Type '{0}' for additional commands", helpmsg));
            Console.WriteLine("Alternatively, you may use this command line to chat as the bot.");
            Console.ResetColor();

            while (true)
            {
                comparemsg = Console.ReadLine();
                if (!Convert.ToBoolean(string.Compare(quitmsg, comparemsg, true))) // string.compare returns 0 for exact match
                    break;
                
                if (ConsoleCommands.CallCommand(comparemsg) == false)
                {
                    chatBot.SendMessage(comparemsg);
                }

            }
            chatBot.Disconnect();
        }
    }
}
