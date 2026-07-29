using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(int userId);
        Task<IEnumerable<Payment>> GetPaymentsByPropertyIdAsync(int propertyId);
        Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);

        Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId);

        Task<decimal> GetTotalPaymentsByUserIdAsync(int userId);

        Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate);
    }
}
