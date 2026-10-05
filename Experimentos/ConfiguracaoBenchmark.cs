namespace AnaliseEmpirica.OrdenacaoDeImagens.Experimentos;

using AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Fatores e parâmetros de uma bateria de experimentos (ver docs/metodologia.md).
/// </summary>
/// <param name="Comparador">Critério de ordenação; sua descrição (ToString) vai para a coluna Criterio.</param>
/// <param name="FracaoDesordem">Fração máxima de elementos fora de posição no caso quase ordenado.</param>
public sealed record ConfiguracaoBenchmark(
    IReadOnlyList<IAlgoritmoOrdenacao> Algoritmos,
    IReadOnlyList<int> Tamanhos,
    IReadOnlyList<CasoDeEntrada> Casos,
    int Repeticoes,
    IComparer<ItemImagem> Comparador,
    double FracaoDesordem,
    int SementeBase)
{
  public static IReadOnlyList<int> TamanhosPadrao { get; } = [100, 250, 500, 1_000, 2_500, 5_000, 10_000];

  public static IReadOnlyList<CasoDeEntrada> CasosPadrao { get; } =
      [CasoDeEntrada.Aleatorio, CasoDeEntrada.Crescente, CasoDeEntrada.Decrescente, CasoDeEntrada.QuaseOrdenado];

  public const int RepeticoesPadrao = 10;
  public const double FracaoDesordemPadrao = 0.05;
  public const int SementePadrao = 42;

  public int TotalDeExecucoes => Tamanhos.Count * Repeticoes * Casos.Count * Algoritmos.Count;

  public string DescricaoCriterio => Comparador.ToString() ?? Comparador.GetType().Name;

  /// <summary>
  /// Semente da repetição <paramref name="repeticao"/> com tamanho <paramref name="tamanho"/>.
  /// Fórmula fixa, para que qualquer linha dos resultados possa ser reproduzida.
  /// </summary>
  public int Semente(int tamanho, int repeticao) => unchecked(SementeBase + 100_003 * repeticao + tamanho);

  /// <summary>
  /// Descarta tamanhos maiores que o número de imagens disponíveis. Se algum foi descartado,
  /// acrescenta o total como último ponto, para que a base pequena seja aproveitada inteira.
  /// </summary>
  /// <remarks>
  /// O total só entra quando algum tamanho pedido não coube: numa base grande, acrescentá-lo
  /// sempre faria os algoritmos O(n²) rodarem com n muito maior que o pedido (horas de execução).
  /// </remarks>
  public static IReadOnlyList<int> AjustarTamanhos(IEnumerable<int> desejados, int totalDisponivel)
  {
    var pedidos = desejados.ToList();
    var tamanhos = pedidos.Where(n => n >= 2 && n <= totalDisponivel).Distinct().Order().ToList();
    bool algumDescartado = pedidos.Any(n => n > totalDisponivel);
    if (algumDescartado && totalDisponivel >= 2 && (tamanhos.Count == 0 || tamanhos[^1] < totalDisponivel))
    {
      tamanhos.Add(totalDisponivel);
    }

    return tamanhos;
  }
}
