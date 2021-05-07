using AlloouBot.SQLite;
using AlloouBot.Variables;
 
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlloouBot.BotInteraction
{
    public class ChatCommands
    {
        public TwitchLib.Api.TwitchAPI twitchAPI = null;
        private FunThings _funThings = new FunThings();
        private char _commandIdentifier = '!';
        int costOfClip = 10;
        List<UserCoolDown> userCoolDowns = new List<UserCoolDown>();
        int userCoolDownSeconds = 2;

        public ChatCommands(char commandIdentifier = '!', TwitchLib.Api.TwitchAPI twitchAPI = null)
        {
            _commandIdentifier = commandIdentifier;
            this.twitchAPI = twitchAPI;
        }

        public void SetCommandIdentifier(char commandIdentifier)
        {
            _commandIdentifier = commandIdentifier;
        }

        //Commands are to be formatted as !command target action
        public string DoChatCommand(TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            UserCoolDown userCoolDown = userCoolDowns.Find(v => v.username == e.ChatMessage.Username);
            if (userCoolDown != null)
            {
                if ((DateTime.Now - userCoolDown.coolDownBegan).TotalSeconds > userCoolDownSeconds)
                    UpdateCooldowns(e.ChatMessage.Username, e.ChatMessage.Message, e.ChatMessage.UserId);
                else return null;
            }
            else
            {
                userCoolDown = new UserCoolDown();
                userCoolDown.username = e.ChatMessage.Username;
                userCoolDown.twichID = e.ChatMessage.UserId;
                userCoolDown.coolDownBegan = DateTime.Now;
                userCoolDowns.Add(userCoolDown);
            }

            string[] splitmessage = e.ChatMessage.Message.Split(' ');
            if(splitmessage.Count() > 1)
            {
                if (splitmessage[1].Contains('@') == true)
                splitmessage[1] = splitmessage[1].Trim('@');
            }    

            if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster)
            {
                if (IsHelpModCommand(e.ChatMessage.Message))
                {
                    return HelpModCommand();
                }
                if (IsGivePoints(splitmessage[0]))
                {
                    if (splitmessage.Count() < 3)
                        return "Command format is: command name amount";
                    if (IsDigitsOnly(splitmessage[2]) == false)
                        return "The points were not formatted correctly";

                    int points = Convert.ToInt32(splitmessage[2]);
                    if (GivePoints(TwitchHelpers.GetUserId(splitmessage[1]), points))
                        return "Points get!";
                    else
                        return "The name provided doesn't exist in my records...";
                }
                if (IsPraise(splitmessage[0]))
                {
                    return TwitchHelpers.PraiseMessage(splitmessage[1]);
                }
                else if (IsRecentFollowers(e.ChatMessage.Message))
                {
                    return GetRecentFollowers();
                }
                else if (IsSpecialThanks(e.ChatMessage.Message))
                {
                    return SpecialThanks();
                }
                if (IsTakePoints(splitmessage[0]))
                {
                    if (splitmessage.Count() < 3)
                        return "Command format is: command name amount";
                    if (IsDigitsOnly(splitmessage[2]) == false)
                        return "The points were not formatted correctly";

                    int points = Convert.ToInt32(splitmessage[2]);
                    if (TakePoints(TwitchHelpers.GetUserId(splitmessage[1]), points))
                        return "Points un-get!";
                    else
                        return "The name provided doesn't exist in my records...";
                }
            }

            // all commands not exclusive to channel moderators
            if (IsHelpCommand(e.ChatMessage.Message))
            {
                return HelpCommand();
            }
            else if (IsGetRandomClipCommand(splitmessage[0]))
            {
                Random random = new Random((int)DateTime.Now.Ticks);
                int max = 20;
                string sUser = string.Empty;

                if (e.ChatMessage.Message.Contains(" "))
                    sUser = splitmessage[1];

                TwitchLib.Api.Helix.Models.Users.GetUsers.User user = TwitchHelpers.GetUser(sUser);

                if (user != null)
                {
                    var clips = twitchAPI.Helix.Clips.GetClipsAsync(null, null, user.Id, null, null, null, null, max).Result.Clips;
                    if (clips.Count() > 0)
                    {
                        int userId = Convert.ToInt32(e.ChatMessage.UserId);
                        DatabaseContext context = new DatabaseContext();
                        var requestor = context.ViewerStats.Where(v => v.TwitchID == userId).FirstOrDefault();
                        requestor.Points -= costOfClip;
                        context.SaveChanges();

                        return string.Format("{0} points deducted. {1}", costOfClip, clips[random.Next(clips.Count() - 1)].Url); //clips index starts at 0
                    }
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
            else if (IsPointsCommand(e.ChatMessage.Message))
            {
               return string.Format("Points: {0}", GetPoints(e.ChatMessage.Username));
            }
            else
                return "Numerous conditions and not one match!";
        }

        #region Non-Mod Commands
        private const string help = "Help";
        private const string helpMods = "HelpMods"; // channel moderators
        private const string getRandomClip = "GetRandomClip";
        private const string funThingsCard = "IsThisYourCard";
        private const string funThingsDice = "RollDice";
        private const string points = "Points";
        #endregion

        #region Mod Commands
        private const string givePoints = "GivePoints";
        private const string praise = "Praise";
        private const string recentFollowers = "RecentFollowers";
        private const string specialThanks = "SpecialThanks";
        private const string takePoints = "TakePoints";
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

        public bool IsPointsCommand(string command)
        {
            if (command.Equals(_commandIdentifier + points, StringComparison.InvariantCultureIgnoreCase))
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

        public bool IsGivePoints(string command)
        {
            if (command.Equals(_commandIdentifier + givePoints, StringComparison.InvariantCultureIgnoreCase))
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

        public bool IsTakePoints(string command)
        {
            if (command.Equals(_commandIdentifier + takePoints, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }
        #endregion

        #region Other Functions
        public bool IsDigitsOnly(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

        private bool GivePoints(string userId, int points)
        {

            int? Id = Convert.ToInt32(userId);
            if (Id == null)
                return false;

            DatabaseContext context = new DatabaseContext();
            var viewer = context.ViewerStats.Where(v => v.TwitchID == Id).FirstOrDefault();
            if (viewer == null)
                return false;
            viewer.Points += points;
            context.SaveChanges();
            return true;
        }

        private bool TakePoints(string userId, int points)
        {

            int? Id = Convert.ToInt32(userId);
            if (Id == null)
                return false;

            DatabaseContext context = new DatabaseContext();
            var viewer = context.ViewerStats.Where(v => v.TwitchID == Id).FirstOrDefault();
            if (viewer == null)
                return false;
            viewer.Points -= points;
            context.SaveChanges();
            return true;
        }

        private void UpdateCooldowns(string userName, string command, string TwitchID = null)
        {
            UserCoolDown userCoolDown = new UserCoolDown();
            userCoolDown = userCoolDowns.Find(u => u.username == userName);
            if (userCoolDown != null)
                userCoolDown.coolDownBegan = DateTime.Now;
            else
            {
                userCoolDown.coolDownBegan = DateTime.Now;
                userCoolDown.username = userName;
                userCoolDown.twichID = TwitchID;
            }
        }
        #endregion

        #region String Functions
        public string HelpCommand()
        {
            string commandList = string.Format("Commands are preceded with '{0}' and they are: {1}, {2}, {3}, {4}, {5}, {6}",
                                                _commandIdentifier, help, helpMods, getRandomClip, funThingsCard, funThingsDice, points);
            return commandList;
        }

        public string HelpModCommand()
        {
            string commandList = string.Format("Mod commands are: {0} {1}, {2}, {3}, {4}",
                                                givePoints, praise, recentFollowers, specialThanks, takePoints);
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

        private int? GetPoints(string userName)
        {
            DatabaseContext Context = new DatabaseContext();
            var user = Context.ViewerStats.Where(v => v.Name == userName).FirstOrDefault();

            if (user != null)
                return user.Points;
            else return null;
        }
        #endregion
    }
}
