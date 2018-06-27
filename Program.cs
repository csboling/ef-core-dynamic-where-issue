using System;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DynamicWhereAfterProjectionTranslationFailure
{
    class Book
    {
        public int Id { get; set; }
    }

    class SimpleDto
    {
        public int Pk { get; set; }
    }

    class DtoBase
    {
        public int Pk { get; set; }
    }

    class DerivedDto : DtoBase
    {
    }

    class ExampleDbContext : DbContext
    {
        public ExampleDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>();
            base.OnModelCreating(modelBuilder);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (var loggerFactory = new LoggerFactory().AddConsole())
            {
                var options = new DbContextOptionsBuilder()
                    .UseSqlite("Data Source=example.db")
                    .UseLoggerFactory(loggerFactory)
                    .Options;

                using (var dbContext = new ExampleDbContext(options))
                {
                    dbContext.Database.EnsureCreated();

                    var dbSet = dbContext.Set<Book>();
                    var book = new Book { Id = 1 };
                    dbSet.Add(book);
                    dbContext.SaveChanges();

                    CheckSimpleQuery(dbSet, x => x.Pk == 1); // OK
                    CheckDerivedQuery(dbSet, x => x.Pk == 1); // OK
                    CheckSimpleQuery(dbSet, BuildExpression<SimpleDto>(1)); // OK
                    CheckDerivedQuery(dbSet, BuildExpression<DerivedDto>(1)); // translation failure

                    dbSet.Remove(book);
                    dbContext.SaveChanges();
                }
            }
        }

        static Expression<Func<T, bool>> BuildExpression<T>(int pk)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyExpression = Expression.Property(parameter, "Pk");
            var constant = Expression.Constant(pk);
            Expression body = Expression.Equal(propertyExpression, constant);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        static void CheckSimpleQuery(
            IQueryable<Book> queryable,
            Expression<Func<SimpleDto, bool>> whereClause)
        {
            Console.WriteLine("\n\n\n\n\n");
            var query = queryable
                .Select(simple => new SimpleDto { Pk = simple.Id })
                .Where(whereClause);
            Console.WriteLine(query.Expression);
            query.ToList();
        }

        static void CheckDerivedQuery(
            IQueryable<Book> queryable,
            Expression<Func<DerivedDto, bool>> whereClause)
        {
            Console.WriteLine("\n\n\n\n\n");
            var query = queryable
                .Select(derived => new DerivedDto { Pk = derived.Id })
                .Where(whereClause);
            Console.WriteLine(query.Expression);
            query.ToList();
        }
    }
}
