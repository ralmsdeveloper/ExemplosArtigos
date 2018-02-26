using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Testes
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var db = new ExemploContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var dados = new[]
                {
                    new Teste
                    {
                        Informacoes = "Informação 01"
                    },
                    new Teste
                    {
                        Informacoes = "Informação 02"
                    },
                        new Teste
                    {
                        Informacoes = "Informação 03"
                    }
                };

                db.Set<Teste>().AddRange(dados);
                db.SaveChanges();

                var registros = db
                    .Set<Teste>()
                    .AsNoTracking()
                    .ToList();

                // Leitura feita pelo EF Core
                Console.WriteLine("Leitura EF Core:");

                foreach (var reg in registros)
                {
                    Console.WriteLine($"{reg.Id}-{reg.Informacoes}");
                }

                // Leitura feita via ADO.NET
                Console.WriteLine("\nLeitura ADO.NET:");
                using (var cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    db.Database.OpenConnection();
                    cmd.CommandText = "SELECT [Id],[Informacoes] FROM [Teste]";
                    using (var ler = cmd.ExecuteReader())
                    {
                        while (ler.Read())
                        {
                            Console.WriteLine($"{ler.GetInt32(0)}-{ler.GetString(1)}");
                        }
                    }
                }
            }

            Console.ReadKey();
        }
    }

    public class ExemploContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=.\Sistemas;Initial Catalog=TesteCriptografia;Integrated Security=True");
            optionsBuilder.UseLoggerFactory(new LoggerFactory().AddConsole());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Teste>(p =>
            {
                p.Property(x => x.Informacoes)
                    .HasConversion(v => Criptografia.Encrypt(v), v => Criptografia.Decrypt(v));
            });
        }
    }

    public class Teste
    {
        public int Id { get; set; }
        public string Informacoes { get; set; }
    }

    public class Criptografia
    {
        private static byte[] _chave = Encoding.UTF8.GetBytes("#ef");

        public static string Encrypt(string texto)
        {
            using (var hashProvider = new MD5CryptoServiceProvider())
            {
                var encriptar = new TripleDESCryptoServiceProvider
                {
                    Mode = CipherMode.ECB,
                    Key = hashProvider.ComputeHash(_chave),
                    Padding = PaddingMode.PKCS7
                };

                using (var transforme = encriptar.CreateEncryptor())
                {
                    var dados = Encoding.UTF8.GetBytes(texto);
                    return Convert.ToBase64String(transforme.TransformFinalBlock(dados, 0, dados.Length));
                }
            }
        }

        public static string Decrypt(string texto)
        {
            using (var hashProvider = new MD5CryptoServiceProvider())
            {
                var descriptografar = new TripleDESCryptoServiceProvider
                {
                    Mode = CipherMode.ECB,
                    Key = hashProvider.ComputeHash(_chave),
                    Padding = PaddingMode.PKCS7
                };

                using (var transforme = descriptografar.CreateDecryptor())
                {
                    var dados = Convert.FromBase64String(texto.Replace(" ", "+"));
                    return Encoding.UTF8.GetString(transforme.TransformFinalBlock(dados, 0, dados.Length));
                }
            }
        }
    }
}
