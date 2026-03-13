using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.Domain.ValueObjects
{
    public record UserId
    {
        public Guid Value {  get; init; }
        public static UserId New() =>  new(Guid.NewGuid());

        public UserId(Guid userId)
        {
            if(userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.");
            Value = userId;
        }
    }
}
