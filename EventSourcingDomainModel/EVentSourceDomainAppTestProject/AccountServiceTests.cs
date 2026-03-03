using EventSourcingDomainModelApp.Application;
using EventSourcingDomainModelApp.Domain;
using EventSourcingDomainModelApp.Infrastructure;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVentSourceDomainAppTestProject
{
    public class AccountServiceTests
    {
        private readonly string _name = "John Doe";
       
        [Fact]
        public async Task HandleAmountDeposit_ShouldLoadAndSaveEvents()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockStore = new Mock<IEventStore>();

            // Mock loading: Return an existing opened account
            mockStore.Setup(s => s.LoadEventsAsync(id))
                     .ReturnsAsync(new List<DomainEvent> { new AccountOpened(id, _name, 100, DateTime.UtcNow) });

            var service = new AccountService(mockStore.Object);
            
            // Act
            await service.HandleAmountDeposit(id, 50);

            // Assert: Verify SaveEventsAsync was called with a MoneyDeposited event
            mockStore.Verify(s => s.SaveEventsAsync(
                id,
                It.Is<IEnumerable<DomainEvent>>(events => events.Any(e => e is MoneyDeposited)),
               1), 
                Times.Once);
        }

        [Fact]
        public async Task HandleAmountWithdraw_ShouldLoadAndSaveEvents()
        {
            var id = Guid.NewGuid();
            var mockStore = new Mock<IEventStore>();

            mockStore.Setup(s=>s.LoadEventsAsync(id))
                     .ReturnsAsync(new List<DomainEvent> { new AccountOpened(id, _name, 100, DateTime.UtcNow)});

            mockStore.Setup(s => s.LoadEventsAsync(id))
                      .ReturnsAsync(new List<DomainEvent> { new MoneyDeposited(id, 300, DateTime.Now) });

            var service = new AccountService(mockStore.Object);


            await service.HandleWithdrawal(id, 150);

            mockStore.Verify(s=>s.SaveEventsAsync(
                id, 
                It.Is<IEnumerable<DomainEvent>>(events=>events.Any(e => e is WithdrawalPerformed)),
                1), Times.Once);
        }
    }

}
