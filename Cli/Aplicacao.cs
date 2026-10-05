namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

/// <summary>
/// Ponto de entrada da linha de comando: escolhe o comando pelo primeiro argumento
/// e trata os erros de uso de forma amigável.
/// </summary>
public static class Aplicacao
{
  private static readonly IComando[] _comandos = [new ComandoExtrair(), new ComandoOrdenar(), new ComandoGerarResultados(), new ComandoGraficos()];

  public static int Executar(string[] args)
  {
    if (args.Length == 0 || args[0] is "ajuda" or "--ajuda" or "-h" or "--help")
    {
      ImprimirAjudaGeral();
      return args.Length == 0 ? 1 : 0;
    }

    var comando = _comandos.FirstOrDefault(c => string.Equals(c.Nome, args[0], StringComparison.OrdinalIgnoreCase));
    if (comando is null)
    {
      Console.Error.WriteLine($"Comando desconhecido: '{args[0]}'.");
      Console.Error.WriteLine();
      ImprimirAjudaGeral();
      return 1;
    }

    if (args.Skip(1).Any(a => a is "--ajuda" or "-h" or "--help"))
    {
      Console.WriteLine(comando.Uso);
      return 0;
    }

    try
    {
      return comando.Executar(new ArgumentosLinhaDeComando(args.Skip(1)));
    }
    catch (ErroDeUsoException ex)
    {
      Console.Error.WriteLine($"Erro: {ex.Message}");
      Console.Error.WriteLine();
      Console.Error.WriteLine(comando.Uso);
      return 1;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
    {
      Console.Error.WriteLine($"Erro: {ex.Message}");
      return 1;
    }
  }

  private static void ImprimirAjudaGeral()
  {
    Console.WriteLine("Análise empírica de algoritmos de ordenação aplicada a imagens.");
    Console.WriteLine();
    Console.WriteLine("Uso: dotnet run -- <comando> [argumentos]");
    Console.WriteLine();
    Console.WriteLine("Comandos:");
    foreach (var comando in _comandos)
    {
      Console.WriteLine($"  {comando.Nome,-18} {comando.Resumo}");
    }

    Console.WriteLine();
    Console.WriteLine("Use <comando> --ajuda para ver as opções de cada comando.");
  }
}
