

namespace BankLedger.WriteProject.Application.Sagas
{
    public class MoneyTransferSagaState
    {
        public Guid SagaId { get; init; } = Guid.NewGuid();

        public Guid SourceAccountId { get; init; }

        public Guid TargetAccountId { get; init; }

        public decimal Amount { get; init; }

        public TransferWorkflowState CurrentState = TransferWorkflowState.NotStarted;
    }

    public enum TransferWorkflowState
    {
        NotStarted,
        WithdrawalStarted,
        WithdrawalCompleted,
        DepositCompleted,
        CompensationStarted,
        FailedAndReversed
    }
}
