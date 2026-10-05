namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

using System.Diagnostics;

using AnaliseEmpirica.OrdenacaoDeImagens.Extracao;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Fase de carga: extrai as propriedades de todas as imagens de uma pasta e grava o features.csv.
/// </summary>
public sealed class ComandoExtrair : IComando
{
  private const int MaximoFalhasListadas = 10;

  public string Nome => "extrair";

  public string Resumo => "Extrai as propriedades das imagens de uma pasta e grava o features.csv";

  public string Uso => $"""
      Uso: extrair <pasta-de-imagens> [opções]

      Lê todas as imagens suportadas da pasta e das subpastas ({string.Join(", ", PreProcessador.ExtensoesSuportadas)}),
      extrai as propriedades e grava o resultado em CSV.

      Opções:
        --saida <arquivo.csv>          Arquivo de saída (padrão: {Caminhos.Features})
        --lado <pixels>                Lado do redimensionamento (padrão: {ParametrosExtracao.Padrao.LadoRedimensionamento})
        --limiar-croma <τ>             Croma média abaixo da qual a imagem é acromática (padrão: {ParametrosExtracao.Padrao.LimiarCromaAcromatica})
        --limiar-concentracao <ρ>      Concentração de matiz abaixo da qual a imagem é acromática (padrão: {ParametrosExtracao.Padrao.LimiarConcentracaoMatiz})
        --limiar-borda <T>             Magnitude de Sobel mínima para contar como borda (padrão: {ParametrosExtracao.Padrao.LimiarBorda})
      """;

  public int Executar(ArgumentosLinhaDeComando argumentos)
  {
    argumentos.ValidarOpcoes("saida", "lado", "limiar-croma", "limiar-concentracao", "limiar-borda");

    string pasta = argumentos.Posicional(0, "a pasta de imagens");
    string saida = argumentos.Texto("saida") ?? Caminhos.Features;

    var padrao = ParametrosExtracao.Padrao;
    var parametros = new ParametrosExtracao(
        argumentos.Inteiro("lado", padrao.LadoRedimensionamento),
        argumentos.Real("limiar-croma", padrao.LimiarCromaAcromatica),
        argumentos.Real("limiar-concentracao", padrao.LimiarConcentracaoMatiz),
        argumentos.Real("limiar-borda", padrao.LimiarBorda));

    if (!Directory.Exists(pasta))
    {
      throw new ErroDeUsoException($"Pasta não encontrada: '{pasta}'.");
    }

    Console.WriteLine($"Extraindo propriedades de '{pasta}'");
    Console.WriteLine($"Parâmetros: lado={parametros.LadoRedimensionamento}px, τ={parametros.LimiarCromaAcromatica}, ρ={parametros.LimiarConcentracaoMatiz}, T={parametros.LimiarBorda}");

    var cronometro = Stopwatch.StartNew();
    var resultado = new ExtratorItemImagem(parametros).ExtrairPasta(pasta, new ProgressoNoConsole());
    cronometro.Stop();
    Console.WriteLine();

    if (resultado.Itens.Count == 0 && resultado.Falhas.Count == 0)
    {
      Console.WriteLine("Nenhuma imagem suportada encontrada.");
      return 1;
    }

    Console.WriteLine($"{resultado.Itens.Count} imagens processadas em {cronometro.Elapsed.TotalSeconds:F1} s");
    ImprimirFalhas(resultado.Falhas);

    if (resultado.Itens.Count == 0)
    {
      return 1;
    }

    ImprimirResumo(resultado.Itens);

    CacheDePropriedades.Gravar(saida, resultado.Itens);
    Console.WriteLine();
    Console.WriteLine($"Propriedades gravadas em '{saida}'");
    return 0;
  }

  private static void ImprimirFalhas(IReadOnlyList<FalhaExtracao> falhas)
  {
    if (falhas.Count == 0)
    {
      return;
    }

    Console.WriteLine($"{falhas.Count} arquivo(s) não puderam ser processados:");
    foreach (var falha in falhas.Take(MaximoFalhasListadas))
    {
      Console.WriteLine($"  {falha.CaminhoArquivo}: {falha.Motivo}");
    }

    if (falhas.Count > MaximoFalhasListadas)
    {
      Console.WriteLine($"  ... e mais {falhas.Count - MaximoFalhasListadas}");
    }
  }

  /// <summary>Distribuição de cada propriedade, útil para calibrar os limiares.</summary>
  private static void ImprimirResumo(IReadOnlyList<ItemImagem> itens)
  {
    Console.WriteLine();
    Console.WriteLine($"{"Propriedade",-14} {"mínimo",10} {"média",10} {"máximo",10}");

    foreach (var propriedade in Enum.GetValues<Propriedade>())
    {
      var valores = itens
          .Where(i => propriedade != Propriedade.Tonalidade || !i.IsAcromatica)
          .Select(i => i.ObterValor(propriedade))
          .ToList();

      if (valores.Count == 0)
      {
        Console.WriteLine($"{propriedade,-14} {"—",10} {"—",10} {"—",10}");
        continue;
      }

      Console.WriteLine($"{propriedade,-14} {valores.Min(),10:F4} {valores.Average(),10:F4} {valores.Max(),10:F4}");
    }

    int acromaticas = itens.Count(i => i.IsAcromatica);
    int semBordas = itens.Count(i => i.Complexidade == 0);
    Console.WriteLine($"Acromáticas: {acromaticas} ({100.0 * acromaticas / itens.Count:F1}%), fora das estatísticas de Tonalidade");
    Console.WriteLine($"Sem nenhuma borda (Complexidade = 0): {semBordas} ({100.0 * semBordas / itens.Count:F1}%)");
  }

  /// <summary>Mostra a porcentagem na mesma linha, atualizando no máximo a cada ponto percentual.</summary>
  private sealed class ProgressoNoConsole : IProgress<(int Processados, int Total)>
  {
    private readonly Lock _trava = new();
    private int _ultimoPercentual = -1;

    public void Report((int Processados, int Total) valor)
    {
      int percentual = valor.Total == 0 ? 100 : 100 * valor.Processados / valor.Total;

      lock (_trava)
      {
        if (percentual <= _ultimoPercentual)
        {
          return;
        }

        _ultimoPercentual = percentual;
        Console.Write($"\r  {valor.Processados}/{valor.Total} ({percentual}%)");
      }
    }
  }
}
