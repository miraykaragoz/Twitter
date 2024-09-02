using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net.Mail;
using twitter.Models;
using System.Net;
using Dapper;

namespace twitter.Controllers
{
    public class AdminController : Controller
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

        public IActionResult Index()
        {
            if (CheckLogin() != true)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult AddTweet()
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Tweet göndermek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewData["username"] = HttpContext.Session.GetString("username");
            return View();
        }

        [HttpPost]
        [Route("/tweetekle")]
        public IActionResult TweetEkle(TweetModel model)
        {
            if(model.Tweet == null)
            {
                ViewBag.ErrorMessage = "Tweet boş olamaz.";
                return View("AddTweet");
            }

            ViewData["username"] = HttpContext.Session.GetString("username");
            model.UserId = (int)HttpContext.Session.GetInt32("Id");

            if (model.Img != null && model.Img.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Img.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                using var fileStream = new FileStream(filePath, FileMode.Create);
                model.Img.CopyTo(fileStream);
                model.ImgPath = fileName;
            }

            using var connection = new SqlConnection(connectionString);

            var sql = "INSERT INTO Tweets (Tweet, CreatedDate, UserId, ImgPath) VALUES (@Tweet, @CreatedDate, @UserId, @ImgPath)";

            var data = new
            {
                model.Tweet,
                CreatedDate = DateTime.Now,
                model.UserId,
                model.ImgPath,
            };

            var rowsAffected = connection.Execute(sql, data);

            ViewBag.SuccessMessage = "Gönderi oluşturuldu.";
            return View("AddTweet", model);
        }

        [HttpPost]
        [Route("/AddReply")]
        public IActionResult AddReply(ReplyModel model)
        {
            if (!CheckLogin())
            {
                ViewBag.mesaj = "Yanıt eklemek için lütfen giriş yapın.";
                return View();
            }

            model.UserId = (int)HttpContext.Session.GetInt32("Id");
            model.CreatedDate = DateTime.Now;

            using var connection = new SqlConnection(connectionString);

            var tweetExists = connection.ExecuteScalar<bool>("SELECT COUNT(1) FROM Tweets WHERE Id = @TweetId", new { model.TweetId });

            if (!tweetExists)
            {
                ModelState.AddModelError("", "Geçersiz TweetId.");
                return View();
            }

            var insertReplySql = "INSERT INTO Replies (Reply, CreatedDate, UserId, TweetId) VALUES (@Reply, @CreatedDate, @UserId, @TweetId)";
            var affectedRows = connection.Execute(insertReplySql, model);

            if (affectedRows > 0)
            {
                var tweetOwnerId = connection.ExecuteScalar<int>("SELECT UserId FROM Tweets WHERE Id = @TweetId", new { model.TweetId });

                if (model.UserId != tweetOwnerId)
                {
                    var tweetOwner = connection.QueryFirstOrDefault("SELECT Email, Username FROM Users WHERE Id = @UserId", new { UserId = tweetOwnerId });

                    if (tweetOwner != null)
                    {
                        var user = new UserModel
                        {
                            Email = tweetOwner.Email,
                            Username = tweetOwner.Username,
                        };

                        ViewBag.Subject = "Yeni Yanıt: Tweetinize Yanıt Geldi!";
                        ViewBag.Body = $"<p>Merhaba {model.Username},</p>\r\n            <p>Tweetinize bir yanıt alındı. Yanıt aşağıda belirtilmiştir:</p>\r\n      <p>{model.Reply}</p>\r\n        <p>Yanıtınızla ilgili daha fazla bilgi almak ve etkileşimde bulunmak için Twitter hesabınıza giriş yapabilirsiniz.</p>\r\n       <p><a href=\"https://twitter.miraykaragoz.com.tr/Account/Login\" class=\"button\">Yanıtı Gör</a></p>";

                        SendMail(user);
                    }
                }

                return RedirectToAction("TweetDetail", "Home", new { id = model.TweetId });
            }
            else
            {
                ViewBag.mesaj = "Yanıt eklenirken bir hata oluştu.";
                return View();
            }
        }

        [Route("/yorumsil/{id}")]
        public IActionResult DeleteReply(int id)
        {
            if (!CheckLogin())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = (int)HttpContext.Session.GetInt32("Id");

            using var connection = new SqlConnection(connectionString);

            var reply = connection.QueryFirstOrDefault<ReplyModel>("SELECT * FROM Replies WHERE Id = @Id", new { Id = id });

            if (reply == null)
            {
                ViewBag.Message = "Yanıt bulunamadı.";
                ViewBag.MessageCssClass = "alert-secondary";
                return View("Message");
            }

            var tweetOwnerId = connection.ExecuteScalar<int>("SELECT UserId FROM Tweets WHERE Id = @TweetId", new { TweetId = reply.TweetId });

            if (reply.UserId == userId || tweetOwnerId == userId)
            {
                var sql = "DELETE FROM Replies WHERE Id = @Id";
                connection.Execute(sql, new { Id = id });

                ViewBag.Message = "Yanıt silindi.";
                ViewBag.MessageCssClass = "alert-primary";
            }
            else
            {
                ViewBag.Message = "Silme izniniz yok.";
                ViewBag.MessageCssClass = "alert-secondary";
            }

            return View("Message");
        }

