using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using TwitchLib.Client;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.PubSub;
using TwitchLib.Api.V5.Models.Channels;
using ConsoleApp1.ChatCommands;
using ConsoleApp1.FunThings;

using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.Helix;

namespace AlloouBot
{
    internal class TwitchChatBot
    {
        readonly TwitchClient client = new TwitchClient();
        readonly JoinedChannel JoinedChannel = new JoinedChannel(TwitchInfo.ChannelName);
        readonly ConnectionCredentials twitchCredentials = new ConnectionCredentials(TwitchInfo.BotName, TwitchInfo.BotAccessToken);
        readonly TwitchPubSub pubSub = new TwitchPubSub();
        private TwitchAPI twitchAPI = new TwitchAPI();
        
        private ushort _viewerCount = 0; // 16-bit int
        private List<string> _currentViewers = new List<string>();
        private List<string> _allViewers = new List<string>();
        private List<string> _newFollowers = new List<string>();
        private char _chatCommandIdentifier = '!';
        
        private readonly List<string> moderators = new List<string>
        { "...", "???", "!!!" };

        #region Bits and Currency Costs
        private float _bitsToUSDConverter = 1.4f; // UPDATED: November 2018. current exchange rate for 1 dollar to 100 bits
        private float _costCandyBar = 2;
        private float _costCoffee = 5;
        private float _costOldGame = 15;
        private float _costGas = 30;
        private float _costGame = 60;
        #endregion

        internal void Connect()
        {
            Console.WriteLine("Connecting...");
            client.Initialize(twitchCredentials, JoinedChannel.Channel, _chatCommandIdentifier, _chatCommandIdentifier);

            twitchAPI.Settings.ClientId = TwitchInfo.ClientId;
            twitchAPI.Settings.AccessToken = TwitchInfo.BotAccessToken;
            twitchAPI.Settings.Secret = "TwitchyTriggerFinger";
            //twitchAPI.Helix.Webhooks.UserReceivesFollowerAsync("What do I type here???", TwitchLib.Api.Core.Enums.WebhookCallMode.Subscribe, TwitchInfo.ClientId); // I'm not doing something right
            ChatCommands.SetCommandIdentifier(_chatCommandIdentifier);

            client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnMessageThrottled += Client_OnMessageThrottled;

            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnReSubscriber += Client_OnReSubscriber;
            //client.OnBeingHosted += Client_OnBeingHosted;

            client.OnUserJoined += Client_OnUserJoined;
            client.OnUserLeft += Client_OnUserLeft;

            //client.OnUserTimedout += Client_OnUserTimedout;
            //client.OnUserBanned += Client_OnUserBanned;

            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;

            client.Connect();

            pubSub.OnPubSubServiceConnected += PubSub_OnPubSubServiceConnected;
            pubSub.OnListenResponse += PubSub_OnListenResponse;

            pubSub.OnFollow += PubSub_OnFollow;
            pubSub.OnBitsReceived += PubSub_OnBitsReceived;
            pubSub.OnStreamDown += PubSub_OnStreamDown;
            pubSub.OnStreamUp += PubSub_OnStreamUp;

            pubSub.Connect();
        }

        internal void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
            client.SendMessage(JoinedChannel, "The plug has been pulled. My time is up. Until next time. ResidentSleeper");

            client.OnConnectionError -= Client_OnConnectionError; // will complain of a fatal network error. Is this something to fix?
            client.LeaveChannel(JoinedChannel.Channel);

            pubSub.Disconnect();
            client.Disconnect();
        }

