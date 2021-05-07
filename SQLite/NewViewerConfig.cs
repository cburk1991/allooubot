using System;
using AlloouBot;
namespace AlloouBot.SQLite
{
    class NewViewerConfig
    {
        public ViewerStats initViewer(string userName, string twitchID = null, string firstSight = null, string followDate = null)
        {
            int Id;
            if (twitchID == null)
                Id = Convert.ToInt32(TwitchHelpers.GetUserId(userName));
            else
                Id = Convert.ToInt32(twitchID);

            if(firstSight == null)
                firstSight = DateTime.Now.ToString();

            if (followDate == null)
                followDate = DateTime.Now.ToString();

            ViewerStats viewer = new ViewerStats()
            {
                TwitchID = Id,
                Name = userName,
                FirstSeen = firstSight,
                FirstFollowed = followDate,
                Points = 10
            };
            return viewer;
        }
    }
}
