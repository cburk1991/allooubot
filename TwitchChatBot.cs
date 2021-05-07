using AlloouBot.BotInteraction;
using AlloouBot.constants;
using AlloouBot.SQLite;
using AlloouBot.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;

namespace AlloouBot
{
    internal class TwitchChatBot
    {
        public readonly TwitchClient client = new TwitchClient();
        readonly JoinedChannel JoinedChannel = new JoinedChannel(TwitchInfo.ChannelName);
        readonly ConnectionCredentials twitchCredentials = new ConnectionCredentials(TwitchInfo.BotName, TwitchInfo.BotAccessToken);
        readonly TwitchPubSub pubSub = new TwitchPubSub();
        internal TwitchAPI twitchAPI = new TwitchAPI();
        private ChatCommands _chatCommands = new ChatCommands();
        private char _chatCommandIdentifier = '!';
        public List<string> moderators;
        public List<string> editors;

        DatabaseContext Context = new DatabaseContext();
        public DateTime LastCommand = new DateTime();
        public int coolDownSecond = 0; //set higher if needed

        internal void Connect()
        {
            // Secret is used to verify validity of incoming data
            Console.WriteLine("Connecting...");
            client.Initialize(twitchCredentials, JoinedChannel.Channel, _chatCommandIdentifier, _chatCommandIdentifier);

            twitchAPI.Settings.ClientId = TwitchInfo.ClientId;
            twitchAPI.Settings.AccessToken = TwitchInfo.ChannelAccessToken;


            _chatCommands.SetCommandIdentifier(_chatCommandIdentifier);
            _chatCommands.twitchAPI = twitchAPI;
            TwitchHelpers.TwitchAPI = twitchAPI;

            client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnMessageThrottled += Client_OnMessageThrottled;

            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnReSubscriber += Client_OnReSubscriber;
            client.OnBeingHosted += Client_OnBeingHosted;

            client.OnUserJoined += Client_OnUserJoined;
            client.OnUserLeft += Client_OnUserLeft;

            client.OnUserTimedout += Client_OnUserTimedout;
            client.OnUserBanned += Client_OnUserBanned;

            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;

            pubSub.OnPubSubServiceConnected += PubSub_OnPubSubServiceConnected;
            // Err_badauth
            //pubSub.ListenToFollows(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));
            //pubSub.ListenToBitsEvents(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));
            pubSub.OnListenResponse += PubSub_OnListenResponse;

            pubSub.OnFollow += PubSub_OnFollow;
            pubSub.OnBitsReceived += PubSub_OnBitsReceived;
            pubSub.OnStreamDown += PubSub_OnStreamDown;
            pubSub.OnStreamUp += PubSub_OnStreamUp;
            try
            {
                pubSub.Connect();
                client.Connect();

                moderators = TwitchHelpers.GetMods().Result;
                editors = TwitchHelpers.GetEditors().Result;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();
                throw;
            }
            LastCommand = DateTime.Now;
        }

        internal void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
            client.SendMessage(JoinedChannel, "Goodnight. ResidentSleeper");

            client.OnConnectionError -= Client_OnConnectionError; // will complain of a fatal network error if not disconnected. Is this something to fix?
            client.LeaveChannel(JoinedChannel.Channel);

            pubSub.Disconnect();
            client.Disconnect();
            Console.WriteLine("Saving changes... Please do not interrupt...");

            int result = Context.SaveChanges();
            if (result == 0)
            {
                Console.WriteLine("Save success!");
            }
            else
            {
                Console.WriteLine("Saving failed. Code returned: {result}");
                Console.ReadKey();
            }
        }

        internal void SendMessage(string message)
        {
            client.SendMessage(JoinedChannel, message);
        }

