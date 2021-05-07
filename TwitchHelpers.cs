using AlloouBot.constants;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.GetChannelEditors;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.V5.Models.Channels;

namespace AlloouBot
{
    public static class TwitchHelpers
    {
        //private static TwitchAPI _twitchAPI = null; 
        //public static void SetTwitchAPI(TwitchAPI twitchAPI)
        //{
        //    if (twitchAPI == null)
        //        _twitchAPI = twitchAPI;
        //    return;
        //}

        private static TwitchAPI _twitchAPI;
        public static TwitchAPI TwitchAPI
        {
            get
            {
                return _twitchAPI;
            }
            set
            {
                if (_twitchAPI == null)
                    _twitchAPI = value;
            }
        }

        public static TimeSpan? GetUpTime()
        {
            string userId = GetUserId(TwitchInfo.ChannelName);

            if (userId == null || string.IsNullOrEmpty(userId))
                return null;

            return TwitchAPI.V5.Streams.GetUptimeAsync(userId).Result;
        }

        public static string GetUserId(string userName)
        {
            List<string> list = new List<string>() { userName };
            try
            {
                var users = TwitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;
                if (users == null || users.Length == 0)
                    return null;

                return users[0].Id;
            }
            catch (System.AggregateException)
            {
                return null;
            }
        }

        public static User GetUser(string userName)
        {
            if (userName == string.Empty)
                return null;

            List<string> list = new List<string>() { userName };
            User[] users = TwitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0];
        }

        public static Channel GetChannel(string userName)
        {
            string userId = GetUserId(userName);

            if (!string.IsNullOrEmpty(userId))
            {
                Channel channel = TwitchAPI.V5.Channels.GetChannelByIDAsync(GetUserId(userName)).Result;
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
        public static string PraiseMessage(string userName)
        {
            Channel channel = GetChannel(userName);

            string sReturn = string.Empty;
            if (channel != null)
            {
                if (channel.Game != null)
                    sReturn = string.Format("Please give {0} a follow. They were playing {1}! \n {2}", channel.Name, channel.Game, channel.Url);
                else
                    sReturn = string.Format("Please give {0} a follow. \n {1}", channel.Name, channel.Url);
            }
            else
                sReturn = "The name given was incorrect. Please try again.";

            return sReturn;
        }

        public static async Task<List<string>> GetEditors()
        {
            //var editorsV5 = await TwitchAPI.V5.Channels.GetChannelEditorsAsync(GetUserId(TwitchInfo.ChannelName), TwitchInfo.BotAccessToken);
            //List<string> resultV5 = new List<string>();

            //int i = 0;
            //while (true)
            //{
            //    if (editorsV5.Editors[i] == null)
            //        break;
            //    resultV5.Add(editorsV5.Editors[i].DisplayName);
            //    i++;
            //}
            //return resultV5;

            GetChannelEditorsResponse editors = await TwitchAPI.Helix.Channels.GetChannelEditorsAsync(GetUserId(TwitchInfo.ChannelName), TwitchInfo.ChannelAccessToken);

            int i = 1;
            List<string> result = new List<string>();
            while (true)
            {
                try
                {
                    result.Add(editors.Data[i].UserName);
                }
                catch (System.IndexOutOfRangeException)
                {
                    break;
                }
                i++;
            }

            return result;
        }

        /// <summary>
        /// Async task to return a list of user names of moderators in the channel.
        /// </summary>
        /// <returns></returns>
        public static async Task<List<string>> GetMods()
        {
            GetModeratorsResponse mods = await TwitchAPI.Helix.Moderation.GetModeratorsAsync(GetUserId(TwitchInfo.ChannelName), null, null, TwitchInfo.ChannelAccessToken);

            int i = 1;
            List<string> result = new List<string>();
            while (true)
            {
                try
                {
                    result.Add(mods.Data[i].UserName);
                }
                catch (System.IndexOutOfRangeException)
                {
                    break;
                }
                i++;
            }

            return result;
        }

        public static async Task<List<ChannelFollow>> GetFollowers()
        {
            var followers = await TwitchAPI.V5.Channels.GetAllFollowersAsync(GetUserId(TwitchInfo.ChannelName));
            
            return followers;
        }
    }
}
