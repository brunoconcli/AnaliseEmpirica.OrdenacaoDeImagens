namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

using System.Globalization;
using System.Text;

using AnaliseEmpirica.OrdenacaoDeImagens.Analise;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Gera os gráficos e o ajuste de curva de um benchmark a partir do resumo.csv.
/// </summary>
public sealed class ComandoGraficos : IComando
{
  private const string PastaResultadosPadrao = "resultados";
  private const string PrefixoBenchmark = "benchmark_";

  public string Nome => "graficos";

  public string Resumo => "Gera os gráficos e o ajuste de curva de um benchmark";

  public string Uso => $"""
      Uso: graficos [pasta-do-benchmark]

      Lê o resumo.csv da pasta (padrão: o benchmark mais recente em '{PastaResultadosPadrao}')
      e cria, dentro dela:
        graficos/comparacoes_por_caso.png   Comparações × n (log-log), um painel por caso
        graficos/comparacoes_linear.png     Comparações × n em escala linear (n² vira parábola)
        graficos/comparacoes_linear_nlogn.png  O mesmo, só com as séries subquadráticas de cada caso
        graficos/tempo_por_caso.png         Tempo × n (log-log, média ± desvio), um painel por caso
        graficos/teoria_vs_pratica.png      Comparações medidas × fórmula do caso médio
        graficos/mapa_casos.png             Tempo mediano por algoritmo × caso, no maior n
        ajuste.csv                          Expoente k de cada curva (custo ≈ c · n^k)
      """;

  public int Executar(ArgumentosLinhaDeComando argumentos)
  {
    argumentos.ValidarOpcoes();

    string pasta = argumentos.Posicional(0) ?? BenchmarkMaisRecente();
    string caminhoResumo = Path.Combine(pasta, "resumo.csv");
    if (!File.Exists(caminhoResumo))
    {
      throw new ErroDeUsoException($"Não há resumo.csv em '{pasta}'. Informe a pasta de um benchmark.");
    }

    var resumo = ResumoBenchmark.Ler(caminhoResumo);
    var gerador = new GeradorDeGraficos(resumo);
    string pastaGraficos = Path.Combine(pasta, "graficos");
    Directory.CreateDirectory(pastaGraficos);

    Console.WriteLine($"Benchmark: {pasta} (critério: {gerador.Criterio})");

    var graficos = new (string Arquivo, Action<string> Gerar)[]
    {
      ("comparacoes_por_caso.png", gerador.SalvarComparacoesPorCaso),
      ("comparacoes_linear.png", gerador.SalvarComparacoesPorCasoLinear),
      ("comparacoes_linear_nlogn.png", gerador.SalvarComparacoesPorCasoLinearSubquadraticos),
      ("tempo_por_caso.png", gerador.SalvarTempoPorCaso),
      ("teoria_vs_pratica.png", gerador.SalvarTeoriaVsPratica),
      ("mapa_casos.png", gerador.SalvarMapaDeCasos),
    };

    foreach (var (arquivo, gerar) in graficos)
    {
      string caminho = Path.Combine(pastaGraficos, arquivo);
      gerar(caminho);
      Console.WriteLine($"  {(File.Exists(caminho) ? caminho : arquivo + " (não gerado: faltam dados)")}");
    }

    var ajustes = CalcularAjustes(resumo);
    GravarAjustes(Path.Combine(pasta, "ajuste.csv"), ajustes);
    ImprimirAjustes(ajustes);
    return 0;
  }

  private static string BenchmarkMaisRecente()
  {
    if (!Directory.Exists(PastaResultadosPadrao))
    {
      throw new ErroDeUsoException($"A pasta '{PastaResultadosPadrao}' não existe. Rode o comando gerar-resultados antes.");
    }

    // O nome contém data e hora (benchmark_AAAAMMDD_HHMMSS), então a ordem alfabética é a cronológica.
    return Directory.GetDirectories(PastaResultadosPadrao, PrefixoBenchmark + "*")
        .Order(StringComparer.Ordinal)
        .LastOrDefault()
        ?? throw new ErroDeUsoException($"Nenhum benchmark encontrado em '{PastaResultadosPadrao}'.");
  }

  private sealed record LinhaAjuste(string Algoritmo, CasoDeEntrada Caso, string Medida, AjustePotencia Ajuste);

  private static List<LinhaAjuste> CalcularAjustes(IReadOnlyList<LinhaResumo> resumo)
  {
    var ajustes = new List<LinhaAjuste>();
    foreach (var grupo in resumo.GroupBy(l => (l.Algoritmo, l.Caso)))
    {
      var linhas = grupo.OrderBy(l => l.TamanhoAmostra).ToList();
      if (linhas.Count < 2)
      {
        continue;
      }

      double[] tamanhos = [.. linhas.Select(l => (double)l.TamanhoAmostra)];
      ajustes.Add(new(grupo.Key.Algoritmo, grupo.Key.Caso, "Comparacoes",
          AjusteDeCurva.AjustarPotencia(tamanhos, [.. linhas.Select(l => l.ComparacoesMedia)])));
      ajustes.Add(new(grupo.Key.Algoritmo, grupo.Key.Caso, "TempoMs",
          AjusteDeCurva.AjustarPotencia(tamanhos, [.. linhas.Select(l => l.TempoMsMediana)])));
    }

    return ajustes;
  }

  private static void GravarAjustes(string caminho, IReadOnlyList<LinhaAjuste> ajustes)
  {
    var cultura = CultureInfo.InvariantCulture;
    var linhas = new List<string> { "Algoritmo,Caso,Medida,Expoente,Coeficiente,R2" };
    linhas.AddRange(ajustes.Select(a => string.Join(',',
        $"\"{a.Algoritmo}\"",
        a.Caso,
        a.Medida,
        a.Ajuste.Expoente.ToString("R", cultura),
        a.Ajuste.Coeficiente.ToString("R", cultura),
        a.Ajuste.R2.ToString("R", cultura))));
    File.WriteAllLines(caminho, linhas, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
  }

  private static void ImprimirAjustes(IReadOnlyList<LinhaAjuste> ajustes)
  {
    var casos = ajustes.Select(a => a.Caso).Distinct().Order().ToList();
    var algoritmos = ajustes.Select(a => a.Algoritmo).Distinct().ToList();

    Console.WriteLine();
    Console.WriteLine("Expoente k das comparações (custo ≈ c · n^k): ~2 indica n²; pouco acima de 1 indica n·log n; ~1 indica n");
    Console.WriteLine($"{"",-16}" + string.Concat(casos.Select(c => $"{c,16}")));
    foreach (string algoritmo in algoritmos)
    {
      Console.Write($"{algoritmo,-16}");
      foreach (var caso in casos)
      {
        var ajuste = ajustes.FirstOrDefault(a => a.Algoritmo == algoritmo && a.Caso == caso && a.Medida == "Comparacoes");
        Console.Write($"{(ajuste is null ? "—" : ajuste.Ajuste.Expoente.ToString("0.00")),16}");
      }

      Console.WriteLine();
    }

    Console.WriteLine();
    Console.WriteLine("Expoentes do tempo e R² de cada ajuste: ajuste.csv");
  }
}
