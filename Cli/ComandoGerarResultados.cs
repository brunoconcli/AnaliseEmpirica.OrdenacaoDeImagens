namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;
using AnaliseEmpirica.OrdenacaoDeImagens.Analise;
using AnaliseEmpirica.OrdenacaoDeImagens.Experimentos;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Executa a bateria de experimentos descrita em docs/metodologia.md e grava
/// os resultados brutos, o resumo estatístico e a descrição do ambiente.
/// </summary>
public sealed class ComandoBenchmark : IComando
{
  private const string CriterioPadrao = "luminosidade";
  private const string PastaResultadosPadrao = "resultados";

  private static readonly Dictionary<string, CasoDeEntrada> _casosPorNome = new()
  {
    ["aleatorio"] = CasoDeEntrada.Aleatorio,
    ["crescente"] = CasoDeEntrada.Crescente,
    ["decrescente"] = CasoDeEntrada.Decrescente,
    ["quase-ordenado"] = CasoDeEntrada.QuaseOrdenado,
    ["original"] = CasoDeEntrada.OrdemOriginal,
  };

  public string Nome => "benchmark";

  public string Resumo => "Mede os algoritmos em vários tamanhos, casos de entrada e repetições (ver docs/metodologia.md)";

  public string Uso => $"""
      Uso: benchmark [arquivo.csv] [opções]

      Lê as propriedades do CSV gerado por "extrair" (padrão: {Caminhos.Features}).
      Execute em Release para medir tempos confiáveis: dotnet run -c Release -- benchmark ...

      {LeitorDeCriterio.Uso}
        (padrão do benchmark: --por {CriterioPadrao})

      Opções:
        --algoritmos <lista>|todos     Ex.: merge,quick (padrão: todos)
        --tamanhos <lista>             Ex.: 100,1000,10000 (padrão: {string.Join(",", ConfiguracaoBenchmark.TamanhosPadrao)})
                                       Tamanhos maiores que o total de imagens são descartados; nesse caso, o total entra como último ponto.
        --casos <lista>                {string.Join(", ", _casosPorNome.Keys)} (padrão: {string.Join(",", _casosPorNome.Keys.Take(4))})
        --repeticoes <r>               Repetições por combinação (padrão: {ConfiguracaoBenchmark.RepeticoesPadrao})
        --fracao-desordem <f>          Fração fora de posição no caso quase-ordenado (padrão: {ConfiguracaoBenchmark.FracaoDesordemPadrao})
        --semente <s>                  Semente base do sorteio das amostras (padrão: {ConfiguracaoBenchmark.SementePadrao})
        --saida <pasta>                Onde criar a pasta do benchmark (padrão: {PastaResultadosPadrao})
      """;

  public int Executar(ArgumentosLinhaDeComando argumentos)
  {
    argumentos.ValidarOpcoes(
        [.. LeitorDeCriterio.Opcoes, "algoritmos", "tamanhos", "casos", "repeticoes", "fracao-desordem", "semente", "saida"]);

    string caminhoCsv = argumentos.Posicional(0) ?? Caminhos.Features;
    var itens = LeitorDeFeatures.Ler(caminhoCsv);
    var configuracao = MontarConfiguracao(argumentos, itens);
    string pasta = Path.Combine(
        argumentos.Texto("saida") ?? PastaResultadosPadrao,
        $"benchmark_{DateTime.Now:yyyyMMdd_HHmmss}");

#if DEBUG
    Console.WriteLine("ATENÇÃO: build Debug. Os tempos não são representativos; use: dotnet run -c Release -- benchmark ...");
    Console.WriteLine();
#endif

    ImprimirConfiguracao(configuracao, caminhoCsv, itens.Length);
    Directory.CreateDirectory(pasta);
    string caminhoAmbiente = Path.Combine(pasta, "ambiente.txt");
    File.WriteAllLines(caminhoAmbiente, DescreverAmbiente(configuracao, caminhoCsv, itens.Length));

    var resultados = new List<ResultadoExecucao>(configuracao.TotalDeExecucoes);
    var cronometro = Stopwatch.StartNew();

    using (var escritor = new EscritorResultadosCsv(Path.Combine(pasta, "brutos.csv")))
    {
      int ultimoPercentual = -1;
      foreach (var resultado in new ExecutorDeBenchmark(configuracao).Executar(itens))
      {
        escritor.Escrever(resultado);
        resultados.Add(resultado);

        int percentual = 100 * resultados.Count / configuracao.TotalDeExecucoes;
        if (percentual > ultimoPercentual)
        {
          ultimoPercentual = percentual;
          Console.Write($"\r  {resultados.Count}/{configuracao.TotalDeExecucoes} ({percentual}%)  n={resultado.TamanhoAmostra}  {cronometro.Elapsed:mm\\:ss}   ");
        }
      }
    }

    cronometro.Stop();
    Console.WriteLine();
    File.AppendAllLines(caminhoAmbiente, [$"Duração total: {cronometro.Elapsed:hh\\:mm\\:ss}"]);

    var resumo = ResumoBenchmark.Calcular(resultados);
    ResumoBenchmark.Gravar(Path.Combine(pasta, "resumo.csv"), resumo);

    ImprimirResumo(resumo, configuracao);

    Console.WriteLine();
    Console.WriteLine($"Concluído em {cronometro.Elapsed:hh\\:mm\\:ss}. Resultados em '{pasta}':");
    Console.WriteLine("  brutos.csv    uma linha por execução");
    Console.WriteLine("  resumo.csv    média e desvio padrão por algoritmo × caso × n");
    Console.WriteLine("  ambiente.txt  configuração e ambiente de execução");
    return 0;
  }

