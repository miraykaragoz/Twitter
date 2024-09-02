namespace twitter.Models
{
    public class ReplyModel
    {
        public int Id { get; set; }
        public int TweetId { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string Reply { get; set; }
        public DateTime CreatedDate { get; set; }
        public IFormFile? Img  { get; set; }
        public string? ImgPath { get; set; }
    }
}
