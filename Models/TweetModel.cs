namespace twitter.Models
{
    public class TweetModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Tweet { get; set; }
        public DateTime CreatedDate { get; set; } 
        public DateTime UpdatedDate { get; set; }
        public IFormFile? Img { get; set; }
        public string? ImgPath { get; set; }
        public string TweetImg { get; set; }
    }
}
