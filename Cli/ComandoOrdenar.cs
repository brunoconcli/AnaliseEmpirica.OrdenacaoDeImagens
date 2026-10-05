namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

using System.Diagnostics;

using AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;
using AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;
using AnaliseEmpirica.OrdenacaoDeImagens.Extracao;
using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Ordena as imagens do features.csv por um critério e mostra o resultado e o custo.
/// </summary>
public sealed class ComandoOrdenar : IComando
{
  private const int LimitePadrao = 20;
  private const double LarguraFaixaPadrao = 30;
  private const string TodosOsAlgoritmos = "todos";

  public string Nome => "ordenar";

  public string Resumo => "Ordena as imagens por um critério e mostra o resultado, as comparações e as movimentações";

  public string Uso => $"""
      Uso: ordenar [arquivo.csv] --por <critério> [opções]

      Lê as propriedades do CSV gerado por "extrair" (padrão: {Caminhos.Features}).

      Critérios (--por):
        luminosidade | tonalidade | saturacao | complexidade | entropia
        faixa-tonalidade      Faixas de tonalidade e, dentro delas, desempate por outra propriedade
            --largura-faixa <graus>    Largura de cada faixa (padrão: {LarguraFaixaPadrao})
            --desempate <propriedade>  Propriedade de desempate (padrão: luminosidade)
        distancia             Da imagem mais parecida com a referência até a mais diferente
            --referencia <arquivo>     Caminho ou nome de uma imagem presente no CSV

      Opções:
        --ordem asc|desc               Crescente ou decrescente (padrão: asc)
        --algoritmo <nome>|todos       {string.Join(", ", CatalogoDeAlgoritmos.Nomes)} ou todos (padrão: merge)
        --limite <n>                   Quantas imagens listar; 0 lista todas (padrão: {LimitePadrao})
        --copiar-para <pasta>          Copia as imagens para a pasta, numeradas na ordem do resultado
                                       (a pasta não pode existir ou deve estar vazia)
      """;

  public int Executar(ArgumentosLinhaDeComando argumentos)
  {
    argumentos.ValidarOpcoes("por", "ordem", "algoritmo", "largura-faixa", "desempate", "referencia", "limite", "copiar-para");

    string caminhoCsv = argumentos.Posicional(0) ?? Caminhos.Features;
    if (!File.Exists(caminhoCsv))
    {
      throw new ErroDeUsoException($"Arquivo não encontrado: '{caminhoCsv}'. Rode o comando \"extrair\" antes.");
    }

    ItemImagem[] itens = [.. CacheDePropriedades.Ler(caminhoCsv)];
    if (itens.Length == 0)
    {
      throw new ErroDeUsoException($"O arquivo '{caminhoCsv}' não contém imagens.");
    }

    var criterio = MontarCriterio(argumentos, itens);
    var algoritmos = ObterAlgoritmos(argumentos.Texto("algoritmo") ?? "merge");
    int limite = argumentos.Inteiro("limite", LimitePadrao);
    string? pastaCopia = argumentos.Texto("copiar-para");
    ValidarPastaCopia(pastaCopia);

    Console.WriteLine($"Critério: {criterio.Comparador}");
    Console.WriteLine($"Imagens: {itens.Length}");
    Console.WriteLine();

    ItemImagem[]? ordenados = null;
    Console.WriteLine($"{"Algoritmo",-16} {"Comparações",14} {"Movimentações",14} {"Tempo (ms)",12}");

    foreach (var algoritmo in algoritmos)
    {
      var execucao = Executar(algoritmo, itens, criterio.Comparador);
      ordenados ??= execucao.Ordenados;

      Console.WriteLine(
          $"{algoritmo.Nome,-16} {execucao.Contadores.Comparacoes,14:N0} {execucao.Contadores.Movimentacoes,14:N0} {execucao.TempoMs,12:F3}");
    }

    Console.WriteLine("Tempo de uma única execução, sem contagem de comparações; para medidas confiáveis, use o benchmark.");
    Console.WriteLine();

    ImprimirResultado(ordenados!, criterio, limite);

    if (pastaCopia is not null)
    {
      CopiarNumeradas(ordenados!, pastaCopia);
    }

    return 0;
  }

