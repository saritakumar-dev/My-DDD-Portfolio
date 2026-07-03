
using BankLedger.Core.Common;
using BankLedger.Core.Common.Commands;
using BankLedger.Core.Common.Events;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Sagas;
using Moq;

namespace BankLedger.Application.Tests
{
    public class MoneyTransferSagaTests
    {

        private readonly MoneyTransferSaga _saga;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ISagaStateRepository> _mockRepo;
        private readonly Mock<ICommandHandler<DepositMoneyCommand>> _mockDepositHandler;
        public MoneyTransferSagaTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockRepo = new Mock<ISagaStateRepository>();
            _mockDepositHandler = new Mock<ICommandHandler<DepositMoneyCommand>>();

            _saga = new MoneyTransferSaga(_mockServiceProvider.Object, _mockRepo.Object);

            _mockServiceProvider.Setup(p=>p.GetService(typeof(ICommandHandler<DepositMoneyCommand>)))
                                           .Returns(_mockDepositHandler.Object);
        }

        [Fact]
        public async Task HandleDepositFailed_ValidActiveSaga_ShouldTriggerSourceAccountRefundAndCompleteCompensation()
        {
            var sourceAccounId = Guid.NewGuid();
            var targetAccounId = Guid.NewGuid();
            var sagaId = Guid.NewGuid(); 

            var existingState = new MoneyTransferSagaState
            {
                Amount = 150.00m,
                SourceAccountId = sourceAccounId,
                CurrentState = TransferWorkflowState.WithdrawalCompleted,
                TargetAccountId = targetAccounId,
                SagaId = sagaId,
            };

            _mockRepo
                .Setup(r => r.GetStateBySagaIdAsync(sagaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingState);

            var failureEvent = new DepositMoneyFailedEvent(
                accountId: targetAccounId,
                amount: 150.00m,
                reason: "Target account is closed.",
                reference: $"Transfer In | SagaId:{sagaId}",
                version: 2
            );

            await _saga.HandleAsync(failureEvent, CancellationToken.None);

            _mockRepo.Verify(r => r.SaveAsync(It.Is<MoneyTransferSagaState>(s => s.CurrentState == TransferWorkflowState.FailedAndReversed),
                                          It.IsAny<CancellationToken>()), Times.Exactly(2));

            _mockDepositHandler.Verify(h => h.HandleAsync(It.Is<DepositMoneyCommand>(c => c.AccountId == sourceAccounId && c.Amount == 150.00m &&
                                                                                    c.Reference.Contains("REVERSAL")), It.IsAny<CancellationToken>())
                                                        , Times.Once);

        }
    }
}
