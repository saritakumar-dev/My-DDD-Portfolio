using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.Domain.ValueObjects
{
    public record Message
    {
        public string Content {  get; private set; }
        public UserId AuthorId { get; private set; }
        public DateTime SentAt { get; private set; }

        private Message() { }
        public Message(string content,
                        UserId authorId,
                        DateTime sentAt)
        {

            if (string.IsNullOrWhiteSpace(content)) { 
                throw new ArgumentNullException("Message content cannot be empty."); 
            }

            if (Guid.Empty == authorId.Value) throw new ArgumentNullException("Message author cannot be empty");

            if (sentAt > DateTime.UtcNow)
            {
                throw new ArgumentException("Message cannot be sent in the future.");
            }
            this.Content = content;
            this.AuthorId = authorId;
            this.SentAt = sentAt;
        }
    }
}
