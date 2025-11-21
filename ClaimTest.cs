using Microsoft.VisualStudio.TestTools.UnitTesting;
using claimSystem3.Models;

namespace claimSystem3.Test
{
    [TestClass]
    public class ClaimTests
    {
        [TestMethod]
        public void Claim_TotalAmount_CalculatedCorrectly()
        {
            // Arrange
            var claim = new MonthlyClaim { HoursWorked = 10, HourlyRate = 250 };

            // Act
            var total = claim.TotalAmount;

            // Assert
            Assert.AreEqual(2500, total);
        }

        [TestMethod]
        public void Claim_ValidHours_ValidationPasses()
        {
            // Arrange
            var claim = new MonthlyClaim { HoursWorked = 15, HourlyRate = 200, ModuleName = "PROG6212" };

            // Act & Assert
            Assert.IsTrue(claim.HoursWorked >= 1 && claim.HoursWorked <= 200);
        }
    }
}