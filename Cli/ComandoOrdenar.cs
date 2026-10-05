namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

using AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;
using AnaliseEmpirica.OrdenacaoDeImagens.Experimentos;
using AnaliseEmpirica.OrdenacaoDeImagens.Extracao;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Ordena as imagens do features.csv por um critério e mostra o resultado e o custo.
/// </summary>
public sealed class ComandoOrdenar : IComando
{
  private const int LimitePadrao = 20;
  private const string AlgoritmoPadrao = "merge";
  private const string TodosOsAlgoritmos = "todos";

  public string Nome => "ordenar";

  public string Resumo => "Ordena as imagens por um critério e mostra o resultado, as comparações e as movimentações";

  public string Uso => $"""
      Uso: ordenar [arquivo.csv] --por <critério> [opções]

      Lê as propriedades do CSV gerado por "extrair" (padrão: {Caminhos.Features}).

      {LeitorDeCriterio.Uso}

      Opções:
        --algoritmo <nome>|todos       {string.Join(", ", CatalogoDeAlgoritmos.Nomes)} ou todos (padrão: {AlgoritmoPadrao})
        --limite <n>                   Quantas imagens listar; 0 lista todas (padrão: {LimitePadrao})
        --copiar-para <pasta>          Copia as imagens para a pasta, numeradas na ordem do resultado
                                       (a pasta não pode existir ou deve estar vazia)
      """;

  public int Executar(ArgumentosLinhaDeComando argumentos)
  {
    argumentos.ValidarOpcoes([.. LeitorDeCriterio.Opcoes, "algoritmo", "limite", "copiar-para"]);

    var itens = LeitorDeFeatures.Ler(argumentos.Posicional(0));
    var criterio = LeitorDeCriterio.Ler(argumentos, itens);
    string nomeAlgoritmo = argumentos.Texto("algoritmo") ?? AlgoritmoPadrao;
    var algoritmos = ObterAlgoritmos(nomeAlgoritmo);
    int limite = argumentos.Inteiro("limite", LimitePadrao);
    string? pastaCopia = argumentos.Texto("copiar-para");
    ValidarPastaCopia(pastaCopia);

    Console.WriteLine($"Critério: {criterio.Comparador}");
    Console.WriteLine($"Imagens: {itens.Length}");
    if (!argumentos.Tem("algoritmo"))
    {
      Console.WriteLine($"Algoritmo: {algoritmos[0].Nome} (padrão; use --algoritmo para escolher outro ou \"todos\")");
    }

    Console.WriteLine();

    ItemImagem[]? ordenados = null;
    Console.WriteLine($"{"Algoritmo",-16} {"Comparações",14} {"Movimentações",14} {"Tempo (ms)",12}");

    foreach (var algoritmo in algoritmos)
    {
      var medicao = MedidorDeExecucao.Medir(algoritmo, itens, criterio.Comparador, MedidorDeExecucao.TempoMinimoPadrao);
      ordenados ??= medicao.Ordenado;

      Console.WriteLine($"{algoritmo.Nome,-16} {medicao.Comparacoes,14:N0} {medicao.Movimentacoes,14:N0} {medicao.TempoMs,12:F3}");
    }

    Console.WriteLine("Tempo indicativo, sem aquecimento nem repetições; para medidas confiáveis, use o comando benchmark.");
    Console.WriteLine();

    ImprimirResultado(ordenados!, criterio, limite);

    if (pastaCopia is not null)
    {
      CopiarNumeradas(ordenados!, pastaCopia);
    }

    return 0;
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

    return [LeitorDeFeatures.ObterAlgoritmo(nome)];
  }
}