  private static ConfiguracaoBenchmark MontarConfiguracao(ArgumentosLinhaDeComando argumentos, ItemImagem[] itens)
  {
    var criterio = LeitorDeCriterio.Ler(argumentos, itens, CriterioPadrao);

    var desejados = argumentos.ListaDeInteiros("tamanhos") ?? ConfiguracaoBenchmark.TamanhosPadrao;
    var tamanhos = ConfiguracaoBenchmark.AjustarTamanhos(desejados, itens.Length);
    if (tamanhos.Count == 0)
    {
      throw new ErroDeUsoException("São necessárias ao menos 2 imagens para o benchmark.");
    }

    int repeticoes = argumentos.Inteiro("repeticoes", ConfiguracaoBenchmark.RepeticoesPadrao);
    if (repeticoes < 1)
    {
      throw new ErroDeUsoException("--repeticoes deve ser ao menos 1.");
    }

    double fracaoDesordem = argumentos.Real("fracao-desordem", ConfiguracaoBenchmark.FracaoDesordemPadrao);
    if (fracaoDesordem is < 0 or > 1)
    {
      throw new ErroDeUsoException("--fracao-desordem deve estar entre 0 e 1.");
    }

    return new ConfiguracaoBenchmark(
        LerAlgoritmos(argumentos.Lista("algoritmos")),
        tamanhos,
        LerCasos(argumentos.Lista("casos")),
        repeticoes,
        criterio.Comparador,
        fracaoDesordem,
        argumentos.Inteiro("semente", ConfiguracaoBenchmark.SementePadrao));
  }

  private static IReadOnlyList<IAlgoritmoOrdenacao> LerAlgoritmos(IReadOnlyList<string>? nomes)
  {
    if (nomes is null || nomes.Any(n => string.Equals(n, "todos", StringComparison.OrdinalIgnoreCase)))
    {
      return [.. CatalogoDeAlgoritmos.Todos];
    }

    return [.. nomes.Select(LeitorDeFeatures.ObterAlgoritmo).Distinct()];
  }

  private static IReadOnlyList<CasoDeEntrada> LerCasos(IReadOnlyList<string>? nomes)
  {
    if (nomes is null)
    {
      return ConfiguracaoBenchmark.CasosPadrao;
    }

    return
    [
      .. nomes
          .Select(nome => _casosPorNome.TryGetValue(ArgumentosLinhaDeComando.Normalizar(nome), out var caso)
              ? caso
              : throw new ErroDeUsoException($"Caso desconhecido: '{nome}'. Use {string.Join(", ", _casosPorNome.Keys)}."))
          .Distinct()
    ];
  }

