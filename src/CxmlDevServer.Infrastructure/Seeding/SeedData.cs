using Cxml.Protocol.Documents.PunchOut;
using CxmlDevServer.Core.Domain;

namespace CxmlDevServer.Infrastructure.Seeding;

/// <summary>The demo data created in an empty database.</summary>
public static class SeedData
{
    /// <summary>The supplier identities: the default demo DUNS and the DUNS used by the specification's OrderRequest example.</summary>
    public static IReadOnlyList<SupplierIdentity> SupplierIdentities() =>
    [
        new() { Domain = "DUNS", Identity = "123456789", Name = "Demo Supplier", IsDefault = true },
        new() { Domain = "DUNS", Identity = "942888711", Name = "Specification example supplier" },
    ];

    /// <summary>The demo trading partners.</summary>
    public static IReadOnlyList<TradingPartner> TradingPartners() =>
    [
        new()
        {
            Name = "Demo Buyer",
            FromDomain = "NetworkId",
            FromIdentity = "AN01000000001",
            SenderDomain = "NetworkId",
            SenderIdentity = "AN01000000001",
            SharedSecret = "devsecret",
            AuthMode = AuthMode.SharedSecret,
            ValidationMode = ValidationMode.Lenient,
            PunchOutPostEncoding = PunchOutPostEncoding.Base64,
            OperationAllowed = PunchOutOperation.Edit,
            Notes = "Default partner for trying the server out. Use these credentials in your procurement system.",
        },
        new()
        {
            Name = "Acme (spec example)",
            FromDomain = "AribaNetworkUserId",
            FromIdentity = "admin@acme.com",
            SenderDomain = "AribaNetworkUserId",
            SenderIdentity = "sysadmin@ariba.com",
            SharedSecret = "abracadabra",
            AuthMode = AuthMode.SharedSecret,
            ValidationMode = ValidationMode.Lenient,
            PunchOutPostEncoding = PunchOutPostEncoding.UrlEncoded,
            OperationAllowed = PunchOutOperation.Create,
            Notes = "Matches the credentials of cXML-spec/Examples/0-OrderRequest.xml, so the example posts as-is.",
        },
    ];

    /// <summary>A disabled example scenario.</summary>
    public static IReadOnlyList<ResponseScenario> Scenarios() =>
    [
        new()
        {
            Name = "Sample: fail the first 3 OrderRequests with 500",
            Enabled = false,
            DocumentType = "OrderRequest",
            Priority = 10,
            RemainingUses = 3,
            Behavior = ScenarioBehavior.Respond,
            StatusCode = 500,
            StatusText = "Internal Server Error",
            StatusDetail = "Simulated failure for retry testing.",
        },
    ];

