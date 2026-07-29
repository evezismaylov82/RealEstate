using Microsoft.EntityFrameworkCore.Storage;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        private IDbContextTransaction? _transaction;

        private IUserRepository? _users;
        private IPropertyRepository? _properties;
        private IPropertyImageRepository? _propertyImages;
        private IFavoriteRepository? _favorites;
        private IPaymentRepository? _payments;
        private IContactMessageRepository? _contactMessages;
        private IRefreshTokenRepository? _refreshTokens;
        private IRoleRepository? _roles;
        private IPermissionRepository? _permissions;
        private ICategoryRepository? _categories;
        private IBookingRepository? _bookings;
        private IReviewRepository? _reviews;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users
        {
            get
            {
                _users ??= new UserRepository(_context);
                return _users;
            }
        }

        public IPropertyRepository Properties
        {
            get
            {
                _properties ??= new PropertyRepository(_context);
                return _properties;
            }
        }

        public IPropertyImageRepository PropertyImages
        {
            get
            {
                _propertyImages ??= new PropertyImageRepository(_context);
                return _propertyImages;
            }
        }

        public IFavoriteRepository Favorites
        {
            get
            {
                _favorites ??= new FavoriteRepository(_context);
                return _favorites;
            }
        }

        public IPaymentRepository Payments
        {
            get
            {
                _payments ??= new PaymentRepository(_context);
                return _payments;
            }
        }

        public IContactMessageRepository ContactMessages
        {
            get
            {
                _contactMessages ??= new ContactMessageRepository(_context);
                return _contactMessages;
            }
        }

        public IRefreshTokenRepository RefreshTokens
        {
            get
            {
                _refreshTokens ??= new RefreshTokenRepository(_context);
                return _refreshTokens;
            }
        }

        public IRoleRepository Roles
        {
            get
            {
                _roles ??= new RoleRepository(_context);
                return _roles;
            }
        }

        public IPermissionRepository Permissions
        {
            get
            {
                _permissions ??= new PermissionRepository(_context);
                return _permissions;
            }
        }

        public ICategoryRepository Categories
        {
            get
            {
                _categories ??= new CategoryRepository(_context);
                return _categories;
            }
        }

        public IBookingRepository Bookings
        {
            get
            {
                _bookings ??= new BookingRepository(_context);
                return _bookings;
            }
        }

        public IReviewRepository Reviews
        {
            get
            {
                _reviews ??= new ReviewRepository(_context);
                return _reviews;
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync(CancellationToken.None);
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started.");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit.");
            }

            try
            {
                await SaveChangesAsync();

                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                return;
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();

            _context.Dispose();
        }
    }
}
