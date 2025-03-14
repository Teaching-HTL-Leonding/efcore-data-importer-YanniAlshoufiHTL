using efcore_data_importer_YanniAlshoufiHTL;
using Microsoft.EntityFrameworkCore;

var dataText = await File.ReadAllTextAsync("data.txt");
var customers = Parser.ParseCustomers(dataText).ToList();


{
    // Importer

    var factory = new ApplicationDataContextFactory();
    using var context = factory.CreateDbContext([]);

    await context.Customers.ExecuteDeleteAsync();

    foreach (var customer in customers)
    {
        try
        {
            await context.AddAsync(customer);
            await context.SaveChangesAsync();
        }
        catch (Exception e) { Console.WriteLine($"INFO: Customer {customer.CompanyName} cannot be added: {e.Message}"); }
    }
}
