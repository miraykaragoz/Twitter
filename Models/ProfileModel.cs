namespace twitter.Models
{
    public class ProfileModel
    {
        public UserModel? User { get; set; }
        public List<TweetModel> Tweet { get; set; }
    }
}
