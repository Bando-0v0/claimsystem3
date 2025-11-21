using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using claimSystem3.Models;
using claimSystem3.Data;

namespace claimSystem3.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get current user info
            var userRole = User.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Lecturer";
            var userName = User.Identity?.Name ?? "User";
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            // Pass user info to view
            ViewBag.UserRole = userRole;
            ViewBag.UserName = userName;

            // Get recent claims for lecturers
            if (userRole == "Lecturer" && !string.IsNullOrEmpty(userId))
            {
                try
                {
                    var recentClaims = await _context.MonthlyClaims
                        .Where(c => c.LecturerId == userId)
                        .OrderByDescending(c => c.SubmissionDate)
                        .Take(5)
                        .ToListAsync();

                    ViewBag.RecentClaims = recentClaims;
                }
                catch (Exception ex)
                {
                    // Log error but don't crash the page
                    Console.WriteLine($"Error loading recent claims: {ex.Message}");
                    ViewBag.RecentClaims = new List<MonthlyClaim>();
                }
            }
            else
            {
                ViewBag.RecentClaims = new List<MonthlyClaim>();
            }

            return View();
        }

        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                // Test if database connection works
                var claimCount = await _context.MonthlyClaims.CountAsync();
                return Content($"Database connection SUCCESS! Total claims: {claimCount}");
            }
            catch (Exception ex)
            {
                return Content($"Database connection FAILED: {ex.Message}");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}