  private static (ItemImagem[] Ordenados, Contadores Contadores, double TempoMs) Executar(
      IAlgoritmoOrdenacao algoritmo, ItemImagem[] itens, IComparer<ItemImagem> comparador)
  {
    // Execução instrumentada: contagem de comparações e movimentações.
    var contadores = new Contadores();
    var ordenados = (ItemImagem[])itens.Clone();
    algoritmo.Ordenar(ordenados, new ContadorComparer<ItemImagem>(comparador, contadores), contadores);

    for (int i = 1; i < ordenados.Length; i++)
    {
      if (comparador.Compare(ordenados[i - 1], ordenados[i]) > 0)
      {
        throw new InvalidOperationException($"{algoritmo.Nome} produziu uma saída fora de ordem na posição {i}.");
      }
    }

    // Execução de tempo: comparador original, sem o envoltório de contagem.
    var copia = (ItemImagem[])itens.Clone();
    var cronometro = Stopwatch.StartNew();
    algoritmo.Ordenar(copia, comparador, new Contadores());
    cronometro.Stop();

    return (ordenados, contadores, cronometro.Elapsed.TotalMilliseconds);
  }

  private static void ImprimirResultado(ItemImagem[] ordenados, Criterio criterio, int limite)
  {
    int quantidade = limite <= 0 ? ordenados.Length : Math.Min(limite, ordenados.Length);
    int digitos = ordenados.Length.ToString().Length;

    for (int i = 0; i < quantidade; i++)
    {
      Console.WriteLine($"{(i + 1).ToString().PadLeft(digitos)}  {criterio.DescreverValor(ordenados[i]),-34}  {ordenados[i].CaminhoArquivo}");
    }

    if (quantidade < ordenados.Length)
    {
      Console.WriteLine($"... e mais {ordenados.Length - quantidade} (use --limite 0 para listar todas)");
    }
  }

  private static void ValidarPastaCopia(string? pasta)
  {
    if (pasta is not null && Directory.Exists(pasta) && Directory.EnumerateFileSystemEntries(pasta).Any())
    {
      throw new ErroDeUsoException($"A pasta '{pasta}' já existe e não está vazia. Escolha outra pasta para não misturar resultados.");
    }
  }

  private static void CopiarNumeradas(ItemImagem[] ordenados, string pasta)
  {
    Directory.CreateDirectory(pasta);
    int digitos = ordenados.Length.ToString().Length;
    var naoEncontradas = new List<string>();

    for (int i = 0; i < ordenados.Length; i++)
    {
      string origem = ordenados[i].CaminhoArquivo;
      if (!File.Exists(origem))
      {
        naoEncontradas.Add(origem);
        continue;
      }

      string destino = Path.Combine(pasta, $"{(i + 1).ToString().PadLeft(digitos, '0')}_{Path.GetFileName(origem)}");
      File.Copy(origem, destino);
    }

    Console.WriteLine();
    Console.WriteLine($"{ordenados.Length - naoEncontradas.Count} imagens copiadas para '{pasta}'");
    if (naoEncontradas.Count > 0)
    {
      Console.WriteLine($"{naoEncontradas.Count} imagens do CSV não existem mais no disco (ex.: '{naoEncontradas[0]}')");
    }
  }

  private static IReadOnlyList<IAlgoritmoOrdenacao> ObterAlgoritmos(string nome)
  {
    if (string.Equals(nome, TodosOsAlgoritmos, StringComparison.OrdinalIgnoreCase))
    {
      return [.. CatalogoDeAlgoritmos.Todos];
    }

    return CatalogoDeAlgoritmos.TryObter(nome, out var algoritmo)
        ? [algoritmo]
        : throw new ErroDeUsoException(
            $"Algoritmo desconhecido: '{nome}'. Use {string.Join(", ", CatalogoDeAlgoritmos.Nomes)} ou {TodosOsAlgoritmos}.");
  }

