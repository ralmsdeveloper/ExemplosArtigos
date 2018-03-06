using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ExemploCritografiaAnnotation
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var db = new ExemploContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                //Fazer no primeiro acesso ao banco
                db.Set<TesteDescriptografado>().Load();
                db.Set<TesteCriptografado>().Load();

                var tempo = new Stopwatch();

                var tempoGravarDecriptografado = TimeSpan.MinValue;
                var tempoGravar = TimeSpan.MinValue;
                for (int y = 0; y < 4; y++)
                {
                    tempo.Restart();
                    for (int i = 0; i < 1000; i++)
                    {
                        db.Set<TesteDescriptografado>().Add(
                            new TesteDescriptografado
                            {
                                Informacoes = $"Informação {i}",
                                Cadastro = DateTime.Now
                            });
                    }
                    db.SaveChanges();
                    tempoGravarDecriptografado = tempo.Elapsed;
                    tempo.Stop();

                    tempo.Restart();
                    for (int i = 0; i < 1000; i++)
                    {
                        db.Set<TesteCriptografado>().Add(
                            new TesteCriptografado
                            {
                                Informacoes = $"Informação {i}",
                                Cadastro = DateTime.Now
                            });
                    }
                    db.SaveChanges();
                    tempoGravar = tempo.Elapsed;
                    tempo.Stop();
                }

                tempo.Restart();
                var registros = db
                    .Set<TesteDescriptografado>()
                    .AsNoTracking()
                    .ToList();

                tempo.Stop();
                Console.WriteLine($"Tempo Leitura: {tempo.Elapsed}");

                Console.WriteLine($"Tempo Gravar DeCriptografado: {tempoGravarDecriptografado}");
                Console.WriteLine($"Tempo Gravar Criptografado: {tempoGravar}");

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
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TesteCriptografado>();
            modelBuilder.Entity<TesteDescriptografado>();

            foreach (var entidade in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var propriedade in entidade.GetProperties())
                {
                    var atributos = propriedade
                        .PropertyInfo
                        .GetCustomAttributes(typeof(EncripatarAttribute), false);

                    if (atributos.Any())
                    {
                        propriedade.SetPropertyAccessMode(PropertyAccessMode.Field);
                    }
                }
            }
        }
    }

    public class TesteCriptografado
    {
        private string informacoes;

        public int Id { get; set; }
        [Encripatar]
        public string Informacoes
        {
            get => Criptografia.Decrypt(informacoes);
            set => informacoes = Criptografia.Encrypt(value);
        }
        public DateTime Cadastro { get; set; }
    }

    public class TesteDescriptografado
    {
        public int Id { get; set; }
        public string Informacoes { get; set; }
        public DateTime Cadastro { get; set; }
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

    [AttributeUsage(AttributeTargets.Property)]
    public class EncripatarAttribute : Attribute
    {
        public EncripatarAttribute()
        {
        }
    }
}
