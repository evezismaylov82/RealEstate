using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(p => p.Property)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByPropertyIdAsync(int propertyId)
        {
            return await _dbSet
                .Include(p => p.User)
                .Where(p => p.PropertyId == propertyId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Property)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Property)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<decimal> GetTotalPaymentsByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(p => p.UserId == userId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Property)
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
