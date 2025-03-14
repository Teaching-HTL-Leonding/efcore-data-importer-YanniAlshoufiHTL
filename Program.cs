using System.Runtime.CompilerServices;
using efcore_data_importer_YanniAlshoufiHTL;
using Microsoft.EntityFrameworkCore;

var factory = new ApplicationDataContextFactory();
var context = factory.CreateDbContext([]);

// context.RemoveRange(context.Customers);

var customer = new Customer(0, "Hogwards", "UK", "Scotland");

Console.WriteLine($"\nCustomer ID: {customer.ID}");
Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(customer)}");
Console.WriteLine($"Customer tracking state: {context.Entry(customer).State}"); // Detached -- "No idea about this POCO!" (POCO = Plain old CLR object)
context.Customers.Add(customer);
Console.WriteLine($"\nCustomer ID (after Add): {customer.ID}");
Console.WriteLine($"Customer Hashcode (after Add): {RuntimeHelpers.GetHashCode(customer)}");
Console.WriteLine($"Customer tracking state: {context.Entry(customer).State}"); // Added -- "It has been added"
await context.SaveChangesAsync();
Console.WriteLine($"\nCustomer ID (after SaveChanges): {customer.ID}");
Console.WriteLine($"Customer Hashcode (after SaveChanges): {RuntimeHelpers.GetHashCode(customer)}");
Console.WriteLine($"Customer tracking state: {context.Entry(customer).State}"); // Unchanged -- "Synconized with the database"

var reReadCustomer = await context.Customers.FindAsync(customer.ID);
Console.WriteLine($"\nRe-Read Customer ID: {reReadCustomer?.ID}");
Console.WriteLine($"Re-Read Customer Hashcode: {RuntimeHelpers.GetHashCode(reReadCustomer)}");
// If the system has a tracked reference it will reassign to the same reference so that everything is consistent
// This also applies for complex queries unless there is a select where only a subset of the columns is selected

customer.CompanyName = "Foo";
Console.WriteLine($"\nCustomer tracking state: {context.Entry(customer).State}"); // Modified -- "Something changed here... I have to update these changes"
Console.WriteLine($"ReReadCustomer Name: {reReadCustomer!.CompanyName}");

// POSSIBLE (for writing)
// await context.Database.ExecuteSqlAsync($"INSERT INTO ...");

// POSSIBLE (for reading)
// using (var connection = context.Database.GetDbConnection())
// {
//     await connection.OpenAsync();
//     var command = connection.CreateCommand();
//     command.CommandText = "SELECT * FROM Customers";
//     using var reader = await command.ExecuteReaderAsync();
//     while (await reader.ReadAsync())
//     {
//         Console.WriteLine(reader["CompanyName"]);
//     }
// }

// POSSIBLE (for reading - does use the same reference and fills the object)
// context.Customers.FromSql(...)

context.Customers.Remove(customer);
Console.WriteLine($"\nCustomer tracking state: {context.Entry(customer).State}"); // Deleted -- "I have to delete this object"
await context.SaveChangesAsync(); // Call once at the end if possible!! Otherwise it will be counted as a severe mistake on the exam

// context.Customers.AddRange([ new Customer(0, "Hogwards", "UK", "Scotland"), new Customer(0, "Hogwards", "UK", "Scotland") ]);
// Bulk addition!!
//
// There is basically no known database that does actual async adding, so here, it is allowed to just not use async.
// It is not a mistake though if you DO use async, one can just always use async.




#region Demo Saving Changes
customer.CompanyName += " School of Witchcraft and Wizardry";
await context.SaveChangesAsync();

/*
Data Context stores a complete list of all objects that have been added to it (as references).
It basically also copies a deep copy of all objects and on SaveChangesAsync() it compares all values
of the old objects with the new ones.
==> Heap allocations!!
*/
#endregion

#region Demo Hierarchies

// Adding hierarchies

var customer1 = new Customer(0, "Hogwards", "UK", "Scotland");
customer1.Orders.Add(new OrderHeader(0, 0, customer1, DateOnly.FromDateTime(DateTime.Now), "UK", "FOB", "Due on receipt"));

context.Customers.Add(customer1);
// context.OrderHeaders.Add(customer.Orders.First()); -- this is a no-op (no operation) - acceptable on exam
await context.SaveChangesAsync();
// context.OrderHeaders.Add(customer.Orders.First()); -- this is bad - NOT acceptable on exam

Console.WriteLine($"\nCustomer1 ID: {customer1.ID}");
Console.WriteLine($"Customer1 ID from Order Header: {customer1.Orders.First().CustomerID}");

var customers = await context.Customers.ToListAsync();

foreach (var c in customers)
{
    Console.WriteLine($"Customer {c.ID} has {c.Orders.Count} orders"); ;

    if (c.Orders.Count > 0)
    {
        Console.WriteLine($"First order of customer {c.ID} was placed on {c.Orders[0].OrderDate}"); // this works because we know about the customer
        // If the customer wasn't in-memory, this would through an exception
        // SOLUTION: USE INCLUDE
        /*
            var customers = await context.Customers.Include(c => c.OrderLines).ToListAsync();
        */
    }
}
#endregion

// var c1 = new Customer(0, "Hogwards", "XYZ", "Scotland");
// var c2 = new Customer(0, "Hogwards", "XY", "Scotland");
// context.Customers.AddRange(c1, c2); // Fails to add either as the transaction is not closed until SaveChanges is called
// await context.SaveChangesAsync(); // ERROR, CHECK CONSTRAINT
/*
We only need to transactions when doing multiple SaveChange calls
*/

/*!!!!!*/
await /*!!!!! can be used because of IAsyncDisposible, not only IDisposable*/ using (var transaction = await context.Database.BeginTransactionAsync())
{
    Customer? c1 = null;

    try
    {
        await context.OrderHeaders.ExecuteDeleteAsync(); // own transaction
        await context.Customers.ExecuteDeleteAsync(); // own transaction

        c1 = new Customer(0, "Hogwards", "XYZ", "Scotland");
        var c2 = new Customer(0, "Hogwards", "XY", "Scotland");
        context.Customers.AddRange(c1, c2); // Fails to add either as the transaction is not closed until SaveChanges is called
        await context.SaveChangesAsync(); // ERROR, CHECK CONSTRAINT
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

    Console.WriteLine($"Customer 1: {c1?.ID}");
}