    /// <summary>About twenty-five realistic catalog items in five categories.</summary>
    public static IReadOnlyList<CatalogItem> CatalogItems() =>
    [
        // Laptops
        Item("LAP-1001", "Contoso ProBook 14", "14-inch business laptop, Intel Core i5, 16 GB RAM, 512 GB SSD, Windows 11 Pro", 899.00m, "EA", "43211503", "Contoso", "PB14-I5-16-512", 5, "Laptops"),
        Item("LAP-1002", "Contoso ProBook 16", "16-inch business laptop, Intel Core i7, 32 GB RAM, 1 TB SSD, Windows 11 Pro", 1349.00m, "EA", "43211503", "Contoso", "PB16-I7-32-1T", 5, "Laptops"),
        Item("LAP-1003", "Fabrikam UltraSlim 13", "13-inch ultraportable, 1.1 kg, 16 GB RAM, 512 GB SSD, 14 hour battery", 1199.00m, "EA", "43211503", "Fabrikam", "US13-16-512", 7, "Laptops"),
        Item("LAP-1004", "Fabrikam Workstation 17", "17-inch mobile workstation, 64 GB RAM, 2 TB SSD, dedicated graphics", 2899.00m, "EA", "43211503", "Fabrikam", "WS17-64-2T", 14, "Laptops"),
        Item("LAP-1005", "Litware TabPro 11", "11-inch tablet with keyboard cover, 8 GB RAM, 256 GB storage", 649.00m, "EA", "43211509", "Litware", "TP11-8-256", 4, "Laptops"),

        // Monitors
        Item("MON-2001", "Northwind 24\" Full HD Monitor", "24-inch IPS monitor, 1920x1080, HDMI and DisplayPort, height adjustable", 159.00m, "EA", "43211902", "Northwind", "NW-24FHD", 3, "Monitors"),
        Item("MON-2002", "Northwind 27\" QHD Monitor", "27-inch IPS monitor, 2560x1440, USB-C with 65 W power delivery", 299.00m, "EA", "43211902", "Northwind", "NW-27QHD-C", 3, "Monitors"),
        Item("MON-2003", "Northwind 32\" 4K Monitor", "32-inch 4K UHD monitor, color calibrated, USB hub", 479.00m, "EA", "43211902", "Northwind", "NW-32UHD", 6, "Monitors"),
        Item("MON-2004", "Tailspin 34\" Ultrawide Monitor", "34-inch curved ultrawide, 3440x1440, 100 Hz", 549.00m, "EA", "43211902", "Tailspin", "TS-34UW", 6, "Monitors"),
        Item("MON-2005", "Tailspin Monitor Arm, Single", "Gas-spring monitor arm for screens up to 32 inches, desk clamp", 49.90m, "EA", "43211902", "Tailspin", "TS-ARM1", 2, "Monitors"),

        // Accessories
        Item("ACC-3001", "Wireless Keyboard and Mouse Set", "Full-size wireless keyboard and optical mouse, USB receiver", 39.90m, "EA", "43211706", "Litware", "LW-KM100", 2, "Accessories"),
        Item("ACC-3002", "Ergonomic Vertical Mouse", "Wireless vertical mouse, 6 buttons, rechargeable", 44.50m, "EA", "43211708", "Litware", "LW-VM60", 2, "Accessories"),
        Item("ACC-3003", "USB-C Docking Station", "Dual 4K display dock, 100 W power delivery, Gigabit Ethernet, 6 USB ports", 189.00m, "EA", "43211603", "Fabrikam", "FB-DOCK-C100", 5, "Accessories"),
        Item("ACC-3004", "Noise-Cancelling Headset", "Over-ear Bluetooth headset with boom microphone and USB dongle", 129.00m, "EA", "43191501", "Contoso", "CT-HS700", 4, "Accessories"),
        Item("ACC-3005", "HDMI Cable 2 m (10 pack)", "High-speed HDMI 2.1 cables, 2 meters, ten per box", 54.00m, "BX", "26121609", "Northwind", "NW-HDMI2-10", 3, "Accessories"),

        // Office supplies
        Item("OFF-4001", "Copy Paper A4, 80 g (500 sheets)", "White multipurpose paper, A4, 80 g/m2, ream of 500 sheets", 5.20m, "PK", "14111507", "Adventure Works", "AW-A4-80", 1, "Office supplies"),
        Item("OFF-4002", "Copy Paper A4, 80 g (5 reams)", "White multipurpose paper, A4, 80 g/m2, carton of five reams", 24.50m, "BX", "14111507", "Adventure Works", "AW-A4-80-5", 1, "Office supplies"),
        Item("OFF-4003", "Ballpoint Pens, Blue (50)", "Medium point retractable ballpoint pens, box of 50", 18.75m, "BX", "44121701", "Adventure Works", "AW-BP-BL50", 1, "Office supplies"),
        Item("OFF-4004", "Sticky Notes 76x76 mm (12 pads)", "Yellow repositionable notes, 100 sheets per pad, 12 pads", 11.40m, "PK", "44121905", "Adventure Works", "AW-SN-76Y12", 1, "Office supplies"),
        Item("OFF-4005", "Ring Binders A4 (10 pack)", "A4 ring binders, 2 ring, 40 mm spine, assorted colors", 21.00m, "PK", "44122003", "Adventure Works", "AW-RB-A4-10", 2, "Office supplies"),
        Item("OFF-4006", "Desktop Stapler with 1000 Staples", "Heavy-duty stapler, 40 sheet capacity, includes staples", 12.30m, "EA", "44121615", "Adventure Works", "AW-ST-40", 1, "Office supplies"),

        // Furniture
        Item("FUR-5001", "Ergonomic Office Chair", "Mesh back chair with lumbar support, adjustable arms and seat depth", 289.00m, "EA", "56112102", "Wingtip", "WT-CH-ERGO", 10, "Furniture"),
        Item("FUR-5002", "Height-Adjustable Desk 160 cm", "Electric sit-stand desk, 160x80 cm, memory presets, white top", 449.00m, "EA", "56112104", "Wingtip", "WT-DSK-160", 12, "Furniture"),
        Item("FUR-5003", "Mobile Pedestal, 3 Drawers", "Lockable steel pedestal with three drawers on casters", 139.00m, "EA", "56112107", "Wingtip", "WT-PED3", 8, "Furniture"),
        Item("FUR-5004", "Bookcase 5 Shelves", "Open bookcase, 5 shelves, 180 cm high, oak finish", 119.00m, "EA", "56101504", "Wingtip", "WT-BC5", 9, "Furniture"),
    ];

    private static CatalogItem Item(
        string partId, string name, string description, decimal price, string uom, string unspsc,
        string manufacturer, string manufacturerPart, int leadTimeDays, string category) => new()
    {
        SupplierPartId = partId,
        ShortName = name,
        Description = description,
        UnitPrice = price,
        Currency = "USD",
        UnitOfMeasure = uom,
        Unspsc = unspsc,
        ManufacturerName = manufacturer,
        ManufacturerPartId = manufacturerPart,
        LeadTimeDays = leadTimeDays,
        Category = category,
    };
}
