using BankLedger.Core.Common.Events;
using BankLedger.Domain.Aggregates;
using BankLedger.Domain.Exceptions;
using FluentAssertions;

namespace BankLedger.Domain.Tests
{
    public class JournalEntryTests
    {
        [Fact]
        public void Post_ShouldSucceedAndRaiseEvent_WhenLedgerEntriesAreBalanced()
        {
            // Arrange
            var journalEntryId = Guid.NewGuid();
            var sut = new JournalEntry(journalEntryId);

            var entries = new List<LedgerEntry>
                                {
                                    new(Guid.NewGuid(), -100.00m, "Debit Entry"),
                                    new(Guid.NewGuid(), 100.00m, "Credit Entry")
                                };

            // Act
            sut.Post(entries);

            // Assert
            sut.IsPosted.Should().BeTrue();
            sut.Entries.Should().HaveCount(2);

            // Verify Event Sourcing Stream State Mutation
            sut.UncommittedEvents.Should().ContainSingle();
            var raisedEvent = sut.UncommittedEvents.First().Should().BeOfType<JournalEntryPostedEvent>().Subject;
            raisedEvent.JournalEntryId.Should().Be(journalEntryId);
        }

        [Fact]
        public void Post_ShouldThrowUnbalancedLedgerException_WhenTotalSumDoesNotEqualZero()
        {
            // Arrange
            var sut = new JournalEntry(Guid.NewGuid());
            var unbalancedEntries = new List<LedgerEntry>
                                        {
                                            new(Guid.NewGuid(), -100.00m, "Debit Entry"),
                                            new(Guid.NewGuid(), 95.00m, "Unbalanced Credit Entry") // Missing €5
                                        };

            // Act
            Action act = () => sut.Post(unbalancedEntries);

            // Assert
            act.Should().Throw<UnbalancedLedgerException>();
            sut.IsPosted.Should().BeFalse();
            sut.UncommittedEvents.Should().BeEmpty();
        }

        [Fact]
        public void Post_ShouldThrowInvalidOperationException_WhenEntryIsAlreadyPosted()
        {
            // Arrange
            var sut = new JournalEntry(Guid.NewGuid());
            var initialEntries = new List<LedgerEntry>
                                    {
                                        new(Guid.NewGuid(), -50.00m, "Debit"),
                                        new(Guid.NewGuid(), 50.00m, "Credit")
                                    };
            sut.Post(initialEntries); // First post successful

            // Act
            Action act = () => sut.Post(initialEntries);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*already posted*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Post_ShouldThrowArgumentException_WhenPayloadContainsLessThanTwoEntries(int count)
        {
            // Arrange
            var sut = new JournalEntry(Guid.NewGuid());
            var singleEntryList = Enumerable.Range(0, count)
                .Select(_ => new LedgerEntry(Guid.NewGuid(), 0.00m, "Invalid"))
                .ToList();

            // Act
            Action act = () => sut.Post(singleEntryList);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("A valid double-entry transaction requires at least two ledger lines.");
        }
    }
}
