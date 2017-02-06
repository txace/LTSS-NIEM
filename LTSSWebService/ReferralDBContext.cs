using Application.PropertyFieldInfoExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Validation;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LTSSWebService.Models
{
    public enum EventEntityType
    {
        ContactEntity = 1,
        Organization = 2,
        Location = 3
    }

    public interface IEntityConfiguration
    {
        void AddConfiguration(ConfigurationRegistrar registrar);
    }

    public static class PrimaryKeyHelper
    {
        private static readonly IDictionary<Type, PropertyInfo> PrimaryKeyStorage = new Dictionary<Type, PropertyInfo>();

        private static readonly object PrimaryKeyStorageLock = new object();

        public static int PrimaryKeyValue(this object obj)
        {
            return (int)PropertyInfoForKey(obj.GetType()).GetValue(obj);
        }

        public static void SetPrimaryKeyValue(this object obj, object value)
        {
            PropertyInfoForKey(obj.GetType()).SetValue(obj, value);
        }

        private static PropertyInfo PropertyInfoForKey(this Type type)
        {
            PropertyInfo result;
            if (PrimaryKeyStorage.TryGetValue(type, out result))
                return result;

            lock (PrimaryKeyStorageLock)
            {
                if (PrimaryKeyStorage.TryGetValue(type, out result))
                    return result;

                result = type.GetProperties().Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(KeyAttribute))).FirstOrDefault();
                PrimaryKeyStorage.Add(type, result);
                return result;
            }
        }
    }

    public class ContextConfiguration
    {
        [ImportMany(typeof(IEntityConfiguration))]
        public IEnumerable<IEntityConfiguration> Configurations
        {
            get;
            set;
        }
    }

    public class ReferralDbContext : DbContext, IDisposable
    {
        // Since the base class is IDisposable, we don't need to implement Dispose()
        private bool _disposed = false;

        private bool _HasUnfinalizedTransaction = false;

        static ReferralDbContext()
        {
            Database.SetInitializer<ReferralDbContext>(null);
        }

        public ReferralDbContext() : this(0, null, 200)
        {
        }

        public ReferralDbContext(int requestID) : this(requestID, null, 200)
        {
        }

        public ReferralDbContext(int requestID, string connectionString = null, int commandTimeout = 200)
        {
            this.RequestID = requestID;
            this.Database.Connection.ConnectionString = connectionString ?? ConfigurationManager.AppSettings["WebDevConnectionString"];
            this.Database.CommandTimeout = commandTimeout;
        }

        public DbSet<ContactEntity> ContactEntity { get; set; }

        public DbSet<Email> Email { get; set; }

        public DbSet<Enrollment> Enrollment { get; set; }

        public DbSet<EntityContactEntity> EntityContactEntity { get; set; }

        public DbSet<EntityEvent> EntityEvent { get; set; }

        public DbSet<Location> Location { get; set; }

        public DbSet<Organization> Organization { get; set; }

        public DbSet<Referral> Referral { get; set; }

        public DbSet<Request> Request { get; set; }

        public int RequestID { get; set; }

        public DbSet<Screening> Screening { get; set; }

        private DbContextTransaction _Transaction { get; set; }

        public static ReferralDbContext Create(int requestID)
        {
            return new ReferralDbContext(requestID);
        }

        public void Commit()
        {
            if (_Transaction != null)
                _Transaction.Commit();
            _Transaction = null;
            _HasUnfinalizedTransaction = false;
        }

        public void CreateOrContinueTransaction()
        {
            _Transaction = _Transaction ?? Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            _HasUnfinalizedTransaction = true;
        }

        public int Execute(string sql, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return 1;
            return this.Database.ExecuteSqlCommand(sql, parameters);
        }

        public ConfiguredTaskAwaitable<int> ExecuteAsync(string sql, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return Task.Run(() => 1).ConfigureAwait(false);
            return this.Database.ExecuteSqlCommandAsync(sql, parameters).ConfigureAwait(false);
        }

        public List<T> Fetch<T>(string sql, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return new List<T>();
            return this.Database.SqlQuery<T>(sql, parameters).ToList();
        }

        public ConfiguredTaskAwaitable<List<T>> FetchAsync<T>(string sql, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return Task.Run(() => new List<T>()).ConfigureAwait(false);
            return this.Database.SqlQuery<T>(sql, parameters).ToListAsync().ConfigureAwait(false);
        }

        public void Rollback()
        {
            if (_Transaction != null)
                _Transaction.Rollback();
            _Transaction = null;
            _HasUnfinalizedTransaction = false;
        }

        public override int SaveChanges()
        {
            var EntityEvents = new List<EntityEvent>();
            var ObjectStateManager = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager;
            var changedEntities = this.ChangeTracker.Entries().Where(x => x.State == EntityState.Modified).ToList();
            var createdEntities = this.ChangeTracker.Entries().Where(x => x.State == EntityState.Added).ToList();
            foreach (var entity in changedEntities)
            {
                var ObjectStateEntry = ObjectStateManager.GetObjectStateEntry(entity.Entity);
                Models.EventEntityType EventEntityType;
                if (!Enum.TryParse(ObjectStateEntry.EntitySet.Name, out EventEntityType))
                    continue;

                var Diff = entity.CurrentValues.PropertyNames.Select(x => new
                {
                    CurrentValue = entity.CurrentValues[x]?.ToString(),
                    OriginalValue = entity.OriginalValues[x]?.ToString(),
                    Name = x
                })
                .Where(x => x.CurrentValue != x.OriginalValue)
                .ToList();

                if (Diff.Count == 0)
                    continue;

                EntityEvents.Add(new EntityEvent
                {
                    EnittyID = (int)ObjectStateEntry.EntityKey.EntityKeyValues[0].Value,
                    EntityType = EventEntityType,
                    Diff = JsonConvert.SerializeObject(Diff.ToDictionary(x => x.Name, x => x.CurrentValue)),
                    RequestID = this.RequestID
                });
            }

            if (EntityEvents.Count > 0)
                this.EntityEvent.AddRange(EntityEvents);

            BaseSaveChanges();

            EntityEvents = new List<EntityEvent>();
            foreach (var entity in createdEntities)
            {
                Models.EventEntityType EventEntityType;
                var EntityType = entity.Entity.GetType();
                if (!Enum.TryParse(EntityType.Name, out EventEntityType))
                    continue;

                EntityEvents.Add(new EntityEvent
                {
                    EnittyID = PrimaryKeyHelper.PrimaryKeyValue(entity.Entity),
                    EntityType = EventEntityType,
                    Diff = JsonConvert.SerializeObject(EntityType.DBPrimativePropsAndFields().ToDictionary(x => x.Name(), x => x.GetStringValue(entity.Entity))),
                    RequestID = this.RequestID
                });
            }

            if (EntityEvents.Count > 0)
                this.EntityEvent.AddRange(EntityEvents);

            return BaseSaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) // for idempotence
                return;

            if (disposing)  // free managed resources
            {
                if (_HasUnfinalizedTransaction && _Transaction != null)
                    _Transaction.Rollback();
            }

            // free unmanaged resources
            _disposed = true;

            base.Dispose(disposing);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.AddFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<PluralizingEntitySetNameConvention>();
        }

        private int BaseSaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                                    .SelectMany(x => x.ValidationErrors)
                                    .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                using (var context = ReferralDbContext.Create(0))
                {
                    context.Request.Where(x => x.RequestID == this.RequestID).First().Exception = fullErrorMessage;
                    context.SaveChanges();
                }

                throw;
            }
        }
    }
}