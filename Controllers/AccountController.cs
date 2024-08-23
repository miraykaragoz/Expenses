using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using expenses.Models;
using System.Net.Mail;
using System.Net;
using Dapper;

namespace expenses.Controllers
{
    public class AccountController : Controller
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
                return RedirectToAction("Login");
            }
            else
            {
                return RedirectToAction("ExpenseReport", "Home");
            }
        }

        public IActionResult Register()
        {
            if (!CheckLogin())
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
                ViewBag.AuthError = TempData["AuthError"] as string;
                return View(new UserModel());
            }
            else
            {
                return RedirectToAction("ExpenseReport", "Home");
            }
        }

        [HttpPost]
        [Route("/kayit")]
        public IActionResult Kayit(UserModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Form eksik veya hatalı.";
                return View("Register", model);
            }

            if (!string.Equals(model.Password, model.PasswordConfirm))
            {
                ViewBag.ErrorMessage = "Şifreler birbiriyle uyuşmuyor!";
                return View("Register", model);
            }

            using var connection = new SqlConnection(connectionString);

            var checkUsername = "SELECT * FROM Users WHERE Username = @Username";

            var checkInputUsername = connection.QueryFirstOrDefault<UserModel>(checkUsername, new { model.Username });

            if (checkInputUsername != null)
            {
                ViewBag.ErrorMessage = "Bu kullanıcı adı zaten kullanılıyor!";
                return View("Register", model);
            }

            var checkMail = "SELECT * FROM Users WHERE Email = @Email";

            var checkInputMail = connection.QueryFirstOrDefault<UserModel>(checkMail, new { model.Email });

            if (checkInputMail != null)
            {
                ViewBag.ErrorMessage = "Bu e-posta adresi zaten kullanılıyor!";
                return View("Register", model);
            }

            var sql = "INSERT INTO Users (Name, Surname, Email, Username, Password) VALUES (@Name, @Surname, @Email, @Username, @Password)";

            var data = new
            {
                model.Name,
                model.Surname,
                model.Email,
                model.Username,
                Password = Helper.Hash(model.Password),
            };

            var rowsAffected = connection.Execute(sql, data);

            using var reader = new StreamReader("wwwroot/mail/Welcome.html");
            var template = reader.ReadToEnd();
            var mailBody = template;

            var client = new SmtpClient("smtp.eu.mailgun.org", 587)
            {
                Credentials = new NetworkCredential("", ""),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("", "Expenses"),
                Subject = "Hoşgeldiniz!",
                Body = mailBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(model.Email, "Değerli kullancı,"));

            client.Send(mailMessage);

            ViewBag.SuccessMessage = "Kayıt başarılı bir şekilde gerçekleşti!";

            return View("Register", model);
        }

        public IActionResult Login()
        {
            if (!CheckLogin())
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
                ViewBag.AuthError = TempData["AuthError"] as string;
                return View(new UserModel());
            }
            else
            {
                return RedirectToAction("ExpenseReport", "Home");
            }
        }

        [HttpPost]
        [Route("/giris")]
        public IActionResult Giris(UserModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                TempData["AuthError"] = "Kullanıcı adı veya şifre boş olamaz.";
                return RedirectToAction("login");
            }

            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password";

            var user = connection.QuerySingleOrDefault<UserModel>(sql, new { model.Username, Password = Helper.Hash(model.Password) });

            if (user != null)
            {
                HttpContext.Session.SetString("username", user.Username);
                HttpContext.Session.SetInt32("Id", user.Id);

                if (!string.IsNullOrEmpty(model.RedirectUrl))
                {
                    return Redirect(model.RedirectUrl);
                }

                return RedirectToAction("ExpenseReport", "Home");
            }

            TempData["AuthError"] = "Kullanıcı adı veya şifre hatalı!";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ForgotPassword()
        {
            if (!CheckLogin())
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
                ViewBag.AuthError = TempData["AuthError"] as string;
                return View(new UserModel());
            }
            else
            {
                return RedirectToAction("ExpenseReport", "Home");
            }
        }

        [HttpPost]
        public IActionResult ForgotPassword(UserModel model)
        {
            if(string.IsNullOrEmpty(model.Email))
            {
                ViewBag.ErrorMessage = "E-Posta adresi boş olamaz.";
                return View();
            }

            using var connection = new SqlConnection(connectionString);

            var checkMail = connection.QueryFirstOrDefault<UserModel>("SELECT * FROM Users WHERE Email = @Email", new { model.Email });

            if ((checkMail != null))
            {
                var client = new SmtpClient("smtp.eu.mailgun.org", 587)
                {
                    Credentials = new NetworkCredential("", ""),
                    EnableSsl = true
                };

                var Key = Guid.NewGuid().ToString();

                var newPassword = "UPDATE Users SET ResetKey = @ResetKey WHERE Email = @Email";

                var newData = new
                {
                    checkMail.Email,
                    ResetKey = Key
                };

                var affectedRows = connection.Execute(newPassword, newData);

                ViewBag.Subject = "Şifre Sıfırlama Talebi";
                ViewBag.Body = $"<p>Merhaba {model.Username},</p>\r\n            <p>Şifrenizi sıfırlamak için bir talepte bulunduz. Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayarak işlemlerinize devam edebilirsiniz.</p>\r\n            <p><a href=\"https://expenses.miraykaragoz.com.tr/Account/ResetPassword/{Key}\" class=\"button\">Şifreyi Sıfırla</a></p>";

                SendMail(model);

                ViewBag.SuccessMessage = "Şifre Sıfırlama Talebiniz Başarıyla Alındı. Şifrenizi değiştirmek için mail adresinize gelen adımları takip ediniz.";
                return View();
            }
            else
            {
                ViewBag.ErrorMessage = "Bu E-Postaya ait bir hesap bulunamadı.";
                return View();
            }
        }

        public IActionResult ResetPassword(string key)
        {
            if (!CheckLogin())
            {
                using var connection = new SqlConnection(connectionString);

                var checkKey = connection.QueryFirstOrDefault<UserModel>("SELECT * FROM Users WHERE ResetKey = @ResetKey", new { ResetKey = key });

                if (checkKey != null)
                {
                    return View(checkKey);
                }
                else
                {
                    ViewBag.MessageCssClass = "alert alert-danger";
                    ViewBag.Message = "Geçersiz bir anahtar girdiniz!";
                    return View("Message");
                }
            }
            else
            {
                return RedirectToAction("ExpenseReport", "Home");
            }
        }

        [HttpPost]
        public IActionResult ResetPassword(UserModel model)
        {
            using var connection = new SqlConnection(connectionString);

            var newPassword = "UPDATE Users SET Password = @Password WHERE Id = @Id";

            var newData = new
            {
                Password = Helper.Hash(model.Password),
                model.Id,
            };

            var affectedRows = connection.Execute(newPassword, newData);

            if (affectedRows > 0)
            {
                ViewBag.SuccessMessage = "Şifreniz başarıyla değiştirildi. Yeni şifrenizle giriş yapabilirsiniz.";
            }
            else
            {
                ViewBag.ErrorMessage = "Şifreniz değiştirilirken bir hata oluştu. Lütfen tekrar deneyiniz.";
            }

            return View();
        }

        public int GetUserId(string username)
        {
            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT Id FROM Users WHERE Username = @Username";

            var userId = connection.QueryFirstOrDefault<int>(sql, new { username });

            return userId;
        }

        [Route("/profile/{username}")]
        public IActionResult Profile(string username)
        {
            if (CheckLogin() != true)
            {
                return RedirectToAction("Login");
            }

            ViewData["username"] = HttpContext.Session.GetString("username");

            int? id = GetUserId(username);

            if (id == null || id != HttpContext.Session.GetInt32("Id"))
            {
                ViewBag.MessageCssClass = "alert alert-danger";
                ViewBag.Message = "Böyle bir kullanıcı bulunamadı veya bu sayfayı görüntülemeye yetkiniz yok!";
                return View("Message");
            }

            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT * FROM Users WHERE Id = @Id";

            var user = connection.QueryFirstOrDefault<UserModel>(sql, new { Id = id });

            return View(user);
        }

        public IActionResult EditProfile(string? username)
        {
            ViewData["username"] = HttpContext.Session.GetString("username");

            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Kullanıcı profilinizi güncellemek için önce giriş yapmalısınız!";
                return RedirectToAction("Login");
            }

            int? id = GetUserId(username);

            if (id == null || id != HttpContext.Session.GetInt32("Id"))
            {
                ViewBag.MessageCssClass = "alert alert-danger";
                ViewBag.Message = "Böyle bir kullanıcı bulunamadı veya bu sayfayı görüntülemeye yetkiniz yok!";
                return View("Message");
            }

            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT * FROM Users WHERE Username = @Username";

            var user = connection.QueryFirstOrDefault<UserModel>(sql, new { Username = username });

            return View(user);
        }

        [HttpPost]
        [Route("/kullaniciduzenle/{username}")]
        public IActionResult KullaniciDuzenle(UserModel model)
        {
            int? userId = HttpContext.Session.GetInt32("Id");

            if (userId != model.Id)
            {
                ViewBag.ErrorMessage = "Bu işlemi yapmaya yetkiniz yok!";
                return RedirectToAction("Login", "Account");
            }

            ViewData["Id"] = userId;
            ViewData["username"] = HttpContext.Session.GetString("username");

            using var connection = new SqlConnection(connectionString);

            var sql = "UPDATE Users SET Name = @Name, Surname = @Surname, Email = @Email, Password = @Password WHERE Id = @Id";

            var data = new
            {
                model.Name,
                model.Surname,
                model.Email,
                Password = Helper.Hash(model.Password),
                model.Id,
            };

            var rowsAffected = connection.Execute(sql, data);

            ViewBag.SuccessMessage = "Kullanıcı bilgileriniz başarıyla güncellendi!";

            return View("EditProfile", model);
        }

        public IActionResult DeleteUser(int id)
        {
            using var connection = new SqlConnection(connectionString);

            var sql = "DELETE FROM Users WHERE Id = @Id";

            var rowsAffected = connection.Execute(sql, new { Id = id });

            if (rowsAffected > 0)
            {
                ViewBag.SuccessMessage = "Hesabınız başarıyla silindi!";
                return RedirectToAction("Logout");
            }
            else
            {
                ViewBag.ErrorMessage = "Hesabınız silinirken bir hata oluştu!";
                return RedirectToAction("Profile", new { username = HttpContext.Session.GetString("username") });
            }
        }

        public IActionResult SendMail(UserModel model)
        {
            var client = new SmtpClient("smtp.eu.mailgun.org", 587)
            {
                Credentials = new NetworkCredential("", ""),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("", "Expenses"),
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
