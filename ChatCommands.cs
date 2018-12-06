using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.ChatCommands
{
    static class ChatCommands
    {
        private static char _commandIdentifier = '!';

        #region Non-Mod Commands
        private const string help = "Help";
        private const string helpMods = "HelpMods"; // channel moderators
        private const string getRandomClip = "GetRandomClip";
        private const string funThingsCard = "IsThisYourCard";
        private const string funThingsDice = "RollDice";
        private const string upTime = "Uptime";
        private const string viewerCount = "ViewerCount";
        #endregion

        #region Mod Commands
        private const string praise = "Praise";
        private const string recentFollowers = "RecentFollowers";
        private const string specialThanks = "SpecialThanks";
        #endregion

        public static void SetCommandIdentifier(char commandIdentifier)
        {
            _commandIdentifier = commandIdentifier;
        }

        public static bool IsHelpCommand(string command)
        {
            bool bReturn = false;

            if (command.Equals(_commandIdentifier + help, StringComparison.InvariantCultureIgnoreCase))
                bReturn = true;

            return bReturn;
        }

        public static string HelpCommand()
        {
            string commandList = string.Format("Commands are preceded with '{0}' and they are: {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                                                _commandIdentifier, help, helpMods, getRandomClip, funThingsCard, funThingsDice, upTime, viewerCount);
            return commandList;
        }

        public static bool IsHelpModCommand(string command)
        {
            bool bReturn = false;

            if (command.Equals(_commandIdentifier + helpMods, StringComparison.InvariantCultureIgnoreCase))
                bReturn = true;

            return bReturn;
        }

        public static string HelpModCommand()
        {
            string commandList = string.Format("Mod commands are: {0} {1}, and {2}",
                                                praise, recentFollowers, specialThanks);
            return commandList;
        }

        public static bool IsFunThingsCardCommand(string command)
        {
            {
                bool bReturn = false;

                if (command.Equals(_commandIdentifier + funThingsCard, StringComparison.InvariantCultureIgnoreCase))
                    bReturn = true;

                return bReturn;
            }
        }

        public static bool IsFunThingsDiceCommand(string command)
        {
            {
                bool bReturn = false;

                if (command.Equals(_commandIdentifier + funThingsDice, StringComparison.InvariantCultureIgnoreCase))
                    bReturn = true;

                return bReturn;
            }
        }

        public static bool IsGetRandomClipCommand(string command)
        {
            {
                bool bReturn = false;

                if (command.Equals(_commandIdentifier + getRandomClip, StringComparison.InvariantCultureIgnoreCase))
                    bReturn = true;

                return bReturn;
            }
        }

        public static bool IsHelpModsCommand(string command)
        {
            bool bReturn = false;

            if (command.Equals(_commandIdentifier + helpMods, StringComparison.InvariantCultureIgnoreCase))
                bReturn = true;

            return bReturn;
        }

        public static bool IsPraise(string command)
        {
            bool bReturn = false;

            if (command.Equals(_commandIdentifier + praise, StringComparison.InvariantCultureIgnoreCase))
                bReturn = true;

            return bReturn;
        }

        public static bool IsRecentFollowers(string command)
        {
            bool bReturn = false;

            if (command.Equals(_commandIdentifier + recentFollowers, StringComparison.InvariantCultureIgnoreCase))
                bReturn = true;

            return bReturn;
        }

        public static bool IsSpecialThanks(string command)
        {
            bool bReturn = false;

            if (command.Equals(_commandIdentifier + specialThanks, StringComparison.InvariantCultureIgnoreCase))
                bReturn = true;

            return bReturn;
        }

        public static bool IsUptime(string command)
        {
            bool bReturn = false;

            if (command.Equals(_commandIdentifier + upTime, StringComparison.InvariantCultureIgnoreCase))
                bReturn = true;

            return bReturn;
        }

        public static bool IsViewers(string command)
        {
            bool bReturn = false;

            if (command.Equals(_commandIdentifier + viewerCount, StringComparison.InvariantCultureIgnoreCase))
                bReturn = true;

            return bReturn;
        }

        public static string SpecialThanks()
        {
            return "A huge thanks to SwiftySpiffy and all the contributors to TwitchLib. Without their open source project, Alloobot wouldn't be what it is now! Additional thanks to Reabs for being dedicated to his own chat bot, ReabsBot!";
        }
    }
}
