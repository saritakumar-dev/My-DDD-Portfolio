using EventSourcingDomainModelApp.Domain;

namespace EVentSourceDomainAppTestProject
{
    public class CreditAccountTests
    {

        private readonly string _name= "John Doe";
        [Fact]
        public void Withdraw_UnderLimit_ShouldReturnMoneyWithdrawn()
        {

            var id = Guid.NewGuid();
            IEnumerable<DomainEvent> history = new List<DomainEvent>()
            {
                (new AccountOpened(id, _name, 500, DateTime.UtcNow)),
                (new MoneyDeposited(id, 800, DateTime.UtcNow))
            };


            var account = new CreditAccount();
            account.LoadFromHistory(history);

            account.Withdraw(150);

            var changes = account.GetUncommittedChanges();
            Assert.Single(changes);
            Assert.IsType<WithdrawalPerformed>(changes.First());
            Assert.Equal(650, account.Balance);
        }

        [Fact]
        public void Withdraw_OverLimit_ShouldThrowException()
        {
            // Arrange: $0 balance, $100 limit
            var id = Guid.NewGuid();
            var account = new CreditAccount();
            account.LoadFromHistory(new[] { new AccountOpened(id, _name, 100, DateTime.UtcNow) });

            // Act & Assert: Should throw when withdrawing $150
            Assert.Throws<Exception>(() => account.Withdraw(150));
        }
    }
}