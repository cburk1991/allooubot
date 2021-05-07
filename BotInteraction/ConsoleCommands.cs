using AlloouBot.Variables;
using AlloouBot.constants;
using System;
using System.Collections.Generic;
using AlloouBot.SQLite;
using System.Linq;

namespace AlloouBot.BotInteraction.Internal
{
    public static class ConsoleCommands
    {
        static List<string> commands = new List<string>()
        {
            "Help",
            "GetMyID",
            "NewFollows",
            "RefreshDBFollowers"
        };

        public static void Help()
        {

            Console.WriteLine("Commands are case and space sensitive.");
            Console.WriteLine("Help commands are: ");
            for (int i = 0; i < commands.Count; i++)
                Console.WriteLine(commands[i]);
        }

        public static bool CallCommand(string commandToRun)
        {
            var type = typeof(ConsoleCommands);

            if (type != null)
            {
                var command = type.GetMethod(commandToRun);

                if (command != null)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    command.Invoke(null, null);
                    Console.ResetColor();
                    return true;
                }
            }
            return false;
        }
        public static void GetMyID()
        {
            Console.WriteLine(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));
        }
        public static void NewFollows()
        {
            string message = string.Format("New Follows are: ");
            for (int i = 0; i < Measurables.newFollowers.Count; i++)
            {
                if (i == 0)
                    message += Measurables.newFollowers;
                else
                    message += string.Format(", {0}", Measurables.newFollowers);
            }
            Console.WriteLine(message);
        }

        public static void RefreshDBFollowers()
        {
            DatabaseContext context = new DatabaseContext();
            var follows = TwitchHelpers.GetFollowers();
            int id;
            string key;
            ViewerStats viewer = new ViewerStats();
            NewViewerConfig newViewer = new NewViewerConfig();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("This is experimental. Use at your own risk. Continue? Y/N");
            while(true)
            {
                key = Console.ReadLine();
                if (key.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                    break;
                if(key.Equals("N", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Execution cancelled.");
                    Console.ResetColor();
                    return;
                }
                else
                    Console.WriteLine("This is experimental. Use at your own risk. Continue? Y/N");

            }


            for (int i = 0; i < follows.Result.Count - 1; i++)
            {
                id = Convert.ToInt32(follows.Result[i].User.Id);
                viewer = new ViewerStats();
                viewer = context.ViewerStats.Where(x => x.TwitchID == id).FirstOrDefault();
                if(viewer == null)
                {
                    newViewer = new NewViewerConfig();
                    viewer = newViewer.initViewer(follows.Result[i].User.Name, follows.Result[i].User.Id, follows.Result[i].CreatedAt.ToString(),
                        follows.Result[i].CreatedAt.ToString());

                    context.ViewerStats.Add(viewer);
                }
                else
                {
                    if (viewer.FirstSeen == null)
                        viewer.FirstSeen = follows.Result[i].CreatedAt.ToString();

                    viewer.FirstFollowed = follows.Result[i].CreatedAt.ToString();
                }
                Console.WriteLine(string.Format("{0} followed at: {1}", follows.Result[i].User.Name, follows.Result[i].CreatedAt));
            }
            Console.WriteLine("Saving... This may take a while.");
            context.SaveChanges();
            Console.WriteLine("Complete!");
            Console.ResetColor();
        }
    }
}
