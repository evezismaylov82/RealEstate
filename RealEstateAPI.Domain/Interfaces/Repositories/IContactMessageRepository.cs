using RealEstateAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
    public interface IContactMessageRepository : IGenericRepository<ContactMessage>
    {
        Task<IEnumerable<ContactMessage>> GetUnreadMessageAsync();
        Task<IEnumerable<ContactMessage>> GetMessagesByUserIdAsync(int userId);
        Task<IEnumerable<ContactMessage>> GetMessagesByPropertyIdAsync(int propertyId);
        Task<IEnumerable<ContactMessage>> GetUnrepliedMessageAsync();
        Task MarkAsReadAsync(int messageId);
        Task ReplyToMessageAsync(int messageId, string replyMessage, int repliedByUser);
    }
}
