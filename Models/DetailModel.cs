namespace twitter.Models
{
    public class DetailModel
    {
        public TweetModel Tweet { get; set; } = new TweetModel();
        public List<ReplyModel> Reply { get; set; } = new List<ReplyModel>();
    }
}