        #region PubSub Events
        private void PubSub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            pubSub.SendTopics();
        }

        private void PubSub_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (e.Successful)
                Console.WriteLine($"Successfully verified listening to topic: {e.Topic}");
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to listen! {e.Topic} Error: {e.Response.Error}");
                Console.ResetColor();
            }
        }

        private void PubSub_OnFollow(object sender, TwitchLib.PubSub.Events.OnFollowArgs e)
        {
            Measurables.newFollowers.Add(e.Username);

            int Id = Convert.ToInt32(e.UserId);
            var newFollower = Context.ViewerStats.Where(v => v.TwitchID == Id).FirstOrDefault();
            if (newFollower == null)
            {
                NewViewerConfig newViewerConfig = new NewViewerConfig();
                Context.ViewerStats.Add(newViewerConfig.initViewer(e.Username, Id.ToString()));
                Context.SaveChanges();
            }
            else
                newFollower.FirstFollowed = DateTime.Now.ToString();
            Context.SaveChanges();
            
            client.SendMessage(JoinedChannel.Channel, string.Format("Thanks for the follow, {0}!", e.Username));
        }

        private void PubSub_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            string message = string.Empty;
            if (e.BitsUsed == 4)
                message = "Thanks for the byte!";
            if (e.BitsUsed == 8)
                message = string.Format("Word, {0} FrankerFaceZ", e.ChannelName);
            if (e.BitsUsed == 16)
                message = "Double Word!";
            else
                message = string.Format("Thanks for the bits! {0}", e.ChannelName);

            client.SendMessage(JoinedChannel.Channel, message);
        }

        private void PubSub_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("The stream is up and running!");
            Console.ResetColor();
        }

        private void PubSub_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("The stream went down!");
            Console.ResetColor();
        }
        #endregion

        #region Client Events
        private void Client_OnLog(object sender, OnLogArgs e)
        {
            if (e.Data.StartsWith("Unaccounted for:"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e.Data);
                Console.ResetColor();
            }
            else if(e.Data.StartsWith("["))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e.Data);
                Console.ResetColor();
            }
            else
                Console.WriteLine(string.Format("Log: {0}", e.Data));
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(string.Format("Error: [{0}] {1}", e.BotUsername, e.Error.Message));
            Console.ResetColor();
        }

        private void Client_OnMessageThrottled(object sender, TwitchLib.Communication.Events.OnMessageThrottledEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Messages have been throttled by the Twitch overlords");
            Console.ResetColor();
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.StartsWith("Hi", StringComparison.InvariantCultureIgnoreCase))
            {
                client.SendMessage(JoinedChannel, "FrankerZ " + e.ChatMessage.DisplayName);
            }

            if (e.ChatMessage.Message.StartsWith(_chatCommandIdentifier.ToString())) // if it is a command
            {
                if ((DateTime.Now - LastCommand).TotalSeconds < coolDownSecond)
                    return;
                else
                    LastCommand = DateTime.Now;

                client.SendMessage(JoinedChannel, _chatCommands.DoChatCommand(e));

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

        private void Client_OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            if (!e.BeingHostedNotification.IsAutoHosted)
                client.SendMessage(JoinedChannel, string.Format("{0} has deemed this channel worthy of a host!", e.BeingHostedNotification.HostedByChannel));
        }

        private void Client_OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            if (!Measurables.allViewers.Contains(e.Username))
            {
                Measurables.allViewers.Add(e.Username);
            }
            Measurables.currentViewers.Add(e.Username);
            Measurables.viewerCount++;
            int Id = Convert.ToInt32(TwitchHelpers.GetUserId(e.Username));
            var newViewer = Context.ViewerStats.Where(v => v.TwitchID == Id).FirstOrDefault();
            if(newViewer == null)
            {
                NewViewerConfig newViewerConfig = new NewViewerConfig();
                Context.ViewerStats.Add(newViewerConfig.initViewer(e.Username, Id.ToString()));
                Context.SaveChanges();
            }
        }

        private void Client_OnUserLeft(object sender, OnUserLeftArgs e)
        {
            Measurables.currentViewers.Remove(e.Username);
            Measurables.viewerCount--;
        }

        private void Client_OnUserTimedout(object sender, OnUserTimedoutArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Mod Action: {0} was timed out for {1} because [{2}]", e.UserTimeout.Username, e.UserTimeout.TimeoutDuration, e.UserTimeout.TimeoutReason);
            Console.ResetColor();
        }

        private void Client_OnUserBanned(object sender, OnUserBannedArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Mod Action: {0} was banned because [{1}]", e.UserBan.Username, e.UserBan.BanReason);
            Console.ResetColor();
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connection established!");
            Console.ResetColor();
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Successfully disconnected!");
            Console.ResetColor();
        }
        #endregion
    }
}
