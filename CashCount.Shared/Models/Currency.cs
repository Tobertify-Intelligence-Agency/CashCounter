namespace CashCount.Shared.Models;

public class Currency
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public List<Denomination> Banknotes { get; set; } = new();
    public List<Denomination> Coins { get; set; } = new();

    public static List<Currency> GetAllCurrencies() => new()
    {
        new Currency
        {
            Code = "EUR",
            Name = "Euro",
            Symbol = "\u20ac",
            Banknotes = new List<Denomination>
            {
                new(500, "500 \u20ac"),
                new(200, "200 \u20ac"),
                new(100, "100 \u20ac"),
                new(50, "50 \u20ac"),
                new(20, "20 \u20ac"),
                new(10, "10 \u20ac"),
                new(5, "5 \u20ac")
            },
            Coins = new List<Denomination>
            {
                new(2, "2 \u20ac"),
                new(1, "1 \u20ac"),
                new(0.50m, "50 Cent"),
                new(0.20m, "20 Cent"),
                new(0.10m, "10 Cent"),
                new(0.05m, "5 Cent"),
                new(0.02m, "2 Cent"),
                new(0.01m, "1 Cent")
            }
        },
        new Currency
        {
            Code = "USD",
            Name = "US Dollar",
            Symbol = "$",
            Banknotes = new List<Denomination>
            {
                new(100, "$100"),
                new(50, "$50"),
                new(20, "$20"),
                new(10, "$10"),
                new(5, "$5"),
                new(2, "$2"),
                new(1, "$1")
            },
            Coins = new List<Denomination>
            {
                new(1, "$1 Coin"),
                new(0.50m, "Half Dollar"),
                new(0.25m, "Quarter"),
                new(0.10m, "Dime"),
                new(0.05m, "Nickel"),
                new(0.01m, "Penny")
            }
        },
        new Currency
        {
            Code = "GBP",
            Name = "British Pound",
            Symbol = "\u00a3",
            Banknotes = new List<Denomination>
            {
                new(100, "\u00a3100"),
                new(50, "\u00a350"),
                new(20, "\u00a320"),
                new(10, "\u00a310"),
                new(5, "\u00a35")
            },
            Coins = new List<Denomination>
            {
                new(2, "\u00a32"),
                new(1, "\u00a31"),
                new(0.50m, "50p"),
                new(0.20m, "20p"),
                new(0.10m, "10p"),
                new(0.05m, "5p"),
                new(0.02m, "2p"),
                new(0.01m, "1p")
            }
        },
        new Currency
        {
            Code = "CHF",
            Name = "Swiss Franc",
            Symbol = "CHF",
            Banknotes = new List<Denomination>
            {
                new(1000, "1000 CHF"),
                new(200, "200 CHF"),
                new(100, "100 CHF"),
                new(50, "50 CHF"),
                new(20, "20 CHF"),
                new(10, "10 CHF")
            },
            Coins = new List<Denomination>
            {
                new(5, "5 CHF"),
                new(2, "2 CHF"),
                new(1, "1 CHF"),
                new(0.50m, "50 Rp."),
                new(0.20m, "20 Rp."),
                new(0.10m, "10 Rp."),
                new(0.05m, "5 Rp.")
            }
        },
        new Currency
        {
            Code = "CAD",
            Name = "Canadian Dollar",
            Symbol = "C$",
            Banknotes = new List<Denomination>
            {
                new(100, "$100"),
                new(50, "$50"),
                new(20, "$20"),
                new(10, "$10"),
                new(5, "$5")
            },
            Coins = new List<Denomination>
            {
                new(2, "Toonie ($2)"),
                new(1, "Loonie ($1)"),
                new(0.25m, "Quarter"),
                new(0.10m, "Dime"),
                new(0.05m, "Nickel")
            }
        },
        new Currency
        {
            Code = "AUD",
            Name = "Australian Dollar",
            Symbol = "A$",
            Banknotes = new List<Denomination>
            {
                new(100, "$100"),
                new(50, "$50"),
                new(20, "$20"),
                new(10, "$10"),
                new(5, "$5")
            },
            Coins = new List<Denomination>
            {
                new(2, "$2"),
                new(1, "$1"),
                new(0.50m, "50c"),
                new(0.20m, "20c"),
                new(0.10m, "10c"),
                new(0.05m, "5c")
            }
        }
    };
}

public class Denomination
{
    public decimal Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Total => Value * Quantity;

    public Denomination() { }

    public Denomination(decimal value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
        Quantity = 0;
    }
}