        #region PubSub Events
        private void PubSub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            pubSub.ListenToFollows(GetUserId(TwitchInfo.ChannelName));
            pubSub.ListenToBitsEvents(GetUserId(TwitchInfo.ChannelName));
        }

        private void PubSub_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (e.Successful)
                Console.WriteLine($"Successfully verified listening to topic: {e.Topic}");
            else
                Console.WriteLine($"Failed to listen! Error: {e.Response.Error}");
        }

        private void PubSub_OnFollow(object sender, TwitchLib.PubSub.Events.OnFollowArgs e)
        {
            _newFollowers.Add(e.Username);
            client.SendMessage(JoinedChannel.Channel, string.Format("Thanks for the follow, {0}!", e.Username));
        }

        private void PubSub_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            string message = string.Empty;
            float value = e.BitsUsed * _bitsToUSDConverter;

            if (e.BitsUsed <= _costCandyBar)
                message = string.Format("Every little bit helps. Thank you for the bits, {0}!", e.Username);
            else if (value > _costCandyBar && value <= _costCoffee)
                message = string.Format("Thank you for the kind bits donation, {0}!", e.Username);
            else if (value > _costCoffee && value <= _costOldGame)
                message = string.Format("An excellent sum, {0}!", e.Username);
            else if (value > _costOldGame && value <= _costGas)
                message = string.Format("Thank you, {0}! You're keeping the dream going.", e.Username);
            else if (value > _costGas && value <= _costGame)
                message = string.Format("Thank you, {0}! I'll be able to drive for a week PogChamp", e.Username);
            else if (value > _costGame)
                message = string.Format("The air glimmers as {0} fills the sky with countless bits. All is beautiful for a minute.", e.Username);

            client.SendMessage(JoinedChannel.Channel, message);
        }

        private void PubSub_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("The stream is up and running!");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void PubSub_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("The stream went down!");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        #endregion

        #region Client Events
        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine(string.Format("Log: {0}", e.Data));
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("Error: [{0}] {1}", e.BotUsername, e.Error.Message));
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void Client_OnMessageThrottled(object sender, TwitchLib.Communication.Events.OnMessageThrottledEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Messages have been throttled by the Twitch overlords");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.StartsWith("Hi", StringComparison.InvariantCultureIgnoreCase))
            {
                client.SendMessage(JoinedChannel.Channel, "FrankerZ " + e.ChatMessage.DisplayName);
            }

            if(e.ChatMessage.Message.StartsWith(_chatCommandIdentifier.ToString())) // if it is a command
            {
                DoChatCommand(e.ChatMessage.Message, e.ChatMessage.Username);
            }
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            client.SendMessage(JoinedChannel, string.Format("{0} has just subscribed! It is very appreciated.", e.Subscriber.DisplayName));
        }

        private void Client_OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            client.SendMessage(JoinedChannel, string.Format("{0} has subscribed for {1} months!", e.ReSubscriber.DisplayName, e.ReSubscriber.Months));
        }

        //private void Client_OnBeingHosted(object sender, OnBeingHostedArgs e)
        //{
        //    if (!e.BeingHostedNotification.IsAutoHosted)
        //        client.SendMessage(JoinedChannel, string.Format("{0} has deemed this channel worthy of a host PogChamp", e.BeingHostedNotification.HostedByChannel));
        //}

        private void Client_OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            if(!_allViewers.Contains(e.Username))
            {
                _allViewers.Add(e.Username);
            }
            _currentViewers.Add(e.Username);
            _viewerCount++;
        }

        private void Client_OnUserLeft(object sender, OnUserLeftArgs e)
        {
            _currentViewers.Remove(e.Username);
            _viewerCount--;
        }

        private void Client_OnUserTimedout(object sender, OnUserTimedoutArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Mod Action: {0} was timed out for {1} because [{2}]", e.UserTimeout.Username, e.UserTimeout.TimeoutDuration, e.UserTimeout.TimeoutReason);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void Client_OnUserBanned(object sender, OnUserBannedArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Mod Action: {0} was banned because [{1}]", e.UserBan.Username, e.UserBan.BanReason);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connection established!");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Successfully disconnected!");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        #endregion

        #region Private Functions
        private TimeSpan? GetUpTime()
        {
            string userId = GetUserId(TwitchInfo.ChannelName);

            if (userId == null || string.IsNullOrEmpty(userId))
                return null;

            return twitchAPI.V5.Streams.GetUptimeAsync(userId).Result;
        }

        private string GetUserId(string userName)
        {
            List<string> list = new List<string>() { userName };
            User[] users = twitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0].Id;
        }

        private User GetUser(string userName)
        {
            if (userName == string.Empty)
                return null;

            List<string> list = new List<string>() { userName };
            User[] users = twitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0];
        }

        /// <summary>
        /// used to form a comma delimited string of the followers gained this session.
        /// </summary>
        /// <returns></returns>
        private string GetRecentFollowers()
        {
            string sReturn = string.Empty;

            foreach(string s in _newFollowers)
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

            foreach (string s in _currentViewers)
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

            foreach (string s in _allViewers)
            {
                sReturn += s + ", ";
            }
            if (!string.IsNullOrEmpty(sReturn))
                sReturn.TrimEnd(new Char[] { ' ', ',' });

            return sReturn;
        }

        private Channel GetChannel(string userName)
        {
            string userId = GetUserId(userName);

            if (!string.IsNullOrEmpty(userId))
            {
                Channel channel = twitchAPI.V5.Channels.GetChannelByIDAsync(GetUserId(userName)).Result;
                if (channel != null)
                    return channel;
            }

            return null;
        }

        /// <summary>
        /// Used to praise a hosting / raiding streamer
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private string PraiseMessage(string userName)
        {
            Channel channel = GetChannel(userName);

            string sReturn = string.Empty;
            if (channel != null)
            {
                if(channel.Game != null)
                    sReturn = string.Format("Please give {0} a follow. They were playing {1}! \n {1}", channel.Name, channel.Game, channel.Url);
                else
                    sReturn = string.Format("Please give {0} a follow. \n {1}", channel.Name, channel.Url);
            }
            else
                sReturn = "The name given was incorrect. Please try again.";

            return sReturn;
        }

        /// <summary>
        /// Performs the chat command requested
        /// </summary>
        /// <param name="command">the command to call</param>
        /// <param name="userId">needed to determine moderator status</param>
        private void DoChatCommand(string command, string userName)
        {

            if(moderators.Contains(userName, StringComparer.InvariantCultureIgnoreCase))
            {
                if(ChatCommands.IsHelpModCommand(command))
                {
                    client.SendMessage(JoinedChannel.Channel, ChatCommands.HelpModCommand());
                }
                if (ChatCommands.IsPraise(command.Split(' ')[0]))
                {
                    client.SendMessage(JoinedChannel.Channel, PraiseMessage(command.Split(' ')[1]));
                }
                else if (ChatCommands.IsRecentFollowers(command))
                {
                    client.SendMessage(JoinedChannel.Channel, GetRecentFollowers());
                }
                else if (ChatCommands.IsSpecialThanks(command))
                {
                    client.SendMessage(JoinedChannel.Channel, ChatCommands.SpecialThanks());
                }
            }
            // all commands not exclusive to channel moderators
            if (ChatCommands.IsHelpCommand(command))
            {
                client.SendMessage(JoinedChannel.Channel, ChatCommands.HelpCommand());
            }
            else if (ChatCommands.IsUptime(command))
            {
                TimeSpan? time = GetUpTime();
                if (time != null)
                    client.SendMessage(JoinedChannel.Channel, string.Format("The channel has been live for {0} hour(s) and {1} minute(s).", time.Value.TotalHours, time.Value.TotalMinutes));
                else
                    client.SendMessage(JoinedChannel.Channel, "There was no uptime to find!");
            }
            else if (ChatCommands.IsViewers(command))
            {
                client.SendMessage(JoinedChannel.Channel, string.Format("{0} viewer(s)", _viewerCount.ToString()));
            }
            else if (ChatCommands.IsGetRandomClipCommand(command.Split(' ')[0]))
            {
                Random random = new Random((int)DateTime.Now.Ticks);
                int max = 20;
                string sUser = string.Empty;

                if (command.Contains(" "))
                    sUser = command.Split(' ')[1];

                User user = GetUser(sUser);

                if (user != null)
                {
                    var clips = twitchAPI.Helix.Clips.GetClipAsync(null, null, user.Id, null, null, max).Result.Clips;
                    if (clips.Count() > 0)
                        client.SendMessage(JoinedChannel.Channel, clips[random.Next(max + 1)].Url);
                    else
                        client.SendMessage(JoinedChannel.Channel, "That user has no clips of their streams!");
                }
                else
                    client.SendMessage(JoinedChannel.Channel, "A valid Twitch name must be provided...");
            }
            else if (ChatCommands.IsFunThingsCardCommand(command))
            {
                client.SendMessage(JoinedChannel.Channel, FunThings.IsThisYourCard());
            }
            else if (ChatCommands.IsFunThingsDiceCommand(command))
            {
                int[] rolls;
                rolls = FunThings.RollDice();
                client.SendMessage(JoinedChannel.Channel, string.Format("The dice were cast. As they come to a stop, two numbers appear: {0}, {1}", rolls[0].ToString(), rolls[1].ToString()));              

            }
            else
                client.SendMessage(JoinedChannel.Channel, "I can't work under these conditions!");
        }
        #endregion
    }
}
