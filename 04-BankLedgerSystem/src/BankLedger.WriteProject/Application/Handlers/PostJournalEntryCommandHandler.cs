using BankLedger.Core.Common;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.Domain.Aggregates;
using BankLedger.Domain.Exceptions;
using BankLedger.WriteProject.Application;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net;

public class PostJournalEntryCommandHandler : ICommandHandler<PostJournalEntryCommand>
{
    private readonly IEventStore _mysqlEventStore;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<PostJournalEntryCommandHandler> _logger;
    private const string StreamCategory = "JournalEntry";
    public PostJournalEntryCommandHandler(IEventStore eventStore, IMessageBus messageBus, ILogger<PostJournalEntryCommandHandler> logger)
    {
        _mysqlEventStore = eventStore;
        _messageBus = messageBus;
        _logger = logger;
    }
    public async Task HandleAsync(PostJournalEntryCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var journalEntry = new JournalEntry(command.JournalEntryId);
            journalEntry.Post(command.LedgerEntries);

            var eventsToPublish = journalEntry.UncommittedEvents.ToList();

            await _mysqlEventStore.AppendEventsAsync(command.JournalEntryId, StreamCategory, 1, journalEntry.UncommittedEvents, cancellationToken);

            journalEntry.ClearUncommittedEvents();

            foreach (var @event in eventsToPublish)
            {
                if (@event is JournalEntryPostedEvent journalEntryPostedEvent)
                    await _messageBus.PublishAsync(journalEntryPostedEvent, cancellationToken);
            }
        }
        catch (UnbalancedLedgerException ex)
        {
            _logger.LogCritical(ex, "CRITICAL FINANCIAL INVARIANT BREACH! Variance: {Imbalance}", ex.NetImbalance);

            // 2. TRANSLATE TO APPLICATION EXCEPTION FOR THE API TO SEE
            throw new ApplicationDomainException(
                title: "Transaction Processing Error",
                detail: "We encountered an unexpected internal issue while processing your transfer.",
                statusCode: (int)HttpStatusCode.InternalServerError
            );
        }
    }
}