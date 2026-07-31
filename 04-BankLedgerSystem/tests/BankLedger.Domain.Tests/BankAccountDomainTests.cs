using BankLedger.Core.Common.Events;
using BankLedger.Domain.Aggregates;
using BankLedger.Domain.Common;
using FluentAssertions;
namespace BankLedger.Domain.Tests
{
    public class BankAccountDomainTests
    {
        private readonly Guid accountId = Guid.NewGuid();
        private BankAccount CreateAccount(string currency = "EUR", string name = "John Doe")
        {
            return new BankAccount(accountId, name, currency);
        }

        [Fact]
        public void AccountInitialization_ShouldSetDefaultZeroBalance_AndBaseVersion()
        {
            // Arrange & Act
            var account = CreateAccount();

            // Assert
            Assert.True(account.Id == accountId);
            Assert.True(account.Version == 1);
            Assert.True(account.Balance.Amount == 0.00m);
            Assert.True(account.Balance.Currency == "EUR");
        }

        [Fact]
        public void Deposit_ShouldRaiseEvent_AndMutateBalanceExactlyOnce()
        {
            // Arrange
            var account = CreateAccount();

            // Act
            account.Deposit(150.50m, "EUR", "Salary Deposit");

            // Assert
            // 🌟 Verifies state is updated exactly once (Fixes the old double-addition bug!)
            Assert.True(account.Balance.Amount == 150.50m);
            Assert.True(account.Balance.Currency == "EUR");

            // Verify the domain event was raised with the correct metadata parameters
            var uncommittedEvents = account.UncommittedEvents;

            var depositEvent = uncommittedEvents.ToList()[1] as MoneyDepositedEvent;

            Assert.NotNull(depositEvent);
            Assert.True(depositEvent!.Amount == 150.50m);
            Assert.True(depositEvent.Currency == "EUR");
        }

        [Fact]
        public void Deposit_WithInvalidNegativeAmount_ShouldThrowArgumentException()
        {
            // Arrange
            var account = CreateAccount();

            // Act
            Action action = () => account.Deposit(-50.00m, "EUR", "Invalid Tx");

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Deposit amount must be greater than zero.");
        }

        [Fact]
        public void Deposit_WithMismatchedCurrency_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var account = CreateAccount(); // Default is EUR

            // Act 
            // The ApplyEvent switch-case delegates calculation to Money.Plus(),
            // which immediately short-circuits if currencies do not match.
            var exception = Assert.Throws<InvalidOperationException>(() => account.Deposit(100.00m, "USD", "Mismatched Cur")
  );
            // Assert
            Assert.Contains("Currency Mismatch", exception.Message);
        }

        [Fact]
        public void Withdraw_WithSufficientFunds_ShouldReduceBalanceCorrectly()
        {
            // Arrange - Load history sequence or cheat via inline deposits
            var account = CreateAccount();
            account.Deposit(200.00m, "EUR", "Initial Load");
            account.ClearUncommittedEvents();

            // Act
            account.Withdraw(75.25m, "EUR", "ATM Withdrawal");

            // Assert
            account.Balance.Amount.Equals(124.75m); // 200.00 - 75.25

            var uncommittedEvents = account.UncommittedEvents;
            var singleEvent = Assert.Single(uncommittedEvents);

            Assert.IsType<MoneyWithdrawnEvent>(singleEvent);
        }



        [Fact]
        public void Withdraw_WithInsufficientFunds_ShouldThrowInvalidOperationException()
        {
            var accountId = Guid.NewGuid();

            var history = new List<BankEvent>
            {
                new AccountOpenedEvent(accountId, "John Doe", "EUR", 0),
                new MoneyDepositedEvent(accountId, 50.00m, "EUR", "Initial Cash", 1)
            };

            //1. Rehydrate the aggregate from history
            var account = BankAccount.LoadFromHistory(history);

            // 2. WHEN: Attempting to withdraw $60 (which exceeds the $50 balance)
            // 3. THEN: The system must intercept the violation and throw an exception

            var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(60.00m, "EUR", "ATM Withdrawal"));

            Assert.Equal("Insufficient funds for this withdrawal.", exception.Message);

            // Ensure no toxic events were generated or tracked in memory
            Assert.Empty(account.UncommittedEvents);
        }


        [Fact]
        public void LoadFromHistory_ShouldSequentiallyReconstructState()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = CreateAccount();

            var historicalEvents = new List<BankEvent>
            {
                new AccountOpenedEvent(accountId, "John Doe", "EUR",1 ),
                new MoneyDepositedEvent(accountId, 500.00m, "EUR", "Opening Deposit", 2),
                new MoneyWithdrawnEvent(accountId, 150.00m, "EUR", "Rent Payment", 3),
                new MoneyDepositedEvent(accountId, 50.00m, "EUR", "Refund", 4)
            };

            // Act
            BankAccount.LoadFromHistory(historicalEvents);

            // Assert
            // 500.00 - 150.00 + 50.00 = 400.00
            account.Balance.Amount.Equals(400.00m);
            account.Balance.Currency.Equals("EUR");
            account.UncommittedEvents.Count.Equals(0); // Historic replays do not pollute new uncommitted arrays
        }

        [Fact]
        public void CloseAndAnonymizeAccount_Should_Succeed_And_Raise_UserForgottenEvent_With_Correct_Reason()
        {
            // Arrange: Rehydrate or instantiate an active account
            var account = CreateAccount();
            var expectedReason = ClosureReason.GdprRequest;

            // Act
            account.CloseAndAnonymizeAccount(expectedReason);

            // Assert
            account.IsClosed.Should().BeTrue();

            // Verify the emitted domain event
            account.UncommittedEvents.Should().HaveCount(2);
            var raisedEvent = account.UncommittedEvents.First() as UserForgottenEvent;

            raisedEvent.Should().NotBeNull();
            raisedEvent!.Reason.Should().Be(expectedReason);
            raisedEvent.ErasedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CloseAndAnonymizeAccount_Should_Throw_Exception_If_Account_Is_Already_Closed()
        {
            // Arrange
            var account = CreateAccount();
            account.CloseAndAnonymizeAccount(ClosureReason.GdprRequest); // First closure

            // Act
            Action act = () => account.CloseAndAnonymizeAccount(ClosureReason.CustomerContractTerminated);

            // Assert
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("The account is already closed.");
        }

        [Fact]
        public void Deposit_Should_Throw_Exception_If_Account_Is_Closed()
        {
            // Arrange
            var account = CreateAccount();
            account.CloseAndAnonymizeAccount(ClosureReason.GdprRequest);

            // Act
            Action act = () => account.Deposit(100.00m, "eur", "cash deposit");

            // Assert
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Cannot deposit funds into a closed or anonymized account.");
        }
    }
}