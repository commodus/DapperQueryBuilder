using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DapperQueryBuilder.Tests
{
    public class QueryBuilderTests
    {
        IDbConnection cn;

        #region Setup
        [SetUp]
        public void Setup()
        {
            string connectionString = @"Data Source=LENOVOFLEX5\SQLEXPRESS;
                            Initial Catalog=AdventureWorks;
                            Integrated Security=True;";
            cn = new SqlConnection(connectionString);
        }
        #endregion

        string expected = @"SELECT ProductId, Name, ListPrice, Weight
FROM [Production].[Product]
WHERE [ListPrice] <= @p0 AND [Weight] <= @p1 AND [Name] LIKE @p2
ORDER BY ProductId
";

        public class Product
        {
            public int ProductId { get; set; }
            public string Name { get; set; }
        }

        int maxPrice = 1000;
        int maxWeight = 15;
        string search = "%Mountain%";

        [Test]
        public void TestFluentAPI()
        {
            int maxPrice = 1000;
            int maxWeight = 15;
            string search = "%Mountain%";

            var q = cn.QueryBuilder()
                .Select($"ProductId")
                .Select($"Name")
                .Select($"ListPrice")
                .Select($"Weight")
                .From($"[Production].[Product]")
                .Where($"[ListPrice] <= {maxPrice}")
                .Where($"[Weight] <= {maxWeight}")
                .Where($"[Name] LIKE {search}")
                .OrderBy($"ProductId")
                ;

            Assert.AreEqual(expected, q.Sql);
            Assert.That(q.Parameters.ParameterNames.Contains("p0"));
            Assert.That(q.Parameters.ParameterNames.Contains("p1"));
            Assert.That(q.Parameters.ParameterNames.Contains("p2"));
            Assert.AreEqual(q.Parameters.Get<int>("p0"), maxPrice);
            Assert.AreEqual(q.Parameters.Get<int>("p1"), maxWeight);
            Assert.AreEqual(q.Parameters.Get<string>("p2"), search);

            var products = q.Query<Product>();
            
            Assert.That(products.Any());
        }


        [Test]
        public void TestTemplateAPI()
        {

            var q = cn.QueryBuilder(
$@"SELECT ProductId, Name, ListPrice, Weight
FROM [Production].[Product]
/**where**/
ORDER BY ProductId
");
            q.Where($"[ListPrice] <= {maxPrice}");
            q.Where($"[Weight] <= {maxWeight}");
            q.Where($"[Name] LIKE {search}");

            Assert.AreEqual(expected, q.Sql);
            Assert.That(q.Parameters.ParameterNames.Contains("p0"));
            Assert.That(q.Parameters.ParameterNames.Contains("p1"));
            Assert.That(q.Parameters.ParameterNames.Contains("p2"));
            Assert.AreEqual(q.Parameters.Get<int>("p0"), maxPrice);
            Assert.AreEqual(q.Parameters.Get<int>("p1"), maxWeight);
            Assert.AreEqual(q.Parameters.Get<string>("p2"), search);

            var products = q.Query<Product>();

            Assert.That(products.Any());
        }

        public class ProductCategories
        {
            public string Category { get; set; }
            public string Subcategory { get; set; }
            public string Name { get; set; }
            public string ProductNumber { get; set; }
        }

        [Test]
        public void JoinsTest()
        {
            var categories = new string[] { "Components", "Clothing", "Acessories" };
            var q = cn.QueryBuilder()
                .SelectDistinct($"c.[Name] as [Category], sc.[Name] as [Subcategory], p.[Name], p.[ProductNumber]")
                .From($"[Production].[Product] p")
                .From($"INNER JOIN [Production].[ProductSubcategory] sc ON p.[ProductSubcategoryID]=sc.[ProductSubcategoryID]")
                .From($"INNER JOIN [Production].[ProductCategory] c ON sc.[ProductCategoryID]=c.[ProductCategoryID]")
                .Where($"c.[Name] IN {categories}");
            var prods = q.Query<ProductCategories>();
        }


        [Test]
        public void TestAndOr()
        {
            int maxPrice = 1000;
            int maxWeight = 15;
            string search = "%Mountain%";

            string expected = @"SELECT ProductId, Name, ListPrice, Weight
FROM [Production].[Product]
WHERE [ListPrice] <= @p0 AND ([Weight] <= @p1 OR [Name] LIKE @p2)
ORDER BY ProductId
";

            var q = cn.QueryBuilder()
                .Select($"ProductId")
                .Select($"Name")
                .Select($"ListPrice")
                .Select($"Weight")
                .From($"[Production].[Product]")
                .Where($"[ListPrice] <= {maxPrice}")
                .Where(new Filters(Filters.FiltersType.OR, 
                    $"[Weight] <= {maxWeight}",
                    $"[Name] LIKE {search}"
                ))
                .OrderBy($"ProductId")
                ;

            Assert.AreEqual(expected, q.Sql);
            Assert.That(q.Parameters.ParameterNames.Contains("p0"));
            Assert.That(q.Parameters.ParameterNames.Contains("p1"));
            Assert.That(q.Parameters.ParameterNames.Contains("p2"));
            Assert.AreEqual(q.Parameters.Get<int>("p0"), maxPrice);
            Assert.AreEqual(q.Parameters.Get<int>("p1"), maxWeight);
            Assert.AreEqual(q.Parameters.Get<string>("p2"), search);

            var products = q.Query<Product>();

            Assert.That(products.Any());
        }

        [Test]
        public void TestAndOr2()
        {
            int minPrice = 200;
            int maxPrice = 1000;
            int maxWeight = 15;
            string search = "%Mountain%";

            string expected = @"SELECT ProductId, Name, ListPrice, Weight
FROM [Production].[Product]
WHERE ([ListPrice] >= @p0 AND [ListPrice] <= @p1) AND ([Weight] <= @p2 OR [Name] LIKE @p3)
";

            var q = cn.QueryBuilder()
                .Select($"ProductId, Name, ListPrice, Weight")
                .From($"[Production].[Product]")
                .Where(new Filters(
                    $"[ListPrice] >= {minPrice}",
                    $"[ListPrice] <= {maxPrice}"
                ))
                .Where(new Filters(Filters.FiltersType.OR,
                    $"[Weight] <= {maxWeight}",
                    $"[Name] LIKE {search}"
                ));

            Assert.AreEqual(expected, q.Sql);
            Assert.That(q.Parameters.ParameterNames.Contains("p0"));
            Assert.That(q.Parameters.ParameterNames.Contains("p1"));
            Assert.That(q.Parameters.ParameterNames.Contains("p2"));
            Assert.That(q.Parameters.ParameterNames.Contains("p3"));
            Assert.AreEqual(q.Parameters.Get<int>("p0"), minPrice);
            Assert.AreEqual(q.Parameters.Get<int>("p1"), maxPrice);
            Assert.AreEqual(q.Parameters.Get<int>("p2"), maxWeight);
            Assert.AreEqual(q.Parameters.Get<string>("p3"), search);

            var products = q.Query<Product>();
        }

        [Test]
        public void TestDetachedFilters()
        {
            int minPrice = 200;
            int maxPrice = 1000;
            int maxWeight = 15;
            string search = "%Mountain%";

            var filters = new Filters(Filters.FiltersType.AND);
            filters.Add(new Filters()
            {
                new Filter($"[ListPrice] >= {minPrice}"),
                new Filter($"[ListPrice] <= {maxPrice}")
            });
            filters.Add(new Filters(Filters.FiltersType.OR)
            {
                new Filter($"[Weight] <= {maxWeight}"),
                new Filter($"[Name] LIKE {search}")
            });
            
            Dapper.DynamicParameters parms = new Dapper.DynamicParameters();
            string where = filters.BuildFilters(parms);

            Assert.AreEqual(@"WHERE ([ListPrice] >= @p0 AND [ListPrice] <= @p1) AND ([Weight] <= @p2 OR [Name] LIKE @p3)", where);
        }

        [Test]
        public void GenericTest()
        {
            string productName = "%mountain%";
            int subCategoryId = 12;
            var query = cn
                .CommandBuilder<Product>($@"SELECT * FROM [Production].[Product]")
                .AppendLine($"WHERE")
                .AppendLine($"[Name] LIKE {productName}")
                .AppendLine($"AND [ProductSubcategoryID] = {subCategoryId}")
                .AppendLine($"ORDER BY [ProductId]");
            Assert.AreEqual(
@"SELECT * FROM [Production].[Product]
WHERE
[Name] LIKE @p0
AND [ProductSubcategoryID] = @p1
ORDER BY [ProductId]", query.Sql);

            var products = query.Query();
        }


    }
}
