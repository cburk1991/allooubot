using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlloouBot
{
    public class ChatCommands
    {
        public TwitchLib.Api.TwitchAPI twitchAPI = null;
        private FunThings _funThings = new FunThings();
        private char _commandIdentifier = '!';
        // implement a global command timer and a command specific timer to prevent spam.

        public ChatCommands(char commandIdentifier = '!', TwitchLib.Api.TwitchAPI twitchAPI = null)
        {
            _commandIdentifier = commandIdentifier;
            this.twitchAPI = twitchAPI;
        }

        public void SetCommandIdentifier(char commandIdentifier)
        {
            _commandIdentifier = commandIdentifier;
        }

        public string DoChatCommand(TwitchLib.Client.Events.OnMessageReceivedArgs e, bool isMod = false)
        {
            if (isMod)
            {
                if (IsHelpModCommand(e.ChatMessage.Message))
                {
                    return HelpModCommand();
                }
                if (IsPraise(e.ChatMessage.Message.Split(' ')[0]))
                {
                    return TwitchHelpers.PraiseMessage(e.ChatMessage.Message.Split(' ')[1]);
                }
                else if (IsRecentFollowers(e.ChatMessage.Message))
                {
                    return GetRecentFollowers();
                }
                else if (IsSpecialThanks(e.ChatMessage.Message))
                {
                    return SpecialThanks();
                }
            }
            // all commands not exclusive to channel moderators
            if (IsHelpCommand(e.ChatMessage.Message))
            {
                return HelpCommand();
            }
            else if (IsUptime(e.ChatMessage.Message))
            {
                TimeSpan? time = TwitchHelpers.GetUpTime();
                if (time != null)
                    return string.Format("The channel has been live for {0} hour(s) and {1} minute(s).", time.Value.TotalHours, time.Value.TotalMinutes);
                else
                    return "There was no uptime to find!";
            }
            else if (IsViewers(e.ChatMessage.Message))
            {
                return string.Format("{0} viewer(s)", Measurables.viewerCount.ToString());
            }
            else if (IsGetRandomClipCommand(e.ChatMessage.Message.Split(' ')[0]))
            {
                Random random = new Random((int)DateTime.Now.Ticks);
                int max = 20;
                string sUser = string.Empty;

                if (e.ChatMessage.Message.Contains(" "))
                    sUser = e.ChatMessage.Message.Split(' ')[1];

                TwitchLib.Api.Helix.Models.Users.User user = TwitchHelpers.GetUser(sUser);

                if (user != null)
                {
                    var clips = twitchAPI.Helix.Clips.GetClipAsync(null, null, user.Id, null, null, max).Result.Clips;
                    if (clips.Count() > 0)
                        return clips[random.Next(max + 1)].Url;
                    else
                        return "That user has no clips of their streams!";
                }
                else
                    return "A valid Twitch name must be provided...";
            }
            else if (IsFunThingsCardCommand(e.ChatMessage.Message))
            {
                return _funThings.IsThisYourCard();
            }
            else if (IsFunThingsDiceCommand(e.ChatMessage.Message))
            {
                int[] rolls;
                rolls = _funThings.RollDice();
                return string.Format("The dice were cast. As they come to a stop, two numbers appear: {0}, {1}", rolls[0].ToString(), rolls[1].ToString());

            }
            else
                return "I can't work under these conditions!";
        }

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

        #region Boolean Functions
        public bool IsHelpCommand(string command)
        {
            if (command.Equals(_commandIdentifier + help, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsHelpModCommand(string command)
        {
            if (command.Equals(_commandIdentifier + helpMods, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsFunThingsCardCommand(string command)
        {
            if (command.Equals(_commandIdentifier + funThingsCard, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsFunThingsDiceCommand(string command)
        {
            if (command.Equals(_commandIdentifier + funThingsDice, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsGetRandomClipCommand(string command)
        {
            if (command.Equals(_commandIdentifier + getRandomClip, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsHelpModsCommand(string command)
        {
            if (command.Equals(_commandIdentifier + helpMods, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsPraise(string command)
        {
            if (command.Equals(_commandIdentifier + praise, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsRecentFollowers(string command)
        {
            if (command.Equals(_commandIdentifier + recentFollowers, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsSpecialThanks(string command)
        {
            if (command.Equals(_commandIdentifier + specialThanks, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsUptime(string command)
        {
            if (command.Equals(_commandIdentifier + upTime, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public bool IsViewers(string command)
        {
            if (command.Equals(_commandIdentifier + viewerCount, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }
        #endregion

        #region String Functions
        public string HelpCommand()
        {
            string commandList = string.Format("Commands are preceded with '{0}' and they are: {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                                                _commandIdentifier, help, helpMods, getRandomClip, funThingsCard, funThingsDice, upTime, viewerCount);
            return commandList;
        }

        public string HelpModCommand()
        {
            string commandList = string.Format("Mod commands are: {0} {1}, and {2}",
                                                praise, recentFollowers, specialThanks);
            return commandList;
        }

        public string SpecialThanks()
        {
            return "A huge thanks to SwiftySpiffy and all the contributors to TwitchLib. Without their open source project, Alloobot wouldn't be what it is now! Additional thanks to Reabs for being dedicated to his own chat bot, ReabsBot!";
        }
        #endregion

        #region Get Functions
        /// <summary>
        /// used to form a comma delimited string of the followers gained this session.
        /// </summary>
        /// <returns></returns>
        private string GetRecentFollowers()
        {
            string sReturn = string.Empty;

            foreach (string s in Measurables.newFollowers)
            {
                sReturn += s + ", ";
            }
            if (!string.IsNullOrEmpty(sReturn))
                sReturn.TrimEnd(new Char[] { ' ', ',' });

            return sReturn;
        }

        /// <summary>
        /// used to form a comma delimited string of the viewers currently in the channel
        /// </summary>
        /// <returns></returns>
        private string GetCurrentViewers()
        {
            string sReturn = string.Empty;

            foreach (string s in Measurables.currentViewers)
            {
                sReturn += s + ", ";
            }
            if (!string.IsNullOrEmpty(sReturn))
                sReturn.TrimEnd(new Char[] { ' ', ',' });

            return sReturn;
        }

        /// <summary>
        /// used to form a comma delimited string of all viewers who came in during this session
        /// </summary>
        /// <returns></returns>
        private string GetAllViewers()
        {
            string sReturn = string.Empty;

            foreach (string s in Measurables.allViewers)
            {
                sReturn += s + ", ";
            }
            if (!string.IsNullOrEmpty(sReturn))
                sReturn.TrimEnd(new Char[] { ' ', ',' });

            return sReturn;
        }
        #endregion

    }
}
