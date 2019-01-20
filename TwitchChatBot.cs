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
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.Helix;

namespace AlloouBot
{
    internal class TwitchChatBot
    {
        //implement SQLite

        readonly TwitchClient client = new TwitchClient();
        readonly JoinedChannel JoinedChannel = new JoinedChannel(TwitchInfo.ChannelName);
        readonly ConnectionCredentials twitchCredentials = new ConnectionCredentials(TwitchInfo.BotName, TwitchInfo.BotAccessToken);
        readonly TwitchPubSub pubSub = new TwitchPubSub();
        private TwitchAPI twitchAPI = new TwitchAPI();
        private ChatCommands _chatCommands = new ChatCommands();
        
        private char _chatCommandIdentifier = '!';
        
        private readonly List<string> moderators = new List<string>
        { "...", "!!!", "???" };

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
            twitchAPI.Settings.Secret = "Twitch"; // Need to not hard code this

            _chatCommands.SetCommandIdentifier(_chatCommandIdentifier);
            _chatCommands.twitchAPI = twitchAPI;
            TwitchHelpers.TwitchAPI = twitchAPI;

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

            client.OnConnectionError -= Client_OnConnectionError; // will complain of a fatal network error if not disconnected. Is this something to fix?
            client.LeaveChannel(JoinedChannel.Channel);

            pubSub.Disconnect();
            client.Disconnect();
        }

        #region PubSub Events
        private void PubSub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            pubSub.ListenToFollows(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));
            pubSub.ListenToBitsEvents(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));
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
            Measurables.newFollowers.Add(e.Username);
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
                if (moderators.Contains(e.ChatMessage.Username, StringComparer.InvariantCultureIgnoreCase))
                    client.SendMessage(JoinedChannel, _chatCommands.DoChatCommand(e, true));
                else
                    client.SendMessage(JoinedChannel, _chatCommands.DoChatCommand(e, false));

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
            if(!Measurables.allViewers.Contains(e.Username))
            {
                Measurables.allViewers.Add(e.Username);
            }
            Measurables.currentViewers.Add(e.Username);
            Measurables.viewerCount++;
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
    }
}
