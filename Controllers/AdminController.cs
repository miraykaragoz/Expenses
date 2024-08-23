using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using expenses.Models;
using Dapper;

namespace expenses.Controllers
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
                return RedirectToAction("ExpenseReport", "Home");
            }
        }

        public IActionResult Expenses(ExpenseModel model)
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Giderleri görmek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["username"] = HttpContext.Session.GetString("username");
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");

            using var connection = new SqlConnection(connectionString);

            var sql = @"
              SELECT Expenses.*, Categories.Category AS CategoryName 
              FROM Expenses 
              LEFT JOIN Categories ON Expenses.CategoryId = Categories.Id  
              WHERE Expenses.UserId = @Id";

            var parameters = new { Id = HttpContext.Session.GetInt32("Id") };

            var expenses = connection.Query<ExpenseModel>(sql, parameters);

            return View(expenses);
        }

        public IActionResult AddExpense()
        {
            using var connection = new SqlConnection(connectionString);

            var userId = (int)HttpContext.Session.GetInt32("Id");

            var sql = "SELECT * FROM Categories WHERE UserId = @UserId";

            var categories = connection.Query<CategoryModel>(sql, new { UserId = userId }).ToList();

            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewData["username"] = HttpContext.Session.GetString("username");

            return View(categories);
        }

        [HttpPost]
        [Route("/GiderEkle")]
        public IActionResult GiderEkle(ExpenseModel model)
        {
            if (string.IsNullOrEmpty(model.Expense) || model.Price <= 0 || model.CategoryId <= 0 || model.CategoryId <= 0)
            {
                ViewBag.MessageCssClass = "alert alert-secondary";
                ViewBag.Message = "Gider, fiyat veya kategori alanı boş bırakılamaz.";
                return View("Message");
            }

            ViewData["username"] = HttpContext.Session.GetString("username");
            model.UserId = (int)HttpContext.Session.GetInt32("Id");

            using var connection = new SqlConnection(connectionString);

            var sql = "INSERT INTO Expenses (Expense, Price, ExpenseDate, UserId, CategoryId) VALUES (@Expense, @Price, @ExpenseDate, @UserId, @CategoryId)";

            var data = new
            {
                model.Expense,
                model.Price,
                ExpenseDate = DateTime.Now,
                model.UserId,
                model.CategoryId,
            };

            var rowsAffected = connection.Execute(sql, data);

            var categorySql = "SELECT * FROM Categories WHERE UserId = @UserId";

            var categories = connection.Query<CategoryModel>(categorySql, new { UserId = model.UserId }).ToList();

            ViewBag.SuccessMessage = "Gider eklendi.";
            return View("AddExpense", categories);
        }

        public IActionResult UpdateExpense(int id)
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Gider güncellemek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewData["username"] = HttpContext.Session.GetString("username");

            using var connection = new SqlConnection(connectionString);

            var userId = (int)HttpContext.Session.GetInt32("Id");

            var expenseSql = "SELECT * FROM Expenses WHERE Id = @Id AND UserId = @UserId";
            var expense = connection.QueryFirstOrDefault<ExpenseModel>(expenseSql, new { Id = id, UserId = userId });

            var categoriesSql = "SELECT Id, Category FROM Categories WHERE UserId = @UserId";
            expense.Categories = connection.Query<CategoryModel>(categoriesSql, new { UserId = userId }).ToList();

            return View(expense);
        }


        [HttpPost]
        [Route("/GiderGuncelle")]
        public IActionResult GiderGuncelle(ExpenseModel model)
        {
            ViewData["username"] = HttpContext.Session.GetString("username");
            model.UserId = (int)HttpContext.Session.GetInt32("Id");

            using var connection = new SqlConnection(connectionString);

            var sql = "UPDATE Expenses SET Expense = @Expense, Price = @Price, ExpenseDate = @ExpenseDate, CategoryId = @CategoryId WHERE Id = @Id AND UserId = @UserId";

            var data = new
            {
                model.Expense,
                model.Price,
                ExpenseDate = DateTime.Now,
                model.CategoryId,
                model.Id,
                model.UserId,
            };

            var rowsAffected = connection.Execute(sql, data);

            ViewBag.MessageCssClass = "alert alert-secondary";
            ViewBag.Message = "Gider güncellendi.";
            return View("Message");
        }

        public IActionResult DeleteExpense(int id)
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Gider silmek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            using var connection = new SqlConnection(connectionString);

            var sql = "DELETE FROM Expenses WHERE Id = @Id";

            var rowsAffected = connection.Execute(sql, new { Id = id });

            ViewBag.MessageCssClass = "alert alert-secondary";
            ViewBag.Message = "Gider silindi.";
            return View("Message");
        }

        public IActionResult Categories(CategoryModel model)
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Kategorileri görmek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["username"] = HttpContext.Session.GetString("username");

            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT * FROM Categories WHERE UserId = @Id";

            var parameters = new { Id = HttpContext.Session.GetInt32("Id") };

            var categories = connection.Query<CategoryModel>(sql, parameters);

            return View(categories);
        }


        public IActionResult AddCategory()
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Kategori eklemek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewData["username"] = HttpContext.Session.GetString("username");
            return View();
        }

        [HttpPost]
        [Route("/KategoriEkle")]
        public IActionResult KategoriEkle(CategoryModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MessageCssClass = "alert alert-secondary";
                ViewBag.Message = "Kategori adı boş bırakılamaz.";
                return View("Message");
            }

            ViewData["username"] = HttpContext.Session.GetString("username");

            model.UserId = (int)HttpContext.Session.GetInt32("Id");

            using var connection = new SqlConnection(connectionString);

            var sql = "INSERT INTO Categories (Category, UserId) VALUES (@Category, @UserId)";

            var data = new
            {
                model.Category,
                model.UserId,
            };

            var rowsAffected = connection.Execute(sql, data);

            TempData["SuccessMessage"] = "Kategori eklendi.";
            return RedirectToAction("AddCategory");
        }

        public IActionResult UpdateCategory(int id)
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Kategori güncellemek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login");
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewData["username"] = HttpContext.Session.GetString("username");

            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT * FROM Categories WHERE Id = @Id";

            var category = connection.QueryFirstOrDefault<CategoryModel>(sql, new { Id = id });

            return View(category);
        }


        [HttpPost]
        [Route("/KategoriGuncelle")]
        public IActionResult KategoriGuncelle(CategoryModel model)
        {
            ViewData["username"] = HttpContext.Session.GetString("username");

            using var connection = new SqlConnection(connectionString);

            var sql = "UPDATE Categories SET Category = @Category WHERE Id = @Id";

            var data = new
            {
                model.Category,
                model.Id,
            };

            var rowsAffected = connection.Execute(sql, data);

            ViewBag.MessageCssClass = "alert alert-secondary";
            ViewBag.Message = "Kategori güncellendi.";
            return View("Message");
        }

        public IActionResult DeleteCategory(int id)
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Kategori silmek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login");
            }

            using var connection = new SqlConnection(connectionString);

            var sql = "DELETE FROM Categories WHERE Id = @Id";

            var rowsAffected = connection.Execute(sql, new { Id = id });

            ViewBag.MessageCssClass = "alert alert-secondary";
            ViewBag.Message = "Kategori silindi.";
            return View("Message");
        }

        public IActionResult Incomes()
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Gelirleri görmek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            ViewData["username"] = HttpContext.Session.GetString("username");

            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT * FROM Incomes WHERE UserId = @Id";

            var parameters = new { Id = HttpContext.Session.GetInt32("Id") };

            var incomes = connection.Query<IncomeModel>(sql, parameters);

            return View(incomes);
        }

        public IActionResult AddIncome()
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Gelir eklemek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewData["username"] = HttpContext.Session.GetString("username");
            return View();
        }

        [HttpPost]
        [Route("/GelirEkle")]
        public IActionResult GelirEkle(IncomeModel model)
        {
            if (string.IsNullOrEmpty(model.Income) || model.Price <= 0)
            {
                ViewBag.MessageCssClass = "alert alert-secondary";
                ViewBag.Message = "Gelir adı ve kazancı boş bırakılamaz.";
                return View("Message");
            }

            ViewData["username"] = HttpContext.Session.GetString("username");
             
            model.UserId = (int)HttpContext.Session.GetInt32("Id");

            using var connection = new SqlConnection(connectionString);

            var sql = "INSERT INTO Incomes (Income, Price, IncomeDate, UserId) VALUES (@Income, @Price, @IncomeDate, @UserId)";

            var data = new
            {
                model.Income,
                model.Price,
                IncomeDate = DateTime.Now,
                model.UserId,
            };

            var rowsAffected = connection.Execute(sql, data);

            TempData["SuccessMessage"] = "Gelir eklendi.";
            return RedirectToAction("AddIncome");
        }

        public IActionResult UpdateIncome(int id)
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Gelir güncellemek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewData["username"] = HttpContext.Session.GetString("username");

            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT * FROM Incomes WHERE Id = @Id";

            var income = connection.QueryFirstOrDefault<IncomeModel>(sql, new { Id = id });

            return View(income);
        }

        [HttpPost]
        [Route("/GelirGuncelle")]
        public IActionResult GelirGuncelle(IncomeModel model)
        {
            ViewData["username"] = HttpContext.Session.GetString("username");

            using var connection = new SqlConnection(connectionString);

            var sql = "UPDATE Incomes SET Income = @Income, Price = @Price, IncomeDate = @IncomeDate WHERE Id = @Id";

            var data = new
            {
                model.Income,
                model.Price,
                IncomeDate = DateTime.Now,
                model.Id,
            };

            var rowsAffected = connection.Execute(sql, data);

            ViewBag.MessageCssClass = "alert alert-secondary";
            ViewBag.Message = "Gelir güncellendi.";
            return View("Message");
        }

        public IActionResult DeleteIncome(int id)
        {
            if (!CheckLogin())
            {
                ViewBag.ErrorMessage = "Gelir silmek için giriş yapmanız gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            using var connection = new SqlConnection(connectionString);

            var sql = "DELETE FROM Incomes WHERE Id = @Id";

            var rowsAffected = connection.Execute(sql, new { Id = id });

            ViewBag.MessageCssClass = "alert alert-secondary";
            ViewBag.Message = "Gelir silindi.";
            return View("Message");
        }
    }
}