  private static void ImprimirConfiguracao(ConfiguracaoBenchmark configuracao, string caminhoCsv, int totalImagens)
  {
    Console.WriteLine($"Entrada: {caminhoCsv} ({totalImagens} imagens)");
    Console.WriteLine($"Critério: {configuracao.DescricaoCriterio}");
    Console.WriteLine($"Algoritmos: {string.Join(", ", configuracao.Algoritmos.Select(a => a.Nome))}");
    Console.WriteLine($"Tamanhos (n): {string.Join(", ", configuracao.Tamanhos)}");
    Console.WriteLine($"Casos: {string.Join(", ", configuracao.Casos)}");
    Console.WriteLine($"Repetições: {configuracao.Repeticoes}");
    Console.WriteLine($"Total: {configuracao.TotalDeExecucoes} execuções medidas");
    Console.WriteLine();
  }

  private static IEnumerable<string> DescreverAmbiente(ConfiguracaoBenchmark configuracao, string caminhoCsv, int totalImagens)
  {
    var cultura = CultureInfo.InvariantCulture;
    yield return "Benchmark de algoritmos de ordenação";
    yield return $"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
    yield return "";
    yield return "[Configuração]";
    yield return $"Arquivo de entrada: {caminhoCsv} ({totalImagens} imagens)";
    yield return $"Critério: {configuracao.DescricaoCriterio}";
    yield return $"Algoritmos: {string.Join(", ", configuracao.Algoritmos.Select(a => a.Nome))}";
    yield return $"Tamanhos (n): {string.Join(", ", configuracao.Tamanhos)}";
    yield return $"Casos: {string.Join(", ", configuracao.Casos)}";
    yield return $"Repetições: {configuracao.Repeticoes}";
    yield return $"Fração de desordem (quase ordenado): {configuracao.FracaoDesordem.ToString(cultura)}";
    yield return $"Semente base: {configuracao.SementeBase} (semente = base + 100003 × repetição + n)";
    yield return $"Tempo mínimo acumulado por medida de tempo: {MedidorDeExecucao.TempoMinimoPadrao.TotalMilliseconds.ToString(cultura)} ms";
    yield return $"Total de execuções medidas: {configuracao.TotalDeExecucoes}";
    yield return "";
    yield return "[Ambiente]";
    yield return $"Sistema operacional: {RuntimeInformation.OSDescription}";
    yield return $"Processador: {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "não identificado"}";
    yield return $"Processadores lógicos: {Environment.ProcessorCount}";
    yield return $"Arquitetura: {RuntimeInformation.ProcessArchitecture}";
    yield return $".NET: {RuntimeInformation.FrameworkDescription}";
#if DEBUG
    yield return "Build: Debug (tempos não representativos)";
#else
    yield return "Build: Release";
#endif
  }

  /// <summary>Uma tabela por caso: comparações médias e tempo médio, com n nas linhas e algoritmos nas colunas.</summary>
  private static void ImprimirResumo(IReadOnlyList<LinhaResumo> resumo, ConfiguracaoBenchmark configuracao)
  {
    var nomes = configuracao.Algoritmos.Select(a => a.Nome).ToList();
    var porChave = resumo.ToDictionary(l => (l.Caso, l.TamanhoAmostra, l.Algoritmo));

    foreach (var caso in configuracao.Casos)
    {
      Console.WriteLine();
      Console.WriteLine($"=== {caso} ===");

      ImprimirTabela("Comparações (média)", nomes, configuracao.Tamanhos,
          (n, algoritmo) => porChave[(caso, n, algoritmo)].ComparacoesMedia.ToString("N0"));
      ImprimirTabela("Tempo em ms (média ± desvio padrão)", nomes, configuracao.Tamanhos,
          (n, algoritmo) =>
          {
            var linha = porChave[(caso, n, algoritmo)];
            return $"{linha.TempoMsMedia:F3} ± {linha.TempoMsDesvio:F3}";
          });
    }
  }

  private static void ImprimirTabela(
      string titulo, IReadOnlyList<string> algoritmos, IReadOnlyList<int> tamanhos, Func<int, string, string> celula)
  {
    const int LarguraColuna = 20;

    Console.WriteLine(titulo);
    Console.WriteLine($"{"n",8}" + string.Concat(algoritmos.Select(a => a.PadLeft(LarguraColuna))));
    foreach (int n in tamanhos)
    {
      Console.WriteLine($"{n,8}" + string.Concat(algoritmos.Select(a => celula(n, a).PadLeft(LarguraColuna))));
    }
  }
}
