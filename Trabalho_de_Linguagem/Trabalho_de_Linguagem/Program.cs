using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// ================= Sistema de faturação de uma loja de produtos alimentares=================

class Produto
{
    public string Nome;
    public int Quantidade;
    public decimal PrecoUnitario;
    public decimal Subtotal { get { return Quantidade * PrecoUnitario; } }
}

class Cliente
{
    public int Senha;
    public string Nome;
    public string Genero;
    public int Idade;
    public bool Gravida;
    public bool Deficiente;
    public bool Prioritario;
    public string MotivoPrioridade = "";
}

class Fatura
{
    public int Numero;
    public Cliente Cliente;
    public DateTime Data;
    public List<Produto> Itens = new List<Produto>();
    public decimal Subtotal;
    public decimal Bonus;
    public decimal TotalPagar;
    public bool TemBonus;


    class Program
    {
        const decimal LIMITE_BONUS = 100000m;   // 100 000 Kz
        const decimal PERCENT_BONUS = 0.10m;    // 10%
        const int IDADE_IDOSO = 60;

        static Queue<Cliente> filaPrioritaria = new Queue<Cliente>();
        static Queue<Cliente> filaNormal = new Queue<Cliente>();
        static List<Fatura> faturas = new List<Fatura>();

        static int proxSenha = 1;
        static int proxFatura = 1;

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            int op;
            do
            {
                Console.WriteLine();
                Console.WriteLine("=============================================");
                Console.WriteLine("   LOJA DE PRODUTOS ALIMENTARES - FATURACAO");
                Console.WriteLine("=============================================");
                Console.WriteLine("1 - Registar cliente na fila de atendimento");
                Console.WriteLine("2 - Ver fila de espera");
                Console.WriteLine("3 - Atender proximo cliente (registar compras e faturar)");
                Console.WriteLine("4 - Listar faturas emitidas");
                Console.WriteLine("5 - Resumo do dia");
                Console.WriteLine("0 - Sair");
                op = LerInt("Escolha uma opcao: ");

                switch (op)
                {
                    case 1: RegistarCliente(); break;
                    case 2: VerFila(); break;
                    case 3: AtenderProximo(); break;
                    case 4: ListarFaturas(); break;
                    case 5: ResumoDia(); break;
                    case 0: Console.WriteLine("A sair do sistema..."); break;
                    default: Console.WriteLine("Opcao invalida!"); break;
                }
            } while (op != 0);
        }



        static string LerTexto(string msg)
        {
            Console.Write(msg);
            string t = Console.ReadLine();
            return (t ?? "").Trim();
        }

        static int LerInt(string msg)
        {
            int n;
            Console.Write(msg);
            while (!int.TryParse(Console.ReadLine(), out n) || n < 0)
            {
                Console.Write("Valor invalido. Tente novamente: ");
            }
            return n;
        }

