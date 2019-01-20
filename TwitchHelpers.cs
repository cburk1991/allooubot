using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users;
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
            User[] users = TwitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0].Id;
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
                    sReturn = string.Format("Please give {0} a follow. They were playing {1}! \n {1}", channel.Name, channel.Game, channel.Url);
                else
                    sReturn = string.Format("Please give {0} a follow. \n {1}", channel.Name, channel.Url);
            }
            else
                sReturn = "The name given was incorrect. Please try again.";

            return sReturn;
        }
    }
}
