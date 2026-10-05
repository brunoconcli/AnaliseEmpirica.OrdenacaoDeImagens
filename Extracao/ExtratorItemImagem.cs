namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using System.Collections.Concurrent;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Fase de carga: transforma arquivos de imagem em <see cref="ItemImagem"/>,
/// aplicando o pré-processamento e todos os extratores de propriedade.
/// </summary>
public sealed class ExtratorItemImagem
{
  private readonly PreProcessador _preProcessador;
  private readonly ExtratorLuminosidade _luminosidade = new();
  private readonly ExtratorTonalidade _tonalidade;
  private readonly ExtratorSaturacao _saturacao = new();
  private readonly ExtratorComplexidade _complexidade;
  private readonly ExtratorEntropia _entropia = new();

  public ExtratorItemImagem(ParametrosExtracao? parametros = null)
  {
    Parametros = parametros ?? ParametrosExtracao.Padrao;

    _preProcessador = new PreProcessador(Parametros.LadoRedimensionamento);
    _tonalidade = new ExtratorTonalidade(Parametros.LimiarCromaAcromatica, Parametros.LimiarConcentracaoMatiz);
    _complexidade = new ExtratorComplexidade(Parametros.LimiarBorda);
  }

  public ParametrosExtracao Parametros { get; }

  /// <summary>Lê, redimensiona e extrai as propriedades de um arquivo.</summary>
  public ItemImagem Extrair(string caminhoArquivo) =>
      Extrair(caminhoArquivo, _preProcessador.Carregar(caminhoArquivo));

  /// <summary>Extrai as propriedades de uma imagem já carregada (útil para testes com imagens sintéticas).</summary>
  public ItemImagem Extrair(string caminhoArquivo, ImagemRgb imagem) => new(
      caminhoArquivo,
      _luminosidade.Extrair(imagem),
      _tonalidade.Extrair(imagem),
      _saturacao.Extrair(imagem),
      _complexidade.Extrair(imagem),
      _entropia.Extrair(imagem));

  /// <summary>
  /// Processa, em paralelo, todas as imagens suportadas da pasta e de suas subpastas.
  /// Arquivos que falham são registrados em <see cref="ResultadoExtracao.Falhas"/> sem interromper o restante.
  /// </summary>
  /// <param name="progresso">
  /// Recebe o número de arquivos já processados e o total. É chamado a partir de
  /// várias threads, então a implementação precisa ser segura para concorrência.
  /// </param>
  public ResultadoExtracao ExtrairPasta(string pasta, IProgress<(int Processados, int Total)>? progresso = null)
  {
    if (!Directory.Exists(pasta))
    {
      throw new DirectoryNotFoundException($"Pasta não encontrada: '{pasta}'.");
    }

    string[] arquivos = Directory
        .EnumerateFiles(pasta, "*", SearchOption.AllDirectories)
        .Where(PreProcessador.IsSuportada)
        .Order(StringComparer.Ordinal)
        .ToArray();

    var itens = new ItemImagem?[arquivos.Length];
    var falhas = new ConcurrentBag<FalhaExtracao>();
    int processados = 0;

    Parallel.For(0, arquivos.Length, i =>
    {
      try
      {
        itens[i] = Extrair(arquivos[i]);
      }
      catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
      {
        falhas.Add(new FalhaExtracao(arquivos[i], ex.Message));
      }

      progresso?.Report((Interlocked.Increment(ref processados), arquivos.Length));
    });

    // Mantém a ordem dos arquivos (e não a ordem de término das threads), para que
    // o features.csv seja idêntico a cada execução.
    return new ResultadoExtracao(
        itens.OfType<ItemImagem>().ToList(),
        falhas.OrderBy(f => f.CaminhoArquivo, StringComparer.Ordinal).ToList());
  }
}
