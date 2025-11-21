using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using claimSystem3.Models;
using claimSystem3.Data;
using Microsoft.EntityFrameworkCore;

namespace claimSystem3.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClaimController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Claim/Submit
        [Authorize(Roles = "Lecturer")]
        public IActionResult Submit()
        {
            return View();
        }

        // POST: /Claim/Submit
        [HttpPost]
        [Authorize(Roles = "Lecturer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(MonthlyClaim claim, IFormFile document)
        {
            Console.WriteLine("=== SUBMIT METHOD STARTED ===");

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("Model is valid");

                    // Get user info from session/claims
                    var userId = HttpContext.Session.GetString("UserId") ?? Guid.NewGuid().ToString();
                    var userName = HttpContext.Session.GetString("UserName") ?? "Unknown User";

                    Console.WriteLine($"User ID: {userId}");
                    Console.WriteLine($"User Name: {userName}");

                    claim.LecturerId = userId;
                    claim.SubmissionDate = DateTime.Now;
                    claim.Status = ClaimStatus.Pending;
                    claim.TotalAmount = claim.HoursWorked * claim.HourlyRate; // Make sure this is set

                    Console.WriteLine($"Claim details - Module: {claim.ModuleName}, Hours: {claim.HoursWorked}, Rate: {claim.HourlyRate}");

                    // Handle document upload
                    if (document != null && document.Length > 0)
                    {
                        Console.WriteLine("Document uploaded");
                        // Validate file size (5MB max)
                        if (document.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "File size must be less than 5MB.");
                            return View(claim);
                        }

                        // Validate file type
                        var allowedExtensions = new[] { ".pdf", ".docx", ".jpg", ".png", ".jpeg" };
                        var fileExtension = Path.GetExtension(document.FileName).ToLower();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "Only PDF, DOCX, JPG, and PNG files are allowed.");
                            return View(claim);
                        }

                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + document.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await document.CopyToAsync(stream);
                        }

                        claim.DocumentPath = uniqueFileName;
                        Console.WriteLine($"Document saved: {uniqueFileName}");
                    }
                    else
                    {
                        Console.WriteLine("No document uploaded");
                        claim.DocumentPath = ""; // Ensure it's not null
                    }

                    Console.WriteLine("Attempting to save claim to database...");
                    _context.MonthlyClaims.Add(claim);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Claim saved successfully!");

                    TempData["Success"] = "Claim submitted successfully!";
                    return RedirectToAction("Status");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== ERROR IN SUBMIT ===");
                    Console.WriteLine($"Error Message: {ex.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    Console.WriteLine($"=== END ERROR ===");

                    ModelState.AddModelError("", $"An error occurred while submitting your claim: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("=== MODEL STATE ERRORS ===");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
                Console.WriteLine("=== END MODEL ERRORS ===");
            }

            return View(claim);
        }

        // In Approve method - replace with:
        [Authorize(Roles = "Coordinator,Manager")]
        public async Task<IActionResult> Approve()
        {
            try
            {
                var userRole = User.Claims.FirstOrDefault(c => c.Type == "Role")?.Value ?? "Coordinator";
                Console.WriteLine($"DEBUG: Loading claims for {userRole} approval");

                var claims = await _context.MonthlyClaims
                    .Where(c =>
                        (userRole == "Coordinator" && c.Status == ClaimStatus.Pending) ||
                        (userRole == "Manager" && c.Status == ClaimStatus.ApprovedByCoordinator))
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Found {claims.Count} claims for approval");
                return View(claims);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DATABASE ERROR in Approve: {ex.Message}");
                TempData["Error"] = "Cannot load claims. Database error.";
                return View(new List<MonthlyClaim>());
            }
        }

        [HttpPost]
        [Authorize(Roles = "Coordinator,Manager")]
        public async Task<IActionResult> ApproveClaim(int claimId, bool isApproved, string comments)
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole") ?? "Coordinator";
                var claim = await _context.MonthlyClaims.FindAsync(claimId);

                if (claim != null)
                {
                    var approval = new ClaimApproval
                    {
                        ClaimId = claimId,
                        ApprovedById = HttpContext.Session.GetString("UserId") ?? Guid.NewGuid().ToString(),
                        Role = userRole,
                        IsApproved = isApproved,
                        Comments = comments,
                        ApprovalDate = DateTime.Now
                    };

                    _context.ClaimApprovals.Add(approval);

                    // Update claim status based on role and decision
                    if (userRole == "Coordinator")
                    {
                        claim.Status = isApproved ? ClaimStatus.ApprovedByCoordinator : ClaimStatus.RejectedByCoordinator;
                    }
                    else if (userRole == "Manager")
                    {
                        claim.Status = isApproved ? ClaimStatus.ApprovedByManager : ClaimStatus.RejectedByManager;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Claim {(isApproved ? "approved" : "rejected")} successfully!";
                }
                else
                {
                    TempData["Error"] = "Claim not found!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"APPROVE CLAIM ERROR: {ex.Message}");
                TempData["Error"] = "An error occurred while processing the claim.";
            }

            return RedirectToAction("Approve");
        }

        // GET: /Claim/Status
        public async Task<IActionResult> Status()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId") ?? Guid.NewGuid().ToString();
                var claims = await _context.MonthlyClaims
                    .Where(c => c.LecturerId == userId)
                    .OrderByDescending(c => c.SubmissionDate)
                    .ToListAsync();

                return View(claims);
            }
            catch (Exception ex)
            {
                // Log the exact error
                Console.WriteLine($"DATABASE ERROR in Status: {ex.Message}");
                Console.WriteLine($"INNER EXCEPTION: {ex.InnerException?.Message}");

                // Return empty list for now
                return View(new List<MonthlyClaim>());
            }
        }

        // NEW: HR View for automated processing
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HRView()
        {
            try
            {
                var approvedClaims = await _context.MonthlyClaims
                    .Include(c => c.Lecturer)
                    .Where(c => c.Status == ClaimStatus.ApprovedByManager)
                    .ToListAsync();

                return View(approvedClaims);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DATABASE ERROR in HRView: {ex.Message}");
                return View(new List<MonthlyClaim>());
            }
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> ProcessPayment(int claimId)
        {
            try
            {
                var claim = await _context.MonthlyClaims.FindAsync(claimId);
                if (claim != null)
                {
                    claim.Status = ClaimStatus.Paid;
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Payment processed successfully!";
                }
                else
                {
                    TempData["Error"] = "Claim not found!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PROCESS PAYMENT ERROR: {ex.Message}");
                TempData["Error"] = "An error occurred while processing payment.";
            }

            return RedirectToAction("HRView");
        }
    }
}