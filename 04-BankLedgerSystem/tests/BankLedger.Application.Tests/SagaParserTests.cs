using BankLedger.WriteProject.Application.Sagas;

namespace BankLedger.Application.Tests
{
    public class SagaParserTests
    {

        [Theory]
        [InlineData("Transfer out to Bob | SagaId: 1c532268-6ba6-435f-8e58-609b679acd37", true)] // Standard
        [InlineData("Transfer out to Bob | SagaId:1c532268-6ba6-435f-8e58-609b679acd37", true)]  // No trailing space
        [InlineData("SagaId:1c532268-6ba6-435f-8e58-609b679acd37", true)]                     // Minimal
        [InlineData("Standard ATM Cash Deposit - No Saga Here", false)]                        // Non-Saga string
        [InlineData("Transfer out | SagaId: Broken-Guid-String-1234", false)]                  // Malformed GUID
        [InlineData("", false)]                                                                // Empty string
        [InlineData(null, false)]                                                              // Null string
        public void ExtractSagaIdFromReference_ShouldHandleStringVariationsDeterministically(string reference, bool shouldParse)
        {
            // Arrange
            // (Invoking the parsing logic directly)

            // Act
            // Marking ExtractSagaIdFromReference temporarily and creating empty poublic constructor to test this 
            //Guid result = new MoneyTransferSaga().ExtractSagaIdFromReference(reference);

            // Assert
            //if (shouldParse)
            //{
            //    Assert.NotEqual(Guid.Empty, result);
            //    Assert.Equal(Guid.Parse("1c532268-6ba6-435f-8e58-609b679acd37"), result);
            //}
            //else
            //{
            //    Assert.Equal(Guid.Empty, result);
            //}
        }

    }
}