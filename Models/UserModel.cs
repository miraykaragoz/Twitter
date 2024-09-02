namespace twitter.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PasswordConfirm { get; set; }
        public DateTime CreatedDate { get; set; }
        public IFormFile? Img { get; set; }
        public string? ImgPath { get; set; }
        public string? RedirectUrl { get; set; }

    }
}
