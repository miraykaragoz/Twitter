using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using twitter.Models;
using Dapper;

namespace twitter.Controllers
{
    public class HomeController : Controller
    {
        string connectionString = "";

        public bool CheckLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return false;
            }

            return true;
        }

        public IActionResult Index(string? MessageCssClass, string? Message)
        {
            ViewData["username"] = HttpContext.Session.GetString("username");
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");

            var reply = new ReplyModel();
            var tweet = new TweetModel();
            var detail = new DetailModel();

            using var connection = new SqlConnection(connectionString);

            var tweetQuery = connection.Query<TweetModel>("SELECT Tweets.Id, Tweet, Users.Username, Tweets.CreatedDate, Users.ImgPath as ImgPath, Tweets.ImgPath as TweetImg FROM Tweets LEFT JOIN Users on Tweets.UserId = Users.Id ORDER BY Tweets.CreatedDate DESC");

            ViewBag.Message = Message;
            ViewBag.MessageCssClass = MessageCssClass;

            return View(tweetQuery);
        }

        [Route("/detay/{id}")]
        public IActionResult TweetDetail(int? id)
        {
            if (id == null)
            {
                ViewBag.Message = "B�yle bir g�nderi bulunamad�.";
                ViewBag.MessageCssClass = "alert-secondary";
                return View("Message");
            }

            if (!CheckLogin())
            {
                ViewBag.Yorum = false;
            }
            else
            {
                ViewBag.Yorum = true;
            }

            using var connection = new SqlConnection(connectionString);

            var sql = @"
            SELECT Tweets.Id, Tweets.UserId, Tweet, Users.Username, Tweets.CreatedDate, Users.ImgPath, Tweets.ImgPath as TweetImg FROM Tweets LEFT JOIN Users ON Tweets.UserId = Users.Id WHERE Tweets.Id = @id;
            SELECT Users.Username, Users.ImgPath, Replies.Reply, Replies.CreatedDate, Replies.Id FROM Replies LEFT JOIN Users ON Replies.UserId = Users.Id WHERE TweetId = @id ORDER BY Replies.CreatedDate DESC";

            using var multi = connection.QueryMultiple(sql, new { id });

            var tweet = multi.ReadFirstOrDefault<TweetModel>();
            var comments = multi.Read<ReplyModel>().ToList();

            var detailModel = new DetailModel
            {
                Tweet = tweet,
                Reply = comments
            };

            ViewBag.Admin = HttpContext.Session.GetString("Admin");
            ViewBag.Username = HttpContext.Session.GetString("username");
            ViewBag.TweetOwnerId = tweet.UserId;
            ViewBag.CanDeleteTweet = tweet != null && (tweet.UserId == HttpContext.Session.GetInt32("Id") || ViewBag.Admin == "Tam");

            return View(detailModel);
        }
    }
}