  private static Criterio MontarCriterio(ArgumentosLinhaDeComando argumentos, ItemImagem[] itens)
  {
    string por = ArgumentosLinhaDeComando.Normalizar(
        argumentos.Texto("por") ?? throw new ErroDeUsoException("Informe o critério com --por."));
    var ordem = LerOrdem(argumentos.Texto("ordem") ?? "asc");

    switch (por)
    {
      case "faixa-tonalidade":
        {
          double largura = argumentos.Real("largura-faixa", LarguraFaixaPadrao);
          var desempate = LerPropriedade(argumentos.Texto("desempate") ?? "luminosidade");
          var faixas = new FaixaTonalidadeComparer(largura, desempate);

          return new Criterio(
              ComparerFactory.PorFaixaDeTonalidade(largura, desempate, ordem),
              item =>
              {
                int faixa = faixas.ObterFaixa(item);
                string textoFaixa = faixa < 0 ? "acromática" : $"faixa {faixa}";
                return $"{textoFaixa} | {desempate} {item.ObterValor(desempate):F2}";
              });
        }

      case "distancia":
        {
          var referencia = EncontrarReferencia(
              argumentos.Texto("referencia") ?? throw new ErroDeUsoException("O critério distancia precisa de --referencia."),
              itens);

          return new Criterio(
              ComparerFactory.PorDistancia(referencia, ordem: ordem),
              item => $"d = {DistanciaReferencia.Calcular(item, referencia, PesosDistancia.Iguais):F4}");
        }

      default:
        {
          var propriedade = LerPropriedade(por);
          return new Criterio(
              ComparerFactory.PorPropriedade(propriedade, ordem),
              item => propriedade == Propriedade.Tonalidade && item.IsAcromatica
                  ? "acromática"
                  : $"{item.ObterValor(propriedade):F4}");
        }
    }
  }

  private static Propriedade LerPropriedade(string texto)
  {
    string normalizado = ArgumentosLinhaDeComando.Normalizar(texto);
    foreach (var propriedade in Enum.GetValues<Propriedade>())
    {
      if (ArgumentosLinhaDeComando.Normalizar(propriedade.ToString()) == normalizado)
      {
        return propriedade;
      }
    }

    throw new ErroDeUsoException(
        $"Critério desconhecido: '{texto}'. Use luminosidade, tonalidade, saturacao, complexidade, entropia, faixa-tonalidade ou distancia.");
  }

  private static Ordem LerOrdem(string texto) => ArgumentosLinhaDeComando.Normalizar(texto) switch
  {
    "asc" or "crescente" => Ordem.Crescente,
    "desc" or "decrescente" => Ordem.Decrescente,
    _ => throw new ErroDeUsoException($"Ordem desconhecida: '{texto}'. Use asc ou desc."),
  };

  /// <summary>Procura a imagem de referência no CSV pelo caminho completo ou, se não houver, pelo nome do arquivo.</summary>
  private static ItemImagem EncontrarReferencia(string referencia, ItemImagem[] itens)
  {
    string caminhoCompleto = Path.GetFullPath(referencia);
    var porCaminho = itens.FirstOrDefault(i =>
        string.Equals(Path.GetFullPath(i.CaminhoArquivo), caminhoCompleto, StringComparison.OrdinalIgnoreCase));
    if (porCaminho is not null)
    {
      return porCaminho;
    }

    var porNome = itens
        .Where(i => string.Equals(Path.GetFileName(i.CaminhoArquivo), referencia, StringComparison.OrdinalIgnoreCase))
        .ToList();

    return porNome.Count switch
    {
      1 => porNome[0],
      0 => throw new ErroDeUsoException($"A imagem de referência '{referencia}' não está no CSV. Ela precisa ter sido extraída junto com as demais."),
      _ => throw new ErroDeUsoException($"Há {porNome.Count} imagens chamadas '{referencia}' no CSV. Informe o caminho completo."),
    };
  }

  /// <summary>Comparador do critério escolhido e a forma de exibir o valor que ele compara.</summary>
  private sealed record Criterio(IComparer<ItemImagem> Comparador, Func<ItemImagem, string> DescreverValor);
}