        [Route("/tweetsil/{id}")]
        public IActionResult DeleteTweet(int id)
        {
            if (!CheckLogin())
            {
                ViewBag.Message = "Tweet silmek için giriş yapmanız gerekiyor.";
                ViewBag.MessageCssClass = "alert-secondary";
                return View("Message");
            }

            var userId = (int)HttpContext.Session.GetInt32("Id");

            using var connection = new SqlConnection(connectionString);

            var tweet = connection.QueryFirstOrDefault("SELECT UserId FROM Tweets WHERE Id = @Id", new { Id = id });

            if (tweet == null)
            {
                ViewBag.Message = "Tweet bulunamadı.";
                ViewBag.MessageCssClass = "alert-secondary";
                return View("Message");
            }

            if (tweet.UserId != userId)
            {
                ViewBag.Message = "Tweet'i silme yetkiniz yok.";
                ViewBag.MessageCssClass = "alert-secondary";
                return View("Message");
            }

            var deleteRepliesSql = "DELETE FROM Replies WHERE TweetId = @Id";
            connection.Execute(deleteRepliesSql, new { Id = id });

            var deleteTweetSql = "DELETE FROM Tweets WHERE Id = @Id";
            connection.Execute(deleteTweetSql, new { Id = id });

            ViewBag.Message = "Tweet silindi.";
            ViewBag.MessageCssClass = "alert-primary";
            return View("Message");
        }

        //public IActionResult EditTweet(int id)
        //{
        //    if (!CheckLogin())
        //    {
        //        ViewBag.Message = "Tweet düzenlemek için giriş yapmanız gerekiyor.";
        //        ViewBag.MessageCssClass = "alert-secondary";
        //        return View("Message");
        //    }

        //    var userId = (int)HttpContext.Session.GetInt32("Id");

        //    using var connection = new SqlConnection(connectionString);

        //    var tweet = connection.QueryFirstOrDefault("SELECT * FROM Tweets WHERE Id = @Id", new { Id = id });

        //    if (tweet == null)
        //    {
        //        ViewBag.Message = "Tweet bulunamadı.";
        //        ViewBag.MessageCssClass = "alert-secondary";
        //        return View("Message");
        //    }

        //    if (tweet.UserId != userId)
        //    {
        //        ViewBag.Message = "Tweet'i düzenleme yetkiniz yok.";
        //        ViewBag.MessageCssClass = "alert-secondary";
        //        return View("Message");
        //    }

        //    return View(tweet);
        //}

        //[HttpPost]
        //[Route("/TweetDuzenle")]
        //public IActionResult TweetDuzenle(TweetModel model)
        //{
        //    if (!CheckLogin())
        //    {
        //        ViewBag.Message = "Tweet düzenlemek için giriş yapmanız gerekiyor.";
        //        ViewBag.MessageCssClass = "alert-secondary";
        //        return View("Message");
        //    }

        //    var userId = (int)HttpContext.Session.GetInt32("Id");

        //    using var connection = new SqlConnection(connectionString);

        //    var tweet = connection.QueryFirstOrDefault("SELECT * FROM Tweets WHERE Id = @Id", new { Id = model.Id });

        //    if (tweet == null)
        //    {
        //        ViewBag.Message = "Tweet bulunamadı.";
        //        ViewBag.MessageCssClass = "alert-secondary";
        //        return View("Message");
        //    }

        //    if (tweet.UserId != userId)
        //    {
        //        ViewBag.Message = "Tweet'i düzenleme yetkiniz yok.";
        //        ViewBag.MessageCssClass = "alert-secondary";
        //        return View("Message");
        //    }

        //    var updateTweetSql = "UPDATE Tweets SET Tweet = @Tweet WHERE Id = @Id";
        //    connection.Execute(updateTweetSql, model);

        //    ViewBag.Message = "Tweet güncellendi.";
        //    ViewBag.MessageCssClass = "alert-primary";
        //    return View("Message");
        //}

        public IActionResult SendMail(UserModel model)
        {
            var client = new SmtpClient("smtp.eu.mailgun.org", 587)
            {
                Credentials = new NetworkCredential("postmaster@bilgi.miraykaragoz.com.tr", ""),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("postmaster@bilgi.miraykaragoz.com.tr", "Twitter"),
                Subject = ViewBag.Subject,
                Body = ViewBag.Body,
                IsBodyHtml = true
            };

            mailMessage.ReplyToList.Add(model.Email);

            mailMessage.To.Add(new MailAddress($"{model.Email}", $"{model.Username}"));

            client.Send(mailMessage);

            return RedirectToAction(ViewBag.Return);
        }
    }
}
