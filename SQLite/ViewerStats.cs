using System.ComponentModel.DataAnnotations;
using System.Data.Linq.Mapping;

namespace AlloouBot.SQLite
{
    [Table(Name = "ViewerStats")]
    public class ViewerStats
    {
        [Column(Name = "ID", IsDbGenerated = true, IsPrimaryKey = true, DbType = "INTEGER")]
        [Key]
        public int ID { get; set; }

        [Column(Name = "TwitchID", DbType = "INTEGER")]
        public int? TwitchID { get; set; }

        [Column(Name = "Name", DbType = "TEXT")]
        public string Name { get; set; }

        [Column(Name = "Points", DbType = "INTEGER")]
        public int? Points { get; set; }

        [Column(Name = "FirstFollowed", DbType = "TEXT")]
        public string FirstFollowed { get; set; }

        [Column(Name = "FirstSeen", DbType = "TEXT")]
        public string FirstSeen { get; set; }
    }
}