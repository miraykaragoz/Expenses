using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using expenses.Models;
using Dapper;

namespace expenses.Controllers
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

        public IActionResult Index()
        {
            ViewData["username"] = HttpContext.Session.GetString("username");
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");

            if (CheckLogin() != true)
            {
                return View();
            }
            else
            {
                return RedirectToAction("ExpenseReport", "Home");
            }
        }

        public IActionResult ExpenseReport()
        {
            if (CheckLogin() != true)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                ViewData["username"] = HttpContext.Session.GetString("username");
                ViewData["Id"] = HttpContext.Session.GetInt32("Id");

                using var connection = new SqlConnection(connectionString);

                var expense = connection.Query<ExpenseModel>("SELECT * FROM Expenses WHERE UserId = @Id", new { Id = HttpContext.Session.GetInt32("Id") }).ToList();

                var income = connection.Query<IncomeModel>("SELECT * FROM Incomes WHERE UserId = @Id", new { Id = HttpContext.Session.GetInt32("Id") }).ToList();

                ViewBag.Expense = expense;
                ViewBag.Income = income;

                return View(new PieChartModel());
            }
        }
    }
}
