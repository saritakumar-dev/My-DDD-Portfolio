using BankLedger.Core.Common.Events;
using BankLedger.WriteProject.Domain.Aggregates;

namespace BankLedger.Domain.Tests
{
    public class BankAccountTests
    {
        [Fact]
        public void Withdraw_WithInsufficientFunds_ShouldThrowInvalidOperationException()
        {
            var accountId = Guid.NewGuid();

            var history = new List<BankEvent>
            {
                new AccountOpenedEvent(accountId, "John Doe", "USD", 0),
                new MoneyDepositedEvent(accountId, 50.00m, "Initial Cash", 1)
            };

            //1. Rehydrate the aggregate from history
            var account = BankAccount.LoadFromHistory(history);

            // 2. WHEN: Attempting to withdraw $60 (which exceeds the $50 balance)
            // 3. THEN: The system must intercept the violation and throw an exception

            var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(60.00m, "ATM Withdrawal"));

            Assert.Equal("Insufficient funds for this withdrawal.", exception.Message);

            // Ensure no toxic events were generated or tracked in memory
            Assert.Empty(account.UncommittedEvents);
        }
    }
}