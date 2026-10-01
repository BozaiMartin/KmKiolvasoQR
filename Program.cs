using QRCoder;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KmKiolvasasRegisztracio
{
    internal class Program
    {
        private const string PrivatKulcsFajl = "private.key";

        private const string NyilvanosKulcsFajl = "public.key";

        static void Main()
        {
            KulcsokEllenorzese();

            Console.Write("Dolgozószám: ");
            string dolgozoSzam = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Dolgozó neve: ");
            string dolgozoNev = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Szervezet: ");
            string szervezet = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Telephely: ");
            string telephely = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Admin? (i/n): ");
            string adminValasz = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";

            int admin = adminValasz == "i" ? 1 : 0;

            RegisztraciosAdat adat = new()
            {
                Verzio = 1,
                RegisztracioId = Guid.NewGuid().ToString("N"),
                DolgozoSzam = dolgozoSzam,
                DolgozoNev = dolgozoNev,
                Szervezet = szervezet,
                Telephely = telephely,
                Admin = admin,
                LejaratUtc = DateTime.UtcNow.AddMinutes(30)
             };

            string regisztraciosQrTartalom = RegisztraciosQrTartalomKeszites(adat);

            string belepesiQrTartalom = $"KMLOGIN:1:{dolgozoSzam}";

            string regisztraciosFajl = $"Regisztracio_{dolgozoSzam}.png";

            string belepesiFajl = $"Belepes_{dolgozoSzam}.png";

            QrKepKeszites(regisztraciosQrTartalom, regisztraciosFajl);

            QrKepKeszites(belepesiQrTartalom, belepesiFajl);
            Console.WriteLine();

            Console.WriteLine($"Regisztrációs QR elkészült:");

            Console.WriteLine(Path.GetFullPath(regisztraciosFajl));
            Console.WriteLine();

            Console.WriteLine($"Belépési QR elkészült:");

            Console.WriteLine(Path.GetFullPath(belepesiFajl));
            Console.WriteLine();

            Console.WriteLine("A regisztrációs QR 30 percig érvényes.");

            Console.WriteLine("A belépési QR nem jár le.");
            Console.WriteLine();

            Console.WriteLine("Nyilvános kulcs:");

            Console.WriteLine(File.ReadAllText(NyilvanosKulcsFajl));
            Console.ReadLine();
        }

        private static void KulcsokEllenorzese()
        {
            if (File.Exists(PrivatKulcsFajl) && File.Exists(NyilvanosKulcsFajl))
                return;

            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            string privatKulcs = Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey());

            string nyilvanosKulcs = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

            File.WriteAllText(PrivatKulcsFajl, privatKulcs);

            File.WriteAllText(NyilvanosKulcsFajl, nyilvanosKulcs);
        }

        private static string RegisztraciosQrTartalomKeszites(RegisztraciosAdat adat)
        {
            string json = JsonSerializer.Serialize(adat);

            byte[] adatBytes = Encoding.UTF8.GetBytes(json);

            byte[] privatKulcsBytes = Convert.FromBase64String(File.ReadAllText(PrivatKulcsFajl));

            using ECDsa ecdsa = ECDsa.Create();

            ecdsa.ImportPkcs8PrivateKey(privatKulcsBytes, out _);

            byte[] alairas = ecdsa.SignData(adatBytes, HashAlgorithmName.SHA256);

            return Base64UrlEncode(adatBytes) + "." + Base64UrlEncode(alairas);
        }

        private static void QrKepKeszites(string tartalom, string fajlNev)
        {
            QRCodeGenerator generator = new();

            QRCodeData qrData = generator.CreateQrCode(tartalom, QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qrCode = new(qrData);

            byte[] png = qrCode.GetGraphic(20);

            File.WriteAllBytes(fajlNev, png);
        }

        private static string Base64UrlEncode(byte[] adat)
        {
            return Convert.ToBase64String(adat).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }

    internal class RegisztraciosAdat
    {
        public int Verzio { get; set; }

        public string RegisztracioId { get; set; } = "";

        public string DolgozoSzam { get; set; } = "";

        public string DolgozoNev { get; set; } = "";

        public string Szervezet { get; set; } = "";

        public string Telephely { get; set; } = "";

        public int Admin { get; set; }

        public DateTime LejaratUtc { get; set; }
    }
}