using System.Text.RegularExpressions;

public static partial class Parser
{
    public static IEnumerable<Customer> ParseCustomers(string customersString)
    {
        var lines = customersString.Split(Environment.NewLine);
        for (int i = 0; i < lines.Length; i++)
        {
            if (CustomerRegex.IsMatch(lines[i]))
            {
                var customerMatch = CustomerRegex.Match(lines[i]);
                var customer = new Customer(default, customerMatch.Groups["Name"].Value, customerMatch.Groups["IsoCode"].Value, customerMatch.Groups["Region"].Value);

                while (++i < lines.Length && lines[i].StartsWith("CUS") == false)
                {
                    if (OrderHeaderRegex.IsMatch(lines[i]))
                    {
                        var headerMatch = OrderHeaderRegex.Match(lines[i]);
                        var header = new OrderHeader(default, default, customer, DateOnly.Parse(headerMatch.Groups["Date"].Value), headerMatch.Groups["DelivaryIsoCode"].Value, headerMatch.Groups["Incoterm"].Value, headerMatch.Groups["PaymentTerms"].Value);
                        customer.Orders.Add(header);

                        while (++i < lines.Length && lines[i].StartsWith("OH") == false)
                        {
                            if (OrderLineRegex.IsMatch(lines[i]))
                            {
                                var lineMatch = OrderLineRegex.Match(lines[i]);
                                var line = new OrderLine(default, default, header, lineMatch.Groups["ProductCode"].Value, int.Parse(lineMatch.Groups["Quantity"].Value), decimal.Parse(lineMatch.Groups["UnitPrice"].Value));
                                header.OrderLines.Add(line);
                            }
                        }

                        i--;
                    }
                }

                yield return customer;
                i--;
            }
        }
    }

    private static readonly Regex CustomerRegex = CustomerRegexPartial();
    private static readonly Regex OrderHeaderRegex = OrderHeaderRegexPartial();
    private static readonly Regex OrderLineRegex = OrderLineRegexPartial();


    [GeneratedRegex(@"^CUS\|(?<Name>.+)\|(?<IsoCode>.+)\|(?<Region>.+)$")]
    private static partial Regex CustomerRegexPartial();
    [GeneratedRegex(@"^OH\|(?<Date>\d\d\d\d\-\d\d\-\d\d)\|(?<DelivaryIsoCode>.+)\|(?<Incoterm>.+)\|(?<PaymentTerms>.+)$")]
    private static partial Regex OrderHeaderRegexPartial();
    [GeneratedRegex(@"^OL\|(?<ProductCode>.+)\|(?<Quantity>\d+)\|(?<UnitPrice>\d+\.\d\d)$")]
    private static partial Regex OrderLineRegexPartial();
}