        // aceita 1500.50 ou 1500,50
        static decimal LerDecimal(string msg)
        {
            decimal v;
            Console.Write(msg);
            while (true)
            {
                string t = (Console.ReadLine() ?? "").Trim().Replace(',', '.');
                if (decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out v) && v >= 0)
                    return v;
                Console.Write("Valor invalido. Tente novamente: ");
            }
        }

        static bool LerSimNao(string msg)
        {
            while (true)
            {
                string r = LerTexto(msg + " (s/n): ").ToLower();
                if (r == "s") return true;
                if (r == "n") return false;
                Console.WriteLine("Responda apenas com s ou n.");
            }
        }

        static string LerGenero()
        {
            while (true)
            {
                string g = LerTexto("Genero (M/F): ").ToUpper();
                if (g == "M" || g == "F") return g;
                Console.WriteLine("Escreva apenas M ou F.");
            }
        }

        static string Kz(decimal valor)
        {
            return valor.ToString("N2", CultureInfo.InvariantCulture) + " Kz";
        }

        // ---------- 1. REGISTAR CLIENTE NA FILA ----------

        static void RegistarCliente()
        {
            Console.WriteLine("\n--- REGISTAR CLIENTE ---");
            Cliente c = new Cliente();
            c.Senha = proxSenha++;
            c.Nome = LerTexto("Nome do cliente: ");
            c.Genero = LerGenero();
            c.Idade = LerInt("Idade: ");

            // Gravida so se aplica a mulheres
            if (c.Genero == "F")
                c.Gravida = LerSimNao("Esta gravida?");
            c.Deficiente = LerSimNao("Tem alguma deficiencia?");

            // Determinar prioridade de atendimento
            List<string> motivos = new List<string>();
            if (c.Gravida) motivos.Add("Mulher gravida");
            if (c.Idade >= IDADE_IDOSO) motivos.Add("Pessoa idosa");
            if (c.Deficiente) motivos.Add("Pessoa com deficiencia");

            if (motivos.Count > 0)
            {
                c.Prioritario = true;
                c.MotivoPrioridade = string.Join(" + ", motivos);
                filaPrioritaria.Enqueue(c);
                Console.WriteLine("Cliente PRIORITARIO (" + c.MotivoPrioridade + ").");
            }
            else
            {
                filaNormal.Enqueue(c);
                Console.WriteLine("Cliente colocado na fila normal.");
            }
            Console.WriteLine("Senha de atendimento: " + c.Senha);
        }

        // ---------- 2. VER FILA ----------

        static void VerFila()
        {
            Console.WriteLine("\n--- FILA DE ESPERA ---");
            Console.WriteLine("Fila PRIORITARIA (" + filaPrioritaria.Count + "):");
            if (filaPrioritaria.Count == 0) Console.WriteLine("   (vazia)");
            foreach (Cliente c in filaPrioritaria)
                Console.WriteLine("   Senha " + c.Senha + " | " + c.Nome + " | " + c.MotivoPrioridade);

            Console.WriteLine("Fila NORMAL (" + filaNormal.Count + "):");
            if (filaNormal.Count == 0) Console.WriteLine("   (vazia)");
            foreach (Cliente c in filaNormal)
                Console.WriteLine("   Senha " + c.Senha + " | " + c.Nome);
        }

        // ---------- 3. ATENDER E FATURAR ----------

        static void AtenderProximo()
        {
            Console.WriteLine("\n--- ATENDIMENTO ---");
            Cliente c;

            // A fila prioritaria e sempre atendida primeiro
            if (filaPrioritaria.Count > 0) c = filaPrioritaria.Dequeue();
            else if (filaNormal.Count > 0) c = filaNormal.Dequeue();
            else
            {
                Console.WriteLine("Nao ha clientes em espera.");
                return;
            }

            Console.WriteLine("A atender: Senha " + c.Senha + " - " + c.Nome +
                (c.Prioritario ? " [PRIORITARIO: " + c.MotivoPrioridade + "]" : ""));

            Fatura f = new Fatura();
            f.Cliente = c;
            f.Data = DateTime.Now;

            // Registar as compras
            Console.WriteLine("Registe os produtos (deixe o nome vazio para terminar).");
            while (true)
            {
                string nome = LerTexto("\nProduto: ");
                if (nome == "") break;

                Produto p = new Produto();
                p.Nome = nome;
                p.Quantidade = LerInt("Quantidade: ");
                p.PrecoUnitario = LerDecimal("Preco unitario (Kz): ");

                if (p.Quantidade == 0)
                {
                    Console.WriteLine("Quantidade zero. Produto ignorado.");
                    continue;
                }
                f.Itens.Add(p);
                Console.WriteLine("Subtotal parcial: " + Kz(f.Itens.Sum(x => x.Subtotal)));
            }

            if (f.Itens.Count == 0)
            {
                Console.WriteLine("Nenhuma compra registada. Nao foi emitida fatura.");
                return;
            }

            // Calculo da fatura
            f.Numero = proxFatura++;
            f.Subtotal = f.Itens.Sum(x => x.Subtotal);

            // Bonus comercial: depende SO do valor das compras
            if (f.Subtotal > LIMITE_BONUS)
            {
                f.TemBonus = true;
                f.Bonus = f.Subtotal * PERCENT_BONUS;
            }
            f.TotalPagar = f.Subtotal - f.Bonus;

            faturas.Add(f);
            MostrarFatura(f);
        }

        static void MostrarFatura(Fatura f)
        {
            Console.WriteLine();
            Console.WriteLine("=============================================");
            Console.WriteLine("              FATURA No " + f.Numero);
            Console.WriteLine("=============================================");
            Console.WriteLine("Data: " + f.Data.ToString("dd/MM/yyyy HH:mm"));
            Console.WriteLine("Cliente: " + f.Cliente.Nome + " (" + f.Cliente.Genero + ", " + f.Cliente.Idade + " anos)");
            if (f.Cliente.Prioritario)
                Console.WriteLine("Atendimento prioritario: " + f.Cliente.MotivoPrioridade);
            Console.WriteLine("---------------------------------------------");
            foreach (Produto p in f.Itens)
            {
                Console.WriteLine(p.Nome + "  " + p.Quantidade + " x " + Kz(p.PrecoUnitario) + " = " + Kz(p.Subtotal));
            }
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("Total das compras:  " + Kz(f.Subtotal));
            if (f.TemBonus)
                Console.WriteLine("Bonus comercial 10%: -" + Kz(f.Bonus));
            else
                Console.WriteLine("Bonus comercial:     nao elegivel (compras ate 100 000 Kz)");
            Console.WriteLine("TOTAL A PAGAR:      " + Kz(f.TotalPagar));
            Console.WriteLine("=============================================");
        }

        // ---------- 4. LISTAR FATURAS ----------

        static void ListarFaturas()
        {
            Console.WriteLine("\n--- FATURAS EMITIDAS ---");
            if (faturas.Count == 0)
            {
                Console.WriteLine("Nenhuma fatura emitida.");
                return;
            }
            foreach (Fatura f in faturas)
            {
                Console.WriteLine("Fatura " + f.Numero + " | " + f.Cliente.Nome +
                    " | Compras: " + Kz(f.Subtotal) +
                    " | Bonus: " + Kz(f.Bonus) +
                    " | A pagar: " + Kz(f.TotalPagar) +
                    (f.Cliente.Prioritario ? " | PRIORITARIO" : ""));
            }
        }

        // ---------- 5. RESUMO DO DIA ----------

        static void ResumoDia()
        {
            Console.WriteLine("\n--- RESUMO DO DIA ---");
            Console.WriteLine("Faturas emitidas: " + faturas.Count);
            Console.WriteLine("Total das compras: " + Kz(faturas.Sum(f => f.Subtotal)));
            Console.WriteLine("Total de bonus concedidos: " + Kz(faturas.Sum(f => f.Bonus)));
            Console.WriteLine("Total faturado (a pagar): " + Kz(faturas.Sum(f => f.TotalPagar)));
            Console.WriteLine("Clientes com bonus: " + faturas.Count(f => f.TemBonus));
            Console.WriteLine("Clientes atendidos com prioridade: " + faturas.Count(f => f.Cliente.Prioritario));
            Console.WriteLine("Homens atendidos: " + faturas.Count(f => f.Cliente.Genero == "M"));
            Console.WriteLine("Mulheres atendidas: " + faturas.Count(f => f.Cliente.Genero == "F"));
            Console.WriteLine("Ainda em espera: " + (filaPrioritaria.Count + filaNormal.Count));
        }
    }
}
